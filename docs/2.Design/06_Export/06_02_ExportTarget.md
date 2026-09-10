# ExportTarget — Thiết kế chi tiết

> Tài liệu này mô tả các hình thức xuất ảnh trong F-Shot.

---

## 1. Các hình thức xuất

| Target | Mô tả | Mức độ |
|--------|-------|--------|
| `SaveToFile` | Lưu ảnh ra đĩa theo đường dẫn | Must |
| `CopyToClipboard` | Copy ảnh vào clipboard | Must |
| `RawPngToStdout` | Xuất byte PNG ra stdout | Should |
| `PrintGeometry` | In `WxH+X+Y` ra stdout | Should |
| `OpenWithDefaultApp` | Mở ảnh bằng ứng dụng mặc định | Should |

---

## 2. SaveToFile

Cần:
- Đường dẫn đầy đủ.
- Định dạng PNG hoặc JPG.
- Nếu chưa có đường dẫn, UI hiển thị save dialog.

## 3. CopyToClipboard

Ghi ảnh dạng PNG/DIB vào clipboard Windows. Ảnh được render từ screenshot + selection + annotations.

## 4. RawPngToStdout

Encode ảnh thành PNG bytes, ghi trực tiếp ra stdout. Dùng cho piping trong CLI.

## 5. PrintGeometry

In kích thước và vị trí vùng chọn ra stdout theo định dạng:

`WxH+X+Y`

Ví dụ: vùng chọn `(100, 80, 300, 200)` sẽ in:

`300x200+100+80`

## 6. Kết nối với code

- `src/FShot.Core/Domain/Export.fs`
- `src/FShot.Platform.Win32/Clipboard/ClipboardService.fs`
- `src/FShot.Platform.Win32/FileSystem/SaveDialog.fs`
