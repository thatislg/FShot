# Migration — Thiết kế chi tiết cơ chế chuyển đổi cấu hình Flameshot

> Tài liệu này thiết kế chi tiết cơ chế phát hiện, phân tích cú pháp (parsing) và tự động chuyển đổi cấu hình từ file `flameshot.ini` cũ sang cấu hình chuẩn JSON của F-Shot (`FR-WIN-006`, `FR-SYS-013`).  
> **Nguyên tắc tài liệu:** Thiết kế thuần kiến trúc, quy tắc chuyển đổi, bảng ánh xạ và sơ đồ luồng — **hoàn toàn không sử dụng code mẫu**.

---

## 1. Mục đích và Phạm vi

Khi người dùng chuyển từ Flameshot (bản C++/Qt gốc) sang F-Shot trên Windows:
- Họ có thói quen sử dụng, thư mục lưu ảnh yêu thích, mẫu tên file cá nhân hóa và bảng màu vẽ riêng biệt đã lưu tại `%APPDATA%\flameshot\flameshot.ini`.
- F-Shot sử dụng định dạng hiện đại `%APPDATA%\FShot\config.json`.
- Mục tiêu của module Migration là bảo toàn trải nghiệm người dùng cũ bằng cách:
  1. Tự động nhận diện cấu hình cũ khi F-Shot khởi động lần đầu.
  2. Phân tích ngữ pháp file INI mà không phụ thuộc thư viện bên ngoài nặng nề.
  3. Ánh xạ chính xác các tham số, chuẩn hóa màu sắc và giá trị số.
  4. Tạo file cấu hình F-Shot mới an toàn, không làm hỏng hoặc xóa file gốc của Flameshot.

---

## 2. Đặc tả Cú pháp file `flameshot.ini`

File `flameshot.ini` tuân theo chuẩn INI của thư viện `QSettings` (Qt Framework):
- **Phần định danh nhóm (Sections):** Nằm trong cặp ngoặc vuông, phổ biến nhất là `[General]` và `[Shortcuts]`.
- **Cặp khóa - giá trị (Key-Value):** Phân tách bằng dấu bằng (`=`), ví dụ `savePath=C:/Screenshots`.
- **Dòng chú thích (Comments):** Bắt đầu bằng dấu chấm phẩy (`;`) hoặc dấu thăng (`#`), được bỏ qua khi phân tích.
- **Dòng trống:** Bỏ qua hoàn toàn.
- **Giá trị mảng (Arrays/Lists):** Thường được phân tách bằng dấu phẩy (`,`), ví dụ `userColors=#ff0000, #00ff00, #0000ff`.
- **Giá trị màu sắc (Color Representations):** Có 3 dạng biểu diễn trong Flameshot:
  1. Định dạng Hex: `#RRGGBB` hoặc `#AARRGGBB`.
  2. Định dạng chuỗi hàm Qt: `rgb(R, G, B)` hoặc `rgba(R, G, B, A)`.
  3. Định dạng số nguyên ARGB có dấu: ví dụ `@Variant(\0\0\0\x43\x1...)` hoặc số nguyên 32-bit.
- **Giá trị boolean:** Biểu diễn dưới dạng chuỗi chữ thường `true` / `false`.

---

## 3. Bảng Ánh xạ Toàn diện (Key Mapping Matrix)

Bảng dưới đây quy định cách chuyển đổi từng thuộc tính từ `flameshot.ini` sang thuộc tính JSON tương ứng của F-Shot:

| Khóa Flameshot (`flameshot.ini`) | Khóa F-Shot (`config.json`) | Kiểu dữ liệu gốc | Kiểu dữ liệu F-Shot | Quy tắc chuyển đổi & Chuẩn hóa |
| :--- | :--- | :--- | :--- | :--- |
| `savePath` | `savePath` | String | String | Chuẩn hóa dấu gạch chéo sang dạng chuẩn của Windows, bỏ dấu gạch chéo ở cuối. |
| `savePathFixed` | `savePathFixed` | Boolean | Boolean | Giữ nguyên giá trị logic. |
| `filenamePattern` | `filenamePattern` | String | String | Tương thích trực tiếp vì cả hai đều dùng token chuẩn strftime (`%Y`, `%m`, `%d`, `%H`, `%M`, `%S`). |
| `saveAsFileExtension` | `saveAsFileExtension` | String | String | Bỏ dấu chấm nếu có (ví dụ `.png` -> `png`), chuẩn hóa chữ thường. Mặc định `png`. |
| `jpegQuality` | `jpegQuality` | Integer | Integer | Giới hạn khoảng giá trị trong khoảng $[1, 100]$. Nếu ngoài khoảng thì dùng $90$. |
| `drawColor` | `drawColor` | Color | String (Hex) | Chuyển đổi sang chuỗi hex viết hoa dạng `#RRGGBB`. |
| `drawThickness` | `drawThickness` | Float / Int | Float | Ép kiểu số thực, giới hạn khoảng $[1.0, 50.0]$. |
| `fontSize` / `drawFontSize` | `drawFontSize` | Float / Int | Float | Giới hạn khoảng $[8.0, 72.0]$. |
| `contrastOpacity` | `contrastOpacity` | Integer | Byte (0–255) | Flameshot lưu từ $0$ đến $255$. Giữ nguyên giá trị byte. |
| `uiColor` | `uiColor` | Color | String (Hex) | Chuyển đổi sang chuỗi hex viết hoa dạng `#RRGGBB`. |
| `contrastUiColor` | `contrastUiColor` | Color | String (Hex) | Chuyển đổi sang chuỗi hex viết hoa dạng `#RRGGBB`. |
| `userColors` | `userColors` | String List | String List | Tách chuỗi theo dấu phẩy, loại bỏ khoảng trắng, chuẩn hóa từng mã màu sang `#RRGGBB`. |
| `buttons` | `buttons` | String List | String List | Ánh xạ danh sách ID công cụ Flameshot sang danh sách tên Tool của F-Shot. |
| `startupLaunch` | `startupLaunch` | Boolean | Boolean | Giữ nguyên giá trị logic. |
| `disabledTrayIcon` | `disabledTrayIcon` | Boolean | Boolean | Giữ nguyên giá trị logic. |
| `showDesktopNotification` | `showDesktopNotification` | Boolean | Boolean | Giữ nguyên giá trị logic. |
| `showAbortNotification` | `showAbortNotification` | Boolean | Boolean | Giữ nguyên giá trị logic. |
| `copyOnDoubleClick` | `copyOnDoubleClick` | Boolean | Boolean | Giữ nguyên giá trị logic. |
| `uiLanguage` | `uiLanguage` | String | String | Ánh xạ mã ngôn ngữ (`vi_VN` -> `vi`, `en_US` -> `en`). |

---

## 4. Quy tắc Chuẩn hóa Dữ liệu (Type Coercion & Normalization)

### 4.1 Quy tắc Chuẩn hóa Mã màu (Color Normalization Algorithm)
Mã màu từ Flameshot được đưa qua bộ lọc tuần tự 4 bước:
1. **Kiểm tra tiền tố `#`:**
   - Nếu độ dài 7 ký tự (`#RRGGBB`): Chấp nhận, chuyển thành chữ in hoa.
   - Nếu độ dài 9 ký tự (`#AARRGGBB`): Chuyển thành chuẩn CSS/Skia `#RRGGBBAA`.
2. **Kiểm tra định dạng hàm `rgb(...)` / `rgba(...)`:**
   - Trích xuất 3 hoặc 4 giá trị số nguyên thành phần đỏ (R), xanh lục (G), xanh lam (B) và độ trong suốt (A).
   - Định dạng lại thành chuỗi hex hai chữ số cho mỗi kênh: `#RRGGBB`.
3. **Kiểm tra mã lỗi / Giá trị không hợp lệ:**
   - Nếu giá trị không thể phân tích, tự động thay thế bằng mã màu mặc định an toàn (`#FF0000` cho nét vẽ, `#38BDF8` cho giao diện).

### 4.2 Quy tắc Ánh xạ Danh sách Nút công cụ (Button ID Mapping)

Flameshot sử dụng các số nguyên định danh cho từng loại nút trên thanh công cụ:

| ID Số Flameshot | Tên Công cụ Flameshot | Tên Công cụ tương ứng F-Shot |
| :---: | :--- | :--- |
| 0 | TYPE_SELECTION | `SelectionTool` |
| 1 | TYPE_DRAWER | `PencilTool` |
| 2 | TYPE_ARROW | `ArrowTool` |
| 3 | TYPE_LINE | `LineTool` |
| 4 | TYPE_RECTANGLE | `RectangleTool` |
| 5 | TYPE_CIRCLE | `CircleTool` |
| 6 | TYPE_MARKER | `MarkerTool` |
| 7 | TYPE_TEXT | `TextTool` |
| 8 | TYPE_PIXELATE | `PixelateTool` |
| 9 | TYPE_INVERT | `InvertTool` |
| 10 | TYPE_CIRCLECOUNTER | `CircleCounterTool` |

Các nút không được F-Shot hỗ trợ trong phiên bản hiện tại sẽ được bỏ qua một cách an toàn mà không làm gián đoạn quá trình nạp.

---

## 5. Sơ đồ Luồng Hoạt động (Migration Workflow)

```
              ┌──────────────────────────────────────────────┐
              │ Khởi động F-Shot hoặc gọi lệnh CLI Migration │
              └──────────────────────┬───────────────────────┘
                                     │
                    File F-Shot config.json đã tồn tại?
                                     │
                     ┌───────────────┴───────────────┐
                    Có                               Không
                     │                                 │
             Bỏ qua auto-migration       Tìm file %APPDATA%\flameshot\flameshot.ini
             (Trừ khi có cờ CLI                        │
             --import-flameshot)              ┌────────┴────────┐
                                            Có                Không
                                              │                 │
                                    Đọc nội dung INI      Tạo config.json mặc định
                                    với bộ giải mã UTF-8  (Khởi tạo lần đầu)
                                              │                 │
                                    Phân tích cú pháp     Kết thúc
                                    Section [General]
                                              │
                                    Ánh xạ từng trường
                                    theo Mapping Matrix
                                              │
                                    Chuẩn hóa giá trị
                                    (Color, Path, Clamp)
                                              │
                                    Ghi file an toàn vào
                                    %APPDATA%\FShot\config.json
                                              │
                                    Ghi nhận log thông báo
                                    Migration thành công
                                              │
                                           Kết thúc
```

---

## 6. Xử lý Lỗi và Độ bền vững (Resilience & Edge Cases)

1. **File `flameshot.ini` bị khóa hoặc không đủ quyền đọc:**
   - Hệ thống ghi log cảnh báo chi tiết nguyên nhân (Access Denied / Sharing Violation).
   - Hệ thống không dừng ứng dụng; tiếp tục khởi tạo `config.json` theo cấu hình mặc định.
2. **File `flameshot.ini` chứa các khóa lạ / plugin không xác định:**
   - Bỏ qua các khóa lạ trong file INI mà không gây lỗi phân tích cú pháp.
3. **Phần `[Shortcuts]` bị xung đột:**
   - Không áp dụng tự động các phím tắt hệ thống nguy hiểm; chỉ import các phím tắt thuộc danh mục được phép của F-Shot.
4. **Bảo toàn file gốc:**
   - Quá trình chuyển đổi chỉ mở file `flameshot.ini` với quyền `FileAccess.Read` và `FileShare.ReadWrite`.
   - Tuyệt đối không xóa, đổi tên hoặc sửa đổi bất kỳ byte nào trong file gốc của Flameshot.
