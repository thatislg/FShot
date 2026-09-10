# ScreenEnumeration — Thiết kế chi tiết

> Tài liệu này mô tả cách F-Shot liệt kê các màn hình và lấy thông tin DPI của chúng.
> Đây là nền tảng để chụp đúng màn hình và xử lý Mixed DPI.

---

## 1. ScreenEnumeration là gì?

`ScreenEnumeration` là quá trình hỏi Windows về tất cả các màn hình đang kết nối. Mỗi màn hình được mô tả bởi:

- Tên hoặc mô tả.
- Vị trí trong Virtual Screen space (`left`, `top`, `right`, `bottom`).
- Kích thước logical và physical.
- Hệ số phóng to DPI (`scale factor`).
- Màn hình chính hay phụ.

Thông tin này dùng để:

- Chọn màn hình cần chụp trong chế độ `SingleScreen`.
- Xác định Virtual Screen bounds cho overlay.
- Chuyển đổi logical ↔ physical pixels.

---

## 2. Cách lấy thông tin màn hình trên Windows

### 2.1 EnumDisplayMonitors

`EnumDisplayMonitors` là hàm Win32 để liệt kê các màn hình. Mỗi màn hình được gọi là một `HMONITOR`. Hàm này trả về callback cho mỗi màn hình.

Thông tin từ callback:

- `left`, `top`, `right`, `bottom` trong Virtual Screen space (logical).
- Cờ `MONITORINFOF_PRIMARY` để biết màn hình chính.

### 2.2 GetDpiForMonitor

`GetDpiForMonitor` (từ Shcore.dll) lấy DPI theo chiều ngang và dọc của một màn hình. Từ DPI tính scale factor:

`scaleFactor = dpi / 96`

Ví dụ: DPI = 144 thì `scaleFactor = 144 / 96 = 1.5`.

### 2.3 GetScaleFactorForMonitor

`GetScaleFactorForMonitor` cũng cho biết scale factor trực tiếp, nhưng chỉ trả về một số giá trị rời rạc (100, 125, 150, ...). Có thể dùng để kiểm tra chéo với `GetDpiForMonitor`.

---

## 3. Cấu trúc dữ liệu màn hình

Mỗi màn hình trong F-Shot được biểu diễn bằng một kiểu dữ liệu gồm:

- `Index`: chỉ số trong danh sách.
- `Name`: tên hiển thị, ví dụ "\\.\\DISPLAY1".
- `IsPrimary`: có phải màn hình chính không.
- `VirtualBounds`: Rect logical trong Virtual Screen.
- `PhysicalSize`: kích thước physical pixel.
- `ScaleFactor`: hệ số phóng to.

Từ đó suy ra:

`physicalWidth = round((right - left) * scaleFactor)`
`physicalHeight = round((bottom - top) * scaleFactor)`

Ví dụ: màn hình có Virtual Bounds `(1920, 0, 4480, 1440)` và scale factor `1.5`:

- logical width = 4480 - 1920 = 2560
- logical height = 1440 - 0 = 1440
- physical width = round(2560 * 1.5) = 3840
- physical height = round(1440 * 1.5) = 2160

---

## 4. Tính Virtual Screen bounds

Virtual Screen bao phủ tất cả các màn hình. Công thức:

`virtualLeft = min(left của tất cả màn hình)`
`virtualTop = min(top của tất cả màn hình)`
`virtualRight = max(right của tất cả màn hình)`
`virtualBottom = max(bottom của tất cả màn hình)`

`virtualWidth = virtualRight - virtualLeft`
`virtualHeight = virtualBottom - virtualTop`

Ví dụ: có hai màn hình.

- Màn hình 1: `(0, 0, 1920, 1080)`, scale 1.0.
- Màn hình 2: `(1920, 0, 4480, 1440)`, scale 1.5.

Tính Virtual Screen:

- `virtualLeft = 0`
- `virtualTop = 0`
- `virtualRight = 4480`
- `virtualBottom = 1440`

Virtual Screen bounds là `(0, 0, 4480, 1440)`.

---

## 5. Tìm màn hình chứa một điểm

Để biết con trỏ chuột đang ở màn hình nào, kiểm tra điểm có nằm trong Virtual Bounds của màn hình nào.

Công thức: điểm `(x, y)` thuộc màn hình có bounds `(left, top, right, bottom)` khi:

`x >= left && x < right`
`y >= top && y < bottom`

Ví dụ: điểm `(2000, 500)`.

- Màn hình 1: `2000 >= 0` nhưng `2000 < 1920` sai, nên không thuộc.
- Màn hình 2: `2000 >= 1920` và `2000 < 4480` đúng; `500 >= 0` và `500 < 1440` đúng, nên thuộc màn hình 2.

---

## 6. Các câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Lưu danh sách màn hình ở đâu và cập nhật khi nào? | Có thể lưu trong Platform.Win32, cập nhật khi display settings thay đổi |
| Có dùng `EnumDisplayMonitors` hay Managed API như `System.Windows.Forms.Screen`? | Managed API đơn giản hơn nhưng hạn chế về DPI |
| Scale factor khác nhau theo chiều ngang/dọc có xử lý không? | Hiếm nhưng có thể xảy ra |
| Có xử lý màn hình xoay orientation (portrait/landscape)? | Ảnh hưởng Virtual Bounds |

---

## 7. Kết nối với phần triển khai

Những quyết định trong tài liệu này sẽ được đưa vào:

- `src/FShot.Platform.Win32/Screen/ScreenInfo.fs` hoặc `.cs`: kiểu dữ liệu màn hình.
- `src/FShot.Platform.Win32/Screen/ScreenEnumeration.fs` hoặc `.cs`: liệt kê màn hình.
- `src/FShot.Core/Domain/Capture.fs`: sử dụng thông tin màn hình trong CaptureRequest/CaptureResult.

---

*ScreenEnumeration là file cuối cùng trong phần Capture. Sau khi chốt, chúng ta chuyển sang triển khai code Capture trong Core và Platform.Win32.*
