# Bộ icon Kawaii Lite cho F-Shot Toolbar

Bộ icon này được tạo thủ công dưới dạng SVG đơn giản, tuân thủ hệ thống màu Kawaii Claymorphism đã định nghĩa trong `12_01_DesignTokens.md` và `12_06_Icon_Asset_Production_Guide.md`.

## Đặc điểm

- **ViewBox:** 32×32 px (chuẩn toolbar icon).
- **Safe zone:** tất cả hình vẽ nằm trong vùng ~26×26 px ở trung tâm.
- **Style:** fill màu pastel theo nhóm semantic + viền đậm, bo góc tối thiểu 1.5 px.
- **Mỗi file SVG:** có thể parse thành `StreamGeometry` hoặc `SKPath` để vẽ trong Avalonia.
- **Đây là phiên bản Lite:** chưa đầy đủ chi tiết Kawaii như mặt cười, bóng đổ, highlight specular; nhưng đã đồng bộ màu sắc và dễ nhận diện ở 16 px.

## Mapping màu theo nhóm

| File | Tool | Nhóm semantic | Màu fill | Màu viền |
| --- | --- | --- | --- | --- |
| `icon_selection.svg` | SelectionTool | Selection | `#86EFAC` Mint | `#15803D` |
| `icon_pencil.svg` | PencilTool | Annotation | `#FF7A70` Coral | `#B91C1C` |
| `icon_arrow.svg` | ArrowTool | Annotation | `#FDE047` Butter | `#B45309` |
| `icon_line.svg` | LineTool | Selection | `#86EFAC` Mint | `#15803D` |
| `icon_rectangle.svg` | RectangleTool | Selection | `#7BD5F5` Sky | `#1D4ED8` |
| `icon_circle.svg` | CircleTool | Selection | `#F472B6` Pink | `#9D174D` |
| `icon_marker.svg` | MarkerTool | Obfuscation | `#FACC15` Yellow | `#B45309` |
| `icon_text.svg` | TextTool | Annotation | `#FDBA74` Peach | `#9A3412` |
| `icon_pixelate.svg` | PixelateTool | Obfuscation | đa màu candy | đa màu |

## Cách dùng trong code

1. Đọc nội dung SVG bằng `StreamReader` hoặc embed vào F# string literal.
2. Trích xuất chuỗi `d="..."` từ thẻ `<path>`.
3. Tạo `StreamGeometry` hoặc parse qua `SKPath.ParseSvgPathData` trong SkiaSharp.
4. Vẽ với màu fill và stroke tương ứng.

## Lưu ý

- Các file cũ trong `assets/icon/` vẫn được giữ nguyên để tham khảo.
- Sau này nếu có icon AI chuẩn Kawaii đầy đủ, có thể thay thế file trong thư mục này mà không cần đổi code (giữ nguyên tên file và viewBox 32×32).
