# GlobalHotkey — Thiết kế chi tiết

> Thiết kế phím nóng toàn cục dùng `PrintScreen` và xử lý xung đột với Snipping Tool.

## 1. Quyết định phím tắt

- **Phím nóng chính:** `PrintScreen` (PrtSc / `VK_SNAPSHOT = 0x2C`).
- **Không dùng** `Win+Shift+X` như dự kiến ban đầu — người dùng chỉ muốn phím PrintScreen, tương tự Flameshot/ShareX.
- Các phím tắt cấu hình được (P2.06) sẽ bổ sung sau.

## 2. Vì sao dùng low-level keyboard hook

`RegisterHotKey` với `VK_SNAPSHOT` thường thất bại trên Windows 11 vì hệ điều hành
bảo vệ phím PrintScreen để chuyển hướng sang Snipping Tool. Vì vậy F-Shot dùng
**Low-Level Keyboard Hook (`WH_KEYBOARD_LL`)** thay cho `RegisterHotKey`:

- Bắt trực tiếp `WM_KEYDOWN/WM_KEYUP` của `VK_SNAPSHOT`.
- Trả về `1` để nuốt phím, ngăn Snipping Tool / ứng dụng khác nhận.
- Chỉ xử lý đúng `VK_SNAPSHOT`, các phím khác chuyển tiếp qua `CallNextHookEx`.

## 3. Kiến trúc luồng (thread-safe)

1. `GlobalHotkeyService.Start()` cài hook `SetWindowsHookEx(WH_KEYBOARD_LL, ...)` và khởi
   động một **thread nền** chạy message loop (`GetMessage`/`TranslateMessage`/`DispatchMessage`).
   Hook được OS gọi về trên chính thread này.
2. Khi nhận `PrintScreen` keydown, callback dispatch sang Avalonia UI Thread qua
   `Dispatcher.UIThread.InvokeAsync` rồi gọi `onHotkey` (mở overlay chụp).
3. `GlobalHotkeyService.Dispose()` gọi `UnhookWindowsHookEx` và `PostThreadMessage(WM_QUIT)`
   để dừng message loop sạch sẽ.

## 4. Vô hiệu hóa Snipping Tool

Module `SnippingTool` ghi `HKCU\Control Panel\Keyboard\PrintScreenKeyForSnippingEnabled = 0`
để tắt việc Windows 11 tự chuyển phím PrintScreen sang Snipping Tool (`FR-SYS-020`, `FR-WIN-003`).

## 5. Phím tắt cấu hình được (P2.06)

- **Hành động:** `CaptureGui` (overlay tương tác), `CaptureFullScreen` (chụp toàn Virtual Screen),
  `CaptureScreenAtCursor` (chụp màn hình chứa con trỏ).
- **Cấu hình** trong `config.json` dạng object:
  ```json
  "hotkeys": {
    "captureGui": "PrintScreen",
    "captureFullScreen": "",
    "captureScreenAtCursor": ""
  }
  ```
  Chuỗi rỗng = tắt hành động.
- **Phân giải:** `HotkeyParser.parse` biến chuỗi (`"Win+Shift+X"`, `"Ctrl+Alt+S"`, `"F11"`...) thành
  modifiers + Virtual Key; `format` chuyển ngược lại chuỗi chuẩn hóa.
- **Cơ chế:** phím PrintScreen (không modifier) do low-level hook xử lý; các phím khác đăng ký qua
  `RegisterHotKey` trên thread message loop.
- **Dynamic rebinding:** `FileSystemWatcher` theo dõi `config.json`, khi thay đổi gửi `WM_APP_REBIND`
  tới thread message loop để hủy đăng ký cũ và đăng ký mới (không cần restart).
- **Validation:** chặn tổ hợp nguy hiểm (`Ctrl+Alt+Delete`, `Win+L`, `Alt+Tab`, `Win+D`...) qua `HotkeyParser.isDangerous`.
