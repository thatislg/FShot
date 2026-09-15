Khi đóng gói ứng dụng đưa lên **MSIX** (đặc biệt là để phân phối qua Microsoft Store hoặc cài đặt enterprise trên Windows 10/11), hệ thống Windows App Packaging sử dụng cơ chế **Visual Elements** (khai báo trong file `AppxManifest.xml`).

Dưới đây là toàn bộ tiêu chuẩn chi tiết về kích thước, quy cách màu sắc, dung lượng và nội dung icon cho gói MSIX.

### 1. Kích thước và Tỷ lệ Scale (Size & Scaling Grid)

Windows áp dụng cơ chế tự động co giãn theo DPI màn hình dựa trên các hệ số tỷ lệ: **100%, 125%, 150%, 200%, và 400%**.

Hệ thống MSIX yêu cầu **4 nhóm asset chính** cho icon ứng dụng:

| **Nhóm Icon**                      | **Kích thước cơ sở (100%)** | **Các kích thước cần tạo (px) theo Scale**                   | **Vị trí hiển thị**                                 |
| ---------------------------------- | --------------------------- | ------------------------------------------------------------ | --------------------------------------------------- |
| **Square44x44Logo**                | `44 x 44 px`                | 44×44 (100%), 55×55 (125%), 66×66 (150%), 88×88 (200%), 176×176 (400%) | Taskbar, Start Menu (danh sách All Apps), Alt+Tab   |
| **Square150x150Logo**              | `150 x 150 px`              | 150×150 (100%), 188×188 (125%), 225×225 (150%), 300×300 (200%), 600×600 (400%) | Medium Tile (Start Menu Windows 10), Search preview |
| **Square71x71Logo** *(Tùy chọn)*   | `71 x 71 px`                | 71×71, 89×89, 107×107, 142×142, 284×284                      | Small Tile trong Start Menu                         |
| **Square310x310Logo** *(Tùy chọn)* | `310 x 310 px`              | 310×310, 388×388, 465×465, 620×620, 1240×1240                | Large Tile trong Start Menu                         |
| **StoreLogo**                      | `50 x 50 px`                | 50×50 (100%), 63×63 (125%), 75×75 (150%), 100×100 (200%), 200×200 (400%) | Trang cài đặt ứng dụng, trang Microsoft Store       |
| **SplashScreen**                   | `620 x 300 px`              | 620×300, 775×375, 930×450, 1240×600, 2480×1200               | Màn hình tải lúc khởi động app                      |

*Quy ước đặt tên file chuẩn của Windows App SDK/MSIX:*

- `Square44x44Logo.scale-100.png`
- `Square44x44Logo.scale-200.png`
- `Square150x150Logo.scale-200.png`
- `StoreLogo.scale-100.png`

> **Mẹo:** Nếu bạn không muốn xuất hàng chục file PNG thủ công, bạn chỉ cần thiết kế **1 file ảnh gốc $1024 \times 1024\text{ px}$ (PNG nền trong suốt)** hoặc **file SVG**, sau đó dùng công cụ tạo asset tự động của Visual Studio (*Package.appxmanifest -> Visual Assets -> Asset Generator*) hoặc lệnh CLI `generate-appx-package-resources`, công cụ sẽ tự động tính toán và cắt ra toàn bộ các biến thể kích thước trên.

### 2. Tiêu chuẩn Nội dung & Safe Margin (Vùng an toàn)

Đây là tiêu chuẩn khắt khe nhất của Microsoft để tránh icon bị méo, tràn viền hoặc che mất nội dung:

- **Vùng đệm an toàn (Padding / Safe Zone):**
  - Đối với **Square44x44Logo**: Nội dung hình vẽ chính (graphic/silhouette) chỉ nên chiếm khoảng **$60\% - 66\%$** diện tích tổng thể (khoảng $28 \times 28\text{ px}$ ở kích thước cơ sở). Để trống $8\text{ px}$ viền ngoài làm khoảng đệm.
  - Đối với **Square150x150Logo**: Nội dung chính chiếm khoảng **$50\% - 60\%$** ở tâm ảnh ($75\text{ px} - 90\text{ px}$).
- **Tránh chữ (Typography):** Tuyệt đối không gắn text nhỏ, tên app bên trong icon Square44x44 hay Square150x150. Windows sẽ tự render text tiêu đề ứng dụng bên dưới hoặc bên cạnh icon.
- **Không nhúng bo góc cứng (Baked-in Rounded Corners):**
  - Windows 11 sẽ tự áp dụng bo góc hoặc đưa icon vào khung bo tròn/vuông tùy theo ngữ cảnh (Taskbar, Start Menu).
  - Nếu bạn vẽ sẵn một viền tròn/bo góc có nền đen/trắng xung quanh, icon khi hiển thị trên Windows 11 sẽ bị hiện viền vuông giả hoặc "double border" rất xấu. Hãy để **nền trong suốt (Transparent background)** hoàn toàn.

### 3. Tiêu chuẩn Màu sắc (Color Standards & Theming)

- **Hệ màu:** **RGB / sRGB** (Tuyệt đối không dùng CMYK hoặc Display P3 chưa convert, vì Windows Shell sẽ render sai màu hoặc xỉn màu).
- **Độ sâu màu (Color Depth):** **32-bit RGBA** (8-bit cho mỗi kênh Red, Green, Blue, Alpha).
- **Hỗ trợ Light / Dark Theme & High Contrast:**
  - MSIX hỗ trợ cơ chế tài nguyên động theo theme người dùng qua hậu tố tên file:
    - `Square44x44Logo.targetsize-24.png`
    - `Square44x44Logo.targetsize-24_altform-lightunplated.png` (dùng cho taskbar nền sáng)
    - `Square44x44Logo.targetsize-24_altform-unplated.png` (dùng cho taskbar nền tối)
  - Nếu icon phong cách Chibi/AoE IV của bạn đã là một khối huy hiệu/sticker có viền màu tương phản đậm (`#3D2B1F` hoặc viền nẹp kim loại sẫm màu) và bên trong có đổ màu rõ ràng, bạn hoàn toàn có thể dùng chung một asset cho cả Light mode lẫn Dark mode mà không sợ bị "chìm" màu vào thanh Taskbar.

### 4. Tiêu chuẩn Dung lượng & Định dạng file (Format & File Size)

- **Định dạng file xuất xưởng:** **PNG (Portable Network Graphics)** có kênh Alpha trong suốt.
- **Dung lượng tối đa (File size):**
  - Icon nhỏ (Square44x44, StoreLogo mọi tỷ lệ scale): Mỗi file nên **$< 50\text{ KB}$**.
  - Icon vừa và lớn (Square150x150, Splash Screen scale 400%): Mỗi file nên **$< 300\text{ KB}$**.
  - Tổng dung lượng toàn bộ thư mục `/Assets` chứa icon của gói MSIX: Nên duy trì **$< 5\text{ MB}$** để đảm bảo quá trình tải/cài đặt và render icon trên Windows Shell tức thì, không bị delay.
- **Tối ưu hóa nén ảnh (Lossless Compression):** Trước khi đóng gói MSIX, nên chạy qua các công cụ nén PNG không giảm chất lượng như `oxipng`, `pngquant`, hoặc tính năng `Optimize PNG` của Inkscape để loại bỏ metadata thừa (EXIF, ICC profiles dư thừa).

### 5. Cấu hình khai báo trong `AppxManifest.xml`

Khi đưa vào cấu hình gói MSIX của ứng dụng (Avalonia / .NET), các thẻ khai báo asset icon tối thiểu cần có dạng như sau:

XML

```
<Applications>
  <Application Id="FshotApp" Executable="Fshot.exe" EntryPoint="Windows.FullTrustApplication">
    <uap:VisualElements
      DisplayName="Fshot"
      Description="Fshot Screenshot Tool"
      BackgroundColor="transparent"
      Square150x150Logo="Assets\Square150x150Logo.png"
      Square44x44Logo="Assets\Square44x44Logo.png">
      
      <uap:DefaultTile 
        ShortName="Fshot"
        Square71x71Logo="Assets\Square71x71Logo.png">
      </uap:DefaultTile>
      
      <uap:SplashScreen Image="Assets\SplashScreen.png" BackgroundColor="#111827"/>
    </uap:VisualElements>
  </Application>
</Applications>
```

> **Lưu ý:** Đặt `BackgroundColor="transparent"` để Windows không tự động chèn một khối màu nền phẳng (accent color) phía sau icon của bạn trên Start Menu.