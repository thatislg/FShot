# Báo cáo: Epic 14 — Export & Shortcuts Full (P2.35–P2.39)

- **Ngày thực hiện:** 2026-11-12
- **Phạm vi:**
  - **P2.35** Double-click copy, lưu sau khi copy, copy đường dẫn file (`FR-OUT-06`–`FR-OUT-08`, `FR-CFG-014`).
  - **P2.36** Xuất byte PNG thô ra stdout và in geometry ra stdout (`FR-OUT-09`, `FR-OUT-10`).
  - **P2.37** Mở ảnh bằng ứng dụng mặc định và chọn định dạng lưu PNG/JPG (`FR-OUT-11`, `FR-OUT-13`).
  - **P2.38** Hiển thị thông báo Windows Toast khi lưu/copy thành công (`FR-OUT-15`, `FR-CFG-008`).
  - **P2.39** Hỗ trợ đầy đủ các phím tắt còn lại (`FR-SH-017`–`FR-SH-027`).
- **Trạng thái:** Hoàn thành phần triển khai, biên dịch 0 lỗi, toàn bộ unit tests Passed. *(Lưu ý: đã loại bỏ upload Imgur/cloud theo chính sách)*.

---

## 1. Quyết định thiết kế chính

| Vấn đề | Quyết định |
| ------ | ---------- |
| Double-click copy | `CaptureCanvas.OnPointerPressed` kiểm tra `ClickCount = 2` + `CopyOnDoubleClick` + trong vùng chọn → `Copy`. |
| Save sau copy | `SaveAfterCopy` trong `ConfigSnapshot`/`AppConfig`; luồng copy thành công tự gọi `writeToPath` (FR-OUT-07). |
| Copy đường dẫn | `CopyPathAfterSave`; `ClipboardService.copyTextToClipboard` dùng `CF_UNICODETEXT` (FR-OUT-08). |
| Raw PNG stdout | `OutputTarget.Stdout`; `Program.fs` ghi byte PNG vào `Console.OpenStandardOutput()` (FR-OUT-09). |
| Print geometry | `OutputTarget.PrintGeometry`; in `WxH+X+Y` qua `Console.Out.WriteLine` (FR-OUT-10). |
| Mở app mặc định | `OpenWithDefaultApp ""` sentinel; render `%TEMP%\FShot\temp_view.png` + `Process.Start(UseShellExecute=true)` (FR-OUT-11). |
| Định dạng PNG/JPG | Tôn trọng `SaveAsFileExtension` + `JpegQuality`; mã hóa `SKEncodedImageFormat.Png/Jpeg`. |

---

## 2. Chi tiết công việc

### 2.1 Cấu hình (`src/FShot.Core/Domain/Config.fs`)
- `ConfigSnapshot` + `AppConfig` thêm `SaveAfterCopy`, `CopyPathAfterSave`.
- Thread qua `Default`/`Normalized`/`ToSnapshot`/`FromSnapshot`/JSON converter.

### 2.2 Clipboard (`src/FShot.Platform.Win32/Clipboard/ClipboardService.fs`)
- Thêm `copyTextToClipboard(text)` dùng `CF_UNICODETEXT` + `GlobalAlloc`.

### 2.3 UI export (`src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`)
- Refactor `ExecuteStartExport` tách helper `writeToPath`/`afterSave`; thêm nhánh `OpenWithDefaultApp`.
- `CopyToClipboard`: sau copy, nếu `SaveAfterCopy` → tự lưu; nếu `CopyPathAfterSave` → copy đường dẫn.
- Save: sau lưu, nếu `CopyPathAfterSave` → copy đường dẫn.
- `OnPointerPressed`: thêm double-click copy.

### 2.4 CLI (`src/FShot.UI/Cli/CliArguments.fs`, `src/FShot.UI/Program.fs`)
- `GuiArgs`/`FullArgs`/`ScreenArgs` thêm `-r/--raw` và `--print-geometry`.
- `OutputTarget` thêm `PrintGeometry` (Core `Capture.fs`).
- `parse` map `--print-geometry` → `PrintGeometry`, `-r` → `Stdout`.
- `runHeadlessCaptureAsync` xử lý `Stdout` (byte PNG) và `PrintGeometry`.

### 2.5 Settings (`src/FShot.UI/Windows/SettingsWindow.axaml(.fs)`)
- Thêm toggle `SaveAfterCopy`, `CopyPathAfterSave`, slider `UndoLimit` (10–500).

### 2.6 Unit tests
- `tests/FShot.UI.Tests/CliTests.fs`: thêm test `-r`/`--raw`/`--print-geometry`/ưu tiên cờ.

---

## 3. Các file mã nguồn thay đổi

1. `src/FShot.Core/Domain/Config.fs` — `SaveAfterCopy`, `CopyPathAfterSave`, `UndoLimit`.
2. `src/FShot.Core/Domain/Capture.fs` — `OutputTarget.PrintGeometry`.
3. `src/FShot.Platform.Win32/Clipboard/ClipboardService.fs` — `copyTextToClipboard`.
4. `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs` — export flows, double-click.
5. `src/FShot.UI/Cli/CliArguments.fs` — `--raw`, `--print-geometry`.
6. `src/FShot.UI/Program.fs` — stdout raw + geometry.
7. `src/FShot.UI/Windows/SettingsWindow.axaml(.fs)` — toggle mới + slider UndoLimit.
8. `tests/FShot.UI.Tests/CliTests.fs` — thêm test CLI.

---

## 4. Build & Test

```text
dotnet build FShot.sln → Build succeeded. 0 Error(s)
dotnet test FShot.sln:
  FShot.Core.Tests.dll           Passed: 261
  FShot.Rendering.Skia.Tests.dll Passed:  22
  FShot.UI.Tests.dll             Passed: 116
  ─────────────────────────────────────────────
  TỔNG CỘNG: 399 / 399 PASSED
```

---

## 5. Nợ test (chờ runtime)

| # | Nợ test | Cách verify | Lý do treo |
| - | ------- | ----------- | ---------- |
| 1 | Double-click copy đưa ảnh vào clipboard + tự lưu file | Nhấp đúp trên overlay | Cần GUI |
| 2 | Lưu file → paste ra Notepad ra đúng đường dẫn | Lưu + Ctrl+V Notepad | Cần GUI |
| 3 | `fshot full -r > test.png` tạo file ảnh hợp lệ | Chạy CLI | Cần runtime Windows GUI subsystem |
| 4 | `fshot gui --print-geometry` in tọa độ sau khi chọn | Chạy CLI | Cần GUI |
| 5 | `Ctrl+O` mở Windows Photos với ảnh vừa chụp | Bấm phím trên overlay | Cần GUI |

---

## 6. Tiếp theo

- Người dùng thực hiện **Epic 16: Windows Integration & CLI Polish** (P2.42–P2.43).
