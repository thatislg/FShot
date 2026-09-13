# OverlayDimming — Thiết kế chi tiết

> Tài liệu này thiết kế lớp phủ tối mờ bên ngoài vùng chọn trong overlay, phục vụ yêu cầu `FR-SEL-01`.

---

## 1. Mục đích

`OverlayDimming` là lớp đồ họa nửa trong suốt phủ lên toàn bộ màn hình chụp, **trừ** vùng chọn đang active. Mục tiêu là giúp người dùng tập trung vào vùng sẽ được xuất, đồng thời vẫn nhìn thấy nội dung màn hình bên dưới ở mức độ mờ.

---

## 2. Yêu cầu

- Phủ toàn bộ `CaptureResult.VirtualBounds`.
- Vùng chọn (`Selection.Bounds`) không bị phủ tối, giữ nguyên độ sáng.
- Độ trong suốt có thể điều chỉnh qua cấu hình, mặc định khoảng 50%.
- Không ảnh hưởng đến ảnh xuất cuối cùng — dimming chỉ là hiệu ứng hiển thị overlay.

---

## 3. Cấu trúc dữ liệu

Dimming được xác định bởi:

- `ScreenBounds`: hình chữ nhật toàn màn hình ảo.
- `SelectionBounds`: hình chữ nhật vùng chọn hiện tại.
- `Color`: màu tối, thường là đen với alpha.
- `Opacity`: hệ số alpha trong khoảng [0.0, 1.0].

Trong code, các giá trị này được truyền vào renderer dưới dạng:

- `CaptureResult`: cung cấp `VirtualBounds` và `ScaleFactor`.
- `Selection`: cung cấp `Bounds`.
- `ConfigSnapshot` (hoặc giá trị mặc định): cung cấp màu và opacity.

---

## 4. Thuật toán vẽ

### 4.1 Vẽ trực tiếp bằng Clear

Cách đơn giản và hiệu quả:

1. Vẽ hình chữ nhật phủ toàn màn hình với màu tối alpha.
2. Vẽ hình chữ nhật vùng chọn với `BlendMode = Clear` để xóa phần tối trong vùng chọn.

Công thức:

```text
drawRect(screenBounds, dimmingPaint)
drawRect(selectionBounds, clearPaint)
```

Trong đó:

- `dimmingPaint`: màu đen, alpha = opacity.
- `clearPaint`: blend mode Clear.

### 4.2 Vẽ bằng Clip

Cách thay thế:

1. Lưu trạng thái canvas.
2. Tạo clip path là vùng chọn.
3. Đảo clip (`ClipDifference`) để chỉ vẽ bên ngoài vùng chọn.
4. Vẽ hình chữ nhật toàn màn hình với màu tối.
5. Khôi phục trạng thái canvas.

Cách này linh hoạt hơn nếu sau này cần khoét nhiều vùng, nhưng phức tạp hơn Clear.

### 4.3 Lựa chọn trong F-Shot

Trong MVP sử dụng cách **Clear** vì:

- Đơn giản, ít state trên canvas.
- Hiệu năng tốt với Skia.
- Dễ kiểm thử bằng cách đọc pixel.

---

## 5. Tọa độ và scaling

Tất cả tọa độ trong domain là logical. Khi vẽ, renderer chuyển sang physical pixels bằng `CaptureResult.ScaleFactor`:

```text
physicalX = round(logicalX * scaleFactor)
physicalY = round(logicalY * scaleFactor)
physicalWidth = round(logicalWidth * scaleFactor)
physicalHeight = round(logicalHeight * scaleFactor)
```

`ScreenBounds` physical = `VirtualBounds` × scaleFactor.
`SelectionBounds` physical = `Selection.Bounds` × scaleFactor.

---

## 6. Trạng thái hiển thị

Dimming chỉ hiển thị khi overlay có vùng chọn hoặc đang trong quá trình tạo vùng chọn. Cụ thể:

- `Idle`: phủ toàn màn hình, không có lỗ khoét.
- `Selecting`: khoét lỗ theo vùng chọn tạm thời.
- `Selected`: khoét lỗ theo vùng chọn đã cố định.
- `Moving` / `Resizing`: khoét lỗ theo vùng chọn đang di chuyển/co giãn.
- `Annotating`: khoét lỗ theo vùng chọn đã cố định.
- `Exporting`: tùy UI, thường vẫn hiển thị.

Trong `Idle` khi chưa có vùng chọn, có thể phủ toàn màn hình hoặc không phủ tùy UX. Trong F-Shot MVP, `Idle` phủ toàn màn hình để người dùng biết overlay đang active.

---

## 7. Tích hợp với SceneComposer

Thứ tự vẽ trong `SceneComposer`:

1. Screenshot bitmap.
2. **Dimming overlay**.
3. Selection border.
4. Resize handles.
5. Annotations.
6. Preview.
7. Toolbar / TextInput.

Dimming phải được vẽ ngay sau screenshot và trước selection border, để viền vùng chọn nổi rõ trên nền tối.

---

## 8. Cấu hình

Giá trị mặc định:

- `DimmingColor`: đen `#000000`.
- `DimmingOpacity`: 0.5 (50%).

Trong tương lai có thể cho phép người dùng điều chỉnh opacity trong Settings (thuộc P2/P3).

---

## 9. Phạm vi trong MVP

Trong MVP cần đảm bảo:

- Dimming hiển thị ở tất cả các trạng thái có vùng chọn.
- Vùng chọn không bị tối.
- Opacity mặc định 50%.
- Không ảnh hưởng đến ảnh xuất.

Không cần trong MVP:

- Tùy chỉnh màu dimming.
- Hiệu ứng gradient.
- Khoét nhiều vùng cùng lúc.

---

## 10. Kết nối với các file khác

- `docs/2.Design/09_Rendering/09_07_SceneComposer.md`: thứ tự layer vẽ.
- `src/FShot.Rendering.Skia/Renderers/SceneComposer.fs`: triển khai điều phối.
- `src/FShot.Rendering.Skia/Renderers/DimmingRenderer.fs`: renderer chuyên trách.
- `src/FShot.Core/Domain/Selection.fs`: cung cấp `Selection.Bounds`.
- `src/FShot.Core/Domain/Capture.fs`: cung cấp `CaptureResult.VirtualBounds`.

---

*Dimming là layer cơ bản giúp người dùng nhận biết vùng chọn. Triển khai xong phần này, Epic 2 sẽ tiếp tục với mouse operations và resize handles.*
