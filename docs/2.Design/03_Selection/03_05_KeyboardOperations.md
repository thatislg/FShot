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

- Kéo cạnh ra xa tâm vùng chọn khi nhấn Shift + Arrow theo hướng tương ứng.
- Nếu cần co vào trong (thu nhỏ) thì dùng Shift + Arrow ngược hướng; công thức tương tự nhưng `W'`/`H'` giảm 1px và `X'`/`Y'` tăng 1px tùy cạnh.

### 3.4 Trường hợp biên

- Vùng đã ở biên capture area + Shift + Left/Up: `X'`/`Y'` bị clamp, `W'`/`H'` có thể không tăng đủ 1px.
- Vùng đạt kích thước tối thiểu + Shift + Arrow ngược hướng: `FinishInteraction` sẽ hủy vùng nếu nhỏ hơn min; nhưng với keyboard resize trực tiếp, ta không gọi `FinishInteraction`, nên cần kiểm tra `IsValid` sau mỗi bước.

---

## 4. Select all

Tổ hợp `Ctrl + A` khi `Selected` hoặc `Idle` chọn toàn bộ capture area:

```text
Bounds = (L, T, W_c, H_c)
```

Trong MVP, select all được xử lý gián tiếp qua các command `Save`/`Copy` từ `Idle`. Tuy nhiên, khi đã có vùng chọn, `Ctrl + A` có thể mở rộng vùng về full screen. Phần này thuộc P1.11 hoặc P2 tùy quyết định.

---

## 5. Kết nối với code

- `src/FShot.Core/State/OverlayState.fs`: xử lý `KeyDown` cho phím mũi tên và Shift+Arrow trong trạng thái `Selected`.
- `src/FShot.Core/Domain/Selection.fs`: cung cấp `ApplyConstraints` và `Bounds`.
- UI layer (Avalonia): chuyển đổi sự kiện phím thành `OverlayEvent.KeyDown` với chuỗi key.

---

*P1.09 tập trung vào nudge 1px. P1.10 mở rộng sang Shift + Arrow. P1.11 xử lý Esc / Ctrl+Backspace.*
