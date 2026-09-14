# LineAndArrow — Thiết kế chi tiết

> Tài liệu này mô tả công cụ đường thẳng và mũi tên.

---

## 1. Line

Line được xác định bởi hai điểm: điểm bắt đầu `A` và điểm kết thúc `B`.

### 1.1 Công thức độ dài

`length = sqrt((Bx - Ax)^2 + (By - Ay)^2)`

Ví dụ: A = `(100, 100)`, B = `(400, 100)`.

`length = sqrt(300^2 + 0) = 300`

### 1.2 Bounding box

`X = min(Ax, Bx)`
`Y = min(Ay, By)`
`Width = |Bx - Ax|`
`Height = |By - Ay|`

### 1.3 Preview và commit

Trong quá trình vẽ, preview hiển thị đoạn thẳng từ điểm bắt đầu `A` đến vị trí con trỏ hiện tại `C`. Khi thả chuột, preview được commit thành annotation `Line(A, C)` với màu sắc và độ dày lấy từ `AnnotationStyle` hiện tại.

Không giới hạn hướng vẽ: điểm kết thúc có thể nằm ở bất kỳ phía nào so với điểm bắt đầu.

## 2. Arrow

Arrow là đường thẳng kèm mũi tên ở đầu B. Mũi tên gồm hai cánh tạo góc với đường chính.

### 2.1 Công thức tính đầu mũi tên

Cho vector đường `v = B - A`, độ dài `L = |v|`. Vector đơn vị `u = v / L`.

Mũi tên gồm hai điểm `P1`, `P2`, cách B một khoảng `arrowLength`, tạo góc `θ` so với hướng đường.

Công thức xoay vector:

`P1x = Bx - arrowLength * (ux * cosθ - uy * sinθ)`
`P1y = By - arrowLength * (ux * sinθ + uy * cosθ)`

`P2x = Bx - arrowLength * (ux * cosθ + uy * sinθ)`
`P2y = By - arrowLength * (-ux * sinθ + uy * cosθ)`

Ví dụ: B = `(400, 100)`, hướng ngang sang phải, arrowLength = 20, θ = 30°.

- `P1 ≈ (382.7, 90)`
- `P2 ≈ (382.7, 110)`

### 2.2 Preview và commit

Tương tự Line, preview Arrow hiển thị đường chính từ `A` đến `C` và mũi tên ở `C` với cùng công thức trên. Khi thả chuột, commit thành `Arrow(A, C, ArrowStyle.Standard)`.

### 2.3 Trường hợp đặc biệt

Nếu `A` và `C` trùng nhau (`L = 0`), vector đơn vị không xác định. Trong trường hợp này, renderer bỏ qua phần mũi tên hoặc vẽ điểm duy nhất tại `A`, không ném lỗi.

## 3. Kết nối với code

- `src/FShot.Core/Domain/Annotation.fs`
- `src/FShot.Rendering.Skia/Renderers/AnnotationRenderer.fs`
- `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`
