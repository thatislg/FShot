# Windows Startup Registration

Tài liệu này mô tả cơ chế đăng ký / hủy đăng ký F-Shot khởi động cùng Windows, tương ứng với `FR-CFG-006` và `FR-WIN-005`.

## 1. Mục tiêu

- Cho phép người dùng bật/tắt tùy chọn "Khởi động cùng Windows" trong cấu hình.
- Đồng bộ trạng thái Registry với trường `StartupLaunch` trong `AppConfig`.

## 2. Vị trí Registry

F-Shot ghi vào Registry người dùng hiện tại (HKCU), không cần quyền Administrator:

```
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
Value name: FShot
Value data: "<path_to_FShot.exe>"
```

## 3. Kiến trúc

```
┌─────────────────────────────────────┐
│         AppConfig.StartupLaunch     │
└─────────────┬───────────────────────┘
              │ load / save config
┌─────────────▼───────────────────────┐
│   FShot.Platform.Win32.Startup      │
│   StartupRegistration module        │
│  • getExecutablePath()              │
│  • buildStartupCommand()              │
│  • isStartupEnabled()               │
│  • setStartup(enable)               │
│  • syncStartup(desired)             │
└─────────────┬───────────────────────┘
              │ Registry
┌─────────────▼───────────────────────┐
│   HKCU\...\CurrentVersion\Run       │
└─────────────────────────────────────┘
```

## 4. Hành vi

- `isStartupEnabled()`: trả về `true` nếu value `"FShot"` tồn tại trong Registry key.
- `setStartup(true)`: ghi đường dẫn `FShot.exe` hiện tại vào Registry. Đường dẫn được bao trong dấu ngoặc kép.
- `setStartup(false)`: xóa value `"FShot"`.
- `syncStartup(desired)`: chỉ thay đổi Registry nếu trạng thái mong muốn khác trạng thái hiện tại.

## 5. Tích hợp

Trong `App.OnFrameworkInitializationCompleted`, sau khi nạp `AppConfig`, gọi:

```fsharp
let appConfig = ConfigStore.loadConfig()
match StartupRegistration.syncStartup appConfig.StartupLaunch with
| Ok () -> FShotLog.write "[Startup] Synced startup registration"
| Error err -> FShotLog.write (sprintf "[Startup] %s" err)
```

## 6. An toàn

- Mọi thao tác Registry đều được bọc trong `try/with`; lỗi trả về `Result<unit, string>`.
- Không can thiệp vào `HKLM` nên không yêu cầu quyền nâng cao.
- Khi ứng dụng được cài ở vị trí di động (portable), đường dẫn hiện tại vẫn chính xác nhờ `Process.GetCurrentProcess().MainModule.FileName`.

## 7. Tham khảo

- `src/FShot.Platform.Win32/Startup/StartupRegistration.fs`
- `src/FShot.Core/Domain/Config.fs` — trường `StartupLaunch`
- `FR-CFG-006`, `FR-WIN-005`: `docs/1.Investigation/003_SRS.md`
