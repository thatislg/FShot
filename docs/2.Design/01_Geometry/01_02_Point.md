# Point — Thiết kế chi tiết

> Tài liệu này mô tả cách F-Shot biểu diễn một vị trí trong không gian màn hình.  
> Point là khối xây dựng nhỏ nhất của mọi thao tác hình học trong hệ thống.

---

## 1. Point là gì?

Trong F-Shot, một điểm không phải là một pixel cụ thể trên màn hình vật lý. Thay vào đó, nó là một vị trí được đo trong không gian ảo của toàn bộ desktop, gọi là **Virtual Screen space**.

Virtual Screen space là hệ tọa độ mà Windows dùng để đặt tất cả các màn hình lên cùng một mặt phẳng lớn. Nếu bạn có hai màn hình, màn hình bên trái có thể bắt đầu tại tọa độ `(0, 0)`, còn màn hình bên phải bắt đầu tại `(1920, 0)`. Một điểm trong F-Shot sẽ được xác định dựa trên mặt phẳng chung này, chứ không phải dựa trên từng màn hình riêng lẻ.

Việc dùng Virtual Screen space giúp F-Shot không bị lúng túng khi di chuyển chuột giữa các màn hình có độ phân giải khác nhau. Con trỏ chuột lúc nào cũng có một vị trí duy nhất trong không gian lớn này.

---

## 2. Point gồm những gì?

Một điểm chỉ đơn giản gồm hai giá trị: tọa độ theo chiều ngang và chiều dọc. Trong tài liệu này, hai giá trị đó được gọi là **X** và **Y**.

### 2.1 X — tọa độ ngang

**X** cho biết điểm nằm cách mép trái của Virtual Screen bao xa.

- Khi X bằng 0, điểm nằm ngay tại mép trái của Virtual Screen.
- Khi X tăng, điểm dịch chuyển sang phải.
- Khi X âm, điểm nằm bên trái mép trái của Virtual Screen. Điều này có thể xảy ra nếu một màn hình trong hệ thống được đặt ở vị trí âm so với màn hình chính, hoặc khi con trỏ di chuyển ra ngoài biên trong lúc kéo vùng chọn.

Ví dụ: nếu Virtual Screen bắt đầu tại `(0, 0)` và màn hình đầu tiên rộng 1920 logical pixel, thì một điểm có X bằng 960 nằm chính giữa theo chiều ngang của màn hình đầu tiên.

### 2.2 Y — tọa độ dọc

**Y** cho biết điểm nằm cách mép trên của Virtual Screen bao xa.

- Khi Y bằng 0, điểm nằm ngay tại mép trên của Virtual Screen.
- Khi Y tăng, điểm dịch chuyển xuống dưới.
- Khi Y âm, điểm nằm bên trên mép trên của Virtual Screen. Điều này cũng có thể xảy ra khi màn hình được đặt ở vị trí âm hoặc khi người dùng kéo vùng chọn ra ngoài biên.

Ví dụ: một điểm có Y bằng 540 trên màn hình cao 1080 logical pixel nằm chính giữa theo chiều dọc.

### 2.3 Cả hai giá trị đều là số thập phân

Cả X và Y đều có thể là số thập phân. Lý do là vì màn hình hiện đại thường có tỉ lệ phóng to khác nhau: màn hình này có thể hiển thị 100% kích thước thật, màn hình khác lại phóng to 150%. Nếu chỉ dùng số nguyên, các tính toán khi chuyển đổi giữa các tỉ lệ này sẽ bị lệch dần. Dùng số thập phân giúp giữ độ chính xác cho đến khi cần làm tròn ra pixel cuối cùng.

Ví dụ: trên màn hình có tỉ lệ 150%, một logical pixel tương đương với 1.5 physical pixel. Nếu con trỏ nằm tại logical position 100.5, điểm đó vẫn có ý nghĩa rõ ràng trong hệ tọa độ logical, và sẽ được chuyển thành physical pixel khi cần vẽ hoặc crop.

---

## 3. Point được dùng ở đâu?

Point xuất hiện ở khắp nơi trong F-Shot:

- Khi người dùng di chuyển chuột, vị trí con trỏ được ghi lại dưới dạng một Point.
- Khi người dùng bắt đầu kéo vùng chọn, điểm bắt đầu và điểm kết thúc của thao tác kéo đều là Point.
- Khi vẽ một đường thẳng hoặc mũi tên, hai đầu mút của đường vẽ là hai Point.
- Khi vẽ bút tự do, mỗi điểm nhỏ trên đường nét vẽ cũng là một Point.
- Khi đặt văn bản lên ảnh, góc trên bên trái của hộp văn bản là một Point.
- Khi hiển thị kính lúp, vị trí trung tâm của kính lúp cũng là một Point.

Nói tóm lại, hầu hết mọi thao tác tương tác trong F-Shot đều bắt đầu từ Point.

---

## 4. Các phép toán cơ bản trên Point

Dù Point chỉ là một cặp số, hệ thống cần hỗ trợ nhiều thao tác trên nó để phục vụ các tính năng khác nhau.

### 4.1 Cộng và trừ hai điểm

Cộng hai điểm có nghĩa là cộng từng thành phần tương ứng. Kết quả là một điểm mới. Phép trừ cũng tương tự. Phép trừ đặc biệt hữu ích khi cần biết khoảng cách hoặc hướng di chuyển giữa hai vị trí, ví dụ như khi tính vector dịch chuyển của một thao tác kéo.

Ví dụ: giả sử điểm A có tọa độ `(X = 100, Y = 200)` và điểm B có tọa độ `(X = 150, Y = 220)`.

- Tổng của A và B là một điểm mới tại `(X = 250, Y = 420)`.
- Hiệu của B trừ A là `(X = 50, Y = 20)`. Kết quả này cho biết để đi từ A đến B, cần dịch chuyển 50 đơn vị sang phải và 20 đơn vị xuống dưới. Trong thực tế, phép trừ thường được dùng để tính vector dịch chuyển, sau đó cộng vector này vào một điểm khác để di chuyển theo cùng hướng.

### 4.2 Nhân và chia với một hệ số

Nhân một điểm với một số có nghĩa là nhân cả X và Y với số đó. Phép này dùng khi cần co giãn tọa độ theo một tỉ lệ, chẳng hạn khi chuyển đổi giữa logical pixels và physical pixels trong môi trường High-DPI.

Ví dụ: một điểm có tọa độ `(X = 100, Y = 200)` được nhân với hệ số 1.5 sẽ thành `(X = 150, Y = 300)`. Điều này có nghĩa là nếu logical position 100 tương đương với 150 physical pixel, thì điểm đó trong physical space nằm tại 150.

Chia một điểm cho một hệ số cũng tương tự. Ví dụ, điểm `(X = 150, Y = 300)` chia cho 1.5 sẽ trở lại `(X = 100, Y = 200)`. Phép chia này dùng khi chuyển từ physical pixels sang logical pixels.

### 4.3 Khoảng cách giữa hai điểm

Khoảng cách giữa hai điểm là độ dài của đoạn thẳng nối chúng. Phép này dùng trong hit-testing: để xác định con trỏ chuột có đang ở gần một đường nét vẽ hay không, hệ thống sẽ tính khoảng cách từ con trỏ đến đường nét đó.

Ví dụ: giả sử điểm A tại `(X = 100, Y = 100)` và điểm B tại `(X = 400, Y = 100)`. Hai điểm này nằm ngang nhau, cách nhau 300 đơn vị theo trục X. Khoảng cách giữa chúng là 300.

Nếu điểm A tại `(X = 100, Y = 100)` và điểm B tại `(X = 400, Y = 400)`, thì khoảng cách không chỉ là 300 theo X hay 300 theo Y. Lúc này cần tính độ dài của đường chéo, bằng cách lấy căn bậc hai của tổng bình phương hai khoảng cách theo X và Y. Kết quả là khoảng 424.3 đơn vị.

### 4.4 Nội suy giữa hai điểm

Nội suy là cách tìm một điểm nằm giữa hai điểm cho trước, theo một tỉ lệ nhất định. Ví dụ, nội suy với tỉ lệ 0.5 sẽ cho ra điểm chính giữa. Phép này ít dùng trong MVP, nhưng có thể hữu ích sau này cho các hiệu ứng hoạt hình hoặc làm mượt đường nét vẽ.

Ví dụ: điểm A tại `(X = 100, Y = 100)` và điểm B tại `(X = 300, Y = 200)`. Điểm nội suy ở giữa, với tỉ lệ 0.5, sẽ là `(X = 200, Y = 150)`. Với tỉ lệ 0.25, điểm nằm gần A hơn, tại `(X = 150, Y = 125)`.

### 4.5 So sánh hai điểm

Cần có cách kiểm tra hai điểm có trùng nhau hay không, có thể với một ngưỡng sai số nhỏ. Điều này quan trọng khi so sánh tọa độ chuột với các điểm neo của vùng chọn, vì con trỏ hiếm khi click chính xác đến từng pixel.

Ví dụ: điểm A tại `(X = 100.0, Y = 200.0)` và điểm B tại `(X = 100.1, Y = 200.1)`. Nếu ngưỡng sai số là 1 đơn vị, hai điểm được coi là trùng nhau vì cả hai khoảng cách theo X và Y đều nhỏ hơn 1. Nếu ngưỡng sai số là 0.05, hai điểm không được coi là trùng nhau.

---

## 5. Mối quan hệ với các khái niệm khác

### 5.1 Point và Rect

Một hình chữ nhật được xác định bởi vị trí góc trên bên trái và kích thước. Vị trí góc đó chính là một Point. Ngoài ra, bốn góc của hình chữ nhật cũng là các Point. Nhờ đó, các phép toán trên Point có thể tái sử dụng để xử lý hình chữ nhật.

Ví dụ: nếu một hình chữ nhật bắt đầu tại Point `(X = 100, Y = 100)` và có kích thước rộng 200, cao 150, thì góc dưới bên phải là Point `(X = 300, Y = 250)`. Trung tâm của hình chữ nhật là Point `(X = 200, Y = 175)`. Các phép toán này chỉ dùng cộng, trừ và chia đôi, nên hoàn toàn dựa trên các phép toán Point đã mô tả.

### 5.2 Point và Vector

Trong F-Shot, sự khác biệt giữa Point và Vector có thể không cần phân biệt quá cứng nhắc. Một Point có thể được coi như một vector tịnh tiến từ gốc tọa độ. Tuy nhiên, khi cần rõ ràng hơn, hệ thống có thể tách riêng Vector để biểu diễn sự thay đổi vị trí, còn Point để biểu diễn vị trí tuyệt đối.

Ví dụ: khi người dùng kéo vùng chọn từ vị trí `(X = 100, Y = 100)` đến `(X = 150, Y = 120)`, sự thay đổi là một Vector `(X = +50, Y = +20)`. Vector này được cộng vào tất cả các điểm của vùng chọn để di chuyển toàn bộ. Quyết định tách hay gộp Point và Vector sẽ được làm rõ trong quá trình thiết kế Rect và Selection.

### 5.3 Point và màn hình vật lý

Một Point trong Virtual Screen space cần được chuyển đổi sang tọa độ màn hình vật lý khi hệ thống cần biết điểm đó thuộc màn hình nào, hoặc khi cần vẽ lên một bitmap cụ thể.

Ví dụ: giả sử có hai màn hình. Màn hình A bắt đầu tại `(X = 0, Y = 0)`, rộng 1920 logical pixel, tỉ lệ 100%. Màn hình B bắt đầu tại `(X = 1920, Y = 0)`, rộng 2560 logical pixel, tỉ lệ 150%.

Một Point tại `(X = 2120, Y = 100)` nằm trên màn hình B, vì 2120 lớn hơn 1920. Khoảng cách từ mép trái màn hình B là `2120 - 1920 = 200` logical pixel. Vì tỉ lệ của B là 150%, vị trí này tương đương với `200 × 1.5 = 300` physical pixel tính từ mép trái màn hình B. Phần DpiAndScaling sẽ đi sâu vào việc chuyển đổi này.

---

## 6. Các câu hỏi cần quyết định

Trước khi chuyển sang triển khai, cần trả lời các câu hỏi sau:

| Câu hỏi | Lý do cần quyết định |
|---------|----------------------|
| Dùng số thập phân hay số nguyên cho tọa độ? | Số thập phân giữ chính xác khi scale, số nguyên đơn giản hơn nhưng dễ lệch |
| Có cần tách Vector thành kiểu riêng? | Giúp phân biệt vị trí tuyệt đối và sự dịch chuyển, nhưng tăng độ phức tạp |
| Có cần phiên bản Point dùng số nguyên cho pixel grid? | Có thể cần cho pixel manipulation hoặc snap-to-grid |
| Hàm so sánh hai điểm nên dùng ngưỡng bao nhiêu? | Ảnh hưởng độ chính xác của hit-testing |

---

## 7. Yêu cầu đối với kiểm thử

Các phép toán trên Point phải dễ kiểm thử bằng unit test. Một số tình huống cần kiểm tra:

- Cộng hai điểm cho ra điểm đúng.
- Trừ hai điểm cho ra khoảng cách và hướng đúng.
- Nhân với hệ số scale giữ tỉ lệ.
- Khoảng cách giữa hai điểm đối xứng bằng nhau.
- So sánh hai điểm gần nhau với ngưỡng sai số hoạt động đúng.

---

## 8. Kết nối với phần triển khai

Những quyết định trong tài liệu này sẽ được đưa vào:

- `src/FShot.Core/Geometry/Types.fs`: định nghĩa kiểu Point.
- `src/FShot.Core/Geometry/Operations.fs` (nếu cần): các phép toán trên Point.
- `tests/FShot.Core.Tests/Geometry/TypesTests.fs`: kiểm thử Point.

---

*Point là kiểu dữ liệu đơn giản nhưng quan trọng nhất trong F-Shot. Sau khi chốt thiết kế Point, chúng ta sẽ chuyển sang Rect — hình chữ nhật, vốn được xây dựng từ Point và kích thước.*
