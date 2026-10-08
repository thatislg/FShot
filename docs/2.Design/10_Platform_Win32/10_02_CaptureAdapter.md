# CaptureAdapter — Thiết kế chi tiết Bộ điều phối Chụp màn hình Win32

> Tài liệu này thiết kế chi tiết kiến trúc tầng điều phối chụp màn hình (`CaptureAdapter`) trong `FShot.Platform.Win32`, chịu trách nhiệm hiện thực hóa giao diện `ICaptureService` của tầng Core (`FR-WIN-001`, `FR-SYS-017`, `FR-CAP-01`).  
> **Nguyên tắc tài liệu:** Thiết kế thuần kiến trúc, mô hình đa backend, trừu tượng hóa giao diện phần cứng và quản lý vòng đời tài nguyên — **hoàn toàn không sử dụng code mẫu**.

---

## 1. Mục đích và Phân tầng Kiến trúc

Tầng `FShot.Core` hoàn toàn độc lập với hệ điều hành và chỉ giao tiếp với dịch vụ chụp thông qua giao diện trừu tượng `ICaptureService`:
- `CaptureVirtualScreenAsync() : Async<Result<CaptureResult, CaptureError>>`
- `CaptureScreenAsync(screenIndex: int) : Async<Result<CaptureResult, CaptureError>>`
- `CaptureCursorScreenAsync() : Async<Result<CaptureResult, CaptureError>>`

Nhiệm vụ của `CaptureAdapter` trong `FShot.Platform.Win32`:
1. Giấu toàn bộ độ phức tạp của COM Interop, DirectX Direct3D 11, WinRT API và GDI Win32 P/Invoke đằng sau giao diện `ICaptureService`.
2. Đóng vai trò là một **Bộ điều phối Đa Backend (Multi-Backend Composite Adapter)**: tự động lựa chọn giữa `WgcCaptureService` (chính) và `WindowsCaptureService` (dự phòng GDI BitBlt).
3. Đảm bảo toàn bộ tài nguyên đồ họa unmanaged (HDC, HBITMAP, ID3D11Device, ID3D11Texture2D) được giải phóng 100% sau mỗi phiên chụp, ngăn ngừa rò rỉ bộ nhớ (Memory Leak / GDI Leak).

---

## 2. Mô hình Đa Backend (Multi-Backend Architecture)

```
┌────────────────────────────────────────────────────────────────────────┐
│                          FShot.Core.Domain                             │
│                     [ Giao diện ICaptureService ]                      │
└───────────────────────────────────▲────────────────────────────────────┘
                                    │ Thực thi
┌───────────────────────────────────┴────────────────────────────────────┐
│                       FShot.Platform.Win32                             │
│                                                                        │
│                    ┌────────────────────────────┐                      │
│                    │  CompositeCaptureService   │ (Bộ điều phối chính) │
│                    └──────────────┬─────────────┘                      │
│                                   │                                    │
│                 ┌─────────────────┴─────────────────┐                  │
│                 ▼                                   ▼                  │
│   ┌───────────────────────────┐       ┌───────────────────────────┐    │
│   │     WgcCaptureService     │       │   WindowsCaptureService   │    │
│   │   (Backend WinRT / WGC)   │       │   (Backend GDI BitBlt)    │    │
│   │ - Tăng tốc GPU DirectX    │       │ - Tương thích tối đa      │    │
│   │ - Chuẩn xác Mixed DPI     │       │ - Fallback khi WGC lỗi    │    │
│   └───────────────────────────┘       └───────────────────────────┘    │
│                 ▲                                   ▲                  │
│                 │                                   │                  │
│   ┌─────────────┴─────────────┐       ┌─────────────┴─────────────┐    │
│   │     Direct3DInterop       │       │    Screen / Desktop DC    │    │
│   │ - D3D11 Hardware Device   │       │ - winsta0\default Desktop │    │
│   │ - Staging Texture Copier  │       │ - User32 / Gdi32 API      │    │
│   └───────────────────────────┘       └───────────────────────────┘    │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Quy trình Thực thi của `CompositeCaptureService`

Khi có yêu cầu chụp (ví dụ `CaptureVirtualScreenAsync`):

1. **Giai đoạn Kiểm tra Cấu hình:**
   - Đọc thuộc tính `CaptureBackend` từ cấu hình hiện tại (`AppConfig`).
   - Nếu người dùng cấu hình rõ ràng là `Gdi`, lập tức chuyển hướng yêu cầu tới `WindowsCaptureService.CaptureVirtualScreenAsync` và bỏ qua WGC.
2. **Giai đoạn Thực thi Backend Chính (WGC):**
   - Nếu cấu hình là `Auto` hoặc `Wgc`:
     - Kiểm tra khả năng hỗ trợ của hệ điều hành (`Build >= 18362` và `GraphicsCaptureSession.IsSupported`).
     - Nếu không hỗ trợ, ghi log thông tin và chuyển thẳng sang GDI.
     - Nếu hỗ trợ, gọi `WgcCaptureService.CaptureVirtualScreenAsync()`.
3. **Giai đoạn Xử lý Lỗi & Tự động Dự phòng (Failover Engine):**
   - Nếu `WgcCaptureService` trả về `Ok CaptureResult`, trả ngay kết quả cho Core.
   - Nếu `WgcCaptureService` ném ngoại lệ hoặc trả về lỗi thuộc nhóm không thể phục hồi (như từ chối quyền, card đồ họa bận, lỗi khởi tạo texture):
     - Ghi nhật ký cảnh báo lỗi của WGC.
     - Tự động kích hoạt cơ chế dự phòng: gọi `WindowsCaptureService.CaptureVirtualScreenAsync()`.
     - Nếu GDI thành công, trả về kết quả kèm cờ ghi chú backend dự phòng đã được sử dụng.
     - Nếu cả GDI cũng thất bại, trả về lỗi chi tiết `CaptureError.Unknown`.

---

## 4. Quản lý Vòng đời Tài nguyên Đồ họa (Resource Lifecycle Management)

Để đảm bảo hiệu năng tối đa và không làm tiêu hao bộ nhớ máy tính:

### 4.1 Tài nguyên Direct3D 11 (WGC)
- **Thiết bị Direct3D 11 (`ID3D11Device`):** Được khởi tạo một lần duy nhất theo mô hình Lazy Singleton và tái sử dụng cho mọi lần chụp trong suốt vòng đời của ứng dụng.
- **Khung hình & Staging Texture (`ID3D11Texture2D`):** Chỉ tồn tại tạm thời trong hàm chụp. Ngay sau khi hoàn tất lệnh `ID3D11DeviceContext::Unmap` và sao chép mảng byte sang bộ nhớ managed, toàn bộ texture tạm và frame pool session được hủy bỏ dứt điểm (`Dispose`).
- **Xử lý sự kiện Device Lost:** Nếu card đồ họa cập nhật driver hoặc reset (TDR), thiết bị Direct3D cũ bị hủy bỏ và tự động khởi tạo lại thiết bị mới trong lần chụp kế tiếp.

### 4.2 Tài nguyên Win32 GDI (BitBlt)
- Toàn bộ các handle GDI (`HDC`, `HBITMAP`) bắt buộc phải được giải phóng bằng các hàm tương ứng `DeleteDC`, `DeleteObject`, `ReleaseDC` trong các khối bảo vệ dọn dẹp tài nguyên nghiêm ngặt (`finally`), bảo đảm số lượng GDI Handle của tiến trình F-Shot luôn duy trì ở mức tối thiểu.

---

## 5. Bảng Ánh xạ Mã Lỗi Hệ thống sang Domain (Error Mapping Matrix)

`CaptureAdapter` chuẩn hóa toàn bộ các mã lỗi từ hệ thống Windows thành các trường của Discriminated Union `CaptureError` trong `FShot.Core`:

| Lỗi Hệ thống Windows (Win32 / WinRT / COM) | Mã Lỗi Domain tương ứng (`CaptureError`) |
| :--- | :--- |
| `HRESULT 0x80070005 (E_ACCESSDENIED)` / Người dùng chặn quyền ghi màn hình | `CaptureError.CaptureApiNotAvailable` |
| `ERROR_INVALID_MONITOR_HANDLE` / Index màn hình không tồn tại trong danh sách | `CaptureError.ScreenNotFound(index)` |
| `GetDIBits` trả về 0 dòng quét / Texture có kích thước chiều rộng hoặc cao bằng 0 | `CaptureError.SurfaceIsEmpty` |
| Thao tác bị hủy bởi người dùng hoặc hệ thống nhận tín hiệu đóng ứng dụng | `CaptureError.UserCancelled` |
| Ngoại lệ không xác định từ driver đồ họa hoặc Win32 P/Invoke | `CaptureError.Unknown(thông điệp lỗi)` |
