# Platform.Win32 — Tổng quan thiết kế

> Thư mục này thiết kế tích hợp hệ thống Windows: capture, hotkey, tray icon, clipboard, file system, startup.  
> Đây là project duy nhất chứa side effect và P/Invoke/WinRT interop.

---

## 1. Mục đích

`Platform.Win32` cung cấp:

- **Capture**: triển khai `Windows.Graphics.Capture` + fallback.
- **Hotkeys**: đăng ký phím nóng toàn cục.
- **Tray icon**: hiển thị icon và menu trong system tray.
- **Clipboard**: copy ảnh vào clipboard Windows.
- **FileSystem**: ghi file, mở save dialog.
- **ConfigStore**: đọc/ghi file config tại `%APPDATA%`.
- **Startup**: đăng ký chạy khi đăng nhập.

Project này phải:

- Triển khai các abstraction định nghĩa trong `FShot.Core`.
- Không chứa domain logic.
- Có thể viết bằng F# hoặc C# cho phần interop phức tạp.

---

## 2. Phạm vi thiết kế

### 2.1 Capture

- Sử dụng `Windows.Graphics.Capture` API.
- Chuyển đổi `SoftwareBitmap` / `Direct3D` sang byte array abstraction.
- Liệt kê màn hình và DPI.
- Fallback BitBlt nếu cần (tùy chọn).

### 2.2 Hotkeys

- `RegisterHotKey` cho `Win+Shift+X`.
- Low-level keyboard hook cho `PrintScreen` (tùy chọn).
- Có thể cần message loop riêng.

### 2.3 Tray icon

- Sử dụng Avalonia `TrayIcon` hoặc Win32 `NotifyIcon`.
- Menu: Capture, Launcher, Config, Open save path, Exit.

### 2.4 Clipboard

- Copy bitmap dạng PNG/JPG vào clipboard.
- Copy text path (v1.x).

### 2.5 FileSystem

- Save dialog (Avalonia `StorageProvider`).
- Ghi file tự động theo pattern.
- Mở thư mục save path.

### 2.6 ConfigStore

- Đường dẫn `%APPDATA%\Roaming\FShot\`.
- Đọc INI cũ Flameshot, ghi JSON mới.

### 2.7 Startup

- Registry key `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

---

## 3. Liên hệ với các phần khác

```
Platform.Win32
    ├── Core.Capture: triển khai CaptureResult
    ├── Core.Export: thực hiện IO
    ├── Core.Config: đọc/ghi file
    ├── Rendering.Skia: nhận byte array để copy clipboard
    └── UI: gọi các service này
```

---

## 4. Dùng trong PoC

Trong Phase 0, Platform.Win32 cần kiểm chứng:

1. `Windows.Graphics.Capture` hoạt động.
2. Chuyển đổi bitmap sang abstraction.
3. Liệt kê màn hình đúng DPI.

Hotkey, tray icon, clipboard, startup có thể bỏ qua ở PoC.

---

## 5. Dùng trong MVP

Trong Phase 1, Platform.Win32 cần hỗ trợ:

- Capture (full, screen, gui).
- Clipboard copy.
- File save / save dialog.
- Config read/write.

Tray icon, global hotkey, startup thuộc v1.0.

---

## 6. Câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Dùng F# hay C# cho P/Invoke phức tạp? | Ảnh hưởng maintainability |
| Có dùng `Windows.Graphics.Capture` hay cần CsWin32? | Ảnh hưởng cách triển khai WinRT interop |
| Tray icon dùng Avalonia hay Win32 native? | Ảnh hưởng UI consistency |
| Có chạy daemon nền cho hotkey không? | Ảnh hưởng kiến trúc app lifecycle |
| Có hỗ trợ Windows 10/11 only hay cả Windows 7? | Ảnh hưởng capture backend |

---

## 7. Các file sẽ được thiết kế trong thư mục này

| File | Nội dung |
|------|----------|
| `10_02_CaptureAdapter.md` | Windows.Graphics.Capture + BitmapAdapter |
| `10_03_ScreenEnumeration.md` | Liệt kê màn hình và DPI |
| `10_04_GlobalHotkey.md` | RegisterHotKey và PrtSc hook |
| `10_05_TrayIcon.md` | Tray icon và menu |
| `10_06_Clipboard.md` | Copy ảnh/text vào clipboard |
| `10_07_FileSystem.md` | Save dialog và file write |
| `10_08_ConfigStore.md` | Đọc/ghi config |
| `10_09_Startup.md` | Đăng ký khởi động cùng Windows |

---

## 8. Kết nối với code

Triển khai trong:

- `src/FShot.Platform.Win32/Capture/*.fs`
- `src/FShot.Platform.Win32/Hotkeys/*.fs`
- `src/FShot.Platform.Win32/Tray/*.fs`
- `src/FShot.Platform.Win32/Clipboard/*.fs`
- `src/FShot.Platform.Win32/FileSystem/*.fs`
- `src/FShot.Platform.Win32/Config/*.fs`
- `src/FShot.Platform.Win32/Startup/*.fs`

---

*Platform.Win32 là chủ đề thứ mười. Sau khi chốt, chúng ta chuyển sang UI.Avalonia — giao diện người dùng.*
