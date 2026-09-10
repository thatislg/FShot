# Export — Tổng quan thiết kế

> Thư mục này thiết kế cách F-Shot xuất ảnh sau khi chụp và chú thích.  
> Export là một trong những tính năng cốt lõi của MVP: lưu file và copy clipboard.

---

## 1. Mục đích

`Export` định nghĩa:

- Các **hình thức xuất**: save file, copy clipboard, raw PNG stdout, geometry stdout, open app.
- **Định dạng file**: PNG, JPG.
- **Mẫu tên file**: dùng token strftime / .NET format.
- **Tích hợp** giữa domain (tính toán) và platform (thực hiện IO).

Export phải:

- Tách biệt rõ logic tính toán và side effect.
- Dễ mở rộng thêm hình thức xuất mới.
- Không chứa UI dialog logic trong Core.

---

## 2. Phạm vi thiết kế

### 2.1 ExportTarget

| Target | Mô tả | Mức độ |
|--------|-------|--------|
| `SaveToFile` | Lưu ra đĩa | M |
| `CopyToClipboard` | Copy ảnh vào clipboard | M |
| `RawPngToStdout` | Xuất byte PNG ra stdout | S |
| `PrintGeometry` | In `WxH+X+Y` ra stdout | S |
| `OpenWithDefaultApp` | Mở ảnh bằng app mặc định | S |

### 2.2 SaveOptions

- `Path`: đường dẫn cố định (nếu có).
- `FileNamePattern`: mẫu tên file.
- `Format`: PNG / JPG.
- `JpegQuality`: 0–100.

### 2.3 Quy trình xuất

1. `FShot.Core` tính `ExportTarget` từ state hiện tại.
2. `FShot.Rendering.Skia` render ảnh cuối từ screenshot + selection + annotations.
3. `FShot.Rendering.Skia` encode ảnh thành PNG/JPG bytes.
4. `FShot.Platform.Win32` thực hiện IO: ghi file, copy clipboard, stdout, mở app.
5. `FShot.UI` hiển thị save dialog nếu cần.

### 2.4 Không thuộc phạm vi

- Upload Imgur (thuộc [I]).
- Copy-path-after-save (thuộc [S]).
- Save-after-copy (thuộc [S]).
- Toast notification (thuộc [S]).

---

## 3. Liên hệ với các phần khác

```
Export
    ├── Capture: screenshot gốc
    ├── Selection: vùng crop
    ├── Annotation: danh sách chú thích cần render
    ├── Config: savePath, filenamePattern, defaultExtension, jpegQuality
    ├── Rendering.Skia: render + encode
    ├── Platform.Win32: file/clipboard/stdout/app operations
    └── UI: save dialog fallback
```

---

## 4. Dùng trong PoC

Trong Phase 0, Export không cần thiết. Có thể bỏ qua hoặc chỉ test render ảnh ra file để kiểm chứng pipeline.

---

## 5. Dùng trong MVP

Trong Phase 1, Export cần hỗ trợ:

- Save file (`Ctrl+S`).
- Copy clipboard (`Ctrl+C`).
- Fixed save path.
- Filename pattern.
- Save dialog fallback.
- PNG / JPG.

---

## 6. Câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Filename pattern dùng .NET `DateTime.ToString` hay custom tokenizer? | Ảnh hưởng tương thích `flameshot.ini` cũ |
| JPG clipboard có cần trong MVP không? | Thuộc [C/P], có thể để sau |
| Có crop theo vùng chọn hay xuất toàn ảnh chụp? | Ảnh hưởng behavior khi chưa chọn vùng |
| Có tạo thư mục savePath nếu chưa tồn tại? | UX và lỗi tiềm ẩn |

---

## 7. Các file sẽ được thiết kế trong thư mục này

| File | Nội dung |
|------|----------|
| `06_02_ExportTarget.md` | Các hình thức xuất |
| `06_03_SaveOptions.md` | SaveOptions và filename pattern |
| `06_04_RenderPipeline.md` | Render + encode ảnh cuối |
| `06_05_FileAndClipboard.md` | IO operations trên Windows |
| `06_06_CliOutput.md` | Raw PNG stdout, geometry stdout |

---

## 8. Kết nối với code

Triển khai trong:

- `src/FShot.Core/Domain/Export.fs`
- `src/FShot.Rendering.Skia/Renderers/SceneComposer.fs`
- `src/FShot.Platform.Win32/Clipboard/ClipboardService.fs`
- `src/FShot.Platform.Win32/FileSystem/SaveDialog.fs`
- `tests/FShot.Core.Tests/Domain/ExportTests.fs`

---

*Export là chủ đề thứ sáu. Sau khi chốt, chúng ta chuyển sang Config — cấu hình.*
