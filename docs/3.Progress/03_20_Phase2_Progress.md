# F-Shot Progress — Phase 2: Windows v1.0

- **Target:** 11/10 → 07/11/2026
- **Trạng thái:** PENDING (0 / 39 tasks)
- **File gốc:** Checklist chi tiết thuộc `docs/3.Progress/03_00_Progres_Overview.md`

---

## Checklist chi tiết Phase 2: Windows v1.0

### Epic 6: System Tray & App Lifecycle
- [x] **P2.01** Biểu tượng tray liên tục với menu ngữ cảnh: chụp GUI, chụp màn hình, mở cài đặt, mở thư mục lưu, thoát (`FR-SYS-001`–`FR-SYS-008`).
  - [x] Hoàn thiện tài liệu thiết kế chi tiết `docs/2.Design/10_Platform_Win32/10_05_TrayIcon.md` (vòng đời Tray, cơ chế Avalonia NativeMenuItem/TrayIcon kết hợp Win32 WinProc, xử lý sự kiện click).
  - [x] Chuẩn bị và tích hợp icon tray chuyên dụng từ vector Kawaii `docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/tray-icon.svg` (chuyển đổi sang WindowIcon / icon định dạng thích hợp cho khay hệ thống Windows Taskbar).
  - [x] Triển khai `TrayService` trong `src/FShot.Platform.Win32/Tray/TrayIcon.fs` và tích hợp vào `App.axaml` / `App.axaml.fs`:
    - [x] `FR-SYS-001`: Khởi tạo biểu tượng thường trực trên khay hệ thống khi khởi động ứng dụng ở chế độ background/daemon.
    - [x] Thao tác Click / Double-click chuột trái vào TrayIcon: kích hoạt chụp ảnh màn hình tương tác GUI ngay lập tức.
    - [x] `FR-SYS-002`: Menu ngữ cảnh "Chụp màn hình (GUI)" kích hoạt `CaptureOverlayWindow` phủ toàn Virtual Screen.
    - [x] `FR-SYS-003`: Submenu "Chụp theo màn hình" tự động cập nhật danh sách màn hình từ `ScreenEnumeration.getScreens()`. Khi chọn màn hình, `CaptureOverlayWindow` chỉ phủ đúng màn hình đó, màn hình khác vẫn sáng bình thường, cho phép chọn region và save/copy.
    - [x] `FR-SYS-004`: Menu "Trình phóng nhanh (Launcher)" hỗ trợ chụp với độ trễ hoặc tùy chọn nhanh.
    - [x] `FR-SYS-005`: Menu "Thông tin & Phím tắt (About)" hiển thị dialog giới thiệu phiên bản F-Shot và cheat sheet phím tắt.
    - [x] `FR-SYS-006`: Menu "Cài đặt (Settings)" mở cửa sổ cấu hình hoặc điều hướng nhanh tới file cấu hình.
    - [x] `FR-SYS-007`: Menu "Mở thư mục ảnh chụp" mở đường dẫn `savePath` trong Windows Explorer qua `Process.Start("explorer.exe", path)`.
    - [x] `FR-SYS-008`: Menu "Thoát F-Shot" kích hoạt luồng đóng ứng dụng sạch sẽ.
  - [x] Viết unit tests kiểm thử khởi tạo menu, trạng thái hiển thị và dispatch command từ tray.
  - [x] Verify runtime trên Windows 10/11: kiểm tra biểu tượng hiển thị rõ nét trên Taskbar (cả light/dark theme), chuột phải mở menu nhạy, click từng action hoạt động chính xác, **và ứng dụng khởi động nền không tự mở overlay (chỉ mở khi click tray)**.
- [x] **P2.02** Giới hạn single-instance và khởi động cùng Windows (`FR-SYS-010`, `FR-CFG-006`, `FR-WIN-005`).
  - [x] Hoàn thiện tài liệu thiết kế `docs/2.Design/10_Platform_Win32/10_09_Startup.md` và `docs/2.Design/10_Platform_Win32/10_10_SingleInstance.md`.
  - [x] Triển khai cơ chế kiểm soát tiến trình duy nhất (Single-Instance Enforcement) trong `src/FShot.Platform.Win32/Lifecycle/SingleInstance.fs`:
    - [x] Dùng `System.Threading.Mutex` toàn cục (`Global\FShot_SingleInstance_Mutex`) để nhận diện instance đầu tiên đang chạy.
    - [x] Thiết lập kênh giao tiếp liên tiến trình qua `NamedPipeServerStream` (`FShot_Ipc_Pipe`): instance nền lắng nghe các lệnh từ instance mới.
    - [x] Khi người dùng chạy tiếp lệnh (ví dụ `fshot gui` hoặc click icon shortcut), instance thứ hai đóng vai trò `NamedPipeClientStream`, gửi tham số chụp sang instance đang chạy rồi tự thoát ngay lập tức (`FR-SYS-010`).
    - [x] Bổ sung cờ CLI `--allow-multiple` để bỏ qua kiểm tra single-instance khi cần debug hoặc chạy kiểm thử song song.
  - [x] Triển khai đăng ký tự khởi động cùng Windows (`StartupLaunch`) trong `src/FShot.Platform.Win32/Startup/StartupRegistration.fs`:
    - [x] Quản lý ghi/xóa Registry key `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` với tên value `"FShot"`.
    - [x] Hàm `isStartupEnabled() : bool`, `setStartup(enable: bool) : Result<unit, string>`.
    - [x] Đảm bảo đường dẫn thực thi trỏ chuẩn xác tới `FShot.exe` (không cần cờ đặc biệt vì chế độ mặc định đã là daemon).
    - [x] Đồng bộ hai chiều với trường cấu hình `StartupLaunch: bool` trong `AppConfig` (`FR-CFG-006`, `FR-WIN-005`).
  - [x] Bổ sung unit tests cho SingleInstance message parsing, IPC serialization và logic kiểm tra Registry.
  - [ ] Verify runtime: bật FShot, thử mở thêm instance từ PowerShell/CMD xác nhận lệnh được chuyển tiếp; kiểm tra Registry trong `regedit` khi bật/tắt tùy chọn khởi động cùng Windows.
- [x] **P2.03** Thoát graceful, thông báo thành công / hủy, tùy chọn ẩn tray icon (`FR-CFG-007`, `FR-CFG-008`).
  - [x] Triển khai quản lý vòng đời ứng dụng và giải phóng tài nguyên tập trung (`AppLifecycle` / `Dispose` pattern):
    - [x] Đăng ký bắt các sự kiện hệ thống `AppDomain.CurrentDomain.ProcessExit`, `Console.CancelKeyPress`.
    - [x] Đảm bảo giải phóng toàn bộ unmanaged resources khi thoát: đóng Mutex (`App.SingleInstanceMutex`), ngắt Named Pipe Server (`App.IpcCancellation`), gỡ TrayIcon khỏi Taskbar để tránh icon bị "treo mờ" (ghost tray icon).
  - [x] Triển khai dịch vụ thông báo desktop (`NotificationService`) trong `src/FShot.UI/Services/` dưới dạng **Avalonia Notification Window** thay cho Win32 Toast/Balloon API không ổn định:
    - [x] Cửa sổ thông báo nhỏ (320x80), không viền, `Topmost`, không hiện trên Taskbar, đặt ở góc dưới phải màn hình chính.
    - [x] Tự động đóng sau ~4 giây; click để đóng; click khi có đường dẫn save sẽ mở Explorer highlight file.
    - [x] Nền đặc `#FF222222`, viền trắng mờ `#55FFFFFF`, chữ trắng/xám — nhìn rõ trên mọi nền.
    - [x] `FR-CFG-008`: Tôn trọng cấu hình `showDesktopNotification` (thông báo khi copy/save thành công kèm tên file) và `showAbortNotification` (thông báo khi hủy thao tác chụp).
    - [x] CaptureCanvas phát sự kiện `CaptureCanvasEvents.ExportCompleted` (Saved/Copied/Failed), App subscribe để gọi `NotificationService.ShowNotification`.
    - [x] Click/selection từ CaptureCanvas khi hủy (Esc) phát sự kiện `CaptureCanvasEvents.AbortRequested`, App subscribe để hiển thị thông báo hủy trong daemon mode.
    - [x] Bổ sung helper `HighlightFileInExplorer(filePath)` để mở và highlight file từ thông báo.
  - [x] Loại bỏ implementation Win32 Balloon/Toast phức tạp (`RegisterClassEx`, `WNDCLASSEX`, `Shell_NotifyIcon`, `Microsoft.Toolkit.Uwp.Notifications`) khỏi `src/FShot.Platform.Win32/Notifications/NotificationService.fs`; giữ lại module helper `highlightFileInExplorer`.
  - [x] `FR-CFG-007`: Hỗ trợ tùy chọn ẩn hoàn toàn tray icon (`disabledTrayIcon = true`):
    - [x] Ứng dụng vẫn chạy nền và lắng nghe phím nóng toàn cục (sẵn sàng cho Epic 7) mà không xuất hiện icon ở Taskbar tray.
    - [x] Đảm bảo có cảnh báo log và hướng dẫn chỉnh sửa `config.json` khi đã ẩn tray icon.
  - [x] Viết unit tests cho notification payload builder (`NotificationServiceTests.fs`) và cấu hình hiển thị.
  - [x] Verify runtime trên Windows: xác nhận notification window hiển thị khi copy/save từ tray menu (màn 1, màn 2, capture full); thoát app từ menu tray xác nhận tiến trình tắt sạch và không để lại icon mờ.

### Epic 7: Global Hotkeys
- [x] **P2.04** Đăng ký phím nóng toàn hệ thống để kích hoạt chụp (`FR-SYS-009`, `FR-WIN-002`, `FR-SH-028`).
  - [x] **Quyết định phím tắt:** dùng thẳng `PrintScreen` (thay vì `Win+Shift+X`) theo yêu cầu thực tế — bắt qua low-level keyboard hook (`WH_KEYBOARD_LL`) vì `RegisterHotKey` với `VK_SNAPSHOT` hay bị Windows 11 chặn.
  - [x] Triển khai Win32 P/Invoke trong `src/FShot.Platform.Win32/Hotkeys/GlobalHotkey.fs`:
    - [x] Khai báo `SetWindowsHookExW`, `UnhookWindowsHookEx`, `CallNextHookEx`, `GetModuleHandleW`, `GetMessageW`, `TranslateMessage`, `DispatchMessageW`, `PostThreadMessageW`, `GetCurrentThreadId`.
    - [x] Tạo message loop trên thread riêng (background) để OS gọi về low-level hook.
    - [x] Cơ chế điều phối an toàn sang Avalonia UI Thread (`Dispatcher.UIThread.InvokeAsync`).
    - [x] Chống auto-repeat: chỉ kích hoạt một lần mỗi lần nhấn, reset khi nhả phím.
  - [x] Bổ sung hằng `HotkeyModifiers` (Alt/Control/Shift/Win/NoRepeat) cho P2.06.
  - [x] Viết unit tests cho SnippingTool registry, cờ modifier và VK code (`GlobalHotkeyTests.fs`).
  - [ ] Verify runtime: nhấn `PrintScreen` từ mọi ứng dụng bên ngoài (Edge, VS Code, game cửa sổ, Desktop) xác nhận overlay FShot được kích hoạt lập tức.
- [x] **P2.05** Tích hợp phím `PrintScreen` và xử lý xung đột với Windows Snipping Tool (`FR-SYS-020`, `FR-WIN-003`, `FR-SH-030`).
  - [x] Triển khai bắt phím `PrintScreen` (`VK_SNAPSHOT = 0x2C`) qua low-level keyboard hook; nuốt phím (trả 1) để ngăn app khác nhận.
  - [x] Xử lý xung đột với Windows Snipping Tool: module `SnippingTool.setPrintScreenRedirect false` ghi `HKCU\Control Panel\Keyboard\PrintScreenKeyForSnippingEnabled = 0` (FR-SYS-020, FR-WIN-003).
  - [x] Đảm bảo gọi `UnhookWindowsHookEx` trong cleanup khi thoát ứng dụng.
  - [x] Bổ sung unit tests cho logic registry Snipping Tool.
  - [ ] Verify runtime: gõ phím `PrtSc` trên bàn phím vật lý trên Windows 11; xác nhận FShot chiếm quyền chụp thay vì mở Windows Snipping Tool.
- [x] **P2.06** Cho phép cấu hình và thay đổi các phím tắt toàn cục (`FR-SH-028`–`FR-SH-030`).
  - [x] Mở rộng mô hình cấu hình phím nóng trong `src/FShot.Core/Domain/Config.fs`:
    - [x] Định nghĩa `HotkeyAction` (`CaptureGui`, `CaptureFullScreen`, `CaptureScreenAtCursor`) và `HotkeyConfig` `{ Action; Key; Enabled }` (modifiers gộp vào chuỗi `Key` như `"Win+Shift+X"`).
    - [x] Mặc định: `CaptureGui` = `"PrintScreen"` (bật), 2 hành động còn lại tắt.
    - [x] JSON lưu dạng object `"hotkeys": { "captureGui": "PrintScreen", ... }`; rỗng = tắt hành động.
  - [x] Triển khai `HotkeyParser` trong `src/FShot.Platform.Win32/Hotkeys/HotkeyParser.fs`:
    - [x] Parse chuỗi (`"PrintScreen"`, `"Win+Shift+X"`, `"Ctrl+Alt+S"`, `"F11"`) thành modifiers + Virtual Key; không phân biệt hoa thường, bỏ qua khoảng trắng.
    - [x] Format ngược từ mã phím thành chuỗi chuẩn hóa (thứ tự Win/Ctrl/Alt/Shift).
  - [x] Dynamic Rebinding: `FileSystemWatcher` theo dõi `config.json`, khi thay đổi tự động hủy đăng ký cũ và đăng ký mới mà không cần restart (qua `WM_APP_REBIND` trên thread message loop).
  - [x] Validation: chặn tổ hợp phím nguy hiểm của OS (`Ctrl+Alt+Delete`, `Win+L`, `Alt+Tab`, `Win+D`, ...) và cảnh báo phím không hợp lệ.
  - [ ] Phát hiện trùng lặp giữa các hành động trong chính F-Shot (chưa làm — nợ nhỏ).
  - [x] Viết unit tests toàn diện cho `HotkeyParser` (11 test: parse hợp lệ, phím không hợp lệ, hoa thường, modifiers, format, dangerous).
  - [ ] Verify runtime: thay đổi phím nóng trong `config.json`, xác nhận phím mới có hiệu lực ngay lập tức.

### Epic 8: Real Capture Backend & Mixed DPI

> **Tài liệu thiết kế & Tham khảo:**
> - [02_05_WindowsGraphicsCapture.md](file:///d:/Kojin/FShot/docs/2.Design/02_Capture/02_05_WindowsGraphicsCapture.md): Thiết kế backend WinRT WGC, FramePool 1-frame, Staging texture và ghép nối đa màn hình Mixed-DPI.
> - [02_06_FallbackBitBlt.md](file:///d:/Kojin/FShot/docs/2.Design/02_Capture/02_06_FallbackBitBlt.md): Thiết kế cơ chế dự phòng GDI BitBlt, ma trận quyết định tự động chuyển đổi backend (Auto-fallback).
> - [10_02_CaptureAdapter.md](file:///d:/Kojin/FShot/docs/2.Design/10_Platform_Win32/10_02_CaptureAdapter.md): Kiến trúc CompositeCaptureService, quản lý vòng đời tài nguyên Direct3D / GDI, ánh xạ mã lỗi sang domain.
> - [01_07_DpiAndScaling.md](file:///d:/Kojin/FShot/docs/2.Design/01_Geometry/01_07_DpiAndScaling.md): Xử lý Mixed DPI, hệ tọa độ Virtual Screen và chuyển đổi logical/physical pixels.
> - [02_07_ScreenEnumeration.md](file:///d:/Kojin/FShot/docs/2.Design/02_Capture/02_07_ScreenEnumeration.md): Liệt kê màn hình, lấy HMONITOR và DPI per-monitor.
> - [003_SRS.md](file:///d:/Kojin/FShot/docs/1.Investigation/003_SRS.md): Yêu cầu kỹ thuật `FR-WIN-001`, `FR-SYS-017`, `FR-SYS-018`, `FR-CAP-01`, `FR-CAP-02`.
> - [20260916_0915_Platform_Capture_Export_Save_Feature.md](file:///d:/Kojin/FShot/docs/099.Report/20260916_0915_Platform_Capture_Export_Save_Feature.md): Báo cáo triển khai Win32 GDI BitBlt nền tảng từ Phase 1 (P1.24).

- [x] **P2.07** Triển khai backend `Windows.Graphics.Capture` cho chụp đa màn hình đúng mixed-DPI (`FR-WIN-001`).
  - [x] Hoàn thiện tài liệu thiết kế chi tiết [02_05_WindowsGraphicsCapture.md](file:///d:/Kojin/FShot/docs/2.Design/02_Capture/02_05_WindowsGraphicsCapture.md) (COM interop, Direct3D11 device, FramePool, staging texture, multi-monitor composition).
  - [x] Hoàn thiện tài liệu thiết kế bộ điều phối [10_02_CaptureAdapter.md](file:///d:/Kojin/FShot/docs/2.Design/10_Platform_Win32/10_02_CaptureAdapter.md) (CompositeCaptureService, vòng đời tài nguyên Direct3D & GDI, ánh xạ lỗi sang domain).
  - [x] Khai báo COM Interop và WinRT Native bindings (`IGraphicsCaptureItemInterop`, `IDirect3DDevice`, `IDXGISurface`, Direct3D 11 APIs: `D3D11CreateDevice`, `ID3D11Texture2D`, `ID3D11DeviceContext`) trong [Direct3DInterop.fs](file:///d:/Kojin/FShot/src/FShot.Platform.Win32/Capture/Direct3DInterop.fs).
  - [x] Khởi tạo Direct3D 11 hardware device (`D3D_DRIVER_TYPE_HARDWARE`) và bọc sang WinRT `IDirect3DDevice` qua `CreateDirect3D11DeviceFromDXGIDevice`.
  - [x] Triển khai `WgcCaptureService` trong `src/FShot.Platform.Win32/Capture/WgcCaptureService.fs`:
    - [x] Tạo `GraphicsCaptureItem` cho từng màn hình thông qua `IGraphicsCaptureItemInterop.CreateForMonitor(hMonitor)`.
    - [x] Khởi tạo `Direct3D11CaptureFramePool.CreateFreeThreaded` với định dạng `DirectXPixelFormat.B8G8R8A8UIntNormalized` và buffer 1 frame.
    - [x] Cấu hình `GraphicsCaptureSession`: tắt con trỏ chuột (`IsCursorCaptureEnabled = false`) và tắt đường viền vàng chụp (`IsBorderRequired = false` trên Windows 11 Build 22000+).
    - [x] Bắt frame đơn (`Direct3D11CaptureFrame`), sao chép sang Staging Texture (với cờ `D3D11_USAGE_STAGING`, `D3D11_CPU_ACCESS_READ`).
    - [x] Map Staging Texture bằng `ID3D11DeviceContext.Map` để đọc raw byte array BGRA32 và giải phóng tài nguyên unmanaged ngay sau khi đọc.
  - [x] Ghép nối toàn bộ Virtual Screen:
    - [x] Chụp độc lập từng monitor qua session tương ứng để thu được ảnh gốc ở đúng độ phân giải physical native của màn hình đó.
    - [x] Tạo Virtual Canvas tổng hợp và composite (vẽ) từng bitmap màn hình vào đúng tọa độ `VirtualBounds` (chuẩn hóa scale logical).
  - [x] Viết unit tests kiểm thử khởi tạo buffer (đã thêm `CaptureBackendTests.fs`, chưa phủ hết test kích thước buffer), tính toán kích thước physical và đóng gói `CaptureResult`.
  - [ ] Verify runtime: chụp màn hình trên máy thật Windows 10/11, xác nhận không xuất hiện viền vàng, hiệu năng bắt frame tức thì (<50ms).
- [x] **P2.08** Hỗ trợ chụp một màn hình cụ thể và màn hình có con trỏ qua WinRT.
  - [x] Triển khai hàm `CaptureScreenAsync(screenIndex: int)`:
    - [x] Tra cứu `HMONITOR` từ [ScreenEnumeration.fs](file:///d:/Kojin/FShot/src/FShot.Platform.Win32/Screen/ScreenEnumeration.fs).
    - [x] Tạo `GraphicsCaptureItem` chỉ cho monitor chỉ định và chụp ảnh ở độ phân giải Native physical của monitor đó.
    - [x] Đóng gói `CaptureResult` với `VirtualBounds` và `ScaleFactor` tương ứng của màn hình.
  - [x] Triển khai hàm `CaptureCursorScreenAsync()`:
    - [x] Đọc vị trí con trỏ chuột hiện tại qua `GetCursorPos`.
    - [x] Dùng `MonitorFromPoint` để xác định chính xác màn hình đang chứa con trỏ.
    - [x] Kích hoạt luồng WinRT capture cho màn hình đó và trả về `CaptureResult`.
  - [x] Tích hợp vào menu Tray "Chụp theo màn hình" trong [TrayIcon.fs](file:///d:/Kojin/FShot/src/FShot.Platform.Win32/Tray/TrayIcon.fs) và lệnh CLI `fshot screen -n <index>`.
  - [x] Viết unit tests kiểm thử mapping `screenIndex` sang monitor item và xử lý khi `screenIndex` không tồn tại.
  - [ ] Verify runtime: thử chụp từng màn hình riêng lẻ trên thiết lập 2 màn hình; thử rê chuột sang màn phụ rồi kích hoạt chụp cursor screen.
- [x] **P2.09** Fallback `BitBlt` khi `Windows.Graphics.Capture` không khả dụng hoặc bị từ chối quyền (`FR-WIN-001`, fallback).
  - [x] *(Đã hoàn thành nền tảng từ P1.24)* Triển khai `WindowsCaptureService` bằng Win32 GDI `BitBlt` + `GetDIBits` (`SRCCOPY ||| CAPTUREBLT`) trong [GraphicsCapture.fs](file:///d:/Kojin/FShot/src/FShot.Platform.Win32/Capture/GraphicsCapture.fs) hỗ trợ `CaptureVirtualScreenAsync`, `CaptureScreenAsync`, `CaptureCursorScreenAsync`.
  - [x] Hoàn thiện tài liệu thiết kế cơ chế dự phòng [02_06_FallbackBitBlt.md](file:///d:/Kojin/FShot/docs/2.Design/02_Capture/02_06_FallbackBitBlt.md) (ma trận quyết định tự động chuyển đổi backend, đảm bảo desktop station winsta0\default).
  - [x] Triển khai bộ điều phối tự động `CompositeCaptureService` / Factory pattern:
    - [x] Kiểm tra điều kiện hỗ trợ WinRT: Windows build $\ge$ 18362 và `GraphicsCaptureSession.IsSupported()`.
    - [x] Cơ chế Try-Catch an toàn: nếu WGC ném ngoại lệ (không có quyền chụp, GPU driver crash, headless VM, remote desktop RDP), tự động ghi warning log và fallback sang GDI `BitBlt`.
  - [x] Bổ sung trường cấu hình `CaptureBackend: string` ("Auto" | "Wgc" | "Gdi") trong [Config.fs](file:///d:/Kojin/FShot/src/FShot.Core/Domain/Config.fs) và cờ dòng lệnh CLI `--backend <type>` (`--force-gdi`) cho phép người dùng chủ động ép dùng GDI BitBlt khi cần.
  - [x] Viết unit tests kiểm thử logic fallback (một phần qua `CaptureBackendTests.fs`, fallback đầy đủ là runtime) khi WGC trả về lỗi hoặc khi hệ thống không hỗ trợ.
  - [ ] Verify runtime: giả lập môi trường chặn WGC (hoặc cấu hình `--force-gdi`), xác nhận F-Shot vẫn chụp màn hình bình thường qua GDI.
- [x] **P2.10** Xử lý đúng Mixed DPI và Per-Monitor V2 DPI Awareness cho cả chụp và UI (`FR-SYS-017`, `FR-SYS-018`, `FR-WIN-001`).
  - [x] Cấu hình manifest ứng dụng: Bổ sung thẻ `<dpiAwareness>PerMonitorV2, unaware</dpiAwareness>` trong `app.manifest` của UI project để OS không tự ý scale bitmap hay cửa sổ overlay.
  - [x] Hoàn thiện [ScreenEnumeration.fs](file:///d:/Kojin/FShot/src/FShot.Platform.Win32/Screen/ScreenEnumeration.fs):
    - [x] Gọi `GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI)` để lấy chính xác DPI trục ngang và trục dọc của từng màn hình.
    - [x] Tính `ScaleFactor = dpiX / 96.0` độc lập cho từng monitor (ví dụ: màn chính 150%, màn phụ 100%).
  - [x] Xử lý chuyển đổi tọa độ Mixed DPI trong `OverlayState` và `CaptureCanvas`:
    - [x] Chuẩn hóa toàn bộ trạng thái vùng chọn, điểm neo (handles), và nét vẽ theo hệ tọa độ Logical Virtual Screen.
    - [ ] Khi crop xuất ảnh (Export) hoặc vẽ preview: xác định vùng chọn nằm trên màn hình nào để áp dụng đúng ma trận biến đổi tỉ lệ (Transform Matrix / Scaling) tương ứng của màn hình đó.
    - [ ] Xử lý trường hợp vùng chọn kéo bắc cầu (cross-monitor selection) qua cả 2 màn hình có DPI khác nhau — ghép nối ảnh physical không bị biến dạng tỉ lệ (stretching/tearing).
  - [x] Bổ sung unit tests cho các trường hợp chuyển đổi tọa độ logical (một phần) $\leftrightarrow$ physical trên môi trường 2 màn hình có scale khác nhau (100% + 125%, 100% + 150%).
  - [ ] Verify runtime trên hệ thống thực tế có Mixed DPI: cắm màn hình 4K (150%) cạnh màn hình Full HD (100%), xác nhận ảnh chụp không bị nhòe, con trỏ chuột và vùng chọn khớp từng pixel.

### Epic 9: Config Persistence & Editor

> **Tài liệu thiết kế & Tham khảo:**
> - [07_01_Overview.md](file:///d:/Kojin/FShot/docs/2.Design/07_Config/07_01_Overview.md): Tổng quan kiến trúc cấu hình người dùng F-Shot.
> - [07_02_Schema.md](file:///d:/Kojin/FShot/docs/2.Design/07_Config/07_02_Schema.md): Đặc tả Schema cấu hình JSON, giá trị mặc định và quy tắc kiểm tra hợp lệ.
> - [07_04_FileStore.md](file:///d:/Kojin/FShot/docs/2.Design/07_Config/07_04_FileStore.md): Cơ chế nạp (Load), ghi (Save) và tích hợp cấu hình trong vòng đời ứng dụng.
> - [10_08_ConfigStore.md](file:///d:/Kojin/FShot/docs/2.Design/10_Platform_Win32/10_08_ConfigStore.md): Thiết kế tầng lưu trữ Win32, ghi tệp nguyên tử (.tmp replace), quản lý khóa tệp đồng thời, hot-reload qua FileSystemWatcher và tự phục hồi khi tệp hỏng.
> - [07_05_Migration.md](file:///d:/Kojin/FShot/docs/2.Design/07_Config/07_05_Migration.md): Thiết kế bộ chuyển đổi cấu hình từ `flameshot.ini`, bảng ánh xạ 19 thuộc tính, chuẩn hóa màu sắc Qt.
> - [11_06_ConfigWindow.md](file:///d:/Kojin/FShot/docs/2.Design/11_UI_Avalonia/11_06_ConfigWindow.md): Thiết kế giao diện Cửa sổ Cài đặt (Settings Window) 4 Tab và component FileNameEditor.
> - [003_SRS.md](file:///d:/Kojin/FShot/docs/1.Investigation/003_SRS.md): Yêu cầu kỹ thuật `FR-CFG-001`–`FR-CFG-209`, `FR-WIN-006`, `FR-SYS-006`, `FR-SYS-011`, `FR-SYS-013`, `FR-SYS-014`.
> - [20260924_1330_P1.29_Json_Config_Persistence.md](file:///d:/Kojin/FShot/docs/099.Report/20260924_1330_P1.29_Json_Config_Persistence.md): Báo cáo triển khai lưu trữ và nạp cấu hình JSON tại `%APPDATA%\FShot\config.json` từ Phase 1 (P1.29).

- [x] **P2.11** Đọc/ghi cấu hình dạng JSON tại `%APPDATA%\FShot\config.json` (`FR-CFG-001`, `FR-CFG-003`–`FR-CFG-005`).
  - [x] *(Đã hoàn thành nền tảng từ P1.29)* Triển khai nền tảng `ConfigStore` đọc/ghi `%APPDATA%\FShot\config.json` với các trường MVP (`SavePath`, `FilenamePattern`, `DrawColor`, `DrawThickness`, `DefaultTool`, `CloseAfterExport`).
  - [x] Hoàn thiện tài liệu thiết kế lưu trữ Win32 [10_08_ConfigStore.md](file:///d:/Kojin/FShot/docs/2.Design/10_Platform_Win32/10_08_ConfigStore.md) (ghi nguyên tử .tmp, kiểm soát khóa tệp, hot-reload qua FileSystemWatcher debounce 300ms, recovery khi tệp hỏng).
  - [x] Mở rộng toàn bộ mô hình dữ liệu `AppConfig` trong [Config.fs](file:///d:/Kojin/FShot/src/FShot.Core/Domain/Config.fs) cho v1.0:
    - [x] **Nhóm Chung (General):** `SavePathFixed: bool` (`FR-CFG-002`), `SaveAsFileExtension: string` (`FR-CFG-004`, "png"|"jpg"), `JpegQuality: int` (`FR-CFG-005`, clamp 1–100), `StartupLaunch: bool` (`FR-CFG-006`), `DisabledTrayIcon: bool` (`FR-CFG-007`), `ShowDesktopNotification: bool`, `ShowAbortNotification: bool` (`FR-CFG-008`), `CopyOnDoubleClick: bool` (`FR-CFG-014`), `SaveLastRegion: bool` (`FR-CFG-015`), `AllowMultipleGuiInstances: bool` (`FR-CFG-016`), `CaptureBackend: string` ("Auto"|"Wgc"|"Gdi").
    - [x] **Nhóm Giao diện (UI/UX):** `UiColor: string` (`FR-CFG-100`), `ContrastUiColor: string` (`FR-CFG-101`), `ContrastOpacity: byte` (`FR-CFG-102`, clamp 0–255), `PredefinedColorPaletteLarge: bool` (`FR-CFG-103`), `UserColors: string list` (`FR-CFG-104`), `Buttons: string list` (`FR-CFG-105`), `UiLanguage: string` (`FR-CFG-106`), `FontFamily: string` (`FR-CFG-107`).
    - [x] **Nhóm Công cụ mặc định (Tool Defaults):** `DrawFontSize: float` (`FR-CFG-202`), `DrawCircleCounterSize: float` (`FR-CFG-203`), `DrawPixelateSize: int` (`FR-CFG-204`), `DrawRectangleRadius: float` (`FR-CFG-205`), `DrawMarkerSize: float` (`FR-CFG-206`).
  - [x] Nâng cấp [ConfigStore.fs](file:///d:/Kojin/FShot/src/FShot.Platform.Win32/Config/ConfigStore.fs):
    - [x] Cơ chế schema migration mềm dẻo: tự động bổ sung các trường v1.0 mới với giá trị mặc định khi đọc file config cũ từ Phase 1 mà không làm mất dữ liệu người dùng.
    - [x] Ghi nguyên tử (Atomic write qua file tạm `.tmp` rồi `File.Move(..., overwrite=true)`) để chống hỏng file nếu xảy ra mất điện hoặc crash giữa chừng.
    - [x] Tích hợp `FileSystemWatcher` lắng nghe file `config.json` thay đổi từ bên ngoài và tự động thông báo phát sự kiện reload (`FR-SYS-011`).
  - [x] Viết unit tests kiểm thử serialization, deserialization và validation toàn diện cho tất cả các trường mới trong [ConfigTests.fs](file:///d:/Kojin/FShot/tests/FShot.Core.Tests/Domain/ConfigTests.fs).
  - [ ] Verify runtime: kiểm tra file `config.json` sinh ra đầy đủ các trường mới, chỉnh sửa thủ công và xác nhận ứng dụng đọc đúng.
- [x] **P2.12** Migrate hoặc đọc cấu hình cũ từ `flameshot.ini` (`FR-WIN-006`, migration).
  - [x] Hoàn thiện tài liệu thiết kế [07_05_Migration.md](file:///d:/Kojin/FShot/docs/2.Design/07_Config/07_05_Migration.md) (cú pháp INI, bảng ánh xạ 19 thuộc tính, chuẩn hóa màu Qt/Hex, quy trình phát hiện tự động không ghi đè file gốc).
  - [x] Triển khai bộ phân giải INI thuần F# `IniParser` trong `src/FShot.Platform.Win32/Config/FlameshotIniParser.fs`:
    - [x] Phân tích các section (`[General]`, `[Shortcuts]`), cặp key-value, comment (`#`, `;`) và mảng giá trị phân tách dấu phẩy.
  - [x] Xây dựng bộ ánh xạ thuộc tính `FlameshotConfigMigrator`:
    - [x] Chuyển đổi các khóa Flameshot sang FShot: `savePath`, `filenamePattern`, `saveAsFileExtension`, `jpegQuality`, `drawColor`, `drawThickness`, `drawFontSize`, `contrastOpacity`, `uiColor`, `userColors`, `startupLaunch`, `disabledTrayIcon`, `showDesktopNotification`.
    - [x] Chuyển đổi định dạng màu từ format Qt (`rgb(255, 0, 0)` hoặc hex) sang chuẩn hex `#RRGGBB` của FShot.
  - [x] Tự động phát hiện và đề xuất di chuyển dữ liệu: (chỉ có CLI import; auto-migrate khi khởi động chưa nối dây)
    - [ ] Khi khởi động lần đầu (chưa có `%APPDATA%\FShot\config.json`), kiểm tra sự tồn tại của `%APPDATA%\flameshot\flameshot.ini`.
    - [ ] Nếu tìm thấy, tự động migrate dữ liệu sang `config.json` và ghi log thông báo.
    - [x] Hỗ trợ cờ CLI `fshot config --import-flameshot [path]` để người dùng chủ động nạp cấu hình cũ bất kỳ lúc nào (`FR-SYS-013`).
  - [x] Viết unit tests kiểm thử parse các mẫu file `flameshot.ini` thực tế từ cộng đồng Flameshot (`MigrationTests.fs`).
  - [ ] Verify runtime: đặt file `flameshot.ini` mẫu vào máy, xóa `config.json` FShot, khởi động app và xác nhận các cài đặt cá nhân từ Flameshot được giữ nguyên vẹn.
- [x] **P2.13** Cửa sổ cài đặt (Config Editor) cho các tùy chọn chung, giao diện và giá trị mặc định công cụ (`FR-SYS-006`, `FR-CFG-100`–`FR-CFG-209`).
  - [x] Hoàn thiện tài liệu thiết kế UI/UX [11_06_ConfigWindow.md](file:///d:/Kojin/FShot/docs/2.Design/11_UI_Avalonia/11_06_ConfigWindow.md) (bố cục 4 Tab, phong cách Kawaii Claymorphism, tracking dirty state, footer actions Apply/Save/Reset Defaults).
  - [x] Thiết kế và tạo cửa sổ `SettingsWindow.axaml` và `SettingsWindow.axaml.fs` trong `src/FShot.UI/Windows/`:
    - [x] Giao diện hiện đại theo phong cách Kawaii Claymorphism, kích thước chuẩn (700x520), hỗ trợ dark/light theme hài hòa.
    - [x] Sử dụng cấu trúc TabControl phân chia 4 khu vực chức năng khoa học:
      - [x] **Tab 1 — Cài đặt chung (General):** Thư mục lưu (TextBox + nút Browse thư mục), mẫu tên file (tích hợp `FileNameEditor`), định dạng lưu mặc định (Radio/ComboBox PNG/JPG kèm slider chất lượng JPG), toggle khởi động cùng Windows, toggle thông báo thành công / hủy, toggle ẩn icon tray, toggle copy on double-click.
      - [x] **Tab 2 — Giao diện & Hiển thị (Interface):** Bảng chọn màu accent UI chính/phụ, slider điều chỉnh độ mờ nền dimming ngoài vùng chọn (0% – 100%), ComboBox chọn ngôn ngữ giao diện (Tiếng Việt, English, Tự động), danh sách bật/tắt và sắp xếp thứ tự các nút công cụ trên thanh Toolbar.
      - [x] **Tab 3 — Công cụ mặc định (Tool Defaults):** Màu vẽ mặc định (`drawColor`, component `ColorSelector`), bảng màu tự chọn (component `ColorSelector`: ô nhập hex + bánh xe màu + nút "Thêm màu" + 8 ô màu, ô đang chọn có viền accent đậm), thanh trượt độ dày nét vẽ mặc định (1–50px), cỡ chữ mặc định, bán kính bo góc rectangle, kích thước mosaic pixelate.
      - [x] **Tab 4 — Phím tắt (Shortcuts):** Bảng tra cứu danh sách hành động và phím tắt tương ứng, cho phép click để lắng nghe và gán phím nóng mới (kết nối P2.06 `HotkeyParser`). *(Click để ghi phím tạm thay bằng TextBox — nợ test)*
    - [x] Thanh điều khiển dưới chân cửa sổ (Footer Actions):
      - [x] Nút "Khôi phục mặc định" (Reset to Defaults - `FR-SYS-014`): hiển thị hộp thoại xác nhận trước khi đưa toàn bộ cài đặt về mặc định. *(Chưa có hộp thoại xác nhận — nợ test)*
      - [x] Nút "Áp dụng" (Apply): lưu vào `config.json` và kích hoạt áp dụng ngay lập tức cho instance đang chạy.
      - [x] Nút "Lưu & Đóng" (Save & Close) và nút "Hủy" (Cancel).
  - [x] Kết nối kích hoạt mở Settings Window:
    - [x] Từ menu Tray icon mục "Cài đặt (Settings)" (`FR-SYS-006`).
    - [x] Từ lệnh dòng lệnh CLI `fshot config` (`FR-CLI-05`).
  - [x] Viết unit tests kiểm thử ViewModel / State binding (một phần) cho các thao tác đổi cài đặt và reset defaults.
  - [ ] Verify runtime: mở cửa sổ Settings từ Tray, chỉnh sửa các tab, bấm Lưu và xác nhận các thay đổi có hiệu lực tức thì trên giao diện chụp ảnh.
- [x] **P2.14** Trình chỉnh sửa mẫu tên file có preview token strftime (`FR-CFG-003`).
  - [x] Hoàn thiện tài liệu thiết kế component `FileNameEditor` trong [11_06_ConfigWindow.md](file:///d:/Kojin/FShot/docs/2.Design/11_UI_Avalonia/11_06_ConfigWindow.md) (live preview động theo thời gian thực, bảng token %Y %m %d %H %M %S, engine phát hiện ký tự cấm Windows).
  - [x] Thiết kế UserControl `FileNameEditor.axaml` nhúng trực tiếp trong Tab General của `SettingsWindow`:
    - [x] Ô nhập văn bản `TextBox` cho phép người dùng gõ chuỗi định dạng (template pattern).
    - [x] Khối hiển thị Xem trước (Live Preview Banner): hiển thị tức thì tên file kết quả theo thời gian thực hệ thống (cập nhật mỗi giây hoặc ngay khi gõ).
    - [x] Bảng nút bấm chèn nhanh các token định dạng thời gian chuẩn strftime: `%Y` (Năm 4 số), `%m` (Tháng 2 số), `%d` (Ngày 2 số), `%H` (Giờ 24h), `%M` (Phút), `%S` (Giây).
    - [x] Click vào token button sẽ tự động chèn mã token vào vị trí con trỏ chuột trong TextBox.
  - [x] Kiểm tra tính hợp lệ và phòng ngừa lỗi (Validation Engine):
    - [x] Phát hiện và cảnh báo tức thì nếu người dùng nhập các ký tự bị cấm trên hệ thống tập tin Windows (`\`, `/`, `:`, `*`, `?`, `"`, `<`, `>`, `|`).
    - [x] Ngăn chặn lưu mẫu rỗng hoặc mẫu chỉ gồm ký tự cấm, tự động gợi ý khôi phục mẫu an toàn mặc định (`fshot_%Y-%m-%d-%H%M%S`).
  - [x] Viết unit tests kiểm thử bộ phân giải preview với các chuỗi mẫu phức tạp và kiểm tra phát hiện ký tự không hợp lệ.
  - [ ] Verify runtime: gõ thử các mẫu tên file trong Settings Window, bấm chèn token, kiểm tra live preview và thử chụp ảnh xác nhận file lưu đúng tên đã đặt.

### Epic 10: Pin Widget

> **Tài liệu thiết kế & Tham khảo:**
> - [11_07_PinWindow.md](file:///d:/Kojin/FShot/docs/2.Design/11_UI_Avalonia/11_07_PinWindow.md): Thiết kế chi tiết cửa sổ ghim ảnh nổi PinWidget, tương tác drag/zoom/rotate/opacity, quản lý vòng đời bộ nhớ và menu ngữ cảnh.
> - [12_01_DesignTokens.md](file:///d:/Kojin/FShot/docs/2.Design/12_UIUX_Mock_Penpot/12_01_DesignTokens.md): Chuẩn thiết kế Kawaii Claymorphism cho viền bo góc và hiệu ứng đổ bóng `FR-PIN-10`.
> - [003_SRS.md](file:///d:/Kojin/FShot/docs/1.Investigation/003_SRS.md): Yêu cầu kỹ thuật `FR-PIN-01`–`FR-PIN-10`.

- [ ] **P2.15** Cửa sổ ghim ảnh Topmost không viền (`FR-PIN-01`, `FR-PIN-10`).
  - [ ] Hoàn thiện tài liệu thiết kế [11_07_PinWindow.md](file:///d:/Kojin/FShot/docs/2.Design/11_UI_Avalonia/11_07_PinWindow.md) (kiến trúc Window không viền, Topmost, quản lý vòng đời multi-instance và giải phóng SKBitmap).
  - [ ] Tạo cửa sổ `PinWindow.axaml` và `PinWindow.axaml.fs` trong `src/FShot.UI/Windows/`:
    - [ ] Cấu hình thuộc tính: `SystemDecorations = None`, `Topmost = true`, `TransparencyLevelHint = Transparent`, `Background = Transparent`, `ShowInTaskbar = false`.
    - [ ] Áp dụng style Kawaii Claymorphism (`FR-PIN-10`): Khung `Border` bo góc (`CornerRadius="8"`), viền mờ cao cấp (`BorderBrush="#40FFFFFF"`), hiệu ứng đổ bóng sâu tách nền (`BoxShadow="0 8 24 0 #40000000"`).
    - [ ] Nhúng control vẽ ảnh Skia (`SKBitmapCanvas` hoặc Avalonia `Image`) chứa ảnh bitmap crop từ vùng chọn kèm chú thích đã render phẳng.
  - [ ] Tích hợp nút Pin vào thanh công cụ Overlay:
    - [ ] Khai báo `PinAction` trong `ToolbarAction` (`src/FShot.Core/Domain/Annotation.fs`).
    - [ ] Thêm icon Pin Kawaii vào `src/FShot.UI/SkiaCanvas/ToolbarIcons.fs` và nút Pin vào `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`.
    - [ ] Bắt sự kiện click Pin: tạo mới instance `PinWindow`, khởi tạo vị trí xuất hiện trùng khớp tọa độ vùng chọn, hiển thị cửa sổ ghim và đóng `CaptureOverlayWindow`.
  - [ ] Quản lý tài nguyên và đa cửa sổ (Multi-instance):
    - [ ] Cho phép mở đồng thời nhiều cửa sổ ghim độc lập trên màn hình.
    - [ ] Bắt sự kiện `Closed`: giải phóng an toàn `SKBitmap.Dispose()` để chống rò rỉ bộ nhớ đồ họa.
  - [ ] Viết unit tests kiểm thử khởi tạo Window, truyền tải bitmap và tính toán kích thước trong `tests/FShot.UI.Tests/Windows/PinWindowTests.fs`.
  - [ ] Verify runtime trên Windows 10/11: chụp vùng chọn bất kỳ, bấm nút Pin, xác nhận cửa sổ ghim nổi trên các cửa sổ khác, không có viền hệ thống, có đổ bóng mờ rõ ràng và overlay chụp đóng sạch sẽ.
- [ ] **P2.16** Di chuyển, thu phóng, điều chỉnh độ trong suốt và xoay ảnh ghim (`FR-PIN-02`–`FR-PIN-05`, `FR-PIN-09`).
  - [ ] Di chuyển tự do trên desktop (`FR-PIN-02`):
    - [ ] Xử lý sự kiện `PointerPressed` trên `PinWindow`: kích hoạt `BeginMoveDrag(e)` cho phép người dùng click giữ chuột trái kéo cửa sổ ghim tới bất kỳ đâu trên Virtual Screen (kể cả giữa các màn hình khác nhau).
    - [ ] Đổi con trỏ chuột sang hình bàn tay (`SizeAll` / `Hand`) khi rê chuột lên ảnh.
  - [ ] Thu phóng tỉ lệ động với khử răng cưa (`FR-PIN-03`, `FR-PIN-09`):
    - [ ] Xử lý `PointerWheelChanged` (khi không giữ `Ctrl`): cuộn lên tăng tỉ lệ (`scale *= 1.1`), cuộn xuống giảm tỉ lệ (`scale /= 1.1`).
    - [ ] Ràng buộc giới hạn tỉ lệ an toàn: $0.1 \le \text{scale} \le 5.0$ (10% đến 500%).
    - [ ] Thu phóng hướng tâm con trỏ chuột (`Zoom toward cursor`): tính toán lại `Position` để điểm dưới con trỏ chuột giữ nguyên vị trí trên màn hình.
    - [ ] Cấu hình Skia `SKFilterQuality.High` / `SKPaint.IsAntialias = true` để ảnh khi phóng to không bị vỡ hạt (`antialiasingPinZoom`).
  - [ ] Điều chỉnh độ trong suốt mờ đục (`FR-PIN-04`):
    - [ ] Xử lý `Ctrl + PointerWheelChanged` hoặc phím tắt `[` / `]`: thay đổi giá trị `Window.Opacity` mỗi bước 5% ($0.05$).
    - [ ] Ràng buộc giới hạn: $0.1 \le \text{Opacity} \le 1.0$ (không cho phép giảm dưới 10% để tránh mất dấu cửa sổ).
  - [ ] Xoay ảnh ghim 90° (`FR-PIN-05`):
    - [ ] Phím tắt `R` (xoay phải 90° cùng chiều kim đồng hồ) và `Shift + R` (xoay trái 90° ngược chiều kim đồng hồ).
    - [ ] Cập nhật góc xoay `angle = (angle + 90) % 360`, áp dụng biến đổi ma trận xoay Skia.
    - [ ] Tự động hoán đổi kích thước cửa sổ (`Width` $\leftrightarrow$ `Height`) khi góc xoay là $90^\circ$ hoặc $270^\circ$.
  - [ ] Viết unit tests kiểm thử ma trận thu phóng, tính toán tọa độ hướng tâm, giới hạn opacity và thuật toán xoay 90°.
  - [ ] Verify runtime: kéo di chuyển cửa sổ qua lại giữa 2 màn hình; lăn chuột phóng to/thu nhỏ kiểm tra độ sắc nét; giữ Ctrl lăn chuột kiểm tra nhìn xuyên thấu xuống cửa sổ bên dưới; nhấn `R` xoay ảnh 4 hướng không bị méo.
- [ ] **P2.17** Menu ngữ cảnh của cửa sổ ghim: copy, save, close (`FR-PIN-06`–`FR-PIN-08`).
  - [ ] Thiết kế `ContextMenu` Kawaii bo góc xuất hiện khi nhấp chuột phải (`PointerReleased` với RightButton):
    - [ ] Mục "Sao chép ảnh" (`Ctrl+C`): gọi `ClipboardService.copyToClipboardNative` đẩy bitmap đã xoay/scale vào clipboard với 2 định dạng `CF_DIB` và `PNG` (`FR-PIN-06`), phát âm thanh `MessageBeep` và thông báo desktop ngắn.
    - [ ] Mục "Lưu ảnh ra tệp..." (`Ctrl+S`): mở hộp thoại `StorageProvider.SaveFilePickerAsync` cho phép lưu file PNG/JPG (`FR-PIN-07`), hoặc lưu nhanh vào `savePath` nếu đã cấu hình `SavePathFixed`.
    - [ ] Mục "Xoay 90° phải" (`R`) và "Khôi phục kích thước 100%" (`Ctrl+0`).
    - [ ] Submenu "Độ mờ đục": các mức nhanh 100%, 75%, 50%, 25%.
    - [ ] Mục "Đóng ghim" (`Esc` / Double-click) (`FR-PIN-08`).
  - [ ] Các cơ chế đóng cửa sổ ghim nhanh (`FR-PIN-08`):
    - [ ] Nhấp đúp chuột trái (Double-click) vào bất kỳ vị trí nào trên ảnh ghim $\to$ đóng cửa sổ ngay lập tức.
    - [ ] Nhấn phím `Escape` khi cửa sổ đang active $\to$ đóng cửa sổ ngay lập tức.
    - [ ] Đảm bảo dọn dẹp giải phóng tài nguyên unmanaged và hủy tham chiếu instance khi đóng.
  - [ ] Viết unit tests kiểm thử các lệnh ContextMenu, dispatch action copy/save và xử lý đóng cửa sổ an toàn.
  - [ ] Verify runtime: click phải mở menu mượt mà, copy rồi paste vào Paint/Discord xác nhận ảnh đúng góc xoay; lưu ra file PNG xác nhận ảnh nét; double click hoặc nhấn Esc xác nhận cửa sổ biến mất sạch sẽ.

### Epic 11: Annotation Tools Advanced

> **Tài liệu thiết kế & Tham khảo:**
> - [04_09_InvertAndCounter.md](file:///d:/Kojin/FShot/docs/2.Design/04_Annotation/04_09_InvertAndCounter.md): Thiết kế chi tiết công cụ Đảo màu (Invert) bằng blend mode Difference và Bong bóng số tự động tăng (Circle Counter) kèm khôi phục chỉ số khi Undo/Redo.
> - [04_10_ConstraintsAndSizing.md](file:///d:/Kojin/FShot/docs/2.Design/04_Annotation/04_10_ConstraintsAndSizing.md): Thiết kế giải thuật ràng buộc góc lượng giác 45°/90°, khóa tỉ lệ 1:1, điều chỉnh kích thước công cụ bằng bàn phím số, con lăn chuột và chỉ báo SizeIndicatorBox.
> - [04_11_ObjectSelectionAndEditing.md](file:///d:/Kojin/FShot/docs/2.Design/04_Annotation/04_11_ObjectSelectionAndEditing.md): Thiết kế chế độ chọn đối tượng, giải thuật Hit-Testing dung sai hình học, di chuyển nét vẽ, xóa đối tượng và commit văn bản nhanh.
> - [04_05_RectangleAndCircle.md](file:///d:/Kojin/FShot/docs/2.Design/04_Annotation/04_05_RectangleAndCircle.md): Ràng buộc hình học cơ bản hình chữ nhật bo góc và elip.
> - [003_SRS.md](file:///d:/Kojin/FShot/docs/1.Investigation/003_SRS.md): Yêu cầu kỹ thuật `FR-ANN-09`–`FR-ANN-21`, `FR-UNDO-005`, `FR-TB-07`, `FR-CFG-203`.

- [ ] **P2.20** Công cụ đảo ngược màu (Invert) trong vùng chỉ định (`FR-ANN-09`).
  - [ ] Hoàn thiện tài liệu thiết kế [04_09_InvertAndCounter.md](file:///d:/Kojin/FShot/docs/2.Design/04_Annotation/04_09_InvertAndCounter.md).
  - [ ] Mở rộng domain model trong `src/FShot.Core/Domain/Annotation.fs`:
    - [ ] Thêm case `Invert of start: Point * endPoint: Point` vào `Tool` DU.
    - [ ] Thêm `InvertTool` vào `ToolKind`.
    - [ ] Cập nhật tính toán `BoundingBox` cho `Invert(a, b)`: $X = \min(a.X, b.X), Y = \min(a.Y, b.Y), W = |b.X - a.X|, H = |b.Y - a.Y|$.
  - [ ] Triển khai Skia render trong `src/FShot.Rendering.Skia/Renderers/AnnotationRenderer.fs`:
    - [ ] Áp dụng `SKPaint` với màu `#FFFFFFFF` và `SKBlendMode.Difference` để đảo ngược bit màu sắc pixel ($255 - C$) trực tiếp bằng phần cứng GPU shader mà không tốn chi phí copy CPU.
    - [ ] Vẽ preview viền đứt nét kết hợp hiệu ứng Difference tức thời khi đang kéo chuột trong `CaptureCanvas`.
  - [ ] Bổ sung icon Kawaii Invert vào `src/FShot.UI/SkiaCanvas/ToolbarIcons.fs` và phím tắt chuyển nhanh (phím `I`).
  - [ ] Viết unit tests kiểm thử khởi tạo Invert, tính toán BoundingBox và serialization trong `tests/FShot.Core.Tests/Domain/AnnotationTests.fs`.
  - [ ] Verify runtime: kéo vùng chọn đảo màu lên văn bản đen/trắng và hình ảnh nhiều màu, xác nhận màu sắc đảo ngược tức thì, xuất ảnh ra file giữ đúng hiệu ứng.
- [ ] **P2.21** Bong bóng đếm số tự động tăng (Circle Counter) (`FR-ANN-10`, `FR-UNDO-005`, `FR-CFG-203`).
  - [ ] Hoàn thiện tài liệu thiết kế [04_09_InvertAndCounter.md](file:///d:/Kojin/FShot/docs/2.Design/04_Annotation/04_09_InvertAndCounter.md).
  - [ ] Mở rộng domain model trong `src/FShot.Core/Domain/Annotation.fs` và `src/FShot.Core/Domain/History.fs`:
    - [ ] Thêm case `CircleCounter of center: Point * index: int * radius: float` vào `Tool` DU.
    - [ ] Thêm `CircleCounterTool` vào `ToolKind`.
    - [ ] Bổ sung trường `CounterIndex: int` vào `Snapshot` trong `History.fs` để hỗ trợ khôi phục chỉ số khi Undo/Redo (`FR-UNDO-005`).
  - [ ] Triển khai logic auto-increment trong `src/FShot.Core/State/OverlayState.fs`:
    - [ ] Quản lý trạng thái `CurrentCounterIndex: int` (khởi tạo = 1).
    - [ ] Khi click chuột đặt huy hiệu: sinh annotation với index hiện tại rồi tự động tăng `CurrentCounterIndex = index + 1`.
    - [ ] Khi Undo (`Ctrl+Z`): khôi phục `CurrentCounterIndex` từ snapshot trước đó (xóa số 3 thì click tiếp theo sẽ đánh lại số 3).
    - [ ] Khi Redo (`Ctrl+Y`): khôi phục `CurrentCounterIndex` tương ứng.
    - [ ] Click phải vào nút CircleCounter trên toolbar: reset chỉ số đếm về 1.
  - [ ] Triển khai render Skia Kawaii Claymorphism trong `src/FShot.Rendering.Skia/Renderers/AnnotationRenderer.fs`:
    - [ ] Vẽ vòng tròn nền tô đặc với màu vẽ `style.Color`, viền mờ bóng bẩy.
    - [ ] Tính toán độ tương phản độ sáng Luminance ($L = 0.299R + 0.587G + 0.114B$): tự động chọn chữ đen trên nền sáng và chữ trắng trên nền tối.
    - [ ] Căn giữa tuyệt đối số thứ tự bên trong vòng tròn, cỡ chữ tỉ lệ theo bán kính (`radius * 1.1`).
    - [ ] Bán kính lấy từ cấu hình `DrawCircleCounterSize: float` (`FR-CFG-203`, mặc định 28.0 px $\to$ bán kính 14.0 px).
  - [ ] Bổ sung icon Kawaii Circle Counter vào `src/FShot.UI/SkiaCanvas/ToolbarIcons.fs` và phím tắt chuyển tool.
  - [ ] Viết unit tests kiểm thử auto-increment, thuật toán tính màu chữ tương phản và khôi phục chỉ số đếm khi Undo/Redo (`tests/FShot.Core.Tests/State/OverlayStateTests.fs`).
  - [ ] Verify runtime: click 5 điểm liên tiếp trên màn hình xác nhận xuất hiện huy hiệu 1, 2, 3, 4, 5; nhấn Ctrl+Z 2 lần rồi click tiếp xác nhận ra số 4; thử đổi màu nền sáng (vàng) và tối (xanh đậm) kiểm tra màu số đọc rõ ràng.
- [ ] **P2.22** Ràng buộc góc 45°/90° khi vẽ line/arrow/marker bằng `Ctrl` (`FR-ANN-11`).
  - [ ] Hoàn thiện tài liệu thiết kế [04_10_ConstraintsAndSizing.md](file:///d:/Kojin/FShot/docs/2.Design/04_Annotation/04_10_ConstraintsAndSizing.md).
  - [ ] Xây dựng module hình học `src/FShot.Core/Geometry/AngleSnapping.fs`:
    - [ ] Hàm `snapAngle45 (start: Point) (current: Point) : Point`.
    - [ ] Tính vector dịch chuyển $\Delta x, \Delta y$, độ dài khoảng cách $L = \sqrt{\Delta x^2 + \Delta y^2}$, góc $\theta = \operatorname{atan2}(\Delta y, \Delta x)$.
    - [ ] Làm tròn góc tới bội số gần nhất của $\pi/4$ ($45^\circ$): $\theta_{\text{snap}} = \operatorname{round}(\theta / (\pi/4)) \times (\pi/4)$.
    - [ ] Tính tọa độ đích mới: $x' = x_1 + L \cos(\theta_{\text{snap}}), y' = y_1 + L \sin(\theta_{\text{snap}})$.
  - [ ] Tích hợp vào `src/FShot.Core/State/OverlayState.fs` và `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`:
    - [ ] Bắt cờ `KeyModifiers.Control` trong sự kiện `PointerMoved`.
    - [ ] Khi giữ `Ctrl` trong lúc vẽ `Line`, `Arrow`, `Marker`: tự động áp dụng `snapAngle45` cho điểm kết thúc trong preview.
    - [ ] Nhả phím `Ctrl`: trả lại tọa độ tự do theo con trỏ chuột trong thời gian thực.
  - [ ] Viết unit tests kiểm thử các góc $0^\circ, 45^\circ, 90^\circ, 135^\circ, 180^\circ, -45^\circ, -90^\circ, -135^\circ$ trong `tests/FShot.Core.Tests/Geometry/AngleSnappingTests.fs`.
  - [ ] Verify runtime: giữ `Ctrl` kéo đường thẳng và mũi tên, xác nhận nét vẽ hít chuẩn xác vào các trục ngang, thẳng đứng và đường chéo 45 độ.
- [ ] **P2.23** Giữ tỉ lệ 1:1 khi vẽ rectangle/circle bằng `Ctrl` (`FR-ANN-12`).
  - [ ] Rà soát tài liệu thiết kế [04_10_ConstraintsAndSizing.md](file:///d:/Kojin/FShot/docs/2.Design/04_Annotation/04_10_ConstraintsAndSizing.md) và [04_05_RectangleAndCircle.md](file:///d:/Kojin/FShot/docs/2.Design/04_Annotation/04_05_RectangleAndCircle.md).
  - [ ] Bổ sung hàm ràng buộc hình vuông / hình tròn trong `src/FShot.Core/Geometry/AngleSnapping.fs`:
    - [ ] Hàm `snapSquare (start: Point) (current: Point) : Point`.
    - [ ] Tính độ lệch cạnh lớn nhất $\text{side} = \max(|x_2 - x_1|, |y_2 - y_1|)$, bảo toàn dấu hướng kéo theo 4 góc phần tư.
    - [ ] Tính tọa độ kết thúc: $x' = x_1 + \operatorname{sign}(x_2 - x_1) \times \text{side}, y' = y_1 + \operatorname{sign}(y_2 - y_1) \times \text{side}$.
  - [ ] Tích hợp vào preview và commit của `Rectangle` và `Circle` trong `OverlayState.fs` và `CaptureCanvas.axaml.fs`:
    - [ ] Khi giữ `Ctrl`: hình chữ nhật ép thành hình vuông hoàn hảo ($\text{Width} = \text{Height}$).
    - [ ] Khi giữ `Ctrl`: hình elip ép thành hình tròn hoàn hảo ($R_x = R_y$).
    - [ ] Commit annotation với trường `aspectLocked = true` cho `Circle`.
  - [ ] Viết unit tests kiểm thử kéo hình chữ nhật / elip theo 4 hướng khi có và không có cờ `Ctrl`.
  - [ ] Verify runtime: kéo vẽ khung chữ nhật và elip kèm phím `Ctrl`, xác nhận hình vuông và tròn đều tuyệt đối; nhả `Ctrl` giữa chừng hình trở lại tỉ lệ tự do.
- [ ] **P2.24** Nhập số để đặt chính xác kích thước công cụ (`FR-ANN-13`, `FR-TB-07`).
  - [ ] Hoàn thiện tài liệu thiết kế [04_10_ConstraintsAndSizing.md](file:///d:/Kojin/FShot/docs/2.Design/04_Annotation/04_10_ConstraintsAndSizing.md).
  - [ ] Triển khai bộ đệm số `NumericInputBuffer` trong `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`:
    - [ ] Lắng nghe sự kiện `KeyDown`: khi người dùng gõ ký tự số `0`–`9` (không đi kèm `Ctrl`/`Alt`):
      - [ ] Tích lũy chuỗi số (ví dụ gõ liên tiếp `1` rồi `6` $\to$ `"16"`).
      - [ ] Cơ chế debounce timer 800ms hoặc kết thúc khi bấm `Enter`.
      - [ ] Phân giải thành số nguyên, clamp trong phạm vi hợp lệ $[1.0 .. 50.0]\text{ px}$.
      - [ ] Dispatch event cập nhật kích thước công cụ hiện tại (`StrokeWidth` hoặc `FontSize`).
  - [ ] Thiết kế và hiển thị chỉ báo nổi `SizeIndicatorBox` (`FR-TB-07`):
    - [ ] Huy hiệu Kawaii Claymorphism nổi ở góc hoặc cạnh con trỏ chuột, hiển thị số pixel kèm chấm tròn mô phỏng độ dày thực tế.
    - [ ] Tự động mờ dần (Fade out) sau 1.2 giây không có thao tác nhập mới.
  - [ ] Viết unit tests kiểm thử bộ đệm số, xử lý debounce timeout, giới hạn min/max và dispatch event kích thước.
  - [ ] Verify runtime: gõ phím số `1`, `4` trên bàn phím xác nhận nét vẽ đổi ngay sang 14px, chỉ báo nổi xuất hiện và biến mất mượt mà.
- [ ] **P2.25** Lăn chuột để tăng/giảm độ dày nét vẽ (`FR-ANN-14`, `FR-TB-07`).
  - [ ] Hoàn thiện tài liệu thiết kế [04_10_ConstraintsAndSizing.md](file:///d:/Kojin/FShot/docs/2.Design/04_Annotation/04_10_ConstraintsAndSizing.md).
  - [ ] Bắt sự kiện `PointerWheelChanged` trong `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs` khi đang ở chế độ vẽ:
    - [ ] Cuộn lên ($\Delta > 0$): tăng kích thước; Cuộn xuống ($\Delta < 0$): giảm kích thước.
    - [ ] Bước nhảy thích ứng (Adaptive step): thay đổi $\pm 1.0\text{ px}$ khi kích thước $< 10$, thay đổi $\pm 2.0\text{ px}$ khi kích thước $\ge 10$.
    - [ ] Clamp giá trị nét vẽ trong khoảng $[1.0 .. 50.0]\text{ px}$.
    - [ ] Áp dụng linh hoạt theo tool hiện tại: `StrokeWidth` cho bút vẽ/hình khối, `FontSize` cho Text, `BlockSize` cho Pixelate, đường kính cho CircleCounter.
    - [ ] Kích hoạt hiển thị chỉ báo nổi `SizeIndicatorBox` hiển thị độ dày nét vẽ tức thời.
  - [ ] Viết unit tests kiểm thử hàm tính delta cuộn chuột, clamping và cập nhật state nét vẽ trong `OverlayStateTests.fs`.
  - [ ] Verify runtime: chọn công cụ Pencil, lăn chuột lên/xuống, xác nhận độ dày nét vẽ thay đổi tức thì và chỉ báo hiển thị đúng số pixel.
- [ ] **P2.26** Chọn, di chuyển hoặc sửa chú thích cũ (`FR-ANN-18`–`FR-ANN-21`).
  - [ ] Hoàn thiện tài liệu thiết kế [04_11_ObjectSelectionAndEditing.md](file:///d:/Kojin/FShot/docs/2.Design/04_Annotation/04_11_ObjectSelectionAndEditing.md).
  - [ ] Triển khai module kiểm tra va chạm hình học `src/FShot.Core/Geometry/HitTesting.fs` (`FR-ANN-19`):
    - [ ] Bán kính dung sai $\text{tolerance} = 6.0\text{ px}$.
    - [ ] Hàm khoảng cách từ điểm tới đoạn thẳng `distancePointToSegment` (cho Line, Arrow, Marker, Pencil).
    - [ ] Hàm kiểm tra va chạm chu vi hình chữ nhật và đường tròn (cho Rectangle, Circle).
    - [ ] Hàm kiểm tra va chạm BoundingBox (cho Text, Invert, Pixelate, CircleCounter, Icon).
    - [ ] Quét theo thứ tự Z-order từ mới nhất đến cũ nhất để ưu tiên chọn đối tượng ở lớp trên cùng.
  - [ ] Quản lý trạng thái chọn và di chuyển trong `src/FShot.Core/State/OverlayState.fs` (`FR-ANN-18`):
    - [ ] Bổ sung trường `SelectedAnnotation: Annotation option` trong state.
    - [ ] Khi click vào nét vẽ cũ: chọn đối tượng, hiển thị khung bao đứt nét và 4 điểm neo tròn ở các góc.
    - [ ] Kéo chuột khi đang chọn: tịnh tiến toàn bộ tọa độ đối tượng theo vector $(\Delta x, \Delta y)$, hiển thị preview di chuyển mượt mà.
    - [ ] Thả chuột: commit tọa độ mới và đẩy snapshot vào `HistoryStack` (hỗ trợ hoàn tác di chuyển bằng `Ctrl+Z`).
  - [ ] Xóa đối tượng chú thích đang chọn (`FR-ANN-20`):
    - [ ] Nhấn phím `Delete` hoặc `Backspace`: loại bỏ đối tượng đang chọn khỏi danh sách `Annotations`.
    - [ ] Đẩy snapshot mới vào `HistoryStack` để có thể hoàn tác khôi phục lại nét vẽ.
  - [ ] Kết thúc chỉnh sửa văn bản tại chỗ (`FR-ANN-21`):
    - [ ] Nhấn `Ctrl + Enter` (hoặc `Ctrl + Return`): kết thúc nhập văn bản ngay lập tức, phẳng hóa TextBox thành `Annotation.Text` và commit snapshot.
    - [ ] Nhấn `Esc`: hủy bỏ thay đổi văn bản đang gõ.
  - [ ] Viết unit tests kiểm thử khoảng cách va chạm cho từng loại tool, chọn theo Z-order, di chuyển tịnh tiến tọa độ và xóa đối tượng trong `tests/FShot.Core.Tests/Geometry/HitTestingTests.fs`.
  - [ ] Verify runtime: vẽ 3 hình chữ nhật và mũi tên, click chuột chọn mũi tên xác nhận hiện khung viền chọn; kéo mũi tên sang vị trí mới; nhấn Delete xác nhận nét vẽ biến mất; nhấn Ctrl+Z xác nhận nét vẽ khôi phục lại.

### Epic 12: Selection Engine Advanced
- [ ] **P2.27** Co giãn đối xứng 2 px bằng `Ctrl+Shift+Arrow` (`FR-SEL-07`).
- [ ] **P2.28** Ngăn vùng chọn thu nhỏ quá mức và giới hạn trong phạm vi chụp (`FR-SEL-08`, `FR-SEL-09`).
- [ ] **P2.29** Chọn toàn bộ màn hình chụp bằng `Ctrl+A` (`FR-SEL-10`).
- [ ] **P2.30** Hiển thị tọa độ và kích thước vùng chọn `WxH+X+Y` (`FR-SEL-12`).
- [ ] **P2.31** Co giãn đối xứng điểm đối diện khi giữ Shift kéo handle (`FR-SEL-16`).

### Epic 13: Undo/Redo Advanced
- [ ] **P2.32** Cấu hình giới hạn số bước lưu lịch sử (`FR-UNDO-03`).
- [ ] **P2.33** Lưu toàn bộ danh sách chú thích vào mỗi snapshot (`FR-UNDO-04`).
- [ ] **P2.34** Hoàn tác việc di chuyển lớp lên/xuống (`FR-UNDO-06`).

### Epic 14: Export & Shortcuts Full
- [ ] **P2.35** Double-click vùng chọn để copy, lưu sau khi copy, copy đường dẫn file (`FR-OUT-06`–`FR-OUT-08`).
- [ ] **P2.36** Xuất byte PNG thô ra stdout và in geometry ra stdout (`FR-OUT-09`, `FR-OUT-10`).
- [ ] **P2.37** Mở ảnh bằng ứng dụng mặc định và chọn định dạng lưu PNG/JPG (`FR-OUT-11`, `FR-OUT-13`).
- [ ] **P2.38** Hiển thị thông báo Windows Toast khi lưu/copy thành công (`FR-OUT-15`, `FR-CFG-008`).
- [ ] **P2.39** Hỗ trợ đầy đủ các phím tắt còn lại: mở app khác, upload Imgur, side panel, color picker, select-all, delete, commit (`FR-SH-017`–`FR-SH-027`, `FR-SH-029`).

### Epic 16: Windows Integration & CLI Polish
- [ ] **P2.42** Console output cho các lệnh CLI, tray launcher, mở thư mục lưu (`FR-WIN-004`, `FR-SYS-004`, `FR-SYS-007`, `FR-SYS-021`).
- [ ] **P2.43** Import/export/reset cấu hình và hot-reload khi file thay đổi (`FR-SYS-011`, `FR-SYS-013`, `FR-SYS-014`).

---

## Báo cáo chi tiết theo task

- **Epic 7 (P2.04–P2.06):** `docs/3.Progress/Phase2/03_21_Epic7_Global_Hotkeys_Report.md` (global hotkeys PrintScreen, Snipping Tool, cấu hình phím tắt + nợ test)
- **Epic 8 (P2.07–P2.10):** `docs/3.Progress/Phase2/03_22_Epic8_Capture_Backend_Report.md` (backend WinRT WGC, CompositeCaptureService, fallback GDI, Per-Monitor V2 + nợ test)
- **Epic 9 (P2.11–P2.14):** `docs/3.Progress/Phase2/03_23_Epic9_Config_Editor_Report.md` (mở rộng AppConfig v1.0, migrate flameshot.ini, SettingsWindow, FileNameEditor + nợ test)
- **Epic 10 (P2.15–P2.17):** `docs/3.Progress/Phase2/03_24_Epic10_Pin_Widget_Report.md` (cửa sổ ghim Topmost không viền, tương tác drag/zoom/rotate/opacity, context menu copy/save/close)
- **Epic 11 (P2.20–P2.26):** `docs/3.Progress/Phase2/03_25_Epic11_Annotation_Tools_Advanced_Report.md` (công cụ Invert, Circle Counter auto-increment, ràng buộc góc/tỉ lệ, chỉnh kích thước phím/wheel, chọn và sửa chú thích)
