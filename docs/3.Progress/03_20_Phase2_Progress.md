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
      - [x] **Tab 3 — Công cụ mặc định (Tool Defaults):** Bảng quản lý màu tùy chỉnh (Color Palette: thêm màu mới, xóa màu, chọn mã hex), thanh trượt đặt độ dày nét vẽ mặc định (1–50px), cỡ chữ mặc định, bán kính bo góc rectangle, kích thước mosaic pixelate.
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
- [ ] **P2.15** Cửa sổ ghim ảnh Topmost không viền (`FR-PIN-01`).
- [ ] **P2.16** Di chuyển, thu phóng, điều chỉnh độ trong suốt và xoay ảnh ghim (`FR-PIN-02`–`FR-PIN-05`).
- [ ] **P2.17** Menu ngữ cảnh của cửa sổ ghim: copy, save, close (`FR-PIN-06`–`FR-PIN-08`).

### Epic 11: Annotation Tools Advanced
- [ ] **P2.20** Công cụ đảo ngược màu (Invert) trong vùng chỉ định (`FR-ANN-09`).
- [ ] **P2.21** Bong bóng đếm số tự động tăng (Circle Counter) (`FR-ANN-10`, `FR-UNDO-005`).
- [ ] **P2.22** Ràng buộc góc 45°/90° khi vẽ line/arrow/marker bằng `Ctrl` (`FR-ANN-11`).
- [ ] **P2.23** Giữ tỉ lệ 1:1 khi vẽ rectangle/circle bằng `Ctrl` (`FR-ANN-12`).
- [ ] **P2.24** Nhập số để đặt chính xác kích thước công cụ (`FR-ANN-13`).
- [ ] **P2.25** Lăn chuột để tăng/giảm độ dày nét vẽ (`FR-ANN-14`).
- [ ] **P2.26** Chọn, di chuyển hoặc sửa chú thích cũ (`FR-ANN-18`, `FR-ANN-19`, `FR-ANN-20`, `FR-ANN-21`).

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
