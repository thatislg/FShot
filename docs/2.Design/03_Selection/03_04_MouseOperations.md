# MouseOperations — Thiết kế chi tiết

> Tài liệu này mô tả cách chuột tương tác với vùng chọn trong F-Shot, từ tạo vùng mới đến di chuyển và co giãn vùng đã có.

---

## 1. Mục đích

MouseOperations định nghĩa luồng xử lý sự kiện chuột liên quan đến vùng chọn:

- Tạo vùng chọn mới bằng cách kéo chuột.
- Di chuyển vùng chọn đã tạo.
- Co giãn vùng chọn qua 8 điểm neo.
- Quyết định hành vi khi nhấn chuột tại các vị trí khác nhau.

Mục tiêu là đảm bảo vùng chọn phản hồi chính xác, mượt mà và có thể kiểm thử được.

---

## 2. Luồng cơ bản

### 2.1 Nhấn chuột (MouseDown / PointerPressed)

Khi người dùng nhấn chuột trái, hệ thống thực hiện hit-test theo thứ tự ưu tiên:

1. **Trên handle**: chuyển sang trạng thái `Resizing`, lưu handle đang active và `OriginalBounds`.
2. **Bên trong vùng chọn**: chuyển sang trạng thái `Moving`, lưu điểm bắt đầu làm `DragStart`.
3. **Bên ngoài vùng chọn**: hủy vùng chọn cũ và chuyển sang `Selecting`, lưu điểm bắt đầu.

Thứ tự ưu tiên này đảm bảo handle được bắt ngay cả khi nó nằm trong vùng chọn.

### 2.2 Kéo chuột (MouseMove / PointerMoved)

Tùy trạng thái hiện tại:

- **Selecting**: cập nhật `Bounds` từ điểm bắt đầu đến vị trí chuột hiện tại.
- **Moving**: tính vector dịch chuyển so với `DragStart`, cộng vào `OriginalBounds`.
- **Resizing**: tính tọa độ mới dựa trên handle đang kéo và `OriginalBounds`.

Trong mọi trường hợp, `Bounds` sau khi cập nhật phải được `ApplyConstraints` để:

- Nằm trong `CaptureResult.VirtualBounds`.
- Không nhỏ hơn kích thước tối thiểu khi đang `Resizing`.

### 2.3 Thả chuột (MouseUp / PointerReleased)

- **Selecting**: chuyển sang `Selected` nếu vùng hợp lệ (`IsValid`), ngược lại quay về `Idle`.
- **Moving**: chuyển sang `Selected`.
- **Resizing**: chuyển sang `Selected`, áp dụng kích thước tối thiểu cuối cùng.

---

## 3. Tạo vùng chọn từ hai điểm (Selecting)

Gọi điểm nhấn chuột ban đầu là `P_s = (sx, sy)` và điểm chuột hiện tại là `P_c = (cx, cy)`.

Vùng chọn tạm thời trong trạng thái `Selecting` là hình chữ nhật chuẩn hóa `R_t = Normalize(P_s, P_c)`, được định nghĩa:

```text
X_t = min(sx, cx)
Y_t = min(sy, cy)
W_t = |cx - sx|
H_t = |cy - sy|
```

Tính chất:

- `W_t` và `H_t` luôn không âm bất kể hướng kéo.
- Nếu `cx < sx`, cạnh trái của `R_t` là `cx`; nếu `cy < sy`, cạnh trên là `cy`.
- Đường đi của chuột giữa `P_s` và `P_c` không ảnh hưởng đến `R_t`; chỉ có điểm đầu và điểm cuối làm căn cứ.

Trong suốt quá trình kéo, mỗi sự kiện `PointerMoved` sinh ra một `P_c` mới và `R_t` được tính lại từ `P_s` ban đầu cộng với `P_c` mới. Trạng thái giữ nguyên `Selecting` cho đến khi nhả chuột.

### 3.1 Hành vi khi hoàn tất tạo vùng

Khi nhả chuột, hệ thống kiểm tra kích thước cuối cùng của `R_t`:

- Nếu `W_t >= MinWidth` và `H_t >= MinHeight`, trạng thái chuyển sang `Selected` và `Bounds = R_t`.
- Ngược lại, vùng chọn bị hủy: trạng thái trở về `Idle` và `Bounds` là hình chữ nhật rỗng.

`MinWidth` và `MinHeight` là các hằng số domain (trong MVP là 10×10 logical pixels). Kiểm tra kích thước tối thiểu **chỉ** thực hiện tại thời điểm hoàn tất, không trong lúc kéo, để người dùng có thể thả chuột ở bất kỳ vị trí nào gần điểm bắt đầu mà không bị ép kéo tiếp.

### 3.2 Trường hợp biên

- Nhấn và nhả tại cùng một điểm: `W_t = 0`, `H_t = 0` → vùng bị hủy.
- Kéo dọc theo một trục: nếu `cx = sx` thì `W_t = 0`; nếu `cy = sy` thì `H_t = 0`. Cả hai trường hợp đều dẫn đến hủy vùng vì nhỏ hơn kích thước tối thiểu.
- Kéo ra ngoài phạm vi chụp: `R_t` vẫn được tính theo hai điểm, sau đó bị clamp theo mục 6.1. Nếu sau clamp vùng vẫn đủ lớn thì được giữ.

---

## 4. Công thức resize theo handle

Khi kéo handle, một hoặc nhiều cạnh của vùng chọn thay đổi. Cho `OriginalBounds = (X, Y, W, H)` và chuột hiện tại tại `(mx, my)`.

| Handle | Công thức |
|--------|-----------|
| TopLeft | `X = mx`, `Y = my`, `W = right - mx`, `H = bottom - my` |
| Top | `Y = my`, `H = bottom - my` |
| TopRight | `W = mx - X`, `Y = my`, `H = bottom - my` |
| Left | `X = mx`, `W = right - mx` |
| Right | `W = mx - X` |
| BottomLeft | `X = mx`, `W = right - mx`, `H = my - Y` |
| Bottom | `H = my - Y` |
| BottomRight | `W = mx - X`, `H = my - Y` |

Trong đó `right = X + W`, `bottom = Y + H` của vùng gốc.

Ví dụ: vùng gốc `(100, 80, 300, 200)`, kéo BottomRight đến `(450, 350)`:

- `W = 450 - 100 = 350`
- `H = 350 - 80 = 270`

Vùng mới: `(100, 80, 350, 270)`.

---

## 5. Công thức di chuyển

Cho điểm bắt đầu kéo `(sx, sy)` và điểm chuột hiện tại `(mx, my)`:

```text
dx = mx - sx
dy = my - sy

newX = originalX + dx
newY = originalY + dy
```

Ví dụ: vùng gốc `(100, 80, 300, 200)`, bắt đầu kéo tại `(150, 150)`, chuột hiện tại `(200, 130)`:

- `dx = 200 - 150 = 50`
- `dy = 130 - 150 = -20`
- `newX = 100 + 50 = 150`
- `newY = 80 - 20 = 60`

Vùng mới: `(150, 60, 300, 200)`.

---

## 6. Ràng buộc trong khi tương tác

### 6.1 Giới hạn trong capture area

Cho capture area `C = (L, T, W_c, H_c)` với cạnh phải `R = L + W_c` và cạnh dưới `B = T + H_c`.

Cho một hình chữ nhật `R = (X, Y, W, H)` sau khi cập nhật. Giá trị bị clamp `R' = Clamp(C, R)` được định nghĩa:

```text
W' = min(W, W_c)
H' = min(H, H_c)

X' = max(L, min(X, R - W'))
Y' = max(T, min(Y, B - H'))
```

Các tính chất:

- `W'` và `H'` không bao giờ vượt quá kích thước capture area.
- `X'` và `Y'` được dịch chuyển về phía trong để toàn bộ `R'` nằm trong `C`.
- Nếu `W > W_c`, `R'` sẽ bị ép về `X' = L` và `W' = W_c`; tương tự cho chiều cao.
- Clamp này áp dụng cho cả `Selecting`, `Moving` và `Resizing`.

### 6.2 Kích thước tối thiểu

Khi đang kéo tạo vùng mới (`Selecting`), kích thước có thể nhỏ hơn min — chỉ kiểm tra tại thời điểm `FinishSelecting`.

Khi đang `Resizing`, kích thước không được nhỏ hơn min trong suốt quá trình kéo để tránh vùng chọn bị đảo chiều hoặc biến mất.

---

## 7. Phạm vi trong MVP

Trong MVP cần hỗ trợ:

- Tạo vùng chọn bằng kéo tự do theo mọi hướng.
- Di chuyển vùng chọn đã chọn.
- Co giãn vùng chọn qua 8 handles.
- Clamp vào capture area.
- Hủy vùng nếu quá nhỏ.

Không cần trong MVP:

- Giữ tỉ lệ khi resize (Shift lock ratio thuộc P2).
- Resize đối xứng qua tâm.
- Kéo vùng chọn bằng cảm ứng đa điểm.

---

## 8. Kết nối với code

- `src/FShot.Core/Domain/Selection.fs`: triển khai `StartSelecting`, `UpdateSelecting`, `FinishSelecting`, `StartMoving`, `UpdateMoving`, `StartResizing`, `UpdateResizing`, `FinishInteraction`.
- `src/FShot.Core/State/OverlayState.fs`: gọi các phương thức trên trong `update`.
- `src/FShot.Core/Geometry/Operations.fs`: `rectFromPoints`, `rectClamp`.

---

*MouseOperations là nền tảng cho mọi tương tác chuột với vùng chọn. P1.06 tập trung vào việc tạo vùng chọn tự do; P1.07 và P1.08 mở rộng sang resize và move.*
