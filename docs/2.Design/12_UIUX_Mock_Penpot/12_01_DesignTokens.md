# UI/UX Mock Penpot — Design Tokens

> Tài liệu định nghĩa toàn bộ tokens thị giác dùng trong không gian làm việc Penpot của F-Shot. Mỗi token được đặt tên có tiền tố rõ ràng để khi xuất sang Avalonia chỉ cần map 1:1 vào `ResourceDictionary`.

---

## 1. Nguyên tắc đặt tên

Mỗi token tuân theo quy tắc phân cấp:

```text
{Domain}.{Property}
```

Trong đó:

- `Domain` là vùng áp dụng: `Overlay`, `Selection`, `Toolbar`, `Annotation`, `Handle`, `Badge`, `Surface`, `Text`.
- `Property` là thuộc tính cụ thể: `Color`, `Opacity`, `Width`, `Height`, `Radius`, `FontSize`, `Gap`, `Padding`.

Tất cả giá trị màu dùng hệ `#RRGGBBAA` hoặc `#RRGGBB` kèm theo giá trị `Opacity` riêng để dễ điều chỉnh khi đưa vào Avalonia `SolidColorBrush` hoặc Skia `Paint`.

**Lưu ý:** Penpot chỉ chấp nhận tên token chứa chữ cái và chữ số, phân cách bằng dấu `.`. Không dùng `/` hay `$`.

---

## 2. Color Tokens

### 2.1. Accent — Màu nhận diện thương hiệu

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Accent.Primary` | `#7C3AED` | Màu chính cho nút active, điểm nhấn toolbar, handle cyan thay thế khi cần. |
| `Accent.Secondary` | `#FF0055` | Màu phụ cho trạng thái lỗi, hủy, hoặc warning nhẹ. |
| `Accent.Hover` | `#9F6CF0` | Biến thể sáng hơn của Primary khi hover. |

### 2.2. Overlay — Lớp phủ ngoài vùng chọn

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Overlay.DimColor` | `#000000` | Màu đen dùng làm lớp phủ mờ toàn màn hình. |
| `Overlay.DimOpacity` | `0.50` | Độ mờ mặc định. Công thức: `Alpha = round(0.50 * 255) = 128`. |
| `Overlay.DimOpacityStrong` | `0.60` | Dùng khi cần tương phản cao hơn, ví dụ màn hình sáng. |

### 2.3. Selection — Vùng chọn

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Selection.BorderColor` | `#FFFFFF` | Viền khung chọn, luôn nổi trên nền tối. |
| `Selection.BorderOpacity` | `1.00` | Viền không trong suốt. |
| `Selection.BorderWidth` | `2px` | Độ dày viền khung chọn. |
| `Selection.BorderDash` | `4px on, 4px off` | Nét đứt di chuyển tạo cảm giác đang kéo. |
| `Selection.ClearFill` | `#FFFFFF` | Không tô màu bên trong vùng chọn, opacity = 0. |
| `Selection.HoverTint` | `#FFFFFF` | Khi di chuyển vùng chọn, một lớp tint rất nhẹ có thể được pha trộn. |
| `Selection.HoverTintOpacity` | `0.05` | Tô nhẹ `5%` để phân biệt vùng đang di chuyển. |

### 2.4. Handle — 8 điểm neo co giãn

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Handle.FillColor` | `#00E5FF` | Màu nền cyan nổi bật. |
| `Handle.FillOpacity` | `1.00` | Không trong suốt để dễ bắt chuột. |
| `Handle.StrokeColor` | `#000000` | Viền đen bao quanh để handle nổi trên mọi nền. |
| `Handle.StrokeOpacity` | `1.00` | Viền đen đậm. |
| `Handle.StrokeWidth` | `1px` | Viền mảnh, không lấn át viền vùng chọn. |

### 2.5. Toolbar — Thanh công cụ nổi

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Toolbar.BackgroundColor` | `#1E1E2E` | Nền tối gần đen-xanh. |
| `Toolbar.BackgroundOpacity` | `0.95` | Hơi trong suốt một chút để hòa với overlay. |
| `Toolbar.BorderColor` | `#FFFFFF` | Viền ngoài toolbar. |
| `Toolbar.BorderOpacity` | `0.10` | Viền rất mờ. |
| `Toolbar.BorderWidth` | `1px` | Viền mảnh. |
| `Toolbar.ShadowColor` | `#000000` | Bóng đổ. |
| `Toolbar.ShadowOpacity` | `0.40` | Bóng đổ 40%. |
| `Toolbar.ShadowBlur` | `12px` | Độ mờ bóng. |
| `Toolbar.ShadowOffsetY` | `4px` | Bóng lệch xuống dưới. |
| `Toolbar.SeparatorColor` | `#FFFFFF` | Đường phân cách nhóm nút. |
| `Toolbar.SeparatorOpacity` | `0.15` | Mờ vừa đủ nhìn. |
| `Toolbar.SeparatorWidth` | `1px` | Đường kẻ mảnh. |

### 2.6. ToolButton — Nút trong toolbar

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `ToolButton.Default.Background` | `#FFFFFF` | Nền trong suốt, opacity = 0. |
| `ToolButton.Default.IconColor` | `#FFFFFF` | Icon trắng. |
| `ToolButton.Default.IconOpacity` | `0.85` | Hơi mờ để tránh chói. |
| `ToolButton.Hover.Background` | `#FFFFFF` | Nền xám nhẹ. |
| `ToolButton.Hover.BackgroundOpacity` | `0.12` | Mờ vừa đủ thấy hover. |
| `ToolButton.Hover.IconColor` | `#FFFFFF` | Icon trắng. |
| `ToolButton.Hover.IconOpacity` | `1.00` | Đậm hơn khi hover. |
| `ToolButton.Active.Background` | `#7C3AED` | Nền accent. |
| `ToolButton.Active.BackgroundOpacity` | `1.00` | Không trong suốt. |
| `ToolButton.Active.IconColor` | `#FFFFFF` | Icon trắng trên nền tím. |
| `ToolButton.Active.IconOpacity` | `1.00` | Icon đậm. |
| `ToolButton.Disabled.Background` | `#FFFFFF` | Không nền. |
| `ToolButton.Disabled.IconColor` | `#FFFFFF` | Icon trắng. |
| `ToolButton.Disabled.IconOpacity` | `0.35` | Mờ đi rõ rệt. |

### 2.7. Annotation — Bảng màu công cụ vẽ

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Annotation.Red` | `#EF4444` | Màu đỏ mặc định. |
| `Annotation.Yellow` | `#F59E0B` | Màu vàng. |
| `Annotation.Green` | `#10B981` | Màu xanh lá. |
| `Annotation.Blue` | `#3B82F6` | Màu xanh dương. |
| `Annotation.White` | `#FFFFFF` | Màu trắng. |
| `Annotation.Black` | `#000000` | Màu đen. |
| `Annotation.StrokeWidth.Default` | `2px` | Độ dày nét mặc định. |
| `Annotation.StrokeWidth.Thick` | `4px` | Dùng cho marker hoặc nhấn mạnh. |
| `Annotation.Marker.Opacity` | `0.40` | Marker bán trong suốt. |
| `Annotation.Text.Color` | `#EF4444` | Màu chữ mặc định trùng với Annotation.Red. |
| `Annotation.Text.Background` | `#000000` | Nền text box trong suốt hoàn toàn khi đang chỉnh sửa. |
| `Annotation.Text.BackgroundOpacity` | `0.00` | Không nền khi edit. |

### 2.8. Badge — Tag kích thước vùng chọn

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Badge.BackgroundColor` | `#7C3AED` | Nền accent. |
| `Badge.BackgroundOpacity` | `0.95` | Gần như đục. |
| `Badge.TextColor` | `#FFFFFF` | Chữ trắng. |
| `Badge.TextOpacity` | `1.00` | Không trong suốt. |
| `Badge.BorderRadius` | `4px` | Bo góc nhỏ. |

### 2.9. Text — Typography cơ bản

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Text.FontFamily` | `Segoe UI, sans-serif` | Font hệ thống Windows mặc định. |
| `Text.FontFamilyMono` | `Consolas, monospace` | Font monospace cho số liệu, tọa độ. |
| `Text.Toolbar.FontSize` | `13px` | Không dùng chữ trên toolbar chính, chỉ dùng cho tooltip. |
| `Text.Badge.FontSize` | `11px` | Chữ trong dimension badge. |
| `Text.Shortcut.FontSize` | `10px` | Chữ phím tắt nhỏ bên dưới icon. |
| `Text.Weight.Regular` | `400` | Thông thường. |
| `Text.Weight.Semibold` | `600` | Nhấn mạnh. |

---

## 3. Size & Radius Tokens

### 3.1. Kích thước tuyệt đối

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Size.Toolbar.Height` | `40px` | Chiều cao thanh công cụ. |
| `Size.Toolbar.MinWidth` | `200px` | Độ rộng tối thiểu khi chứa ít nút. |
| `Size.ToolButton.Width` | `32px` | Chiều rộng nút công cụ. |
| `Size.ToolButton.Height` | `32px` | Chiều cao nút công cụ. |
| `Size.Icon.Width` | `18px` | Chiều rộng icon trong nút. |
| `Size.Icon.Height` | `18px` | Chiều cao icon trong nút. |
| `Size.Handle.Width` | `8px` | Chiều rộng handle. |
| `Size.Handle.Height` | `8px` | Chiều cao handle. |
| `Size.Handle.HitPadding` | `4px` | Vùng mở rộng bắt chuột xung quanh handle, tổng vùng bắt = `HandleSize + 2 * HitPadding = 16px`. |
| `Size.Badge.Height` | `18px` | Chiều cao badge kích thước. |
| `Size.Badge.PaddingX` | `6px` | Padding ngang trong badge. |
| `Size.Badge.PaddingY` | `2px` | Padding dọc trong badge. |
| `Size.Separator.Height` | `20px` | Chiều cao đường phân cách trong toolbar. |

### 3.2. Bán kính bo góc

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Radius.Toolbar` | `8px` | Bo góc thanh công cụ. |
| `Radius.ToolButton` | `6px` | Bo góc nút công cụ. |
| `Radius.Badge` | `4px` | Bo góc badge. |
| `Radius.Popup` | `10px` | Bo góc popup nhỏ như color picker. |

---

## 4. Spacing Tokens

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Spacing.Toolbar.InnerGap` | `4px` | Khoảng cách giữa các nút trong cùng nhóm. |
| `Spacing.Toolbar.GroupGap` | `8px` | Khoảng cách giữa 2 nhóm nút, tính từ mép separator. |
| `Spacing.Toolbar.PaddingX` | `8px` | Padding ngang toàn toolbar. |
| `Spacing.Toolbar.PaddingY` | `4px` | Padding dọc toàn toolbar. |
| `Spacing.Toolbar.OffsetFromSelection` | `8px` | Khoảng cách từ mép dưới vùng chọn đến toolbar. |
| `Spacing.Popup.Offset` | `6px` | Khoảng cách từ nút kích hoạt đến popup. |

---

## 5. Tổng hợp công thức chuyển đổi sang Avalonia

### 5.1. Màu với opacity

Khi đưa token có opacity vào `SolidColorBrush`, công thức tính alpha:

```text
AlphaByte = round(Opacity * 255)
```

Ví dụ: `Overlay.DimOpacity = 0.50` thì alpha = 128, tương đương `#00000080`.

### 5.2. Vùng bắt chuột handle

```text
HitTestRect.Width  = Handle.Width  + 2 * Handle.HitPadding
HitTestRect.Height = Handle.Height + 2 * Handle.HitPadding
```

Với token đã định nghĩa: `HitTestRect = 16px × 16px`, handle hiển thị bên trong là `8px × 8px`.

### 5.3. Tổng chiều rộng toolbar tối thiểu

```text
ToolbarMinWidth = PaddingX
                + (số nút nhóm 1 × ToolButton.Width)
                + (số nút nhóm 1 - 1) × InnerGap
                + GroupGap
                + SeparatorWidth
                + GroupGap
                + (số nút nhóm 2 × ToolButton.Width)
                + (số nút nhóm 2 - 1) × InnerGap
                + PaddingX
```

Với 8 nút annotation + 5 nút action:

```text
ToolbarMinWidth = 8 + 8×32 + 7×4 + 8 + 1 + 8 + 5×32 + 4×4 + 8 = 624px
```

---

## 6. Notes cho Penpot

- Tất cả tokens nên được tạo trong tab **Tokens** của Penpot, nhóm theo tên miền tương ứng.
- Không dùng màu hardcode trực tiếp trên component; mọi fill, stroke, text đều binding về token.
- Các token opacity nên tách riêng khỏi mã màu để khi xuất sang Avalonia có thể dùng `Opacity` property thay vì ép alpha vào hex.
- Nên tạo thêm token cho dark mode ngay từ đầu vì F-Shot overlay luôn hoạt động trên nền màn hình tối.
- **Dùng dấu `.` để phân cấp token trong Penpot** (ví dụ `Accent.Primary`, `Overlay.DimOpacity`). Không dùng `/` vì Penpot không hỗ trợ.
