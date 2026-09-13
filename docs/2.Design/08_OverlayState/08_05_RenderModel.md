# RenderModel — Thiết kế chi tiết

> Tài liệu này định nghĩa dữ liệu đầu ra từ OverlayState để UI layer vẽ lại màn hình overlay.

---

## 1. Mục đích

`RenderModel` là bản sao chỉ đọc của toàn bộ trạng thái cần thiết để renderer vẽ scene. Nó được tạo ra từ OverlayState sau mỗi sự kiện và được gửi đến UI layer. UI layer sử dụng RenderModel để vẽ screenshot, lớp phủ tối, vùng chọn, chú thích, toolbar, và con trỏ.

Mục tiêu là đảm bảo renderer không cần biết logic state machine. Nó chỉ cần vẽ theo dữ liệu được cung cấp.

---

## 2. Cấu trúc tổng quan

RenderModel bao gồm các thành phần sau:

- **Screenshot**: hình ảnh gốc đã chụp.
- **Dimming**: thông tin về lớp phủ tối mờ ngoài vùng chọn.
- **Selection**: vùng chọn hiện tại, viền, điểm neo, badge kích thước.
- **Annotations**: danh sách chú thích đã commit, lấy từ snapshot hiện tại.
- **Preview**: chú thích tạm thời đang được vẽ, nếu có.
- **Toolbar**: trạng thái hiển thị và vị trí của toolbar.
- **Cursor**: hình dạng con trỏ mong muốn.
- **TextInput**: thông tin về control nhập liệu text tạm, nếu có.

---

## 3. Screenshot

Screenshot là hình ảnh toàn bộ màn hình ảo đã được chụp trước khi overlay mở. Nó là nền của toàn bộ scene.

Thuộc tính:

- Kích thước: chiều rộng và chiều cao của toàn màn hình ảo.
- Vị trí: gốc tọa độ thường là (0, 0) trong hệ tọa độ màn hình ảo.
- Pixel data: nội dung hình ảnh thô hoặc tham chiếu đến texture đã upload.

Trong RenderModel, screenshot không thay đổi trong suốt phiên chụp. Nó chỉ được đọc và vẽ.

---

## 4. Dimming

Dimming là lớp phủ tối mờ phía ngoài vùng chọn, giúp người dùng tập trung vào vùng sẽ được xuất.

Thuộc tính:

- Màu: thường là đen với độ trong suốt khoảng 40% đến 60%.
- Hình dạng: toàn bộ màn hình ảo trừ đi vùng chọn.
- Hiển thị: luôn hiển thị trong `Idle`, `Selecting`, `Selected`, `Annotating`, `Exporting`.

Trong `Idle` khi chưa có vùng chọn, dimming phủ toàn bộ màn hình.

Trong `Selecting`, dimming được cắt theo vùng chọn tạm thời.

Trong `Selected` và `Annotating`, dimming được cắt theo vùng chọn cuối cùng.

---

## 5. Selection

Selection trong RenderModel mô tả vùng chọn và các yếu tố đi kèm.

### 5.1 Vùng chọn

Thuộc tính:

- Hình chữ nhật: vị trí và kích thước.
- Trạng thái: đang kéo, đang co giãn, hoặc đã cố định.
- Viền: màu trắng, đường nét đứt hoặc liền.

### 5.2 Điểm neo

Tám điểm neo hiển thị khi vùng chọn đã cố định và ở trạng thái `Selected` hoặc `Annotating`.

Thuộc tính:

- Vị trí: bốn góc và bốn trung điểm cạnh của vùng chọn.
- Kích thước: thường 8x8 hoặc 10x10 pixel.
- Màu: màu nổi bật, ví dụ cyan, có viền đen để dễ nhìn trên mọi nền.
- Hiển thị: chỉ hiển thị khi vùng chọn đủ lớn.

### 5.3 Badge kích thước

Badge hiển thị kích thước vùng chọn, ví dụ `800 x 600`.

Thuộc tính:

- Vị trí: góc trên bên phải hoặc góc dưới bên phải của vùng chọn.
- Hiển thị: trong `Selecting`, `Selected`, `Annotating`.

---

## 6. Annotations

Annotations trong RenderModel là danh sách các chú thích đã commit, lấy từ `HistoryStack.Current.Annotations`.

Mỗi annotation bao gồm:

- Loại công cụ: Line, Arrow, Rectangle, Circle, Pencil, Marker, Text, Pixelate.
- Tham số hình học: điểm, đường, hình chữ nhật, danh sách điểm, vùng pixelate, văn bản.
- Style: màu, độ dày, alpha, cỡ chữ, font.

Renderer vẽ từng annotation theo thứ tự trong danh sách. Annotation sau chồng lên annotation trước.

---

## 7. Preview

Preview là annotation tạm thời đang được vẽ trong `Annotating`. Nó chưa được commit nên không nằm trong danh sách annotations đã commit.

Thuộc tính:

- Loại công cụ đang vẽ.
- Tham số hình học cập nhật theo chuyển động chuột.
- Style hiện tại.

Renderer vẽ preview sau cùng, chồng lên tất cả annotation đã commit, để người dùng thấy rõ nét đang vẽ.

Trong trường hợp công cụ Text, preview có thể là một TextBox tạm thời của UI framework thay vì vẽ trực tiếp bằng Skia.

---

## 8. Toolbar

Toolbar hiển thị khi có vùng chọn và ở trạng thái `Selected` hoặc `Annotating`.

Thuộc tính:

- Vị trí: thường nằm ngay sát mép dưới bên phải của vùng chọn.
- Trạng thái: visible hoặc hidden.
- Công cụ đang chọn: được đánh dấu active.
- Các nút hành động: Undo, Redo, Copy, Save, Cancel.
- Trạng thái enable/disable của từng nút: Undo/Redo chỉ enable khi `CanUndo`/`CanRedo` đúng.

Toolbar không nằm trong `HistoryStack` và không tạo snapshot khi thay đổi công cụ.

---

## 9. Cursor

Cursor trong RenderModel chỉ định hình dạng con trỏ mong muốn.

Các loại con trỏ:

- Crosshair: trong `Idle` và `Selecting`.
- Default: trong `Selected` khi không trên vùng chọn hay handle.
- Move: khi di chuột trong vùng chọn, sẵn sàng di chuyển.
- Resize NS/EW/NESW/NWSE: khi di chuột trên các điểm neo tương ứng.
- Tool-specific: khi đang ở `Annotating`, có thể là crosshair hoặc hình dạng tùy công cụ.

Lưu ý: UI framework cụ thể quyết định cách hiển thị con trỏ. RenderModel chỉ cung cấp gợi ý.

---

## 10. TextInput

TextInput chỉ tồn tại khi ở sub-state `EditingText`.

Thuộc tính:

- Vị trí đặt TextBox: tọa độ trong vùng chọn.
- Kích thước TextBox: đủ rộng để chứa văn bản đang nhập.
- Nội dung hiện tại: text người dùng đã nhập.
- Style: font, cỡ chữ, màu.
- Trạng thái focus: TextBox đang được focus.

UI framework chịu trách nhiệm tạo và quản lý TextBox thực tế. RenderModel chỉ mô tả vị trí và trạng thái để UI đặt control đúng chỗ.

---

## 11. Cách tạo RenderModel từ State

Mỗi khi state machine xử lý một sự kiện, nó tạo ra một RenderModel mới dựa trên trạng thái hiện tại. RenderModel là bất biến và không được state machine lưu trữ lại.

Các bước tạo RenderModel:

1. Lấy screenshot từ `CaptureResult`.
2. Tính vùng cần dimming dựa trên vùng chọn hiện tại hoặc toàn màn hình nếu chưa có vùng chọn.
3. Tạo đối tượng Selection từ vùng chọn và trạng thái sub-state.
4. Lấy danh sách annotations từ `HistoryStack.Current`.
5. Thêm preview nếu đang trong `Annotating`.
6. Tính vị trí và trạng thái toolbar.
7. Xác định cursor phù hợp.
8. Thêm TextInput nếu đang trong `EditingText`.

---

## 12. Cách UI dùng RenderModel

UI layer nhận RenderModel và thực hiện vẽ lại toàn bộ scene. Renderer không sửa đổi RenderModel.

Thứ tự vẽ khuyến nghị:

1. Vẽ screenshot toàn màn hình.
2. Vẽ dimming layer, cắt lỗ vùng chọn.
3. Vẽ vùng chọn rõ (ảnh gốc không bị dim trong vùng chọn).
4. Vẽ viền vùng chọn.
5. Vẽ tám điểm neo.
6. Vẽ badge kích thước.
7. Vẽ danh sách annotations đã commit.
8. Vẽ preview annotation.
9. Vẽ toolbar.
10. Đặt TextBox tạm nếu có TextInput.
11. Cập nhật con trỏ.

---

## 13. Phạm vi trong MVP

Trong MVP, RenderModel cần chứa đầy đủ:

- Screenshot.
- Dimming.
- Selection với viền, handle, badge.
- Annotations.
- Preview.
- Toolbar.
- Cursor.
- TextInput khi dùng công cụ Text.

Không cần trong MVP:

- Hiệu ứng animation.
- Layer blending phức tạp.
- Hiển thị tooltip trên toolbar.
- Preview độ phân giải cao khác với screenshot.

---

## 14. Kết nối với các file thiết kế khác

- **08_02_States.md**: mỗi trạng thái cho ra một RenderModel khác nhau.
- **08_03_Events.md**: sự kiện làm thay đổi trạng thái và do đó làm thay đổi RenderModel.
- **08_04_Transitions.md**: transitions làm thay đổi dữ liệu trong RenderModel.
- **08_06_Integration.md**: RenderModel là giao diện giữa state machine và UI layer.
- **05_03_HistoryStack.md**: annotations trong RenderModel lấy từ HistoryStack.
- **04_02_ToolModel.md**: preview và annotations trong RenderModel dựa trên tool model.

---

*RenderModel đã được định nghĩa. Tiếp theo, `08_06_Integration.md` sẽ mô tả cách OverlayState tích hợp với các phần khác.*
