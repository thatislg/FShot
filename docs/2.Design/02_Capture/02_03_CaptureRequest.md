# CaptureRequest — Thiết kế chi tiết

> Tài liệu này mô tả cách F-Shot đóng gói một yêu cầu chụp màn hình.
> CaptureRequest là "lệnh" mà phần UI hoặc CLI gửi đến phần Core để thực hiện chụp.

---

## 1. CaptureRequest là gì?

`CaptureRequest` là một gói thông tin mô tả đầy đủ một lần chụp. Nó chứa:

- Chế độ chụp: toàn màn hình, một màn hình, overlay tương tác, hay vùng cũ.
- Độ trễ trước khi chụp.
- Vùng chọn ban đầu (nếu có).
- Có tự động chấp nhận vùng chọn khi thả chuột hay không.
- Mục tiêu xuất ảnh: file, clipboard, stdout, hay mở GUI.

CaptureRequest không thực hiện chụp. Nó chỉ là dữ liệu đầu vào để phần backend capture quyết định cách lấy ảnh.

---

## 2. Các thành phần của CaptureRequest

### 2.1 Mode

`Mode` là một trong các giá trị của `CaptureMode` đã thiết kế ở tài liệu trước: `FullScreen`, `SingleScreen`, `GuiInteractive`, hoặc `LastRegion`.

### 2.2 DelayMs

`DelayMs` là số milli-giây chờ trước khi bắt đầu chụp. Độ trễ cho phép người dùng di chuyển chuột ra khỏi menu hoặc chuẩn bị màn hình.

Ví dụ: `DelayMs = 3000` nghĩa là đợi 3 giây rồi mới chụp. Nếu người dùng đang mở menu, họ có thời gian đóng menu trước khi ảnh được chụp.

### 2.3 InitialSelection

`InitialSelection` là một `Rect` tùy chọn, dùng để đặt sẵn vùng chọn khi mở overlay. Nếu không có, overlay sẽ bắt đầu với vùng chọn rỗng.

Ví dụ: `fshot gui --region 100,100,500,400` sẽ tạo CaptureRequest với `InitialSelection` là Rect `(100, 100, 500, 400)`.

### 2.4 AcceptOnSelect

`AcceptOnSelect` là một cờ boolean. Khi `true`, người dùng chỉ cần kéo vùng chọn và thả chuột, F-Shot sẽ tự động xuất ảnh ngay. Khi `false`, người dùng phải nhấn Enter hoặc nút xác nhận để xuất.

Ví dụ: `fshot gui --accept-on-select` giúp chụp nhanh khi chỉ cần crop, không cần vẽ thêm.

### 2.5 OutputTarget

`OutputTarget` mô tả ảnh sẽ được xuất đi đâu. Các khả năng:

- `File path`: lưu vào đường dẫn cụ thể.
- `Clipboard`: sao chép ảnh vào clipboard.
- `Stdout`: gửi ảnh dạng binary ra stdout, dùng cho piping.
- `OpenGui`: mở overlay để người dùng tiếp tục chỉnh sửa.

---

## 3. Công thức tính toán độ trễ

Thời điểm chụp thực tế được tính từ thời điểm nhận request:

`captureTime = requestTime + DelayMs`

Trong đó:
- `requestTime` là thời điểm hệ thống nhận được CaptureRequest.
- `DelayMs` là độ trễ người dùng yêu cầu, tính bằng milli-giây.
- `captureTime` là thời điểm thực sự bắt đầu chụp.

Ví dụ: người dùng gửi request lúc `T = 0`, `DelayMs = 1500`. Hệ thống sẽ bắt đầu chụp ở `T = 1500` milli-giây.

---

## 4. Các ràng buộc trên CaptureRequest

- `DelayMs` không được âm. Nếu âm, đặt về 0.
- `InitialSelection` nếu có phải là Rect hợp lệ (Width và Height >= 0).
- `Mode` và `OutputTarget` phải tương thích. Ví dụ, nếu `Mode = GuiInteractive` thì `OutputTarget` thường là `OpenGui`, còn nếu `Mode = FullScreen` thì `OutputTarget` có thể là `File` hoặc `Clipboard`.

---

## 5. Mối quan hệ với các phần khác

### 5.1 CaptureRequest và CLI

CLI parser sẽ chuyển các tham số dòng lệnh thành một CaptureRequest. Ví dụ:

```
fshot full -p screenshot.png -d 3000
```

tương đương với:

- `Mode = FullScreen`
- `OutputTarget = File "screenshot.png"`
- `DelayMs = 3000`

### 5.2 CaptureRequest và OverlayState

Khi `Mode = GuiInteractive`, CaptureRequest được chuyển thành trạng thái ban đầu của overlay. `InitialSelection` trở thành vùng chọn ban đầu, `AcceptOnSelect` ảnh hưởng cách overlay chuyển sang xuất ảnh.

### 5.3 CaptureRequest và History

`LastRegion` cần đọc vùng chọn từ lịch sử. Ngược lại, sau mỗi lần chụp thành công qua overlay, vùng chọn cuối cùng được ghi vào lịch sử để dùng cho `LastRegion` sau này.

---

## 6. Các câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Delay tối đa cho phép là bao nhiêu? | Tránh người dùng đặt delay quá lớn |
| Có cho phép kết hợp nhiều OutputTarget cùng lúc không? | Ví dụ lưu file và clipboard |
| `AcceptOnSelect` có bỏ qua bước vẽ chú thích không? | Ảnh hưởng luồng overlay |
| `LastRegion` nên lấy từ lần chụp nào: lần cuối cùng, lần cuối cùng có vùng chọn, hay lần cuối cùng chụp thành công? | Ảnh hưởng behavior |

---

## 7. Kết nối với phần triển khai

Những quyết định trong tài liệu này sẽ được đưa vào:

- `src/FShot.Core/Domain/Capture.fs`: kiểu `CaptureRequest`.
- `src/FShot.UI/Program.fs` hoặc `src/FShot.UI/CLI/Args.fs`: parse CLI thành `CaptureRequest`.
- `src/FShot.Core/State/OverlayState.fs`: chuyển `CaptureRequest` sang trạng thái overlay.

---

*Sau khi chốt CaptureRequest, chúng ta chuyển sang CaptureResult — dữ liệu trả về sau khi chụp.*
