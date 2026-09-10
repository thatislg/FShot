# Config — Tổng quan thiết kế

> Thư mục này thiết kế cấu hình người dùng của F-Shot.  
> Config ở MVP chỉ cần đọc/ghi file đơn giản; UI config đầy đủ sẽ làm ở v1.0.

---

## 1. Mục đích

`Config` định nghĩa:

- **Schema cấu hình**: các trường cần thiết cho MVP.
- **Default values**.
- Cách **đọc/ghi** file config.
- Cách **tương thích** với `flameshot.ini` cũ (nếu cần).

Config phải:

- Định nghĩa trong `FShot.Core` (schema + default).
- Đọc/ghi trong `FShot.Platform.Win32` (file IO).
- Dễ mở rộng cho các trường v1.x.

---

## 2. Phạm vi thiết kế

### 2.1 Cấu hình trong MVP

| Nhóm | Trường | Mức độ |
|------|--------|--------|
| General | `savePath` | M |
| General | `filenamePattern` | M |
| Tool Defaults | `drawColor` | M |
| Tool Defaults | `drawThickness` | M |
| General | `saveAsFileExtension` | S |
| General | `jpegQuality` | S |
| General | `savePathFixed` | S |

### 2.2 Cấu hình v1.0+

- Interface colors, opacity, language, font.
- Startup, tray icon, notifications.
- Upload settings.
- Hotkey bindings.

### 2.3 Định dạng file

- Đọc INI cũ Flameshot để migrate.
- Ghi JSON mới cho F-Shot.
- Lưu tại `%APPDATA%\Roaming\FShot\`.

---

## 3. Liên hệ với các phần khác

```
Config
    ├── Export: savePath, filenamePattern, format
    ├── Annotation: drawColor, drawThickness, tool defaults
    ├── Rendering.Skia: colors, opacity
    ├── Platform.Win32: read/write file
    └── UI (v1.0): config editor
```

---

## 4. Dùng trong PoC

Trong Phase 0, Config không cần thiết. Có thể dùng hardcoded defaults.

---

## 5. Dùng trong MVP

Trong Phase 1, Config cần:

- Đọc/ghi `savePath`, `filenamePattern`, `drawColor`, `drawThickness`.
- Cung cấp default values.
- Tạo file config nếu chưa tồn tại.

---

## 6. Câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| File config dùng JSON hay giữ INI? | Ảnh hưởng tương thích ngược |
| Có migrate tự động từ `flameshot.ini` không? | Ảnh hưởng onboarding người dùng cũ |
| Schema validation nghiêm ngặt hay permissive? | Ảnh hưởng robustness |
| Có hot-reload khi file thay đổi không? | Thuộc [S], v1.0 |

---

## 7. Các file sẽ được thiết kế trong thư mục này

| File | Nội dung |
|------|----------|
| `07_02_Schema.md` | Config schema và default values |
| `07_03_MvpConfig.md` | Các trường cần cho MVP |
| `07_04_FileStore.md` | Đọc/ghi file JSON/INI |
| `07_05_Migration.md` | Tương thích `flameshot.ini` cũ |

---

## 8. Kết nối với code

Triển khai trong:

- `src/FShot.Core/Domain/Config.fs`
- `src/FShot.Platform.Win32/Config/ConfigStore.fs`
- `tests/FShot.Core.Tests/Domain/ConfigTests.fs`

---

*Config là chủ đề thứ bảy. Sau khi chốt, chúng ta chuyển sang OverlayState — state machine.*
