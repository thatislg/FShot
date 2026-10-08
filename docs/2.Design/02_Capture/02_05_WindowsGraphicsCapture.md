# WindowsGraphicsCapture — Thiết kế chi tiết Backend Chụp màn hình WinRT

> Tài liệu này thiết kế chi tiết kiến trúc chụp màn hình hiện đại bằng API `Windows.Graphics.Capture` (WGC) kết hợp Direct3D 11 trên Windows 10 (1903+) và Windows 11 (`FR-WIN-001`, `FR-SYS-017`, `FR-SYS-018`).  
> **Nguyên tắc tài liệu:** Thiết kế thuần kiến trúc, đường ống xử lý GPU-to-CPU, giải thuật ghép nối đa màn hình Mixed-DPI — **hoàn toàn không sử dụng code mẫu**.

---

## 1. Động lực Kỹ thuật và Lợi thế Kiến trúc

So với phương thức GDI `BitBlt` truyền thống, `Windows.Graphics.Capture` mang lại các ưu thế vượt trội:
1. **Khắc phục triệt để biến dạng Mixed DPI:** Hoạt động trực tiếp ở tầng phần cứng Desktop Duplication/Compositor, nhận frame ở đúng độ phân giải vật lý gốc của từng màn hình mà không bị hệ điều hành tự động scale làm mờ.
2. **Khả năng chụp cửa sổ hiện đại:** Chụp chuẩn xác các ứng dụng UWP, XAML Islands, cửa sổ tăng tốc phần cứng (Chrome, VS Code, game không viền) mà BitBlt thường chỉ chụp ra vùng màu đen.
3. **Hiệu năng cao và độ trễ thấp:** Quá trình chụp diễn ra hoàn toàn trong bộ nhớ VRAM của GPU; chỉ sao chép sang RAM khi cần đọc mảng byte pixel.
4. **Không phụ thuộc vào tương tác cửa sổ:** Không cần cửa sổ đích phải hiển thị trên cùng để lấy nội dung.

---

## 2. Kiến trúc Thành phần (Component Architecture)

Hệ thống chụp WGC bao gồm 5 tầng thành phần liên kết chặt chẽ:

```
┌────────────────────────────────────────────────────────────┐
│ 1. Tầng Định danh Mục tiêu (Capture Target Identification) │
│ - ScreenEnumeration cung cấp HMONITOR của từng màn hình   │
│ - IGraphicsCaptureItemInterop::CreateForMonitor            │
│   biến đổi HMONITOR thành GraphicsCaptureItem              │
└─────────────────────────────┬──────────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────────┐
│ 2. Tầng Thiết bị Đồ họa (Hardware Direct3D 11 Pipeline)    │
│ - Direct3D 11 Hardware Device (Driver Type Hardware)       │
│ - Chuyển đổi sang WinRT IDirect3DDevice qua DXGI Interop   │
└─────────────────────────────┬──────────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────────┐
│ 3. Tầng Bắt Khung hình (Frame Pool & Capture Session)      │
│ - Direct3D11CaptureFramePool (Free-Threaded, Buffer = 1)   │
│ - GraphicsCaptureSession:                                  │
│   * IsCursorCaptureEnabled = false (Tắt con trỏ vẽ đè)     │
│   * IsBorderRequired = false (Tắt viền vàng Windows 11)   │
└─────────────────────────────┬──────────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────────┐
│ 4. Tầng Đọc Dữ liệu (GPU-to-CPU Staging Texture Pipeline)   │
│ - Lấy Direct3D11CaptureFrame đầu tiên                      │
│ - CopyResource từ Render Texture sang Staging Texture      │
│   (cờ D3D11_USAGE_STAGING, D3D11_CPU_ACCESS_READ)          │
│ - ID3D11DeviceContext::Map để lấy con trỏ mảng byte BGRA32 │
└─────────────────────────────┬──────────────────────────────┘
                              │
┌─────────────────────────────▼──────────────────────────────┐
│ 5. Tầng Đóng gói Domain (CaptureResult Assembly)           │
│ - Trích xuất mảng byte thô, Stride, Width, Height          │
│ - Gán VirtualBounds và ScaleFactor tương ứng               │
│ - Trả về CaptureResult cho FShot.Core                      │
└────────────────────────────────────────────────────────────┘
```

---

## 3. Kiến trúc Đa màn hình Hỗn hợp (Mixed-DPI Virtual Screen Composition)

Trong cấu hình đa màn hình với các tỉ lệ scale khác nhau (ví dụ Màn hình 1 là 4K tỉ lệ 150%, Màn hình 2 là Full HD tỉ lệ 100%):
- Nếu dùng một session duy nhất chụp toàn bộ Virtual Desktop, Windows Compositor buộc phải nội suy kéo giãn một trong hai màn hình, dẫn đến hiện tượng vỡ hình và sai lệch tọa độ pixel.

### Giải pháp Kiến trúc: Ghép nối Độc lập (Multi-Session Discrete Composition)
Thay vì chụp chung một lần, F-Shot thực hiện quy trình 4 bước:

1. **Chụp Song song Từng Màn hình:**
   - Khởi tạo session WGC riêng biệt cho từng `HMONITOR` phát hiện được.
   - Mỗi session thu về một mảng byte ảnh ở đúng độ phân giải vật lý bản địa (`PhysicalWidth = LogicalWidth × ScaleFactor`).
2. **Khởi tạo Canvas Ảo Trung gian:**
   - Tính toán kích thước Virtual Desktop theo hệ tọa độ Logical chuẩn hóa:
     - `VirtualWidth = Max(Right của mọi màn hình) - Min(Left của mọi màn hình)`.
     - `VirtualHeight = Max(Bottom của mọi màn hình) - Min(Top của mọi màn hình)`.
3. **Tổng hợp Bitmap (Assembly):**
   - Đưa các ảnh thành phần lên Virtual Canvas. Tọa độ đặt của từng ảnh được căn chỉnh chuẩn xác theo `VirtualBounds.X` và `VirtualBounds.Y` của màn hình tương ứng.
4. **Bảo toàn Độ phân giải Xuất:**
   - Lưu thông tin `ScaleFactor` độc lập của từng màn hình trong danh sách mô tả để khi người dùng kéo vùng chọn trên màn hình nào, bộ render export sẽ crop ảnh gốc từ bitmap của chính màn hình đó, đảm bảo độ sắc nét 100%.

---

## 4. Đường ống Chuyển đổi Bộ nhớ (GPU Staging Texture Pipeline)

Để đọc dữ liệu pixel mà không gây đứng luồng (freeze UI):

1. **Định dạng Pixel Chuẩn:** Bắt buộc sử dụng `DirectXPixelFormat.B8G8R8A8UIntNormalized` (tương ứng BGRA32). Đây là định dạng bản địa tối ưu nhất của Windows Desktop Compositor, không cần chuyển đổi không gian màu.
2. **Cấu hình Staging Texture:**
   - Texture được tạo với thuộc tính `Usage = D3D11_USAGE_STAGING`.
   - Cờ truy cập CPU: `CPUAccessFlags = D3D11_CPU_ACCESS_READ`.
   - Cờ liên kết: `BindFlags = 0` (không liên kết với pipeline render để giảm thiểu overhead).
3. **Quy trình Ánh xạ (Mapping) & Giải phóng:**
   - Lệnh `CopyResource` sao chép surface từ GPU VRAM sang Staging Texture.
   - Lệnh `Map(D3D11_MAP_READ)` khóa vùng nhớ Staging và lấy con trỏ dữ liệu thô.
   - Sao chép bộ nhớ nhanh (Memory Block Copy) sang mảng byte được quản lý.
   - Gọi ngay lập tức `Unmap` và giải phóng đối tượng Frame / Texture để trả lại tài nguyên VRAM.

---

## 5. Quyết định Thiết kế Kiến trúc (Architectural Decisions)

| Nội dung quyết định | Lựa chọn kiến trúc | Lý do & Tác động |
| :--- | :--- | :--- |
| **Giao diện chọn vùng `GraphicsCapturePicker`** | **Không sử dụng** | F-Shot cần kích hoạt chụp tức thì (<50ms) bằng phím nóng hoặc menu Tray. Hộp thoại Picker của Windows làm gián đoạn người dùng và không phù hợp với luồng tương tác chụp nhanh. |
| **Tạo `GraphicsCaptureItem`** | Dùng COM Interop `IGraphicsCaptureItemInterop::CreateForMonitor` | Cho phép ứng dụng tự động lấy handle của màn hình bất kỳ trong nền mà không yêu cầu người dùng bấm chọn. |
| **Kích thước FramePool Buffer** | Đúng **1 Frame** (Single Buffer) | F-Shot chỉ chụp ảnh tĩnh (screenshot snapshot), không quay video màn hình. Buffer 1 frame giúp giảm thiểu tối đa mức chiếm dụng RAM/VRAM. |
| **Đường viền chụp màu vàng trên Windows 11** | Đặt `IsBorderRequired = false` | Từ Windows 11 Build 22000 trở lên, Windows mặc định vẽ viền vàng quanh màn hình đang chụp. Tắt viền vàng giúp giao diện chụp ảnh tự nhiên, không làm che khuất các phần tử ở mép màn hình. |
| **Hiển thị con trỏ chuột hệ thống** | Đặt `IsCursorCaptureEnabled = false` | Con trỏ chuột hệ thống sẽ không bị in chết vào ảnh chụp tĩnh, giúp vùng chọn sạch sẽ. Khi cần vẽ con trỏ, F-Shot có thể tự render lớp con trỏ riêng biệt. |

---

## 6. Xử lý Lỗi và Phân loại Ngoại lệ (Error Classification Matrix)

| Loại lỗi | Nguyên nhân gốc rễ | Phản ứng của hệ thống |
| :--- | :--- | :--- |
| **OS Version Unsupported** | Windows cũ hơn phiên bản 1903 (Build < 18362) | Tự động chuyển hướng ngay sang fallback GDI `BitBlt`. |
| **Access Denied / Consent Refused** | Chính sách bảo mật Group Policy hoặc người dùng từ chối quyền ghi màn hình | Ghi log cảnh báo và tự động fallback sang GDI `BitBlt`. |
| **Graphics Adapter Reset / TDR** | Driver card đồ họa bị khởi động lại hoặc crash đột ngột | Giải phóng device cũ, tạo lại Direct3D 11 device mới; nếu tiếp tục lỗi thì fallback sang `BitBlt`. |
| **Remote Desktop (RDP) / VM** | Môi trường máy ảo hoặc phiên RDP không hỗ trợ tăng tốc phần cứng WGC | Phát hiện và tự động chuyển sang chế độ GDI `BitBlt`. |
| **Monitor Disconnect During Capture** | Màn hình bị rút cáp đúng lúc đang bắt frame | Bắt lỗi invalid handle, tự động refresh danh sách màn hình từ `ScreenEnumeration` và chụp lại trên các màn hình còn lại. |
