# OverlayWindow — Thiết kế chi tiết

> Tài liệu này mô tả cửa sổ overlay chụp màn hình trong F-Shot UI.

---

## 1. Mục đích

Cửa sổ overlay phủ toàn bộ Virtual Screen, cho phép người dùng:

- Xem ảnh chụp nền.
- Kéo chuột tạo vùng chọn.
- Di chuyển và co giãn vùng chọn.
- Vẽ chú thích.
- Xuất ảnh.

---

## 2. Thuộc tính cửa sổ

- `WindowState = FullScreen` hoặc kích thước bằng Virtual Screen.
- `SystemDecorations = None` (không viền, không tiêu đề).
- `Topmost = true` (luôn ở trên cùng).
- `Background = Transparent` hoặc đen trong suốt.
- `CanResize = false`.
- Vị trí: `Position = (virtualLeft, virtualTop)`.
- Kích thước: `(virtualWidth, virtualHeight)`.

---

## 3. Công thức kích thước và vị trí

Cho Virtual Screen bounds `(L, T, R, B)`:

`windowX = L`
`windowY = T`
`windowWidth = R - L`
`windowHeight = B - T`

Ví dụ: Virtual Screen `(0, 0, 4480, 1440)` → cửa sổ `(0, 0, 4480, 1440)`.

---

## 4. Nội dung cửa sổ

Cửa sổ chứa một custom control duy nhất: `CaptureCanvas`.

`CaptureCanvas`:
- Kế thừa từ `Control` hoặc `UserControl` của Avalonia.
- Tự vẽ bằng SkiaSharp qua override `Render`.
- Nhận input chuột và bàn phím.

---

## 5. Chu kỳ overlay

1. Mở cửa sổ.
2. Capture toàn Virtual Screen.
3. Hiển thị screenshot + overlay.
4. Người dùng tương tác.
5. Khi xuất: render ảnh cuối và gọi Export service.
6. Đóng cửa sổ.

---

## 6. Kết nối với code

- `src/FShot.UI/Windows/CaptureOverlayWindow.fs`
- `src/FShot.UI/Controls/CaptureCanvas.fs`
- Dùng `CaptureResult`, `OverlayState`, `SceneComposer`.
