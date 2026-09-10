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

Công thức: cho một hình chữ nhật với cạnh trái `L`, cạnh phải `R`, cạnh trên `T`, cạnh dưới `B`, và con trỏ tại `(x, y)`. Điểm nằm trong hình chữ nhật khi cả bốn điều kiện sau đúng:

- `x >= L`
- `x <= R`
- `y >= T`
- `y <= B`

Ví dụ: hình chữ nhật có `L = 100`, `T = 80`, `R = 300`, `B = 230`.

- Con trỏ tại `(150, 150)`: `150 >= 100`, `150 <= 300`, `150 >= 80`, `150 <= 230` — cả bốn điều kiện đúng, nên điểm nằm trong.
- Con trỏ tại `(50, 150)`: `50 >= 100` sai, nên điểm nằm ngoài.
- Con trỏ tại `(300, 230)`: tất cả điều kiện đúng nếu dùng `<=`, nên điểm nằm trên biên cũng được coi là trong.

### 3.2 Kiểm tra điểm gần đường thẳng

Dùng cho đường nét vẽ, mũi tên, và đường viền vùng chọn. Thay vì kiểm tra điểm nằm trong vùng, hệ thống tính khoảng cách từ con trỏ đến đường thẳng gần nhất. Nếu khoảng cách nhỏ hơn bán kính dung sai, hit-test thành công.

Cho đường thẳng đi qua hai điểm `A = (x1, y1)` và `B = (x2, y2)`, và con trỏ tại `P = (x0, y0)`. Khoảng cách từ P đến đường thẳng vô hạn qua A, B được tính bằng công thức:

`distance = |(y2 - y1) × x0 - (x2 - x1) × y0 + x2 × y1 - y2 × x1| / sqrt((y2 - y1)^2 + (x2 - x1)^2)`

Tuy nhiên, trong thực tế, đường nét vẽ là một đoạn thẳng có hạn, không phải đường thẳng vô hạn. Nếu hình chiếu của P lên đường thẳng nằm ngoài đoạn AB, hệ thống nên tính khoảng cách đến điểm gần nhất trong hai đầu mút A hoặc B.

Ví dụ: đoạn thẳng từ `A = (100, 100)` đến `B = (400, 100)`, con trỏ tại `P = (250, 110)`.
- `(y2 - y1) = 0`, `(x2 - x1) = 300`.
- `distance = |0 × 250 - 300 × 110 + 400 × 100 - 100 × 100| / sqrt(0 + 300^2)`
- `distance = |0 - 33000 + 40000 - 10000| / 300 = |-3000| / 300 = 10`

Khoảng cách là 10 pixel. Nếu bán kính dung sai là 8, hit-test thất bại. Nếu bán kính dung sai là 12, hit-test thành công.

### 3.3 Kiểm tra điểm gần đường cong

Dùng cho bút chì tự do. Đường nét vẽ là một chuỗi các đoạn thẳng nhỏ. Hệ thống kiểm tra từng đoạn nhỏ và tìm đoạn gần con trỏ nhất.

Với mỗi đoạn nhỏ, hệ thống tính khoảng cách từ con trỏ đến đoạn thẳng bằng cách tương tự mục 3.2. Sau đó chọn khoảng cách nhỏ nhất trong tất cả các đoạn. Nếu khoảng cách nhỏ nhất nhỏ hơn bán kính dung sai, hit-test thành công.

Ví dụ: đường cong gồm hai đoạn: đoạn 1 từ `(100, 100)` đến `(200, 150)`, đoạn 2 từ `(200, 150)` đến `(300, 100)`. Con trỏ tại `(205, 155)`.

Với đoạn 1, khoảng cách đến điểm `(200, 150)` là:
- `sqrt((205 - 200)^2 + (155 - 150)^2) = sqrt(25 + 25) = sqrt(50) ≈ 7.07`

Với đoạn 2, khoảng cách đến điểm `(200, 150)` cũng tương tự, khoảng 7.07.

Nếu bán kính dung sai là 8, hit-test thành công vì khoảng cách nhỏ nhất nhỏ hơn 8.

### 3.4 Kiểm tra điểm neo

Mỗi điểm neo là một hình chữ nhật nhỏ xung quanh góc hoặc cạnh của vùng chọn. Hit-test mở rộng điểm neo ra thêm bán kính dung sai để dễ bắt.

Công thức đơn giản: điểm neo được mở rộng đều ra `tolerance` pixel theo bốn hướng. Nếu con trỏ nằm trong hình chữ nhật mở rộng, hit-test thành công.

Cho điểm neo gốc có `L = 200`, `T = 200`, `R = 210`, `B = 210`, và bán kính dung sai `tolerance = 6`.

Hình chữ nhật mở rộng:
- `L' = L - 6 = 194`
- `T' = T - 6 = 194`
- `R' = R + 6 = 216`
- `B' = B + 6 = 216`

Con trỏ tại `(215, 215)` nằm trong hình chữ nhật mở rộng vì `215 >= 194`, `215 <= 216`, `215 >= 194`, `215 <= 216`. Hit-test thành công. Nếu không mở rộng, con trỏ này nằm ngoài điểm neo gốc.

### 3.5 Kiểm tra nút thanh công cụ

Nút thanh công cụ cũng là các hình chữ nhật hoặc hình tròn. Hit-test cho nút thường đơn giản hơn vì nút lớn và có ranh giới rõ ràng.

Với nút hình chữ nhật, công thức giống mục 3.1. Với nút hình tròn có tâm `(cx, cy)` và bán kính `r`, con trỏ `(x, y)` nằm trong nút nếu:

`sqrt((x - cx)^2 + (y - cy)^2) <= r`

Ví dụ: nút tròn có tâm `(500, 100)`, bán kính `20`. Con trỏ tại `(515, 115)`.
- `sqrt((515 - 500)^2 + (115 - 100)^2) = sqrt(225 + 225) = sqrt(450) ≈ 21.21`
- 21.21 > 20, nên con trỏ nằm ngoài nút.

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
