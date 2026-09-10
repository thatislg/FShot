# CaptureResult — Thiết kế chi tiết

> Tài liệu này mô tả kết quả trả về sau khi chụp màn hình.
> CaptureResult là abstraction giữa backend chụp và phần còn lại của hệ thống.

---

## 1. CaptureResult là gì?

`CaptureResult` là dữ liệu mà backend capture trả về sau khi chụp xong. Nó chứa:

- Ảnh bitmap dưới dạng mảng byte.
- Thông tin kích thước và định dạng pixel.
- Vị trí và kích thước trong Virtual Screen space.
- Hệ số phóng to DPI của màn hình đã chụp.
- Thông tin về lỗi nếu chụp thất bại.

CaptureResult giúp các phần khác của hệ thống không cần biết backend chụp là gì. Dù dùng `Windows.Graphics.Capture`, `BitBlt`, hay một backend khác, kết quả cuối cùng đều có cùng hình dạng.

---

## 2. Các thành phần của CaptureResult

### 2.1 Pixels

`Pixels` là mảng byte chứa dữ liệu pixel thô. Thứ tự byte phụ thuộc vào `PixelFormat`.

Ví dụ: với định dạng BGRA32, mỗi pixel chiếm 4 byte theo thứ tự: xanh lam, lục, đỏ, alpha. Một dòng có `Width` pixel sẽ chiếm `Width * 4` byte.

### 2.2 Width và Height

`Width` và `Height` là kích thước ảnh tính bằng physical pixel. Đây là số pixel thực tế trong mảng byte.

Ví dụ: một màn hình 1920×1080 logical với scale factor 1.5 sẽ cho ra ảnh 2880×1620 physical pixel.

### 2.3 Stride

`Stride` là số byte mỗi dòng. Thường bằng `Width * bytesPerPixel`, nhưng có thể lớn hơn nếu dữ liệu được căn chỉnh theo biên nhớ.

Ví dụ: với ảnh 2880 pixel rộng, định dạng BGRA32 (4 byte/pixel), stride tối thiểu là `2880 * 4 = 11520` byte.

### 2.4 PixelFormat

`PixelFormat` cho biết cách diễn giải mảng byte. Các giá trị phổ biến:

- `Bgra32`: 4 byte mỗi pixel, thứ tự B-G-R-A.
- `Rgba32`: 4 byte mỗi pixel, thứ tự R-G-B-A.
- `Rgb24`: 3 byte mỗi pixel, không có alpha.

F-Shot nên chọn một định dạng mặc định duy nhất để đơn giản hóa việc chuyển sang SkiaSharp.

### 2.5 VirtualBounds

`VirtualBounds` là một `Rect` trong Virtual Screen space, tính bằng logical pixel. Nó cho biết ảnh đã chụp nằm ở đâu trên desktop ảo.

Ví dụ: chụp màn hình chính có Virtual Bounds `(0, 0, 1920, 1080)`. Chụp màn hình thứ hai bên phải có thể là `(1920, 0, 2560, 1080)`.

### 2.6 ScaleFactor

`ScaleFactor` là hệ số phóng to DPI của màn hình đã chụp. Dùng để chuyển đổi giữa logical pixel (tọa độ con trỏ, vùng chọn) và physical pixel (bitmap).

Ví dụ: màn hình 150% có `ScaleFactor = 1.5`.

### 2.7 ScreenIndex

`ScreenIndex` là chỉ số màn hình đã chụp trong danh sách màn hình. Hữu ích khi chụp một màn hình cụ thể hoặc khi cần hiển thị overlay đúng màn hình.

### 2.8 Error

`Error` là thông tin lỗi nếu chụp thất bại, ví dụ bị từ chối quyền, thiết bị không hỗ trợ, hoặc màn hình bị ngắt kết nối.

---

## 3. Công thức tính toán kích thước mảng byte

Tổng số byte trong mảng `Pixels` phải đủ để chứa toàn bộ ảnh:

`totalBytes = Stride * Height`

Ví dụ: ảnh có `Width = 2880`, `Height = 1620`, `Stride = 11520`, `PixelFormat = Bgra32`.

- `totalBytes = 11520 * 1620 = 18,662,400` byte.
- Số pixel thực tế = `2880 * 1620 = 4,665,600` pixel.
- Mỗi pixel 4 byte, nên tổng byte lý thuyết = `4,665,600 * 4 = 18,662,400` byte.

Hai giá trị khớp nhau, chứng tỏ stride đúng.

---

## 4. Công thức chuyển đổi logical → physical

Khi UI nhận được vùng chọn ở logical pixel, cần chuyển sang physical pixel để crop từ bitmap:

`physicalX = round(virtualX * scaleFactor)`
`physicalY = round(virtualY * scaleFactor)`
`physicalWidth = round(virtualWidth * scaleFactor)`
`physicalHeight = round(virtualHeight * scaleFactor)`

Trong đó `(virtualX, virtualY, virtualWidth, virtualHeight)` là vùng chọn trong Virtual Screen space.

Ví dụ: vùng chọn logical `(100, 80, 300, 200)`, scale factor `1.5`:

- `physicalX = round(100 * 1.5) = 150`
- `physicalY = round(80 * 1.5) = 120`
- `physicalWidth = round(300 * 1.5) = 450`
- `physicalHeight = round(200 * 1.5) = 300`

Vùng crop physical là `(150, 120, 450, 300)`.

---

## 5. Mối quan hệ với các phần khác

### 5.1 CaptureResult và Rendering.Skia

`Rendering.Skia` nhận `CaptureResult`, chuyển mảng byte thành `SKBitmap` và vẽ lên canvas. Nó cần biết `PixelFormat` để chọn đúng cách tạo bitmap.

### 5.2 CaptureResult và Selection

Vùng chọn trong overlay được tính trong Virtual Screen space. Khi cần crop, `Selection` dùng `VirtualBounds` và `ScaleFactor` của `CaptureResult` để tính vùng physical.

### 5.3 CaptureResult và Export

Export dùng `Pixels`, `Width`, `Height`, `Stride`, `PixelFormat`, và vùng crop physical để cắt ảnh và lưu ra file hoặc clipboard.

---

## 6. Các câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| PixelFormat mặc định là gì? | BGRA32 phổ biến trên Windows, RGBA32 dễ chuyển sang Skia |
| Có lưu ảnh dưới dạng byte[] hay dùng Stream? | byte[] đơn giản cho PoC, Stream tốt hơn cho ảnh lớn |
| CaptureResult có nên là Result/Success/Failure rõ ràng? | Giúp xử lý lỗi tường minh |
| Có cần lưu cursor chuột trong ảnh không? | Tùy chọn nâng cao, có thể để v1.x |

---

## 7. Kết nối với phần triển khai

Những quyết định trong tài liệu này sẽ được đưa vào:

- `src/FShot.Core/Domain/Capture.fs`: kiểu `CaptureResult`.
- `src/FShot.Platform.Win32/Capture/BitmapAdapter.fs`: tạo `CaptureResult` từ Direct3D surface.
- `src/FShot.Rendering.Skia/Converters/DomainToSkia.fs`: chuyển `CaptureResult` sang `SKBitmap`.

---

*Sau khi chốt CaptureResult, chúng ta chuyển sang WindowsGraphicsCapture — backend chụp chính.*
