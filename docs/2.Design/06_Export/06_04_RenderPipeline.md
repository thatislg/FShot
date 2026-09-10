# RenderPipeline — Thiết kế chi tiết

> Tài liệu này mô tả quy trình render ảnh cuối cùng trước khi xuất.

---

## 1. Các bước render

1. Tạo bitmap trống kích thước vùng chọn (physical pixels).
2. Vẽ ảnh screenshot đã crop theo vùng chọn.
3. Vẽ danh sách annotations lên trên.
4. Encode bitmap thành PNG hoặc JPG bytes.

## 2. Tính kích thước bitmap cuối

Cho vùng chọn logical `(x, y, w, h)` và scale factor `s`:

`outputWidth = round(w * s)`
`outputHeight = round(h * s)`

Ví dụ: vùng `(100, 80, 300, 200)`, scale 1.5:

- `outputWidth = 450`
- `outputHeight = 300`

## 3. Crop screenshot

Vùng crop trong bitmap gốc được tính bằng `CaptureResult.LogicalSelectionToPhysical`:

`cropRect = captureResult.LogicalSelectionToPhysical(selectionBounds)`

## 4. Vẽ annotations

Mỗi annotation được vẽ lên bitmap output theo tọa độ tương đối so với vùng chọn. Công thức chuyển điểm từ Virtual Screen space sang tọa độ trong output bitmap:

`localX = (point.X - selection.X) * s`
`localY = (point.Y - selection.Y) * s`

## 5. Kết nối với code

- `src/FShot.Rendering.Skia/Renderers/SceneComposer.fs`
- `src/FShot.Rendering.Skia/Pipeline/CanvasComposer.fs`
