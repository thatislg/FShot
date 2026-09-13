# UI/UX Mock Penpot — Export Guidelines

> Quy tắc xuất các thành phần thiết kế từ Penpot sang tài nguyên dùng trong Avalonia XAML và SkiaSharp rendering. Mục tiêu là giảm thiểu việc đo đạc lại khi chuyển từ design sang code.

---

## 1. Nguyên tắc chung

### 1.1. Xuất cái gì?

Từ workspace Penpot cần xuất ra 3 nhóm tài nguyên:

1. **Design Tokens**: từ tab Tokens, dùng để xây dựng Avalonia `ResourceDictionary`.
2. **Vector Icons**: các icon trong `ToolButton`, dùng để tạo `PathIcon` hoặc `Geometry` trong XAML.
3. **Layout Metrics**: kích thước, padding, gap, shadow, radius từ Master Components, dùng để viết XAML layout.

### 1.2. Không xuất cái gì?

- Không xuất toàn bộ board dưới dạng ảnh raster làm background UI.
- Không xuất ảnh screenshot mẫu.
- Không xuất các layer tạm như cursor, grid positioning helper.

---

## 2. Export Design Tokens

### 2.1. Tổ chức token trong Penpot

Trong tab Tokens của Penpot, tổ chức theo cây:

```text
FShot Tokens
├── Accent
│   ├── Primary
│   ├── Secondary
│   └── Hover
├── Overlay
│   ├── DimColor
│   ├── DimOpacity
│   └── DimOpacityStrong
├── Selection
│   ├── BorderColor
│   ├── BorderOpacity
│   ├── BorderWidth
│   └── BorderDash
├── Handle
│   ├── FillColor
│   ├── StrokeColor
│   ├── StrokeWidth
│   ├── Width
│   └── Height
├── Toolbar
│   ├── BackgroundColor
│   ├── BackgroundOpacity
│   ├── BorderColor
│   ├── BorderOpacity
│   ├── BorderWidth
│   ├── ShadowColor
│   ├── ShadowOpacity
│   ├── ShadowBlur
│   └── ShadowOffsetY
├── ToolButton
│   ├── Default.Background
│   ├── Default.IconColor
│   ├── Hover.Background
│   └── Active.Background
├── Annotation
│   ├── Red
│   ├── Yellow
│   ├── Green
│   ├── Blue
│   ├── White
│   ├── Black
│   ├── StrokeWidth.Default
│   └── Marker.Opacity
├── Text
│   ├── FontFamily
│   ├── FontFamilyMono
│   ├── Toolbar.FontSize
│   ├── Badge.FontSize
│   └── Shortcut.FontSize
├── Size
│   ├── Toolbar.Height
│   ├── ToolButton.Width
│   ├── ToolButton.Height
│   ├── Icon.Width
│   ├── Handle.Width
│   ├── Handle.HitPadding
│   └── Badge.Height
├── Radius
│   ├── Toolbar
│   ├── ToolButton
│   └── Badge
└── Spacing
    ├── Toolbar.InnerGap
    ├── Toolbar.GroupGap
    ├── Toolbar.PaddingX
    └── Toolbar.OffsetFromSelection
```

### 2.2. Cách map sang Avalonia

Mỗi token màu trong Penpot chuyển thành một `SolidColorBrush` hoặc `Color` trong Avalonia. Công thức:

```text
Brush = Color * Opacity
```

Ví dụ:

- `Overlay.DimColor = #000000`, `Overlay.DimOpacity = 0.50` → tạo brush `#00000080`.
- `Toolbar.BackgroundColor = #1E1E2E`, `Toolbar.BackgroundOpacity = 0.95` → tạo brush `#1E1E2EF2`.

Các token kích thước chuyển thành `double` trong XAML hoặc constant trong F#.

Các token font chuyển thành `FontFamily` và `FontSize` trong Avalonia.

---

## 3. Export Icons

### 3.1. Định dạng xuất

Xuất icon dưới dạng **SVG** để giữ vector. Không xuất PNG vì sẽ bị vỡ khi phóng to hoặc DPI khác nhau.

### 3.2. Kích thước viewBox

Tất cả icon SVG phải có `viewBox="0 0 18 18"` để đồng nhất với token `Icon/Width = 18px` và `Icon/Height = 18px`.

### 3.3. Danh sách icon cần xuất

| Icon | Tên file gợi ý | Dùng trong nút |
| :--- | :--- | :--- |
| Pencil | `icon_pencil.svg` | ToolButton Pencil |
| Line | `icon_line.svg` | ToolButton Line |
| Arrow | `icon_arrow.svg` | ToolButton Arrow |
| Rectangle | `icon_rectangle.svg` | ToolButton Rectangle |
| Circle | `icon_circle.svg` | ToolButton Circle |
| Marker | `icon_marker.svg` | ToolButton Marker |
| Text | `icon_text.svg` | ToolButton Text |
| Pixelate | `icon_pixelate.svg` | ToolButton Pixelate |
| Undo | `icon_undo.svg` | ToolButton Undo |
| Redo | `icon_redo.svg` | ToolButton Redo |
| Copy | `icon_copy.svg` | ToolButton Copy |
| Save | `icon_save.svg` | ToolButton Save |
| Cancel | `icon_cancel.svg` | ToolButton Cancel |

### 3.4. Quy tắc nội dung SVG

- Tất cả đường nét dùng `stroke="currentColor"` để trong Avalonia có thể đổi màu qua `Foreground`.
- Không đặt màu cố định trong SVG.
- Không dùng gradient.
- Tối ưu path, tránh điểm thừa.

---

## 4. Export Layout Metrics

### 4.1. Từ Master Component `ToolButton`

Cần lấy:

- Board size: `32 × 32px`.
- Icon size: `18 × 18px`.
- Bo góc container: `6px`.
- Màu nền các variant: Default, Hover, Active, Disabled.

### 4.2. Từ Master Component `BottomToolbar_Container`

Cần lấy:

- Chiều cao: `40px`.
- Padding ngang: `8px`, padding dọc: `4px`.
- Gap giữa các nút trong nhóm: `4px`.
- Gap giữa hai nhóm qua separator: `8px`.
- Kích thước separator: `1px × 20px`.
- Màu nền, màu viền, shadow.
- Bo góc: `8px`.

### 4.3. Từ Master Component `ResizeHandle`

Cần lấy:

- Visible square: `8 × 8px`.
- Hit area: `16 × 16px`.
- Stroke: `1px` màu đen.
- Fill: màu cyan `#00E5FF`.

### 4.4. Từ Board trạng thái

Cần lấy:

- Vị trí tương đối của toolbar so với vùng chọn.
- Vị trí của badge.
- Vị trí 8 handle quanh vùng chọn.
- Z-index layer.

---

## 5. Mapping sang Avalonia XAML

### 5.1. Brush mapping

Mỗi màu từ token trở thành `SolidColorBrush`. Khi token có opacity, cách tốt nhất là:

```text
SolidColorBrush Color = HexColor
Opacity = TokenOpacity
```

Thay vì ép alpha vào hex. Điều này giúp binding opacity động sau này.

### 5.2. Size mapping

Token kích thước trở thành giá trị số trong XAML, ví dụ `Width="32"`, `Height="32"`.

### 5.3. Layout mapping

- `Flex Layout` trong Penpot tương đương `StackPanel` hoặc `DockPanel` trong Avalonia.
- `gap` trong Penpot tương đương `Spacing` trong `StackPanel`.
- `padding` trong Penpot tương đương `Padding` trong container.

### 5.4. Icon mapping

SVG xuất ra được đưa vào Avalonia qua `PathIcon` hoặc `DrawingImage`. Mỗi icon nên có `Width="18" Height="18"`.

---

## 6. Mapping sang SkiaSharp Rendering

### 6.1. Màu sắc

Skia dùng `SKPaint` với `Color` và `IsAntialias`. Công thức chuyển từ token:

```text
SKColor = RGBA(Color.R, Color.G, Color.B, round(Opacity * 255))
```

### 6.2. Overlay dimming

Lớp phủ tối toàn màn hình dùng `SKPaint` màu đen với alpha từ `Overlay.DimOpacity`.

### 6.3. Selection border

Viền vùng chọn dùng `SKPaint` với:

- `Color = Selection.BorderColor`.
- `StrokeWidth = Selection.BorderWidth`.
- `PathEffect` nếu là nét đứt.

### 6.4. Resize handles

Mỗi handle vẽ bằng 2 lần:

1. Fill hình vuông `8 × 8px` màu cyan.
2. Stroke hình vuông `8 × 8px` màu đen, width `1px`.

---

## 7. Cấu trúc thư mục tài nguyên trong repository

Sau khi xuất từ Penpot, tài nguyên nên được sắp xếp trong dự án như sau:

```text
docs/2.Design/12_UIUX_Mock_Penpot/
├── 12_00_Overview.md
├── 12_01_DesignTokens.md
├── 12_02_CommonComponents.md
├── 12_03_CaptureOverlayStates.md
├── 12_04_ToolbarLayout.md
├── 12_05_ExportGuidelines.md
└── assets/
    ├── fshot_tokens.json     (file import tokens DTCG cho Penpot)
    └── icons/
        ├── icon_pencil.svg
        └── ...
```

## 9. Import nhanh tokens vào Penpot

### 9.1. Cách import từ file JSON

Penpot hỗ trợ import token theo chuẩn DTCG (Design Tokens Community Group) từ file JSON. Thay vì tạo từng token thủ công:

1. Mở project Penpot.
2. Chọn tab **Tokens** ở sidebar phải.
3. Click nút **Import** (biểu tượng upload / import).
4. Chọn file `assets/fshot_tokens.json`.
5. Penpot sẽ tạo toàn bộ cây token theo nhóm: `Accent`, `Overlay`, `Selection`, `Handle`, `Toolbar`, `ToolButton`, `Annotation`, `Badge`, `Text`, `Size`, `Radius`, `Spacing`.

### 9.2. Lưu ý khi import

- File JSON phải đúng định dạng DTCG với các key `$type`, `$value`, `$description`.
- Penpot có thể chưa hỗ trợ đầy đủ tất cả các type; nếu gặp lỗi, hãy kiểm tra các token kiểu `string` hoặc `number` không có đơn vị.
- Nếu Penpot báo lỗi alias/reference, đảm bảo không dùng cú pháp `{token.path}` trong `$value`.
- Sau khi import, kiểm tra lại nhóm token để đảm bảo không bị lệch cấu trúc.

### 9.3. Fallback nếu import không hoạt động

Nếu Penpot tại thời điểm sử dụng chưa hỗ trợ import JSON:

- Dùng file `fshot_tokens.json` làm bảng tra cứu.
- Tạo từng nhóm token trong Penpot theo cây đã định nghĩa.
- Có thể copy tên token từ file JSON để tránh nhầm lẫn.

### 9.4. Export ngược từ Penpot

Khi cần đồng bộ token từ Penpot về code:

1. Trong tab Tokens, chọn **Export**.
2. Chọn định dạng DTCG JSON.
3. Lưu vào `assets/fshot_tokens_exported.json`.
4. So sánh với `fshot_tokens.json` gốc để phát hiện thay đổi.

---

## 8. Notes cho quá trình export

- Xuất icon theo đúng thứ tự toolbar để tránh nhầm lẫn.
- Kiểm tra lại `viewBox` của mỗi SVG trước khi đưa vào code.
- Đối chiếu màu sắc từ Penpot với token trong `12_01_DesignTokens.md`.
- Nếu thay đổi kích thước component trong Penpot, phải cập nhật lại token và tài liệu thiết kế trước khi sửa code.
- Không dùng ảnh raster cho bất kỳ thành phần tương tác nào.
- Nên giữ một bản copy file Penpot gốc trong repository hoặc ít nhất ghi link Penpot Cloud trong `12_00_Overview.md`.
