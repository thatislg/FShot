# MVP Config — Thiết kế kiến trúc & Chuyển đổi dữ liệu

> Tài liệu này mô tả mô hình dữ liệu cấu hình trong MVP, sự phân định giữa `AppConfig` và `ConfigSnapshot`, cùng cơ chế chuyển đổi hai chiều.

---

## 1. Phân định giữa `AppConfig` và `ConfigSnapshot`

Trong kiến trúc F-Shot, cấu hình được tách bạch rõ ràng giữa hai tầng:

```
┌────────────────────────────────────────┐
│           JSON File trên đĩa           │
│        (%APPDATA%\FShot\config.json)   │
└───────────────────▲────────────────────┘
                    │  System.Text.Json
                    ▼
┌────────────────────────────────────────┐
│            AppConfig (Domain)          │
│  - Phẳng, thân thiện tuần tự hóa       │
│  - Kiểu nguyên thủy: string, float...  │
│  - Thể hiện trạng thái cấu hình bền vững│
└───────────────────▲────────────────────┘
                    │
       ToSnapshot() │ FromSnapshot()
                    ▼
┌────────────────────────────────────────┐
│        ConfigSnapshot (Runtime)        │
│  - Phục vụ trực tiếp OverlayState      │
│  - Kiểu Domain: Color, StrokeWidth,    │
│    ToolKind, SaveOptions               │
│  - Bất biến (Immutable) tại thời điểm  │
│    phiên chụp bắt đầu                  │
└────────────────────────────────────────┘
```

### 1.1 `AppConfig`
- Định nghĩa trong `FShot.Core.Domain.Config`.
- Chứa các kiểu dữ liệu nguyên thủy (primitive types) phù hợp cho việc serialize/deserialize JSON một cách trực tiếp mà không cần converter phức tạp.
- Cung cấp phương thức `Normalized()` để kiểm tra biên và làm sạch dữ liệu nếu người dùng chỉnh sửa JSON bằng tay với giá trị không hợp lệ.

### 1.2 `ConfigSnapshot`
- Bản chụp trạng thái cấu hình phục vụ phiên chụp của `OverlayState`.
- Giữ nguyên lý bất biến: khi overlay đã mở, cấu hình bên ngoài nếu có thay đổi cũng không làm xáo trộn phiên vẽ hiện tại.
- Chứa các kiểu dữ liệu hình học và domain: `Color` (RGBA), `StrokeWidth`, `ToolKind`, `SaveOptions`.

---

## 2. Quy tắc chuyển đổi dữ liệu

### 2.1 Từ `AppConfig` sang `ConfigSnapshot` (`ToSnapshot`)
1. **`DefaultTool`**:
   - Khớp không phân biệt hoa thường tên công cụ (`"selection"`, `"pencil"`, `"arrow"`, `"line"`, v.v.).
   - Mặc định hoặc chuỗi không xác định: chuyển thành `ToolKind.SelectionTool`.
2. **`DrawColor`**:
   - Giải mã chuỗi hex qua `Operations.colorFromHex`.
   - Nếu lỗi cú pháp hoặc rỗng: dùng `Color.Red` (`#FF0000`).
3. **`DrawThickness`**:
   - Chuẩn hóa qua `StrokeWidth.Create(v)`. Tự động clamp giá trị trong `[1.0, 50.0]`.
4. **`SaveOptions`**:
   - `Path`: nếu `SavePath` là chuỗi không rỗng thì chuyển thành `Some path`, ngược lại `None`.
   - `FileNamePattern`: dùng `FilenamePattern` cấu hình, nếu rỗng dùng `"fshot_%Y-%m-%d-%H%M%S"`.
   - `Format`: mặc định `Png`.
   - `JpegQuality`: mặc định `90`.
5. **`CloseAfterExport`**:
   - Sao chép trực tiếp giá trị boolean.

### 2.2 Từ `ConfigSnapshot` sang `AppConfig` (`FromSnapshot`)
- `SavePath`: trích xuất từ `snapshot.SaveOptions.Path |> Option.defaultValue ""`
- `FilenamePattern`: `snapshot.SaveOptions.FileNamePattern`
- `DrawColor`: chuyển từ `snapshot.DefaultColor.ToHex()` (dạng `#RRGGBBAA` hoặc `#RRGGBB`)
- `DrawThickness`: `snapshot.DefaultStrokeWidth.Value`
- `DefaultTool`: chuỗi tên phân loại của `ToolKind` (ví dụ `"SelectionTool"`, `"PencilTool"`)
- `CloseAfterExport`: `snapshot.CloseAfterExport`
