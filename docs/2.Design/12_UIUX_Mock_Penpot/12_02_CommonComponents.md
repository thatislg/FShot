# UI/UX Mock Penpot — Common Components

> Mô tả các Master Component dùng chung trong workspace Penpot. Mỗi component được thiết kế bằng Flex Layout để tái sử dụng qua các board trạng thái.

---

## 1. Component `ToolButton`

### 1.1. Mục đích

`ToolButton` là nút vuông dùng để chọn công cụ hoặc thực hiện hành động. Nó là phần tử nhỏ nhất trong toolbar.

### 1.2. Cấu trúc layer

```text
ToolButton (Board 32×32px, Flex layout căn giữa)
├── Container (32×32px, bo góc 6px)
│   ├── Icon (18×18px, SVG vector)
│   └── ShortcutLabel (10px, nằm dưới icon — chỉ hiển thị khi tooltip/không nằm trong toolbar chính)
```

### 1.3. Variants

Penpot tạo component với 4 variants chính:

| Variant | Màu nền | Màu icon | Opacity icon | Dùng khi |
| :--- | :--- | :--- | :--- | :--- |
| `Default` | Trong suốt (`#FFFFFF`, alpha = 0) | `#FFFFFF` | `0.85` | Nút chưa được chọn, chuột chưa hover. |
| `Hover` | `#FFFFFF`, alpha = `0.12` | `#FFFFFF` | `1.00` | Chuột đang nằm trên nút. |
| `Active` | `#7C3AED`, alpha = `1.00` | `#FFFFFF` | `1.00` | Công cụ đang được kích hoạt. |
| `Disabled` | Trong suốt | `#FFFFFF` | `0.35` | Nút bị vô hiệu hóa, ví dụ Undo khi không có lịch sử. |

### 1.4. Khoảng cách và căn chỉnh

- Board ngoài: `32 × 32px`.
- Icon bên trong: `18 × 18px`, căn giữa theo cả hai chiều.
- Container căn giữa icon bằng Flex với `justify-content: center`, `align-items: center`.
- Không padding bên trong vì icon đã vừa khít vùng nút.

### 1.5. Trạng thái transition

Khi chuyển variant, không dùng animation phức tạp. Chỉ thay đổi:

- Màu nền.
- Độ mờ icon.
- Không thay đổi kích thước board để tránh layout shift.

---

## 2. Component `BottomToolbar_Container`

### 2.1. Mục đích

`BottomToolbar_Container` là vỏ bọc nền chứa toàn bộ `ToolButton`. Nó nổi sát mép dưới vùng chọn.

### 2.2. Cấu trúc layer

```text
BottomToolbar_Container (auto-width × 40px)
├── Background (nền tối, bo góc 8px, viền 1px mờ, shadow 12px blur)
├── FlexGroup_Horizontal
│   ├── Group_AnnotationTools
│   │   ├── ToolButton_Pencil
│   │   ├── ToolButton_Line
│   │   ├── ToolButton_Arrow
│   │   ├── ToolButton_Rectangle
│   │   ├── ToolButton_Circle
│   │   ├── ToolButton_Marker
│   │   ├── ToolButton_Text
│   │   └── ToolButton_Pixelate
│   ├── Separator (1×20px, màu trắng 15% opacity)
│   └── Group_ActionTools
│       ├── ToolButton_Undo
│       ├── ToolButton_Redo
│       ├── ToolButton_Copy
│       ├── ToolButton_Save
│       └── ToolButton_Cancel
```

### 2.3. Layout chi tiết

- Chiều cao cố định: `40px`.
- Chiều rộng tự động theo số lượng nút.
- Padding ngang: `8px`; padding dọc: `4px`.
- Khoảng cách giữa các nút trong cùng nhóm: `4px`.
- Khoảng cách giữa hai nhóm qua separator: `8px` tính từ mép nút cuối nhóm 1 đến separator, và `8px` từ separator đến nút đầu nhóm 2.
- Separator là hình chữ nhật đứng `1px × 20px`, căn giữa theo chiều dọc trong toolbar.

### 2.4. Góc cạnh và bóng đổ

- Bo góc `8px`.
- Viền ngoài `1px`, màu trắng `10%` opacity.
- Shadow: màu đen `40%` opacity, blur `12px`, offsetY `4px`, không offsetX.

### 2.5. Hành vi responsive

Khi vùng chọn nằm gần cạnh màn hình:

- Nếu toolbar vượt quá biên phải màn hình, dịch chuyển sang trái để vừa vặn.
- Nếu vùng chọn ở sát biên dưới màn hình, toolbar hiển thị ở mép trên vùng chọn thay vì mép dưới.
- Khoảng cách tối thiểu từ toolbar đến biên màn hình: `8px`.

---

## 3. Component `ResizeHandle`

### 3.1. Mục đích

`ResizeHandle` là điểm neo vuông nhỏ xuất hiện quanh vùng chọn khi đã hoàn tất. Dùng để bắt chuột co giãn.

### 3.2. Cấu trúc layer

```text
ResizeHandle (8×8px)
├── VisibleSquare (8×8px, fill cyan, stroke đen 1px)
└── HitTestArea (16×16px, trong suốt, nằm centered xung quanh VisibleSquare)
```

### 3.3. Kích thước

- Hình vuông hiển thị: `8 × 8px`.
- Vùng bắt chuột mở rộng: `16 × 16px`.
- Stroke ngoài: `1px` màu đen.
- Fill: màu cyan `#00E5FF` không trong suốt.

### 3.4. Vị trí 8 handle

Gọi vùng chọn là `R` với `Left = R.X`, `Top = R.Y`, `Right = R.X + R.Width`, `Bottom = R.Y + R.Height`. Tọa độ tâm của 8 handle:

| Handle | Tọa độ tâm (CenterX, CenterY) |
| :--- | :--- |
| TopLeft | `(R.Left, R.Top)` |
| TopCenter | `(R.Left + R.Width / 2, R.Top)` |
| TopRight | `(R.Right, R.Top)` |
| MiddleLeft | `(R.Left, R.Top + R.Height / 2)` |
| MiddleRight | `(R.Right, R.Top + R.Height / 2)` |
| BottomLeft | `(R.Left, R.Bottom)` |
| BottomCenter | `(R.Left + R.Width / 2, R.Bottom)` |
| BottomRight | `(R.Right, R.Bottom)` |

Mỗi handle được vẽ sao cho tâm của nó trùng với tọa độ trên. Do kích thước hiển thị là `8 × 8px`, góc trên-trái của hình vuông hiển thị sẽ lệch `-4px` so với tâm.

### 3.5. Cursor tương ứng

| Handle | Con trỏ chuột |
| :--- | :--- |
| TopLeft, BottomRight | `nwse-resize` |
| TopRight, BottomLeft | `nesw-resize` |
| TopCenter, BottomCenter | `ns-resize` |
| MiddleLeft, MiddleRight | `ew-resize` |

---

## 4. Component `DimensionBadge`

### 4.1. Mục đích

`DimensionBadge` hiển thị kích thước vùng chọn `Width × Height` hoặc `Width × Height + X + Y` khi đang kéo hoặc co giãn.

### 4.2. Cấu trúc layer

```text
DimensionBadge (auto-width × 18px)
├── Background (nền accent, bo góc 4px)
└── TextLabel (font 11px, màu trắng, weight 600)
```

### 4.3. Nội dung văn bản

- Khi đang kéo tạo vùng chọn hoặc resize: `"{Width} × {Height}"`.
- Khi đã chọn xong và cần thông tin đầy đủ (tùy chọn): `"{Width} × {Height} @ {X},{Y}"`.
- Font: `Segoe UI`, `11px`, `weight 600`.

### 4.4. Vị trí

- Mặc định đặt ở góc trên-phải của vùng chọn.
- Khoảng cách từ góc vùng chọn: `4px` về phía trên và `4px` về phía phải.
- Nếu vùng chọn nằm sát biên trên màn hình, badge chuyển xuống góc dưới-phải.
- Padding ngang: `6px`, padding dọc: `2px`.

---

## 5. Component `ColorSwatch`

### 5.1. Mục đích

`ColorSwatch` là ô màu tròn nhỏ trong popup chọn màu hoặc palette công cụ. Dùng để xem trước màu annotation đang chọn.

### 5.2. Cấu trúc layer

```text
ColorSwatch (20×20px, hình tròn)
├── FillCircle (18×18px, màu annotation)
└── OuterRing (20×20px, viền trắng 1px, hiển thị khi swatch đang active)
```

### 5.3. Variants

| Variant | Mô tả |
| :--- | :--- |
| `Default` | Hình tròn tô màu, viền mờ `1px` trắng `30%` opacity. |
| `Active` | Hình tròn tô màu, viền trắng `2px` đậm hơn, kích thước ngoài `20px`. |
| `Hover` | Tô thêm lớp trắng mờ `10%` lên trên màu gốc. |

---

## 6. Component `TooltipHint`

### 6.1. Mục đích

`TooltipHint` là nhãn nhỏ hiển thị tên công cụ và phím tắt khi hover lâu vào `ToolButton`. Không bắt buộc trong MVP nhưng nên thiết kế sẵn.

### 6.2. Cấu trúc layer

```text
TooltipHint (auto-width × 24px)
├── Background (nền đen `#000000`, opacity 0.85, bo góc 4px)
└── Text (13px, trắng)
```

### 6.3. Nội dung

Dạng `"Tên công cụ (Shortcut)"`, ví dụ `"Bút vẽ (P)"`.

---

## 7. Quan hệ giữa các component

```mermaid
graph TD
    A[BottomToolbar_Container] --> B[ToolButton]
    A --> C[Separator]
    D[CaptureOverlay Board] --> A
    D --> E[ResizeHandle x8]
    D --> F[DimensionBadge]
    D --> G[ColorSwatch trong Color Picker]
    B --> H[TooltipHint]
```

---

## 8. Quy tắc đặt tên layer trong Penpot

Mỗi component Master trong Penpot nên có cấu trúc layer thống nhất để dễ tìm và override. Quy tắc đặt tên:

```text
{ComponentName}_{Role}_{Element}
```

Ví dụ:

- `ToolButton_Container_Background`
- `ToolButton_Container_Icon`
- `BottomToolbar_Background`
- `BottomToolbar_Separator`
- `ResizeHandle_VisibleSquare`
- `ResizeHandle_HitTestArea`
- `DimensionBadge_Background`
- `DimensionBadge_Text`

Layer nền luôn đặt ở dưới cùng. Layer tương tác hoặc hit-test trong suốt đặt ở trên cùng. Icon, text, badge nằm giữa.

---

## 9. Checklist tạo từng component trong Penpot

### 9.1. Tạo `ToolButton`

Bước 1: Tạo Board mới tên `ToolButton`, kích thước `32 × 32px`.

Bước 2: Vẽ Rectangle `32 × 32px` tên `ToolButton_Container_Background`, bo góc `Radius.ToolButton = 6px`.

Bước 3: Gán fill:
- Variant Default: `ToolButton.Default.Background` với opacity `0`.
- Variant Hover: `ToolButton.Hover.Background` với opacity `0.12`.
- Variant Active: `ToolButton.Active.Background` với opacity `1`.
- Variant Disabled: `ToolButton.Disabled.Background` với opacity `0`.

Bước 4: Import SVG icon `18 × 18px`, đặt tên `ToolButton_Container_Icon`, căn giữa trong Board. Màu icon binding về `ToolButton.{Variant}.IconColor`, opacity binding về `ToolButton.{Variant}.IconOpacity`.

Bước 5: Tạo 4 Variants: `Default`, `Hover`, `Active`, `Disabled`.

Bước 6: Đảm bảo mỗi variant chỉ thay đổi fill background và opacity icon, không thay đổi kích thước board.

### 9.2. Tạo `BottomToolbar_Container`

Bước 1: Tạo Board mới tên `BottomToolbar_Container`, chiều cao `40px`, chiều rộng tự động (`Hug content`).

Bước 2: Vẽ Rectangle nền tên `BottomToolbar_Background`, kích thước full board, bo góc `Radius.Toolbar = 8px`.

Bước 3: Gán fill `Toolbar.BackgroundColor` với opacity `Toolbar.BackgroundOpacity`.

Bước 4: Thêm stroke `1px`, màu `Toolbar.BorderColor`, opacity `Toolbar.BorderOpacity`.

Bước 5: Thêm shadow: màu `Toolbar.ShadowColor`, opacity `Toolbar.ShadowOpacity`, blur `Toolbar.ShadowBlur`, offsetY `Toolbar.ShadowOffsetY`.

Bước 6: Tạo Flex Layout ngang tên `BottomToolbar_FlexGroup`:
- Direction: Row.
- Align items: Center.
- Gap giữa các phần tử: tính theo `Spacing.Toolbar.InnerGap` và `Spacing.Toolbar.GroupGap`.
- Padding: `Spacing.Toolbar.PaddingX` ngang, `Spacing.Toolbar.PaddingY` dọc.

Bước 7: Tạo nhóm `Group_AnnotationTools`, thêm 8 instance của `ToolButton`.

Bước 8: Tạo Separator: Rectangle `1 × 20px` tên `BottomToolbar_Separator`, màu `Toolbar.SeparatorColor`, opacity `Toolbar.SeparatorOpacity`.

Bước 9: Tạo nhóm `Group_ActionTools`, thêm 5 instance của `ToolButton`.

Bước 10: Đảm bảo khoảng cách giữa 2 nhóm qua separator là `2 × Spacing.Toolbar.GroupGap + Toolbar.SeparatorWidth`.

### 9.3. Tạo `ResizeHandle`

Bước 1: Tạo Board mới tên `ResizeHandle`, kích thước `16 × 16px` (vùng bắt chuột). Board này là hit-test area.

Bước 2: Vẽ Rectangle `8 × 8px` tên `ResizeHandle_VisibleSquare`, căn giữa trong Board.

Bước 3: Fill `Handle.FillColor`, opacity `Handle.FillOpacity`.

Bước 4: Stroke `Handle.StrokeWidth`, màu `Handle.StrokeColor`, opacity `Handle.StrokeOpacity`.

Bước 5: (Tùy chọn) Thêm Rectangle `16 × 16px` tên `ResizeHandle_HitTestArea`, trong suốt hoàn toàn, nằm centered. Layer này giúp designer và developer thấy rõ vùng bắt chuột.

### 9.4. Tạo `DimensionBadge`

Bước 1: Tạo Board mới tên `DimensionBadge`, chiều cao `Size.Badge.Height = 18px`, chiều rộng tự động.

Bước 2: Vẽ Rectangle nền tên `DimensionBadge_Background`, bo góc `Radius.Badge = 4px`.

Bước 3: Fill `Badge.BackgroundColor`, opacity `Badge.BackgroundOpacity`.

Bước 4: Thêm Text tên `DimensionBadge_Text`, nội dung mẫu `"800 × 500"`.

Bước 5: Font: `Text.FontFamily`, size `Text.Badge.FontSize`, weight `Text.Weight.Semibold`, màu `Badge.TextColor`, opacity `Badge.TextOpacity`.

Bước 6: Padding ngang `Size.Badge.PaddingX`, padding dọc `Size.Badge.PaddingY`.

### 9.5. Tạo `ColorSwatch`

Bước 1: Tạo Board mới tên `ColorSwatch`, kích thước `20 × 20px`.

Bước 2: Vẽ Ellipse `18 × 18px` tên `ColorSwatch_FillCircle`, căn giữa.

Bước 3: Fill binding về token màu annotation tương ứng (ví dụ `Annotation.Red`).

Bước 4: Vẽ Ellipse `20 × 20px` tên `ColorSwatch_OuterRing`, nằm dưới FillCircle, màu trắng `30%` opacity variant Default, `100%` opacity variant Active.

Bước 5: Tạo variants `Default`, `Hover`, `Active`. Variant Hover thêm overlay trắng mờ `10%`.

---

## 10. Bảng binding token cho từng component

| Component | Layer | Fill / Stroke / Text | Token | Notes |
| :--- | :--- | :--- | :--- | :--- |
| `ToolButton` | Container | Fill color | `ToolButton.{Variant}.Background` | Opacity theo variant. |
| `ToolButton` | Icon | Fill color | `ToolButton.{Variant}.IconColor` | Opacity theo variant. |
| `BottomToolbar` | Background | Fill color | `Toolbar.BackgroundColor` | Opacity `Toolbar.BackgroundOpacity`. |
| `BottomToolbar` | Background | Stroke color | `Toolbar.BorderColor` | Width `Toolbar.BorderWidth`, opacity `Toolbar.BorderOpacity`. |
| `BottomToolbar` | Background | Shadow | `Toolbar.ShadowColor` | Opacity, blur, offset theo token. |
| `BottomToolbar` | Separator | Fill color | `Toolbar.SeparatorColor` | Opacity `Toolbar.SeparatorOpacity`. |
| `ResizeHandle` | VisibleSquare | Fill color | `Handle.FillColor` | Opacity `Handle.FillOpacity`. |
| `ResizeHandle` | VisibleSquare | Stroke color | `Handle.StrokeColor` | Width `Handle.StrokeWidth`, opacity `Handle.StrokeOpacity`. |
| `DimensionBadge` | Background | Fill color | `Badge.BackgroundColor` | Opacity `Badge.BackgroundOpacity`. |
| `DimensionBadge` | Text | Text color | `Badge.TextColor` | Opacity `Badge.TextOpacity`. |
| `DimensionBadge` | Text | Font size | `Text.Badge.FontSize` | Weight `Text.Weight.Semibold`. |
| `ColorSwatch` | FillCircle | Fill color | `Annotation.{ColorName}` | Ví dụ `Annotation.Red`. |

---

## 11. Yêu cầu icon SVG cho ToolButton

Mỗi icon trong toolbar phải là vector `18 × 18px`, viewBox `0 0 18 18`. Danh sách icon cần thiết:

| Nút | Tên file | Mô tả hình dạng |
| :--- | :--- | :--- |
| Pencil | `icon_pencil.svg` | Nét cong tự do hoặc hình bút chì. |
| Line | `icon_line.svg` | Đường thẳng nghiêng 45°. |
| Arrow | `icon_arrow.svg` | Đường thẳng với mũi tên. |
| Rectangle | `icon_rectangle.svg` | Hình chữ nhật rỗng. |
| Circle | `icon_circle.svg` | Hình tròn / elip rỗng. |
| Marker | `icon_marker.svg` | Nét ngang bán trong suốt. |
| Text | `icon_text.svg` | Chữ `T`. |
| Pixelate | `icon_pixelate.svg` | Lưới ô vuông. |
| Undo | `icon_undo.svg` | Mũi tên cong trái. |
| Redo | `icon_redo.svg` | Mũi tên cong phải. |
| Copy | `icon_copy.svg` | Hai tờ giấy chồng lệch. |
| Save | `icon_save.svg` | Đĩa mềm hoặc mũi tên xuống. |
| Cancel | `icon_cancel.svg` | Dấu `X`. |

Tất cả icon dùng `currentColor` để trong Avalonia có thể đổi màu qua `Foreground`.

---

## 12. Notes cho Penpot

- Mỗi component Master nên được đặt trong Board riêng với kích thước chuẩn, ví dụ `ToolButton` trong Board `32 × 32px`.
- Dùng **Variants** của Penpot để quản lý Default / Hover / Active / Disabled.
- Không gộp nhiều trạng thái vào cùng một layer; tách rõ các layer nền, icon, label.
- Icon nên là vector SVG đơn giản, không raster, để dễ export.
- `ResizeHandle` cần tách lớp `HitTestArea` trong suốt riêng với `VisibleSquare` để designer và developer cùng hiểu vùng bắt chuột.
- Khi tạo instance trong Board trạng thái, nhớ chọn đúng variant của component. Ví dụ trong Board `State_04_Annotating_Mode`, nút Pencil phải dùng variant `Active`.
- Không hardcode màu hay kích thước trong component; tất cả binding về token đã import từ `fshot_tokens.json`. Nếu token thay đổi, toàn bộ component tự động cập nhật.
