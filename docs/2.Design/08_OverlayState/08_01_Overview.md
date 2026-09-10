# OverlayState — Tổng quan thiết kế

> Thư mục này thiết kế state machine cho cửa sổ overlay tương tác.  
> OverlayState là "bộ não" điều phối input, selection, annotation, export.

---

## 1. Mục đích

`OverlayState` định nghĩa:

- Các **trạng thái** của overlay: Idle, Selecting, Selected, Annotating, Exporting.
- Các **sự kiện đầu vào**: mouse down/move/up, key down/up, tool shortcut.
- Cách **chuyển trạng thái** khi nhận input.
- Dữ liệu **RenderModel** để UI vẽ.

OverlayState phải:

- Pure F#, dễ test.
- Không chứa side effect.
- Trả về command để UI/platform thực hiện.

---

## 2. Phạm vi thiết kế

### 2.1 Trạng thái chính

| Trạng thái | Mô tả |
|------------|-------|
| `Idle` | Overlay hiển thị, chưa có vùng chọn |
| `Selecting` | Đang kéo chuột tạo vùng chọn |
| `Selected` | Đã có vùng chọn, sẵn sàng move/resize/tool |
| `Annotating` | Đang vẽ một chú thích |
| `Exporting` | Đang thực hiện xuất ảnh |

### 2.2 Sub-state

Có thể cần thêm:

- `MovingSelection` trong Selected.
- `ResizingSelection` trong Selected.
- `EditingText` trong Annotating.

### 2.3 Input events

- MouseDown / MouseMove / MouseUp.
- KeyDown / KeyUp.
- Tool shortcut (P/D/A/S/R/C/M/T/B/I).
- Accept (Return), Cancel (Esc/Q).
- Copy (Ctrl+C), Save (Ctrl+S), Undo/Redo (Ctrl+Z/Y).

### 2.4 Output commands

- `RenderModel`: dữ liệu để UI vẽ.
- `ExportCommand`: yêu cầu xuất ảnh.
- `CloseOverlay`: đóng overlay.

---

## 3. Liên hệ với các phần khác

```
OverlayState
    ├── Geometry: tọa độ input
    ├── Capture: screenshot đầu vào
    ├── Selection: cập nhật vùng chọn
    ├── Annotation: cập nhật chú thích
    ├── History: push/undo/redo
    ├── Export: tính export target
    ├── Config: đọc tool defaults
    ├── Rendering.Skia: nhận RenderModel
    └── UI: gửi input, nhận RenderModel
```

---

## 4. Dùng trong PoC

Trong Phase 0, OverlayState cần kiểm chứng:

1. Idle → Selecting → Selected.
2. Di chuyển và resize vùng chọn.
3. Hủy overlay.

---

## 5. Dùng trong MVP

Trong Phase 1, OverlayState cần hỗ trợ:

- Tạo và điều chỉnh vùng chọn.
- Chuyển công cụ vẽ.
- Annotating → commit → push history.
- Undo/redo.
- Export command.
- Cancel/close.

---

## 6. Câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Có tách Moving/Resizing thành state riêng không? | Ảnh hưởng độ phức tạp state machine |
| UI gửi raw events hay đã chuyển thành commands? | Ảnh hưởng coupling giữa UI và Core |
| State machine trả về RenderModel hay UI tự tính? | Ảnh hưởng testability |
| Có state cho global hotkey khi overlay đóng? | Thuộc v1.0 system tray |

---

## 7. Các file sẽ được thiết kế trong thư mục này

| File | Nội dung |
|------|----------|
| `08_02_States.md` | Định nghĩa các trạng thái |
| `08_03_Events.md` | Input events và commands |
| `08_04_Transitions.md` | Luồng chuyển trạng thái |
| `08_05_RenderModel.md` | Dữ liệu đầu ra cho UI |
| `08_06_Integration.md` | Tích hợp với Selection, Annotation, History |

---

## 8. Kết nối với code

Triển khai trong:

- `src/FShot.Core/State/OverlayState.fs`
- `tests/FShot.Core.Tests/State/OverlayStateTests.fs`

---

*OverlayState là chủ đề thứ tám. Sau khi chốt, chúng ta chuyển sang Rendering — cách vẽ scene bằng Skia.*
