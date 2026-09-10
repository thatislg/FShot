# Chi tiết Phase 1: MVP Core

> Tài liệu này liệt kê phạm vi MVP, acceptance criteria và phương châm triển khai.  
> Chỉ chứa **tư duy thiết kế và tiêu chí kiểm chứng** — không chứa code implementation mẫu.

---

## 1. Phạm vi MVP

MVP của F-Shot là một ứng dụng chụp màn hình Windows có thể:

1. Chụp toàn màn hình hoặc một vùng tùy chọn.
2. Cho phép người dùng kéo vùng chọn, điều chỉnh kích thước, di chuyển.
3. Vẽ chú thích cơ bản: Pencil, Line, Arrow, Rectangle, Circle, Marker, Text, Pixelate.
4. Hoàn tác / làm lại các thao tác vẽ.
5. Lưu ảnh ra file hoặc copy vào clipboard.

**Không bao gồm trong MVP:** system tray, global hotkey, pin widget, config UI đầy đủ, upload Imgur, i18n, update checker, import/export config.

---

## 2. Acceptance Criteria theo nhóm

### 2.1 Chụp màn hình

| Mã SRS | Tính năng | Tiêu chí chấp nhận |
|--------|-----------|--------------------|
| FR-CAP-01 | Overlay tương tác | Mở cửa sổ không viền phủ toàn Virtual Screen, hiển thị ảnh desktop đã chụp |
| FR-CAP-02 | Full-screen capture | Lệnh `fshot full -p path` chụp toàn bộ màn hình và lưu ngay, không hiện UI |
| FR-CAP-03 | Single-screen capture | Lệnh `fshot screen -n 0` chụp màn hình chỉ định; `fshot screen` chụp màn hình có con trỏ |
| FR-CAP-05 | Delayed capture | Lệnh `fshot gui -d 2000` chờ 2 giây rồi mở overlay |

### 2.2 Vùng chọn

| Mã SRS | Tính năng | Tiêu chí chấp nhận |
|--------|-----------|--------------------|
| FR-SEL-01 | Overlay tối | Phần ngoài vùng chọn được phủ màu đen với độ mờ có thể cấu hình |
| FR-SEL-02 | Kéo vùng chọn | Kéo chuột trái tạo hình chữ nhật từ điểm bắt đầu đến điểm kết thúc |
| FR-SEL-03 | 8 điểm neo | Di chuột qua cạnh/góc thay đổi cursor; kéo điểm neo thay đổi kích thước |
| FR-SEL-04 | Di chuyển vùng | Kéo bên trong vùng chọn di chuyển toàn bộ vùng, không mất kích thước |
| FR-SEL-05 | Nudge 1 px | Phím mũi tên dịch vùng chọn 1 pixel |
| FR-SEL-06 | Resize 1 px | `Shift + Arrow` thay đổi kích thước 1 pixel theo hướng mũi tên |
| FR-SEL-11 | Hủy chụp | `Esc` hoặc `Ctrl+Backspace` đóng overlay và không lưu gì cả |

### 2.3 Công cụ chú thích

| Mã SRS | Tính năng | Tiêu chí chấp nhận |
|--------|-----------|--------------------|
| FR-ANN-01 | Pencil | Vẽ nét tự do mượt; nét được lưu và hiển thị khi vẽ xong |
| FR-ANN-02 | Line | Vẽ đường thẳng từ điểm nhấn đến điểm thả |
| FR-ANN-03 | Arrow | Vẽ đường thẳng kèm đầu mũi tên tại điểm kết thúc |
| FR-ANN-04 | Rectangle | Vẽ hình chữ nhật; hỗ trợ bo góc theo cấu hình |
| FR-ANN-05 | Circle | Vẽ hình tròn/elip; giữ `Ctrl` để khóa tỉ lệ 1:1 |
| FR-ANN-06 | Marker | Nét bán trong suốt alpha ~0.5 để làm nổi bật |
| FR-ANN-07 | Text | Click đặt hộp text, gõ văn bản, `Return` hoặc click ngoài commit |
| FR-ANN-08 | Pixelate | Làm mờ/mosaic vùng chọn theo block size |

### 2.4 Undo / Redo

| Mã SRS | Tính năng | Tiêu chí chấp nhận |
|--------|-----------|--------------------|
| FR-UNDO-01 | Undo | `Ctrl+Z` xóa bước vẽ vừa thêm; có thể undo nhiều lần |
| FR-UNDO-02 | Redo | `Ctrl+Shift+Z` hoặc `Ctrl+Y` khôi phục bước đã undo |

### 2.5 Xuất dữ liệu

| Mã SRS | Tính năng | Tiêu chí chấp nhận |
|--------|-----------|--------------------|
| FR-OUT-01 | Save file | `Ctrl+S` lưu ảnh cuối theo đường dẫn và định dạng mặc định |
| FR-OUT-02 | Fixed save path | Nếu `savePathFixed=true`, lưu ngay vào `savePath` không hỏi |
| FR-OUT-03 | Filename pattern | Tên file tự sinh theo mẫu, ví dụ `2026-09-10_14-30-25.png` |
| FR-OUT-04 | Save dialog | Nếu chưa có `savePath` cố định, hiện dialog chọn nơi lưu |
| FR-OUT-05 | Clipboard | `Ctrl+C` copy ảnh cuối vào clipboard dạng PNG |

### 2.6 Cấu hình tối thiểu

| Mã SRS | Tính năng | Tiêu chí chấp nhận |
|--------|-----------|--------------------|
| FR-CFG-001 | Save path | Đọc/ghi đường dẫn lưu mặc định từ file config |
| FR-CFG-003 | Filename pattern | Đọc/ghi mẫu tên file từ file config |
| FR-CFG-200 | Draw color | Lưu màu vẽ cuối, dùng cho công cụ tiếp theo |
| FR-CFG-201 | Draw thickness | Lưu độ dày nét cuối, dùng cho công cụ tiếp theo |

### 2.7 Phím tắt

| Mã SRS | Tính năng | Tiêu chí chấp nhận |
|--------|-----------|--------------------|
| FR-SH-001 đến FR-SH-010 | Chuyển công cụ | Phím P/D/A/S/R/C/M/T/B/I kích hoạt đúng công cụ |
| FR-SH-011 | Di chuyển vùng | `Ctrl+M` chuyển sang mode di chuyển vùng |
| FR-SH-012 | Undo | `Ctrl+Z` undo |
| FR-SH-013 | Redo | `Ctrl+Shift+Z` redo |
| FR-SH-014 | Copy | `Ctrl+C` copy |
| FR-SH-015 | Save | `Ctrl+S` save |
| FR-SH-016 | Hủy | `Ctrl+Q` hoặc `Ctrl+Backspace` đóng overlay |
| FR-SH-019 | Commit | `Return` kết thúc chụp và xuất |
| FR-SH-022 | Nudge | Phím mũi tên di chuyển vùng |
| FR-SH-023 | Resize keyboard | `Shift + Arrow` co giãn vùng |

### 2.8 Yêu cầu phi chức năng

| Mã SRS | Tính năng | Tiêu chí chấp nhận |
|--------|-----------|--------------------|
| FR-NF-001 | FPS | Render đạt ≥ 60 FPS khi kéo chuột |
| FR-NF-002 | Phản hồi | Phím tắt và thanh công cụ phản hồi trong 1 frame |
| FR-NF-003 | Mixed DPI | Vùng chọn đúng tọa độ trên màn hình 100% + 150% |
| FR-NF-004 | High-DPI | UI và ảnh chụp tôn trọng scale factor |
| FR-NF-008 | Xử lý lỗi | Không crash khi chụp thất bại; hiển thị lỗi rõ ràng |

---

## 3. Phương châm triển khai

### Epic 1: Foundation

- Ưu tiên định nghĩa **domain model đúng** trước khi viết bất kỳ UI nào.
- Mọi kiểu dữ liệu trong `FShot.Core` phải là **immutable**.
- Viết unit test song song với domain model.

### Epic 2: Overlay Window & Selection

- Không dùng controls Avalonia cho vùng chọn; vẽ hoàn toàn bằng renderer.
- Hit-testing điểm neo dựa trên tọa độ chuột và bán kính dung sai.
- Các phép biến đổi vùng chọn (move, resize, nudge) là pure functions.

### Epic 3: Annotation Tools

- Mỗi tool là một case trong DU; không dùng inheritance OOP.
- Tool đang vẽ dở chỉ là **preview**, chưa đẩy vào history cho đến khi commit.
- Màu và độ dày nét lấy từ config/state hiện tại.

### Epic 4: Undo / Redo

- Mỗi lần commit annotation phải push snapshot mới.
- Không push snapshot khi chỉ di chuyển vùng chọn hoặc đang preview.
- Redo stack bị xóa khi có thao tác mới.

### Epic 5: Export

- Tách rõ **tính toán export target** (Core) và **thực hiện IO** (Platform).
- `FShot.Core` cung cấp: ảnh cuối (byte array), định dạng, tên file đề xuất.
- `FShot.Platform.Win32` thực hiện: ghi file, copy clipboard, stdout.

### Epic 6: CLI & Config

- CLI parser đơn giản, đủ cho `gui`, `full`, `screen` và các flag cơ bản.
- Config ở MVP chỉ là file đọc/ghi đơn giản; chưa cần UI config.
- Lưu config tại `%APPDATA%\Roaming\FShot\`.

### Epic 7: Polish & NFR

- Đo FPS bằng công cụ dev đơn giản.
- Test trên ít nhất 2 cấu hình DPI khác nhau.
- Xử lý lỗi capture bằng cách hiển thị thông báo và thoát gracefully.

---

## 4. Definition of Done cho MVP

- [ ] `fshot full` và `fshot gui` chạy được trên Windows 10/11.
- [ ] Có thể chụp vùng, vẽ 8 loại chú thích, undo/redo, save/copy.
- [ ] Unit test pass cho `FShot.Core` (geometry, history, export).
- [ ] Không crash khi chụp thất bại.
- [ ] Đạt ≥ 60 FPS khi kéo vùng chọn trên màn hình 1080p.

---

## 5. Câu hỏi cần quyết định trong Phase 1

| Câu hỏi | Tác động |
|---------|----------|
| Text tool dùng inline Avalonia TextBox hay render hoàn toàn bằng Skia? | Ảnh hưởng UX và cách commit text |
| Pixelate nên xử lý trên CPU (byte array) hay GPU shader? | Ảnh hưởng hiệu năng và bảo mật |
| Pencil smoothing dùng thuật toán nào? | Ảnh hưởng cảm giác vẽ tay tự do |
| Có cần render toolbar ở MVP không, hay dùng phím tắt trước? | Ảnh hưởng scope MVP |
| Filename pattern dùng .NET `DateTime.ToString` hay custom tokenizer? | Ảnh hưởng tương thích với `flameshot.ini` cũ |

---

**Quyết định kỹ thuật cho các vấn đề cốt lõi**

| **Vấn đề kỹ thuật**                            | **Phương án tối ưu**                                         | **Phân tích chi tiết & Lý do lựa chọn**                      |
| ---------------------------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **1. Text Tool (Avalonia TextBox vs Skia)**    | **Hybrid:** Gõ bằng Avalonia TextBox, commit sang Skia       | • Khi gõ: Hiện một `TextBox` trong suốt (không viền, caret nhấp nháy) đúng vị trí click để tận dụng bộ gõ IME tiếng Việt, clipboard OS, chọn bôi đen text. <br /> • Khi commit (`Enter` / click ngoài): Lấy string, font, tọa độ chuyển thành `Annotation.Text` để Skia render phẳng vào canvas và hủy control `TextBox`. Tự code toàn bộ text input trên Skia rất tốn công và dễ lỗi gõ dấu IME. |
| **2. Pixelate (CPU byte array vs GPU Shader)** | **CPU (Skia Bitmap byte array)**                             | • Vùng pixelate trong ảnh chụp màn hình thường nhỏ ($< 500 \times 500$ px), xử lý chia khối trung bình màu (box blur / downscale-upscale) trên CPU bằng `Span<byte>` chỉ mất $< 2$ ms.  <br />• Quan trọng nhất: Ghi đè triệt để mảng byte gốc để ngăn trích xuất dữ liệu nhạy cảm (`FR-NF-007`). GPU Shader phức tạp khi export ảnh tĩnh và có nguy cơ chỉ áp hiệu ứng hiển thị mà không xóa dữ liệu thô. |
| **3. Pencil Smoothing**                        | **Catmull-Rom Spline sang Bézier** (hoặc Chaikin's Algorithm) | • Khi người dùng di chuột nhanh, các điểm bắt được cách xa nhau. Dùng thuật toán nội suy điểm giữa (Midpoint Quad Bézier) của SkiaPath: lấy trung điểm giữa điểm trước và điểm hiện tại làm điểm điều khiển (Control Point). <br /> • Cho nét vẽ mượt mà tự nhiên, không bị gãy góc (polyline) và tốn rất ít chi phí tính toán theo thời gian thực. |
| **4. Toolbar ở MVP**                           | **Có Toolbar tối giản** (Ưu tiên Bottom Bar)                 | • Không thể phụ thuộc 100% vào phím tắt vì người dùng phổ thông không nhớ hết shortcut (`P`, `R`, `C`, `Ctrl+C`).    <br />• MVP chỉ cần dựng một thanh ngang cố định bên dưới bounding-box gồm 6 nút icon SVG cơ bản: *Pencil, Rect, Arrow, Pixelate, Copy, Save*. Các logic tự nhảy tránh mép màn hình (`FR-TB-04`) có thể để Phase sau.   <br /> |
| **5. Filename Pattern**                        | **Custom Tokenizer ánh xạ sang .NET format**                 | •Phần này sử dụng công cụ .NET, ko sử dụng theo Flameshot    |

