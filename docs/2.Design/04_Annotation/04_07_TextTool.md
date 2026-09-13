# TextTool — Thiết kế chi tiết

> Tài liệu này thiết kế công cụ Text, công cụ chú thích duy nhất trong MVP cần nhập liệu từ bàn phím.

---

## 1. Mục đích

Text tool cho phép người dùng thêm văn bản vào ảnh chụp. Khác với các công cụ hình học, Text cần một giai đoạn nhập liệu riêng biệt: người dùng chọn vị trí, gõ nội dung, định dạng nếu cần, rồi xác nhận để chuyển thành một phần của ảnh kết quả.

Mục tiêu thiết kế:

- Nhập liệu phải tự nhiên, hỗ trợ Unicode và các phím thông dụng.
- Văn bản sau khi commit phải được render phẳng vào ảnh, không còn phụ thuộc vào control nhập liệu.
- Có thể undo nếu người dùng muốn xóa hoặc thay đổi.

Text tool khác biệt so với các công cụ khác vì nó cần một trạng thái chỉnh sửa tạm thời. Trong khi Pencil, Line, Rectangle chỉ cần chuột để hoàn thành, Text cần cả chuột để chọn vị trí và bàn phím để nhập nội dung.

---

## 2. Giai đoạn tương tác

Text tool hoạt động qua ba giai đoạn chính: chọn vị trí, nhập liệu, và commit.

### 2.1 Chọn vị trí

Người dùng nhấn chuột tại vị trí mong muốn trên ảnh. Điểm này trở thành gốc của văn bản. Tùy cách căn chỉnh, gốc có thể là góc trái trên, giữa trên, hoặc góc phải trên của khối văn bản.

Ví dụ, nếu cách căn là căn trái, điểm nhấn chuột tại (200, 150) sẽ là góc trái trên của dòng chữ đầu tiên. Dòng chữ sẽ mở rộng sang phải từ điểm này. Nếu cách căn là căn giữa, điểm (200, 150) sẽ là điểm giữa trên cùng của khối văn bản. Nửa chiều rộng văn bản sẽ nằm bên trái điểm này và nửa còn lại nằm bên phải.

Trong MVP, cách căn mặc định là căn trái. Điều này phù hợp với hầu hết các ngôn ngữ đọc từ trái sang phải và đơn giản hóa việc đặt vị trí.

### 2.2 Nhập liệu

Sau khi chọn vị trí, một control nhập liệu xuất hiện tại chỗ, cho phép người dùng gõ trực tiếp. Control này hiển thị nội dung đang nhập với các thuộc tính định dạng hiện tại như cỡ chữ, màu, in đậm, in nghiêng, gạch chân.

Trong giai đoạn này, văn bản chưa được coi là chú thích hoàn thành. Nó chỉ là preview tạm thời. Người dùng có thể:

- Tiếp tục gõ ký tự.
- Thay đổi định dạng như in đậm, in nghiêng, gạch chân.
- Di chuyển con trỏ bằng phím mũi tên.
- Chọn và xóa một phần nội dung.
- Hủy bằng phím Escape.

Ví dụ cụ thể: người dùng nhấn chuột tại vị trí (200, 150). Control nhập liệu xuất hiện. Người dùng gõ "Lỗi ở đây". Họ nhấn Ctrl+A để chọn toàn bộ, rồi nhấn Ctrl+B để in đậm. Dòng chữ trong control hiển thị đậm hơn. Toàn bộ quá trình này diễn ra trong giai đoạn nhập liệu.

Trong MVP, control nhập liệu nên dùng TextBox tích hợp của UI framework. Cách này đảm bảo người dùng có trải nghiệm nhập liệu quen thuộc, hỗ trợ bộ gõ tiếng Việt, emoji, và các phím tắt chuẩn. Sau khi commit, văn bản được render phẳng bằng thư viện đồ họa.

### 2.3 Commit

Khi người dùng nhấn Enter hoặc click ra ngoài vùng nhập liệu, giai đoạn nhập liệu kết thúc. Nội dung văn bản cùng với các thuộc tính định dạng được chuyển thành một đối tượng Text Annotation cố định. Đối tượng này sau đó được thêm vào danh sách chú thích và lưu vào lịch sử.

Nếu nội dung rỗng khi commit, Annotation không được tạo.

Ví dụ, sau khi gõ "Lỗi ở đây" và nhấn Enter:

- Control nhập liệu biến mất.
- Dòng chữ "Lỗi ở đây" được render phẳng vào ảnh tại vị trí (200, 150).
- Một Text Annotation được tạo với nội dung "Lỗi ở đây", cỡ chữ 20, màu đỏ, in đậm.
- Text Annotation được thêm vào danh sách chú thích của snapshot hiện tại.
- Snapshot mới được đẩy vào HistoryStack.

Nếu người dùng nhấn Escape thay vì Enter, control nhập liệu biến mất và không có Annotation nào được tạo.

---

## 3. Dữ liệu Text Annotation

Một Text Annotation cần lưu các thông tin sau:

- Vị trí gốc trên ảnh, là điểm do người dùng chọn.
- Nội dung văn bản dạng chuỗi ký tự.
- Cỡ chữ.
- Màu chữ.
- Kiểu định dạng: in đậm, in nghiêng, gạch chân.
- Cách căn chỉnh: trái, giữa, phải.
- Thời điểm tạo để sắp xếp lớp.
- Font mặc định được dùng để render.

Ví dụ, một Text Annotation có thể lưu:

- Vị trí gốc: (200, 150)
- Nội dung: "Lỗi ở đây"
- Cỡ chữ: 20 pixel
- Màu chữ: đỏ
- In đậm: có
- Căn trái
- Thời điểm tạo: thời điểm nhấn Enter
- Font: font mặc định từ cấu hình

Tất cả các thuộc tính này là bất biến sau khi commit. Nếu người dùng muốn thay đổi, họ phải xóa Annotation cũ và tạo Annotation mới. Khả năng sửa đổi trực tiếp thuộc phạm vi v1.x.

---

## 4. Tính toán kích thước và vị trí

### 4.1 Bounding box

Kích thước thực của văn bản được tính dựa trên nội dung, cỡ chữ, font, và các kiểu định dạng. Hệ thống render sẽ đo chiều rộng và chiều cao của chuỗi ký tự trước khi vẽ.

Ví dụ, chuỗi "ABC" với cỡ chữ 20 pixel có thể rộng 36 pixel và cao 24 pixel, tùy thuộc vào font. Chuỗi dài hơn sẽ có chiều rộng lớn hơn tỉ lệ với số ký tự. Cụ thể, chuỗi "Lỗi ở đây" với cùng cỡ chữ có thể rộng khoảng 120 pixel và cao 24 pixel.

Công thức tính chiều rộng tổng quát:

`width = sum of advance widths of all glyphs in the string`

Trong đó advance width của mỗi glyph là khoảng cách từ đầu glyph đến đầu glyph tiếp theo, phụ thuộc vào font và cỡ chữ.

Chiều cao của dòng văn bản thường được tính từ font metrics:

`height = ascent + descent + line gap`

Trong đó ascent là khoảng cách từ baseline lên đỉnh chữ, descent là khoảng cách từ baseline xuống đáy chữ, và line gap là khoảng cách giữa các dòng. Trong MVP chỉ có một dòng nên line gap không đóng vai trò quan trọng.

Vị trí gốc được dùng để xác định góc của bounding box. Nếu căn trái, gốc là góc trái trên. Nếu căn giữa, gốc là điểm giữa trên cùng. Nếu căn phải, gốc là góc phải trên.

Ví dụ, với vị trí gốc (200, 150) và bounding box (120, 24):

- Căn trái: góc trái trên là (200, 150), góc phải dưới là (320, 174).
- Căn giữa: góc trái trên là (140, 150), góc phải dưới là (260, 174).
- Căn phải: góc trái trên là (80, 150), góc phải dưới là (200, 174).

### 4.2 Giới hạn trong ảnh

Nếu văn bản vượt ra ngoài biên ảnh sau khi nhập, có hai cách xử lý:

- Cho phép văn bản tràn ra ngoài và chỉ hiển thị phần nằm trong ảnh.
- Tự động dịch chuyển vị trí gốc để toàn bộ văn bản nằm trong ảnh.

Trong MVP, nên chọn cách cho phép tràn, sau đó cắt theo biên ảnh khi xuất. Cách này đơn giản và không gây bất ngờ cho người dùng.

Ví dụ, nếu ảnh rộng 1920 pixel và người dùng đặt văn bản tại vị trí (1850, 150) với chiều rộng 120 pixel, thì văn bản sẽ tràn từ 1850 đến 1970. Phần từ 1920 đến 1970 sẽ bị cắt khi render và xuất ảnh. Phần nhìn thấy được chỉ từ 1850 đến 1920.

Người dùng có thể di chuyển vị trí đặt văn bản trong giai đoạn nhập liệu nếu muốn hiển thị toàn bộ. Tuy nhiên, trong MVP, sau khi commit, vị trí không thể di chuyển trừ khi xóa và tạo lại.

---

## 5. Render sau commit

Sau khi commit, văn bản được render phẳng vào ảnh. Có hai cách tiếp cận:

- Render trực tiếp bằng thư viện đồ họa, sử dụng font hệ thống. Cách này cho kết quả nhất quán khi xuất ảnh.
- Chụp ảnh control nhập liệu và vẽ ảnh đó lên screenshot. Cách này dễ triển khai nhưng có thể gặp vấn đề về độ phân giải và trong suốt.

Trong MVP, nên render trực tiếp bằng thư viện đồ họa để đảm bảo chất lượng và tính nhất quán.

Ví dụ, Text Annotation với nội dung "Lỗi ở đây", cỡ chữ 20, màu đỏ, in đậm, sẽ được vẽ tại vị trí gốc với các thuộc tính tương ứng. Font mặc định được lấy từ cấu hình. Kết quả là dòng chữ màu đỏ, in đậm, nằm tại vị trí đã chọn, phẳng trên ảnh.

### 5.1 Cách render từng glyph

Khi render trực tiếp, hệ thống duyệt từng ký tự trong chuỗi. Với mỗi ký tự, hệ thống:

- Lấy glyph tương ứng từ font.
- Tính vị trí x của glyph dựa trên tổng advance width của các glyph trước đó.
- Vẽ glyph tại vị trí đã tính, với cỡ chữ, màu, và kiểu định dạng.

Ví dụ, chuỗi "ABC" được render như sau:

- Glyph A được vẽ tại vị trí x ban đầu.
- Glyph B được vẽ tại vị trí x cộng với advance width của A.
- Glyph C được vẽ tại vị trí x cộng với advance width của A và B.

Cách căn chỉnh ảnh hưởng đến vị trí x ban đầu. Căn trái bắt đầu từ gốc. Căn giữa bắt đầu từ gốc trừ đi một nửa tổng chiều rộng. Căn phải bắt đầu từ gốc trừ đi tổng chiều rộng.

### 5.2 Xử lý kiểu định dạng

Các kiểu định dạng ảnh hưởng đến cách render:

- In đậm: sử dụng font weight đậm hơn hoặc vẽ glyph dày hơn.
- In nghiêng: áp dụng phép biến đổi nghiêng theo trục x.
- Gạch chân: vẽ thêm một đường ngang dưới baseline.

Trong MVP, các kiểu này áp dụng đồng những cho toàn bộ chuỗi. Việc định dạng từng phần trong cùng một Text Annotation thuộc phạm vi v1.x.

---

## 6. Undo và sửa đổi

Khi Text Annotation đã được commit, nó trở thành một phần của danh sách chú thích. Người dùng có thể undo để xóa nó. Khả năng sửa đổi văn bản đã commit thuộc phạm vi v1.x, không bắt buộc trong MVP.

Tuy nhiên, trong giai đoạn nhập liệu, người dùng vẫn có thể hủy bằng Escape để không tạo Annotation nào.

Ví dụ, nếu người dùng gõ sai và nhấn Escape trước khi nhấn Enter, control nhập liệu biến mất và không có Annotation được tạo. Nếu người dùng đã nhấn Enter, họ phải dùng Ctrl+Z để undo.

### 6.1 Cách hoạt động của undo

Khi undo, hệ thống lấy snapshot trước đó từ HistoryStack. Snapshot trước đó không chứa Text Annotation vừa thêm. Do đó, văn bản biến mất khỏi ảnh. Khi redo, snapshot sau đó được khôi phục, văn bản xuất hiện trở lại.

Vì dữ liệu là bất biến, không có Annotation nào bị xóa thực sự. Chỉ có con trỏ đến snapshot hiện tại trong HistoryStack thay đổi.

---

## 7. Các câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Control nhập liệu dùng TextBox tích hợp của UI framework hay tự render? | Ảnh hưởng UX nhập liệu và cách commit |
| Có hỗ trợ đa dòng không? | Ảnh hưởng phạm vi tính năng Text trong MVP |
| Có hỗ trợ thay đổi font hay chỉ dùng font mặc định? | Ảnh hưởng Config và UI |
| Nhấn Enter là commit hay xuống dòng? | Ảnh hưởng cách người dùng kết thúc nhập liệu |
| Có cho phép kéo di chuyển vị trí văn bản sau khi đặt không? | Ảnh hưởng tương tác trong giai đoạn nhập |
| Vị trí gốc mặc định là căn trái, căn giữa, hay căn phải? | Ảnh hưởng UX khi đặt văn bản |
| Có cho phép xoay văn bản không? | Ảnh hưởng phạm vi tính năng và render |

---

## 8. Kết nối với các phần khác

- Geometry cung cấp kiểu Point để xác định vị trí văn bản.
- Config cung cấp giá trị mặc định cho cỡ chữ, màu chữ, font, và các kiểu định dạng.
- UI cung cấp control nhập liệu và chuyển sự kiện bàn phím thành lệnh cập nhật preview.
- Rendering.Skia thực hiện việc vẽ văn bản đã commit lên ảnh.
- History đẩy snapshot khi Text Annotation được commit.

---

*Sau khi chốt TextTool, nhóm Annotation cơ bản đã đầy đủ. Chúng ta chuyển sang CommitAndPreview để định nghĩa rõ ranh giới giữa preview và trạng thái lưu trữ.*
