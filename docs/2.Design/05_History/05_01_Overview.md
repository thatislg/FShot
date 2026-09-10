# History — Tổng quan thiết kế

> Thư mục này thiết kế hệ thống hoàn tác / làm lại (undo/redo).  
> History đơn giản nhưng quan trọng, vì nó ảnh hưởng đến cách mọi thao tác vẽ được lưu trữ.

---

## 1. Mục đích

`History` định nghĩa:

- Cách **lưu snapshot** sau mỗi thao tác thay đổi chú thích.
- Cấu trúc **undo stack** và **redo stack**.
- Giới hạn số bước lưu.
- Khôi phục trạng thái khi undo/redo.

History phải:

- Immutable.
- Dễ test.
- Không phụ thuộc UI.

---

## 2. Phạm vi thiết kế

### 2.1 Snapshot

Mỗi snapshot lưu:

- `Annotations`: danh sách chú thích hiện tại.
- `NextCounter`: chỉ số counter tiếp theo (cho CircleCounter tool).

### 2.2 History

- `UndoStack`: danh sách snapshot.
- `RedoStack`: danh sách snapshot đã undo.
- `Limit`: giới hạn kích thước (mặc định 100).

### 2.3 Thao tác

- **Push**: thêm snapshot mới, xóa redo stack.
- **Undo**: di chuyển snapshot hiện tại sang redo, khôi phục snapshot trước.
- **Redo**: di chuyển snapshot từ redo sang undo, khôi phục.

### 2.4 Khi nào push snapshot?

- Sau khi commit một annotation.
- Sau khi xóa annotation.
- Sau khi di chuyển layer (v1.x).
- **Không push** khi chỉ di chuyển/resize selection (tùy quyết định).

---

## 3. Liên hệ với các phần khác

```
History
    ├── Annotation: danh sách chú thích trong snapshot
    ├── Config: undoLimit
    ├── State: gọi push/undo/redo
    ├── UI: phím tắt Ctrl+Z / Ctrl+Shift+Z
    └── Rendering.Skia: render snapshot hiện tại
```

---

## 4. Dùng trong PoC

Trong Phase 0, History không cần thiết. Có thể bỏ qua.

---

## 5. Dùng trong MVP

Trong Phase 1, History cần:

- Undo/redo cơ bản (`Ctrl+Z`, `Ctrl+Shift+Z`).
- Giới hạn stack.
- Push sau mỗi lần commit annotation.
- Push sau khi xóa annotation.

---

## 6. Câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Có push snapshot khi di chuyển/resize selection không? | Ảnh hưởng undo behavior |
| Có gộp nhiều nét Pencil liên tiếp thành một bước undo? | Ảnh hưởng UX khi vẽ tự do |
| Giới hạn stack mặc định bao nhiêu? | Ảnh hưởng bộ nhớ |
| Có cần lưu counter state không? | Ảnh hưởng CircleCounter tool |

---

## 7. Các file sẽ được thiết kế trong thư mục này

| File | Nội dung |
|------|----------|
| `05_02_Snapshot.md` | Cấu trúc snapshot |
| `05_03_HistoryStack.md` | Undo/redo stack operations |
| `05_04_Integration.md` | Tích hợp với Annotation và State |
| `05_05_CounterRestoration.md` | Khôi phục CircleCounter index |

---

## 8. Kết nối với code

Triển khai trong:

- `src/FShot.Core/Domain/History.fs`
- `tests/FShot.Core.Tests/Domain/HistoryTests.fs`

---

*History là chủ đề thứ năm. Sau khi chốt, chúng ta chuyển sang Export — lưu/copy ảnh.*
