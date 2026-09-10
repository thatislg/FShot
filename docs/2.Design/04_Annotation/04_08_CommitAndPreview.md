# CommitAndPreview — Thiết kế chi tiết

> Tài liệu này mô tả sự khác biệt giữa preview và commit, và cách tích hợp với History.

---

## 1. Preview là gì?

Preview là trạng thái tạm thời trong khi người dùng đang vẽ. Nó không được lưu vào danh sách chú thích chính thức cho đến khi hoàn tất.

Ví dụ: khi kéo đường thẳng, UI hiển thị đường thẳng tạm. Chỉ khi thả chuột, nó mới trở thành `Annotation`.

## 2. Commit là gì?

Commit là hành động biến preview thành annotation chính thức. Khi commit:

1. Tạo `Annotation` mới từ preview.
2. Thêm vào danh sách annotations.
3. Push snapshot của toàn bộ trạng thái vào History.
4. Xóa preview.

## 3. Công thức tạo Annotation từ preview

`annotation = { Id = newGuid(); Tool = previewTool; Color = currentColor; StrokeWidth = currentStrokeWidth; CreatedAt = now }`

## 4. Undo/Redo

Mỗi lần commit, state hiện tại (selection + annotations) được lưu snapshot. Undo khôi phục snapshot trước đó.

## 5. Kết nối với code

- `src/FShot.Core/Domain/Annotation.fs`
- `src/FShot.Core/Domain/History.fs`
- `src/FShot.Core/State/OverlayState.fs`
