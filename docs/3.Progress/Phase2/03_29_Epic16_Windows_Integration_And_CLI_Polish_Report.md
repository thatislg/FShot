# Báo cáo: Epic 16 — Windows Integration & CLI Polish (P2.42–P2.43)

- **Ngày thực hiện:** 2026-11-12
- **Phạm vi:**
  - **P2.42** Console output cho CLI, tray launcher, mở thư mục lưu (`FR-WIN-004`, `FR-SYS-004`, `FR-SYS-007`, `FR-SYS-021`).
  - **P2.43** Import/export/reset cấu hình và hot-reload khi file thay đổi (`FR-SYS-011`, `FR-SYS-013`, `FR-SYS-014`).
- **Trạng thái:** Hoàn thành phần triển khai, biên dịch 0 lỗi, toàn bộ unit tests Passed. *(Verify runtime chờ người dùng test GUI/CLI)*.

---

## 1. Quyết định thiết kế chính

| Vấn đề | Quyết định |
| ------ | ---------- |
| GUI SubSystem không in text ra console | `NativeConsole.AttachConsole(ATTACH_PARENT_PROCESS = -1u)` + redirect `Console.SetOut/SetError` sang stream console (FR-WIN-004). |
| Tray launcher | `TrayCommand.OpenLauncher` mở `LauncherWindow` (ComboBox chế độ + độ trễ + màn hình); callback `LauncherRuntime.startCaptureCallback` dispatch về `App.LaunchCapture`. |
| Mở thư mục lưu | `openSaveFolder` tự tạo thư mục nếu chưa tồn tại trước khi `explorer.exe`; thêm CLI `fshot config --open-dir`. |
| Hot-reload | `startConfigWatcher` (debounce 300ms) reload `App.ConfigSnapshot` + `applyHotkeyConfig` (màu vẽ, độ dày nét, rebind phím nóng). |
| Import/export/reset | `ConfigStore.transferConfig` (dùng chung import/export, tham số hóa đường dẫn để test được) + `resetConfigTo`; CLI `config --export/--import/--reset`. |

---

## 2. Chi tiết công việc

### 2.1 ConfigStore (`src/FShot.Platform.Win32/Config/ConfigStore.fs`)
- Thêm `transferConfig(source, target)` — nạp + xác thực JSON, ghi bản chuẩn hóa sang đích.
- Thêm `exportConfigTo`, `importConfigFrom`, `resetConfigTo`, `resetConfig`.

### 2.2 CLI (`src/FShot.UI/Cli/CliArguments.fs`, `src/FShot.UI/Program.fs`)
- `ConfigArgs` thêm `Open_Dir`, `Export`, `Import`, `Reset`.
- Thêm DU `ConfigAction` (`OpenSaveDir`/`ExportConfig`/`ImportConfig`/`ResetConfig`) và field `ConfigAction` trong `ParsedCliRequest`.
- `Program.fs`: `NativeConsole.AttachConsole` + `tryAttachConsole`; `runConfigAction` xử lý 4 hành động headless; gọi trước single-instance.

### 2.3 Launcher (`src/FShot.UI/Windows/LauncherWindow.axaml(.fs)` mới)
- ComboBox chế độ (GUI/Full/Màn hình cụ thể) + độ trễ (0/3/5/10s) + danh sách màn hình.
- `OnStartClick` dựng `CaptureRequest` + `Rect option`, gọi `LauncherRuntime.startCaptureCallback`, đóng hộp thoại.

### 2.4 Tray & App (`src/FShot.Platform.Win32/Tray/TrayIcon.fs`, `src/FShot.UI/App.axaml.fs`)
- `TrayCommand.OpenLauncher`; menu "Trình phóng nhanh" dispatch `OpenLauncher`.
- `App.ShowLauncherWindow`/`App.LaunchCapture`; `openSaveFolder` tự tạo thư mục; `startConfigWatcher` reload full config.

### 2.5 Settings (`src/FShot.UI/Windows/SettingsWindow.axaml(.fs)`)
- Thêm nút "Nhập..." / "Xuất..." (file picker) + `OnImportClick`/`OnExportClick`.
- `OnResetClick` hiển thị hộp thoại xác nhận `ConfirmAsync` (Yes/No) trước khi `resetConfig`.

### 2.6 Unit tests
- `CliTests.fs`: 4 test parse `--open-dir`/`--export`/`--import`/`--reset`.
- `ConfigStoreTests.fs`: 3 test `transferConfig` round-trip, từ chối JSON hỏng, `resetConfigTo`.

---

## 3. Các file mã nguồn thay đổi

1. `src/FShot.Platform.Win32/Config/ConfigStore.fs` — transfer/export/import/reset.
2. `src/FShot.UI/Cli/CliArguments.fs` — `ConfigArgs` + `ConfigAction` + parse.
3. `src/FShot.UI/Program.fs` — `AttachConsole`, `runConfigAction`.
4. `src/FShot.UI/Windows/LauncherWindow.axaml(.fs)` — **mới**.
5. `src/FShot.Platform.Win32/Tray/TrayIcon.fs` — `OpenLauncher`.
6. `src/FShot.UI/App.axaml.fs` — launcher dispatch, open-save-folder, hot-reload.
7. `src/FShot.UI/Windows/SettingsWindow.axaml(.fs)` — import/export/reset + confirm.
8. `src/FShot.UI/FShot.UI.fsproj` — đăng ký LauncherWindow.
9. `tests/FShot.UI.Tests/CliTests.fs`, `ConfigStoreTests.fs` — test mới.

---

## 4. Build & Test

```text
dotnet build FShot.sln → Build succeeded. 0 Error(s)
dotnet test FShot.sln:
  FShot.Core.Tests.dll           Passed: 261
  FShot.Rendering.Skia.Tests.dll Passed:  22
  FShot.UI.Tests.dll             Passed: 123
  ─────────────────────────────────────────────
  TỔNG CỘNG: 406 / 406 PASSED
```

---

## 5. Nợ test (chờ runtime)

| # | Nợ test | Cách verify | Lý do treo |
| - | ------- | ----------- | ---------- |
| 1 | `fshot --help` in text trực tiếp ra PowerShell | Chạy CLI từ terminal | Cần GUI subsystem + console cha |
| 2 | Launcher trễ 3 giây → đếm ngược mượt | Mở tray → Trình phóng nhanh | Cần GUI |
| 3 | "Mở thư mục ảnh chụp" tạo thư mục + mở Explorer | Click tray menu | Cần GUI |
| 4 | Sửa `config.json` bằng Notepad → app nhận màu mới ngay | Chạy daemon + sửa file | Cần GUI daemon |
| 5 | Export → reset → import khôi phục nguyên vẹn | Settings window | Cần GUI |

---

## 6. Tiếp theo

- Người dùng verify runtime 5 mục nợ test trên, sau đó commit Epic 16.
