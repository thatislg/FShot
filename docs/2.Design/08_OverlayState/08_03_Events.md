# Events — Thiết kế chi tiết

> Tài liệu này định nghĩa các sự kiện đầu vào mà OverlayState nhận, cùng với thông tin đi kèm và ý nghĩa của từng sự kiện.

---

## 1. Mục đích

OverlayState là một máy trạng thái thuần khiết. Nó không tự đọc chuột hay bàn phím mà chỉ nhận các sự kiện đã được UI hoặc platform đóng gói sẵn. Mỗi sự kiện mang theo đủ thông tin để state machine quyết định trạng thái kế tiếp.

Mục tiêu là tách biệt hoàn toàn giữa việc thu thập input phần cứng và việc xử lý input theo logic nghiệp vụ.

---

## 2. Phân loại sự kiện

Các sự kiện được chia thành bốn nhóm chính:

- Pointer events: sự kiện chuột hoặc cảm ứng.
- Keyboard events: sự kiện bàn phím.
- Tool commands: lệnh chuyển công cụ hoặc thực hiện hành động.
- Lifecycle commands: lệnh điều khiển vòng đời overlay.

---

## 3. Pointer events

### 3.1 PointerPressed

Sự kiện này xảy ra khi người dùng nhấn nút chuột trái xuống.

Thông tin đi kèm:

- Tọa độ con trỏ tại thời điểm nhấn, tính theo hệ tọa độ của toàn màn hình ảo.
- Trạng thái modifier keys nếu có: Shift, Ctrl, Alt.

Ý nghĩa:

- Trong `Idle`, sự kiện này khởi động việc tạo vùng chọn, chuyển sang `Selecting`.
- Trong `Selected`, sự kiện này có thể khởi động di chuyển vùng chọn, co giãn vùng chọn, hoặc bắt đầu vẽ annotation tùy vị trí nhấn.
- Trong `Annotating`, sự kiện này bắt đầu preview của annotation mới hoặc xác nhận vị trí đặt text.

### 3.2 PointerMoved

Sự kiện này xảy ra khi con trỏ di chuyển, kể cả khi không có nút chuột nào được nhấn.

Thông tin đi kèm:

- Tọa độ con trỏ hiện tại.
- Trạng thái nút chuột có đang được giữ hay không.
- Trạng thái modifier keys.

Ý nghĩa:

- Trong `Selecting`, sự kiện này cập nhật hình chữ nhật vùng chọn tạm thời.
- Trong sub-state `MovingSelection`, sự kiện này cập nhật vị trí mới của vùng chọn.
- Trong sub-state `ResizingSelection`, sự kiện này cập nhật kích thước mới của vùng chọn.
- Trong `Annotating`, sự kiện này cập nhật preview của annotation đang vẽ.
- Trong các trạng thái khác, sự kiện này chỉ dùng để thay đổi hình dạng con trỏ.

### 3.3 PointerReleased

Sự kiện này xảy ra khi người dùng thả nút chuột trái.

Thông tin đi kèm:

- Tọa độ con trỏ tại thời điểm thả.
- Modifier keys.

Ý nghĩa:

- Trong `Selecting`, sự kiện này hoàn tất việc tạo vùng chọn. Nếu vùng đủ lớn thì chuyển sang `Selected`, ngược lại có thể quay về `Idle`.
- Trong sub-state `MovingSelection` hoặc `ResizingSelection`, sự kiện này cố định vùng chọn mới và quay về sub-state `Standby` của `Selected`.
- Trong `Annotating`, sự kiện này hoàn tất preview và chuyển preview thành annotation commit. Sau đó gọi `HistoryStack.Push` và quay về `Selected`.

### 3.4 PointerDoubleTapped

Sự kiện này xảy ra khi người dùng nhấn đúp chuột.

Ý nghĩa:

- Trong `Selected`, sự kiện này có thể kích hoạt xuất ảnh nhanh, ví dụ copy vào clipboard.
- Trong các trạng thái khác, sự kiện này có thể được bỏ qua.

### 3.5 PointerWheelChanged

Sự kiện này xảy ra khi người dùng lăn bánh xe chuột.

Ý nghĩa:

- Trong `Selected` hoặc `Annotating`, sự kiện này có thể điều chỉnh độ dày nét vẽ.
- Trong v1.x, có thể dùng để zoom vùng chọn.

---

## 4. Keyboard events

### 4.1 KeyDown

Sự kiện này xảy ra khi một phím được nhấn xuống.

Thông tin đi kèm:

- Mã phím.
- Trạng thái modifier keys.
- Có phải phím đang được giữ lặp lại hay không.

Ý nghĩa:

- Phím mũi tên di chuyển vùng chọn 1 pixel hoặc co giãn vùng chọn khi kèm Shift.
- Phím Esc hủy thao tác hiện tại hoặc đóng overlay.
- Phím Enter xác nhận xuất ảnh hoặc commit text.
- Phím Backspace hoặc Delete xóa annotation đang được chọn trong v1.x, trong MVP có thể chưa hỗ trợ.
- Các phím tắt chuyển công cụ annotation (`P`, `L`, `A`, `R`, `C`, `M`, `T`, `B`, `S`) thường được UI xử lý trước bằng `e.Key` (phím vật lý) để tránh bị bộ gõ tiếng Việt chặn, sau đó gửi `SelectTool tool` vào state machine. Xem thêm `11_05_InputHandling.md`, mục 3.2.

### 4.2 KeyUp

Sự kiện này xảy ra khi một phím được thả.

Ý nghĩa:

- Dùng để theo dõi việc thả phím modifier, ví dụ thả Shift để kết thúc co giãn đối xứng.
- Ít được dùng hơn KeyDown trong MVP.

---

## 5. Tool commands

Các lệnh này đại diện cho việc người dùng chọn công cụ vẽ hoặc thay đổi style. Chúng có thể đến từ toolbar click, phím tắt, hoặc bất kỳ nguồn nào khác.

### 5.1 SelectTool

Lệnh chuyển sang một công cụ annotation cụ thể.

Thông tin đi kèm:

- Loại công cụ: Pencil, Line, Arrow, Rectangle, Circle, Marker, Text, Pixelate.

Ý nghĩa:

- Trong `Selected`, lệnh này thay đổi `CurrentTool`.
- Trong `Annotating` khi chưa có preview, lệnh này cũng thay đổi công cụ.
- Nếu đang có preview, lệnh này có thể hủy preview hiện tại hoặc bị bỏ qua tùy quyết định UX.

### 5.2 SetColor

Lệnh thay đổi màu sắc hiện tại.

Ý nghĩa:

- Thay đổi `CurrentStyle.Color`.
- Không tạo snapshot vì chưa thay đổi annotation đã commit.

### 5.3 SetStrokeWidth

Lệnh thay đổi độ dày nét vẽ.

Ý nghĩa:

- Thay đổi `CurrentStyle.StrokeWidth`.
- Không tạo snapshot.

### 5.4 SetFontSize

Lệnh thay đổi cỡ chữ.

Ý nghĩa:

- Thay đổi `CurrentStyle.FontSize`.
- Không tạo snapshot.

---

## 6. Action commands

Các lệnh này đại diện cho các hành động người dùng thực hiện thông qua phím tắt hoặc toolbar.

### 6.1 Undo

Yêu cầu hoàn tác một bước.

Ý nghĩa:

- Gọi `HistoryStack.Undo`.
- Cập nhật danh sách annotation hiện tại theo snapshot mới.
- Xóa redo stack nếu sau đó có thao tác mới.

### 6.2 Redo

Yêu cầu làm lại một bước.

Ý nghĩa:

- Gọi `HistoryStack.Redo`.
- Khôi phục annotation từ redo stack.

### 6.3 Copy

Yêu cầu copy ảnh vùng chọn vào clipboard.

Ý nghĩa:

- Chuyển sang `Exporting` với target là clipboard.
- Có thể đóng overlay sau khi copy thành công.

### 6.4 Save

Yêu cầu lưu ảnh vùng chọn xuống file.

Ý nghĩa:

- Chuyển sang `Exporting` với target là file.
- Nếu chưa có đường dẫn cấu hình, UI hiển thị hộp thoại Save As.

### 6.5 Accept

Xác nhận và đóng overlay.

Ý nghĩa:

- Thường tương đương với Save hoặc Copy tùy cấu hình mặc định.
- Trong `Selected`, xuất ảnh và đóng overlay.

### 6.6 Cancel

Hủy và đóng overlay.

Ý nghĩa:

- Đóng overlay không xuất gì.
- Nếu đang trong `Annotating` với preview, hủy preview trước.

---

## 7. Lifecycle commands

### 7.1 OpenOverlay

Lệnh khởi tạo overlay.

Thông tin đi kèm:

- CaptureResult: hình ảnh đã chụp.
- ConfigSnapshot: cấu hình hiện tại.
- CaptureMode: chế độ chụp.

Ý nghĩa:

- Tạo trạng thái `Idle` ban đầu.
- Khởi tạo `HistoryStack` với snapshot rỗng.

### 7.2 CloseOverlay

Lệnh đóng overlay.

Ý nghĩa:

- Kết thúc phiên chụp.
- UI thực hiện đóng cửa sổ.

### 7.3 ExportCompleted

Lệnh báo hiệu việc xuất ảnh đã hoàn tất.

Thông tin đi kèm:

- Kết quả thành công hoặc thất bại.
- Đường dẫn file nếu có.

Ý nghĩa:

- Từ `Exporting`, nếu thành công thì chuyển sang `CloseOverlay` hoặc `Selected`.
- Nếu thất bại thì quay về `Selected`.

---

## 8. Cách UI gửi events

UI layer không gửi trực tiếp raw hardware events vào state machine. Thay vào đó, UI đóng gói raw events thành các sự kiện có ngữ nghĩa rõ ràng.

Ví dụ:

- UI nhận `PointerPressed` từ Avalonia, kiểm tra xem nhấn vào vùng nào, sau đó gửi `PointerPressed` kèm tọa độ vào state machine.
- UI nhận `KeyDown`, kiểm tra tổ hợp phím, sau đó gửi `Undo`, `Redo`, `Copy`, `Save`, hoặc `Cancel` tùy trường hợp.

Điều này giúp state machine không phụ thuộc vào framework UI cụ thể.

---

## 9. Phạm vi trong MVP

Trong MVP, cần hỗ trợ đầy đủ:

- PointerPressed, PointerMoved, PointerReleased.
- KeyDown cho Esc, Enter, mũi tên, Ctrl+Z, Ctrl+Y, Ctrl+C, Ctrl+S.
- SelectTool cho tất cả công cụ annotation.
- Undo, Redo, Copy, Save, Cancel.
- OpenOverlay, CloseOverlay, ExportCompleted.

Không cần trong MVP:

- PointerDoubleTapped.
- PointerWheelChanged.
- KeyUp.
- SetFontSize nếu text chỉ dùng cỡ chữ mặc định.

---

## 10. Kết nối với các file thiết kế khác

- **08_02_States.md**: các trạng thái nhận sự kiện.
- **08_04_Transitions.md**: các sự kiện kích hoạt chuyển trạng thái.
- **08_05_RenderModel.md**: một số sự kiện làm thay đổi RenderModel.
- **08_06_Integration.md**: cách UI gửi events và nhận commands.
- **04_02_ToolModel.md**: SelectTool liên quan đến các công cụ annotation.
- **05_03_HistoryStack.md**: Undo, Redo liên quan đến HistoryStack.

---

*Các sự kiện đã được định nghĩa. Tiếp theo, `08_04_Transitions.md` sẽ mô tả luồng chuyển trạng thái khi nhận từng sự kiện.*
