Bạn nhận xét rất chuẩn xác. Để chuyển toàn bộ dự án từ concept sang **tệp vector SVG sạch bằng AI** hoặc dựng trên **Avalonia UI**, ta cần 3 tài liệu đặc tả hoàn chỉnh:

1. **Bộ Style DNA (Toàn cảnh hệ thống):** Quy định triệt để về ánh sáng, tỷ lệ, bảng màu và các trạng thái nút (Hover, Active, Disabled).
2. **Catalog đặc tả đầy đủ 100% các icon (Kể cả các icon phụ của Flameshot):** Từng icon phải có prompt chi tiết và chỉ dẫn kỹ thuật hình học riêng.
3. **Quy trình chuẩn hóa Vector/SVG với AI:** Cách prompt để AI sinh ra vector sạch nhất, không dính gradient rác.

Dưới đây là bản đặc tả toàn diện (Master Specification) để bạn lưu trữ và tái sử dụng cho bất kỳ AI nào:

# PHẦN 1: STYLE DNA TOÀN CẢNH (GLOBAL DESIGN SYSTEM)

Để hoàn chỉnh phần **Style DNA Toàn cảnh (Global Design System)** thành một quy chuẩn kỹ thuật cấp hệ thống (Production-ready Design System), bạn nên bổ sung thêm **4 trụ cột** còn thiếu dưới đây.

Những bổ sung này sẽ giải quyết triệt để vấn đề: AI gen icon bị lệch kích thước, không khớp grid, icon bị "chìm" khi đặt vào thanh công cụ Avalonia, hoặc thiếu định nghĩa về các trạng thái tương tác (State Tokens).

### 1. Hệ thống lưới & Vùng an toàn (Grid & Keylines)

Khi thiết kế icon cho phần mềm desktop, nếu không khóa kích thước hình học, AI sẽ vẽ cái quá to, cái quá nhỏ, khiến thanh công cụ lộn xộn.

- **Canvas chuẩn:** $32 \times 32\text{ px}$ (chuẩn UI desktop) hoặc $48 \times 48\text{ px}$ (High-DPI).
- **Live Area & Padding:**
  - **Khuôn vẽ thực tế (Safe Zone):** Tối đa $26 \times 26\text{ px}$.
  - **Vùng đệm an toàn (Padding):** Tối thiểu $3\text{ px}$ xung quanh 4 mép canvas để icon không bao giờ bị cắt cụt bóng đổ hoặc viền mép khi render trong Avalonia.
- **Keyline Shapes (Hình học định khung):**
  - Icon dạng tròn (Circle/Bubble): Đường kính $24\text{ px}$.
  - Icon dạng vuông bo góc (Squircle/Disk): Kích thước $22 \times 22\text{ px}$ với bán kính bo góc $R = 6\text{ px}$.
  - Icon dạng chữ nhật đứng/ngang (Pencil/Marker): Chiều dài tối đa $26\text{ px}$, bề ngang $16\text{ px}$.

### 2. Quy chuẩn cấu trúc nút & Trạng thái tương tác (UI States & Container)

Một icon trên thanh công cụ không đứng một mình, nó luôn nằm trong một "đĩa nút" (Button Slot). Cần định nghĩa rõ các trạng thái này để khi viết XAML/CSS cho Avalonia không bị lúng túng:

- **Container Base (Đế nút mặc định):**
  - Hình dạng: Hình tròn hoặc Squircle bo tròn $R = 10\text{ px}$.
  - Màu nền: Màu kem trắng ngà `#FFFDF9` hoặc xám pastel nhạt `#F1F5F9`.
  - Viền nút: 1px màu xám tro nhẹ `#E2E8F0`.
  - Bóng đáy: `BoxShadow="0 4 8 0 #1A000000"` (bóng mờ 10%).
- **Hover State (Khi di chuột qua):**
  - Nút hơi phóng to nhẹ ($105\%$ scale).
  - Nền sáng rực lên hoặc chuyển nhẹ sang màu vàng bơ nhạt `#FEF9C3`.
  - Đốm sáng phản quang trên icon tăng độ tương phản.
- **Active / Selected State (Khi công cụ đang được chọn để vẽ):**
  - Nút lún xuống (dịch chuyển trục Y xuống $1\text{ px}$ - tạo cảm giác ấn phím silicon đồ chơi).
  - Viền nút đổi sang màu xanh biển pastel đậm `#38BDF8` với độ dày $2\text{ px}$.
  - Nền chuyển sang tone pastel của chính công cụ đó (ví dụ: đang chọn bút dạ quang thì nền nút chuyển vàng nhạt `#FEF08A`).
- **Disabled State (Khi không thể sử dụng - ví dụ: Undo khi chưa vẽ gì):**
  - Độ mờ đục toàn phần (`Opacity`) giảm xuống $40\%$.
  - Màu sắc chuyển sang tone xám chì nhạt (desaturated grey `#94A3B8`).

### 3. Tỷ lệ & Mật độ chi tiết (Visual Density & Simplicity Cap)

AI tạo hình (đặc biệt là DALL-E hay Midjourney) rất dễ bị "thừa chi tiết" (over-detailed) khiến ảnh thu nhỏ lại trên màn hình desktop bị nát hình. Ta phải đặt trần giới hạn chi tiết:

- **Quy tắc "Không quá 3 mảng màu chính" (Color Cap):** Mỗi icon đơn lẻ chỉ chứa tối đa 3-4 mã màu (1 màu nền chính, 1 màu bóng, 1 màu viền, 1 màu highlight). Không dùng họa tiết hoa văn li ti.
- **Quy tắc nhận diện ở kích thước nhỏ (16px Glancing Test):** Khi thu nhỏ xuống 16px, hình bóng đen (silhouette) của icon vẫn phải nhận diện được ngay là cái gì (cây bút chì, mũi tên hay con dấu).
- **Độ cong tối thiểu (Corner Radius Floor):** Không chấp nhận bất kỳ góc lượn nào có bán kính nhỏ hơn $3\text{ px}$.

### 4. Quy chuẩn kỹ thuật xuất file & Lớp vector (Export & Semantic Layering)

Phục vụ trực tiếp cho việc đưa vào Inkscape và code Avalonia:

- **Cấu trúc 3 lớp chuẩn (3-Layer Architecture):**
  - `Layer 1: Shadow & Base Container` (Bóng đổ và đế đĩa nút).
  - `Layer 2: Silhouette & Body` (Thân khối chính của icon, nét viền).
  - `Layer 3: Highlights & Facial Details` (Đốm sáng trắng, mắt, má hồng).
- **Định dạng màu:** Sử dụng mã màu RGB/HEX thuần túy, tuyệt đối **không dùng CMYK** để tránh bị xỉn màu khi render qua SkiaSharp của Avalonia.
- **Không dùng Filter Blur phức tạp của SVG:** Vì bộ render SkiaSharp trên Linux/macOS đôi khi render filter `<feGaussianBlur>` rất tốn tài nguyên GPU, nên toàn bộ hiệu ứng chuyển khối phải được dựng bằng các mảng vector đặc (solid shapes) xếp chồng hoặc gradient tuyến tính đơn giản.

### Khối Master Style DNA hoàn chỉnh (Đã tích hợp toàn bộ các yếu tố trên)

Bạn có thể thay thế đoạn Master Prefix cũ bằng bản đặc tả hoàn chỉnh này:

Plaintext

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



# PHẦN 2: CATALOG ĐẦY ĐỦ CÁC ICON CỦA FLAMESHOT (PROMPT ĐƠN LẺ TỪNG ICON)



**Có, rất cần.** Trước khi liệt kê chi tiết từng icon cụ thể, bạn cần một **khung kiến trúc phân loại công cụ (Tool Categorization Framework & Extension Blueprint)**.

Khung này đóng vai trò như một bộ "luật quy hoạch". Khi bổ sung thêm bất kỳ công cụ mới nào trong tương lai (ví dụ: công cụ làm mờ khuôn mặt tự động, trích xuất text OCR, quay video màn hình, thước đo pixel, v.v.), bạn hoặc AI chỉ cần đối chiếu vào bảng tiêu chí này là tự động ra ngay màu sắc, góc xoay và biểu cảm mà không phá vỡ tính đồng bộ của toàn bộ ứng dụng.

Dưới đây là **Phần 2.0: Overview & Kiến trúc phân loại công cụ mở rộng**:

### 1. Phân nhóm ngữ nghĩa & Ma trận màu sắc (Semantic Grouping Matrix)

Mỗi công cụ khi thêm vào Fshot bắt buộc phải rơi vào 1 trong 4 nhóm ngữ nghĩa. Nhóm ngữ nghĩa sẽ quyết định **Tone màu chủ đạo (Primary Accent)** của icon đó:

- **Nhóm 1: Annotation & Draw (Công cụ vẽ & Đánh dấu)**
  - *Bản chất:* Tác động trực tiếp tạo nét lên ảnh chụp.
  - *Dải màu bắt buộc:* Gam màu nóng/ấm nổi bật (**Coral Red `#FF7A70`**, **Butter Yellow `#FDE047`**).
  - *Quy tắc:* Luôn đặt góc nghiêng $45^\circ$ theo hướng từ dưới-trái lên trên-phải (tư thế bàn tay đang cầm viết).
- **Nhóm 2: Obfuscation & Measurement (Bảo mật, Đo đạc & Tiện ích nội dung)**
  - *Bản chất:* Che giấu dữ liệu (Blur, Pixelate) hoặc đo lường, phân tích (Ruler, Color Picker, OCR).
  - *Dải màu bắt buộc:* Gam màu phân tách dạng kẹo ngọt hoặc tím mộng mơ (**Lavender Purple `#DDD6FE`**, **Candy Mix** gồm cam đào + xanh mint).
  - *Quy tắc:* Sử dụng các cụm hình học module lặp lại (mảng lưới $3 \times 3$, hạt sương, bong bóng).
- **Nhóm 3: Selection & Canvas Control (Thao tác khung & Vùng chọn)**
  - *Bản chất:* Tương tác với không gian màn hình (Crop, Resize, Move, Fullscreen).
  - *Dải màu bắt buộc:* Gam màu lạnh công nghệ (**Mint Macaron `#86EFAC`**, **Sky Blue `#7BD5F5`**).
  - *Quy tắc:* Luôn có yếu tố đường nét đứt (dashed stroke) hoặc các điểm mấu chốt (handle nodes) ở 4 góc.
- **Nhóm 4: System & Output Actions (Hành động hệ thống & Xuất file)**
  - *Bản chất:* Kết thúc luồng chụp (Lưu, Copy, Upload, Ghim, Settings, Cancel).
  - *Dải màu bắt buộc:* Màu trung tính hoặc màu trạng thái hành động (**Ice Blue `#BAE6FD`**, **Strawberry Red `#FB7185`** cho hủy, **Grass Green** cho xác nhận).
  - *Quy tắc:* Không dùng tư thế vẽ hay nét đứt, biểu tượng mang tính đồ vật tĩnh (vật thể đóng gói, đĩa, kẹp, rương, bánh xe răng cưa).

### 2. Quy tắc nhân cách hóa có điều kiện (Anthropomorphic Rules)

Trong phong cách Chibi, việc lạm dụng mắt-mũi-miệng cho tất cả mọi thứ sẽ biến giao diện thành một "nhà trẻ hỗn loạn", rất khó nhìn khi thu nhỏ. Bạn cần quy định rõ điều kiện được phép gắn mặt:

- **Được phép gắn biểu cảm (Gương mặt Kawaii):**
  - Chỉ áp dụng cho các **vật thể đơn khối có tính chủ động thao tác** (ví dụ: Cây bút chì, Chiếc ghim cắm, Con trỏ chuột, Chú chim mascot).
  - Cấu trúc mặt chuẩn: 2 chấm tròn mắt xếch nhẹ + 2 má hồng + miệng cười chữ "w" hoặc "v".
- **Tuyệt đối KHÔNG gắn gương mặt:**
  - Các công cụ mang tính chất **lưới/khung viền** (Crop selection, Pixelate, Khung chữ nhật, Thước đo).
  - Các biểu tượng **hành động khẩn cấp/hệ thống** (Nút Cancel chữ X, Nút Undo/Redo, Đĩa lưu file). Những icon này phải giữ bề mặt trơn nhẵn như thạch dẻo để đảm bảo thị giác rõ ràng nhất.

### 3. Công thức Prompt chuẩn cho công cụ mới (Extensible Prompt Formula)

Để sau này khi nghĩ ra một tool mới (ví dụ: *Color Picker / Ống hút màu*), bạn chỉ cần điền vào công thức sau là AI sẽ tạo ra đúng chuẩn:

Plaintext

```
[FORMULA CHO TOOL MỚI]
[STYLE MASTER SPECIFICATION] 
A single [Tên công cụ] icon for screenshot app UI.
- Subject: A cute, chubby, squishy [Vật thể đời thực được Chibi hóa] representing [Ý nghĩa chức năng].
- Geometry: Ultra-rounded edges, 2-head proportion, tilted at [Góc nghiêng 45 độ hoặc Đứng thẳng].
- Color Tokens: Primary body in [Mã màu nhóm ngữ nghĩa], highlights in Pure White, bold colored outlines in [Mã viền đậm tương ứng].
- Character Detail: [Có mặt Kawaii với chấm mắt má hồng / Hoặc Bề mặt trơn mịn bóng nhẹ].
- Composition: Centered, isolated on pure white background (#FFFFFF), flat color separation, clean vector sticker style, zero realistic textures.
```

### 4. Bảng Checklist kiểm duyệt icon mới (Production QA Checklist)

Trước khi đưa một icon mới vào code Avalonia, đối chiếu qua 4 tiêu chí nhanh:

1. **Kiểm tra tương phản nền trắng:** Tắt bóng đổ đi, viền ngoài có đủ đậm để phân biệt rõ với nền trắng của canvas không?
2. **Kiểm tra độ nặng chi tiết:** Đếm số mảng màu trên thân icon có vượt quá 4 màu không? (Nếu vượt quá -> gộp mảng bớt).
3. **Kiểm tra góc xoay (Orientation Consistency):** Các công cụ cầm tay (bút, dao, ống hút màu) đã xoay nghiêng đúng góc $45^\circ$ chưa? Các công cụ khung/nút đã đặt thẳng đứng chưa?
4. **Kiểm tra thu nhỏ (16px Silhouette Test):** Thu nhỏ về kích cỡ ngón tay út, hình dạng tổng thể có bị biến thành một "cục màu nhòe" không?

Bổ sung bộ khung này lên đầu Phần 2 sẽ giúp hệ thống của bạn mở rộng lên 30 hay 50 công cụ trong tương lai mà giao diện vẫn ngăn nắp, đồng bộ tuyệt đối.



Cụ thể 1 số tools 

### Nhóm 1: Công cụ chú thích (Annotation Tools)

#### 1. Mũi tên (Arrow Tool)

- **Ý tưởng hình học:** Mũi tên mập uốn lượn hình chữ C nhẹ, đầu tam giác bo tròn mép, gắn 1 đốm sao 4 cánh nhỏ xíu ở đỉnh.

- **Màu sắc:** Vàng bơ (`#FDE047`), viền nâu đồng (`#B45309`).

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single chibi arrow tool icon. A chubby, short, curved golden arrow pointing upwards to the right. The arrow head has soft rounded corners with a cute white specular highlight dot. Thick bold deep-brown outline, soft warm clay shading, isolated on pure white background.`

#### 2. Bút vẽ tự do (Pencil / Freehand)

- **Ý tưởng hình học:** Cây bút chì sáp lùn tỉ lệ 1:1, thân đỏ cam, có 2 mắt chấm tròn và 2 vệt má hồng. Đầu ngòi chì nhọn tròn.

- **Màu sắc:** Thân đỏ cam (`#FF7A70`), đầu gỗ màu be (`#FEF3C7`), viền nâu đậm.

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single cute chubby pencil icon. Stubby short wooden pencil body in coral-red color, kawaii smiling face with two tiny dot eyes and pink blush cheeks on the barrel. Sharp but rounded wooden nib drawing a tiny squiggle line below. Thick smooth outlines, isolated on pure white background.`

#### 3. Bút dạ quang (Highlighter / Marker)

- **Ý tưởng hình học:** Thân bút ngắn tròn béo ú, ngòi vát chéo nghiêng 45 độ màu vàng neon. Có nắp bút cài lỏng ở đuôi.

- **Màu sắc:** Thân đen mờ bo tròn, ngòi và viền dạ quang màu vàng chanh (`#FACC15`).

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single chubby highlighter marker icon. Fat, rounded pastel-yellow marker body with a wide slanted chisel tip. Glossy highlight on the side, bold clean dark outline, soft clay texture, isolated on pure white background.`

#### 4. Đường thẳng (Line Tool)

- **Ý tưởng hình học:** Một thanh kẹo dẻo dài thẳng đứng nhưng bo tròn 2 đầu, chính giữa có một ngôi sao nhỏ lấp lánh.

- **Màu sắc:** Xanh ngọc mint (`#86EFAC`), viền xanh rừng (`#15803D`).

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single straight line drawing tool icon. A clean, straight rounded rod with completely rounded pill-shaped ends in pastel mint green color, with tiny white star sparkles. Thick bold outline, isolated on pure white background.`

#### 5. Khung hình chữ nhật rỗng (Rectangle Tool)

- **Ý tưởng hình học:** Một khung tranh vuông bo góc cực lớn, các góc phồng lên như bong bóng cao su.

- **Màu sắc:** Xanh da trời pastel (`#7BD5F5`), viền xanh đậm.

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single rounded rectangle tool icon. A thick hollow rounded square frame with chubby pill-shaped borders and puffy circular corners in pastel blue, bold smooth dark outline, isolated on pure white background.`

#### 6. Hình tròn / Elip (Circle Tool)

- **Ý tưởng hình học:** Một chiếc phao bơi mini hoặc bánh donut trơn tròn xoe phồng căng.

- **Màu sắc:** Hồng đào pastel (`#F472B6`), viền hồng sẫm (`#9D174D`).

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single hollow circle tool icon. A chubby doughnut-shaped ring with a hollow center in vibrant pastel pink, soft specular reflection on top-left, thick smooth dark outline, isolated on pure white background.`

### Nhóm 2: Công cụ che chắn & Định danh (Utility Tools)

#### 7. Làm mờ dạng ô vuông (Pixelate Tool)

- **Ý tưởng hình học:** Bảng lưới 3x3 gồm 9 viên kẹo dẻo hình vuông bo góc, màu sắc xen kẽ nhau (cam, xanh, hồng).

- **Màu sắc:** Coral (`#FF7A70`), Mint (`#86EFAC`), Sky Blue (`#7BD5F5`).

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single pixelate mosaic tool icon. A neat 3x3 grid composed of 9 tiny squishy rounded square jelly candies in alternating pastel colors (coral pink, mint green, and baby blue). Cute sticker vector, bold outline, isolated on pure white background.`

#### 8. Làm mờ dạng nhòe (Blur Tool)

- **Ý tưởng hình học:** Một đám mây nhỏ tròn phồng xốp, đang tỏa ra những hạt sương mờ lung linh.

- **Màu sắc:** Tím hoa cà pastel (`#DDD6FE`) và Trắng sữa (`#FFFDF9`).

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single blur tool icon. A fluffy, puffy chubby little cloud shape in soft lavender and white, emitting tiny soft bubbles around it, bold dark rounded outlines, isolated on pure white background.`

#### 9. Đánh số bước thứ tự (Step Bubble / Counter)

- **Ý tưởng hình học:** Bong bóng thoại tròn xoe, có một đuôi nhọn nhỏ vểnh sang góc, bên trong là số '1' font tròn đậm nét.

- **Màu sắc:** Xanh băng tuyết (`#BAE6FD`), số màu trắng nổi viền xanh sẫm.

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single step counter badge icon. A puffy circular speech bubble in pastel baby blue with a tiny cute tail, containing a bold, rounded, white number '1' inside. Sparkling highlights, thick bold outline, isolated on pure white background.`

#### 10. Ghim lên màn hình (Pin to screen)

- **Ý tưởng hình học:** Chiếc đinh ghim bảng đầu nhựa tròn phồng, có khuôn mặt cười tít mắt và chiếc kim sắt ngắn cùn an toàn.

- **Màu sắc:** Nhựa xanh dương (`#7BD5F5`), kim xám bạc (`#CBD5E1`).

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single pin tool icon. A chubby pushpin with a round bulbous plastic head in sky blue, an adorable smiling kawaii face on the pin head, and a short, stubby rounded silver needle tip. Thick smooth outline, isolated on pure white background.`

#### 11. Nhập văn bản (Text Tool)

- **Ý tưởng hình học:** Chữ cái **"A"** in hoa béo tròn, bụng phồng to như gối hơi, có 2 mắt chấm tròn đáng yêu.

- **Màu sắc:** Vàng cam đào (`#FDBA74`), viền nâu hạt dẻ.

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single text tool icon. A super chubby bold capital letter 'A' with swollen, pillow-like rounded limbs, wearing a tiny happy face expression, soft drop shadow, bold outlines, isolated on pure white background.`

### Nhóm 3: Công cụ vùng chọn & Hành động (Selection & Action Tools)

#### 12. Vùng chọn / Cắt hình (Crop / Rect Selection)

- **Ý tưởng hình học:** Khung viền nét đứt bo tròn góc, ở 4 góc là 4 vòng xoắn xoay tròn (curly loop) như tai thỏ.

- **Màu sắc:** Xanh mint pastel (`#86EFAC`) viền xanh biển.

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single crop selection tool icon. A rounded squircle selection box with dashed lines and cute curly looped handles at all four corners, pastel cyan and mint color scheme, flat clean vector look, isolated on pure white background.`

#### 13. Chọn toàn màn hình (Select All / Screen)

- **Ý tưởng hình học:** Chiếc màn hình máy tính CRT cổ điển béo lùn, màn hình cong phồng đang mở to mắt nhìn.

- **Màu sắc:** Thân be xám (`#F1F5F9`), màn hình xanh lơ (`#E0F2FE`).

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single full-screen capture tool icon. A chubby retro desktop computer monitor with an ultra-rounded screen, tiny antenna on top, cute aesthetic, thick smooth outline, isolated on pure white background.`

#### 14. Sao chép vào bộ nhớ đệm (Copy to Clipboard)

- **Ý tưởng hình học:** Hai chiếc bánh quy kẹp hình chữ nhật bo tròn nằm so le nhau, mép có kẹp giấy tròn mini.

- **Màu sắc:** Vàng bơ (`#FEF08A`) và Cam nhạt.

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single copy to clipboard icon. Two overlapping chubby rounded sheets of paper with folded corners, held together by a cute colorful paperclip, soft sticker style, isolated on pure white background.`

#### 15. Lưu vào ổ cứng (Save File)

- **Ý tưởng hình học:** Đĩa mềm 3.5 inch cổ điển màu xanh lá mạ, các góc bo tròn mềm, nhãn dán trắng có 3 vạch kẻ ngang.

- **Màu sắc:** Xanh lá macaron (`#86EFAC`), nhãn trắng kem.

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single save icon. A vintage 3.5-inch floppy disk reimagined as a cute squishy toy with rounded corners, mint macaron green plastic casing, white label sticker, bold outlines, isolated on pure white background.`

#### 16. Hoàn tác & Làm lại (Undo / Redo)

- **Ý tưởng hình học:** Mũi tên vòng cung mập lùn quay ngược lại hình móng ngựa, có gắn cánh thiên thần nhỏ ở đuôi.

- **Màu sắc:** Tím pastel (`#C4B5FD`), viền tím sẫm.

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single undo tool icon. A chubby counter-clockwise curved arrow shaped like a plump horseshoe with rounded edges, soft pastel lilac color, thick dark stroke, isolated on pure white background.`

#### 17. Hủy bỏ / Đóng (Cancel / Exit)

- **Ý tưởng hình học:** Dấu gạch chéo **"X"** mập mạp, hai thanh bắt chéo phồng to như 2 khúc xương mềm cún gặm.

- **Màu sắc:** Đỏ dâu tây (`#FB7185`), viền đỏ sẫm.

- **Prompt chi tiết:**

  > `[STYLE MASTER SPECIFICATION] A single cancel cross icon. A chubby, puffed-up 'X' shape with bulbous rounded tips, soft strawberry red color, bold smooth dark outline, isolated on pure white background.`

# PHẦN 3: CÁCH DÙNG AI CHUYỂN TRỰC TIẾP RA FILE SVG SẠCH

Để biến các mô tả trên thành mã vector SVG không bị lỗi node hay gradient bẩn:

1. **Sinh ảnh gốc (PNG):**
   - Sử dụng Midjourney v6 / FLUX.1 / DALL-E với prompt đơn lẻ ở trên.
   - *Lưu ý sống còn:* Luôn thêm cụm từ `"isolated on pure white background, flat vector colors, no complex gradients, no photographic noise"` vào cuối prompt.
2. **Dùng AI Vectorizer chuyên sâu (Không dùng chức năng Trace Bitmap thông thường):**
   - Tải ảnh PNG nền trắng lên **Vectorizer.AI** (chạy mô hình Deep Vector Networks).
   - Công cụ này sẽ tự nhận diện:
     - Nét viền đậm -> Biến thành các thẻ `<path>` viền kín có độ dày đồng nhất.
     - Mảng màu phẳng -> Biến thành các shape polygon/bezier tối ưu nhất, không bị hiện tượng hàng ngàn mảnh tam giác li ti.
3. **Làm sạch nhanh trong Inkscape trước khi nạp vào Avalonia:**
   - Mở file SVG xuất ra từ Vectorizer.AI vào Inkscape.
   - Dùng công cụ chọn (`S`), bấm vào nền trắng và xóa (`Delete`).
   - Chọn toàn bộ icon -> Nhấn `Ctrl + Shift + R` (Resize page to selection) để đưa canvas về sát mép icon.
   - `Save As...` -> Chọn **Optimized SVG** (Bật tích chọn *Remove metadata*, *Convert styles to XML attributes*, *Collapse groups*). File này bạn có thể nạp thẳng vào Avalonia qua `<Image Source="...svg"/>` hoặc copy chuỗi `d="..."` của thẻ path vào `PathIcon`.