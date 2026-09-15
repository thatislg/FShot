# Hướng dẫn sản xuất Icon Asset cho F-Shot

Tài liệu này tổng hợp toàn bộ quy trình, tiêu chuẩn và prompt để tạo bộ icon cho F-Shot: từ icon ứng dụng MSIX, icon trên toolbar, đến cách chuyển ảnh AI thành vector SVG sạch để nhúng vào Avalonia UI.

---

## PHẦN 1: TIÊU CHUẨN ICON ỨNG DỤNG MSIX

Khi đóng gói ứng dụng đưa lên MSIX (Microsoft Store hoặc enterprise Windows 10/11), Windows App Packaging sử dụng cơ chế **Visual Elements** trong `AppxManifest.xml`.

### 1.1 Kích thước và tỷ lệ Scale

Windows tự động co giãn theo DPI với các hệ số: **100%, 125%, 150%, 200%, 400%**.

| Nhóm Icon | Kích thước cơ sở (100%) | Các kích thước theo Scale | Vị trí hiển thị |
| --- | --- | --- | --- |
| Square44x44Logo | 44×44 px | 44, 55, 66, 88, 176 px | Taskbar, Start Menu, Alt+Tab |
| Square150x150Logo | 150×150 px | 150, 188, 225, 300, 600 px | Medium Tile, Search preview |
| Square71x71Logo (tùy chọn) | 71×71 px | 71, 89, 107, 142, 284 px | Small Tile |
| Square310x310Logo (tùy chọn) | 310×310 px | 310, 388, 465, 620, 1240 px | Large Tile |
| StoreLogo | 50×50 px | 50, 63, 75, 100, 200 px | Store, trang cài đặt |
| SplashScreen | 620×300 px | 620, 775, 930, 1240, 2480 px | Màn hình khởi động |

Quy ước đặt tên:
- `Square44x44Logo.scale-100.png`
- `Square150x150Logo.scale-200.png`
- `StoreLogo.scale-100.png`

Mẹo: chỉ cần thiết kế 1 file gốc 1024×1024 px PNG nền trong suốt hoặc SVG, sau đó dùng Asset Generator của Visual Studio trong `Package.appxmanifest` để tự động sinh các kích thước.

### 1.2 Vùng an toàn (Safe Margin)

- **Square44x44Logo**: nội dung chính chiếm 60%–66% diện tích (~28×28 px), để trống 8 px viền ngoài.
- **Square150x150Logo**: nội dung chính chiếm 50%–60% ở tâm (~75–90 px).
- Không gắn text/tên app bên trong icon nhỏ.
- Không vẽ sẵn bo góc cứng; để nền trong suốt để Windows 11 tự áp dụng bo góc theo ngữ cảnh.

### 1.3 Màu sắc và định dạng

- **Hệ màu:** RGB / sRGB, 32-bit RGBA.
- **Định dạng xuất:** PNG có kênh Alpha trong suốt.
- **Dung lượng:** icon nhỏ < 50 KB, icon vừa/lớn < 300 KB, tổng thư mục Assets < 5 MB.
- **Tối ưu:** dùng `oxipng`, `pngquant` hoặc Optimize PNG của Inkscape để xóa metadata thừa.
- **Light/Dark theme:** MSIX hỗ trợ hậu tố như `targetsize-24_altform-lightunplated.png` (nền sáng) và `_altform-unplated.png` (nền tối).

### 1.4 Khai báo trong AppxManifest.xml

```xml
<uap:VisualElements
  DisplayName="Fshot"
  Description="Fshot Screenshot Tool"
  BackgroundColor="transparent"
  Square150x150Logo="Assets\Square150x150Logo.png"
  Square44x44Logo="Assets\Square44x44Logo.png">
  <uap:DefaultTile ShortName="Fshot" Square71x71Logo="Assets\Square71x71Logo.png"/>
  <uap:SplashScreen Image="Assets\SplashScreen.png" BackgroundColor="#111827"/>
</uap:VisualElements>
```

---

## PHẦN 2: STYLE DNA CHO ICON TRÊN TOOLBAR

### 2.1 Lưới & vùng an toàn

- **Canvas chuẩn:** 32×32 px (hoặc 48×48 px cho High-DPI).
- **Safe Zone:** tối đa 26×26 px.
- **Padding:** tối thiểu 3 px xung quanh 4 mép.
- **Keyline Shapes:**
  - Tròn: đường kính 24 px.
  - Vuông bo góc (Squircle): 22×22 px, bán kính bo góc R = 6 px.
  - Chữ nhật đứng/ngang (Pencil/Marker): dài tối đa 26 px, rộng 16 px.

### 2.2 Cấu trúc nút & trạng thái tương tác

Mỗi icon toolbar nằm trong một Button Slot với các trạng thái:

- **Container Base:** hình tròn hoặc Squircle R = 10 px, nền `#FFFDF9` hoặc `#F1F5F9`, viền 1px `#E2E8F0`, bóng `0 4 8 0 #1A000000`.
- **Hover:** scale 105%, nền chuyển nhẹ `#FEF9C3`.
- **Active/Selected:** nút lún xuống 1 px, viền `#38BDF8` dày 2 px, nền tone pastel của tool đang chọn.
- **Disabled:** opacity 40%, màu xám `#94A3B8`.

### 2.3 Giới hạn chi tiết

- Tối đa 3–4 mảng màu chính mỗi icon.
- Phải nhận diện được ở kích thước 16 px (Silhouette Test).
- Bán kính bo góc tối thiểu 3 px, không góc vuông 90 độ sắc cạnh.

### 2.4 Xuất file vector

- **Cấu trúc 3 lớp:** Shadow/Base → Body/Stroke → Highlight/Face.
- Dùng mã HEX, không dùng CMYK.
- Không dùng SVG filter blur phức tạp; dùng shape đặc xếp chồng hoặc gradient tuyến tính đơn giản.

### 2.5 Master Style DNA (Prompt prefix dùng cho AI)

```
[STYLE MASTER SPECIFICATION: PRODUCTION KAWAII CLAYMORPHISM]
- Core Philosophy: Cute 2.5D Chibi Sticker Vector, Squishy Claymorphism, Clean UI Assets.
- Canvas & Metrics: Standard 32x32px coordinate system, 3px outer safety padding, max bounding shape 26x26px. Visual density capped at 3 primary flat tones per asset for instant 16px micro-recognition.
- Geometry & Radii: 2-head chubby proportion, minimum fillet radius 3px, zero sharp 90-degree corners, rounded bulbous limbs and pill shapes.
- Lighting Engine: Top-left light source (10 o'clock), subtle warm ambient occlusion, crisp circular specular white highlights on top-left surface.
- Stroke Hierarchy: Uniform bold stroke weight (approx. 2.5px scale), colored outlines using Dark Walnut (#3D2B1F) or Deep Slate Navy (#1E293B). Absolutely NO pure black (#000000).
- Color Architecture (Tokens):
  * Sky Blue: #7BD5F5 | Outline: #1D4ED8
  * Butter Yellow: #FDE047 | Outline: #B45309
  * Coral Red: #FF7A70 | Outline: #B91C1C
  * Mint Macaron: #86EFAC | Outline: #15803D
  * Bubble Ice Blue: #BAE6FD | Outline: #0284C7
  * Base Container: #FFFDF9 | Border: #E2E8F0 | Active Accent: #38BDF8
  * Cheek Blush: #F472B6 | Highlight Specular: #FFFFFF (85% opacity)
- UI Integration Rules: Explicit 3-layer stack (Shadow/Base -> Body/Stroke -> Highlight/Face), solid shapes with hard vector transitions (no complex SVG raster blurs), optimized for SkiaSharp rendering in Avalonia UI.
- Target Output: Isolated asset centered on pure white background (#FFFFFF), high contrast, flat color separation, zero realistic textures, ready for automated SVG vectorization.
```

---

## PHẦN 3: KHUNG PHÂN LOẠI CÔNG CỤ VÀ MÀU SẮC

### 3.1 Ma trận nhóm ngữ nghĩa

Mỗi tool mới bắt buộc thuộc 1 trong 4 nhóm; nhóm quyết định tone màu chủ đạo:

| Nhóm | Bản chất | Dải màu | Quy tắc hình học |
| --- | --- | --- | --- |
| Annotation & Draw | Tạo nét lên ảnh | Coral `#FF7A70`, Butter `#FDE047` | Nghiêng 45° như cầm bút |
| Obfuscation & Measurement | Che giấu, đo đạc | Lavender `#DDD6FE`, Candy Mix | Dùng module lặp lại: lưới 3×3, hạt sương |
| Selection & Canvas Control | Tương tác khung vùng | Mint `#86EFAC`, Sky `#7BD5F5` | Có nét đứt hoặc handle 4 góc |
| System & Output Actions | Lưu, copy, hủy | Ice `#BAE6FD`, Strawberry `#FB7185` | Biểu tượng vật thể tĩnh, không nét vẽ |

### 3.2 Quy tắc nhân cách hóa

- **Được gắn mặt Kawaii:** vật thể đơn khối có tính chủ động (bút chì, ghim, con trỏ, mascot). Mặt chuẩn: 2 chấm mắt + 2 má hồng + miệng cười chữ "w" hoặc "v".
- **Không gắn mặt:** công cụ dạng lưới/khung (Crop, Pixelate, Rectangle, Ruler), biểu tượng hành động hệ thống (Cancel, Undo/Redo, Save).

### 3.3 Công thức Prompt mở rộng cho tool mới

```
[FORMULA CHO TOOL MỚI]
[STYLE MASTER SPECIFICATION]
A single [Tên công cụ] icon for screenshot app UI.
- Subject: A cute, chubby, squishy [Vật thể đời thực được Chibi hóa] representing [Ý nghĩa chức năng].
- Geometry: Ultra-rounded edges, 2-head proportion, tilted at [45 độ hoặc đứng thẳng].
- Color Tokens: Primary body in [Mã màu nhóm ngữ nghĩa], highlights in Pure White, bold colored outlines in [Mã viền đậm tương ứng].
- Character Detail: [Có mặt Kawaii / Hoặc bề mặt trơn mịn bóng nhẹ].
- Composition: Centered, isolated on pure white background (#FFFFFF), flat color separation, clean vector sticker style, zero realistic textures.
```

### 3.4 Checklist kiểm duyệt icon mới

1. **Tương phản nền trắng:** tắt bóng đổ, viền ngoài có đủ đậm để phân biệt không?
2. **Độ nặng chi tiết:** số mảng màu trên thân có vượt quá 4 không?
3. **Góc xoay đồng nhất:** công cụ cầm tay đã nghiêng 45°, khung/nút đã đặt thẳng đứng?
4. **16px Silhouette Test:** thu nhỏ về 16 px, hình dạng tổng thể có bị nhòe thành cục màu không?

---

## PHẦN 4: CATALOG PROMPT CHO TỪNG ICON

### Nhóm 1: Công cụ chú thích (Annotation Tools)

#### 1. Mũi tên (Arrow Tool)

- **Hình học:** mũi tên mập uốn lượn hình chữ C nhẹ, đầu tam giác bo tròn, 1 đốm sao 4 cánh nhỏ ở đỉnh.
- **Màu:** vàng bơ `#FDE047`, viền nâu đồng `#B45309`.

```
[STYLE MASTER SPECIFICATION] A single chibi arrow tool icon. A chubby, short, curved golden arrow pointing upwards to the right. The arrow head has soft rounded corners with a cute white specular highlight dot. Thick bold deep-brown outline, soft warm clay shading, isolated on pure white background.
```

#### 2. Bút vẽ tự do (Pencil / Freehand)

- **Hình học:** bút chì sáp lùn tỉ lệ 1:1, thân đỏ cam, 2 mắt chấm tròn + 2 vệt má hồng, đầu ngòi chì nhọn tròn.
- **Màu:** thân đỏ cam `#FF7A70`, đầu gỗ be `#FEF3C7`, viền nâu đậm.

```
[STYLE MASTER SPECIFICATION] A single cute chubby pencil icon. Stubby short wooden pencil body in coral-red color, kawaii smiling face with two tiny dot eyes and pink blush cheeks on the barrel. Sharp but rounded wooden nib drawing a tiny squiggle line below. Thick smooth outlines, isolated on pure white background.
```

#### 3. Bút dạ quang (Highlighter / Marker)

- **Hình học:** thân bút ngắn tròn béo ú, ngòi vát chéo 45° màu vàng neon, có nắp bút cài lỏng ở đuôi.
- **Màu:** thân đen mờ bo tròn, ngòi và viền dạ quang `#FACC15`.

```
[STYLE MASTER SPECIFICATION] A single chubby highlighter marker icon. Fat, rounded pastel-yellow marker body with a wide slanted chisel tip. Glossy highlight on the side, bold clean dark outline, soft clay texture, isolated on pure white background.
```

#### 4. Đường thẳng (Line Tool)

- **Hình học:** thanh kẹo dẻo dài thẳng đứng nhưng bo tròn 2 đầu, giữa có ngôi sao nhỏ lấp lánh.
- **Màu:** xanh ngọc mint `#86EFAC`, viền xanh rừng `#15803D`.

```
[STYLE MASTER SPECIFICATION] A single straight line drawing tool icon. A clean, straight rounded rod with completely rounded pill-shaped ends in pastel mint green color, with tiny white star sparkles. Thick bold outline, isolated on pure white background.
```

#### 5. Khung hình chữ nhật rỗng (Rectangle Tool)

- **Hình học:** khung tranh vuông bo góc cực lớn, các góc phồng lên như bong bóng cao su.
- **Màu:** xanh da trời pastel `#7BD5F5`, viền xanh đậm.

```
[STYLE MASTER SPECIFICATION] A single rounded rectangle tool icon. A thick hollow rounded square frame with chubby pill-shaped borders and puffy circular corners in pastel blue, bold smooth dark outline, isolated on pure white background.
```

#### 6. Hình tròn / Elip (Circle Tool)

- **Hình học:** phao bơi mini hoặc bánh donut trơn tròn xoe phồng căng.
- **Màu:** hồng đào pastel `#F472B6`, viền hồng sẫm `#9D174D`.

```
[STYLE MASTER SPECIFICATION] A single hollow circle tool icon. A chubby doughnut-shaped ring with a hollow center in vibrant pastel pink, soft specular reflection on top-left, thick smooth dark outline, isolated on pure white background.
```

### Nhóm 2: Công cụ che chắn & Định danh (Utility Tools)

#### 7. Làm mờ dạng ô vuông (Pixelate Tool)

- **Hình học:** bảng lưới 3×3 gồm 9 viên kẹo dẻo hình vuông bo góc, màu xen kẽ.
- **Màu:** Coral `#FF7A70`, Mint `#86EFAC`, Sky Blue `#7BD5F5`.

```
[STYLE MASTER SPECIFICATION] A single pixelate mosaic tool icon. A neat 3x3 grid composed of 9 tiny squishy rounded square jelly candies in alternating pastel colors (coral pink, mint green, and baby blue). Cute sticker vector, bold outline, isolated on pure white background.
```

#### 8. Làm mờ dạng nhòe (Blur Tool)

- **Hình học:** đám mây nhỏ tròn phồng xốp, đang tỏa ra hạt sương mờ lung linh.
- **Màu:** tím hoa cà pastel `#DDD6FE`, trắng sữa `#FFFDF9`.

```
[STYLE MASTER SPECIFICATION] A single blur tool icon. A fluffy, puffy chubby little cloud shape in soft lavender and white, emitting tiny soft bubbles around it, bold dark rounded outlines, isolated on pure white background.
```

#### 9. Đánh số bước thứ tự (Step Bubble / Counter)

- **Hình học:** bong bóng thoại tròn xoe, đuôi nhọn nhỏ vểnh sang góc, bên trong số "1" font tròn đậm nét.
- **Màu:** xanh băng tuyết `#BAE6FD`, số trắng nổi viền xanh sẫm.

```
[STYLE MASTER SPECIFICATION] A single step counter badge icon. A puffy circular speech bubble in pastel baby blue with a tiny cute tail, containing a bold, rounded, white number '1' inside. Sparkling highlights, thick bold outline, isolated on pure white background.
```

#### 10. Ghim lên màn hình (Pin to screen)

- **Hình học:** đinh ghim bảng đầu nhựa tròn phồng, khuôn mặt cười tít mắt, kim sắt ngắn cùn an toàn.
- **Màu:** nhựa xanh dương `#7BD5F5`, kim xám bạc `#CBD5E1`.

```
[STYLE MASTER SPECIFICATION] A single pin tool icon. A chubby pushpin with a round bulbous plastic head in sky blue, an adorable smiling kawaii face on the pin head, and a short, stubby rounded silver needle tip. Thick smooth outline, isolated on pure white background.
```

#### 11. Nhập văn bản (Text Tool)

- **Hình học:** chữ "A" in hoa béo tròn, bụng phồng to như gối hơi, 2 mắt chấm tròn đáng yêu.
- **Màu:** vàng cam đào `#FDBA74`, viền nâu hạt dẻ.

```
[STYLE MASTER SPECIFICATION] A single text tool icon. A super chubby bold capital letter 'A' with swollen, pillow-like rounded limbs, wearing a tiny happy face expression, soft drop shadow, bold outlines, isolated on pure white background.
```

### Nhóm 3: Công cụ vùng chọn & Hành động (Selection & Action Tools)

#### 12. Vùng chọn / Cắt hình (Crop / Rect Selection)

- **Hình học:** khung viền nét đứt bo tròn góc, 4 góc có vòng xoắn xoay tròn (curly loop) như tai thỏ.
- **Màu:** xanh mint pastel `#86EFAC`, viền xanh biển.

```
[STYLE MASTER SPECIFICATION] A single crop selection tool icon. A rounded squircle selection box with dashed lines and cute curly looped handles at all four corners, pastel cyan and mint color scheme, flat clean vector look, isolated on pure white background.
```

#### 13. Chọn toàn màn hình (Select All / Screen)

- **Hình học:** màn hình máy tính CRT cổ điển béo lùn, màn hình cong phồng đang mở to mắt nhìn.
- **Màu:** thân be xám `#F1F5F9`, màn hình xanh lơ `#E0F2FE`.

```
[STYLE MASTER SPECIFICATION] A single full-screen capture tool icon. A chubby retro desktop computer monitor with an ultra-rounded screen, tiny antenna on top, cute aesthetic, thick smooth outline, isolated on pure white background.
```

#### 14. Sao chép vào bộ nhớ đệm (Copy to Clipboard)

- **Hình học:** hai bánh quy kẹp hình chữ nhật bo tròn nằm so le, mép có kẹp giấy tròn mini.
- **Màu:** vàng bơ `#FEF08A`, cam nhạt.

```
[STYLE MASTER SPECIFICATION] A single copy to clipboard icon. Two overlapping chubby rounded sheets of paper with folded corners, held together by a cute colorful paperclip, soft sticker style, isolated on pure white background.
```

#### 15. Lưu vào ổ cứng (Save File)

- **Hình học:** đĩa mềm 3.5 inch cổ điển màu xanh lá mạ, các góc bo tròn mềm, nhãn dán trắng có 3 vạch kẻ ngang.
- **Màu:** xanh lá macaron `#86EFAC`, nhãn trắng kem.

```
[STYLE MASTER SPECIFICATION] A single save icon. A vintage 3.5-inch floppy disk reimagined as a cute squishy toy with rounded corners, mint macaron green plastic casing, white label sticker, bold outlines, isolated on pure white background.
```

#### 16. Hoàn tác & Làm lại (Undo / Redo)

- **Hình học:** mũi tên vòng cung mập lùn quay ngược lại hình móng ngựa, có gắn cánh thiên thần nhỏ ở đuôi.
- **Màu:** tím pastel `#C4B5FD`, viền tím sẫm.

```
[STYLE MASTER SPECIFICATION] A single undo tool icon. A chubby counter-clockwise curved arrow shaped like a plump horseshoe with rounded edges, soft pastel lilac color, thick dark stroke, isolated on pure white background.
```

#### 17. Hủy bỏ / Đóng (Cancel / Exit)

- **Hình học:** dấu gạch chéo "X" mập mạp, hai thanh bắt chéo phồng to như 2 khúc xương mềm cún gặm.
- **Màu:** đỏ dâu tây `#FB7185`, viền đỏ sẫm.

```
[STYLE MASTER SPECIFICATION] A single cancel cross icon. A chubby, puffed-up 'X' shape with bulbous rounded tips, soft strawberry red color, bold smooth dark outline, isolated on pure white background.
```

---

## PHẦN 5: QUY TRÌNH CHUYỂN ẢNH AI SANG SVG SẠCH

### 5.1 Sinh ảnh gốc (PNG)

- Dùng Midjourney v6 / FLUX.1 / DALL-E với prompt đơn lẻ ở trên.
- Luôn thêm vào cuối prompt:
  ```
  isolated on pure white background, flat vector colors, no complex gradients, no photographic noise
  ```

### 5.2 Vector hóa bằng AI

- Tải ảnh PNG nền trắng lên **Vectorizer.AI** (Deep Vector Networks) hoặc **SVGcode**.
- Công cụ tự nhận diện:
  - Nét viền đậm → thẻ `<path>` viền kín độ dày đồng nhất.
  - Mảng màu phẳng → shape polygon/bezier tối ưu, không bị hàng ngàn mảnh tam giác li ti.

### 5.3 Làm sạch trong Inkscape

1. Mở SVG xuất ra trong Inkscape.
2. Dùng công cụ chọn (`S`), bấm nền trắng và xóa (`Delete`).
3. Chọn toàn bộ icon → `Ctrl + Shift + R` (Resize page to selection) để canvas sát mép icon.
4. Mở `Fill and Stroke` (`Shift + Ctrl + F`) để đồng bộ lại mã màu HEX nếu cần.
5. Dùng `Path` → `Simplify` (`Ctrl + L`) nếu đường cong có quá nhiều node thừa.
6. `Save As...` → chọn **Optimized SVG**, bật:
   - Remove metadata
   - Convert styles to XML attributes
   - Collapse groups

### 5.4 Nhúng vào Avalonia

- Copy chuỗi `d="..."` của thẻ `<path>` vào `PathIcon` hoặc `StreamGeometry`.
- Hoặc dùng `<Image Source="...svg"/>` trực tiếp.
- Tuyệt đối không dùng `<feGaussianBlur>`; mọi hiệu ứng chuyển khối phải là shape đặc xếp chồng.

---

## PHẦN 6: PROMPT NGẮN GỌN TỪNG ICON (DÙNG CHO AI GEN ẢNH)

Bộ prompt rút gọn khi không cần ghi đầy đủ màu sắc vào catalog:

```
A single cute chibi arrow tool icon for a screenshot app UI, chubby curved arrow pointing upwards, rounded edges, soft pastel orange-gold fill with a subtle white highlight dot, thick clean outline, flat vector art style, isolated on a pure white background, no text, no realistic gradients.
```

```
A single kawaii selection crop tool icon for app UI, rounded dashed square border in pastel mint and cyan, curly loop nodes at four corners, 2D flat vector sticker style, isolated on a pure white background, minimal and clean.
```

```
A single pixelate tool icon for app UI, a 3x3 grid composed of 9 cute rounded squishy jelly candy blocks in pastel peach, mint, and sky blue colors, 2D vector illustration, isolated on pure white background, clean vector lines.
```

```
A single counter step bubble icon for app UI, a puffy round speech bubble in pastel baby blue with a white bold rounded number '1' inside, tiny cute star sparkle accent, clean vector sticker style, isolated on a pure white background.
```

```
A single cute stubby pencil icon, fat and short red crayon pencil with a sharp tip, kawaii smiling face with tiny blush dots on the wooden body, bold dark outlines, flat pastel colors, 2D vector graphic, isolated on pure white background.
```

```
A single kawaii fat highlighter marker icon, pastel yellow and black slanted chisel tip, chubby rounded body, clean sticker vector style, soft pastel colors, isolated on pure white background, minimal clean lines.
```

```
A single cute pushpin icon for app UI, chubby pastel blue pinhead with an adorable kawaii smiling face, slanted metallic tip, clean 2D vector sticker style, flat shading, isolated on pure white background.
```

```
A single cute vintage 3.5-inch floppy disk icon for save action, pastel mint green plastic shell with rounded corners, white label sticker, chubby cartoon aesthetic, 2D clean vector icon, isolated on pure white background.
```

---

## Tóm tắt quy trình sản xuất

1. Xác định tool thuộc nhóm ngữ nghĩa → chọn tone màu.
2. Chọn prompt từ catalog, thêm `[STYLE MASTER SPECIFICATION]` làm prefix.
3. Sinh ảnh PNG nền trắng.
4. Vector hóa bằng Vectorizer.AI / SVGcode.
5. Làm sạch trong Inkscape và xuất Optimized SVG.
6. Nhúng geometry vào Avalonia UI hoặc lưu vào thư mục Assets cho MSIX.
