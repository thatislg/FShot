Phân tích tính năng của **Flameshot** dưới góc nhìn kiến trúc phần mềm, quy chiếu trực tiếp sang hệ sinh thái **F# / .NET** và **Avalonia UI** trên Windows:  

### 1. Hệ thống tương tác và Overlay chụp màn hình (Capture Core)

Đây là cốt lõi phức tạp nhất của Flameshot:

- **Toàn màn hình phủ mờ (Darkened Overlay):** Chụp snapshot toàn bộ desktop trước, sau đó dựng một cửa sổ không viền (borderless, topmost, transparent) đè lên tất cả màn hình.
- **Hỗ trợ Multi-monitor & Mixed DPI:** Tự động tính toán Virtual Screen Bounds bao gồm toàn bộ màn hình ghép lại, xử lý lệch tỷ lệ scale (100%, 125%, 150%).
- **Vùng chọn động (Region Selection):**
  - Kéo chuột tự do tạo bounding box hình chữ nhật.  
  - 8 điểm neo (resize handles) để co giãn vùng chọn sau khi thả chuột.
  - Kéo di chuyển toàn bộ vùng chọn (Move selection).  
  - Dùng phím mũi tên điều chỉnh tọa độ từng pixel (`Arrow keys`, `Shift + Arrow`).
- **Kính lúp phóng to (Magnifier):** Hiển thị pixel grid xung quanh con trỏ chuột kèm tọa độ $(X, Y)$ và mã màu HEX/RGB tại vị trí trỏ chuột.
- **Nhớ vùng chọn cũ (Last Region):** Chụp lại đúng tọa độ đã chụp lần trước (`--last-region`).  

### 2. Bộ công cụ chú thích tại chỗ (In-app Annotation Tools)

Flameshot nổi tiếng nhờ thanh công cụ trực quan bo quanh vùng chọn:

| **Nhóm công cụ**        | **Tính năng cụ thể**                                         | **Thách thức kỹ thuật F# / Avalonia**                        |
| ----------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **Vẽ cơ bản**           | Bút vẽ tự do (Pencil), Đường thẳng (Line), Mũi tên (Arrow).  | Dùng Canvas tùy chỉnh / SkiaSharp, làm mịn nét (Bézier smoothing). |
| **Hình học**            | Hình chữ nhật (Rectangle), Elip/Hình tròn (Circle), Đổ bóng/Filled. | Giữ phím `Ctrl` để ép vuông/tròn tỉ lệ 1:1.                  |
| **Đánh dấu & Che mờ**   | Bút nhớ (Highlighter - alpha blend), Làm mờ (Blur), Điểm ảnh hóa (Pixelate/Mosaic). | Shader effect hoặc xử lý mảng byte Skia bitmap trực tiếp trong bounding box. |
| **Văn bản & Số thứ tự** | Text tool (đổi font/size), Counter tool (bong bóng số tự tăng 1, 2, 3...). | Text formatting, auto-increment state. Cần mô hình dữ liệu dạng F# discriminated unions. |
| **Biến đổi**            | Undo / Redo vô hạn.                                          | Cấu trúc dữ liệu Immutable List trong F# cực kỳ phù hợp cho Event Sourcing / Action Stacks. |
| **Tùy biến nét**        | Bảng màu tròn (Color wheel), cuộn chuột chỉnh Stroke width.  | Xử lý sự kiện `PointerWheelChanged` và custom popup.         |

### 3. Xuất & Tương tác ngoại vi (Output & Integration)

- **Ghim lên màn hình (Pin to Desktop):** Cắt vùng ảnh vừa chụp hiển thị thành một cửa sổ Topmost trôi nổi, có thể thu nhỏ, kéo thả hoặc chỉnh độ trong suốt.
- **Clipboard & File System:** Lưu vào clipboard (`Ctrl+C`), lưu ra file (`Ctrl+S`) với quy tắc đặt tên tự động theo format thời gian (`flameshot_%Y-%m-%d_%H-%M-%S.png`).
- **Open with App:** Đưa ảnh sang trình xem ảnh mặc định của Windows.
- **Upload Cloud:** Mặc định Flameshot hỗ trợ tải trực tiếp lên Imgur và sinh link vào clipboard.  

### 4. Hệ thống nền & Cấu hình (System Integration)

- **Chạy nền (System Tray):** Icon góc taskbar với context menu (Chụp mới, Lịch sử, Cấu hình, Thoát).
- **Global Hotkey:** Đăng ký phím tắt toàn hệ thống (như phím `PrintScreen`, `Ctrl+Shift+X`) ngay cả khi app đang chạy ngầm.
- **Giao diện CLI:** Nhận lệnh qua command line (ví dụ: `flameshot gui`, `flameshot full -c`, `flameshot gui -d 2000`).

### 5. Ánh xạ công nghệ kiến nghị (F# + Avalonia trên Windows)

- **Chụp màn hình (Screen Capture):** Avalonia không có sẵn API chụp màn hình Win32. Bạn cần gọi P/Invoke `BitBlt` hoặc thư viện hiện đại hơn là **Windows.Graphics.Capture API** (DirectX, mượt và hỗ trợ tốt đa màn hình/DPI).

- **Hiển thị & Vẽ (Rendering Engine):** Avalonia chạy trên nền **SkiaSharp**. Tận dụng `WriteableBitmap` và Skia Canvas để vẽ vector annotations thay vì add từng Avalonia UI element (giúp render mượt 60+ FPS).

- **Mô hình trạng thái (State Modeling):** Dùng kiến trúc **Elmish (Avalonia.FuncUI)** hoặc MVVM thuần F#. F# Discriminated Unions rất mạnh để biểu diễn các công cụ:

  F#

  ```
  type Tool = 
      | Pencil of StrokeSize: float * Color: SKColor
      | Rectangle of Filled: bool * Color: SKColor
      | Arrow of Color: SKColor
      | Counter of CurrentIndex: int
      | Pixelate of BlockSize: int
  ```

- **Global Hotkey:** Dùng Win32 API `RegisterHotKey` / hook bàn phím qua P/Invoke.