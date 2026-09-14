# UI/UX Mock Penpot — Toolbar Layout

> Tài liệu chi tiết bố cục, nhóm công cụ, vị trí, icon và hành vi responsive của thanh công cụ nổi trong màn hình chụp.

---

## 1. Mục đích toolbar

Toolbar nổi cung cấp nhanh các công cụ annotation và hành động sau khi vùng chọn được tạo. Toolbar luôn gắn liền với vùng chọn, không phải một thanh cố định toàn màn hình.

---

## 2. Cấu trúc nhóm công cụ

Toolbar chia thành 2 nhóm chính, phân cách bằng một đường separator dọc.

### 2.1. Nhóm 1: Annotation Tools

Các công cụ dùng để vẽ hoặc chú thích trên vùng chọn. Thứ tự từ trái sang phải:

| Thứ tự | Công cụ | Phím tắt | Mô tả icon |
| :--- | :--- | :--- | :--- |
| 1 | Pencil | `P` | Icon bút chì hoặc đường cong tự do. |
| 2 | Line | `L` | Đường thẳng nghiêng 45°. |
| 3 | Arrow | `A` | Đường thẳng có đầu mũi tên. |
| 4 | Rectangle | `R` | Hình chữ nhật. |
| 5 | Circle | `C` | Hình tròn / elip. |
| 6 | Marker | `M` | Bút highlight, có thể thể hiện bằng nét ngang bán trong suốt. |
| 7 | Text | `T` | Chữ `T`. |
| 8 | Pixelate | `B` | Lưới ô vuông hoặc icon làm mờ. |

### 2.2. Nhóm 2: Action Tools

Các hành động điều khiển vùng chọn, undo/redo, xuất dữ liệu. Thứ tự từ trái sang phải:

| Thứ tự | Công cụ | Phím tắt | Mô tả icon |
| :--- | :--- | :--- | :--- |
| 1 | Undo | `Ctrl+Z` | Mũi tên cong quay ngược trái. |
| 2 | Redo | `Ctrl+Shift+Z` / `Ctrl+Y` | Mũi tên cong quay phải. |
| 3 | Copy | `Ctrl+C` | Hai tờ giấy chồng nhau. |
| 4 | Save | `Ctrl+S` | Biểu tượng đĩa mềm hoặc mũi tên xuống thùng. |
| 5 | Cancel | `Esc` | Dấu `X`. |

---

## 3. Bố cục chi tiết

### 3.1. Container tổng thể

- Chiều cao: `40px`.
- Chiều rộng: tự động theo số lượng nút.
- Padding ngang: `8px`; padding dọc: `4px`.
- Gồm 2 nhóm nút và 1 separator.

### 3.2. Khoảng cách bên trong

```text
PaddingX = 8px
PaddingY = 4px
InnerGap = 4px
GroupGap = 8px
SeparatorWidth = 1px
```

Công thức tổng chiều rộng với `n` nút annotation và `m` nút action:

```text
ToolbarWidth = 2*PaddingX
             + n*ToolButton.Width + (n-1)*InnerGap
             + 2*GroupGap + SeparatorWidth
             + m*ToolButton.Width + (m-1)*InnerGap
```

Với `n = 8`, `m = 5`:

```text
ToolbarWidth = 16 + 8*32 + 7*4 + 16 + 1 + 5*32 + 4*4 = 624px
```

### 3.3. Thứ tự z-index

Trong Penpot, layer `BottomToolbar` đặt trên cùng của board trạng thái đã chọn.

---

## 4. Vị trí toolbar so với vùng chọn

Toolbar luôn đặt ngoài vùng chọn, cách ít nhất `8px`. Vị trí mặc định là dưới đáy vùng chọn, căn giữa theo chiều ngang. Nếu mép dưới không đủ chỗ, toolbar tự động chuyển lên trên. Nếu cả trên và dưới đều không đủ chỗ (ví dụ vùng chọn quá rộng so với chiều cao màn hình hoặc toolbar ngang quá dài so với vùng chọn), toolbar chuyển sang dạng dọc bên phải vùng chọn, căn giữa theo chiều dọc. Nếu phải không đủ chỗ thì chuyển sang dọc bên trái.

Các trường hợp cụ thể:

### 4.1. Vị trí mặc định (BottomHorizontal)

Toolbar đặt dưới đáy vùng chọn, căn giữa:

```text
Toolbar.X = Selection.X + Selection.Width / 2 - Toolbar.Width / 2
Toolbar.Y = Selection.Bottom + 8px
```

### 4.2. Khi vùng chọn sát mép dưới (TopHorizontal)

Nếu toolbar vượt quá chiều cao màn hình:

```text
if Toolbar.Y + Toolbar.Height > ScreenHeight then
    Toolbar.Y = Selection.Top - Toolbar.Height - 8px
```

### 4.3. Khi vùng chọn hẹp hơn toolbar ngang hoặc không đủ chỗ trên/dưới (RightVertical)

Toolbar chuyển sang dạng dọc bên phải vùng chọn:

```text
Toolbar.X = Selection.Right + 8px
Toolbar.Y = Selection.Y + Selection.Height / 2 - Toolbar.Height / 2
```

### 4.4. Khi phải không đủ chỗ (LeftVertical)

Toolbar chuyển sang dọc bên trái vùng chọn:

```text
Toolbar.X = Selection.Left - Toolbar.Width - 8px
Toolbar.Y = Selection.Y + Selection.Height / 2 - Toolbar.Height / 2
```

### 4.5. Xử lý khi vùng chọn ở biên phải màn hình

Nếu toolbar dọc phải bị tràn:

```text
if Toolbar.X + Toolbar.Width > ScreenWidth then
    Toolbar.X = ScreenWidth - Toolbar.Width - 8px
```

### 4.6. Toolbar không bao giờ che vùng chọn

Toolbar luôn đặt ngoài vùng chọn, cách ít nhất `8px`. Trong trường hợp vùng chọn chiếm gần hết màn hình, toolbar có thể bị đẩy ra một bên nhưng vẫn không che phủ vùng chọn.

---

## 5. Trạng thái nút trên toolbar

### 5.1. Default

- Nền trong suốt.
- Icon trắng, opacity `0.85`.
- Áp dụng cho các công cụ chưa được chọn.

### 5.2. Hover

- Nền trắng `12%` opacity.
- Icon trắng đậm, opacity `1.00`.
- Xuất hiện khi chuột nằm trên nút.

### 5.3. Active

- Nền accent `#7C3AED` đục.
- Icon trắng đậm.
- Áp dụng cho công cụ annotation đang được kích hoạt. Trong nhóm Action, nút `Cancel` không dùng variant Active.

### 5.4. Disabled

- Nền trong suốt.
- Icon trắng `35%` opacity.
- Áp dụng cho `Undo` khi không có lịch sử hoàn tác, hoặc `Redo` khi không có lịch sử làm lại.

---

## 6. Iconography

### 6.1. Quy tắc chung

- Tất cả icon dạng vector `18 × 18px`.
- Stroke đều `1.5px` đến `2px`.
- Không dùng fill trừ khi công cụ yêu cầu (Marker, Pixelate).
- Màu icon luôn là trắng để đồng nhất trên nền tối.

### 6.2. Mô tả từng icon

| Công cụ | Mô tả hình dạng |
| :--- | :--- |
| Pencil | Đường cong tự do mỏng, gợi ý nét vẽ tay. |
| Line | Đường thẳng nghiêng 45° từ góc dưới-trái lên góc trên-phải. |
| Arrow | Đường thẳng nghiêng với đầu mũi tên tam giác ở cuối. |
| Rectangle | Hình chữ nhật rỗng, góc vuông hoặc bo góc nhẹ. |
| Circle | Hình tròn / elip rỗng. |
| Marker | Hình chữ nhật ngang ngắn, opacity giảm để gợi ý bán trong suốt. |
| Text | Chữ cái `T` in hoa. |
| Pixelate | Lưới `3 × 3` ô vuông nhỏ. |
| Undo | Mũi tên cong quay ngược sang trái. |
| Redo | Mũi tên cong quay sang phải. |
| Copy | Hai hình chữ nhật chồng lệch nhau. |
| Save | Hình đĩa mềm cổ điển hoặc mũi tên xuống. |
| Cancel | Dấu `X` hoặc dấu gạch chéo. |

---

## 7. Hành vi responsive tổng quát

### 7.1. Khi màn hình quá hẹp

Nếu tổng chiều rộng toolbar lớn hơn chiều rộng màn hình trừ hai lề `8px`:

```text
if ToolbarWidth > ScreenWidth - 16px then
    Giảm InnerGap xuống 2px
    Giảm ToolButton.Width xuống 28px
    Giảm Icon size xuống 16px
```

Đây là chế độ compact, chỉ kích hoạt khi cần.

### 7.2. Khi vùng chọn nằm ở biên trên

Nếu `Selection.Top - Toolbar.Height - 8px < 0` và cũng không đủ chỗ ở dưới, toolbar đặt sát biên dưới màn hình với `Y = ScreenHeight - Toolbar.Height - 8px`, và `X` clamp theo biên.

### 7.3. Toolbar không bao giờ che vùng chọn

Toolbar luôn đặt ngoài vùng chọn, cách ít nhất `8px`. Trong trường hợp vùng chọn chiếm gần hết màn hình, toolbar có thể bị đẩy ra một bên nhưng vẫn không che phủ vùng chọn.

---

## 8. Phím tắt trực tiếp

Người dùng có thể chuyển công cụ bằng phím tắt mà không cần hover toolbar. Toolbar chỉ phản ánh trạng thái active của phím tắt:

| Phím | Công cụ |
| :--- | :--- |
| `P` | Pencil |
| `L` | Line |
| `A` | Arrow |
| `R` | Rectangle |
| `C` | Circle |
| `M` | Marker |
| `T` | Text |
| `B` | Pixelate |
| `Esc` | Cancel / về Selection tool |
| `Ctrl+Z` | Undo |
| `Ctrl+Shift+Z` / `Ctrl+Y` | Redo |
| `Ctrl+C` | Copy |
| `Ctrl+S` | Save |

---

## 9. Notes cho Penpot

- Toolbar nên được tạo thành Master Component để dùng lại ở Board 3, Board 4, Board 5.
- Mỗi `ToolButton` trong toolbar là instance từ Master Component `ToolButton`.
- Nên tạo các variant của toolbar: `Default`, `WithActiveTool`, `Compact`.
- Separator nên là một layer riêng để dễ điều chỉnh khoảng cách nhóm.
- Không hardcode màu trong toolbar; tất cả binding về token trong `12_01_DesignTokens.md`.
- Nên có một board phụ `Toolbar_Positioning_Grid` để minh họa các trường hợp đặt toolbar ở 4 góc màn hình.
