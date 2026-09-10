# Constraints — Thiết kế chi tiết

> Tài liệu này mô tả các ràng buộc khi tạo và điều chỉnh vùng chọn.

---

## 1. Kích thước tối thiểu

Vùng chọn phải đủ lớn để hiển thị handles và số đo. Giả sử kích thước tối thiểu là `minWidth × minHeight`, ví dụ `20 × 20`.

Nếu kích thước sau resize nhỏ hơn, hệ thống điều chỉnh để đạt tối thiểu.

Công thức:

`W' = max(W, minWidth)`
`H' = max(H, minHeight)`

---

## 2. Giới hạn trong vùng ảnh chụp

Vùng chọn không được vượt ra ngoài `CaptureResult.VirtualBounds`. Công thức giới hạn đã mô tả trong `01_03_Rect.md`:

`X' = max(L, min(X, R - W))`
`Y' = max(T, min(Y, B - H))`

Ví dụ: vùng chọn `(1800, 900, 300, 200)`, capture bounds `(0, 0, 1920, 1080)`.

- `X' = max(0, min(1800, 1920 - 300)) = max(0, 1620) = 1620`
- `Y' = max(0, min(900, 1080 - 200)) = max(0, 880) = 880`

Vùng sau khi giới hạn: `(1620, 880, 300, 200)`.

---

## 3. Giữ tỉ lệ khi resize

Khi người dùng giữ phím Shift trong lúc resize góc, vùng chọn giữ nguyên tỉ lệ:

`aspectRatio = originalWidth / originalHeight`

Khi thay đổi chiều rộng: `newHeight = newWidth / aspectRatio`.
Khi thay đổi chiều cao: `newWidth = newHeight * aspectRatio`.

---

## 4. Kết nối với code

- `src/FShot.Core/Domain/Selection.fs`
- `src/FShot.Core/Geometry/Operations.fs`
