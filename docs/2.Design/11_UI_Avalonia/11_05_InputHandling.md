# InputHandling — Thiết kế chi tiết

> Tài liệu này mô tả cách UI chuyển sự kiện chuột/bàn phím thành command cho domain.

---

## 1. Mục đích

UI chỉ chuyển input thô (tọa độ chuột, phím bấm) thành các hành động domain. UI không tự tính toán geometry.

---

## 2. Sự kiện chuột

### MouseDown

Gửi vị trí chuột (logical pixel trong Virtual Screen space).

OverlayState quyết định:
- Bắt đầu tạo vùng chọn.
- Bắt đầu di chuyển.
- Bắt đầu resize handle.

### MouseMove

Gửi vị trí chuột hiện tại. State cập nhật preview: selecting, moving, resizing, hoặc annotation preview.

### MouseUp

Kết thúc thao tác: finish selection/move/resize, hoặc commit annotation.

### MouseWheel

Thay đổi `StrokeWidth` khi đang ở tool vẽ.

---

## 3. Sự kiện bàn phím

### 3.1 Mapping phím sang OverlayEvent

UI chuyển `KeyEventArgs` thành chuỗi key theo format:

```text
modifiers = []
if Ctrl -> add "Ctrl"
if Shift -> add "Shift"
if Alt -> add "Alt"

keyName = Key.ToString()

if modifiers empty -> eventKey = keyName
else -> eventKey = String.Join("+", modifiers) + "+" + keyName
```

Ví dụ:

- Phím mũi tên Right → `"Right"`
- Shift + Down → `"Shift+Down"`
- Escape → `"Escape"`
- Ctrl + Backspace → `"Ctrl+Backspace"`
- Enter → `"Enter"`
- Ctrl + S → `"Ctrl+S"`

Chuỗi này được gửi vào `OverlayState` dưới dạng `KeyDown(eventKey)` để xử lý các phím không phải tool: nudge, resize keyboard, Esc, Enter, Ctrl+S, Ctrl+C, v.v.

### 3.2 Phím tắt chuyển công cụ annotation

Các phím tắt chuyển công cụ annotation được xử lý riêng trong UI bằng `e.Key` (mã phím vật lý) thay vì chuỗi `KeyEventArgs.Key.ToString()`. Lý do: trên Windows, bộ gõ tiếng Việt (và một số bộ gõ ngôn ngữ khác) có thể bắt mất phím `P`, `L`, `A`, `R`, `C`, `M`, `T`, `B`, `S` và không trả lại chuỗi như mong muốn. Dùng `e.Key` đảm bảo phím vật lý vẫn được nhận diện ngay cả khi bộ gõ đang ở chế độ tiếng Việt.

Mapping phím tắt hiện tại:

| Phím vật lý | Công cụ | OverlayEvent |
|-------------|---------|--------------|
| `P` | Pencil | `SelectTool PencilTool` |
| `L` | Line | `SelectTool LineTool` |
| `A` | Arrow | `SelectTool ArrowTool` |
| `R` | Rectangle | `SelectTool RectangleTool` |
| `C` | Circle | `SelectTool CircleTool` |
| `M` | Marker | `SelectTool MarkerTool` |
| `T` | Text | `SelectTool TextTool` |
| `B` | Pixelate | `SelectTool PixelateTool` |
| `S` | Selection | `SelectTool SelectionTool` |

> **Nợ kỹ thuật (Tech Debt):** Việc dùng `e.Key` chỉ phù hợp với layout bàn phím US/QWERTY và có thể gây xung đột với bộ gõ hoặc shortcut hệ thống. Cần giải quyết sau bằng cách:
> - Dùng `KeyGesture` của Avalonia kết hợp với `InputManager`.
> - Hoặc lắng nghe sự kiện trước khi bộ gõ xử lý (preview key) trên window level.
> - Hoặc cung cấp cấu hình phím tắt do người dùng tự định nghĩa.

### 3.3 Bảng hành động bàn phím khác

| Phím | Hành động |
|------|-----------|
| Arrow | Nudge selection 1px (Shift = 10px) |
| Shift + Arrow | Resize selection 1px |
| Ctrl + Shift + Arrow | Resize đối xứng |
| Ctrl + A | Select all (mở rộng ra toàn capture area) |
| Esc | Hủy hoặc đóng overlay |
| Enter / Ctrl + S | Xuất ảnh |
| Ctrl + C | Copy clipboard |
| Ctrl + Z | Undo |
| Ctrl + Shift + Z / Ctrl + Y | Redo |

---

## 4. Chuyển tọa độ

Avalonia cung cấp tọa độ chuột tương đối với control. Cần cộng thêm vị trí cửa sổ để có tọa độ Virtual Screen:

`virtualX = controlX + windowX`
`virtualY = controlY + windowY`

---

## 5. Kết nối với code

- `src/FShot.UI/Controls/CaptureCanvas.fs`
- `src/FShot.UI/Windows/CaptureOverlayWindow.fs`
- `src/FShot.Core/State/OverlayState.fs`
