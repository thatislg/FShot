# Schema Cấu hình — Thiết kế chi tiết

> Tài liệu này đặc tả lược đồ dữ liệu (JSON Schema), định dạng trường, giá trị mặc định và quy tắc kiểm tra hợp lệ của cấu hình F-Shot (`FR-CFG-001`, `FR-CFG-003`, `FR-CFG-200`, `FR-CFG-201`).

---

## 1. Tổng quan lược đồ JSON

F-Shot sử dụng định dạng JSON thuần để lưu trữ cấu hình người dùng tại `%APPDATA%\FShot\config.json`.
File được format dễ đọc (`WriteIndented = true`), phân biệt hoặc không phân biệt hoa thường khi nạp (`PropertyNameCaseInsensitive = true`), và tuân thủ chuẩn `camelCase` khi ghi.

### Cấu trúc JSON mẫu (MVP):

```json
{
  "savePath": "",
  "filenamePattern": "fshot_%Y-%m-%d-%H%M%S",
  "drawColor": "#FF0000",
  "drawThickness": 2.0,
  "defaultTool": "SelectionTool",
  "closeAfterExport": true
}
```

---

## 2. Chi tiết các trường dữ liệu MVP

| Tên trường (JSON) | Tên thuộc tính (F#) | Kiểu dữ liệu | Giá trị mặc định | Mô tả & Quy tắc kiểm tra (Validation) |
| :--- | :--- | :--- | :--- | :--- |
| `savePath` | `SavePath` | `string` | `""` | Đường dẫn thư mục lưu ảnh (`FR-CFG-001`). Nếu rỗng (`""`), ứng dụng sẽ mở hộp thoại Save As khi lưu. Nếu có đường dẫn hợp lệ, kích hoạt lưu tức thì (Instant Save). |
| `filenamePattern` | `FilenamePattern` | `string` | `"fshot_%Y-%m-%d-%H%M%S"` | Mẫu sinh tên file tự động (`FR-CFG-003`). Hỗ trợ các token: `%Y`, `%m`, `%d`, `%H`, `%M`, `%S`. Nếu rỗng hoặc null, fallback về mặc định. |
| `drawColor` | `DrawColor` | `string` | `"#FF0000"` | Mã màu vẽ mặc định (`FR-CFG-200`). Định dạng hex `#RRGGBB` hoặc `#RRGGBBAA`. Nếu không hợp lệ, tự động fallback về `#FF0000` (đỏ). |
| `drawThickness` | `DrawThickness` | `float` | `2.0` | Độ dày nét vẽ mặc định (`FR-CFG-201`), tính theo pixel logic. Tự động clamp trong khoảng `[1.0, 50.0]`. |
| `defaultTool` | `DefaultTool` | `string` | `"SelectionTool"` | Công cụ được kích hoạt sẵn khi mở overlay (`SelectionTool`, `PencilTool`, `LineTool`, `ArrowTool`, `RectangleTool`, `CircleTool`, `MarkerTool`, `TextTool`, `PixelateTool`, `IconTool`). Fallback: `SelectionTool`. |
| `closeAfterExport` | `CloseAfterExport` | `bool` | `true` | Tự động đóng overlay chụp ảnh sau khi xuất thành công ra file hoặc copy vào clipboard. |

---

## 3. Khả năng mở rộng cho Phase 2 (v1.0+)

Lược đồ JSON được thiết kế theo hướng mềm dẻo (permissive schema), cho phép bổ sung các trường thuộc Epic 9 trong tương lai mà không làm hỏng cấu hình cũ:

- `uiColor`, `contrastOpacity` (Cài đặt giao diện)
- `startupLaunch`, `disabledTrayIcon` (System Tray & Lifecycle)
- `historyLimit` (Undo/Redo)
- `predefinedColors` (Danh sách màu tùy chỉnh)

Khi nạp JSON, các trường mới chưa có trong file cũ sẽ nhận giá trị mặc định; ngược lại các trường chưa được app nhận diện sẽ được bỏ qua an toàn mà không gây crash.
