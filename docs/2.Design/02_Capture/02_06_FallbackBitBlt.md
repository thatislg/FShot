# FallbackBitBlt — Thiết kế chi tiết Cơ chế Dự phòng GDI BitBlt

> Tài liệu này thiết kế chi tiết cơ chế dự phòng an toàn bằng Win32 GDI `BitBlt` khi backend chính `Windows.Graphics.Capture` không khả dụng hoặc bị từ chối quyền trên Windows (`FR-WIN-001`, `FR-SYS-017`).  
> **Nguyên tắc tài liệu:** Thiết kế thuần kiến trúc, ma trận quyết định tự động chuyển đổi, xử lý phiên Desktop không tương tác — **hoàn toàn không sử dụng code mẫu**.

---

## 1. Vai trò của Phương án Dự phòng

Mặc dù `Windows.Graphics.Capture` (WGC) là backend chính hiện đại, F-Shot vẫn bắt buộc phải duy trì cơ chế dự phòng `BitBlt` vì các lý do thực tế sau:
1. **Khả năng tương thích môi trường đặc thù:** Các môi trường máy ảo (Virtual Machines không có GPU Passthrough), phiên kết nối từ xa (Remote Desktop RDP / Citrix), hoặc máy trạm sử dụng Windows 10 phiên bản cũ thường không hỗ trợ DirectX Desktop Duplication/WGC.
2. **Quyền riêng tư và bảo mật Windows:** Một số bản dựng Windows Enterprise hoặc người dùng kích hoạt chính sách chặn ứng dụng ghi màn hình sẽ khiến WinRT từ chối cấp surface.
3. **Độ ổn định tối thượng:** GDI `BitBlt` là API cơ bản nhất của nhân Windows tồn tại qua nhiều thập kỷ, đảm bảo F-Shot **luôn chụp được màn hình** trong mọi tình huống thay vì bị crash hoặc hiện màn hình đen.

---

## 2. Ma trận Quyết định Tự động Điều phối (Auto-Fallback Decision Matrix)

Bộ điều phối tổng hợp `CompositeCaptureService` sử dụng bảng quyết định sau để lựa chọn backend chụp phù hợp:

| Điều kiện Hệ thống / Cấu hình | Backend được chọn | Lý do quyết định |
| :--- | :---: | :--- |
| Cấu hình người dùng ép dùng GDI (`captureBackend = "Gdi"` hoặc cờ CLI `--force-gdi`) | **GDI BitBlt** | Tôn trọng lựa chọn rõ ràng của người dùng khi gặp sự cố driver GPU. |
| Hệ điều hành Windows Build < 18362 (trước Windows 10 1903) | **GDI BitBlt** | WinRT API WGC chưa tồn tại trên phiên bản hệ điều hành này. |
| Cấu hình `Auto` + Windows Build $\ge$ 18362 + WGC hỗ trợ | **WGC (DirectX)** | Tận dụng tối đa ưu thế hiệu năng phần cứng và độ chuẩn xác Mixed DPI. |
| WGC ném ngoại lệ phân quyền (`UnauthorizedAccessException`) | **GDI BitBlt** | Người dùng từ chối quyền WGC; fallback sang GDI để tiếp tục phục vụ thao tác chụp. |
| WGC ném lỗi Direct3D Device Removed / GPU Hang | **GDI BitBlt** | Card đồ họa bị treo; chuyển sang CPU GDI để tránh crash ứng dụng. |
| Phiên làm việc từ xa Remote Desktop (RDP) không có 3D Acceleration | **GDI BitBlt** | GDI hoạt động ổn định nhất trên kênh ảo RDP Desktop. |

---

## 3. Kiến trúc Chụp GDI và Đảm bảo Phiên Màn hình Tương tác

Để `BitBlt` chụp thành công nội dung màn hình Windows từ một tiến trình chạy nền (daemon), hệ thống phải giải quyết bài toán phân quyền Window Station và Desktop:

```
┌────────────────────────────────────────────────────────┐
│ 1. Xác thực Phiên Màn hình (Desktop Station Security)  │
│ - Mở Window Station tương tác "winsta0"               │
│ - Thiết lập Desktop tương tác mặc định "default"       │
│ - Ngăn ngừa lỗi chụp ra màn hình đen khi chạy nền      │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│ 2. Khởi tạo Ngữ cảnh Thiết bị (Device Context Pipeline)│
│ - GetDC(IntPtr.Zero) lấy Screen DC của toàn Desktop   │
│ - CreateCompatibleDC tạo Memory DC trong bộ nhớ        │
│ - CreateCompatibleBitmap tạo vùng đệm bitmap           │
│ - SelectObject gán bitmap vào Memory DC                │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│ 3. Sao chép Khối Pixel (Raster Transfer)               │
│ - Gọi BitBlt với cờ kết hợp:                           │
│   SRCCOPY | CAPTUREBLT (0x00CC0020 | 0x40000000)      │
│   (CAPTUREBLT bắt buộc để chụp được các cửa sổ bán     │
│    trong suốt, menu đổ bóng và popup chuột)            │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│ 4. Trích xuất Dữ liệu Pixel sang Mảng Byte Quản lý     │
│ - Cấu hình BITMAPINFOHEADER với chiều cao âm (-H)      │
│   để thu được thứ tự quét từ trên xuống (Top-down)     │
│ - Đặt biBitCount = 32us, biCompression = BI_RGB       │
│ - GetDIBits sao chép pixel trực tiếp sang byte[]       │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│ 5. Thu hồi Tuyệt đối Tài nguyên Unmanaged (GDI Leak)   │
│ - Khôi phục Object ban đầu của DC                      │
│ - DeleteObject(hBitmap), DeleteDC(hdcMem)              │
│ - ReleaseDC(IntPtr.Zero, hdcSrc)                       │
│ - Bảo vệ bằng các khối finally nghiêm ngặt             │
└────────────────────────────────────────────────────────┘
```

---

## 4. Giải pháp Giảm thiểu Sai lệch Mixed DPI trong GDI Fallback

GDI `BitBlt` sao chép theo không gian tọa độ desktop do hệ điều hành quản lý. Khi tồn tại Mixed DPI (các màn hình có tỉ lệ phóng to khác nhau), Windows GDI dễ bị kéo giãn bitmap.

Để hạn chế tối đa nhược điểm này khi phải dùng GDI Fallback:
1. **Chụp Từng Màn hình Riêng biệt:**
   - Thay vì gọi `BitBlt` một lần bao trọn toàn bộ Virtual Screen (rất dễ bị Windows tự động nội suy làm mờ toàn bộ ảnh), hệ thống duyệt qua danh sách màn hình từ `ScreenEnumeration`.
   - Với mỗi màn hình, tính toán vị trí vật lý chính xác dựa trên `LogicalBounds` nhân với `ScaleFactor` của riêng màn hình đó và gọi `BitBlt` trên phạm vi đơn lẻ.
2. **Cờ DPI Awareness Cấp Tiến trình:**
   - Đảm bảo tiến trình F-Shot đã được đánh dấu `PerMonitorV2` trong tệp cấu hình ứng dụng (`app.manifest`), giúp GDI không bị Windows ảo hóa độ phân giải (DPI Virtualization).
3. **Cảnh báo Thông minh:**
   - Khi phát hiện hệ thống có cấu hình Mixed DPI nhưng người dùng đang chạy ở chế độ fallback GDI, ứng dụng ghi log khuyến nghị và hiển thị thông báo một lần gợi ý bật tính năng tăng tốc đồ họa WGC để có chất lượng hình ảnh sắc nét nhất.
