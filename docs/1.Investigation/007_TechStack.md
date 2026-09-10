# Lựa chọn công nghệ (Tech Stack)

> Tài liệu này ghi rõ các công nghệ, framework, thư viện và công cụ dùng trong F-Shot, cùng lý do chọn lựa và các câu hỏi cần quyết định.
> Không chứa code implementation mẫu.

---

## 1. Ngôn ngữ và runtime

| Công nghệ | Phiên bản đề xuất | Lý do chọn |
|-----------|-------------------|------------|
| .NET | 8.0 LTS | Ổn định, hỗ trợ dài hạn, hiệu năng tốt |
| F# | theo .NET 8 SDK | Phù hợp domain model, DU, immutable records, pattern matching |
| C# | theo .NET 8 SDK | Dùng cho phần P/Invoke phức tạp nếu cần (Win32, WinRT COM) |

### Phương châm

- Ưu tiên F# cho mọi domain logic.
- Chỉ dùng C# khi interop Win32/WinRT quá khó viết bằng F#.

---

## 2. UI Framework

| Công nghệ | Phiên bản đề xuất | Lý do chọn |
|-----------|-------------------|------------|
| Avalonia UI | 11.x | Cross-platform desktop UI, hỗ trợ SkiaSharp, borderless window, tray icon |
| Avalonia.Desktop | 11.x | Template ứng dụng desktop |
| Avalonia MVVM Toolkit | 11.x | MVVM cho config window và tray menu |

### Câu hỏi cần quyết định

- Dùng **ReactiveUI** hay **CommunityToolkit.Mvvm**? CommunityToolkit nhẹ hơn, ít ceremony.
- Có cần **Avalonia.Xaml.Behaviors** cho gesture phức tạp không?

---

## 3. Đồ họa và render

| Công nghệ | Phiên bản đề xuất | Lý do chọn |
|-----------|-------------------|------------|
| SkiaSharp | 2.x hoặc 3.x | Vẽ 2D hiệu năng cao, backing bitmap, path, text |
| Avalonia.Skia | tương ứng | Tích hợp Skia vào Avalonia |
| HarfBuzzSharp | đi kèm SkiaSharp | Hỗ trợ font/text nâng cao nếu cần |

### Phương châm render

- Dùng **backing bitmap**: vẽ toàn bộ scene xuống bitmap, sau đó hiển thị.
- Không tạo nhiều `Shape` Avalonia cho từng nét vẽ.
- Cache geometry của các chú thích đã commit.

### Câu hỏi cần quyết định

- SkiaSharp đặt trong `FShot.Core` hay tách project `FShot.Rendering.Skia`? -> tách riêng. 
- Có dùng `WriteableBitmap` của Avalonia hay chỉ dùng `SKCanvas` trong custom control?

---

## 4. Chụp màn hình Windows

| Công nghệ | Mục đích | Lưu ý |
|-----------|----------|-------|
| `Windows.Graphics.Capture` | Chụp màn hình hiện đại, đúng Mixed DPI, hỗ trợ nhiều màn hình | Cần Windows 10 1903+ |
| P/Invoke `user32.dll` / `gdi32.dll` | Fallback BitBlt, cursor position, monitor enumeration | Dùng khi `Graphics.Capture` không khả dụng |
| `WinRT.Interop` | Kết nối WinRT GraphicsCaptureItem với HWND | Có thể cần C# interop hoặc CsWin32 |

### Phương châm

- Ưu tiên `Windows.Graphics.Capture` vì hỗ trợ Mixed DPI và đa màn hình tốt hơn BitBlt.
- Thiết kế abstraction trong `FShot.Core` để sau này thay đổi capture backend không ảnh hưởng domain.

---

## 5. Hotkey và system integration

| Công nghệ | Mục đích |
|-----------|----------|
| `RegisterHotKey` (User32) | Global hotkey `Win+Shift+X` |
| Low-level keyboard hook | Bắt `PrintScreen` nếu cần |
| Avalonia TrayIcon | Tray icon và context menu |
| Registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` | Startup with Windows |

### Câu hỏi cần quyết định

- Có nên dùng low-level hook để bắt `PrintScreen` hay chỉ đăng ký `RegisterHotKey`? ->  chỉ đăng ký `RegisterHotKey`
- Hook toàn cục có thể bị antivirus cảnh báo; cần cân nhắc UX và bảo mật.

---

## 6. Test

| Công nghệ | Mục đích |
|-----------|----------|
| xUnit | Unit test cho `FShot.Core` |
| FsUnit | Matcher F# cho xUnit |
| SkiaSharp trong test | Render reference image để so sánh pixel nếu cần |
| Avalonia.Headless | UI test không cần window thật (tùy chọn) |

### Phương châm

- Viết unit test cho domain ngay từ đầu.
- Tránh UI test quá sớm vì tốn thời gian và dễ flaky.

---

## 7. CLI và parsing

| Công nghệ | Mục đích |
|-----------|----------|
| Argu (F#) hoặc System.CommandLine | Parse lệnh `fshot gui/full/screen/launcher/config` |
| Console output | Raw PNG, geometry stdout cho CLI mode |

### Câu hỏi cần quyết định

- Dùng `Argu` cho idiomatic F# hay `System.CommandLine` cho tính năng phức tạp hơn? -> Dùng Argu. 

---

## 8. Cấu hình và lưu trữ

| Công nghệ | Mục đích |
|-----------|----------|
| INI parser (tự viết hoặc thư viện) | Đọc `flameshot.ini` cũ để tương thích -> ko dùng ini. |
| System.Text.Json | Lưu config mới của F-Shot -> dùng JSON |
| `%APPDATA%\Roaming\FShot\` | Thư mục config và lịch sử upload |

### Phương châm

- Đọc được config cũ Flameshot để dễ migrate. -> ko sử dụng ini. 
- Ghi config mới bằng JSON để dễ maintain và merge.

---

## 9. CI/CD và build

| Công nghệ | Mục đích |
|-----------|----------|
| GitHub Actions | Build Windows, chạy test, tạo release |
| `dotnet publish` | Publish self-contained hoặc framework-dependent |
| Inno Setup hoặc MSIX (tùy chọn) | Tạo installer Windows |
| zip artifact | Phân phối portable |

### Câu hỏi cần quyết định

- Publish **self-contained** hay **framework-dependent**? Self-contained lớn hơn nhưng dễ phân phối.
- Có cần **AOT** để giảm startup time? Không ở MVP.

---

## 10. Tóm tắt stack đề xuất

```
F# (.NET 8)
├── Avalonia 11 (UI + windowing)
├── SkiaSharp (render 2D)
├── Windows.Graphics.Capture (screen capture)
├── P/Invoke User32/Gdi32/WinRT (platform integration)
├── xUnit + FsUnit (testing)
└── Argu / System.CommandLine (CLI)
```

---

## 11. Các quyết định cần chốt trước scaffolding

| Câu hỏi | Phương án cần cân nhắc |
|---------|------------------------|
| .NET version | .NET 8 LTS / .NET 9 |
| Avalonia minor version | 11.x exact |
| MVVM toolkit | CommunityToolkit.Mvvm / ReactiveUI |
| SkiaSharp location | In Core / Separate Rendering project |
| Capture backend | Windows.Graphics.Capture only / + BitBlt fallback |
| Config format | Read INI old / Write JSON new |
| Global hotkey method | RegisterHotKey / Low-level hook / Cả hai |
| Publish mode | Self-contained / Framework-dependent |

---

*Tài liệu này sẽ được cập nhật khi chốt các phiên bản cụ thể trong file project.*
