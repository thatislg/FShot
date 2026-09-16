# Báo cáo thực thi tác vụ: Sửa lỗi Save/Export & Triển khai Win32 Real Screen Capture

- **File Name:** `20260916_0915_Platform_Capture_Export_Save_Feature.md`
- **Thời gian hoàn thành:** 2026-09-16 09:15 (JST)
- **Người thực hiện:** Antigravity (Google DeepMind)
- **Trạng thái:** Completed (Save & Real Screen Capture verified)
- **Trace SRS / Architecture:**
  - SRS Items: `FR-OUT-01`, `FR-OUT-02`, `FR-OUT-04`, `FR-CAP-01`, `FR-CAP-02`
  - Layer thực thi: `FShot.Core` | `FShot.Rendering.Skia` | `FShot.Platform.Win32` | `FShot.UI` | `FShot.Rendering.Skia.Tests`

---

### 1. Tóm tắt công việc (Summary)
- **Sửa lỗi crash khi xuất ảnh (Save/Export)**:
  - Phát hiện và loại bỏ lệnh lỗi `bitmap.SetPixels(IntPtr.Zero)` trong `DomainToSkia.captureResultToBitmap`. Lệnh này trước đó đã xóa con trỏ bộ nhớ của `SKBitmap`, gây lỗi `ArgumentNullException: Value cannot be null (Parameter 'image')` tại `SceneComposer.renderExport`, khiến mọi thao tác xuất ảnh (Save, Copy) bị văng ngoại lệ trước khi mở hộp thoại lưu file.
- **Triển khai chụp màn hình thật (Real Screen Capture)**:
  - Thay thế `StubCaptureService` (trước đây chỉ trả về mảng màu xám/đen giả lập) bằng `WindowsCaptureService` sử dụng Win32 GDI API chuẩn (`BitBlt` với `SRCCOPY | CAPTUREBLT` và `GetDIBits`).
  - Chụp chuẩn xác toàn bộ màn hình hoặc đa màn hình (Virtual Screen, ví dụ 3840x1200).
  - Đổi thứ tự mở overlay: Chụp màn hình thật **trước khi** hiển thị cửa sổ overlay để tránh hiện tượng cửa sổ tự đè lên ảnh chụp và loại bỏ hoàn toàn độ trễ 150ms.
- **Kiểm thử & Bổ sung Test Regression**:
  - Viết 2 unit tests độc lập cho `captureResultToBitmap` và `SceneComposer.renderExport`.
  - Toàn bộ 204/204 unit tests đều đạt (Core: 179, Rendering.Skia: 22, UI: 3).
  - Người dùng đã nghiệm thu thực tế: Tính năng lưu file (Save) và chụp màn hình thật hoạt động chính xác.

---

### 2. Chi tiết thay đổi mã nguồn (File Changes)
- `src/FShot.Rendering.Skia/Converters/DomainToSkia.fs`:
  - Loại bỏ lệnh `SetPixels(IntPtr.Zero)` làm mất con trỏ pixel.
  - Sử dụng `Marshal.Copy` an toàn với kiểm tra mảng byte và kích thước `min captureResult.Pixels.Length captureResult.TotalBytes`.
- `src/FShot.Rendering.Skia/Renderers/SceneComposer.fs`:
  - Bổ sung `max 1` cho chiều rộng và chiều cao của vùng cắt `physicalSelection` để phòng ngừa lỗi kích thước 0.
- `src/FShot.Platform.Win32/Capture/GraphicsCapture.fs`:
  - Thêm `WindowsCaptureService` thực hiện chụp màn hình Win32 GDI với đầy đủ các hàm Win32 P/Invoke (`GetDC`, `CreateCompatibleDC`, `CreateCompatibleBitmap`, `BitBlt`, `GetDIBits`, `GetSystemMetrics`, `GetCursorPos`).
  - Đảm bảo quyền truy cập window station/desktop (`winsta0\default`).
  - Giữ lại `StubCaptureService` phục vụ kiểm thử và fallback.
- `src/FShot.UI/Windows/CaptureOverlayWindow.axaml.fs`:
  - Gọi `WindowsCaptureService.CaptureVirtualScreenAsync()` trước khi cửa sổ overlay được show, đảm bảo chụp được màn hình sạch hoàn toàn.
- `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`:
  - Chuẩn hóa tọa độ hit-test toolbar bằng hàm dùng chung `Toolbar.itemRect`.
  - Hỗ trợ phím tắt `Ctrl+S` và `Ctrl+C` trong `OnKeyDown`.
  - Bổ sung log chẩn đoán chi tiết cho toolbar click và quá trình export.
- `tests/FShot.Rendering.Skia.Tests/Tests.fs`:
  - Bổ sung 2 test case: `captureResultToBitmap allocates pixel memory and copies bytes` và `SceneComposer renderExport crops correctly and does not throw ArgumentNullException`.

---

### 3. Đánh giá tác động kiến trúc (Architectural Check)
- [x] **Domain Purity:** `FShot.Core` hoàn toàn thuần F#, không phụ thuộc thư viện đồ họa hay UI nào.
- [x] **Resource Management:** Các tài nguyên Win32 GDI Handles (`HDC`, `HBITMAP`, `hDesktop`, `hWinSta`) và Skia unmanaged objects (`SKBitmap`, `SKSurface`) đều được giải phóng hoàn toàn qua `use` và khối `finally`.
- [x] **Contract Integrity:** Giữ nguyên contract `ICaptureService`, `CaptureResult`, `Selection`.

---

### 4. Kết quả kiểm thử & Nghiệm thu (Acceptance & Verification)
- **Build Status:** Pass (`dotnet build`) - 0 errors, 0 warnings.
- **Unit Tests:** 204 passed, 0 failed (`dotnet test`).
- **Manual Verification:**
  - [x] Chụp màn hình thật hiển thị ngay khi mở ứng dụng.
  - [x] Vùng chọn và các công cụ vẽ annotation hoạt động bình thường trên nền ảnh chụp thật.
  - [x] Bấm nút Save hoặc `Ctrl+S` mở Save File Picker Dialog của Windows.
  - [x] File ảnh `.png` / `.jpg` lưu ra đúng vùng chọn cùng các nét vẽ annotation, hình ảnh rõ nét.

---

### 5. Nợ kỹ thuật & Bước kế tiếp (Technical Debt & Next Action)
- **Vấn đề tồn đọng:**
  - Tính năng Copy vào Clipboard chưa hoạt động theo kỳ vọng của người dùng (chờ người dùng mô tả cụ thể triệu chứng để xử lý tiếp).
- **Tác vụ kế tiếp:**
  - Điều tra và xử lý dứt điểm tính năng Copy vào Clipboard (`Avalonia.Input.Clipboard` / Win32 Clipboard native format).
