# ResizeHandles — Thiết kế chi tiết

> Tài liệu này mô tả 8 điểm neo của vùng chọn và cách hit-test chúng.

---

## 1. 8 điểm neo

Mỗi vùng chọn có 8 điểm neo để co giãn:

- 4 góc: TopLeft, TopRight, BottomLeft, BottomRight.
- 4 cạnh: Top, Right, Bottom, Left.

## 2. Vị trí các handle

Cho vùng chọn có `Left = L`, `Top = T`, `Right = R`, `Bottom = B`, và kích thước handle `s`.

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

Ví dụ: vùng `(100, 80, 300, 230)`, tức `L=100, T=80, R=300, B=230`.

- TopLeft tại `(100, 80)`.
- Top tại `(200, 80)`.
- BottomRight tại `(300, 230)`.

## 3. Hit-test handle

Mỗi handle là hình vuông nhỏ kích thước `s × s`. Để dễ bắt, mở rộng bán kính dung sai `tolerance`:

`effectiveBounds = handleBounds inflated by tolerance`

Nếu con trỏ nằm trong `effectiveBounds`, handle được chọn.

Ví dụ: handle tại `(200, 80)`, kích thước 8, tolerance 6.

- `effectiveLeft = 200 - 4 - 6 = 190`
- `effectiveTop = 80 - 4 - 6 = 70`
- `effectiveRight = 200 + 4 + 6 = 210`
- `effectiveBottom = 80 + 4 + 6 = 90`

Con trỏ `(205, 85)` nằm trong vùng hiệu lực.

---

## 4. Thứ tự ưu tiên hit-test

Khi con trỏ nằm gần nhiều handle, ưu tiên theo thứ tự:

1. Góc.
2. Cạnh.
3. Bên trong vùng chọn.
4. Bên ngoài.

---

## 5. Kết nối với code

- `src/FShot.Core/Domain/Selection.fs`
- `src/FShot.Core/Geometry/Operations.fs` (hitHandle)
