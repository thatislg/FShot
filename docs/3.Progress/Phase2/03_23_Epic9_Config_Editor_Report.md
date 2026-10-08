# Báo cáo: Epic 9 — Config Persistence & Editor (P2.11–P2.14)

- **Ngày thực hiện:** 2026-10-08
- **Phạm vi:**
  - **P2.11** Đọc/ghi cấu hình JSON đầy đủ v1.0 (`FR-CFG-001`, `FR-CFG-003`–`FR-CFG-005`, `FR-CFG-100`–`FR-CFG-209`).
  - **P2.12** Migrate/đọc cấu hình cũ từ `flameshot.ini` (`FR-WIN-006`).
  - **P2.13** Cửa sổ cài đặt (Config Editor) 4 tab (`FR-SYS-006`, `FR-CFG-100`–`FR-CFG-209`).
  - **P2.14** Trình chỉnh sửa mẫu tên file có preview token (`FR-CFG-003`).
- **Trạng thái:** Hoàn thành phần triển khai, biên dịch 0 lỗi, **295 / 295 unit tests Passed**. Runtime verification **treo lại** (chờ UI), xem mục 6.

---

## 1. Quyết định thiết kế chính

| Vấn đề | Quyết định |
| ------ | ---------- |
| Mở rộng `AppConfig` | Thêm ~25 trường v1.0 (General / Interface / Tool Defaults) + `CaptureBackend`, thread đầy đủ qua `Read`/`Write`/`Normalized`/`ToSnapshot`/`FromSnapshot`. |
| Schema migration mềm dẻo | JSON converter tự bù giá trị mặc định khi thiếu trường (permissive schema), không làm mất dữ liệu cũ. |
| Ghi nguyên tử | `ConfigStore` ghi `.tmp` → `File.Move(overwrite=true)` + `Flush(true)`. |
| Crash recovery | File JSON hỏng → sao lưu `.bad` rồi fallback `AppConfig.Default`. |
| Hot-reload | `ConfigStore.watchConfig` (FileSystemWatcher + debounce Timer). |
| Parser INI | `FlameshotIniParser` thuần F# (section/key-value/comment/mảng, tách dấu phẩy tôn trọng ngoặc đơn cho `rgb()`). |
| Migrator | `FlameshotConfigMigrator` chuẩn hóa màu (hex/rgb/ARGB), map 19 thuộc tính + button IDs. |
| Cửa sổ cài đặt | Code-behind (không MVVM framework), 4 tab + footer Apply/Save/Cancel/Reset, load/save qua `ConfigStore`. |

---

## 2. Chi tiết công việc

### 2.1 P2.11 — Cấu hình JSON đầy đủ
- **`Config.fs`:** thêm `SavePathFixed`, `SaveAsFileExtension`, `JpegQuality`, `CopyOnDoubleClick`, `SaveLastRegion`, `AllowMultipleGuiInstances`, `CaptureBackend`, `UiColor`, `ContrastUiColor`, `ContrastOpacity`, `PredefinedColorPaletteLarge`, `UserColors`, `Buttons`, `UiLanguage`, `FontFamily`, `DrawFontSize`, `DrawCircleCounterSize`, `DrawPixelateSize`, `DrawRectangleRadius`, `DrawMarkerSize` + clamp/normalize + JSON read/write (giúp `getInt`/`getByte`/`getStringList`).
- **`ConfigStore.fs`:** ghi nguyên tử, crash recovery, `watchConfig`.

### 2.2 P2.12 — Migrate Flameshot
- **`FlameshotIniParser.fs` (mới):** `parse`, `tryFind`/`tryFindBool`/`tryFindInt`/`tryFindFloat`/`tryFindList`.
- **`FlameshotConfigMigrator.fs` (mới):** `tryNormalizeColor`/`normalizeColor`, `mapButtonIds`, `normalizeLanguage`, `normalizePath`, `migrate`/`migrateFromString`/`migrateFromFile`.
- **`Program.fs`:** auto-import `fshot config --import-flameshot [path]` (headless, ghi config rồi thoát). Tự phát hiện khi khởi động lần đầu: *(nợ — xem mục 6 #4)*.

### 2.3 P2.13 — Cửa sổ cài đặt
- **`SettingsWindow.axaml` + `.fs` (mới):** 4 tab (General / Interface / Tool Defaults / Shortcuts) + footer (Reset/Apply/Save&Close/Cancel), folder picker, slider có nhãn, combo.
- **Wiring:** Tray "Cài đặt" → `showSettingsWindow()`; CLI `fshot config` → `App.OpenSettingsOnStartup`.

### 2.4 P2.14 — FileNameEditor
- **`FileNamePatternValidator` (Core/Export.fs):** ký tự cấm `\ / : * ? " < > |`, `isValid`, `findForbiddenChars`, `normalized` (fallback `fshot_%Y-%m-%d-%H%M%S`).
- **`FileNameEditor.axaml` + `.fs` (mới):** TextBox + 6 nút token (%Y %m %d %H %M %S) chèn tại caret + live preview (DispatcherTimer 1s) + hiển thị lỗi ký tự cấm.

### 2.5 Follow-up — Nối dây màu giao diện vào overlay chụp
- **`Config.fs`:** mở rộng `ConfigSnapshot` thêm `UiColor` / `ContrastUiColor` / `ContrastOpacity`, thread qua `ToSnapshot` / `FromSnapshot` / `Default`.
- **`CaptureCanvas.axaml.fs`:**
  - `RenderDimming` đọc độ mờ + màu nền từ `overlayState.Config` (thay vì hardcode đen alpha 180).
  - `RenderSelectionOverlay` đọc màu accent từ `Config.UiColor` cho viền ngoài/trong và tự sinh gradient sáng/tối cho knob handle (pha trắng/đen).
- Giờ đổi màu accent / màu tương phản / độ mờ trong Settings → Apply → overlay chụp kế tiếp áp dụng ngay (không cần restart).

---

## 3. Các file mã nguồn thay đổi

1. `src/FShot.Core/Domain/Config.fs` — mở rộng `AppConfig` + converter.
2. `src/FShot.Core/Domain/Export.fs` — thêm `FileNamePatternValidator`.
3. `src/FShot.Platform.Win32/Config/ConfigStore.fs` — atomic write + recovery + `watchConfig`.
4. `src/FShot.Platform.Win32/Config/FlameshotIniParser.fs` — **mới**.
5. `src/FShot.Platform.Win32/Config/FlameshotConfigMigrator.fs` — **mới**.
6. `src/FShot.UI/Windows/FileNameEditor.axaml(.fs)` — **mới**.
7. `src/FShot.UI/Windows/SettingsWindow.axaml(.fs)` — **mới**.
8. `src/FShot.UI/App.axaml.fs` — `showSettingsWindow`, `OpenSettingsOnStartup`.
9. `src/FShot.UI/Cli/CliArguments.fs` — subcommand `config` + `--import-flameshot`.
10. `src/FShot.UI/Program.fs` — import flameshot + `OpenSettingsOnStartup`.
11. Tests: `MigrationTests.fs` (mới), `CaptureBackendTests.fs` (mới), `ExportTests.fs` (validator), `CliTests.fs` (config), cập nhật `ConfigTests.fs`, `ConfigStoreTests.fs`.

---

## 4. Build & Test

```text
dotnet build FShot.sln   → Build succeeded. 0 Error(s)

dotnet test FShot.sln:
  FShot.Core.Tests.dll           Passed: 202
  FShot.Rendering.Skia.Tests.dll Passed:  22
  FShot.UI.Tests.dll             Passed:  71
  ─────────────────────────────────────────────
  TỔNG CỘNG: 295 / 295 PASSED
```

---

## 5. Nợ test (treo lại, chờ UI / runtime)

| # | Nợ test | Cách verify | Lý do treo |
| - | ------- | ----------- | ---------- |
| 1 | Mở cửa sổ cài đặt từ Tray, sửa từng tab, Lưu có hiệu lực tức thì | Chạy app, click "Cài đặt" | Cần GUI |
| 2 | `FileNameEditor` live preview cập nhật mỗi giây + cảnh báo ký tự cấm | Gõ mẫu có ký tự cấm trong UI | Cần GUI |
| 3 | Folder picker chọn thư mục lưu | Bấm "Duyệt..." | Cần GUI + StorageProvider |
| 4 | Auto-migrate khi khởi động lần đầu (chưa có `config.json` nhưng có `flameshot.ini`) | Đặt `flameshot.ini` mẫu, xóa `config.json`, khởi động | Chưa wire auto-detect vào `Program.main` (mới có `--import-flameshot`) |
| 5 | Hot-reload qua `ConfigStore.watchConfig` phát sự kiện | Sửa `config.json` bên ngoài khi app chạy | Cần runtime (hiện App vẫn dùng watcher riêng cho hotkey) |
| 6 | Reset Defaults có dialog xác nhận | Bấm "Khôi phục mặc định" | Chưa thêm dialog xác nhận (reset trực tiếp) |

---

## 6. Tiếp theo

- Người dùng thực hiện tài liệu thiết kế **Epic 10: Pin Widget** (P2.15–P2.17) hoặc các Epic tiếp theo.
- Khi có UI, quay lại giải quyết các mục nợ test ở mục 5.
