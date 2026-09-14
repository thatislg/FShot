# ToolModel — Thiết kế chi tiết

> Tài liệu này mô tả mô hình dữ liệu cho các công cụ chú thích trong F-Shot.

---

## 1. Tool là gì?

`Tool` là khái niệm trừu tượng dùng để phân loại hình học mà người dùng đang vẽ. Mỗi loại tool mang theo một bộ tham số riêng, phù hợp với cách tương tác và kết quả render của nó.

Ví dụ, một công cụ vẽ đường thẳng chỉ cần hai điểm đầu và cuối. Một công cụ vẽ tự do cần một chuỗi nhiều điểm. Một công cụ vẽ hình chữ nhật cần hai điểm tạo góc đối diện và có thể thêm bán kính bo góc.

---

## 2. Các loại tool trong MVP

Trong phiên bản MVP, F-Shot hỗ trợ chín loại tool. Dưới đây là mô tả dữ liệu của từng loại, diễn giải hoàn toàn bằng lời.

### 2.1 Pencil

Pencil mô tả một nét vẽ tự do. Dữ liệu của nó gồm một danh sách các điểm được thu thập theo thứ tự thời gian từ lúc nhấn chuột đến lúc thả chuột. Danh sách này sau khi làm mịn sẽ được dùng để vẽ đường cong.

**Cách tương tác:** Người dùng nhấn chuột tại điểm bắt đầu, giữ và di chuyển chuột để tạo đường nét, thả chuột để kết thúc. Chuỗi điểm thu được từ sự kiện chuột di chuyển sẽ được lưu theo thứ tự.

**Kết quả hình học:** Một đường cong liền mạch đi qua hoặc gần các điểm đã thu thập, với độ dày đồng nhất và màu do người dùng chọn.

**Ràng buộc:** Không có ràng buộc hình học nào trong lúc vẽ. Đường nét chỉ phụ thuộc vào chuyển động của chuột.

**Preview:** Trong lúc giữ chuột, hệ thống vẽ đường nét đang hình thành ngay lập tức.

**Commit:** Khi thả chuột, chuỗi điểm được làm mịn và chuyển thành Annotation.

### 2.2 Line

Line mô tả một đoạn thẳng. Dữ liệu gồm hai điểm: điểm bắt đầu và điểm kết thúc.

**Cách tương tác:** Người dùng nhấn chuột tại điểm bắt đầu, giữ và di chuyển chuột đến điểm kết thúc, thả chuột để hoàn tất.

**Kết quả hình học:** Một đoạn thẳng nối hai điểm, với độ dày và màu do người dùng chọn.

**Ràng buộc:** Không có ràng buộc góc trong MVP. Trong v1.x có thể thêm ràng buộc góc 45° hoặc 90° khi giữ phím điều khiển.

**Preview:** Trong lúc kéo chuột, đoạn thẳng từ điểm bắt đầu đến vị trí chuột hiện tại được hiển thị liên tục.

**Commit:** Khi thả chuột, hai điểm cuối cùng được ghi nhận và chuyển thành Annotation.

### 2.3 Arrow

Arrow mô tả một đoạn thẳng kèm mũi tên ở một đầu. Dữ liệu gồm hai điểm tạo thành thân mũi tên, cùng với thông tin về hướng và kiểu của đầu mũi tên. Trong MVP, mũi tên mặc định hướng từ điểm bắt đầu đến điểm kết thúc.

**Cách tương tác:** Tương tự Line. Người dùng nhấn chuột, kéo, thả chuột.

**Kết quả hình học:** Một đoạn thẳng nối hai điểm, kèm theo phần đầu mũi tên tại điểm kết thúc. Đầu mũi tên gồm hai cánh tạo thành tam giác cân hoặc tam giác nhọn, có kích thước tỉ lệ với độ dày nét.

**Ràng buộc:** Trong MVP, đầu mũi tên luôn ở điểm kết thúc. Không hỗ trợ đảo hướng hay đầu mũi tên cong trong MVP.

**Preview:** Hiển thị đoạn thẳng và đầu mũi tên tạm thời trong lúc kéo chuột.

**Commit:** Khi thả chuột, hệ thống ghi nhận hai điểm và tạo Annotation Arrow.

### 2.4 Rectangle

Rectangle mô tả một hình chữ nhật. Dữ liệu gồm hai điểm tạo thành hai góc đối diện của hình chữ nhật, cùng với giá trị bán kính bo góc nếu có. Hình chữ nhật có thể được vẽ dưới dạng chỉ viền, tô đặc, hoặc cả hai tùy cấu hình.

**Cách tương tác:** Người dùng nhấn chuột tại một góc, kéo đến góc đối diện, thả chuột.

**Kết quả hình học:** Một hình chữ nhật có bốn cạnh song song với trục tọa độ. Nếu có bán kính bo góc, bốn góc của hình chữ nhật được thay thế bằng các cung tròn. Trong MVP hình chữ nhật mặc định chỉ có viền, không tô đặc.

**Ràng buộc:** Không có ràng buộc tỉ lệ trong MVP. Trong v1.x, giữ phím điều khiển sẽ khóa tỉ lệ 1:1.

**Preview:** Hình chữ nhật tạm được vẽ từ điểm bắt đầu đến vị trí chuột hiện tại.

**Commit:** Khi thả chuột, hai góc đối diện và bán kính bo góc được ghi nhận.

### 2.5 Circle

Circle mô tả một hình tròn hoặc hình elip. Dữ liệu gồm hai điểm tạo thành hình chữ nhật bao quanh đường tròn hoặc elip. Nếu người dùng giữ phím khóa tỉ lệ trong lúc vẽ, hình dạng sẽ bị ràng buộc thành hình tròn thay vì elip.

**Cách tương tác:** Tương tự Rectangle. Người dùng nhấn chuột, kéo để xác định hình chữ nhật bao, thả chuột.

**Kết quả hình học:** Một đường elip nằm trong hình chữ nhật bao. Nếu hình chữ nhật bao là hình vuông, kết quả là hình tròn. Trong MVP hình tròn mặc định chỉ có viền.

**Ràng buộc:** Không có ràng buộc trong MVP. Trong v1.x, giữ phím điều khiển sẽ buộc hình vuông bao để tạo hình tròn.

**Preview:** Đường elip tạm được vẽ trong hình chữ nhật bao từ điểm bắt đầu đến vị trí chuột.

**Commit:** Khi thả chuột, hai góc của hình chữ nhật bao được ghi nhận.

### 2.6 Marker

Marker mô tả một nét vẽ tự do bán trong suốt, tương tự Pencil về cách thu thập điểm. Điểm khác biệt nằm ở thuộc tính: Marker có độ dày lớn hơn và độ trong suốt cao hơn để tạo hiệu ứng làm nổi bật.

**Cách tương tác:** Giống Pencil. Nhấn chuột, giữ, di chuyển tự do, thả chuột.

**Kết quả hình học:** Một nét rộng, bán trong suốt phủ lên ảnh gốc. Màu tại các pixel dưới nét được trộn theo alpha giữa màu gốc và màu Marker.

**Ràng buộc:** Không có ràng buộc hình học.

**Preview:** Nét Marker tạm được vẽ với alpha blend trong lúc di chuyển chuột.

**Commit:** Khi thả chuột, chuỗi điểm được chuyển thành Annotation.

### 2.7 Text

Text mô tả một khối văn bản. Dữ liệu gồm vị trí gốc trên ảnh, nội dung chuỗi ký tự, cỡ chữ, màu chữ, các kiểu định dạng như in đậm, in nghiêng, gạch chân, và cách căn chỉnh.

**Cách tương tác:** Người dùng nhấn chuột tại vị trí đặt văn bản. Một control nhập liệu xuất hiện tại chỗ cho phép gõ. Khi nhấn Enter hoặc click ra ngoài, văn bản được commit.

**Kết quả hình học:** Một khối văn bản nằm tại vị trí đã chọn, với kích thước tính từ nội dung, cỡ chữ, và font.

**Ràng buộc:** Trong MVP chỉ hỗ trợ một dòng. Nhấn Enter sẽ commit thay vì xuống dòng.

**Preview:** Nội dung đang gõ được hiển thị qua control nhập liệu tại chỗ, với định dạng hiện tại.

**Commit:** Khi nhấn Enter hoặc mất focus, nếu nội dung không rỗng, văn bản được chuyển thành Annotation.

### 2.8 Pixelate

Pixelate mô tả một vùng hình chữ nhật được xử lý hiệu ứng mosaic. Dữ liệu gồm hai điểm tạo thành hình chữ nhật cần che mờ và kích thước của mỗi ô mosaic.

**Cách tương tác:** Giống Rectangle. Người dùng nhấn chuột, kéo để tạo hình chữ nhật, thả chuột.

**Kết quả hình học:** Vùng hình chữ nhật được chia thành lưới ô vuông, mỗi ô mang một màu trung bình tính từ pixel gốc. Kết quả là hiệu ứng làm mờ mosaic.

**Ràng buộc:** Kích thước ô mosaic phải là số nguyên dương. Vùng Pixelate bị giới hạn trong biên ảnh.

**Preview:** Hình chữ nhật tạm với viền hoặc lớp phủ mờ cho biết vùng sẽ bị xử lý.

**Commit:** Khi thả chuột, thuật toán pixelate được áp dụng lên ảnh gốc trong vùng đã chọn. Thông tin vùng và kích thước ô được lưu vào Annotation.

### 2.9 Icon

Icon là công cụ chèn biểu tượng hoặc hình ảnh nhỏ vào vùng chụp. Trong MVP, công cụ này chỉ là placeholder để dành phím tắt `I`; việc chèn icon thực sự sẽ triển khai trong v1.x.

Dữ liệu trong MVP gồm vị trí gốc, kích thước placeholder, và một định danh icon tạm thời.

**Cách tương tác:** Người dùng nhấn `I` để chọn công cụ. Click vào capture region hiển thị placeholder hình chữ nhật nét đứt với chữ "ICON" bên trong. Không tạo annotation thực trong MVP.

**Kết quả hình học:** Hình chữ nhật placeholder 64×64 pixel tại vị trí click, màu theo `CurrentStyle.Color`.

**Ràng buộc:** Không có ràng buộc trong MVP.

**Preview:** Hình chữ nhật nét đứt và chữ "ICON" hiển thị tại vị trí con trỏ khi click.

**Commit:** Không commit trong MVP. Annotation `Tool.Icon` chỉ được tạo khi nào có thư viện icon trong v1.x.

---

## 3. Annotation record

`Annotation` là một đơn vị dữ liệu lưu trữ một lần vẽ hoặc một đối tượng chú thích đã được commit. Nó đóng vai trò như một dòng thời gian ngắn trong lịch sử thao tác của người dùng.

### 3.1 Thành phần

Mỗi Annotation bao gồm:

- Một định danh duy nhất, dùng để phân biệt với các Annotation khác khi cần tham chiếu hoặc xóa cụ thể.
- Loại tool và các tham số riêng của tool đó.
- Màu sắc được sử dụng.
- Độ dày nét hoặc cỡ chữ tùy theo loại tool.
- Thời điểm tạo, dùng để xác định thứ tự hiển thị khi có nhiều Annotation chồng nhau.

### 3.2 Cách sinh ra

Annotation được sinh ra từ Preview khi người dùng hoàn tất một thao tác. Quá trình này gồm các bước:

- Lấy dữ liệu từ Preview.
- Gán một định danh mới.
- Gán thời điểm tạo là thời điểm hiện tại.
- Sao chép các thuộc tính đi kèm như màu, độ dày, cỡ chữ.

Ví dụ: khi người dùng thả chuột sau khi vẽ một đường Line, Preview Line được chuyển thành Annotation Line với đầy đủ định danh và thời điểm.

### 3.3 Cách lưu trữ

Annotation sau khi được tạo sẽ được thêm vào một danh sách bất biến trong snapshot của overlay. Danh sách này đại diện cho toàn bộ chú thích hiện có trên ảnh chụp.

Khi người dùng vẽ thêm một Annotation, hệ thống tạo ra một danh sách mới bằng cách nối Annotation mới vào cuối danh sách cũ. Danh sách cũ không bị thay đổi.

### 3.4 Cách hiển thị

Khi render, hệ thống duyệt danh sách Annotation theo thứ tự thời gian tạo. Mỗi Annotation được vẽ lên ảnh theo loại tool và tham số của nó. Annotation tạo sau sẽ nằm trên Annotation tạo trước, trừ khi có cơ chế sắp xếp lớp khác được áp dụng trong v1.x.

### 3.5 Cách dùng trong undo và redo

Mỗi lần commit một Annotation, hệ thống tạo ra một snapshot mới chứa danh sách Annotation cập nhật. Snapshot này được đẩy vào lịch sử.

Khi undo, hệ thống lấy snapshot trước đó, trong đó danh sách Annotation chưa chứa Annotation vừa thêm. Khi redo, hệ thống lấy snapshot sau đó, trong đó Annotation đã có trở lại. Vì dữ liệu là bất biến, việc chuyển đổi giữa các snapshot không gây ra tác dụng phụ.

### 3.6 Cách xóa

Để xóa một Annotation, hệ thống tạo ra một danh sách mới bao gồm tất cả Annotation cũ trừ Annotation cần xóa. Danh sách mới này trở thành snapshot hiện tại. Khả năng xóa từng Annotation thuộc phạm vi v1.x, không bắt buộc trong MVP.

### 3.7 Ví dụ luồng hoạt động

Giả sử người dùng thực hiện các bước sau:

1. Vẽ một đường Line từ góc trái sang góc phải.
2. Vẽ thêm một mũi tên Arrow chỉ xuống dưới.
3. Hoàn tác bằng phím tắt.

Luồng dữ liệu:

- Sau bước 1: danh sách Annotation chứa một Line.
- Sau bước 2: danh sách Annotation chứa Line và Arrow.
- Sau bước 3: hệ thống quay lại snapshot trước đó, danh sách Annotation chỉ còn Line.

Không có Annotation nào bị sửa đổi trực tiếp trong quá trình này. Chỉ có các danh sách snapshot thay đổi.

---

## 4. Preview

Preview là dữ liệu tạm thời trong lúc người dùng đang tương tác. Nó chưa được lưu vào danh sách Annotation chính thức.

### 4.1 Cách sinh ra

Preview được tạo ra khi người dùng bắt đầu một thao tác vẽ. Ví dụ, khi nhấn chuột để vẽ Line, hệ thống tạo một Preview Line với điểm bắt đầu là vị trí nhấn chuột.

### 4.2 Cách cập nhật

Khi người dùng di chuyển chuột, Preview được thay thế bằng một Preview mới có tham số cập nhật. Ví dụ, điểm kết thúc của Preview Line thay đổi theo vị trí chuột hiện tại.

### 4.3 Cách kết thúc

Khi người dùng thả chuột hoặc nhấn Enter, Preview được chuyển thành Annotation. Nếu người dùng nhấn Escape hoặc chuyển công cụ, Preview bị hủy và không tạo Annotation.

---

## 5. Commit

Commit là hành động chuyển Preview thành Annotation và đưa vào lịch sử.

### 5.1 Điều kiện commit

- Thao tác vẽ phải hoàn tất.
- Dữ liệu Preview phải hợp lệ. Ví dụ, Line phải có hai điểm khác nhau, Text không được rỗng.

### 5.2 Quy trình commit

- Tạo Annotation mới từ Preview.
- Thêm Annotation vào danh sách Annotation hiện tại để tạo danh sách mới.
- Tạo snapshot mới từ danh sách Annotation mới.
- Đẩy snapshot vào HistoryStack.

### 5.3 Tác động đến HistoryStack

Mỗi lần commit tạo ra một snapshot mới. Snapshot này trở thành trạng thái hiện tại của overlay. HistoryStack lưu toàn bộ các snapshot theo thứ tự thời gian.

### 5.4 Ví dụ

Khi người dùng vẽ Line:

- Nhấn chuột tại A: tạo Preview Line(A, A).
- Di chuyển chuột đến B: thay Preview bằng Line(A, B).
- Thả chuột: commit, tạo Annotation Line(A, B), thêm vào danh sách, đẩy snapshot mới.

---

## 6. Tính bất biến

Tất cả các đối tượng Tool, Preview, và Annotation đều được thiết kế theo hướng bất biến. Khi cần thay đổi, hệ thống tạo ra một bản sao mới thay vì sửa đổi trực tiếp đối tượng cũ. Điều này đảm bảo việc undo và redo hoạt động an toàn, không có tác dụng phụ không mong muốn.

---

## 6. Câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Mỗi tool có kiểu dữ liệu riêng hay gộp chung thành một cấu trúc duy nhất? | Ảnh hưởng cách viết renderer và cách mở rộng tool mới |
| Có nên lưu thêm thông tin về người dùng đã chọn tool nào không? | Ảnh hưởng khả năng khôi phục trạng thái công cụ sau undo |
| Có lưu trạng thái selected/hovered của Annotation không? | Ảnh hưởng chế độ chỉnh sửa chú thích cũ trong v1.x |

---

## 7. Kết nối với các phần khác

- Geometry cung cấp Point, Rect, và các phép đo khoảng cách.
- Selection cung cấp tọa độ tương đối và vùng crop.
- Config cung cấp giá trị mặc định cho màu, độ dày, cỡ chữ, và các thuộc tính khác.
- Rendering.Skia sẽ vẽ từng loại Tool dựa trên tham số của nó.
- History nhận snapshot khi Annotation được commit.

---

*ToolModel là nền tảng của toàn bộ nhóm Annotation. Sau khi chốt, các công cụ cụ thể sẽ được thiết kế chi tiết trong các tài liệu riêng.*
