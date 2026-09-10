# Annotation — Tổng quan thiết kế

> Thư mục này thiết kế các công cụ chú thích (annotation tools) trong F-Shot.  
> Annotation là nhóm tính năng chính của MVP, bao gồm 8 công cụ vẽ cơ bản.

---

## 1. Mục đích

`Annotation` định nghĩa:

- **Các loại công cụ**: Pencil, Line, Arrow, Rectangle, Circle, Marker, Text, Pixelate.
- **Mô hình dữ liệu**: `Tool` DU và `Annotation` record.
- **Cách vẽ**: preview trong khi tương tác, commit khi hoàn tất.
- **Thuộc tính chung**: màu, độ dày, alpha, kiểu mũi tên.

Annotation phải:

- Pure F#, không phụ thuộc Skia/Avalonia.
- Dễ dàng thêm công cụ mới sau này.
- Hỗ trợ undo/redo qua immutable snapshots.

---

## 2. Phạm vi thiết kế

### 2.1 Các công cụ trong MVP

| Tool | Kiểu dữ liệu | Mô tả |
|------|--------------|-------|
| Pencil | Danh sách điểm | Vẽ tự do, cần smoothing |
| Line | 2 điểm | Đường thẳng |
| Arrow | 2 điểm + style | Đường thẳng + đầu mũi tên |
| Rectangle | 2 điểm + corner radius | Hình chữ nhật, hỗ trợ bo góc |
| Circle | 2 điểm + aspect lock | Hình tròn / elip |
| Marker | 2 điểm | Nét bán trong suốt làm nổi bật |
| Text | Vị trí + nội dung | Chèn văn bản |
| Pixelate | 2 điểm + block size | Làm mờ / mosaic vùng |

### 2.2 Annotation record

Mỗi chú thích gồm:

- `Id`: GUID.
- `Tool`: loại công cụ + tham số.
- `Color`: màu vẽ.
- `StrokeWidth`: độ dày nét.
- `CreatedAt`: thời điểm tạo (dùng cho sắp xếp lớp).

### 2.3 Preview và commit

- Trong khi tương tác: tool chưa được thêm vào danh sách, chỉ là **preview**.
- Khi hoàn tất (thả chuột, kết thúc text): tạo `Annotation` mới, push snapshot vào history.

### 2.4 Không thuộc phạm vi MVP

- Invert colors (thuộc [S]).
- Circle counter (thuộc [S]).
- Object selection/edit mode (thuộc [S]).
- Arrow style / reverse arrow (thuộc [C]).
- Hit-testing tolerance (thuộc [S]).

---

## 3. Liên hệ với các phần khác

```
Annotation
    ├── Geometry: Point, Rect, StrokeWidth, Color
    ├── Selection: vùng crop, tọa độ tương đối
    ├── History: push snapshot khi commit
    ├── Config: drawColor, drawThickness, tool defaults
    ├── Rendering.Skia: vẽ từng loại tool
    └── UI: chuyển pointer events thành annotation commands
```

---

## 4. Dùng trong PoC

Trong Phase 0, Annotation không cần thiết. Có thể demo đơn giản bằng cách vẽ một đường thẳng cố định lên screenshot để kiểm chứng render pipeline.

---

## 5. Dùng trong MVP

Trong Phase 1, Annotation cần hỗ trợ đầy đủ 8 công cụ:

- Phím tắt chuyển công cụ (P/D/A/S/R/C/M/T/B/I).
- Màu và độ dày lấy từ state/config.
- Preview trong khi vẽ.
- Commit vào danh sách khi hoàn tất.
- Undo/redo hoạt động.

---

## 6. Câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Pencil smoothing dùng thuật toán nào? | Ảnh hưởng cảm giác vẽ tay tự do |
| Text tool render hoàn toàn bằng Skia hay dùng inline TextBox? | Ảnh hưởng UX và cách commit |
| Pixelate xử lý trên CPU hay GPU? | Ảnh hưởng hiệu năng và bảo mật |
| Có lưu thứ tự z-index của annotation không? | Ảnh hưởng layer ordering |
| Mỗi tool có kiểu dữ liệu riêng hay gộp chung? | Ảnh hưởng cách viết renderer |

---

## 7. Các file sẽ được thiết kế trong thư mục này

| File | Nội dung |
|------|----------|
| `04_02_ToolModel.md` | `Tool` DU và `Annotation` record |
| `04_03_Pencil.md` | Pencil và smoothing |
| `04_04_LineAndArrow.md` | Line và Arrow |
| `04_05_RectangleAndCircle.md` | Rectangle, Circle, constraints |
| `04_06_MarkerAndPixelate.md` | Marker alpha blend, Pixelate |
| `04_07_TextTool.md` | Text tool và commit |
| `04_08_CommitAndPreview.md` | Preview vs commit, tích hợp History |

---

## 8. Kết nối với code

Triển khai trong:

- `src/FShot.Core/Domain/Annotation.fs`
- `src/FShot.Rendering.Skia/Renderers/AnnotationRenderer.fs`
- `tests/FShot.Core.Tests/Domain/AnnotationTests.fs`

---

*Annotation là chủ đề thứ tư. Sau khi chốt, chúng ta chuyển sang History — undo/redo.*
