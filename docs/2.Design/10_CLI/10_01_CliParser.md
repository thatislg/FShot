# CLI Parser (P1.27)

> Tài liệu thiết kế parser dòng lệnh cho F-Shot.
> Triển khai bằng thư viện **Argu** trong project `FShot.UI`.

---

## Mục tiêu

Cung cấp các lệnh dòng lệnh cơ bản để chụp màn hình mà không cần mở GUI:

- `fshot gui` — mở overlay tương tác (mặc định).
- `fshot full` — chụp toàn bộ màn hình hiện tại.
- `fshot screen <index>` — chụp màn hình chỉ định theo chỉ số.
- `fshot --help` / `fshot --version`.

Các tùy chọn chung:

- `-p, --path <file>` — đường dẫn file để lưu ảnh.
- `-c, --clipboard` — sao chép ảnh vào clipboard.
- `-d, --delay-ms <ms>` — độ trễ trước khi chụp (mili-giây, clamp ≥ 0).

---

## Kiến trúc parser

```text
FShot.UI.Cli
├── CliArguments.fs
│   ├── GuiArgs      (subcommand flags)
   ├── FullArgs      (subcommand flags + values)
   ├── ScreenArgs    (subcommand flags + values)
   ├── CliArguments  (root commands)
   └── ParsedCliRequest
└── Program.fs
    ├── CliParser.parse
    ├── runHeadlessCaptureAsync
    └── runGuiAsync
```

### Tại sao dùng Argu?

- Idiomatic với F#: định nghĩa CLI bằng Discriminated Union.
- Tự động sinh usage/help.
- Hỗ trợ subcommands, alt command lines và type-safe values.

### Hạn chế và cách xử lý

Argu không hỗ trợ positional arguments trong subcommand một cách trực tiếp (ví dụ `screen 0`). Vì vậy:

- Subcommand `screen` vẫn được định nghĩa trong Argu để nhận diện và parse flags.
- Chỉ số màn hình (`0`) được parser bỏ qua (`ignoreUnrecognized = true`) và lấy lại từ raw `args` thủ công qua `parseScreenIndex`.

---

## Luồng thực thi

```text
argv
  │
  ▼
CliParser.parse
  ├── --help / --version → in usage / version → exit
  ├── gui                → ParsedCliRequest(GuiInteractive, delay, OpenGui)
  ├── full               → ParsedCliRequest(FullScreen, delay, File/Clipboard/OpenGui)
  └── screen <index>     → ParsedCliRequest(SingleScreen index, delay, File/Clipboard/OpenGui)
          │
          ▼
Program.main
  ├── Mode = GuiInteractive → runGuiAsync → Avalonia desktop lifetime
  └── Mode = Full/Screen    → runHeadlessCaptureAsync → capture + export
```

### Headless capture

1. `Async.Sleep` nếu `delay > 0`.
2. Gọi `WindowsCaptureService` theo mode (`CaptureCursorScreenAsync` cho full, `CaptureScreenAsync` cho screen).
3. Fallback sang `StubCaptureService` nếu thất bại.
4. Render toàn bộ ảnh màn hình qua `SceneComposer.renderExport` (vùng chọn = toàn bộ capture bounds).
5. Xuất ra file hoặc clipboard bằng Win32 native clipboard service.

---

## Ví dụ sử dụng

```powershell
# Mở GUI overlay
fshot gui

# Chụp toàn màn hình và lưu file
fshot full -p C:\Users\Me\Pictures\shot.png

# Chụp toàn màn hình, copy vào clipboard, đợi 3 giây
fshot full -c -d 3000

# Chụp màn hình thứ 2 và lưu file
fshot screen 1 -p D:\shot.png

# Hiển thị help
fshot --help
```

---

## Test

Unit tests nằm trong `tests/FShot.UI.Tests/CliTests.fs`, bao gồm:

- Default về `gui` khi args rỗng.
- Parse `gui -d <ms>`.
- Parse `full -c`, `full -p <path>`, `full -d <ms> -p <path>`.
- Parse `screen <index> -c`, `screen <index> -p <path>`.
- Clamp delay âm về 0.

---

## TODO / Phase tiếp theo

- P1.28: tích hợp delay vào cả chế độ GUI (hiện tại `gui -d` đã parse nhưng `App.axaml.fs` chưa dùng; đã dùng trong `App.axaml.fs` rồi).
- P1.29: đọc/ghi config JSON để lấy `SavePath`, `FilenamePattern`, v.v.
- Phase 2: thêm global hotkey `Win+Shift+X` gọi `fshot gui`.
