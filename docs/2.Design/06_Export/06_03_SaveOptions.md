# SaveOptions — Thiết kế chi tiết

> Tài liệu này mô tả các tùy chọn lưu file ảnh, cơ chế phân giải mẫu tên file theo thời gian, chất lượng nén, và luồng lưu tức thì / Save As fallback (`FR-OUT-01`, `FR-OUT-02`, `FR-OUT-03`, `FR-OUT-04`, `FR-OUT-13`).

---

## 1. Các thành phần dữ liệu

```
SaveOptions
├── Path: string option
├── FileNamePattern: string
├── Format: Png | Jpg
└── JpegQuality: int (0–100)
```

- **Path**: Thư mục lưu ảnh hoặc đường dẫn file cụ thể. Nếu có giá trị (`Some path`), FShot kích hoạt luồng **lưu tức thì (Instant Save)** không hiển thị dialog.
- **FileNamePattern**: Chuỗi mẫu định dạng tên file hỗ trợ token ngày tháng. Mặc định: `"fshot_%Y-%m-%d-%H%M%S"`.
- **Format**: Định dạng ảnh xuất (`Png` hoặc `Jpg`).
- **JpegQuality**: Mức chất lượng nén JPEG (từ 0 đến 100, chuẩn hóa qua `NormalizedJpegQuality`, mặc định 90).

---

## 2. FileNamePattern — Bảng quy đổi Token

Mô-đun `FileNamePattern.resolve` hỗ trợ các token thời gian sau:

| Token | Ý nghĩa | Ví dụ giá trị |
| :--- | :--- | :--- |
| `%Y` | Năm 4 chữ số | `2026` |
| `%m` | Tháng 2 chữ số (`01`–`12`) | `09` |
| `%d` | Ngày 2 chữ số (`01`–`31`) | `15` |
| `%H` | Giờ 24h 2 chữ số (`00`–`23`) | `16` |
| `%M` | Phút 2 chữ số (`00`–`59`) | `30` |
| `%S` | Giây 2 chữ số (`00`–`59`) | `45` |

### Ví dụ mẫu phổ biến:
- `fshot_%Y-%m-%d-%H%M%S` → `fshot_2026-09-15-163045.png`
- `Screenshot_%Y%m%d_%H%M%S` → `Screenshot_20260915_163045.png`
- `Capture_%Y-%m-%d` → `Capture_2026-09-15.png`

Nếu chuỗi pattern rỗng hoặc null, hệ thống tự động fallback về mẫu `fshot_yyyy-MM-dd-HHmmss`.

---

## 3. Quy trình phân giải đường dẫn lưu (`ResolveSavePath`)

```
Input: SaveOptions (Path, FileNamePattern, Format), defaultFolder, time
                      │
            Có chỉ định Path?
              ├── Có ──> Path có extension file? (.png, .jpg)
              │            ├── Có ──> Dùng trực tiếp Path
              │            └── Không ─> Path.Combine(Path, generatedFileName)
              │
              └── Không ─> Path.Combine(defaultFolder, generatedFileName)
```

1. **Khi `Path` là đường dẫn file cụ thể**:
   - Giữ nguyên đường dẫn chỉ định (vd: `D:\Captures\my_shot.png`).
2. **Khi `Path` là thư mục**:
   - Tự động kết hợp thư mục với tên file sinh từ pattern kèm đuôi extension (vd: `D:\Captures\fshot_2026-09-15-163045.png`).
3. **Khi `Path` là `None`**:
   - Fallback hộp thoại **Save As Dialog** với tên gợi ý `suggestedFileName` sinh từ pattern, vị trí mặc định gợi ý là thư mục Pictures của người dùng.

---

## 4. Chất lượng ảnh (JPEG / PNG)

- **PNG**: Định dạng lossless, giữ trọn vẹn màu sắc và độ sắc nét của text/vector annotations. Luôn mã hóa ở mức 100.
- **JPEG**: Định dạng nén lossy, phù hợp ảnh chụp dung lượng nhỏ. Mức chất lượng nén được quy định bởi `NormalizedJpegQuality` (0–100, mặc định 90).

---

## 5. Kết nối mã nguồn

- `src/FShot.Core/Domain/Export.fs`: Định nghĩa kiểu `FileFormat`, `SaveOptions`, `FileNamePattern`.
- `src/FShot.Core/Domain/Config.fs`: Khai báo `SaveOptions` bên trong `ConfigSnapshot`.
- `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`: Luồng thực thi `ExecuteStartExport` cho cả Instant Save và Save As Dialog.
- `tests/FShot.Core.Tests/Domain/ExportTests.fs`: Bộ kiểm thử unit tests cho token formatting và phân giải đường dẫn.
