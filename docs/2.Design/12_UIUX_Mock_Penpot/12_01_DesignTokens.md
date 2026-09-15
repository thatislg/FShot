# UI/UX Mock Penpot — Design Tokens

> Tài liệu định nghĩa toàn bộ tokens thị giác dùng trong không gian làm việc Penpot của F-Shot. Mỗi token được đặt tên có tiền tố rõ ràng để khi xuất sang Avalonia chỉ cần map 1:1 vào `ResourceDictionary`.
> 
> **Chuẩn màu hiện hành:** Kawaii Claymorphism, dựa trên `12_06_Icon_Asset_Production_Guide.md`. Toàn bộ icon toolbar, overlay, handle, badge và annotation sử dụng chung bảng màu dưới đây để đảm bảo đồng nhất 100%.

---

## 1. Nguyên tắc đặt tên

Mỗi token tuân theo quy tắc phân cấp:

```text
{Domain}.{Property}
```

Trong đó:

- `Domain` là vùng áp dụng: `Overlay`, `Selection`, `Toolbar`, `ToolButton`, `Annotation`, `Handle`, `Badge`, `Surface`, `Text`.
- `Property` là thuộc tính cụ thể: `Color`, `Opacity`, `Width`, `Height`, `Radius`, `FontSize`, `Gap`, `Padding`.

Tất cả giá trị màu dùng hệ `#RRGGBBAA` hoặc `#RRGGBB` kèm theo giá trị `Opacity` riêng để dễ điều chỉnh khi đưa vào Avalonia `SolidColorBrush` hoặc Skia `Paint`.

**Lưu ý:** Penpot chỉ chấp nhận tên token chứa chữ cái và chữ số, phân cách bằng dấu `.`. Không dùng `/` hay `$`.

---

## 2. Color Architecture — Kiến trúc màu chuẩn

Bảng màu được chia thành **Core Palette** (dùng toàn app), **Semantic Groups** (4 nhóm công cụ) và **Functional Colors** (trạng thái tương tác).

### 2.1 Core Palette — Bảng màu nền tảng

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Core.SkyBlue` | `#7BD5F5` | Màu nhận diện chính F-Shot (bầu trời pastel). Dùng cho accent, handle, active states. |
| `Core.ButterYellow` | `#FDE047` | Vàng bơ ấm áp. Dùng cho annotation nóng, highlight, icon arrow. |
| `Core.CoralRed` | `#FF7A70` | Đỏ san hô. Dùng cho annotation vẽ, pencil, nét nổi bật. |
| `Core.MintMacaron` | `#86EFAC` | Xanh bạc hà macaron. Dùng cho selection, crop, rectangle, line. |
| `Core.BubbleIceBlue` | `#BAE6FD` | Xanh băng tuyết. Dùng cho text tool, step bubble, system actions nhẹ. |
| `Core.LavenderPurple` | `#DDD6FE` | Tím lavender. Dùng cho blur, obfuscation, undo/redo. |
| `Core.StrawberryRed` | `#FB7185` | Đỏ dâu tây. Dùng cho cancel, close, error, delete. |
| `Core.BaseContainer` | `#FFFDF9` | Kem trắng ngà. Nền nút/container trong toolbar. |
| `Core.BorderLight` | `#E2E8F0` | Xám tro nhạt. Viền container, separator. |
| `Core.DarkWalnut` | `#3D2B1F` | Nâu óc chó đậm. Viền ngoài icon (thay vì đen). |
| `Core.DeepSlateNavy` | `#1E293B` | Xanh navy đậm. Viền icon khi cần tương phản lạnh. |
| `Core.CheekBlush` | `#F472B6` | Hồng má. Dùng cho chi tiết kawaii trên icon. |
| `Core.SpecularHighlight` | `#FFFFFF` | Đốm sáng trắng trên icon (opacity 85%). |

### 2.2 Semantic Groups — 4 nhóm công cụ

Mỗi tool thuộc đúng 1 nhóm; nhóm quyết định màu chủ đạo của icon và accent liên quan.

| Nhóm | Token gốc | Màu chính | Màu viền | Công cụ thuộc nhóm |
| :--- | :--- | :--- | :--- | :--- |
| `Semantic.Annotation` | `Core.CoralRed` / `Core.ButterYellow` | `#FF7A70` | `#B91C1C` | Pencil, Arrow, Text |
| `Semantic.Obfuscation` | `Core.LavenderPurple` | `#DDD6FE` | `#8B5CF6` | Pixelate, Blur |
| `Semantic.Selection` | `Core.MintMacaron` / `Core.SkyBlue` | `#86EFAC` | `#15803D` | Selection, Crop, Line, Rectangle, Circle |
| `Semantic.System` | `Core.BubbleIceBlue` / `Core.StrawberryRed` | `#BAE6FD` | `#0284C7` | Save, Copy, Undo, Redo, Cancel |

### 2.3 Functional Colors — Trạng thái tương tác

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Functional.ActiveAccent` | `#38BDF8` | Viền/highlight khi tool hoặc nút đang active. |
| `Functional.HoverTint` | `#FEF9C3` | Nền nhẹ khi hover container. |
| `Functional.Disabled` | `#94A3B8` | Màu xám khi disabled (opacity 40%). |
| `Functional.Shadow` | `#000000` | Bóng đổ mềm, opacity 10% (không dùng đen đậm). |
| `Functional.Outline` | `#3D2B1F` | Viền ngoài icon chuẩn. |
| `Functional.OutlineAlt` | `#1E293B` | Viền icon khi cần tương phản lạnh. |

---

## 3. Domain Tokens

### 3.1 Accent — Màu nhận diện thương hiệu

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Accent.Primary` | `#7BD5F5` | Màu chính F-Shot (Sky Blue). Dùng cho nút active, điểm nhấn toolbar, handle. |
| `Accent.Secondary` | `#FB7185` | Màu phụ cho trạng thái lỗi, hủy, warning (Strawberry Red). |
| `Accent.Hover` | `#BAE6FD` | Biến thể sáng hơn của Primary khi hover (Bubble Ice Blue). |

### 3.2 Overlay — Lớp phủ ngoài vùng chọn

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Overlay.DimColor` | `#45B6F7` | Màu xanh Flameshot-style dùng làm lớp phủ mờ toàn màn hình. Đồng bộ với Core.SkyBlue. |
| `Overlay.DimOpacity` | `0.55` | Độ mờ mặc định. Công thức: `Alpha = round(0.55 * 255) = 140`. |
| `Overlay.DimOpacityStrong` | `0.65` | Dùng khi cần tương phản cao hơn, ví dụ màn hình sáng. |

### 3.3 Selection — Vùng chọn

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Selection.BorderColor` | `#FFFFFF` | Viền khung chọn, luôn nổi trên nền tối. |
| `Selection.BorderOpacity` | `1.00` | Viền không trong suốt. |
| `Selection.BorderWidth` | `2px` | Độ dày viền khung chọn. |
| `Selection.BorderDash` | `4px on, 4px off` | Nét đứt di chuyển tạo cảm giác đang kéo. |
| `Selection.ClearFill` | `#FFFFFF` | Không tô màu bên trong vùng chọn, opacity = 0. |
| `Selection.HoverTint` | `#FFFFFF` | Khi di chuyển vùng chọn, một lớp tint rất nhẹ có thể được pha trộn. |
| `Selection.HoverTintOpacity` | `0.05` | Tô nhẹ `5%` để phân biệt vùng đang di chuyển. |

### 3.4 Handle — 8 điểm neo co giãn

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Handle.FillColor` | `#7BD5F5` | Màu nền Sky Blue nổi bật (Core.SkyBlue). |
| `Handle.FillOpacity` | `1.00` | Không trong suốt để dễ bắt chuột. |
| `Handle.StrokeColor` | `#FFFFFF` | Viền trắng bao quanh để handle nổi trên mọi nền. |
| `Handle.StrokeOpacity` | `1.00` | Viền trắng đậm. |
| `Handle.StrokeWidth` | `1px` | Viền mảnh, không lấn át viền vùng chọn. |

### 3.5 Toolbar — Thanh công cụ nổi

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Toolbar.BackgroundColor` | `#FFFDF9` | Nền kem trắng ngà (Core.BaseContainer), phù hợp với icon màu pastel. |
| `Toolbar.BackgroundOpacity` | `0.98` | Hầu như đục, giữ icon rõ trên nền overlay xanh. |
| `Toolbar.BorderColor` | `#E2E8F0` | Viền ngoài toolbar (Core.BorderLight). |
| `Toolbar.BorderOpacity` | `1.00` | Viền nhẹ nhưng nhìn rõ. |
| `Toolbar.BorderWidth` | `1px` | Viền mảnh. |
| `Toolbar.ShadowColor` | `#000000` | Bóng đổ. |
| `Toolbar.ShadowOpacity` | `0.10` | Bóng đổ 10%, mềm nhẹ. |
| `Toolbar.ShadowBlur` | `12px` | Độ mờ bóng. |
| `Toolbar.ShadowOffsetY` | `4px` | Bóng lệch xuống dưới. |
| `Toolbar.SeparatorColor` | `#E2E8F0` | Đường phân cách nhóm nút (Core.BorderLight). |
| `Toolbar.SeparatorOpacity` | `1.00` | Viền nhạt nhưng rõ. |
| `Toolbar.SeparatorWidth` | `1px` | Đường kẻ mảnh. |

### 3.6 ToolButton — Nút trong toolbar

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `ToolButton.Default.Background` | `#FFFDF9` | Nền trong suốt, opacity = 0. |
| `ToolButton.Default.IconColor` | `#3D2B1F` | Icon màu nâu óc chó đậm (Functional.Outline) để nổi trên nền kem. |
| `ToolButton.Default.IconOpacity` | `0.90` | Hơi mờ để tránh chói. |
| `ToolButton.Hover.Background` | `#FEF9C3` | Nền vàng bơ nhạt (Functional.HoverTint). |
| `ToolButton.Hover.BackgroundOpacity` | `1.00` | Hover rõ. |
| `ToolButton.Hover.IconColor` | `#3D2B1F` | Icon giữ màu outline. |
| `ToolButton.Hover.IconOpacity` | `1.00` | Đậm hơn khi hover. |
| `ToolButton.Active.Background` | `#38BDF8` | Nền Active Accent (Functional.ActiveAccent). |
| `ToolButton.Active.BackgroundOpacity` | `1.00` | Không trong suốt. |
| `ToolButton.Active.IconColor` | `#FFFFFF` | Icon trắng trên nền xanh dương active. |
| `ToolButton.Active.IconOpacity` | `1.00` | Icon đậm. |
| `ToolButton.Active.BorderColor` | `#1D4ED8` | Viền xanh đậm khi active. |
| `ToolButton.Disabled.Background` | `#FFFDF9` | Không nền. |
| `ToolButton.Disabled.IconColor` | `#94A3B8` | Icon xám (Functional.Disabled). |
| `ToolButton.Disabled.IconOpacity` | `0.40` | Mờ đi rõ rệt. |

### 3.7 Annotation — Bảng màu công cụ vẽ

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Annotation.DefaultColor` | `#FF7A70` | Màu mặc định cho nét vẽ (Core.CoralRed). |
| `Annotation.Red` | `#FF7A70` | Đỏ san hô. |
| `Annotation.Yellow` | `#FDE047` | Vàng bơ. |
| `Annotation.Green` | `#86EFAC` | Xanh bạc hà. |
| `Annotation.Blue` | `#7BD5F5` | Xanh trời. |
| `Annotation.Purple` | `#DDD6FE` | Tím lavender (dùng cho blur/pixelate effect). |
| `Annotation.White` | `#FFFFFF` | Trắng. |
| `Annotation.Black` | `#3D2B1F` | Nâu đậm thay cho đen thuần. |
| `Annotation.StrokeWidth.Default` | `2px` | Độ dày nét mặc định. |
| `Annotation.StrokeWidth.Thick` | `4px` | Dùng cho marker hoặc nhấn mạnh. |
| `Annotation.Marker.Opacity` | `0.40` | Marker bán trong suốt. |
| `Annotation.Text.Color` | `#FF7A70` | Màu chữ mặc định trùng với Annotation.DefaultColor. |
| `Annotation.Text.Background` | `#000000` | Nền text box trong suốt hoàn toàn khi đang chỉnh sửa. |
| `Annotation.Text.BackgroundOpacity` | `0.00` | Không nền khi edit. |

### 3.8 Badge — Tag kích thước vùng chọn

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Badge.BackgroundColor` | `#38BDF8` | Nền Active Accent. |
| `Badge.BackgroundOpacity` | `0.95` | Gần như đục. |
| `Badge.TextColor` | `#FFFFFF` | Chữ trắng. |
| `Badge.TextOpacity` | `1.00` | Không trong suốt. |
| `Badge.BorderRadius` | `4px` | Bo góc nhỏ. |

### 3.9 Text — Typography cơ bản

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

## 4. Size & Radius Tokens

### 4.1 Kích thước tuyệt đối

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Size.Toolbar.Height` | `40px` | Chiều cao thanh công cụ. |
| `Size.Toolbar.MinWidth` | `200px` | Độ rộng tối thiểu khi chứa ít nút. |
| `Size.ToolButton.Width` | `32px` | Chiều rộng nút công cụ. |
| `Size.ToolButton.Height` | `32px` | Chiều cao nút công cụ. |
| `Size.Icon.Width` | `20px` | Chiều rộng icon trong nút (tăng từ 18px để phù hợp style chi tiết hơn). |
| `Size.Icon.Height` | `20px` | Chiều cao icon trong nút. |
| `Size.Handle.Width` | `8px` | Chiều rộng handle. |
| `Size.Handle.Height` | `8px` | Chiều cao handle. |
| `Size.Handle.HitPadding` | `4px` | Vùng mở rộng bắt chuột xung quanh handle, tổng vùng bắt = `HandleSize + 2 * HitPadding = 16px`. |
| `Size.Badge.Height` | `18px` | Chiều cao badge kích thước. |
| `Size.Badge.PaddingX` | `6px` | Padding ngang trong badge. |
| `Size.Badge.PaddingY` | `2px` | Padding dọc trong badge. |
| `Size.Separator.Height` | `20px` | Chiều cao đường phân cách trong toolbar. |

### 4.2 Bán kính bo góc

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Radius.Toolbar` | `10px` | Bo góc thanh công cụ (squishy clay style). |
| `Radius.ToolButton` | `6px` | Bo góc nút công cụ. |
| `Radius.Badge` | `4px` | Bo góc badge. |
| `Radius.Popup` | `10px` | Bo góc popup nhỏ như color picker. |
| `Radius.MinimumFillet` | `3px` | Bán kính bo góc tối thiểu cho icon; không cho phép góc vuông sắc cạnh. |

---

## 5. Spacing Tokens

| Token | Giá trị | Ý nghĩa |
| :--- | :--- | :--- |
| `Spacing.Toolbar.InnerGap` | `4px` | Khoảng cách giữa các nút trong cùng nhóm. |
| `Spacing.Toolbar.GroupGap` | `8px` | Khoảng cách giữa 2 nhóm nút, tính từ mép separator. |
| `Spacing.Toolbar.PaddingX` | `8px` | Padding ngang toàn toolbar. |
| `Spacing.Toolbar.PaddingY` | `4px` | Padding dọc toàn toolbar. |
| `Spacing.Toolbar.OffsetFromSelection` | `8px` | Khoảng cách từ mép dưới vùng chọn đến toolbar. |
| `Spacing.Popup.Offset` | `6px` | Khoảng cách từ nút kích hoạt đến popup. |

---

## 6. Tổng hợp công thức chuyển đổi sang Avalonia

### 6.1 Màu với opacity

Khi đưa token có opacity vào `SolidColorBrush`, công thức tính alpha:

```text
AlphaByte = round(Opacity * 255)
```

Ví dụ: `Overlay.DimOpacity = 0.55` thì alpha = 140, tương đương `#45B6F78C`.

### 6.2 Vùng bắt chuột handle

```text
HitTestRect.Width  = Handle.Width  + 2 * Handle.HitPadding
HitTestRect.Height = Handle.Height + 2 * Handle.HitPadding
```

Với token đã định nghĩa: `HitTestRect = 16px × 16px`, handle hiển thị bên trong là `8px × 8px`.

### 6.3 Tổng chiều rộng toolbar tối thiểu

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

## 7. Notes cho Penpot

- Tất cả tokens nên được tạo trong tab **Tokens** của Penpot, nhóm theo tên miền tương ứng.
- Không dùng màu hardcode trực tiếp trên component; mọi fill, stroke, text đều binding về token.
- Các token opacity nên tách riêng khỏi mã màu để khi xuất sang Avalonia có thể dùng `Opacity` property thay vì ép alpha vào hex.
- **Dùng dấu `.` để phân cấp token trong Penpot** (ví dụ `Accent.Primary`, `Overlay.DimOpacity`). Không dùng `/` vì Penpot không hỗ trợ.
- Màu nền toolbar đã chuyển từ tối sang **kem trắng ngà** để phù hợp với phong cách Kawaii Claymorphism; overlay dim vẫn giữ xanh Sky Blue để tạo độ tương phản rõ.
- Khi tạo icon trong Penpot, icon phải nằm trong safe zone 26×26 px trên canvas 32×32 px, và các góc không được nhỏ hơn `Radius.MinimumFillet = 3px`.
