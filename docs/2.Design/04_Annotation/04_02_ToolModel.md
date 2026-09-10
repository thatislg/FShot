# ToolModel — Thiết kế chi tiết

> Tài liệu này mô tả mô hình dữ liệu cho các công cụ chú thích.

---

## 1. Tool là gì?

`Tool` là kiểu dữ liệu phân biệt (DU) mô tả loại hình học mà người dùng đang vẽ. Mỗi loại tool có tham số riêng.

## 2. Các tool trong MVP

```
Tool
├── Pencil of Point list
├── Line of start: Point * end: Point
├── Arrow of start: Point * end: Point * arrowStyle
├── Rectangle of start: Point * end: Point * cornerRadius
├── Circle of start: Point * end: Point * aspectLocked
├── Marker of start: Point * end: Point
├── Text of position: Point * content: string
└── Pixelate of start: Point * end: Point * blockSize
```

## 3. Annotation record

Mỗi chú thích đã hoàn thành được lưu trong `Annotation`:

- `Id`: định danh.
- `Tool`: loại và tham số.
- `Color`: màu.
- `StrokeWidth`: độ dày nét.
- `CreatedAt`: thứ tự vẽ.

## 4. Preview và Commit

Trong lúc tương tác, chỉ có `Preview` (tool đang vẽ). Khi thả chuột hoặc kết thúc text, tạo `Annotation` mới và thêm vào danh sách.

## 5. Kết nối với code

- `src/FShot.Core/Domain/Annotation.fs`
- `src/FShot.Core/State/OverlayState.fs`
