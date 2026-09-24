# TrayIcon — Thiết kế chi tiết

Tài liệu này mô tả thiết kế biểu tượng khay hệ thống (system tray) và menu ngữ cảnh cho F-Shot trên Windows, tương ứng với Epic 6 / P2.01 (`FR-SYS-001`–`FR-SYS-008`).

## 1. Mục tiêu

- Hiển thị biểu tượng F-Shot thường trực trên Taskbar system tray.
- Click / double-click chuột trái vào icon: kích hoạt chụp ảnh màn hình tương tác GUI ngay lập tức.
- Menu ngữ cảnh (chuột phải) cung cấp các lệnh: chụp GUI, chụp theo màn hình, trình phóng nhanh, thông tin & phím tắt, cài đặt, mở thư mục ảnh chụp, thoát.
- Icon vector Kawaii được render động bằng SkiaSharp để luôn sắc nét trên mọi DPI.

## 2. Kiến trúc tổng quan

```
┌─────────────────────────────────────┐
│           FShot.UI.App              │
│  (IClassicDesktopStyleApplicationLifetime)  │
└─────────────┬───────────────────────┘
              │ khởi tạo khi lifetime sẵn sàng
┌─────────────▼───────────────────────┐
│     FShot.Platform.Win32.Tray       │
│        TrayIconService              │
│  • build icon bitmap (vector → PNG) │
│  • build NativeMenu                 │
│  • wire click / menu events         │
└─────────────┬───────────────────────┘
              │ dispatch TrayCommand
┌─────────────▼───────────────────────┐
│           FShot.UI.App              │
│  • mở / khởi động lại overlay       │
│  • chạy headless capture            │
│  • mở hộp thoại / file / folder     │
│  • shutdown ứng dụng                │
└─────────────────────────────────────┘
```

## 3. Vòng đời TrayIcon

1. **Khởi tạo**: trong `App.OnFrameworkInitializationCompleted`, khi lifetime là `IClassicDesktopStyleApplicationLifetime`, gọi `TrayIconService.createAndRegister(this, dispatcher)`.
2. **Icon**: `TrayIcon.Icon` được gán một `WindowIcon` tạo từ PNG bytes do `TrayIconBitmap` render từ vector Kawaii.
3. **Menu**: `TrayIcon.Menu` là một `NativeMenu` chứa các `NativeMenuItem` tương ứng `FR-SYS-002` đến `FR-SYS-008`.
4. **Click chuột trái**: event `TrayIcon.Clicked` kích hoạt chụp GUI. Double-click được phát hiện bằng cách theo dõi khoảng cách thời gian giữa hai lần click (vẫn kích hoạt chụp GUI).
5. **Hủy đăng ký**: khi ứng dụng shutdown, `TrayIcon` được remove khỏi `TrayIcons` collection để tránh icon bị "treo mờ" (ghost icon).

## 4. Cơ chế Avalonia NativeMenu

- `NativeMenuItem` trong Avalonia Desktop được ánh xạ sang native Win32 context menu.
- Một `NativeMenuItem` có thể chứa `Menu` con (`NativeMenu`) để tạo submenu.
- Mục không khả dụng (ví dụ chưa có `savePath`) vẫn hiển thị nhưng bị `disabled` (`IsEnabled <- false`).
- Separator được tạo bằng `NativeMenuItemSeparator`.

### Cấu trúc menu

| Header                        | Submenu | Command               | FR-SYS |
|-------------------------------|---------|-----------------------|--------|
| Chụp màn hình (GUI)           | —       | Mở overlay            | 002    |
| Chụp theo màn hình ▶          | Danh sách màn hình động | Chụp màn hình đã chọn | 003    |
| Trình phóng nhanh...          | —       | Chụp GUI với độ trễ 3 giây | 004    |
| —                             | —       | —                     | —      |
| Thông tin & Phím tắt          | —       | Mở hộp thoại About    | 005    |
| Cài đặt                       | —       | Mở file `config.json` | 006    |
| Mở thư mục ảnh chụp           | —       | `explorer.exe <savePath>` | 007 |
| —                             | —       | —                     | —      |
| Thoát F-Shot                 | —       | `lifetime.Shutdown()` | 008    |

## 5. Xử lý sự kiện click

- `TrayIcon.Clicked` handler kiểm tra thời điểm click trước đó:
  - Nếu cách nhau `< 500 ms` thì coi là double-click, vẫn dispatch `TrayCommand.GuiCapture`.
  - Ngược lại dispatch `TrayCommand.GuiCapture`.
- Không xử lý phân biệt single/double-click khác nhau; cả hai đều mở overlay chụp.

## 6. Xây dựng icon

- Icon gốc: `docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/tray-icon.svg`.
- Không nhúng trực tiếp SVG vì `WindowIcon` cần raster (PNG/ICO).
- Module `FShot.Rendering.Skia.Icons.TrayIconBitmap` vẽ lại icon bằng SkiaSharp:
  - Nền trong suốt.
  - Hình tròn đầu chim (`#7BD5F5`, viền `#3D2B1F`).
  - Mỏ cam (`#FB923C`, viền `#3D2B1F`).
  - Hai mắt chấm đen + điểm sáng trắng.
- Kích thước mặc định 64×64 px, có thể scale lên cho DPI cao.
- Encode thành PNG và truyền vào `WindowIcon(new MemoryStream(pngBytes))`.

## 7. Tích hợp vào ứng dụng

Trong `App.axaml.fs`:

```fsharp
match this.ApplicationLifetime with
| :? IClassicDesktopStyleApplicationLifetime as desktop ->
    // Khởi tạo overlay như cũ
    ...
    // Đăng ký tray icon
    let tray = TrayIconService.createAndRegister(this, desktop, onTrayCommand)
    ...
| _ -> ...
```

`onTrayCommand` là hàm dispatcher xử lý các `TrayCommand`:

- `GuiCapture` — khởi động lại overlay mới.
- `CaptureScreen index` — chạy headless capture màn hình đã chọn.
- `LaunchWithDelay ms` — mở overlay sau `ms`.
- `OpenAbout` — hiển thị hộp thoại thông tin.
- `OpenSettings` — `Process.Start` file `config.json`.
- `OpenSaveFolder` — `Process.Start("explorer.exe", savePath)`.
- `Exit` — `desktop.Shutdown()`.

## 8. Các cân nhắc an toàn

- `TrayIconService` không lưu trữ trực tiếp `IClassicDesktopStyleApplicationLifetime` để tránh vòng tham chiếu; nó nhận dispatcher function.
- Mọi tương tác UI đều chạy trên UI thread thông qua `Dispatcher.UIThread.InvokeAsync`.
- Menu được rebuild khi cấu hình thay đổi để phản ánh đúng `savePath` / màn hình hiện tại.

## 9. Tham khảo

- `FR-SYS-001`–`FR-SYS-008`: `docs/1.Investigation/003_SRS.md`
- Epic 6: `docs/3.Progress/03_00_Progres_Overview.md`
- Source: `src/FShot.Platform.Win32/Tray/TrayIcon.fs`
