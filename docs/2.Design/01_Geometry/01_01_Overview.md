# Geometry — Tổng quan thiết kế

> Thư mục này thiết kế các kiểu dữ liệu hình học cơ bản của F-Shot.  
> Đây là nền tảng đầu tiên của domain model, được dùng bởi hầu hết mọi module khác.

---

## 1. Mục đích

`Geometry` định nghĩa các khái niệm không gian đơn giản và không phụ thuộc UI:

- **Point**: tọa độ 2D trong không gian màn hình.
- **Rect**: hình chữ nhật với vị trí và kích thước.
- **Color**: màu RGBA.
- **StrokeWidth**: độ dày nét vẽ.

Các kiểu này phải:

- **Immutable**: mọi phép toán trả về giá trị mới.
- **Không phụ thuộc SkiaSharp / Avalonia / Win32**: chỉ dùng kiểu dữ liệu cơ bản của F#.
- **Dễ test**: có thể viết unit test đơn giản cho hit-test, clamp, intersect, transform.

---

## 2. Phạm vi thiết kế

### 2.1 Các kiểu dữ liệu chính

| Kiểu | Vai trò | Ghi chú |
|------|---------|---------|
| `Point` | Tọa độ 2D trong Virtual Screen space | Dùng `float` để tránh lỗi làm tròn khi scale |
| `Rect` | Vùng chữ nhật: vị trí + kích thước | Hỗ trợ normalized coords, hit-test, clamp |
| `Color` | Màu RGBA | Không dùng `System.Drawing.Color` hay `SkiaSharp.SKColor` |
| `StrokeWidth` | Độ dày nét vẽ | Có thể là single-case DU để phân biệt với float thường |

### 2.2 Chủ đề liên quan đến geometry

| Chủ đề | Vai trò | Ghi chú |
|--------|---------|---------|
| `HitTesting` | Xác định con trỏ/tranh vùng nào | Dùng cho handles, annotations, toolbar |
| `DpiAndScaling` | Xử lý Mixed DPI | Chuyển đổi giữa logical pixels và physical pixels |

### 2.3 Các phép toán cần thiết

- **Point**: cộng, trừ, nhân với scalar, khoảng cách, lerp.
- **Rect**: left/top/right/bottom/center, contains point, intersect, union, inflate, clamp inside another rect, aspect-ratio lock.
- **Color**: blend alpha, lighten/darken, convert to/from hex string.
- **StrokeWidth**: min/max constraint, tăng/giảm theo bước.
- **HitTesting**: bán kính dung sai, point-in-rect, point-near-line.
- **DpiAndScaling**: scale point/rect theo DPI, convert virtual/physical coords.

### 2.5 Không thuộc phạm vi

- Không chứa logic render.
- Không chứa logic input handling.
- Không chứa kiểu dữ liệu phụ thuộc nền tảng (như `System.Windows.Rect`, `SKRect`).

---

## 3. Liên hệ với các phần khác

```
Geometry
    ├── Capture: bounds, screen rects, virtual screen
    ├── Selection: selection rect, resize handles, hit-testing
    ├── Annotation: tool coordinates, bounding boxes
    ├── History: snapshot không chứa geometry logic
    ├── Export: crop rect, output dimensions
    ├── Rendering.Skia: convert geometry → Skia types
    └── UI: convert pointer events → geometry Point
```

Mọi module đều dùng `Point` và `Rect` để trao đổi tọa độ. Điều này giúp `FShot.Core` giữ purity và dễ test.

---

## 4. Dùng trong PoC

Trong Phase 0 (PoC), Geometry cần đủ để:

1. Đại diện vị trí con trỏ chuột trên Virtual Screen.
2. Định nghĩa vùng chọn bounding-box cơ bản.
3. Kiểm tra hit-test đơn giản với 8 điểm neo.
4. Tính toán đúng trên Mixed DPI.

Các phép toán phức tạp (aspect-ratio lock, snap-to-grid, complex intersect) có thể bổ sung sau trong Phase 1.

---

## 5. Dùng trong MVP

Trong Phase 1 (MVP), Geometry cần hỗ trợ đầy đủ:

- Tạo vùng chọn bằng kéo chuột.
- Co giãn vùng chọn qua 8 điểm neo.
- Di chuyển vùng chọn bằng kéo bên trong.
- Nudge / resize bằng bàn phím (1 px, `Shift + Arrow`, `Ctrl + Shift + Arrow`).
- Tính toán bounding box cho các công cụ vẽ (Line, Arrow, Rectangle, Circle, Marker, Pixelate, Invert).
- Hit-test cho chú thích cũ để chọn/sửa.

---

## 6. Câu hỏi cần quyết định trước khi viết code

| Câu hỏi | Tác động |
|---------|----------|
| Dùng `float` hay `double` cho tọa độ? | Ảnh hưởng độ chính xác và cách convert sang Skia |
| `Rect` dùng `X/Y/Width/Height` hay `Left/Top/Right/Bottom`? | Ảnh hưởng API và tính toán |
| `Color` có nên dùng byte hay float cho kênh? | Ảnh hưởng convert sang Skia và hex string |
| Có cần `Point` dạng integer song song (`PointI`) cho pixel grid? | Có thể cần cho snap-to-grid hoặc pixel manipulation |
| `StrokeWidth` nên là DU hay alias của `float`? | Ảnh hưởng type safety |

---

## 7. Các file sẽ được thiết kế trong thư mục này

| File | Nội dung |
|------|----------|
| `01_02_Point.md` | Thiết kế kiểu Point và các phép toán |
| `01_03_Rect.md` | Thiết kế kiểu Rect và các phép toán |
| `01_04_Color.md` | Thiết kế kiểu Color và conversion |
| `01_05_StrokeWidth.md` | Thiết kế StrokeWidth và validation |
| `01_06_HitTesting.md` | Phương châm hit-test cho điểm neo và chú thích |
| `01_07_DpiAndScaling.md` | Cách xử lý Mixed DPI trong geometry |

---

## 8. Kết nối với code

Các kiểu này sẽ được triển khai trong:

- `src/FShot.Core/Geometry/Types.fs`
- `src/FShot.Core/Geometry/Operations.fs` (nếu cần)
- `tests/FShot.Core.Tests/Geometry/TypesTests.fs`

---

*Geometry là phần thiết kế đầu tiên vì mọi domain khác đều phụ thuộc nó. Sau khi chốt Geometry, chúng ta sẽ chuyển sang Capture.*
