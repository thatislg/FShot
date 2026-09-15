# Báo cáo: Chuẩn hóa hệ thống màu F-Shot theo Kawaii Claymorphism

- **Ngày thực hiện:** 2026-09-15
- **Phạm vi:** Thiết kế UI/UX — chuẩn bị cho P1.23 Toolbar SVG icons.
- **Trạng thái:** Hoàn thành.

---

## 1. Mục tiêu

Đồng bộ toàn bộ hệ thống màu của F-Shot với concept icon đã định nghĩa trong `docs/2.Design/12_UIUX_Mock_Penpot/12_06_Icon_Asset_Production_Guide.md` (phong cách Kawaii Claymorphism) để:
- Toolbar, overlay, handle, badge, annotation và icon cùng sử dụng một bảng màu duy nhất.
- Tránh tình trạng mỗi phần dùng một palette khác nhau gây rối mắt.
- Tạo cơ sở để triển khai P1.23 với SVG icons tự vẽ theo chuẩn.

---

## 2. Tài liệu đã cập nhật

| File | Thay đổi chính |
| --- | --- |
| `docs/2.Design/12_UIUX_Mock_Penpot/12_01_DesignTokens.md` | Viết lại toàn bộ phần Color Tokens: thêm **Core Palette**, **Semantic Groups** (4 nhóm công cụ), **Functional Colors**, cập nhật giá trị Accent/Overlay/Handle/Toolbar/ToolButton/Annotation/Badge. |

---

## 3. Thay đổi màu sắc chính

### 3.1 Core Palette mới

| Màu | Mã HEX | Vai trò |
| --- | --- | --- |
| Sky Blue | `#7BD5F5` | Màu nhận diện chính F-Shot, accent, handle |
| Butter Yellow | `#FDE047` | Annotation nóng, icon arrow, hover tint |
| Coral Red | `#FF7A70` | Annotation vẽ, pencil, nét nổi bật |
| Mint Macaron | `#86EFAC` | Selection, crop, line, rectangle, circle |
| Bubble Ice Blue | `#BAE6FD` | Text tool, step bubble, system actions nhẹ |
| Lavender Purple | `#DDD6FE` | Blur, pixelate, undo/redo |
| Strawberry Red | `#FB7185` | Cancel, close, error |
| Base Container | `#FFFDF9` | Nền nút/toolbar (kem trắng ngà) |
| Dark Walnut | `#3D2B1F` | Viền icon thay cho đen thuần |

### 3.2 Các token cũ → mới

| Token | Giá trị cũ | Giá trị mới | Lý do |
| --- | --- | --- | --- |
| `Accent.Primary` | `#7C3AED` (tím) | `#7BD5F5` (Sky Blue) | Đồng bộ với overlay dim xanh đang dùng |
| `Accent.Secondary` | `#FF0055` | `#FB7185` (Strawberry Red) | Mềm hơn, phù hợp phong cách |
| `Overlay.DimColor` | `#000000` | `#45B6F7` | Giữ trải nghiệm Flameshot xanh đã test OK |
| `Overlay.DimOpacity` | `0.50` | `0.55` | Tăng nhẹ để nổi trên nền sáng |
| `Handle.FillColor` | `#00E5FF` (cyan neon) | `#7BD5F5` (Sky Blue) | Đồng bộ accent |
| `Handle.StrokeColor` | `#000000` | `#FFFFFF` | Nổi trên overlay xanh |
| `Toolbar.BackgroundColor` | `#1E1E2E` (tối) | `#FFFDF9` (kem) | Phù hợp icon pastel, không chìm |
| `Toolbar.BorderColor` | `#FFFFFF` opacity 10% | `#E2E8F0` opacity 100% | Viền nhẹ nhưng rõ |
| `ToolButton.Default.IconColor` | `#FFFFFF` | `#3D2B1F` | Icon nổi trên nền kem |
| `ToolButton.Active.Background` | `#7C3AED` | `#38BDF8` | Active Accent xanh dương sáng |
| `ToolButton.Hover.Background` | `#FFFFFF` opacity 12% | `#FEF9C3` opacity 100% | Hover vàng bơ nhạt |
| `Annotation.DefaultColor` | `#EF4444` | `#FF7A70` | Coral Red đồng bộ icon |
| `Annotation.Text.Color` | `#EF4444` | `#FF7A70` | Theo Annotation.DefaultColor |
| `Badge.BackgroundColor` | `#7C3AED` | `#38BDF8` | Active Accent |

### 3.3 Semantic Groups cho icon

Mỗi tool sẽ có màu icon theo nhóm:

| Nhóm | Màu chính | Màu viền | Tools |
| --- | --- | --- | --- |
| Annotation | `#FF7A70` | `#B91C1C` | Pencil, Arrow, Text |
| Obfuscation | `#DDD6FE` | `#8B5CF6` | Pixelate, Blur |
| Selection | `#86EFAC` / `#7BD5F5` | `#15803D` / `#1D4ED8` | Selection, Crop, Line, Rectangle, Circle |
| System | `#BAE6FD` / `#FB7185` | `#0284C7` | Save, Copy, Undo, Redo, Cancel |

---

## 4. Hệ quả đối với code hiện tại

- Code hiện tại đang dùng overlay dim xanh `#45B6F7` và toolbar tối tạm thời. Khi triển khai P1.23, cần cập nhật `Toolbar.draw` và các brush trong `CaptureCanvas.axaml.fs` để map về token mới.
- Handle hiện đang là cyan neon + viền đen; cần chuyển sang Sky Blue + viền trắng.
- Annotation default color trong code (`#EF4444`) cần đổi thành `#FF7A70` để đồng bộ.
- Badge trong code hiện có thể khác màu accent cũ; cần chuyển sang `#38BDF8`.

> **Ghi chú:** Chưa cập nhật code ngay lập tức để tránh xáo trộn giữa chừng. Sẽ áp dụng đồng loạt khi triển khai P1.23.

---

## 5. Cách tạo icon theo chuẩn (tóm tắt cho bạn)

Dựa trên `12_06_Icon_Asset_Production_Guide.md`, quy trình tạo 1 icon:

1. **Xác định tool thuộc nhóm ngữ nghĩa** → chọn màu chính + viền từ bảng Semantic Groups.
2. **Lấy prompt từ catalog** trong `12_06_Icon_Asset_Production_Guide.md` (Phần 4).
3. **Thêm prefix `[STYLE MASTER SPECIFICATION]`** ở đầu prompt.
4. **Gen ảnh PNG nền trắng** bằng Midjourney/FLUX/DALL-E với cụm `isolated on pure white background, flat vector colors`.
5. **Vector hóa** bằng Vectorizer.AI hoặc SVGcode.
6. **Làm sạch trong Inkscape**: xóa nền trắng, resize page to selection, simplify path, lưu Optimized SVG.
7. **Copy chuỗi `d="..."`** của path vào code Avalonia (`PathIcon` / `StreamGeometry`).

Nếu bạn muốn bỏ qua AI gen ảnh, có thể vẽ trực tiếp SVG path trong Inkscape dựa trên mô tả hình học trong catalog.

---

## 6. Tiếp theo

1. Bạn có thể bắt đầu tạo icon đầu tiên theo quy trình trên (khuyên bắt đầu với **Pencil** vì nó thuộc nhóm Annotation, hình học đơn giản).
2. Tôi sẽ triển khai P1.23 code (Toolbar SVG icons) sau khi có ít nhất 1 icon SVG sample hoặc bạn yêu cầu triển khai bằng placeholder geometry trước.
