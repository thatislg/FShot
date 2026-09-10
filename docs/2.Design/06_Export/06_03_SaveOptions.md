# SaveOptions — Thiết kế chi tiết

> Tài liệu này mô tả các tùy chọn lưu file ảnh.

---

## 1. Các thành phần

```
SaveOptions
├── Path: string option
├── FileNamePattern: string
├── Format: Png | Jpg
└── JpegQuality: int (0–100)
```

## 2. FileNamePattern

Mẫu tên file hỗ trợ token thời gian theo .NET DateTime format.

Ví dụ:
- `screenshot_%Y%m%d_%H%M%S` → `screenshot_20240115_143052`
- `fshot_%Y-%m-%d-%H%M%S` → `fshot_2024-01-15-143052`

## 3. Giải quyết đường dẫn

Công thức:

`fullPath = saveFolder / generatedFileName + extension`

Nếu `Path` được chỉ định, dùng `Path` trực tiếp. Nếu không, dùng `FileNamePattern` + thư mục mặc định từ config.

## 4. JPG Quality

Giá trị từ 0 đến 100. 100 là chất lượng cao nhất, 0 là thấp nhất. Mặc định 90.

## 5. Kết nối với code

- `src/FShot.Core/Domain/Export.fs`
- `src/FShot.Core/Domain/Config.fs`
