# CaptureMode — Thiết kế chi tiết

> Tài liệu này mô tả các chế độ chụp màn hình trong F-Shot.
> Mỗi chế độ xác định vùng ảnh nào sẽ được chụp và cách ứng dụng phản hồi sau khi chụp.

---

## 1. CaptureMode là gì?

`CaptureMode` là danh sách các cách thức mà F-Shot có thể chụp màn hình. Nó trả lời câu hỏi: "Chụp cái gì và bắt đầu từ đâu?"

Các chế độ này được dùng trong hai ngữ cảnh chính:

- **CLI**: người dùng gõ lệnh `fshot full`, `fshot screen`, hoặc `fshot gui`.
- **Hotkey / Tray**: người dùng nhấn tổ hợp phím nóng hoặc chọn từ khay hệ thống.

---

## 2. Các chế độ chụp

### 2.1 FullScreen — Chụp toàn bộ màn hình hoạt động

Chụp toàn bộ màn hình mà con trỏ chuột đang ở đó. Đây là chế độ đơn giản nhất và thường được dùng khi người dùng muốn chụp nhanh toàn bộ một màn hình.

Ví dụ: người dùng đang làm việc trên màn hình thứ hai và nhấn `PrintScreen`. F-Shot sẽ chụp toàn bộ màn hình thứ hai đó.

### 2.2 SingleScreen — Chụp một màn hình cụ thể

Chụp một màn hình xác định bởi chỉ số thứ tự hoặc theo vị trí con trỏ. Chỉ số màn hình bắt đầu từ 0 theo thứ tự mà Windows liệt kê.

Ví dụ: `fshot screen -n 1` sẽ chụp màn hình thứ hai trong danh sách màn hình. Nếu chỉ số không hợp lệ, F-Shot có thể chuyển sang chụp màn hình có con trỏ hoặc báo lỗi.

### 2.3 GuiInteractive — Chụp tương tác qua overlay

Mở một cửa sổ phủ toàn màn hình cho phép người dùng kéo chuột để chọn vùng, sau đó có thể vẽ chú thích trước khi lưu. Đây là chế độ linh hoạt nhất và là cốt lõi của F-Shot.

Ví dụ: người dùng nhấn `Ctrl + Shift + S`, cửa sổ overlay xuất hiện, người dùng kéo chọn vùng chat, thêm mũi tên và đánh dấu, rồi nhấn Enter để lưu.

### 2.4 LastRegion — Chụp lại vùng trước đó

Dùng lại vùng chọn từ lần chụp gần nhất. Chế độ này hữu ích khi cần chụp nhiều ảnh cùng một kích thước ở cùng một vị trí, chẳng hạn theo dõi tiến độ một cửa sổ.

Ví dụ: người dùng đã chụp vùng `(100, 100, 500, 400)` lần trước. Lần sau dùng `fshot gui --last-region`, vùng chọn ban đầu sẽ là `(100, 100, 500, 400)`.

---

## 3. Cách chọn chế độ

Chế độ chụp được chọn theo thứ tự ưu tiên:

1. Tham số dòng lệnh, ví dụ `fshot full`.
2. Cấu hình mặc định nếu CLI không chỉ định.
3. Hotkey được gán trước.

Nếu người dùng chỉ chạy `fshot` không kèm tham số, F-Shot có thể mặc định sang `GuiInteractive` hoặc `FullScreen` tùy cấu hình.

---

## 4. Mối quan hệ với các phần khác

### 4.1 CaptureMode và CaptureRequest

`CaptureMode` là một thành phần bên trong `CaptureRequest`. Một yêu cầu chụp cần biết chế độ, độ trễ, vùng ban đầu, và cách xuất ảnh.

### 4.2 CaptureMode và CaptureResult

Mỗi chế độ sẽ cho ra một `CaptureResult` khác nhau về kích thước và vị trí Virtual Bounds:

- `FullScreen`: Virtual Bounds của màn hình có con trỏ.
- `SingleScreen`: Virtual Bounds của màn hình được chỉ định.
- `GuiInteractive`: toàn bộ Virtual Screen, để overlay hiển thị đúng trên mọi màn hình.
- `LastRegion`: Virtual Bounds phủ đủ toàn bộ vùng chọn cũ.

### 4.3 CaptureMode và Config

Người dùng có thể đặt chế độ mặc định trong file cấu hình, ví dụ `defaultMode = "gui"`.

---

## 5. Các câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Chế độ mặc định khi chạy `fshot` không tham số là gì? | Ảnh hưởng UX khi khởi động |
| `SingleScreen` có hỗ trợ tên màn hình như `primary`, `cursor` không? | Mở rộng CLI parsing |
| `LastRegion` lưu vùng ở đâu? | Config hoặc file state riêng |
| Có chế độ chụp cửa sổ đang active không? | Flameshot gốc có tính năng này, có thể để v1.x |

---

## 6. Kết nối với phần triển khai

Những quyết định trong tài liệu này sẽ được đưa vào:

- `src/FShot.Core/Domain/Capture.fs`: định nghĩa kiểu `CaptureMode`.
- `src/FShot.UI/Program.fs`: phân giải CLI thành `CaptureRequest`.
- `src/FShot.Core/Domain/Config.fs`: lưu chế độ mặc định.

---

*Sau khi chốt CaptureMode, chúng ta chuyển sang CaptureRequest — cách đóng gói một lần chụp thành dữ liệu.*
