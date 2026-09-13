# MarkerAndPixelate — Thiết kế chi tiết

> Tài liệu này thiết kế hai công cụ đặc biệt trong nhóm Annotation: Marker và Pixelate.

---

## 1. Mục đích

Marker và Pixelate là hai công cụ có cách sử dụng tương tự Pencil hoặc Rectangle, nhưng mục đích và kết quả render khác biệt.

- Marker dùng để làm nổi bật vùng nào đó bằng nét bán trong suốt phủ lên ảnh gốc. Người dùng dùng Marker giống như dùng bút nhớ dán giấy để đánh dấu đoạn văn bản hoặc vùng quan trọng.
- Pixelate dùng để che mờ thông tin nhạy cảm bằng cách chia vùng chọn thành các ô vuông lớn, mỗi ô mang một màu trung bình. Người dùng dùng Pixelate khi cần ẩn tên, mật khẩu, số điện thoại, hoặc các thông tin riêng tư khác.

Cả hai đều được kích hoạt bằng cách nhấn chuột, kéo, và thả chuột. Marker sử dụng chuyển động tự do, Pixelate sử dụng chuyển động tạo hình chữ nhật.

---

## 2. Marker

### 2.1 Ý tưởng

Marker tạo ra một nét bán trong suốt phủ lên ảnh chụp, giống như bút nhớ dán giấy. Nét Marker vẫn cho phép nhìn thấy hình ảnh bên dưới, nhưng đổi màu vùng đó theo màu người dùng chọn.

Khác với Pencil, Marker không nhằm vẽ chi tiết nhỏ mà nhằm phủ một vùng rộng để thu hút sự chú ý. Do đó, Marker có độ dày lớn hơn nhiều so với Pencil và sử dụng alpha blend để giữ lại hình ảnh gốc bên dưới.

### 2.2 Cách tương tác

Cách tương tác của Marker giống Pencil. Người dùng nhấn chuột tại điểm bắt đầu, giữ và di chuyển chuột tự do để tạo nét, sau đó thả chuột để kết thúc. Khác với Pencil, Marker thường được dùng để khoanh vùng hoặc tô nền, do đó người dùng thường vẽ các nét rộng hơn và ít chi tiết hơn.

Ví dụ, người dùng có thể dùng Marker để khoanh một đoạn văn bản quan trọng bằng cách vẽ một đường nét rộng quanh đoạn văn. Khi nhấn chuột, điểm đầu tiên được ghi nhận. Khi di chuyển chuột, các điểm tiếp theo được thêm vào chuỗi. Khi thả chuột, nét hoàn tất và sẵn sàng commit.

### 2.3 Dữ liệu

Một nét Marker được mô tả bởi một chuỗi điểm, tương tự Pencil. Tuy nhiên, Marker có các đặc điểm riêng:

- Độ dày nét lớn hơn Pencil đáng kể, thường gấp nhiều lần.
- Màu có độ trong suốt cao, thường alpha khoảng một phần ba đến một nửa giá trị tối đa.
- Không cần làm mịn quá mức vì Marker nhằm phủ vùng rộng, không cần chi tiết nhỏ.

Dữ liệu lưu trữ của Marker gồm:

- Chuỗi điểm đã thu thập, có thể đã qua giảm dư thừa nhẹ.
- Màu Marker.
- Độ dày nét.
- Giá trị alpha.
- Thời điểm tạo.

### 2.4 Cách render nét Marker

Khi render, hệ thống vẽ từng đoạn giữa các điểm trong chuỗi bằng một nét có độ dày lớn và alpha thấp. Các đoạn có thể được nối bằng đoạn thẳng hoặc đường cong đơn giản tùy thuộc vào yêu cầu mượt.

Ví dụ, với chuỗi điểm P0, P1, P2, hệ thống vẽ đoạn từ P0 đến P1, rồi từ P1 đến P2. Mỗi đoạn có cùng độ dày và alpha. Tại điểm giao nhau, phần chồng lấp sẽ làm vùng đó đậm hơn do alpha blend tích lũy.

### 2.5 Công thức trộn màu

Khi vẽ Marker lên ảnh, màu tại mỗi pixel dưới nét vẽ được tính bằng cách trộn màu Marker với màu gốc của ảnh theo tỉ lệ alpha.

Nếu ký hiệu màu gốc là G, màu Marker là M, và độ trong suốt của Marker là α, thì màu kết quả R được tính theo công thức:

`R = (1 - α) * G + α * M`

Trong đó α nằm trong khoảng từ 0 đến 1. Khi α bằng 0, vùng đó hoàn toàn trong suốt. Khi α bằng 1, vùng đó hoàn toàn bị che bởi màu Marker.

Công thức tương tự áp dụng cho từng kênh màu đỏ, xanh lá, xanh dương.

Ví dụ, nếu màu gốc của pixel là màu xám trung bình có giá trị kênh đỏ 128, màu Marker là vàng có giá trị kênh đỏ 255, và alpha là 0.4, thì kênh đỏ của pixel sau khi vẽ Marker là:

`R = (1 - 0.4) * 128 + 0.4 * 255 = 0.6 * 128 + 0.4 * 255 = 76.8 + 102 = 178.8`

Làm tròn thành 179. Tương tự cho các kênh xanh lá và xanh dương. Kết quả là một màu pha trộn giữa xám và vàng, nghiêng về vàng nhưng vẫn nhìn thấy màu gốc.

### 2.6 Lưu ý về chồng chéo

Nếu người dùng vẽ Marker đè lên một vùng đã có Marker, màu kết quả sẽ được tính tiếp từ màu hiện tại của vùng đó. Điều này có nghĩa là vùng bị đè nhiều lần sẽ đậm dần lên, phản ánh đúng hành vi của bút nhớ thật.

Ví dụ, nếu một pixel đã được phủ Marker vàng với alpha 0.4, kênh đỏ của pixel đó là 179 như tính ở trên. Nếu người dùng vẽ thêm một lớp Marker vàng với alpha 0.4 lên cùng pixel, thì kênh đỏ mới là:

`R = (1 - 0.4) * 179 + 0.4 * 255 = 0.6 * 179 + 0.4 * 255 = 107.4 + 102 = 209.4`

Làm tròn thành 209. Pixel này giờ gần với màu vàng hơn so với lần đầu.

Nếu hệ thống render lại từ đầu từ danh sách Annotation, hiệu ứng chồng chéo phải được tái tạo giống hệt. Điều này có nghĩa là Marker phải được render theo thứ tự thời gian tạo, và mỗi lần render phải áp dụng alpha blend lên kết quả đã có.

### 2.7 Preview và commit

Trong lúc vẽ, hệ thống hiển thị preview bằng cách vẽ nét Marker lên một lớp tạm. Lớp tạm này cho phép vẽ nét bán trong suốt mà không làm thay đổi ảnh gốc bên dưới. Khi thả chuột, nét được commit thành một Annotation và đẩy vào lịch sử.

#### 2.7.1 Cách tạo Preview

Khi người dùng nhấn chuột, hệ thống tạo Preview Marker với chuỗi điểm ban đầu chỉ chứa điểm nhấn chuột. Mỗi khi chuột di chuyển, điểm mới được thêm vào chuỗi. Preview được vẽ lên lớp tạm với độ dày và alpha của Marker.

#### 2.7.2 Cách cập nhật Preview

Preview được cập nhật mỗi khi có sự kiện chuột di chuyển. Có thể cập nhật bằng cách thêm điểm mới vào chuỗi và render lại toàn bộ nét trên lớp tạm. Cách này đơn giản và đủ nhanh vì số điểm trong một nét Marker thường không quá lớn.

#### 2.7.3 Cách commit

Khi thả chuột:

- Chuỗi điểm thô được giảm dư thừa nhẹ.
- Một đối tượng Marker hoàn chỉnh được tạo ra với màu, độ dày, alpha, và chuỗi điểm.
- Đối tượng này được đóng gói thành Annotation.
- Annotation mới được thêm vào danh sách chú thích, tạo ra snapshot mới.
- Snapshot mới được đẩy vào HistoryStack.

#### 2.7.4 Cách hủy Preview

Nếu người dùng nhấn Escape hoặc chuyển sang công cụ khác trong lúc vẽ, Preview bị hủy. Lớp tạm bị xóa và không có Annotation nào được tạo.

---

## 3. Pixelate

### 3.1 Ý tưởng

Pixelate dùng để che thông tin nhạy cảm như tên, mật khẩu, số điện thoại, bằng cách làm mờ vùng chọn theo dạng mosaic. Thay vì làm mờ Gaussian thông thường, Pixelate chia vùng thành các ô vuông có kích thước cố định và thay thế toàn bộ pixel trong mỗi ô bằng một màu duy nhất.

Kết quả là thông tin trong vùng đã chọn trở nên không đọc được, nhưng vùng đó vẫn giữ được hình dạng và vị trí tổng thể. Điều này hữu ích khi người dùng muốn chỉ ra vị trí của thông tin nhạy cảm mà không tiết lộ nội dung.

### 3.2 Cách tương tác

Cách tương tác của Pixelate giống Rectangle hơn là Pencil. Người dùng nhấn chuột tại một góc của vùng cần che, kéo chuột đến góc đối diện, rồi thả chuột để xác định hình chữ nhật cần pixelate.

Ví dụ, người dùng muốn che một dòng chữ chứa email. Họ nhấn chuột tại góc trái trên của dòng chữ, kéo đến góc phải dưới, thả chuột. Toàn bộ vùng dòng chữ đó sẽ được chuyển thành các ô vuông mờ.

Trong lúc kéo, hệ thống hiển thị một hình chữ nhật tạm để người dùng xác nhận vùng cần che. Khi thả chuột, hiệu ứng mosaic mới thực sự được áp dụng.

### 3.3 Dữ liệu

Một tác vụ Pixelate được mô tả bởi hai điểm tạo thành một hình chữ nhật, cùng với kích thước ô mosaic. Thông thường kích thước ô có giá trị mặc định và có thể điều chỉnh.

Pixelate không dùng chuỗi điểm tự do như Pencil hay Marker. Người dùng kéo chuột từ một góc đến góc đối diện để xác định hình chữ nhật cần che mờ.

Dữ liệu lưu trữ của Pixelate gồm:

- Hai điểm tạo thành hình chữ nhật cần che.
- Kích thước ô mosaic.
- Thời điểm tạo.

### 3.4 Cách xác định vùng xử lý

Từ hai điểm đầu và cuối, hệ thống tính toán hình chữ nhật bao chuẩn hóa. Góc trái trên của hình chữ nhật là điểm có tọa độ x nhỏ nhất và y nhỏ nhất. Chiều rộng là khoảng cách theo trục x giữa hai điểm. Chiều cao là khoảng cách theo trục y giữa hai điểm.

Ví dụ, nếu điểm đầu là (100, 80) và điểm cuối là (300, 200), thì hình chữ nhật bao có góc trái trên là (100, 80), chiều rộng là 200, chiều cao là 120.

Nếu điểm đầu nằm bên phải điểm cuối, chẳng hạn điểm đầu là (300, 200) và điểm cuối là (100, 80), thì sau khi chuẩn hóa, hình chữ nhật vẫn là (100, 80, 200, 120). Điều này đảm bảo thuật toán xử lý không phụ thuộc vào thứ tự nhấn và thả chuột.

### 3.5 Thuật toán xử lý

Vùng Pixelate được xử lý trực tiếp trên mảng byte của ảnh. Quy trình như sau:

- Xác định hình chữ nhật cần che mờ dựa trên hai điểm đầu và cuối.
- Chia hình chữ nhật đó thành lưới ô vuông theo kích thước ô.
- Với mỗi ô, tính màu trung bình của tất cả các pixel gốc nằm trong ô đó.
- Gán toàn bộ pixel trong ô bằng màu trung bình đã tính.

Công thức màu trung bình:

`avgColor = (sum of all pixel colors in block) / (number of pixels in block)`

Nếu ký hiệu tổng màu của các pixel trong ô là S và số pixel trong ô là N, thì màu trung bình của ô là S chia cho N. Công thức này áp dụng riêng cho từng kênh màu.

Ví dụ, nếu một ô chứa 100 pixel, tổng giá trị kênh đỏ của các pixel là 12000, thì giá trị đỏ trung bình của ô là 12000 chia 100, bằng 120.

Nếu một ô có 50 pixel, tổng giá trị kênh xanh lá là 7500, thì giá trị xanh lá trung bình là 7500 chia 50, bằng 150.

### 3.6 Cách xử lý biên

Nếu hình chữ nhật không chia hết cho kích thước ô, các ô ở biên có thể nhỏ hơn kích thước chuẩn. Hệ thống vẫn tính màu trung bình cho các ô biên này theo số pixel thực tế có trong ô.

Ví dụ, nếu hình chữ nhật rộng 55 pixel, cao 30 pixel, và kích thước ô là 10 pixel, thì:

- Theo chiều rộng, sẽ có 5 ô đầy đủ 10 pixel và một ô cuối rộng 5 pixel.
- Theo chiều cao, sẽ có 3 ô đầy đủ 10 pixel.

Tổng cộng có 18 ô: 15 ô đầy đủ 10x10 và 3 ô biên rộng 5x10. Mỗi ô đều được tính màu trung bình từ số pixel thực tế của nó.

### 3.7 Chế độ bảo mật

Có hai cách thực hiện Pixelate:

- Cách nhanh: lấy mẫu một pixel duy nhất ở góc ô và dùng nó cho toàn bộ ô. Cách này nhanh hơn nhưng có thể để lộ thông tin nếu pixel được chọn may mắn chứa dữ liệu nhạy cảm.
- Cách an toàn: tính trung bình toàn bộ pixel trong ô. Cách này chậm hơn nhưng đảm bảo thông tin bị trộn đều.

Trong MVP, nên sử dụng cách tính trung bình để đảm bảo tính năng bảo mật cơ bản.

Ví dụ, nếu một ô chứa chữ cái "A" màu đen trên nền trắng, cách lấy mẫu một pixel có thể chọn đúng pixel chữ "A" và vẫn để lộ hình dạng chữ. Cách tính trung bình sẽ cho ra màu xám, làm mất hoàn toàn hình dạng chữ.

### 3.8 Preview và commit

Trong lúc kéo chuột, hệ thống hiển thị một hình chữ nhật semi-transparent để chỉ vùng sẽ bị Pixelate. Preview này chưa thực hiện xử lý pixel thật. Preview giúp người dùng xác nhận vùng cần che trước khi commit.

#### 3.8.1 Cách tạo Preview

Khi người dùng nhấn chuột, hệ thống tạo Preview Pixelate với hai điểm ban đầu là cùng một vị trí. Khi chuột di chuyển, điểm thứ hai được cập nhật, và hình chữ nhật tạm được vẽ từ điểm nhấn đến vị trí chuột hiện tại.

Preview có thể là một hình chữ nhật trong suốt màu xám hoặc một lớp phủ checkerboard để người dùng nhận biết vùng sẽ bị xử lý.

#### 3.8.2 Cách cập nhật Preview

Preview được cập nhật liên tục khi chuột di chuyển. Hình chữ nhật tạm được thay thế bằng hình chữ nhật mới có kích thước cập nhật. Không có xử lý pixel thật nào xảy ra trong giai đoạn này.

#### 3.8.3 Cách commit

Khi thả chuột, hệ thống thực hiện các bước:

- Chuẩn hóa hình chữ nhật từ hai điểm đầu và cuối.
- Áp dụng thuật toán pixelate lên ảnh gốc trong vùng đã chọn.
- Tạo Annotation Pixelate lưu hình chữ nhật và kích thước ô.
- Thêm Annotation vào danh sách chú thích, tạo snapshot mới.
- Đẩy snapshot mới vào HistoryStack.

#### 3.8.4 Cách hủy Preview

Nếu người dùng nhấn Escape hoặc chuyển công cụ trong lúc kéo, Preview bị hủy. Không có xử lý pixel nào được áp dụng và không có Annotation nào được tạo.

### 3.9 Cách lưu trữ sau commit

Annotation Pixelate lưu:

- Hai điểm tạo thành hình chữ nhật cần che.
- Kích thước ô mosaic.
- Thời điểm tạo.

Khi render lại, hệ thống có thể:

- Áp dụng lại thuật toán pixelate lên ảnh gốc trong vùng đã lưu.
- Hoặc lưu sẵn ảnh đã pixelate trong snapshot và render trực tiếp.

Trong MVP, cách thứ hai đơn giản hơn vì tránh việc tính toán lại mỗi lần render. Tuy nhiên, cách này làm tăng kích thước snapshot. Cách thứ nhất tiết kiệm bộ nhớ hơn nhưng tốn CPU hơn khi render. Quyết định này cần cân nhắc dựa trên hiệu năng thực tế.

### 3.10 Tác động đến undo và redo

Vì Pixelate thay đổi trực tiếp pixel của ảnh gốc trong vùng chọn, việc undo không thể đơn giản là xóa Annotation khỏi danh sách. Hệ thống cần phục hồi lại ảnh gốc trong vùng đó.

Có hai cách phục hồi:

- Lưu bản sao của ảnh gốc trước khi pixelate trong snapshot. Khi undo, thay thế vùng đã pixelate bằng bản sao gốc.
- Lưu toàn bộ ảnh đã pixelate trong snapshot. Khi undo, quay lại ảnh trước đó.

Trong MVP, nên chọn cách lưu toàn bộ ảnh trong snapshot để đơn giản hóa undo/redo. Cách này tốn bộ nhớ hơn nhưng đảm bảo tính đúng đắn. Chi tiết cụ thể sẽ được thiết kế trong phần History.

### 3.11 Cách điều chỉnh kích thước ô

Trong MVP, kích thước ô mosaic có thể là một giá trị cố định hoặc lấy từ cấu hình. Người dùng không thể điều chỉnh trực tiếp trong lúc vẽ. Tuy nhiên, giá trị này có thể được thay đổi qua cửa sổ cài đặt hoặc phím tắt tăng giảm kích thước công cụ nếu có.

Ví dụ, kích thước ô mặc định có thể là 10 pixel. Nếu người dùng muốn che mờ mạnh hơn, họ có thể tăng lên 20 pixel. Nếu muốn che mờ nhẹ hơn, họ có thể giảm xuống 5 pixel. Việc điều chỉnh này ảnh hưởng đến các lần pixelate tiếp theo, không ảnh hưởng đến các Annotation đã commit.

---

## 4. So sánh Marker và Pixelate

| Đặc điểm | Marker | Pixelate |
|----------|--------|----------|
| Cách tương tác | Kéo tự do như bút | Kéo tạo hình chữ nhật |
| Mục đích | Làm nổi bật vùng | Che giấu thông tin |
| Xử lý pixel | Trộn alpha trên lớp vẽ tạm | Thay đổi trực tiếp mảng byte ảnh |
| Có cần làm mịn | Có thể nhẹ để giảm dư thừa | Không |
| Dữ liệu lưu | Danh sách điểm + alpha + độ dày | Hình chữ nhật + kích thước ô |
| Tác động đến ảnh gốc | Không, chỉ phủ lên trên | Có, thay đổi pixel trong vùng |
| Cách undo | Xóa Annotation khỏi danh sách | Phục thuộc snapshot lưu trạng thái ảnh |

---

## 5. Các câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Giá trị alpha mặc định của Marker là bao nhiêu? | Ảnh hưởng độ trong suốt và khả năng nhìn xuyên qua |
| Độ dày mặc định của Marker là bao nhiêu? | Ảnh hưởng cảm giác vẽ highlight |
| Kích thước ô mosaic mặc định là bao nhiêu pixel? | Ảnh hưởng mức độ che giấu và thẩm mỹ |
| Pixelate nên xử lý trên ảnh gốc hay trên một bản sao? | Ảnh hưởng hiệu năng và khả năng undo |
| Có cho phép điều chỉnh kích thước ô mosaic trong lúc vẽ không? | Ảnh hưởng UX |
| Có lưu ảnh đã pixelate trong snapshot hay tính lại mỗi lần render? | Ảnh hưởng bộ nhớ và CPU |

---

## 6. Kết nối với các phần khác

- Marker sử dụng cùng kiểu dữ liệu điểm như Pencil, nhưng với thuộc tính alpha và độ dày khác.
- Pixelate sử dụng kiểu hình chữ nhật từ Selection và thao tác trực tiếp trên dữ liệu ảnh từ CaptureResult.
- Rendering.Skia sẽ vẽ Marker bằng alpha blend và vẽ Pixelate bằng cách render ảnh đã xử lý hoặc tái tạo hiệu ứng mosaic.
- History cần lưu trạng thái ảnh sau khi Pixelate để đảm bảo undo/redo đúng.

---

*Sau khi chốt Marker và Pixelate, chúng ta chuyển sang TextTool — công cụ duy nhất trong MVP cần nhập liệu từ bàn phím.*
