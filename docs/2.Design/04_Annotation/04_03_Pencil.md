# Pencil — Thiết kế chi tiết

> Tài liệu này thiết kế công cụ Pencil, công cụ vẽ tự do cơ bản nhất trong F-Shot.

---

## 1. Mục đích

Pencil cho phép người dùng vẽ các nét tay tự do trên ảnh chụp. Khác với các công cụ hình học khác, Pencil không bị ràng buộc bởi hai điểm cố định. Người dùng nhấn chuột, di chuyển để tạo một chuỗi điểm, và thả chuột để kết thúc một nét vẽ.

Mục tiêu thiết kế:

- Cảm giác vẽ mượt mà, không bị giật cục.
- Đường nét nhận được phải gần với ý định của người dùng, không bị méo mó quá mức.
- Dữ liệu lưu trữ gọn nhẹ, phục vụ undo và rendering sau này.

---

## 2. Mô hình dữ liệu

Một nét Pencil được mô tả bằng một chuỗi các điểm trong không gian hai chiều, được ghi lại theo thứ tự thời gian từ lúc nhấn chuột đến lúc thả chuột.

### 2.1 Cách thu thập điểm

Quá trình thu thập điểm bắt đầu khi người dùng nhấn chuột. Từ thời điểm đó, mỗi khi chuột di chuyển, hệ thống ghi lại tọa độ mới. Quá trình kết thúc khi người dùng thả chuột.

Ví dụ, nếu người dùng nhấn chuột tại vị trí P0, sau đó di chuyển qua các vị trí P1, P2, P3, rồi thả chuột, thì chuỗi điểm thu được là P0, P1, P2, P3 theo đúng thứ tự thời gian.

### 2.2 Thuộc tính đi kèm

Mỗi điểm trong chuỗi chứa tọa độ x và tọa độ y. Ngoài chuỗi điểm, mỗi nét Pencil còn cần lưu các thuộc tính chung:

- Màu sắc của nét.
- Độ dày của nét.
- Độ trong suốt nếu có.

Các thuộc tính này được dùng chung cho toàn bộ nét, thay vì lưu riêng từng điểm. Điều này giúp giảm kích thước dữ liệu và đơn giản hóa việc render.

### 2.3 Cách chuyển thành Annotation

Khi nét Pencil hoàn thành, chuỗi điểm sau khi xử lý cùng với các thuộc tính kèm theo được đóng gói thành một Annotation. Annotation này được gán định danh duy nhất và thời điểm tạo, sau đó thêm vào danh sách chú thích của snapshot hiện tại.

---

## 3. Vấn đề của dữ liệu thô

Khi người dùng di chuyển chuột nhanh, chuỗi điểm thu được có thể bị thưa hoặc dày không đều. Khi di chuyển chậm, số điểm rất nhiều, gây dư thừa và làm đường nét bị gợn sóng nhỏ do run tay.

Do đó, dữ liệu thô từ chuột cần được xử lý trước khi lưu hoặc render. Quá trình này gọi là làm mịn.

---

## 4. Chiến lược làm mịn

Có nhiều cách làm mịn đường nét tự do. Trong phạm vi MVP, chúng ta chọn phương pháp làm mịn bằng đường cong bậc hai xuyên qua các điểm trung gian. Phương pháp này cân bằng giữa độ mượt và độ trung thực với đường vẽ gốc.

### 4.1 Nguyên tắc

Thay vì nối các điểm bằng đoạn thẳng, hệ thống sẽ tạo ra các đường cong liền mạch. Mỗi đường cong đi qua các điểm đã thu thập, nhưng không nhất thiết phải đi qua đúng từng điểm một cách cứng nhắc.

Ví dụ, với bốn điểm A, B, C, D thu được từ chuột, hệ thống có thể tạo thành các đoạn cong liên tục đi qua gần các điểm này, thay vì nối A-B, B-C, C-D bằng các đoạn thẳng gấp khúc.

### 4.2 Cách chọn điểm điều khiển

Với mỗi cặp điểm liên tiếp trong chuỗi, ta lấy điểm nằm giữa hai điểm đó làm điểm đích của một đoạn cong bậc hai. Điểm điều khiển của đường cong được chọn dựa trên điểm trước đó, giúp đảm bảo độ liền mạch giữa các đoạn.

Cụ thể, nếu có hai điểm liên tiếp là điểm trước và điểm sau, điểm giữa được tính bằng trung bình cộng của hai tọa độ. Điểm giữa này trở thành điểm kết thúc của đoạn cong bậc hai hiện tại và cũng là điểm bắt đầu của đoạn cong tiếp theo.

### 4.3 Cách tạo đường cong liền mạch

Mỗi đoạn cong bậc hai cần ba điểm: điểm bắt đầu, điểm điều khiển, và điểm kết thúc. Điểm bắt đầu và kết thúc là các điểm giữa của các cặp điểm liên tiếp trong chuỗi gốc. Điểm điều khiển là điểm gốc nằm giữa hai điểm giữa đó.

Ví dụ, với ba điểm gốc liên tiếp P0, P1, P2, ta có:

- Điểm bắt đầu của đoạn cong là điểm giữa của P0 và P1.
- Điểm điều khiển là P1.
- Điểm kết thúc là điểm giữa của P1 và P2.

Cách này tạo ra đường cong liền mạch qua P1 mà không cần đi qua đúng P0 hay P2.

### 4.4 Làm mịn theo cấp độ

Mức độ làm mịn có thể điều chỉnh được. Khi mức làm mịn cao, đường cong sẽ bỏ qua nhiều điểm nhỏ và tạo ra đường nét mềm hơn. Khi mức làm mịn thấp, đường nét sẽ sát với đường vẽ gốc hơn, giữ lại nhiều chi tiết nhỏ.

Mức độ làm mịn có thể được điều chỉnh qua cấu hình hoặc giá trị mặc định của công cụ. Ví dụ, nếu người dùng vẽ nhanh và muốn đường nét mềm, mức làm mịn có thể cao. Nếu người dùng cần độ chính xác cao, mức làm mịn nên thấp.

### 4.5 Cách render từ đường cong đã làm mịn

Khi render, hệ thống duyệt qua các đoạn cong bậc hai đã tạo. Mỗi đoạn cong được vẽ bằng một nét liên tục với độ dày và màu đã chọn. Các đoạn cong nối tiếp nhau tạo thành đường nét hoàn chỉnh từ điểm đầu đến điểm cuối.

---

## 5. Giảm số điểm dư thừa

Trước khi làm mịn, hệ thống có thể loại bỏ các điểm quá gần nhau. Quy tắc đơn giản là: nếu khoảng cách từ điểm mới đến điểm cuối cùng đã lưu nhỏ hơn một ngưỡng nhất định, thì điểm mới bị bỏ qua.

### 5.1 Cách xác định ngưỡng

Ngưỡng này nên phụ thuộc vào độ dày nét. Nét càng mảnh, ngưỡng càng nhỏ để giữ độ chính xác. Nét càng dày, ngưỡng có thể lớn hơn vì các điểm dư thừa ít ảnh hưởng đến hình dạng tổng thể.

Ví dụ, nếu độ dày nét là 2 pixel, ngưỡng có thể là 1 pixel. Nếu độ dày nét là 20 pixel, ngưỡng có thể là 5 pixel. Ngưỡng có thể được tính theo tỉ lệ cố định của độ dày nét, chẳng hạn một phần tư độ dày.

### 5.2 Cách loại bỏ điểm

Khi một điểm mới đến, hệ thống tính khoảng cách đến điểm cuối cùng đã được giữ lại. Nếu khoảng cách nhỏ hơn ngưỡng, điểm mới bị bỏ. Nếu lớn hơn hoặc bằng ngưỡng, điểm mới được thêm vào chuỗi điểm sẽ xử lý.

Quá trình này giúp giảm số lượng điểm cần làm mịn, từ đó tăng hiệu năng render và giảm rung tay.

### 5.3 Giới hạn số điểm tối đa

Trong trường hợp người dùng vẽ một nét rất dài, số điểm có thể rất lớn ngay cả sau khi loại bỏ dư thừa. Hệ thống có thể đặt một giới hạn tối đa số điểm cho mỗi nét. Khi vượt quá giới hạn, các điểm cũ hơn có thể được lấy mẫu hoặc làm mịn sớm hơn để giữ kích thước dữ liệu hợp lý.

Giới hạn này phải đủ lớn để không làm giảm chất lượng đường nét trong các trường hợp bình thường, nhưng đủ nhỏ để tránh lãng phí bộ nhớ.

---

## 6. Preview và commit

Trong khi người dùng vẽ, hệ thống chỉ hiển thị nét đang vẽ dưới dạng preview. Preview này chưa được lưu vào danh sách chú thích chính thức.

### 6.1 Cách tạo Preview

Khi người dùng nhấn chuột, hệ thống tạo một Preview Pencil với chuỗi điểm ban đầu chỉ chứa điểm nhấn chuột. Mỗi khi chuột di chuyển, điểm mới được thêm vào chuỗi của Preview. Preview được vẽ ngay lập tức lên canvas để phản hồi thao tác của người dùng.

### 6.2 Cách cập nhật Preview

Preview được thay thế liên tục trong lúc chuột di chuyển. Có thể có hai cách cập nhật:

- Cập nhật từng điểm: thêm điểm mới vào chuỗi hiện có và render lại toàn bộ nét. Cách này đơn giản nhưng có thể chậm nếu số điểm lớn.
- Giới hạn render: chỉ render phần nét mới được thêm, giữ phần cũ không đổi. Cách này phức tạp hơn nhưng hiệu năng tốt hơn.

Trong MVP, có thể chọn cách cập nhật từng điểm vì số điểm trong mỗi sự kiện chuột di chuyển không quá lớn. Sau này có thể tối ưu nếu cần.

### 6.3 Cách commit

Khi người dùng thả chuột:

- Chuỗi điểm thô từ Preview được làm mịn và giảm dư thừa.
- Một đối tượng Pencil hoàn chỉnh được tạo ra với màu, độ dày, và danh sách điểm đã xử lý.
- Đối tượng này được đóng gói thành Annotation.
- Annotation mới được thêm vào danh sách chú thích của snapshot hiện tại, tạo ra snapshot mới.
- Snapshot mới được đẩy vào lịch sử để hỗ trợ undo.

### 6.4 Cách hủy Preview

Nếu người dùng nhấn Escape hoặc chuyển sang công cụ khác trong lúc vẽ, Preview bị hủy. Không có Annotation nào được tạo và lịch sử không thay đổi.

---

## 7. Hiệu năng và tối ưu

### 7.1 Tần suất cập nhật

Số lần cập nhật Preview phụ thuộc vào tần suất sự kiện chuột di chuyển. Nếu cập nhật quá thường xuyên, hiệu năng có thể giảm. Nếu cập nhật quá thưa, đường nét có thể bị giật.

Trong MVP, hệ thống cập nhật Preview mỗi khi nhận được sự kiện chuột di chuyển. Điều này đảm bảo phản hồi nhanh trong hầu hết các trường hợp. Nếu sau này gặp vấn đề hiệu năng, có thể thêm cơ chế giới hạn tần suất cập nhật.

### 7.2 Cách giảm tải render

Thay vì vẽ toàn bộ nét Pencil từ đầu mỗi lần cập nhật, hệ thống có thể vẽ phần nét mới lên trên phần đã vẽ. Tuy nhiên, vì đường cong có thể thay đổi khi thêm điểm mới, cách này chỉ hiệu quả nếu đường cong được xấp xỉ bằng đoạn thẳng ngắn hoặc nếu làm mịn không ảnh hưởng đến phần đã vẽ.

Trong MVP, có thể chấp nhận render lại toàn bộ Preview nếu hiệu năng vẫn đạt yêu cầu. Yêu cầu hiệu năng tối thiểu là 60 FPS khi kéo chuột đã được xác minh trong Phase 0.

### 7.3 Kích thước dữ liệu

Mỗi nét Pencil lưu một chuỗi điểm. Kích thước dữ liệu tỉ lệ với số điểm. Sau khi giảm dư thừa và làm mịn, số điểm thường giảm đáng kể so với số điểm thô từ chuột. Việc đặt giới hạn số điểm tối đa giúp tránh các nét vẽ quá dài gây tiêu tốn bộ nhớ.

---

## 8. Các câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Mức làm mịn mặc định là bao nhiêu? | Ảnh hưởng cảm giác vẽ tay tự do |
| Có cho phép người dùng điều chỉnh mức làm mịn không? | Cần thêm cấu hình công cụ |
| Ngưỡng bỏ điểm dư thừa tính theo pixel hay theo tỉ lệ độ dày nét? | Ảnh hưởng chất lượng đường nét ở các độ phân giải màn hình khác nhau |
| Có cần lưu cả chuỗi điểm thô để chỉnh sửa sau không? | Ảnh hưởng kích thước dữ liệu và khả năng sửa chú thích cũ |

---

## 9. Kết nối với các phần khác

- Geometry cung cấp kiểu Point và các phép tính khoảng cách.
- Rendering.Skia sẽ vẽ đường cong dựa trên danh sách điểm đã xử lý.
- History đảm bảo mỗi nét Pencil hoàn thành đều tạo ra một snapshot mới.
- Config cung cấp giá trị mặc định cho màu, độ dày, và mức làm mịn.

---

*Sau khi chốt Pencil, chúng ta chuyển sang Line và Arrow — các công cụ hình học đơn giản hơn.*
