# DpiAndScaling — Thiết kế chi tiết

> Tài liệu này mô tả cách F-Shot xử lý nhiều màn hình có tỉ lệ phóng to khác nhau.  
> Mixed DPI là một trong những thách thức lớn nhất khi chụp màn hình trên Windows.

---

## 1. DpiAndScaling là gì?

Màn hình hiện đại không chỉ khác nhau về độ phân giải, mà còn khác nhau về mật độ điểm ảnh. Một số màn hình hiển thị 96 điểm ảnh trên một inch, trong khi màn hình khác có thể hiển thị 144 hoặc 192 điểm ảnh trên một inch.

Để văn bản và giao diện không bị quá nhỏ trên màn hình High-DPI, Windows sử dụng tỉ lệ phóng to. Ví dụ, một màn hình có tỉ lệ 150% nghĩa là mỗi logical pixel trong giao diện ứng dụng tương đương với 1.5 physical pixel trên màn hình.

Khi một ứng dụng chụp màn hình, nó phải hiểu rõ sự khác biệt này. Nếu không, ảnh chụp có thể bị mờ, lệch tọa độ, hoặc chỉ chụp được một phần màn hình.

---

## 2. Các khái niệm cần phân biệt

### 2.1 Logical pixel

Logical pixel là đơn vị mà ứng dụng và người dùng thường làm việc. Khi bạn nói "cửa sổ rộng 800 pixel", đó thường là logical pixel. Logical pixel được Windows phóng to theo tỉ lệ DPI.

### 2.2 Physical pixel

Physical pixel là điểm ảnh thực sự trên màn hình. Một màn hình 4K vật lý có thể hiển thị nhiều physical pixel hơn một màn hình 1080p, ngay cả khi cả hai đều được đặt tỉ lệ 100%.

### 2.3 DPI scale factor

DPI scale factor là tỉ lệ giữa logical pixel và physical pixel. Ví dụ, tỉ lệ 1.5 nghĩa là 1 logical pixel = 1.5 physical pixel. Trong thực tế, tỉ lệ thường là 1.0, 1.25, 1.5, 1.75, hoặc 2.0.

### 2.4 Virtual Screen

Virtual Screen là hệ tọa độ bao phủ tất cả các màn hình. Nếu bạn có hai màn hình 1920x1080 đặt cạnh nhau, Virtual Screen có thể rộng 3840 pixel. Nhưng với Mixed DPI, mỗi màn hình có thể đóng góp một số logical pixel khác nhau.

---

## 3. Vấn đề cần giải quyết

### 3.1 Ảnh chụp phải đúng kích thước

Khi chụp một màn hình, ảnh thu được phải có đủ số physical pixel để hiển thị sắc nét. Nếu chụp theo logical size, ảnh sẽ bị mờ khi phóng to.

### 3.2 Tọa độ con trỏ phải khớp với ảnh chụp

Nếu con trỏ chuột được báo cáo theo logical pixel, nhưng ảnh chụp là physical pixel, hệ thống phải chuyển đổi để vùng chọn vẽ đúng chỗ. Nếu không, vùng chọn sẽ bị lệch hoặc sai kích thước.

### 3.3 Di chuyển giữa các màn hình

Khi con trỏ di chuyển từ màn hình 100% sang màn hình 150%, Virtual Screen space không đổi, nhưng số physical pixel tương ứng thay đổi. Hệ thống cần biết đang ở màn hình nào để chuyển đổi đúng.

---

## 4. Cách tiếp cận đề xuất

### 4.1 Làm việc trong Virtual Screen space

Tất cả tọa độ trong domain model, bao gồm Point và Rect, đều dùng Virtual Screen space ở mức logical pixel. Điều này giúp code domain đơn giản và không phụ thuộc DPI.

### 4.2 Capture trả về physical bitmap

Khi chụp màn hình, backend capture trả về một bitmap với kích thước physical pixel, cùng với thông tin scale factor và Virtual Bounds. Bitmap này có thể lớn hơn logical bounds, nhưng tỉ lệ được ghi rõ.

### 4.3 Chuyển đổi khi render

Khi render bitmap lên màn hình, UI biết scale factor và có thể vẽ đúng kích thước. Khi vẽ các hình học như vùng chọn, hệ thống vẽ trong logical space rồi để render engine phóng to theo scale factor.

### 4.4 Crop ảnh cuối theo logical Rect

Khi xuất ảnh, vùng crop được tính trong logical space. Sau đó, vùng này được nhân với scale factor để cắt đúng số physical pixel từ bitmap gốc.

---

## 5. Mối quan hệ với các khái niệm khác

### 5.1 DpiAndScaling và Point

Point trong domain model luôn là logical pixel. Chỉ khi cần tương tác với bitmap physical hoặc với Win32 API mới cần chuyển đổi.

### 5.2 DpiAndScaling và Rect

Rect của vùng chọn cũng là logical. Khi crop, Rect được nhân với scale factor để có kích thước physical.

### 5.3 DpiAndScaling và Capture

Capture backend phải báo cáo đúng scale factor cho từng màn hình. Đây là trách nhiệm của `FShot.Platform.Win32`.

### 5.4 DpiAndScaling và Rendering.Skia

Rendering.Skia nhận logical geometry và bitmap physical. Nó phải biết cách kết hợp hai thứ để vẽ đúng.

---

## 6. Các câu hỏi cần quyết định

| Câu hỏi | Lý do cần quyết định |
|---------|----------------------|
| Domain model dùng logical hay physical pixel? | Logical giúp code đơn giản, physical tránh lỗi làm tròn |
| Có lưu scale factor trong Point/Rect không? | Có thể làm rõ nhưng tăng độ phức tạp |
| Cách xử lý màn hình có tỉ lệ không nguyên như 125%? | Cần làm tròn cẩn thận khi crop |
| Có hỗ trợ màn hình được xoay orientation? | Ảnh hưởng Virtual Bounds |
| Có cần scale StrokeWidth theo DPI? | Ảnh hưởng cảm nhận nét vẽ trên màn hình High-DPI |

---

## 7. Yêu cầu đối với kiểm thử

Mixed DPI khó kiểm thử tự động, nhưng có thể viết test cho các phép chuyển đổi:

- Chuyển đổi logical Rect sang physical Rect với scale factor 1.5.
- Chuyển đổi physical point sang logical point.
- Đảm bảo crop Rect không bị lệch 1 pixel khi làm tròn.

Ngoài ra, cần kiểm thử thủ công trên cấu hình Multi-monitor Mixed DPI thực tế.

---

## 8. Kết nối với phần triển khai

Những quyết định trong tài liệu này sẽ được đưa vào:

- `src/FShot.Core/Geometry/Types.fs`: có thể thêm kiểu ScaleFactor.
- `src/FShot.Core/Domain/Capture.fs`: CaptureResult chứa scale factor.
- `src/FShot.Platform.Win32/Capture/GraphicsCapture.fs`: lấy đúng DPI từng màn hình.
- `src/FShot.Rendering.Skia/Converters/DomainToSkia.fs`: chuyển đổi logical → physical.

---

*DpiAndScaling là chủ đề cross-cutting cuối cùng trong Geometry. Sau khi chốt, chúng ta sẽ chuyển sang phần Capture — cách chụp màn hình.*
