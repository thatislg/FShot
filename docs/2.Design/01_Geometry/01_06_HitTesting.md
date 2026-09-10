# HitTesting — Thiết kế chi tiết

> Tài liệu này mô tả cách F-Shot xác định con trỏ chuột đang tương tác với phần nào của giao diện.  
> Hit-testing là cầu nối giữa input từ người dùng và các đối tượng hình học trong domain model.

---

## 1. HitTesting là gì?

Hit-testing là quá trình trả lời câu hỏi: "Với vị trí con trỏ chuột hiện tại, nó đang chạm vào đâu?"

Trong F-Shot, câu hỏi này xuất hiện liên tục:

- Con trỏ có đang ở trên vùng chọn không?
- Con trỏ có đang ở gần một điểm neo để co giãn không?
- Con trỏ có đang ở trên một chú thích cũ để chọn sửa không?
- Con trỏ có đang ở trên một nút thanh công cụ không?

Mỗi tình huống đều cần một cách xác định khác nhau, nhưng tất cả đều dựa trên việc so sánh tọa độ con trỏ với các hình học trên màn hình.

---

## 2. Tại sao cần bán kính dung sai?

Người dùng hiếm khi click chính xác đến từng pixel. Một điểm neo chỉ rộng vài pixel, nhưng người dùng cần có thể bắt nó dễ dàng. Vì vậy, hit-test không chỉ kiểm tra "con trỏ có nằm trong điểm neo hay không", mà kiểm tra "con trỏ có nằm trong vùng lân cận của điểm neo hay không".

Vùng lân cận này được gọi là **bán kính dung sai**. Bán kính càng lớn thì càng dễ bắt, nhưng cũng dễ gây nhầm lẫn giữa các điểm gần nhau. Bán kính thường trong khoảng 4 đến 8 pixel.

---

## 3. Các loại hit-test trong F-Shot

### 3.1 Kiểm tra điểm trong hình chữ nhật

Đây là loại hit-test đơn giản nhất. Dùng để kiểm tra con trỏ có đang nằm trong vùng chọn hay không. Cũng dùng để kiểm tra con trỏ có đang nằm trong bounding box của một chú thích hay không.

### 3.2 Kiểm tra điểm gần đường thẳng

Dùng cho đường nét vẽ, mũi tên, và đường viền vùng chọn. Thay vì kiểm tra điểm nằm trong vùng, hệ thống tính khoảng cách từ con trỏ đến đường thẳng gần nhất. Nếu khoảng cách nhỏ hơn bán kính dung sai, hit-test thành công.

### 3.3 Kiểm tra điểm gần đường cong

Dùng cho bút chì tự do. Đường nét vẽ là một chuỗi các đoạn thẳng nhỏ. Hệ thống kiểm tra từng đoạn nhỏ và tìm đoạn gần con trỏ nhất.

### 3.4 Kiểm tra điểm neo

Mỗi điểm neo là một hình chữ nhật nhỏ xung quanh góc hoặc cạnh của vùng chọn. Hit-test mở rộng điểm neo ra thêm bán kính dung sai để dễ bắt.

### 3.5 Kiểm tra nút thanh công cụ

Nút thanh công cụ cũng là các hình chữ nhật hoặc hình tròn. Hit-test cho nút thường đơn giản hơn vì nút lớn và có ranh giới rõ ràng.

---

## 4. Thứ tự ưu tiên khi có nhiều đối tượng trùng nhau

Đôi khi con trỏ nằm trong vùng của nhiều đối tượng cùng lúc. Ví dụ, con trỏ có thể vừa nằm trong vùng chọn, vừa nằm gần một điểm neo. Trong trường hợp này, điểm neo nên được ưu tiên hơn vì thao tác co giãn cụ thể hơn thao tác di chuyển vùng chọn.

Thứ tự ưu tiên đề xuất:

1. Điểm neo của vùng chọn.
2. Các chú thích cũ đang được chọn/sửa.
3. Thanh công cụ.
4. Vùng chọn (để di chuyển).
5. Nền screenshot (để bắt đầu kéo vùng mới hoặc vẽ chú thích).

---

## 5. Mối quan hệ với các khái niệm khác

### 5.1 HitTesting và Point

Mọi hit-test đều bắt đầu từ một Point đại diện cho vị trí con trỏ.

### 5.2 HitTesting và Rect

Hầu hết các hit-test đơn giản đều dựa trên Rect: vùng chọn, điểm neo, nút toolbar, bounding box chú thích.

### 5.3 HitTesting và Selection

Selection sử dụng hit-test để quyết định người dùng đang muốn di chuyển vùng chọn, co giãn vùng chọn, hay bắt đầu một thao tác mới.

### 5.4 HitTesting và Annotation

Annotation sử dụng hit-test để cho phép người dùng chọn lại một chú thích cũ để di chuyển hoặc xóa. Đây là tính năng thuộc [S], không bắt buộc trong MVP.

---

## 6. Các câu hỏi cần quyết định

| Câu hỏi | Lý do cần quyết định |
|---------|----------------------|
| Bán kính dung sai mặc định là bao nhiêu pixel? | Ảnh hưởng độ dễ bắt và độ chính xác |
| Có dung sai khác nhau cho handles, lines, curves? | Handles cần dễ bắt hơn lines |
| Hit-test có tính theo logical pixels hay physical pixels? | Ảnh hưởng High-DPI |
| Có cần visual feedback khi hover? | Ảnh hưởng UX nhưng không phải logic domain |
| Thứ tự ưu tiên có cần cấu hình? | Thuộc v1.x |

---

## 7. Yêu cầu đối với kiểm thử

Hit-testing phải dễ kiểm thử bằng cách cung cấp một Point và một hình học, sau đó kiểm tra kết quả. Các tình huống cần kiểm tra:

- Điểm nằm trong và ngoài Rect.
- Điểm gần và xa đường thẳng.
- Điểm gần điểm neo với bán kính dung sai.
- Hai đối tượng trùng nhau, thứ tự ưu tiên đúng.

---

## 8. Kết nối với phần triển khai

Những quyết định trong tài liệu này sẽ được đưa vào:

- `src/FShot.Core/Geometry/Operations.fs`: các hàm hit-test.
- `src/FShot.Core/Domain/Selection.fs`: hit-test cho vùng chọn và handles.
- `src/FShot.Core/Domain/Annotation.fs`: hit-test cho chú thích (v1.x).
- `tests/FShot.Core.Tests/Geometry/TypesTests.fs`: kiểm thử hit-test.

---

*HitTesting là chủ đề cross-cutting đầu tiên trong Geometry. Sau khi chốt, chúng ta sẽ chuyển sang DpiAndScaling — cách xử lý màn hình có tỉ lệ phóng to khác nhau.*
