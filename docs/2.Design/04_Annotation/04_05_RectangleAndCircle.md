# RectangleAndCircle — Thiết kế chi tiết

> Tài liệu này mô tả công cụ hình chữ nhật, hình tròn và elip.

---

## 1. Rectangle

Hình chữ nhật được xác định bởi hai điểm đối diện, tương tự vùng chọn.

### 1.1 Công thức tạo Rect

`X = min(Ax, Bx)`
`Y = min(Ay, By)`
`Width = |Bx - Ax|`
`Height = |By - Ay|`

Ví dụ: A = `(100, 100)`, B = `(400, 300)`.

Rect = `(100, 100, 300, 200)`.

### 1.2 Bo góc (corner radius)

Nếu có `cornerRadius`, bán kính bo góc được giới hạn:

`radius = min(cornerRadius, Width / 2, Height / 2)`

Ví dụ: Rect rộng 300, cao 200, cornerRadius yêu cầu 50.

`radius = min(50, 150, 100) = 50`

### 1.3 Preview và commit

Trong quá trình vẽ, preview hiển thị hình chữ nhật từ điểm bắt đầu `A` đến vị trí con trỏ hiện tại `C`, dùng công thức tạo Rect như trên. Khi thả chuột, commit thành `Rectangle(A, C, cornerRadius=0.0)` trong MVP. `cornerRadius` có thể được điều chỉnh sau này qua cấu hình hoặc UI.

## 2. Circle / Ellipse

Hình tròn/elip cũng được xác định bởi bounding box Rect.

### 2.1 Hình tròn thực sự (tỉ lệ 1:1)

Khi giữ phím modifier `Ctrl`, hình tròn có `Width = Height = min(|Bx - Ax|, |By - Ay|)`.

Ví dụ: kéo từ `(100, 100)` đến `(400, 350)`, giữ `Ctrl`:

`side = min(300, 250) = 250`

Bounding box = `(100, 100, 250, 250)`.

### 2.2 Hình elip

Nếu không giữ `Ctrl`, bounding box là `(100, 100, 300, 250)`, elip nằm khít trong.

### 2.3 Preview và commit

Preview hiển thị elip/tròn từ `A` đến `C`. Nếu `Ctrl` được giữ, preview cũng khóa tỉ lệ 1:1 theo công thức trên. Khi thả chuột, commit thành `Circle(A, C, aspectLocked)` với `aspectLocked = Ctrl được giữ`.

## 3. Kết nối với code

- `src/FShot.Core/Domain/Annotation.fs`
- `src/FShot.Core/State/OverlayState.fs`
- `src/FShot.Rendering.Skia/Renderers/AnnotationRenderer.fs`
- `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`
