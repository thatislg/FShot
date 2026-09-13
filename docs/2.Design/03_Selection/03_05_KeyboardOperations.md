# KeyboardOperations — Thiết kế chi tiết

> Tài liệu này thiết kế nudge, keyboard resize, select all bằng bàn phím.

---

## 1. Mục đích

KeyboardOperations định nghĩa các thao tác điều chỉnh vùng chọn bằng bàn phím, bổ sung cho chuột khi người dùng cần độ chính xác pixel.

---

## 2. Nudge — dịch chuyển 1px bằng phím mũi tên

### 2.1 Kích hoạt

Nudge chỉ hoạt động khi `Selection.State = Selected` và `AnnotationInteraction = NoAnnotation`. Nếu đang trong bất kỳ thao tác annotation nào, phím mũi tên được UI layer xử lý (ví dụ di chuyển con trỏ trong textbox) và không đi qua OverlayState.

### 2.2 Công thức

Cho vùng chọn hiện tại `Bounds = (X, Y, W, H)` và phím mũi tên:

| Phím | Công thức |
|------|-----------|
| Left  | `X' = X - 1`, `Y' = Y` |
| Right | `X' = X + 1`, `Y' = Y` |
| Up    | `X' = X`, `Y' = Y - 1` |
| Down  | `X' = X`, `Y' = Y + 1` |

Sau khi tính `Bounds'` mới, áp dụng `ApplyConstraints(CaptureArea)` để đảm bảo vùng không bị đẩy ra ngoài capture area.

### 2.3 Tính chất

- Mỗi lần nhấn phím mũi tên dịch chuyển đúng 1 logical pixel.
- Giữ phím mũi tên liên tục sẽ tạo ra nhiều sự kiện `KeyDown`; mỗi sự kiện dịch thêm 1px.
- Nudge không thay đổi kích thước vùng chọn.
- Nudge không cần nhấn modifier (khác với keyboard resize cần Shift).

### 2.4 Trường hợp biên

- Vùng ở biên trái (`X = L`) + nhấn Left: clamp giữ `X' = L`.
- Vùng ở biên phải (`X + W = R`) + nhấn Right: clamp giữ `X' = R - W`.
- Tương tự cho biên trên/dưới.

---

## 3. Keyboard resize — co giãn 1px bằng Shift + phím mũi tên

### 3.1 Kích hoạt

Shift + Arrow hoạt động khi `Selection.State = Selected` và `AnnotationInteraction = NoAnnotation`.

### 3.2 Công thức

| Tổ hợp phím | Cạnh thay đổi | Công thức |
|-------------|---------------|-----------|
| Shift + Left  | Left edge   | `X' = X - 1`, `W' = W + 1` |
| Shift + Right | Right edge  | `W' = W + 1` |
| Shift + Up    | Top edge    | `Y' = Y - 1`, `H' = H + 1` |
| Shift + Down  | Bottom edge | `H' = H + 1` |

Sau khi tính `Bounds'`, áp dụng `ApplyConstraints`.

### 3.3 Tính chất

- Shift + Arrow kéo cạnh ở hướng mũi tên ra xa tâm vùng chọn, làm vùng mở rộng 1px theo hướng đó.
- Trong MVP, keyboard resize chỉ hỗ trợ **mở rộng** vùng. Việc thu nhỏ (co vào trong) bằng bàn phím thuộc enhancement có thể xem xét ở P2.
- Giữ Shift + phím mũi tên liên tục sẽ tạo nhiều sự kiện; mỗi sự kiện mở rộng thêm 1px.

### 3.4 Trường hợp biên

- Vùng đã ở biên capture area + Shift + Arrow ra ngoài: `ApplyConstraints` giữ vùng trong phạm vi; nếu kéo cạnh trái/trên ra ngoài, `X'`/`Y'` bị clamp và `W'`/`H'` có thể không tăng đủ 1px.
- Vùng có kích thước tối thiểu: keyboard resize làm mở rộng vùng nên không bao giờ làm vùng nhỏ hơn min. Do đó không cần kiểm tra `IsValid` để hủy vùng trong P1.10.
- Trong `OverlayState`, nếu sau `ApplyConstraints` vùng không hợp lệ (trường hợp hiếm do clamp), vùng được reset về `Idle` để duy trì invariant.

---

## 4. Select all

Tổ hợp `Ctrl + A` khi `Selected` hoặc `Idle` chọn toàn bộ capture area:

```text
Bounds = (L, T, W_c, H_c)
```

Trong MVP, select all được xử lý gián tiếp qua các command `Save`/`Copy` từ `Idle`. Tuy nhiên, khi đã có vùng chọn, `Ctrl + A` có thể mở rộng vùng về full screen. Phần này thuộc P1.11 hoặc P2 tùy quyết định.

---

## 5. Cancel — Esc và Ctrl+Backspace

### 5.1 Kích hoạt

Hai phím tắt `Esc` và `Ctrl+Backspace` đều được UI layer chuyển thành sự kiện `KeyDown` với chuỗi tương ứng. `OverlayState` nhận diện các chuỗi này và xử lý tương đương với `Cancel`.

Các chuỗi được chấp nhận (không phân biệt hoa thường):

- `"Escape"`, `"Esc"`
- `"Ctrl+Backspace"`, `"Control+Backspace"`, `"Ctrl+Back"`

### 5.2 Hành vi theo trạng thái

| Trạng thái | Hành vi khi nhấn Esc / Ctrl+Backspace |
|------------|--------------------------------------|
| `Idle` | Đóng overlay (`CloseOverlay`). |
| `Selecting` | Hủy vùng chọn đang tạo, quay về `Idle`. |
| `Selected` | Hủy vùng chọn, quay về `Idle`. |
| `Moving` | Hủy thao tác di chuyển, khôi phục `OriginalBounds`, quay về `Selected`. |
| `Resizing` | Hủy thao tác co giãn, khôi phục `OriginalBounds`, quay về `Selected`. |
| `Annotating` (DrawingPreview / FreehandDrawing) | Hủy preview annotation, quay về `Selected`. |
| `TextEditing` (EditingText) | Hủy text input, ẩn text box, quay về `Selected`. |

### 5.3 Phân biệt Esc và Ctrl+Backspace

Trong MVP, cả hai tổ hợp có hành vi giống nhau. Sự khác biệt có thể được mở rộng trong tương lai:

- `Esc`: hủy thao tác hiện tại nhưng không đóng app khi đang ở `Idle`.
- `Ctrl+Backspace`: luôn đóng app khi ở `Idle`.

Trong MVP, để đơn giản, cả hai đều tương đương `Cancel`.

### 5.4 Kết nối với code

- `src/FShot.Core/State/OverlayState.fs`: thêm hàm `isCancelKey` và xử lý `KeyDown` cancel ở tất cả các trạng thái.
- UI layer: gửi `"Escape"` hoặc `"Ctrl+Backspace"` qua `OverlayEvent.KeyDown`.

---

## 6. Kết nối với code

- `src/FShot.Core/State/OverlayState.fs`: xử lý `KeyDown` cho phím mũi tên, Shift+Arrow, Esc/Ctrl+Backspace.
- `src/FShot.Core/Domain/Selection.fs`: cung cấp `ApplyConstraints`, `Bounds`, `Cancel`.
- UI layer (Avalonia): chuyển đổi sự kiện phím thành `OverlayEvent.KeyDown` với chuỗi key.

---

*P1.09 tập trung vào nudge 1px. P1.10 mở rộng sang Shift + Arrow. P1.11 xử lý Esc / Ctrl+Backspace.*
