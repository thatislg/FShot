# Báo cáo thực thi tác vụ: PoC Integration Layer (Platform + Rendering + UI)

- **File Name:** `20260910_2130_Platform_Rendering_UI_PoC_Stub.md`
- **Thời gian hoàn thành:** 2026-09-10 21:30 (JST)
- **Người thực hiện:** Kimi-2.7-Code
- **Trạng thái:** Completed
- **Trace SRS / Architecture:**
  - SRS Items: `FR-CAP-01`, `FR-CAP-02`, `FR-SEL-01`, `FR-SEL-02`, `FR-CLI-01`, `FR-NF-01`
  - Layer thực thi: `FShot.Core` | `FShot.Platform.Win32` | `FShot.Rendering.Skia` | `FShot.UI` | `FShot.Core.Tests`

---

### 1. Tóm tắt công việc (Summary)

Triển khai toàn bộ pipeline PoC từ capture giả lập đến hiển thị overlay:

- Lấy thông tin màn hình và Virtual Desktop từ Win32 API.
- Chụp ảnh giả lập bằng `StubCaptureService` trả về byte array BGRA32.
- Chuyển domain types sang Skia types trong `FShot.Rendering.Skia`.
- Vẽ scene overlay gồm screenshot, dimming, selection border, handles, annotations.
- Mở cửa sổ Avalonia borderless topmost phủ toàn Virtual Screen.
- Xử lý chuột để tạo, di chuyển, co giãn vùng chọn.
- Parse CLI tối thiểu để khởi động overlay.

---

### 2. Chi tiết thay đổi mã nguồn (File Changes)

- `src/FShot.Platform.Win32/Screen/ScreenInfo.fs`: Thêm kiểu `ScreenInfo` với physical size và hit-test.
- `src/FShot.Platform.Win32/Screen/ScreenEnumeration.fs`: Triển khai `EnumDisplayMonitors`, `GetDpiForMonitor`, `getVirtualScreenBounds`.
- `src/FShot.Platform.Win32/Capture/BitmapAdapter.fs`: Tạo bitmap BGRA32 stub.
- `src/FShot.Platform.Win32/Capture/GraphicsCapture.fs`: Triển khai `StubCaptureService` theo `ICaptureService`.
- `src/FShot.Core/Geometry/Types.fs`: Thêm `Color.Red`, `Color.Green`, `Color.Blue`.
- `src/FShot.Rendering.Skia/Converters/DomainToSkia.fs`: Chuyển `Point`, `Rect`, `Color`, `CaptureResult` sang Skia.
- `src/FShot.Rendering.Skia/Renderers/SceneComposer.fs`: Vẽ overlay layers và export render.
- `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`: Custom control vẽ bằng `WriteableBitmap`, xử lý chuột/bàn phím.
- `src/FShot.UI/Windows/CaptureOverlayWindow.axaml/fs`: Cửa sổ borderless topmost, gọi capture service.
- `src/FShot.UI/App.axaml.fs`: Khởi tạo app và mở overlay.
- `src/FShot.UI/Program.fs`: CLI parser đơn giản.
- `src/FShot.UI/FShot.UI.fsproj`: Thêm `Avalonia.Skia` package.
- `src/FShot.Platform.Win32/FShot.Platform.Win32.fsproj`: Thêm `Screen` files.

---

### 3. Đánh giá tác động kiến trúc (Architectural Check)

- [x] **Domain Purity:** `FShot.Core` vẫn giữ immutable domain, không dính UI/Rendering library.
- [x] **Resource Management:** `use`/`use!` được dùng cho `SKBitmap`, `SKSurface`, `WriteableBitmap` trong render path. Cần kiểm tra kỹ hơn khi chuyển sang capture thực với COM objects.
- [x] **Contract Integrity:** `ICaptureService` giữ nguyên contract; `StubCaptureService` implement đầy đủ 3 phương thức.

---

### 4. Kết quả kiểm thử & Nghiệm thu (Acceptance & Verification)

- **Build Status:** Pass (`dotnet build FShot.sln` — 0 warning, 0 error)
- **Unit Tests:** 43 passed, 0 failed in `FShot.Core.Tests`; 1 passed, 0 failed in `FShot.Rendering.Skia.Tests` (`dotnet test`)
- **Manual Verification:**
  - [x] Chạy thử binary: `FShot.UI.exe` và `dotnet run` đều build thành công và cố gắng khởi động, nhưng môi trường hiện tại không có desktop GUI nên cửa sổ overlay không hiển thị.
  - [~] **P0.6** Mở cửa sổ borderless topmost — code đã triển khai, chưa verify trên Windows desktop.
  - [~] **P0.7** Xử lý chuột kéo vùng chọn — code đã triển khai, chưa verify trên Windows desktop.
  - [ ] **P0.8** Đo FPS ≥ 60 — chưa thực hiện, cần chạy trên Windows desktop.

---

---

### 5. Nợ kỹ thuật & Bước kế tiếp (Technical Debt & Next Action)

- **Technical Debt / Cần thay thế sau:**
  - `StubCaptureService` cần thay bằng `Windows.Graphics.Capture` thực.
  - `CaptureCanvas` hiện vẽ qua `WriteableBitmap`; cần đánh giá hiệu năng và có thể chuyển sang `Avalonia.Skia` nếu cần FPS cao hơn.
  - `SceneComposer` mới vẽ bounding box stub của annotations, chưa render chi tiết từng tool.
- **Tác vụ kế tiếp:**
  1. Chạy thử `FShot.UI.exe` trên Windows để xác nhận overlay hiển thị đúng.
  2. Triển khai `Windows.Graphics.Capture` thực thay cho stub.
  3. Tích hợp Export service (Save file + Clipboard).
