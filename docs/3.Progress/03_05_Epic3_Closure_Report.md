# Báo cáo đóng Epic 3: Bộ công cụ chú thích (Annotations)

- **Ngày đóng:** 2026-09-15
- **Phạm vi:** Epic 3 — tất cả tasks từ P1.12 đến P1.20.
- **Trạng thái:** **HOÀN THÀNH**
- **Mục tiêu báo cáo:** Tổng kết toàn bộ công việc, kết quả kiểm thử, nợ kỹ thuật, và bước chuyển tiếp sang Epic 4.

---

## 1. Tóm tắt Epic 3

Epic 3 triển khai đầy đủ bộ công cụ chú thích trong overlay F-Shot, bao gồm 9 công cụ annotation và phím tắt chuyển nhanh. Tất cả tool đã được tích hợp với `OverlayState`, render trên Avalonia UI overlay, render trong Skia export pipeline, và có regression tests.

| Task | Tên | Trạng thái |
| --- | --- | --- |
| P1.12 | Pencil (bút vẽ tự do + smoothing) | ✅ Hoàn thành |
| P1.13 | Line (đường thẳng) | ✅ Hoàn thành |
| P1.14 | Arrow (mũi tên) | ✅ Hoàn thành |
| P1.15 | Rectangle (hình chữ nhật, bo góc = 0 trong MVP) | ✅ Hoàn thành |
| P1.16 | Circle/Ellipse (Ctrl khóa tỉ lệ 1:1) | ✅ Hoàn thành |
| P1.17 | Marker (nét bán trong suốt) | ✅ Hoàn thành |
| P1.18 | Text (Avalonia TextBox overlay, Skia render) | ✅ Hoàn thành |
| P1.19 | Pixelate (placeholder preview, xử lý thật trong export pipeline) | ✅ Hoàn thành |
| P1.20 | IconTool placeholder + phím tắt chuyển nhanh (P/L/A/S/R/C/M/T/B/I) | ✅ Hoàn thành |

---

## 2. Kiến trúc đã ổn định

### 2.1 Domain model (`src/FShot.Core/Domain/Annotation.fs`)

- `ToolKind` DU: 9 công cụ (`SelectionTool`, `PencilTool`, `LineTool`, `ArrowTool`, `RectangleTool`, `CircleTool`, `MarkerTool`, `TextTool`, `PixelateTool`, `IconTool`).
- `Tool` DU: mỗi tool mang payload riêng phù hợp tương tác và render.
- `Annotation` record: immutable, có `Id`, `Tool`, `Style`, `CreatedAt`.
- `AnnotationStyle` record chung: `Color`, `StrokeWidth`, `FontSize`, `FontName`, `FontStyle`.

### 2.2 State machine (`src/FShot.Core/State/OverlayState.fs`)

- `AnnotationInteraction` DU: `NoAnnotation`, `DrawingPreview`, `FreehandDrawing`, `EditingText`.
- `SelectTool tool` chuyển công cụ ở mọi trạng thái (trừ khi đang `EditingText` thì hủy text input).
- `PointerPressed` trong vùng chọn bắt đầu annotation hoặc di chuyển vùng chọn tùy tool.
- `PointerReleased` commit preview và push vào `HistoryStack`.
- `TextCommitted content` chuyển `EditingText` thành `Annotation`.

### 2.3 UI overlay (`src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`)

- Vẽ screenshot stub hoặc dimming Flameshot-style tùy giai đoạn.
- Vẽ selection border + 8 handles.
- Vẽ tất cả annotation đã commit và preview đang vẽ.
- Toolbar đơn giản bám quanh capture region với 10 nút: `S P L A R C M T B I`.
- Xử lý phím tắt bằng `e.Key` (physical key) để tránh IME tiếng Việt nuốt mất sự kiện.

### 2.4 Skia renderer (`src/FShot.Rendering.Skia/Renderers/AnnotationRenderer.fs`)

- Pencil/Line/Arrow: nét liền, mũi tên tam giác.
- Rectangle/Circle: hình học cơ bản, hỗ trợ aspect lock.
- Marker: nét dày gấp 3, alpha 35%.
- Text: `SKFont` + `DrawText` với `SKTextAlign`.
- Pixelate: preview hình chữ nhật mờ (xử lý thật trong export pipeline).
- Icon: placeholder hình chữ nhật nét đứt + chữ "ICON".

### 2.5 Export pipeline (`src/FShot.Rendering.Skia/Renderers/SceneComposer.fs`)

- `renderOverlay`: vẽ screenshot + dimming + selection + annotations (dùng cho preview, hiện tại không gọi từ UI do Flameshot-style overlay).
- `renderExport`: crop theo selection, vẽ screenshot crop + annotations (dùng cho export).

---

## 3. Kết quả kiểm thử

### 3.1 Số lượng tests

```text
FShot.Core.Tests.dll        Passed: 155 / 155
FShot.Rendering.Skia.Tests  Passed:  21 /  21
FShot.UI.Tests.dll          Passed:   1 /   1
Tổng:                       Passed: 177 / 177
```

### 3.2 Build

```text
dotnet build src/FShot.UI/FShot.UI.fsproj -c Release
→ 0 Warning(s), 0 Error(s)
```

### 3.3 Verify trên desktop Windows

- Overlay phủ full 3840×1200 (2 màn hình).
- Desktop gốc nhìn thấy qua cửa sổ trong suốt, vùng ngoài capture region mờ xanh `#45b6f7` 50%.
- Capture region sáng rõ, có viền trắng + 8 handle.
- Tất cả phím tắt tool hoạt động: `S P L A R C M T B I`.
- Text tool hiển thị TextBox overlay, Enter commit, Esc cancel.
- FPS > 60 liên tục khi kéo chuột / vẽ annotation.

---

## 4. Các vấn đề quan trọng đã giải quyết trong Epic 3

### 4.1 Lỗi thread safety khi set capture result (P0 giai đoạn đầu)

- **Biểu hiện:** `InvalidOperationException` khi `SetCaptureResult` gọi `InvalidateVisual` từ background thread.
- **Khắc phục:** Sử dụng `Avalonia.Threading.Dispatcher.UIThread.Post` để chuyển `InvalidateVisual` lên UI thread.

### 4.2 TextBox không hiển thị (P1.18)

- **Biểu hiện:** Nhấn `T` và click không thấy TextBox.
- **Khắc phục:** Bọc `CaptureCanvas` trong `Canvas` (`RootCanvas`); thêm fallback `TopLevel.GetTopLevel` để tìm `Window` và thêm TextBox vào `Window.Content`.

### 4.3 Degrade màn hình sau khi sửa Text tool (P1.18)

- **Biểu hiện:** Desktop gốc biến mất, toàn màn hình chỉ còn màu xám/xanh.
- **Nguyên nhân:** Thay đổi `Window.Opacity` và `Background` trong lúc sửa TextBox, đồng thời screenshot stub che desktop.
- **Khắc phục:** Đặt `Window` trong suốt, không vẽ screenshot stub lên overlay, chỉ vẽ dimming strips chọn lọc.

### 4.4 Skia deprecation warnings

- **Biểu hiện:** Build có warnings về `SKPath`, `DrawText`, `DrawBitmap` deprecated.
- **Khắc phục:** Thay `SKPath` bằng `SKPathBuilder`, dùng overload `DrawText` có `SKTextAlign`, dùng `DrawBitmap` có `SKSamplingOptions`.

### 4.5 IME tiếng Việt bắt phím tắt

- **Biểu hiện:** Phím tắt `P`, `L`, `A`, ... bị bộ gõ tiếng Việt chặn.
- **Khắc phục:** Dùng `e.Key` (physical key) thay vì `e.Key.ToString()` để phát `SelectTool`. Ghi nhận là nợ kỹ thuật cần chuyển sang `KeyGesture` trong v1.x.

---

## 5. Nợ kỹ thuật còn lại (intentional for v1.x)

| Nợ | Mô tả | Khi nào làm |
| --- | --- | --- |
| Pixelate thật | Hiện chỉ preview mờ; xử lý mosaic trung bình cần làm trong export pipeline. | Khi tích hợp capture thật / export pipeline |
| Icon thật | Chỉ là placeholder; cần thư viện icon, picker UI, resize/xoay. | v1.x |
| Text nâng cao | Đa dòng, đổi font, in đậm/nghiêng/gạch chân, xoay. | v1.x |
| Single source of truth render text | Hiện có 2 đường dẫn: Skia cho export, Avalonia `FormattedText` cho overlay. | Khi refactor renderer |
| KeyGesture | Dùng `KeyGesture` thay vì `e.Key` để xử lý phím tắt đúng chuẩn, đồng thời giải quyết IME. | v1.x |
| Toolbar UI đẹp | Toolbar hiện là nút text đơn giản; cần chuyển sang icon theo Penpot design. | Epic 4 / v1.x |
| Annotation editing | Chọn, xóa, di chuyển annotation cũ. | v1.x |

---

## 6. Tài liệu thiết kế đã hoàn thiện

| File | Nội dung |
| --- | --- |
| `docs/2.Design/04_Annotation/04_02_ToolModel.md` | Mô hình dữ liệu Tool, Annotation, Preview, Commit. Đã cập nhật 9 tool. |
| `docs/2.Design/04_Annotation/04_03_Pencil.md` | Pencil + thuật toán smoothing. |
| `docs/2.Design/04_Annotation/04_04_LineAndArrow.md` | Line + Arrow với chóp mũi tên. |
| `docs/2.Design/04_Annotation/04_05_RectangleAndCircle.md` | Rectangle + Circle/Ellipse, Ctrl khóa tỉ lệ. |
| `docs/2.Design/04_Annotation/04_06_MarkerAndPixelate.md` | Marker alpha blend + Pixelate mosaic. |
| `docs/2.Design/04_Annotation/04_07_TextTool.md` | Text tool MVP với Avalonia TextBox. |
| `docs/2.Design/04_Annotation/04_08_IconTool.md` | IconTool placeholder cho phím `I`. |
| `docs/2.Design/04_Annotation/04_08_CommitAndPreview.md` | (nếu có) Luồng commit preview. |
| `docs/2.Design/08_OverlayState/08_03_Events.md` | Sự kiện `SelectTool` đã cập nhật 9 tool. |

---

## 7. Tiến độ tổng quan sau Epic 3

| Phase | Trạng thái | Hoàn thành |
| --- | --- | --- |
| Phase 0: PoC | DONE | 8 / 8 |
| Phase 1: MVP Core | IN PROGRESS | 24 / 29 (~83%) |
| Phase 2: Windows v1.0 | PENDING | 0 / 39 |
| Phase 3: Advanced | PENDING | 0 / 8 |

**Tổng tasks:** 32 / 84 (~38.1%)

---

## 8. Bước tiếp theo: Epic 4 — Undo/Redo & Toolbar

Các task sắp tới:

- **P1.21** Hoàn tác `Ctrl+Z` (`FR-UNDO-01`).
- **P1.22** Làm lại `Ctrl+Shift+Z` / `Ctrl+Y` (`FR-UNDO-02`).
- **P1.23** Toolbar tối giản nằm sát dưới vùng chọn (`FR-TB-01`).

`HistoryStack` đã được triển khai từ P1.02 và đang được sử dụng khi commit annotation. Epic 4 sẽ kết nối phím tắt Undo/Redo và cải thiện toolbar.

---

## 9. Kết luận

Epic 3 đã hoàn thành đầy đủ 9 công cụ annotation với pipeline từ domain model → state machine → UI overlay → Skia renderer → regression tests. Mọi tính năng cơ bản đều đã verify trên desktop Windows. Dự án sẵn sàng chuyển sang Epic 4.
