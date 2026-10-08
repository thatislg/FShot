# ConfigStore — Thiết kế chi tiết Lưu trữ Cấu hình trên nền tảng Win32

> Tài liệu này thiết kế chi tiết tầng lưu trữ cấu hình trên hệ điều hành Windows cho F-Shot (`FR-CFG-001`, `FR-SYS-011`, `FR-SYS-014`).  
> **Nguyên tắc tài liệu:** Thiết kế thuần kiến trúc, cơ chế ghi nguyên tử (Atomic Write), quản lý đồng thời (Concurrency), giám sát thay đổi file (File Watching) — **hoàn toàn không sử dụng code mẫu**.

---

## 1. Trách nhiệm của Module

Module `ConfigStore` trong `FShot.Platform.Win32` chịu trách nhiệm làm cầu nối giữa mô hình dữ liệu cấu hình thuần túy (`FShot.Core.Domain.Config`) và hệ thống tập tin vật lý của Windows:
1. **Quản lý vị trí lưu trữ:** Phân giải thư mục dữ liệu ứng dụng của người dùng theo chuẩn Roaming AppData.
2. **Ghi tệp an toàn (Atomic File Persistence):** Bảo vệ tính toàn vẹn của tệp cấu hình trước các sự cố mất điện đột ngột hoặc dừng tiến trình đột ngột.
3. **Kiểm soát truy cập đồng thời:** Xử lý xung đột khi nhiều tiến trình (ví dụ: tiến trình nền daemon và tiến trình dòng lệnh CLI) cùng truy cập tệp cấu hình.
4. **Tự động tải lại cấu hình khi tệp thay đổi (Hot-Reload):** Giám sát tệp cấu hình từ hệ điều hành để cập nhật tức thì vào bộ nhớ khi người dùng sửa thủ công hoặc sửa qua ứng dụng khác.

---

## 2. Cấu trúc và Vị trí Lưu trữ Hệ thống

- **Đường dẫn thư mục:** Nằm trong thư mục chuyển vùng của người dùng Windows (`CSIDL_APPDATA` tương ứng biến môi trường `%APPDATA%\FShot`).
- **Lý do chọn Roaming AppData:** Cấu hình người dùng (phím tắt, màu sắc, thư mục lưu) thuộc dạng cấu hình cá nhân cần được đồng bộ khi người dùng đăng nhập trong mạng doanh nghiệp Active Directory hoặc chuyển đổi máy tính trong cùng tài khoản.
- **Tập tin chính:** `config.json` (định dạng văn bản UTF-8 không có BOM).
- **Tập tin tạm thời phục vụ ghi nguyên tử:** `config.json.tmp`.
- **Tập tin sao lưu khi phát hiện hỏng hóc:** `config.json.bad`.

---

## 3. Kiến trúc Ghi Tệp Nguyên tử (Atomic Write Architecture)

Việc ghi đè trực tiếp lên một tệp cấu hình đang tồn tại tiềm ẩn rủi ro rất cao: nếu máy tính bị sập nguồn hoặc tiến trình bị ngắt đúng thời điểm đang ghi dở, tệp sẽ chỉ chứa một phần dữ liệu JSON và bị hỏng vĩnh viễn.

Để triệt tiêu rủi ro này, `ConfigStore` áp dụng mẫu thiết kế **Ghi nguyên tử (Atomic Replace Pattern)** thông qua quy trình 5 giai đoạn:

```
┌────────────────────────────────────────────────────────┐
│ Giai đoạn 1: Chuẩn bị Dữ liệu                          │
│ - Chuẩn hóa mô hình AppConfig trong bộ nhớ             │
│ - Tuần tự hóa sang chuỗi JSON (Indented, CamelCase)    │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│ Giai đoạn 2: Ghi vào Tệp Tạm (.tmp)                     │
│ - Tạo tệp config.json.tmp trong cùng thư mục           │
│ - Ghi toàn bộ dữ liệu JSON vào tệp tạm                 │
│ - Gọi lệnh FlushBuffers để bảo đảm dữ liệu đã xuống đĩa│
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│ Giai đoạn 3: Tạm dừng Giám sát Tự sinh (Self-Watch Sup)│
│ - Đặt cờ bỏ qua sự kiện FileSystemWatcher              │
│   để tránh kích hoạt vòng lặp tải lại không cần thiết   │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│ Giai đoạn 4: Hoán đổi Nguyên tử (Atomic Replace)       │
│ - Thực hiện hoán đổi tệp config.json.tmp thành         │
│   config.json với cờ ghi đè (Overwrite = true)        │
│   thao tác này là nguyên tử ở tầng NTFS               │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│ Giai đoạn 5: Dọn dẹp & Kích hoạt lại Giám sát          │
│ - Xóa tệp tạm nếu còn sót lại                          │
│ - Đặt lại cờ cho phép FileSystemWatcher hoạt động       │
└────────────────────────────────────────────────────────┘
```

---

## 4. Kiểm soát Truy cập Đồng thời & Khóa Tệp (Concurrency & Locking)

Khi người dùng chạy lệnh `fshot config --set-key value` từ PowerShell trong lúc tiến trình daemon `FShot.exe` đang chạy nền:
1. **Xung đột ghi đĩa:**
   - Cả hai tiến trình đều cố gắng ghi vào `config.json`.
   - Cơ chế xử lý: Mỗi lần mở tệp đều chỉ định chế độ chia sẻ `FileShare.ReadWrite`.
   - Nếu tệp đang bị khóa độc quyền bởi tiến trình khác, áp dụng giải thuật **Thử lại với thời gian chờ lũy thừa (Exponential Backoff Retry)**: tối đa 5 lần thử, khoảng cách giữa các lần từ 20ms đến 150ms.
2. **Đồng bộ hóa qua Named Pipe IPC:**
   - Khi lệnh CLI thực hiện thay đổi cấu hình, tiến trình CLI gửi một thông điệp IPC dạng thông báo (Notification) qua Named Pipe `FShot_Ipc_Pipe` để thông báo cho tiến trình daemon tải lại ngay lập tức mà không cần chờ bộ đếm thời gian của hệ điều hành.

---

## 5. Kiến trúc Giám sát Tự động Tải lại (Hot-Reload via FileSystemWatcher)

Để đáp ứng yêu cầu `FR-SYS-011` (tự động cập nhật khi người dùng chỉnh sửa tệp cấu hình bằng trình soạn thảo văn bản bên ngoài như Notepad, VS Code):

### 5.1 Các vấn đề kỹ thuật của FileSystemWatcher trên Windows
- Hệ điều hành Windows thường bắn nhiều sự kiện thay đổi (`Changed`) liên tiếp cho cùng một lần lưu tệp (do các thao tác ghi kích thước, cập nhật thuộc tính và sửa đổi timestamp diễn ra rời rạc).
- Bộ lọc sự kiện dễ gặp hiện tượng tệp đang bị khóa tạm thời bởi trình soạn thảo tại thời điểm sự kiện vừa được phát ra.

### 5.2 Giải pháp Kiến trúc: Bộ đệm Khử rung (Debounce Engine)

```
Sự kiện OS (Changed / Created)
            │
            ▼
┌───────────────────────────────┐
│ Kiểm tra Cờ Nội bộ            │
│ (Bỏ qua nếu chính F-Shot ghi) │
└───────────┬───────────────────┘
            │ Không phải nội bộ
            ▼
┌───────────────────────────────┐
│ Khởi động Bộ đếm Hẹn giờ      │◄─── Sự kiện mới đến sẽ Reset lại
│ (Debounce Timer: 300 ms)      │     bộ đếm về 300 ms
└───────────┬───────────────────┘
            │ Hết 300 ms không còn sự kiện mới
            ▼
┌───────────────────────────────┐
│ Nạp lại Cấu hình (LoadConfig) │
└───────────┬───────────────────┘
            │ Thành công
            ▼
┌───────────────────────────────┐
│ Phát Sự kiện Thay đổi Toàn cục│
│ (ConfigChanged Event Dispatch)│
└───────────────────────────────┘
```

---

## 6. Chiến lược Khôi phục khi Tệp Cấu hình Bị hỏng (Crash Recovery)

Nếu người dùng vô tình chỉnh sửa tệp `config.json` làm sai cú pháp JSON (ví dụ thiếu dấu ngoặc nhọn, sai kiểu dữ liệu):
1. **Phát hiện lỗi:** Bộ phân tích cú pháp JSON ném ngoại lệ phân tích cú pháp.
2. **Bảo tồn hiện trường:**
   - Hệ thống không ghi đè mất dữ liệu bị lỗi của người dùng.
   - Sao chép tệp lỗi thành `config.json.bad` để người dùng có thể mở ra kiểm tra và cứu lại các thiết lập cũ nếu muốn.
3. **Tự phục hồi an toàn:**
   - Tự động nạp bộ cấu hình mặc định an toàn (`AppConfig.Default`).
   - Ghi thông điệp cảnh báo rõ ràng vào hệ thống ghi nhật ký (Log) của F-Shot.
   - Hiển thị thông báo trên giao diện thông báo nếu đang ở chế độ đồ họa để người dùng biết cấu hình đã được khôi phục về mặc định.
