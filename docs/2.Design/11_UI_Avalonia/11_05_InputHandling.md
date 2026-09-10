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

| Phím | Hành động |
|------|-----------|
| Arrow | Nudge selection 1px (Shift = 10px) |
| Shift + Arrow | Resize selection 1px |
| Ctrl + Shift + Arrow | Resize đối xứng |
| Ctrl + A | Select all (mở rộng ra toàn capture area) |
| Esc | Hủy hoặc đóng overlay |
| Enter / Ctrl + S | Xuất ảnh |
| Ctrl + C | Copy clipboard |
| P/D/A/S/R/C/M/T/B/I | Chuyển annotation tool |

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
