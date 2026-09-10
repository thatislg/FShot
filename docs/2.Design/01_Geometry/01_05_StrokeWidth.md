# StrokeWidth — Thiết kế chi tiết

> Tài liệu này mô tả cách F-Shot biểu diễn độ dày nét vẽ.  
> StrokeWidth là thuộc tính chung cho hầu hết các công cụ vẽ, từ bút chì đến mũi tên.

---

## 1. StrokeWidth là gì?

Độ dày nét vẽ là giá trị cho biết đường nét sẽ rộng bao nhiêu pixel khi hiển thị. Một nét mảnh tạo cảm giác tinh tế, một nét dày tạo cảm giác mạnh mẽ và dễ nhìn.

Trong F-Shot, độ dày nét là một thuộc tính của mỗi chú thích. Khi người dùng chuyển sang công cụ khác, độ dày nét có thể được giữ nguyên theo giá trị mặc định hoặc theo giá trị người dùng đã điều chỉnh lần trước.

---

## 2. StrokeWidth gồm những gì?

Về bản chất, độ dày nét chỉ là một con số dương. Tuy nhiên, hệ thống có thể chọn cách biểu diễn khác nhau để phản ánh ý nghĩa của nó.

- **Biểu diễn đơn giản**: một số thực dương, ví dụ `3.0`.
- **Biểu diễn bằng kiểu dữ liệu đặc biệt**: một kiểu dữ liệu chỉ dùng cho độ dày nét, giúp tránh nhầm lẫn với các con số khác trong hệ thống.

Cách thứ hai giúp code rõ ràng hơn: nếu một hàm nhận độ dày nét, người đọc code ngay lập tức biết đó là gì. Tuy nhiên, cách thứ hai cũng đòi hỏi thêm một chút boilerplate khi tạo giá trị.

---

## 3. Các ràng buộc trên StrokeWidth

### 3.1 Giá trị dương

Độ dày nét không thể là số âm hoặc bằng không. Nếu người dùng cố gắng giảm xuống dưới giá trị tối thiểu, hệ thống cần giữ nó ở mức tối thiểu.

### 3.2 Giá trị tối đa

Để tránh nét vẽ quá to chiếm hết màn hình, hệ thống có thể đặt một giá trị tối đa. Giá trị tối đa này có thể cấu hình hoặc cố định.

### 3.3 Thay đổi theo bước

Khi người dùng lăn chuột hoặc bấm phím tắt để tăng/giảm độ dày nét, giá trị nên thay đổi theo từng bước rõ ràng, ví dụ 1, 2, 3, 5, 8, 12, thay vì tăng liên tục từng 0.1. Cách này giúp người dùng dễ dàng chọn độ dày phổ biến.

---

## 4. Cách người dùng điều chỉnh StrokeWidth

### 4.1 Bằng lăn chuột

Khi một công cụ vẽ đang được chọn, lăn chuột lên hoặc xuống sẽ tăng hoặc giảm độ dày nét. Đây là cách nhanh nhất.

### 4.2 Bằng bàn phím

Người dùng có thể gõ một con số để đặt độ dày chính xác. Ví dụ, gõ "5" rồi nhấn Enter để đặt độ dày là 5 pixel.

### 4.3 Từ cấu hình

Độ dày nét mặc định được lưu trong file config. Khi khởi động, hệ thống đọc giá trị này và áp dụng cho công cụ đầu tiên.

---

## 5. Mối quan hệ với các khái niệm khác

### 5.1 StrokeWidth và Annotation

Mỗi chú thích lưu độ dày nét riêng. Khi vẽ lại danh sách chú thích, mỗi nét vẽ sử dụng độ dày đã lưu. Điều này đảm bảo một đường vẽ trước đó không bị thay đổi khi người dùng đổi độ dày cho nét vẽ tiếp theo.

### 5.2 StrokeWidth và High-DPI

Trên màn hình High-DPI, một nét có độ dày 3 pixel theo logical size có thể hiển thị rất mảnh nếu không được nhân với hệ số phóng to. Hệ thống cần quyết định liệu độ dày nét là đơn vị logical hay physical. Điều này liên quan đến phần DpiAndScaling.

### 5.3 StrokeWidth và Config

`drawThickness` là một cấu hình quan trọng. Nó được lưu khi người dùng thay đổi, và được khôi phục khi mở lại ứng dụng.

---

## 6. Các câu hỏi cần quyết định

| Câu hỏi | Lý do cần quyết định |
|---------|----------------------|
| Có nên dùng kiểu dữ liệu riêng cho StrokeWidth? | Ảnh hưởng type safety và boilerplate |
| Độ dày tối thiểu và tối đa là bao nhiêu? | Ảnh hưởng UX và giới hạn hợp lý |
| Các bước điều chỉnh độ dày là gì? | Ảnh hưởng cảm giác khi lăn chuột |
| Độ dày là logical pixel hay physical pixel? | Ảnh hưởng hiển thị trên High-DPI |
| Có độ dày riêng cho từng loại công cụ? | Flameshot gốc có một số giá trị mặc định khác nhau |

---

## 7. Yêu cầu đối với kiểm thử

Các quy tắc trên StrokeWidth phải dễ kiểm thử. Các tình huống cần kiểm tra bao gồm:

- Giá trị dưới mức tối thiểu bị kéo lên.
- Giá trị trên mức tối đa bị kéo xuống.
- Tăng/giảm theo bước cho đúng giá trị tiếp theo.
- Lưu và khôi phục giá trị từ config.

---

## 8. Kết nối với phần triển khai

Những quyết định trong tài liệu này sẽ được đưa vào:

- `src/FShot.Core/Geometry/Types.fs`: định nghĩa kiểu StrokeWidth.
- `src/FShot.Core/Geometry/Operations.fs` (nếu cần): validation và thay đổi bước.
- `tests/FShot.Core.Tests/Geometry/TypesTests.fs`: kiểm thử StrokeWidth.

---

*StrokeWidth là kiểu dữ liệu hình học thứ tư. Sau khi chốt StrokeWidth, chúng ta sẽ chuyển sang HitTesting — cách xác định con trỏ đang ở đâu.*
