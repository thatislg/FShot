# ResizeHandles — Thiết kế chi tiết

> Tài liệu này mô tả 8 điểm neo của vùng chọn và cách hit-test chúng.

---

## 1. 8 điểm neo

Mỗi vùng chọn có 8 điểm neo để co giãn:

- 4 góc: TopLeft, TopRight, BottomLeft, BottomRight.
- 4 cạnh: Top, Right, Bottom, Left.

## 2. Vị trí các handle

Cho vùng chọn có `Left = L`, `Top = T`, `Right = R`, `Bottom = B`.

Vị trí trung tâm của mỗi handle:

| Handle | Trung tâm |
|--------|-----------|
| TopLeft | `(L, T)` |
| Top | `((L + R) / 2, T)` |
| TopRight | `(R, T)` |
| Left | `(L, (T + B) / 2)` |
| Right | `(R, (T + B) / 2)` |
| BottomLeft | `(L, B)` |
| Bottom | `((L + R) / 2, B)` |
| BottomRight | `(R, B)` |

Ví dụ: vùng `(100, 80, 300, 200)`, tức `L=100, T=80, R=400, B=280`.

- TopLeft tại `(100, 80)`.
- Top tại `(250, 80)`.
- BottomRight tại `(400, 280)`.

Lưu ý: cách tính trung tâm dùng `(left + right) / 2` thay vì `X + Width / 2` để tránh sai số làm tròn và duy trì tính đối xứng.

## 3. Hit-test handle

Mỗi handle được biểu diễn thành hình vuông kích thước `s × s` tại trung tâm. Để dễ bắt khi chuột ở gần, mở rộng vùng hiệu lực đều ra `tolerance` theo 4 hướng. Vùng hiệu lực `E` có:

```text
E.Left   = Center.X - s/2 - tolerance
E.Top    = Center.Y - s/2 - tolerance
E.Width  = s + 2 * tolerance
E.Height = s + 2 * tolerance
```

Nếu con trỏ `P` nằm trong `E`, handle được chọn. Công thức tương đương:

```text
|P.X - Center.X| <= s/2 + tolerance
|P.Y - Center.Y| <= s/2 + tolerance
```

Ví dụ: handle tại `(400, 280)`, kích thước `s = 8`, tolerance `6`.

- `E.Left = 400 - 4 - 6 = 390`
- `E.Top = 280 - 4 - 6 = 270`
- `E.Right = 400 + 4 + 6 = 410`
- `E.Bottom = 280 + 4 + 6 = 290`

Con trỏ `(405, 285)` nằm trong vùng hiệu lực, nên handle BottomRight được chọn.

---

## 4. Thứ tự ưu tiên hit-test

Khi con trỏ nằm gần nhiều handle (ví dụ góc và hai cạnh giao nhau), `HandleCenters` được duyệt theo thứ tự cố định:

1. TopLeft
2. Top
3. TopRight
4. Left
5. Right
6. BottomLeft
7. Bottom
8. BottomRight

Handle đầu tiên trong danh sách mà con trỏ nằm trong vùng hiệu lực được chọn. Thứ tự này ưu tiên góc trước cạnh, phù hợp với mong đợi người dùng vì góc thường nhỏ hơn và khó bắt hơn cạnh.

---

## 5. Co giãn vùng chọn qua handle

Khi một handle đang được kéo, vùng chọn được tính lại từ `OriginalBounds` và vị trí chuột hiện tại `(mx, my)`. Công thức cho từng handle:

| Handle | Công thức |
|--------|-----------|
| TopLeft | `X = mx`, `Y = my`, `W = right - mx`, `H = bottom - my` |
| Top | `Y = my`, `H = bottom - my` |
| TopRight | `X = orig.X`, `Y = my`, `W = mx - orig.X`, `H = bottom - my` |
| Left | `X = mx`, `W = right - mx` |
| Right | `W = mx - orig.X` |
| BottomLeft | `X = mx`, `Y = orig.Y`, `W = right - mx`, `H = my - orig.Y` |
| Bottom | `H = my - orig.Y` |
| BottomRight | `W = mx - orig.X`, `H = my - orig.Y` |

Trong đó `right = OriginalBounds.Right`, `bottom = OriginalBounds.Bottom`.

### 5.1 Ràng buộc khi resize

Trong quá trình resize, `Bounds` sau khi cập nhật được `ApplyConstraints` để:

- Giữ `X'` và `Y'` trong capture area.
- Giữ `Width'` và `Height'` không vượt quá capture area.

Nếu sau công thức `Width` hoặc `Height` âm, clamp sẽ ép về 0 hoặc giá trị tối thiểu tùy tình huống. Kết quả cuối cùng sau `FinishInteraction` kiểm tra `IsValid`; nếu không hợp lệ, vùng chọn bị hủy.

### 5.2 Kích thước tối thiểu khi resize

`MinWidth` và `MinHeight` là các hằng số domain. Sau khi thả chuột (`FinishInteraction`), nếu vùng không đạt kích thước tối thiểu thì trạng thái quay về `Idle` và `Bounds` rỗng. Trong khi đang kéo, không ép cứng min-size để tránh cảm giác giật cục; chỉ clamp vào capture area.

---

## 6. Kết nối với code

- `src/FShot.Core/Domain/Selection.fs`: định nghĩa `ResizeHandle`, `HandleCenters`, `HitTestHandle`, `StartResizing`, `UpdateResizing`.
- `src/FShot.Core/Geometry/Operations.fs`: `rectInflate`, `rectContainsPoint` phục vụ `hitHandle`.
- `src/FShot.Core/State/OverlayState.fs`: chuyển trạng thái `Selected` → `Resizing` khi `HitTestHandle` trả về handle.

---

*P1.07 tập trung đảm bảo 8 handles được đặt đúng vị trí, hit-test chính xác, và co giãn theo đúng công thức.
