# WindowsGraphicsCapture — Thiết kế chi tiết

> Tài liệu này mô tả cách F-Shot sử dụng Windows.Graphics.Capture để chụp màn hình.
> Đây là backend chụp chính trên Windows 10 1903 trở lên.

---

## 1. Windows.Graphics.Capture là gì?

`Windows.Graphics.Capture` là API của Windows cho phép ứng dụng chụp màn hình, cửa sổ, hoặc một khu vực màn hình. API này sử dụng DirectX để sao chép nội dung màn hình vào một surface, sau đó ứng dụng có thể đọc pixel từ đó.

Ưu điểm của API này so với BitBlt:

- Hỗ trợ đúng Mixed DPI.
- Có thể chụp cửa sổ UWP và các cửa sổ hiện đại.
- Hiệu năng tốt hơn.
- Được Microsoft khuyến khích cho ứng dụng chụp màn hình mới.

Nhược điểm:

- Chỉ chạy trên Windows 10 version 1903 trở lên (`Windows 10.0.18362`).
- Người dùng có thể bị hỏi quyền khi ứng dụng lần đầu chụp.
- Cần hiểu rõ COM interop và WinRT.

---

## 2. Các khái niệm cần biết

### 2.1 GraphicsCaptureItem

`GraphicsCaptureItem` là đối tượng đại diện cho "mục tiêu" cần chụp. Nó có thể là:

- Một màn hình (`Display`).
- Một cửa sổ (`Window`).
- Một khu vực tự do trong tương lai (`Region`).

Trong F-Shot, mục tiêu chính là màn hình (`Display`).

### 2.2 GraphicsCapturePicker

`GraphicsCapturePicker` là hộp thoại cho phép người dùng chọn cửa sổ hoặc màn hình để chụp. F-Shot có thể dùng picker trong chế độ chụp tương tác, nhưng để tự động chụp toàn màn hình, cần tạo `GraphicsCaptureItem` từ màn hình trực tiếp.

### 2.3 Direct3D11CaptureFramePool

`Direct3D11CaptureFramePool` là nơi chứa các khung hình (frames) được chụp. Mỗi khung hình là một surface Direct3D. Ứng dụng đăng ký một callback để nhận khung hình mới.

### 2.4 Direct3D11CaptureFrame

`Direct3D11CaptureFrame` là một khung hình đơn lẻ. Nó chứa surface, kích thước, và thời gian. Ứng dụng đọc surface này để lấy pixel.

### 2.5 Surface đọc được CPU

Surface từ `Direct3D11CaptureFrame` ban đầu nằm trên GPU. Để đọc pixel bằng CPU, cần copy surface sang một texture khác có `CPU read access`.

---

## 3. Luồng hoạt động

Luồng chụp một màn hình bằng Windows.Graphics.Capture:

1. Liệt kê màn hình, chọn màn hình cần chụp.
2. Tạo `GraphicsCaptureItem` từ màn hình đã chọn.
3. Tạo Direct3D11 device và `Direct3D11CaptureFramePool`.
4. Tạo `GraphicsCaptureSession` từ item và frame pool.
5. Bắt đầu session.
6. Khi có frame mới, copy surface sang CPU-readable texture.
7. Đọc mảng byte từ texture.
8. Dừng session và giải phóng tài nguyên.
9. Trả về `CaptureResult`.

Trong F-Shot, luồng này được gói trong một hàm bất đồng bộ, ví dụ `CaptureScreenAsync(screenIndex)`.

---

## 4. Tính toán kích thước bitmap

Khi chụp một màn hình, API trả về surface với kích thước physical pixel. Kích thước này phụ thuộc vào độ phân giải vật lý và tỉ lệ phóng to.

Cho một màn hình có:
- Logical bounds: `(left, top, right, bottom)`.
- Scale factor: `s`.

Kích thước physical:

`physicalWidth = round((right - left) * s)`
`physicalHeight = round((bottom - top) * s)`

Ví dụ: màn hình logical `(0, 0, 1920, 1080)`, scale `1.5`.

- `physicalWidth = round(1920 * 1.5) = 2880`
- `physicalHeight = round(1080 * 1.5) = 1620`

Bitmap thu được sẽ là 2880 × 1620 physical pixel.

---

## 5. Xử lý Mixed DPI

Khi chụp màn hình có Mixed DPI, API tự động trả về ảnh ở độ phân giải physical của màn hình đó. Tuy nhiên, tọa độ con trỏ từ UI framework thường là logical pixel.

Cách xử lý:

- Lưu `VirtualBounds` và `ScaleFactor` trong `CaptureResult`.
- Tất cả tọa độ trong domain model dùng logical pixel.
- Khi cần crop hoặc render, nhân logical với `ScaleFactor` để có physical.

Ví dụ: con trỏ tại `(150, 120)` logical trên màn hình scale `1.5` tương đương `(225, 180)` physical.

---

## 6. Các lỗi thường gặp

### 6.1 Thiếu quyền chụp màn hình

Khi ứng dụng lần đầu chụp, Windows có thể hiển thị thông báo yêu cầu quyền. Nếu người dùng từ chối, API sẽ báo lỗi hoặc trả về surface đen.

### 6.2 Màn hình bị ngắt kết nối

Nếu màn hình bị ngắt kết nối trong lúc chụp, `GraphicsCaptureItem` có thể trở nên không hợp lệ. Cần kiểm tra lại danh sách màn hình trước khi chụp.

### 6.3 Surface đen hoặc trống

Nếu copy surface chưa đúng cách, mảng byte có thể toàn 0. Nguyên nhân thường là chưa đợi frame sẵn sàng hoặc chưa map CPU-readable texture đúng cách.

---

## 7. Các câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Có dùng `GraphicsCapturePicker` cho chế độ tương tác? | Picker tăng tính linh hoạt nhưng làm chậm luồng |
| Có nên chụp toàn bộ Virtual Screen bằng một session hay nhiều session? | Một session đơn giản hơn, nhiều session chính xác hơn cho Mixed DPI |
| Làm thế nào xử lý khi `Windows.Graphics.Capture` không khả dụng? | Cần fallback BitBlt |
| Có cần buffer nhiều frame hay chỉ cần một frame? | Chụp màn hình tĩnh chỉ cần một frame |

---

## 8. Kết nối với phần triển khai

Những quyết định trong tài liệu này sẽ được đưa vào:

- `src/FShot.Platform.Win32/Capture/GraphicsCapture.fs`: triển khai Windows.Graphics.Capture.
- `src/FShot.Platform.Win32/Capture/Direct3DInterop.fs`: tạo device và copy surface.
- `src/FShot.Platform.Win32/Capture/BitmapAdapter.fs`: chuyển texture thành `CaptureResult`.

---

*Sau khi chốt WindowsGraphicsCapture, chúng ta xem xét FallbackBitBlt — phương án dự phòng khi API chính không dùng được.*
