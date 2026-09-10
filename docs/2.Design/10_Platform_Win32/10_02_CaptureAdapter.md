# CaptureAdapter — Thiết kế chi tiết

> Tài liệu này mô tả cách `FShot.Platform.Win32` triển khai `ICaptureService` bằng Windows.Graphics.Capture.

---

## 1. Mục đích

Chụp màn hình Windows và trả về `CaptureResult` cho `FShot.Core`. Giấu mọi chi tiết COM/WinRT/DirectX bên trong project này.

---

## 2. Các thành phần

### 2.1 Direct3D11 Device

Tạo một `ID3D11Device` để làm việc với `Direct3D11CaptureFramePool`. Device này dùng chung cho mọi lần chụp.

### 2.2 GraphicsCaptureItem

Tạo từ màn hình (`Display`). Mỗi màn hình trong `ScreenInfo` có thể chuyển thành một `GraphicsCaptureItem`.

### 2.3 FramePool và Session

- `Direct3D11CaptureFramePool`: chứa các frame.
- `GraphicsCaptureSession`: điều khiển việc chụp.
- Bắt đầu session, đợi frame đầu tiên, sau đó dừng.

### 2.4 BitmapAdapter

Copy frame surface sang CPU-readable texture, sau đó đọc byte array. Kết quả là `byte[]` theo định dạng BGRA32.

---

## 3. Luồng chụp một màn hình

1. Lấy `ScreenInfo` theo index.
2. Tạo `GraphicsCaptureItem` từ màn hình.
3. Tạo `FramePool` với kích thước physical của màn hình.
4. Tạo `Session`, bắt đầu chụp.
5. Đợi frame đầu tiên.
6. Copy surface sang texture CPU-readable.
7. Đọc `byte[]`.
8. Dừng session, giải phóng.
9. Trả về `CaptureResult` với `VirtualBounds`, `ScaleFactor`, `ScreenIndex`.

---

## 4. Công thức kích thước

Cho màn hình logical `(L, T, R, B)` và scale `s`:

`physicalWidth = round((R - L) * s)`
`physicalHeight = round((B - T) * s)`

Stride với BGRA32:

`stride = physicalWidth * 4`

Tổng byte:

`totalBytes = stride * physicalHeight`

---

## 5. Lỗi và xử lý

| Lỗi | Nguyên nhân | Xử lý |
|-----|-------------|-------|
| PermissionDenied | Người dùng từ chối quyền | Hiển thị thông báo, hướng dẫn bật quyền |
| CaptureApiNotAvailable | OS cũ hơn 1903 | Fallback BitBlt (nếu có) |
| SurfaceIsEmpty | Copy surface thất bại | Thử lại hoặc báo lỗi |
| ScreenNotFound | Index không hợp lệ | Dùng màn hình chính hoặc báo lỗi |

---

## 6. Kết nối với code

- `src/FShot.Platform.Win32/Capture/GraphicsCapture.fs`
- `src/FShot.Platform.Win32/Capture/Direct3DInterop.fs`
- `src/FShot.Platform.Win32/Capture/BitmapAdapter.fs`
- Triển khai `ICaptureService` từ `FShot.Core.Domain`.
