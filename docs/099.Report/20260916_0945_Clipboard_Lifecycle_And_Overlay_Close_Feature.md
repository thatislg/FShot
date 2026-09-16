# Báo cáo thực thi tác vụ: Triển khai Win32 Native Clipboard & Tự động đóng Overlay sau Export (P1.25, P1.26)

- **File Name:** `20260916_0945_Clipboard_Lifecycle_And_Overlay_Close_Feature.md`
- **Thời gian hoàn thành:** 2026-09-16 09:45 (JST)
- **Người thực hiện:** Antigravity (Google DeepMind)
- **Trạng thái:** Completed (Save As Dialog, Windows Clipboard Native CF_DIB + PNG, Auto-Close Overlay đã được nghiệm thu thực tế)
- **Trace SRS / Architecture:**
  - SRS Items: `FR-OUT-04` (Save As Fallback), `FR-OUT-05` (Windows Clipboard Format), `FR-CFG-008` (CloseAfterExport), `FR-TB-01` (Toolbar Action Buttons)
  - Layer thực thi: `FShot.Core` | `FShot.Platform.Win32` | `FShot.UI` | `FShot.Core.Tests` | `FShot.Rendering.Skia.Tests` | `FShot.UI.Tests`

---

### 1. Tóm tắt công việc (Summary)

1. **Khắc phục lỗi mất dữ liệu Clipboard khi đóng ứng dụng**:
   - **Hiện tượng trước khi sửa**: Người dùng nhấn nút Copy (hoặc phím tắt `Ctrl+C`), overlay đóng lại nhưng khi chuyển sang Zalo, Word, Paint để dán (`Ctrl+V`) thì không dán được hoặc clipboard bị rỗng.
   - **Nguyên nhân gốc rễ**: Avalonia sử dụng tầng OLE COM In-Process (`IDataObject`) để trì hoãn render. Khi `CaptureOverlayWindow` đóng, `desktop.Shutdown()` được gọi khiến tiến trình `FShot.UI.exe` kết thúc ngay lập tức. Toàn bộ con trỏ bộ nhớ của OLE data object bị hủy cùng tiến trình. Hơn nữa, việc gọi `SetDataAsync` của Avalonia đã ghi đè và xóa mất `CF_DIB` native Win32.
   - **Giải pháp**: Triển khai trực tiếp `ClipboardService.copyImageToClipboard` trong `FShot.Platform.Win32`, sử dụng `GlobalAlloc(GMEM_MOVEABLE)` để cấp phát bộ nhớ toàn cục và chuyển giao quyền sở hữu trực tiếp cho Windows OS Kernel qua `SetClipboardData`. Hỗ trợ đồng thời 2 định dạng:
     - `CF_DIB` (Format 8): DIBv5 bottom-up chuẩn cho MS Paint, Office (Word, Excel), Photoshop.
     - `PNG` (Registered Format): Giữ kênh trong suốt (Alpha channel) cho Zalo, Telegram, Discord, Chrome.
     - Gọi `OleFlushClipboard()` để dọn dẹp các handle OLE cũ.
   - **Âm thanh phản hồi (Audio feedback)**: Tích hợp `MessageBeep(0x40u)` phát âm thanh ngắn xác nhận đã copy thành công vào clipboard.

2. **Khắc phục lỗi Overlay không tự đóng sau Save / Copy**:
   - **Hiện tượng**: Sau khi Save hoặc Copy xong, màn hình vẫn bị mờ (dimmed), buộc người dùng phải nhấn `Alt+F4`.
   - **Nguyên nhân**: Trong `CaptureCanvas.ExecuteCommands`, `this.VisualRoot` chỉ là `IRenderRoot` nên mẫu khớp `| :? Window as w` thất bại. Ngoài ra, trong `OverlayState.fs`, `ExportCompleted` chỉ được khớp trong trạng thái `Selecting` hoặc `Annotating`, bỏ sót khi state chuyển sang các pha khác.
   - **Giải pháp**: 
     - Mở rộng pattern match `| _, _, ExportCompleted success ->` trong `OverlayState.fs` để luôn dispatch `CloseOverlay` nếu `Config.CloseAfterExport = true`.
     - Tìm cửa sổ thông qua `TopLevel.GetTopLevel(this) :? Window` kết hợp fallback `desktop.MainWindow`, đảm bảo gọi `w.Close()` trên UI Thread.

---

### 2. Chi tiết thay đổi mã nguồn (File Changes)

- `src/FShot.Core/State/OverlayState.fs`:
  - Chuẩn hóa pattern match `| _, _, ExportCompleted success ->` xử lý sự kiện xuất hoàn tất không phụ thuộc vào trạng thái tương tác tạm thời, phát sinh command `CloseOverlay` chuẩn xác.
- `src/FShot.Platform.Win32/Clipboard/ClipboardService.fs`:
  - Khai báo Win32 P/Invoke: `OpenClipboard`, `CloseClipboard`, `EmptyClipboard`, `SetClipboardData`, `RegisterClipboardFormatW`, `GlobalAlloc`, `GlobalLock`, `GlobalUnlock`, `GlobalFree`, `OleFlushClipboard`, `MessageBeep`.
  - Triển khai hàm `copyImageToClipboard` sinh định dạng kép: PNG byte buffer và DIB (Device Independent Bitmap với `BITMAPINFOHEADER`, 32-bit BI_RGB, lật dòng top-down sang bottom-up).
  - Triển khai hàm `playNotificationSound` kích hoạt âm thanh hệ thống `MB_ICONASTERISK`.
- `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`:
  - Sửa lệnh `CloseOverlay`: Tìm chính xác cửa sổ qua `TopLevel.GetTopLevel(this)` hoặc `desktop.MainWindow`, loại bỏ việc xóa state đột ngột gây lỗi render frame cuối.
  - Sửa `CopyToClipboard`: Loại bỏ `clipboard.SetDataAsync` của Avalonia để tránh xung đột OLE; sử dụng độc quyền Win32 native clipboard và phát âm thanh thông báo.
  - Sửa `SaveToFile`: Xử lý đóng overlay mượt mà sau khi lưu file thành công.

---

### 3. Đánh giá tác động kiến trúc & Hệ điều hành (Architectural & OS Evaluation)

- **Cơ chế Clipboard của Windows**:
  - Clipboard là tài nguyên chia sẻ của hệ điều hành (*Shared System Resource*), hoạt động theo nguyên tắc lưu giữ cho đến khi bị ghi đè (*Persist until overwritten*).
  - Thao tác `Ctrl+V` là thao tác **Đọc (Read)**, không xóa nội dung. Người dùng có thể dán ảnh nhiều lần vào nhiều ứng dụng khác nhau mà không lo mất.
- **Tính khả chuyển đa nền tảng (Cross-platform Strategy)**:
  - Tầng `FShot.Core` và `FShot.Rendering.Skia` hoàn toàn độc lập với Win32 API.
  - Mã Win32 native được đóng gói biệt lập trong `FShot.Platform.Win32`.
  - Khi mở rộng sang Linux/macOS trong tương lai, chỉ cần triển khai thêm `FShot.Platform.Linux` (sử dụng `xclip` cho X11 hoặc `wl-copy` cho Wayland D-Bus portal) mà không cần can thiệp vào logic lõi của Canvas hay Engine vẽ SkiaSharp.

---

### 4. Kết quả kiểm thử & Nghiệm thu (Acceptance & Verification)

- **Build Status:** Pass (`dotnet build`) - 0 errors, 0 warnings.
- **Unit Tests:** 204 passed, 0 failed (`dotnet test`).
- **Nghiệm thu thực tế (User Verified):**
  - [x] Nhấn nút Copy (hoặc `Ctrl+C`), hệ thống phát âm thanh thông báo ngắn và tự động đóng màn hình overlay ngay lập tức.
  - [x] Mở Paint, Word, Zalo hoặc trình duyệt web, nhấn `Ctrl+V`: Ảnh dán ra lập tức, đầy đủ chi tiết, đúng vùng crop và rõ nét.
  - [x] Nhấn nút Save (hoặc `Ctrl+S`), chọn thư mục và lưu: File ảnh được tạo thành công và overlay tự động đóng.

---

### 5. Kế hoạch tiếp theo (Next Action)

- Chuyển sang **P1.27**: CLI Parser bằng Argu (`fshot gui`, `fshot full`, `fshot screen`) và cấu hình đường dẫn dòng lệnh.
