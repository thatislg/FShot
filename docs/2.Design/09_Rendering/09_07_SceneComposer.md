# SceneComposer — Thiết kế chi tiết

> Tài liệu này mô tả cách kết hợp các lớp vẽ: screenshot, overlay tối, vùng chọn, annotations.

---

## 1. Mục đích

`SceneComposer` điều phối việc vẽ toàn bộ scene trong overlay. Nó nhận state từ domain và vẽ theo thứ tự layer.

---

## 2. Các layer vẽ

Thứ tự từ dưới lên:

1. **Screenshot bitmap**: toàn bộ ảnh chụp, được vẽ từ `CaptureResult`.
2. **Dimming overlay**: lớp mờ tối ngoài vùng chọn.
3. **Selection border**: đường viền vùng chọn.
4. **Resize handles**: 8 điểm neo.
5. **Annotations**: các chú thích đã commit.
6. **Preview**: annotation đang vẽ.
7. **Toolbar**: thanh công cụ (do UI vẽ hoặc Skia vẽ tùy quyết định).

---

## 3. Vẽ screenshot

Tạo `SKBitmap` từ `CaptureResult.Pixels`. Vẽ toàn bộ bitmap lên canvas tại vị trí `(0, 0)` với kích thước physical.

## 4. Vẽ dimming overlay

Vẽ hình chữ nhật phủ toàn màn hình với màu đen alpha khoảng 0.5. Sau đó cắt (clip) phần trong vùng chọn để không bị tối.

## 5. Vẽ selection

- Đường viền: vẽ Rect của vùng chọn với màu trắng hoặc màu config, độ dày 1–2 physical pixel.
- Handles: vẽ 8 hình vuông nhỏ tại vị trí đã tính trong `Selection.HandleCenters`.

## 6. Vẽ annotations

Duyệt danh sách `Annotation`, vẽ từng tool bằng `AnnotationRenderer`.

## 7. Kết nối với code

- `src/FShot.Rendering.Skia/Pipeline/SceneComposer.fs`
- `src/FShot.Rendering.Skia/Renderers/ScreenshotRenderer.fs`
- `src/FShot.Rendering.Skia/Renderers/SelectionRenderer.fs`
- `src/FShot.Rendering.Skia/Renderers/AnnotationRenderer.fs`
