# SelectionModel — Thiết kế chi tiết

> Tài liệu này mô tả kiểu dữ liệu `Selection` và các trạng thái tương tác của nó.

---

## 1. Selection là gì?

`Selection` là vùng chữ nhật trong Virtual Screen space mà người dùng chọn để chụp. Nó gồm:

- `Bounds`: hình chữ nhật hiện tại.
- `State`: trạng thái tương tác.
- `OriginalBounds`: vùng gốc trước khi đang resize/move, dùng để tính toán khi kéo.
- `ActiveHandle`: điểm neo đang được kéo (nếu có).

---

## 2. Các trạng thái

| Trạng thái | Ý nghĩa |
|------------|---------|
| `Idle` | Chưa có vùng chọn, chờ người dùng bắt đầu kéo. |
| `Selecting` | Đang kéo chuột để tạo vùng chọn từ điểm bắt đầu. |
| `Selected` | Đã có vùng chọn, chờ thao tác move/resize. |
| `Moving` | Đang kéo vùng chọn đến vị trí mới. |
| `Resizing` | Đang kéo một handle để co giãn vùng chọn. |

---

## 3. Công thức tạo vùng chọn từ hai điểm

Khi người dùng nhấn chuột tại `A` và kéo đến `B`, vùng chọn được tạo bằng công thức:

`X = min(Ax, Bx)`
`Y = min(Ay, By)`
`Width = |Bx - Ax|`
`Height = |By - Ay|`

Ví dụ: A = `(100, 100)`, B = `(400, 300)`.

- `X = min(100, 400) = 100`
- `Y = min(100, 300) = 100`
- `Width = 400 - 100 = 300`
- `Height = 300 - 100 = 200`

Vùng chọn là `(100, 100, 300, 200)`.

---

## 4. Công thức di chuyển vùng chọn

Khi đang `Moving`, vị trí mới = vị trí cũ + vector dịch chuyển:

`newX = originalX + dx`
`newY = originalY + dy`

Ví dụ: vùng gốc `(100, 100, 300, 200)`, chuột dịch `(50, -30)`.

- `newX = 100 + 50 = 150`
- `newY = 100 - 30 = 70`

Vùng mới là `(150, 70, 300, 200)`.

---

## 5. Các câu hỏi cần quyết định

- Kích thước tối thiểu của vùng chọn là bao nhiêu?
- Có lưu `OriginalBounds` trong Selection hay dùng state riêng?

---

## 6. Kết nối với code

- `src/FShot.Core/Domain/Selection.fs`
- `src/FShot.Core/State/OverlayState.fs`
