# Converters — Thiết kế chi tiết

> Tài liệu này mô tả cách chuyển domain types của F-Shot sang Skia types.

---

## 1. Mục đích

`FShot.Rendering.Skia` là project duy nhất được phép dùng SkiaSharp. Nó cung cấp các hàm chuyển đổi từ `Point`, `Rect`, `Color` sang `SKPoint`, `SKRect`, `SKColor`.

---

## 2. Chuyển Point

Công thức: `(X, Y)` → `SKPoint(X, Y)`.

Khi cần vẽ physical pixels, nhân với scale factor trước khi chuyển.

Ví dụ: logical point `(100, 80)`, scale 1.5 → physical `(150, 120)` → `SKPoint(150, 120)`.

---

## 3. Chuyển Rect

Công thức:

`SKRect(left, top, right, bottom)`

Với logical Rect và scale factor:

`left = round(L * s)`
`top = round(T * s)`
`right = round(R * s)`
`bottom = round(B * s)`

Ví dụ: logical `(100, 80, 300, 230)`, scale 1.5 → `SKRect(150, 120, 450, 345)`.

---

## 4. Chuyển Color

Công thức: `(R, G, B, A)` → `SKColor(R, G, B, A)`.

Vì domain `Color` dùng `byte`, chuyển trực tiếp.

---

## 5. Chuyển CaptureResult → SKBitmap

Từ `CaptureResult`:

- `Width`, `Height`: kích thước physical.
- `Pixels`: byte array BGRA32.
- Tạo `SKBitmap` với `SKColorType.Bgra8888`.
- Install pixel data từ `Pixels`.

---

## 6. Kết nối với code

- `src/FShot.Rendering.Skia/Converters/DomainToSkia.fs`
- Được dùng bởi `ScreenshotRenderer`, `SelectionRenderer`, `AnnotationRenderer`.
