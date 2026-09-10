# RenderControl — Thiết kế chi tiết

> Tài liệu này thiết kế custom render control cho overlay F-Shot.

---

## 1. Mục đích

`CaptureCanvas` là custom control duy nhất bên trong `CaptureOverlayWindow`. Nó chịu trách nhiệm:

- Hiển thị ảnh chụp màn hình nền.
- Vẽ overlay vùng chọn và annotations.
- Nhận input chuột và bàn phím.
- Đo và báo cáo FPS để đảm bảo hiệu năng.

---

## 2. Kiến trúc render

Trong PoC, control kế thừa từ `Avalonia.Controls.Control` và override phương thức `Render`.

Do trực tiếp dùng `Avalonia.Skia.ISkiaDrawingContextImpl` gặp lỗi trong môi trường này, PoC sử dụng:

- `WriteableBitmap` để hiển thị screenshot nền.
- `Avalonia.Media.DrawingContext` để vẽ overlay (border vùng chọn, handles).

### 2.1. Cache bitmap nền

Screenshot nền có kích thước lớn (toàn Virtual Screen) và hiếm khi thay đổi. Để đạt FPS ≥ 60:

- Tạo `WriteableBitmap` một lần duy nhất khi nhận `CaptureResult`.
- Lưu bitmap vào biến `cachedBitmap`.
- Mỗi frame render chỉ gọi `context.DrawImage(cachedBitmap, rect)`.
- Khi nhận `CaptureResult` mới, dispose bitmap cũ rồi tạo bitmap mới.

### 2.2. Vẽ overlay độc lập

Vùng chọn và annotations thay đổi liên tục khi kéo chuột. Overlay được vẽ trực tiếp bằng `DrawingContext` mà không cần cập nhật bitmap nền.

Các thành phần overlay trong PoC:

- Border trắng quanh vùng chọn.
- 8 handle vuông nhỏ tại các điểm neo.

---

## 3. Công thức tọa độ

`CaptureResult` lưu tọa độ theo Virtual Screen (không gian vật lý, đơn vị pixel ảo).
`CaptureCanvas` vẽ trong không gian control (bắt đầu từ góc trái trên của cửa sổ).

Vì cửa sổ được đặt chính xác tại `(virtualLeft, virtualTop)` và kích thước bằng Virtual Screen, tọa độ control khớp với tọa độ Virtual Screen khi `ScaleFactor = 1.0`.

Khi DPI khác 100%, cần nhân với `CaptureResult.ScaleFactor`:

```text
screenX = virtualX * ScaleFactor
screenY = virtualY * ScaleFactor
screenW = virtualW * ScaleFactor
screenH = virtualH * ScaleFactor
```

Ví dụ: vùng chọn `(100, 100, 200, 150)` với `ScaleFactor = 1.5` được vẽ tại `(150, 150, 300, 225)`.

---

## 4. Đo hiệu năng FPS

Mục tiêu PoC: render ≥ 60 FPS khi kéo chuột.

Các chỉ số theo dõi:

- `CurrentFps`: số frame render trong 1 giây gần nhất.
- `LastFrameTimeMs`: thời gian render frame gần nhất.
- `AverageFrameTimeMs`: trung bình động thời gian render frame.

Công thức:

```text
frameTimeMs = (frameEndTicks - frameStartTicks) * 1000 / Stopwatch.Frequency
fps = frameCount / elapsedSeconds
```

Ngưỡng đạt yêu cầu:

- `CurrentFps >= 60` hoặc `AverageFrameTimeMs <= 16.67 ms`.

---

## 5. Input handling

Control override các sự kiện:

- `OnPointerPressed`: bắt đầu chọn/moving/resizing.
- `OnPointerMoved`: cập nhật vùng chọn.
- `OnPointerReleased`: hoàn tất tương tác.
- `OnKeyDown`: xử lý `Esc`, `Enter`, phím tắt công cụ.

Tọa độ chuột được chuyển sang Virtual Screen space:

```text
virtualX = controlX + window.X
virtualY = controlY + window.Y
```

---

## 6. Kết nối với code

- `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`
- `src/FShot.UI/Windows/CaptureOverlayWindow.axaml.fs`
