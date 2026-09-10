# Selection — Tổng quan thiết kế

> Thư mục này thiết kế vùng chọn (selection) trong overlay tương tác.  
> Selection là tính năng trung tâm của MVP: người dùng kéo vùng, điều chỉnh kích thước, di chuyển, rồi mới vẽ chú thích hoặc xuất ảnh.

---

## 1. Mục đích

`Selection` định nghĩa:

- **Vùng chọn**: hình chữ nhật trong Virtual Screen space.
- **Thao tác**: tạo vùng, di chuyển, co giãn bằng chuột và bàn phím.
- **8 điểm neo** (resize handles): 4 góc + 4 cạnh.
- **Hit-testing**: xác định người dùng đang tương tác với phần nào.
- **Overlay tối**: phần ngoài vùng chọn được làm mờ.

Selection phải:

- Pure F#, không phụ thuộc UI.
- Hỗ trợ Mixed DPI và tọa độ ảo.
- Cho phép undo/redo các thao tác di chuyển/resize nếu cần (hoặc không).

---

## 2. Phạm vi thiết kế

### 2.1 Kiểu dữ liệu chính

| Kiểu | Vai trò |
|------|---------|
| `Selection` | Vùng chọn hiện tại: bounds + trạng thái tương tác |
| `ResizeHandle` | 8 vị trí neo để co giãn |
| `SelectionState` | Idle / Moving / Resizing / Selecting |

### 2.2 Thao tác

- **Create**: kéo chuột từ điểm bắt đầu đến điểm kết thúc.
- **Move**: kéo bên trong vùng chọn.
- **Resize**: kéo các điểm neo.
- **Nudge**: phím mũi tên dịch 1 px.
- **Keyboard Resize**: `Shift + Arrow` co giãn 1 px.
- **Symmetric Resize**: `Ctrl + Shift + Arrow` co giãn đối xứng.
- **Select All**: `Ctrl + A` mở rộng vùng chọn ra toàn màn hình chụp.
- **Cancel**: `Esc` hủy vùng chọn / đóng overlay.

### 2.3 Hit-testing

Xác định con trỏ đang ở đâu:

- Trên handle nào.
- Bên trong vùng chọn.
- Bên ngoài vùng chọn.

### 2.4 Không thuộc phạm vi

- Vẽ overlay tối (thuộc Rendering).
- Vẽ đường viền và handles (thuộc Rendering).
- Xử lý sự kiện chuột trực tiếp (thuộc UI).

---

## 3. Liên hệ với các phần khác

```
Selection
    ├── Geometry: Rect, Point, hit-test logic
    ├── Capture: nhận screenshot và virtual bounds
    ├── Annotation: cung cấp vùng crop cho export
    ├── History: có thể lưu snapshot khi di chuyển vùng (tùy chọn)
    ├── Rendering.Skia: vẽ selection, handles, overlay tối
    └── UI: chuyển pointer events thành selection commands
```

---

## 4. Dùng trong PoC

Trong Phase 0, Selection cần kiểm chứng:

1. Kéo tạo vùng chọn.
2. Di chuyển vùng chọn.
3. Co giãn bằng handles.
4. Overlay tối render đúng vùng.

---

## 5. Dùng trong MVP

Trong Phase 1, Selection cần hỗ trợ đầy đủ:

- 8 handles với cursor phù hợp.
- Move / resize bằng chuột.
- Nudge 1 px, keyboard resize 1 px.
- Symmetric resize.
- Select all.
- Cancel.
- Minimum size enforcement.
- Constrain inside capture area.
- XYWH display (optional in MVP).

---

## 6. Câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Có push snapshot vào history khi di chuyển/resize vùng không? | Ảnh hưởng undo behavior |
| Có tách Moving/Resizing thành state riêng hay gộp trong Selected? | Ảnh hưởng OverlayState machine |
| Handle size / tolerance bao nhiêu pixel? | Ảnh hưởng UX |
| Có cần snap-to-grid trong MVP không? | Thuộc [C], có thể để sau |
| Có giữ nguyên tỉ lệ khi resize bằng Shift? | Thuộc [C] |

---

## 7. Các file sẽ được thiết kế trong thư mục này

| File | Nội dung |
|------|----------|
| `03_02_SelectionModel.md` | Kiểu dữ liệu Selection |
| `03_03_ResizeHandles.md` | 8 điểm neo và hit-testing |
| `03_04_MouseOperations.md` | Kéo tạo vùng, move, resize |
| `03_05_KeyboardOperations.md` | Nudge, keyboard resize, select all |
| `03_06_Constraints.md` | Minimum size, clamp inside capture area |
| `03_07_OverlayDimming.md` | Phần tối ngoài vùng chọn (hợp tác với Rendering) |

---

## 8. Kết nối với code

Triển khai trong:

- `src/FShot.Core/Domain/Selection.fs`
- `src/FShot.Core/State/OverlayState.fs`
- `tests/FShot.Core.Tests/Domain/SelectionTests.fs`

---

*Selection là chủ đề thứ ba. Sau khi chốt, chúng ta chuyển sang Annotation — các công cụ vẽ trên vùng chọn.*
