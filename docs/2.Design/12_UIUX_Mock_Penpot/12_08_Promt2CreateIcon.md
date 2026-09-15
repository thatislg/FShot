Dưới đây là bộ prompt mẫu chuẩn cùng quy trình tối ưu để tạo ảnh và chuyển đổi trực tiếp sang mã vector SVG sạch cho ứng dụng.

### 1. Prompt Master tạo cả bộ icon trên một canvas (Showcase Board)

Dùng prompt này nếu bạn muốn AI tạo một bức ảnh tổng hợp trọn bộ icon trên cùng một phong cách để đối chiếu:

Plaintext

```
A professional 2D vector UI icon set sprite sheet for a desktop screenshot tool named "Fshot", designed in an adorable kawaii chibi aesthetic. High-contrast, clean vector art style, flat design with soft claymorphism embossed depth. 

Arranged neatly in a grid with isolated icons, including:
1. Center Mascot: A super cute chubby round blue bird holding a vintage miniature camera with a small smile.
2. Arrow Tool: A chubby curved golden arrow with soft rounded corners and tiny white sparkle highlights.
3. Crop/Selection Tool: A rounded pastel cyan square outline with cute curly looped corner handles and dashed border lines.
4. Pixelate/Blur Tool: A 3x3 grid of colorful rounded square gummy jelly candies in pastel coral and teal.
5. Counter/Step Tool: A puffy round pastel blue speech bubble containing a bold rounded number '1'.
6. Pencil/Pen Tool: A stubby, chubby red wooden pencil with an adorable smiling face and rosy cheeks on the barrel.
7. Highlighter Tool: A fat pastel-yellow marker pen with a slanted soft tip and glossy highlight.
8. Pin Tool: A cute smiling pastel-blue pushpin with tiny dot eyes and a pointed metallic needle.
9. Save Tool: A chubby vintage floppy disk in pastel mint green with rounded edges.

Style specifications: Bold smooth outlines, soft pastel color palette (pastel sky blue, mint green, butter yellow, coral pink), minimal drop shadow, no photo-realistic textures, clean isolated white background, optimized for UI icon design.
```

### 2. Prompt chi tiết để gen từng Icon đơn lẻ (Khuyên dùng để vector hóa sang SVG)

Khi tạo để chuyển thành SVG, hãy tạo **từng icon đơn lẻ trên nền trắng thuần (`pure white background`)**, phong cách **vector flat/sticker** để công cụ AI vector hóa không bị lem màu.

**Mũi tên (Arrow)**

Plaintext

```
A single cute chibi arrow tool icon for a screenshot app UI, chubby curved arrow pointing upwards, rounded edges, soft pastel orange-gold fill with a subtle white highlight dot, thick clean outline, flat vector art style, isolated on a pure white background, no text, no realistic gradients.
```

**Vùng chọn / Cắt (Crop / Selection Box)**

Plaintext

```
A single kawaii selection crop tool icon for app UI, rounded dashed square border in pastel mint and cyan, curly loop nodes at four corners, 2D flat vector sticker style, isolated on a pure white background, minimal and clean.
```

**Làm mờ (Pixelate / Blur)**

Plaintext

```
A single pixelate tool icon for app UI, a 3x3 grid composed of 9 cute rounded squishy jelly candy blocks in pastel peach, mint, and sky blue colors, 2D vector illustration, isolated on pure white background, clean vector lines.
```

**Đánh số thứ tự (Counter Bubble)**

Plaintext

```
A single counter step bubble icon for app UI, a puffy round speech bubble in pastel baby blue with a white bold rounded number '1' inside, tiny cute star sparkle accent, clean vector sticker style, isolated on a pure white background.
```

**Bút vẽ (Pencil)**

Plaintext

```
A single cute stubby pencil icon, fat and short red crayon pencil with a sharp tip, kawaii smiling face with tiny blush dots on the wooden body, bold dark outlines, flat pastel colors, 2D vector graphic, isolated on pure white background.
```

**Bút dạ quang (Highlighter)**

Plaintext

```
A single kawaii fat highlighter marker icon, pastel yellow and black slanted chisel tip, chubby rounded body, clean sticker vector style, soft pastel colors, isolated on pure white background, minimal clean lines.
```

**Ghim màn hình (Pin to screen)**

Plaintext

```
A single cute pushpin icon for app UI, chubby pastel blue pinhead with an adorable kawaii smiling face, slanted metallic tip, clean 2D vector sticker style, flat shading, isolated on pure white background.
```

**Lưu (Save / Floppy Disk)**

Plaintext

```
A single cute vintage 3.5-inch floppy disk icon for save action, pastel mint green plastic shell with rounded corners, white label sticker, chubby cartoon aesthetic, 2D clean vector icon, isolated on pure white background.
```

### 3. Cách chuyển đổi từ ảnh tạo bởi AI sang SVG chuẩn cho Inkscape / Avalonia

Ảnh do AI tạo ra luôn là raster (PNG/JPEG). Để biến thành SVG vector sạch đưa vào Inkscape hoặc Avalonia UI:

1. **Dùng AI Vectorizer chuyên dụng:**
   - Tải ảnh icon nền trắng vào **Vectorizer.AI** hoặc **SVGcode**. Các công cụ này dùng deep learning để dò viền (curve fitting) và nhóm mảng màu thành từng thẻ `<path>` rất gọn thay vì tạo ra hàng nghìn điểm neo rác.
2. **Làm sạch trực tiếp trong Inkscape:**
   - Mở file SVG vừa tạo bằng Inkscape.
   - Quét chọn nền trắng thừa và bấm `Delete`.
   - Chọn từng chi tiết -> mở bảng `Fill and Stroke` (`Shift + Ctrl + F`) để đồng bộ lại mã màu HEX chính xác nếu cần.
   - Dùng lệnh `Path` -> `Simplify` (`Ctrl + L`) nếu đường cong có quá nhiều node không cần thiết.
3. **Lưu file cuối:** Chọn `Save As...` -> định dạng **Optimized SVG** để sẵn sàng nhúng thẳng vào Avalonia qua thẻ `<Image Source="...svg"/>` hoặc lấy chuỗi `Geometry Path Data`.