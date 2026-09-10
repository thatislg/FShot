# Rendering — Tổng quan thiết kế

> Thư mục này thiết kế cách F-Shot vẽ toàn bộ scene xuống bitmap và hiển thị lên màn hình.  
> Rendering là cầu nối giữa domain model (pure F#) và thư viện đồ họa SkiaSharp.

---

## 1. Mục đích

`Rendering` định nghĩa:

- Cách **convert domain types sang Skia types**.
- Cách **vẽ từng thành phần**: screenshot, overlay tối, vùng chọn, chú thích, toolbar, kính lúp.
- Cách **tối ưu hiệu năng**: cache, invalid region.
- Cách **encode ảnh cuối** thành PNG/JPG.

Rendering phải:

- Nằm trong `FShot.Rendering.Skia`.
- Không chứa logic UI event handling.
- Có thể test bằng reference image comparison.

---

## 2. Phạm vi thiết kế

### 2.1 Converters

- `Point` → `SKPoint`.
- `Rect` → `SKRect`.
- `Color` → `SKColor`.
- `StrokeWidth` → `SKPaint.StrokeWidth`.

### 2.2 Renderers

| Renderer | Vai trò |
|----------|---------|
| `ScreenshotRenderer` | Vẽ ảnh desktop gốc |
| `SelectionRenderer` | Vẽ overlay tối, đường viền, 8 handles |
| `AnnotationRenderer` | Vẽ từng loại tool |
| `ToolbarRenderer` | Vẽ thanh công cụ |
| `MagnifierRenderer` | Vẽ kính lúp |
| `SceneComposer` | Kết hợp các lớp theo thứ tự đúng |

### 2.3 Backing store

- Một `SKBitmap` cho screenshot (bất biến).
- Một `SKBitmap` hoặc vẽ trực tiếp cho composition frame.

### 2.4 Encode

- Render final image từ selection + annotations.
- Encode PNG (lossless).
- Encode JPG (quality configurable).

---

## 3. Liên hệ với các phần khác

```
Rendering.Skia
    ├── Core.Geometry: convert types
    ├── Core.Selection: vẽ vùng chọn
    ├── Core.Annotation: vẽ chú thích
    ├── Core.Export: encode ảnh cuối
    ├── Core.OverlayState: nhận RenderModel
    └── UI: hiển thị bitmap lên màn hình
```

---

## 4. Dùng trong PoC

Trong Phase 0, Rendering cần kiểm chứng:

1. Hiển thị screenshot bằng SkiaSharp trong Avalonia control.
2. Vẽ overlay tối ngoài vùng chọn.
3. Đạt ≥ 60 FPS khi kéo chuột.

---

## 5. Dùng trong MVP

Trong Phase 1, Rendering cần hỗ trợ:

- Vẽ screenshot, overlay, selection, annotations.
- Vẽ preview của tool đang vẽ.
- Render ảnh cuối để save/copy.
- Encode PNG/JPG.

---

## 6. Câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Dùng `SKBitmap` riêng cho screenshot và composition hay vẽ trực tiếp? | Ảnh hưởng hiệu năng và bộ nhớ |
| Có cache `SKPath` cho annotation không? | Ảnh hưởng FPS |
| Text tool vẽ bằng Skia hay dùng Avalonia TextBox overlay? | Ảnh hưởng cách commit |
| Pixelate xử lý trên `SKBitmap` pixel buffer hay shader? | Ảnh hưởng hiệu năng và bảo mật |
| Có hỗ trợ anti-aliasing và drop shadow cho pin widget? | Thuộc [C], v1.x |

---

## 7. Các file sẽ được thiết kế trong thư mục này

| File | Nội dung |
|------|----------|
| `09_02_Converters.md` | Domain → Skia type conversion |
| `09_03_ScreenshotRenderer.md` | Vẽ ảnh desktop |
| `09_04_SelectionRenderer.md` | Vẽ vùng chọn và overlay tối |
| `09_05_AnnotationRenderer.md` | Vẽ từng loại tool |
| `09_06_ToolbarAndMagnifier.md` | Vẽ toolbar và kính lúp |
| `09_07_SceneComposer.md` | Kết hợp các lớp |
| `09_08_Encoding.md` | Encode PNG/JPG |
| `09_09_Performance.md` | Cache và tối ưu FPS |

---

## 8. Kết nối với code

Triển khai trong:

- `src/FShot.Rendering.Skia/Converters/DomainToSkia.fs`
- `src/FShot.Rendering.Skia/Renderers/*.fs`
- `tests/FShot.Rendering.Skia.Tests/*.fs`

---

*Rendering là chủ đề thứ chín. Sau khi chốt, chúng ta chuyển sang Platform.Win32 — tích hợp Windows.*
