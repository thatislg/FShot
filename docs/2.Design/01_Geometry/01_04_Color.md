# Color — Thiết kế chi tiết

> Tài liệu này mô tả cách F-Shot biểu diễn màu sắc.  
> Color được dùng cho màu vẽ, màu overlay, màu giao diện, và mọi thao tác đồ họa khác.

---

## 1. Color là gì?

Trong F-Shot, màu là một giá trị gồm bốn thành phần: đỏ, lục, lam, và độ trong suốt. Bốn thành phần này thường được gọi là **red, green, blue, alpha**.

- **Red, green, blue** xác định màu sắc thực.
- **Alpha** xác định mức độ trong suốt. Alpha cao nghĩa là màu đậm và che phủ hoàn toàn; alpha thấp nghĩa là màu trong suốt, nhìn xuyên thấy nền phía sau.

Cách biểu diễn này phổ biến trong hầu hết các thư viện đồ họa, giúp dễ dàng chuyển đổi sang các hệ thống render như SkiaSharp mà không mất thông tin.

---

## 2. Color gồm những gì?

Mỗi kênh màu có thể được lưu dưới dạng số nguyên trong khoảng từ 0 đến 255, hoặc dạng số thập phân trong khoảng từ 0 đến 1. Mỗi cách có ưu điểm riêng.

- **Số nguyên 0–255**: phù hợp với cách người dùng thường nghĩ về màu, và phù hợp với định dạng hex như `#RRGGBBAA`.
- **Số thập phân 0–1**: phù hợp với tính toán đồ họa, đặc biệt khi cần pha trộn màu.

F-Shot cần quyết định cách lưu chính trong domain model. Quyết định này không chỉ ảnh hưởng đến cách viết code, mà còn ảnh hưởng đến cách hiển thị màu cho người dùng và cách chuyển đổi sang thư viện render.

---

## 3. Các thao tác cơ bản trên Color

### 3.1 Chuyển đổi sang chuỗi hex

Người dùng thường nhìn thấy màu dưới dạng chuỗi như `#FF5733`. Hệ thống cần có cách chuyển đổi từ Color sang chuỗi hex và ngược lại. Điều này quan trọng khi lưu màu vào file config hoặc hiển thị giá trị màu trong kính lúp.

### 3.2 Pha trộn hai màu theo alpha

Pha trộn là cách tính màu khi một màu trong suốt đè lên một màu khác. Thao tác này dùng trong Marker tool, khi nét bán trong suốt phủ lên ảnh gốc, và trong overlay tối, khi lớp mờ đè lên screenshot.

### 3.3 Làm sáng hoặc tối màu

Hệ thống có thể cần điều chỉnh độ sáng của một màu để tạo hiệu ứng hover, đổ bóng, hoặc đảm bảo độ tương phản. Ví dụ, màu của thanh công cụ có thể cần được làm sáng khi con trỏ di qua.

### 3.4 Kiểm tra độ sáng

Biết một màu sáng hay tối giúp hệ thống chọn màu chữ hoặc biểu tượng phù hợp. Ví dụ, nền tối thì nên dùng biểu tượng sáng, và ngược lại. Thao tác này dùng trong toolbar khi tự động chọn icon sáng/tối.

---

## 4. Mối quan hệ với các khái niệm khác

### 4.1 Color và Annotation

Mỗi chú thích đều có một màu. Khi người dùng chuyển sang công cụ mới, màu hiện tại được giữ lại. Màu mặc định ban đầu được lấy từ config.

### 4.2 Color và Selection

Overlay tối ngoài vùng chọn có một màu đen với alpha nhất định. Màu này có thể cấu hình được. Đường viền vùng chọn cũng có màu riêng.

### 4.3 Color và Config

`drawColor` là một trong những cấu hình quan trọng nhất. Hệ thống cần lưu màu này giữa các lần sử dụng. Ngoài ra, người dùng cũng có thể định nghĩa bảng màu tùy chỉnh trong v1.0.

### 4.4 Color và Rendering.Skia

Khi vẽ, Color trong domain model cần được chuyển thành kiểu màu của SkiaSharp. Nếu cách lưu trong domain model khác với Skia, cần có bước chuyển đổi rõ ràng.

---

## 5. Các tình huống đặc biệt cần xử lý

### 5.1 Màu trong suốt hoàn toàn

Một màu có alpha bằng không là hoàn toàn trong suốt. Hệ thống cần xử lý đúng khi vẽ: màu trong suốt không nên làm thay đổi nền.

### 5.2 Màu không hợp lệ

Khi đọc từ file config hoặc từ chuỗi hex, màu có thể không hợp lệ. Hệ thống cần quyết định cách xử lý: trả về màu mặc định, báo lỗi, hay bỏ qua thành phần lỗi.

### 5.3 So sánh màu

Cần có cách kiểm tra hai màu có gần giống nhau hay không. Điều này ít dùng trong MVP, nhưng có thể cần cho các tính năng như bảng màu tùy chỉnh.

---

## 6. Các câu hỏi cần quyết định

| Câu hỏi | Lý do cần quyết định |
|---------|----------------------|
| Lưu kênh màu dạng byte hay float? | Ảnh hưởng convert sang Skia và hiển thị hex |
| Có cần kiểu dữ liệu Color riêng hay dùng tuple? | Kiểu riêng giúp type safety, tuple đơn giản hơn |
| Alpha mặc định là bao nhiêu? | Ảnh hưởng màu vẽ ban đầu |
| Có hỗ trợ named colors như "red", "blue"? | Thuộc v1.x hoặc CLI parsing |
| Có cần gamma correction khi pha trộn? | Ảnh hưởng chất lượng màu của Marker |

---

## 7. Yêu cầu đối với kiểm thử

Các thao tác trên Color phải dễ kiểm thử. Các tình huống cần kiểm tra bao gồm:

- Chuyển đổi Color sang hex và ngược lại.
- Pha trộn màu trong suốt lên màu đậm.
- Làm sáng/tối màu giữ đúng hue.
- Kiểm tra độ sáng của màu đen, trắng, và xám.
- Xử lý alpha bằng không.

---

## 8. Kết nối với phần triển khai

Những quyết định trong tài liệu này sẽ được đưa vào:

- `src/FShot.Core/Geometry/Types.fs`: định nghĩa kiểu Color.
- `src/FShot.Core/Geometry/Operations.fs` (nếu cần): các phép toán trên Color.
- `tests/FShot.Core.Tests/Geometry/TypesTests.fs`: kiểm thử Color.

---

*Color là kiểu dữ liệu hình học thứ ba. Sau khi chốt Color, chúng ta sẽ chuyển sang StrokeWidth — cách F-Shot biểu diễn độ dày nét vẽ.*
