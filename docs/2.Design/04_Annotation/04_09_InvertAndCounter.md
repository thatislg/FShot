# Invert & Circle Counter — Thiết kế chi tiết Công cụ Đảo màu và Đếm số

> Tài liệu này thiết kế chi tiết hai công cụ chú thích nâng cao trong F-Shot: Công cụ Đảo ngược màu sắc (**Invert Tool** — `FR-ANN-09`) và Công cụ Bong bóng số tự động tăng (**Circle Counter Tool** — `FR-ANN-10`, `FR-UNDO-005`, `FR-CFG-203`).  
> **Nguyên tắc tài liệu:** Thiết kế thuần giải thuật toán học, mô hình kiểu dữ liệu Domain F#, ma trận render SkiaSharp và tích hợp lịch sử Undo/Redo — **không dùng placeholder**.

---

## 1. Ma trận Yêu cầu Kỹ thuật

| Mã SRS | Mã gốc | Tính năng & Đặc tả chi tiết | Mức độ | Cơ chế triển khai kỹ thuật (F# / SkiaSharp) |
| :--- | :--- | :--- | :--- | :--- |
| **FR-ANN-09** | ANN-009 | Đảo ngược màu sắc (Invert colors) bên trong vùng chỉ định. | S | Toán tử đảo bit trên `SKBitmap` hoặc vẽ với `SKBlendMode.Difference` / `SKColorFilter.CreateColorMatrix`. |
| **FR-ANN-10** | ANN-010 | Bong bóng đếm số tự động tăng dần (Circle Counter: 1, 2, 3…). | S | F# State lưu giữ `CurrentCounterIndex`; vẽ huy hiệu tròn với số tương phản cao. |
| **FR-UNDO-005** | UNDO-005 | Phục hồi chỉ số counter tiếp theo sau khi Undo/Redo. | S | Đóng gói `CounterIndex: int` vào mỗi phần tử `Snapshot` trong `HistoryStack`. |
| **FR-CFG-203** | CFG-203 | Cấu hình kích thước đường kính mặc định của bong bóng số. | S | Thuộc tính `DrawCircleCounterSize: float` trong `AppConfig` (mặc định 28.0 px). |

---

## 2. Công cụ Đảo ngược màu sắc (Invert Tool — `FR-ANN-09`)

### 2.1 Ý nghĩa Sử dụng & Mô hình Dữ liệu

Công cụ Invert cho phép người dùng kéo thả chuột để tạo một vùng hình chữ nhật. Mọi pixel của ảnh chụp màn hình nằm bên trong vùng này sẽ bị đảo ngược giá trị màu sắc ($C_{\text{new}} = 255 - C_{\text{old}}$). Công cụ này cực kỳ hữu ích để làm nổi bật tức thì các khối văn bản, đoạn code hoặc làm lộ các chi tiết chìm mà không che khuất nội dung bên dưới.

**Bổ sung vào `Tool` Discriminated Union (`src/FShot.Core/Domain/Annotation.fs`):**
```fsharp
type Tool =
    // ... các tool hiện có ...
    /// Đảo ngược màu sắc vùng chữ nhật từ start đến endPoint.
    | Invert of start: Point * endPoint: Point
```

**Bổ sung vào `ToolKind`:**
```fsharp
type ToolKind =
    // ...
    | InvertTool
```

### 2.2 Giải thuật Biến đổi Hình học & Tạo Rect

Tương tự công cụ `Rectangle`, vùng đảo màu được xác định bởi hai điểm đối diện $A(x_1, y_1)$ và $B(x_2, y_2)$:
$$X = \min(x_1, x_2), \quad Y = \min(y_1, y_2)$$
$$\text{Width} = |x_2 - x_1|, \quad \text{Height} = |y_2 - y_1|$$

### 2.3 Chiến lược Render SkiaSharp (Render Pipeline)

Có hai phương án kỹ thuật để render hiệu ứng đảo màu trên `SKCanvas`:

1. **Phương án 1 — Blend Mode Difference (Được khuyến nghị vì hiệu năng GPU vượt trội):**
   - Vẽ một hình chữ nhật phủ đúng vùng $Rect$ bằng `SKPaint` với màu trắng tinh `#FFFFFFFF` và chế độ hòa trộn `SKBlendMode.Difference`.
   - Theo công thức hòa trộn Difference:
     $$R_{\text{result}} = |R_{\text{src}} - R_{\text{dst}}| = |255 - R_{\text{dst}}| = 255 - R_{\text{dst}}$$
     $$G_{\text{result}} = |G_{\text{src}} - G_{\text{dst}}| = 255 - G_{\text{dst}}$$
     $$B_{\text{result}} = |B_{\text{src}} - B_{\text{dst}}| = 255 - B_{\text{dst}}$$
   - Ưu điểm: Skia thực hiện trực tiếp trong shader của GPU, tốc độ render tức thời (< 1ms), không cần đọc/ghi pixel thô trên CPU.
2. **Phương án 2 — Color Matrix Filter:**
   - Dùng ma trận đảo màu 4x5:
     $$\begin{pmatrix} -1 & 0 & 0 & 0 & 255 \\ 0 & -1 & 0 & 0 & 255 \\ 0 & 0 & -1 & 0 & 255 \\ 0 & 0 & 0 & 1 & 0 \end{pmatrix}$$
   - Tạo qua `SKColorFilter.CreateColorMatrix(matrix)`.

**Preview trong khi vẽ:**
- Trong lúc người dùng đang kéo chuột (`DrawingPreview`): vẽ viền đứt nét mảnh (dashed rectangle) kèm hiệu ứng Difference tạm thời bên trong để người dùng thấy ngay kết quả trước khi thả chuột.

---

## 3. Công cụ Bong bóng đếm số (Circle Counter Tool — `FR-ANN-10`)

### 3.1 Ý nghĩa & Mô hình Dữ liệu

Circle Counter cho phép người dùng click chuột liên tiếp lên ảnh để đánh số các bước hướng dẫn (ví dụ: bước ① mở menu, bước ② nhấn nút, bước ③ nhập dữ liệu). Mỗi cú click sẽ tự động sinh ra một hình tròn chứa số thứ tự tăng dần bắt đầu từ 1.

**Bổ sung vào `Tool` Discriminated Union:**
```fsharp
type Tool =
    // ...
    /// Bong bóng đếm số tại vị trí tâm, số thứ tự index và bán kính radius.
    | CircleCounter of center: Point * index: int * radius: float
```

**Bổ sung vào `ToolKind`:**
```fsharp
type ToolKind =
    // ...
    | CircleCounterTool
```

### 3.2 Quy chuẩn Thẩm mỹ Kawaii Claymorphism

Để phù hợp với phong cách chung của F-Shot:
- **Nền bong bóng (Badge Circle):**
  - Tô đặc (`SKPaintStyle.Fill`) bằng màu vẽ hiện tại của người dùng (`style.Color`).
  - Viền ngoài nhẹ (`SKPaintStyle.Stroke`, độ dày 1.5px) màu trắng mờ `#60FFFFFF` hoặc màu tối mờ `#30000000` tùy theo độ sáng nền để tạo hiệu ứng clay bóng bẩy.
- **Số hiển thị (Numeral Text):**
  - Font chữ: Bold Sans-serif (Segoe UI / Inter), căn giữa tuyệt đối theo cả trục ngang và trục dọc của hình tròn.
  - Tự động tương phản màu chữ (`Luminance Contrast`):
    - Tính độ sáng tương đối $L = 0.299 R + 0.587 G + 0.114 B$.
    - Nếu $L > 150$ (màu nền sáng như vàng, xanh nhạt, trắng): chữ màu đen `#1E293B`.
    - Nếu $L \le 150$ (màu nền tối như đỏ đậm, tím, xanh dương): chữ màu trắng `#FFFFFF`.
  - Cỡ chữ tỉ lệ thuận với đường kính: $\text{FontSize} = \text{radius} \times 1.1$.
- **Kích thước mặc định:** Bán kính $R = \text{DrawCircleCounterSize} / 2.0$ (mặc định $28.0 / 2 = 14.0\text{ px}$).

### 3.3 Quản lý Chỉ số Đếm Tự động Tăng (Auto-Increment State)

1. **Khởi tạo và tăng tiến:**
   - Trong `OverlayState`, duy trì trường `CurrentCounterIndex: int` (bắt đầu bằng 1).
   - Khi người dùng click chuột với công cụ `CircleCounterTool`:
     - Tạo một annotation `CircleCounter(center = clickPos, index = state.CurrentCounterIndex, radius = defaultRadius)`.
     - Cập nhật state: `CurrentCounterIndex = state.CurrentCounterIndex + 1`.
2. **Khôi phục chỉ số khi Undo / Redo (`FR-UNDO-005`):**
   - Vấn đề: Nếu người dùng đã đặt các số 1, 2, 3 rồi nhấn `Ctrl+Z` để xóa số 3, cú click tiếp theo phải đặt số 3 chứ không được nhảy cóc lên số 4.
   - Giải pháp: Lưu `CounterIndex: int` vào record `Snapshot` trong `src/FShot.Core/Domain/History.fs`:
     ```fsharp
     type Snapshot = {
         Annotations: Annotation list
         CounterIndex: int
         Timestamp: DateTime
     }
     ```
   - Khi thực hiện `Undo`: khôi phục `CurrentCounterIndex` từ snapshot liền trước.
   - Khi thực hiện `Redo`: khôi phục `CurrentCounterIndex` tương ứng của snapshot được redo.
3. **Đặt lại chỉ số về 1 (Reset Counter):**
   - Người dùng có thể click phải vào nút `CircleCounter` trên toolbar hoặc nhấn phím tắt bổ trợ để reset chỉ số đếm về 1 bất cứ lúc nào.

---

## 4. Bounding Box & Hit-testing

1. **BoundingBox của `Invert(a, b)`:**
   $$X = \min(a.X, b.X), \quad Y = \min(a.Y, b.Y)$$
   $$W = |b.X - a.X|, \quad H = |b.Y - a.Y|$$
2. **BoundingBox của `CircleCounter(center, _, radius)`:**
   $$X = center.X - radius, \quad Y = center.Y - radius$$
   $$W = 2 \times radius, \quad H = 2 \times radius$$

---

## 5. Kết nối Mã nguồn & Kế hoạch Triển khai

- **Domain Model:** `src/FShot.Core/Domain/Annotation.fs` (thêm `Invert`, `CircleCounter`), `src/FShot.Core/Domain/History.fs` (thêm `CounterIndex` vào `Snapshot`).
- **State Machine:** `src/FShot.Core/State/OverlayState.fs` (quản lý `CurrentCounterIndex`, dispatch `CommitAnnotation`).
- **Skia Renderer:** `src/FShot.Rendering.Skia/Renderers/AnnotationRenderer.fs` (thêm nhánh vẽ `Invert` với `SKBlendMode.Difference` và `CircleCounter` với badge + high-contrast text).
- **Toolbar & UI:** `src/FShot.UI/SkiaCanvas/ToolbarIcons.fs` (icon Invert và Counter), `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`.
- **Unit Tests:** `tests/FShot.Core.Tests/Domain/AnnotationTests.fs`, `tests/FShot.Core.Tests/State/OverlayStateTests.fs` (kiểm tra auto-increment, snapshot restore).
