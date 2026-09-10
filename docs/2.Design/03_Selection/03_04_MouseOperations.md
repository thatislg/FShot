# MouseOperations — Thiết kế chi tiết

> Tài liệu này mô tả cách chuột tương tác với vùng chọn.

---

## 1. Luồng cơ bản

### 1.1 Nhấn chuột (MouseDown)

1. Hit-test tại vị trí chuột.
2. Nếu trên handle: chuyển sang `Resizing`, lưu handle đang active và `originalBounds`.
3. Nếu trong vùng chọn: chuyển sang `Moving`, lưu điểm bắt đầu.
4. Nếu ngoài vùng chọn: chuyển sang `Selecting`, lưu điểm bắt đầu.

### 1.2 Kéo chuột (MouseMove)

- `Selecting`: cập nhật `Bounds` từ điểm bắt đầu đến vị trí chuột hiện tại.
- `Moving`: tính vector dịch chuyển, cộng vào `originalBounds`.
- `Resizing`: tính tọa độ mới dựa trên handle đang kéo.

### 1.3 Thả chuột (MouseUp)

- `Selecting`: chuyển sang `Selected` nếu vùng hợp lệ, hoặc `Idle` nếu quá nhỏ.
- `Moving`: chuyển sang `Selected`.
- `Resizing`: chuyển sang `Selected`.

---

## 2. Công thức resize theo handle

Khi kéo handle, một hoặc nhiều cạnh của vùng chọn thay đổi. Cho `originalBounds = (X, Y, W, H)` và chuột hiện tại tại `(mx, my)`.

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

Ví dụ: vùng gốc `(100, 80, 300, 200)`, kéo BottomRight đến `(450, 350)`.

- `W = 450 - 100 = 350`
- `H = 350 - 80 = 270`

Vùng mới là `(100, 80, 350, 270)`.

---

## 3. Công thức di chuyển

Cho điểm bắt đầu kéo `(sx, sy)` và điểm chuột hiện tại `(mx, my)`.

`dx = mx - sx`
`dy = my - sy`

`newX = originalX + dx`
`newY = originalY + dy`

Ví dụ: vùng gốc `(100, 80, 300, 200)`, bắt đầu kéo tại `(150, 150)`, chuột hiện tại `(200, 130)`.

- `dx = 200 - 150 = 50`
- `dy = 130 - 150 = -20`
- `newX = 100 + 50 = 150`
- `newY = 80 - 20 = 60`

Vùng mới là `(150, 60, 300, 200)`.

---

## 4. Kết nối với code

- `src/FShot.Core/Domain/Selection.fs`
- `src/FShot.Core/State/OverlayState.fs`
