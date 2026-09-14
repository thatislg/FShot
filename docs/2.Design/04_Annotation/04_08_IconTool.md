# IconTool — Thiết kế chi tiết

> Tài liệu này thiết kế công cụ chèn icon/hình ảnh đánh dấu vào vùng chụp. Trong MVP, công cụ này chỉ là placeholder để dành chỗ phím tắt `I`; tính năng chèn icon thực sự sẽ triển khai trong v1.x.

---

## 1. Mục đích

`IconTool` cho phép người dùng chèn các biểu tượng, sticker, hoặc hình ảnh nhỏ vào vùng chụp để làm nổi bật hoặc minh họa. Ví dụ: mũi tên chỉ hướng, icon cảnh báo, logo, hoặc sticker cảm xúc.

---

## 2. Phạm vi MVP

Trong MVP:

- Công cụ được đăng ký trong `ToolKind` để chiếm giữ phím tắt `I`.
- Chưa mở panel chọn icon.
- Chưa render icon thực.
- Khi người dùng nhấn `I` hoặc click toolbar, công cụ được chọn nhưng chưa tạo annotation nào khi click chuột.
- UI có thể hiển thị một hình placeholder (hình chữ nhật có chữ "ICON" hoặc đường viền nét đứt) nếu muốn preview vị trí.

Trong v1.x:

- Mở thư viện icon / picker.
- Hỗ trợ kéo thả icon từ file.
- Hỗ trợ resize, xoay, di chuyển icon sau khi chèn.
- Lưu icon vào annotation dưới dạng tham chiếu file hoặc base64 tùy cấu hình.

---

## 3. Dữ liệu

### 3.1 ToolKind

`IconTool` là một giá trị trong `ToolKind`, ánh xạ phím tắt `I`.

### 3.2 Tool DU

Trong MVP, `Tool.Icon` chứa tối thiểu:

- `position`: điểm gốc trong domain space.
- `size`: kích thước placeholder (width, height).
- `iconId`: string định danh icon trong thư viện. Trong MVP luôn rỗng hoặc `"placeholder"`.

Cấu trúc:

```text
Icon of position: Point * size: Size * iconId: string
```

`Size` là kiểu dữ liệu hình học đã có trong `FShot.Core.Geometry`.

---

## 4. Cách tương tác (MVP placeholder)

1. Người dùng nhấn `I` hoặc click nút toolbar.
2. `CurrentTool` chuyển sang `IconTool`.
3. Khi người dùng click vào capture region, hệ thống commit ngay một annotation placeholder `Tool.Icon` tại vị trí click, kích thước 64×64 logical pixel.
4. Placeholder được render là hình chữ nhật nét đứt với chữ "ICON" bên trong.
5. Annotation này có thể được undo/redo như các annotation khác.

Trong v1.x, click sẽ mở picker chọn icon thay vì dùng placeholder.

---

## 5. Render

### 5.1 Trong UI overlay (CaptureCanvas)

Vẽ một hình chữ nhật nét đứt màu hiện tại với kích thước cố định 64×64 pixel tại vị trí click. Bên trong có thể vẽ chữ "ICON" để báo hiệu placeholder.

### 5.2 Trong Skia export renderer

Vẽ cùng placeholder khi export (nếu có annotation `Tool.Icon`). Trong MVP không cần vì chưa tạo annotation.

---

## 6. Phím tắt

- `I`: chuyển sang `IconTool`.
- `Esc`: hủy chế độ chọn icon (quay về `Selected` với tool hiện tại).

---

## 7. Nợ kỹ thuật

- Chưa có thư viện icon.
- Chưa có UI picker.
- Chưa lưu icon vào file cấu hình hoặc annotation.
- Chưa hỗ trợ resize/xoay.

---

## 8. Kết nối

- `docs/2.Design/04_Annotation/04_02_ToolModel.md`: mô tả chung về Tool.
- `docs/2.Design/08_OverlayState/08_03_Events.md`: sự kiện `SelectTool IconTool`.
- `src/FShot.Core/Domain/Annotation.fs`: định nghĩa `ToolKind.IconTool` và `Tool.Icon`.
- `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`: mapping phím `I`.

---

*IconTool được giới thiệu trong MVP chỉ để dành phím tắt `I` và chuẩn bị mở rộng trong v1.x.*
