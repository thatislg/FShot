# Báo cáo: Epic 7 — Global Hotkeys (P2.04, P2.05, P2.06)

- **Ngày thực hiện:** 2026-10-08
- **Phạm vi:**
  - **P2.04** Đăng ký phím nóng toàn hệ thống (quyết định dùng `PrintScreen` thay vì `Win+Shift+X`).
  - **P2.05** Tích hợp phím `PrintScreen` & xử lý xung đột với Windows Snipping Tool.
  - **P2.06** Cấu hình và thay đổi phím tắt toàn cục qua `config.json` (dynamic rebinding).
- **Trạng thái:** Hoàn thành phần triển khai, biên dịch 0 lỗi 0 cảnh báo, **271 / 271 unit tests Passed**. Runtime verification **treo lại** chờ có UI (xem mục 6).

---

## 1. Mục tiêu công việc

1. Đăng ký phím nóng toàn hệ thống để kích hoạt chụp mà không cần mở cửa sổ (chạy nền daemon).
2. Chiếm phím `PrintScreen` và tắt việc Windows 11 tự chuyển phím này sang Snipping Tool.
3. Cho phép cấu hình/đổi phím tắt qua `config.json` và áp dụng ngay lập tức (hot-reload, không cần restart).

---

## 2. Quyết định thiết kế chính

| Vấn đề | Quyết định |
| ------ | ---------- |
| Phím tắt chính | Dùng `PrintScreen` (thay vì `Win+Shift+X` như dự kiến ban đầu), theo yêu cầu thực tế của người dùng. |
| Cơ chế bắt PrintScreen | **Low-level keyboard hook (`WH_KEYBOARD_LL`)** thay vì `RegisterHotKey`, vì `RegisterHotKey` với `VK_SNAPSHOT` hay bị Windows 11 chặn. |
| Các phím tắt khác | Dùng `RegisterHotKey` trên thread message loop riêng. |
| "Xóa app mặc định Windows" | Tắt redirect `PrintScreenKeyForSnippingEnabled = 0` trong registry (không gỡ app Snipping Tool). |
| Cấu hình | Lưu trong `config.json` dạng object `"hotkeys": { ... }`, chuỗi rỗng = tắt hành động. |
| Dynamic rebinding | `FileSystemWatcher` theo dõi `config.json`, gửi `WM_APP_REBIND` tới thread message loop để đăng ký lại. |

---

## 3. Chi tiết công việc đã thực hiện

### 3.1 P2.04 / P2.05 — Phím nóng PrintScreen & Snipping Tool

- `GlobalHotkeyService`: cài hook `SetWindowsHookEx(WH_KEYBOARD_LL)`, chạy message loop trên **thread nền**; bắt `VK_SNAPSHOT`, nuốt phím (trả `1`) để ngăn app khác nhận, chống auto-repeat.
- Dispatch an toàn sang Avalonia UI Thread qua `Dispatcher.UIThread.InvokeAsync`.
- Module `SnippingTool` ghi `HKCU\Control Panel\Keyboard\PrintScreenKeyForSnippingEnabled = 0`.
- Cleanup: `UnhookWindowsHookEx` + `PostThreadMessage(WM_QUIT)` khi thoát.

### 3.2 P2.06 — Cấu hình phím tắt toàn cục

- **Domain (`Config.fs`):** thêm `HotkeyAction` (`CaptureGui` / `CaptureFullScreen` / `CaptureScreenAtCursor`), `HotkeyConfig` `{ Action; Key; Enabled }`, module `HotkeyConfigs`, trường `AppConfig.Hotkeys` + JSON converter (đọc/ghi object `hotkeys`).
- **`HotkeyParser.fs` (mới):** parse chuỗi (`"PrintScreen"`, `"Win+Shift+X"`, `"Ctrl+Alt+S"`, `"F11"`…) ↔ modifiers + Virtual Key; `format` chuẩn hóa ngược; `isDangerous` chặn tổ hợp nguy hiểm.
- **`GlobalHotkeyService.Rebind`:** hủy đăng ký cũ, đăng ký mới không cần restart; phím PrintScreen vẫn do hook xử lý, phím khác qua `RegisterHotKey`.
- **`App.axaml.fs`:** dispatch theo `HotkeyAction` (GUI overlay / chụp full Virtual Screen / chụp màn hình con trỏ); `FileSystemWatcher` hot-reload; refactor headless capture thành `deliverCaptureResult` dùng chung.

---

## 4. Các file mã nguồn thay đổi

1. `src/FShot.Core/Domain/Config.fs` — thêm `HotkeyAction`, `HotkeyConfig`, `HotkeyConfigs`, trường `Hotkeys` + JSON converter.
2. `src/FShot.Platform.Win32/Hotkeys/GlobalHotkey.fs` — viết lại từ placeholder: P/Invoke, low-level hook, `RegisterHotKey`, `Rebind`, `SnippingTool`.
3. `src/FShot.Platform.Win32/Hotkeys/HotkeyParser.fs` — **mới**: parse/format/validate phím tắt.
4. `src/FShot.Platform.Win32/FShot.Platform.Win32.fsproj` — thêm `HotkeyParser.fs`.
5. `src/FShot.UI/App.axaml.fs` — dispatch hành động, hot-reload, refactor headless capture.
6. Tests: `HotkeyParserTests.fs` (mới), `GlobalHotkeyTests.fs` (mới), cập nhật `ConfigStoreTests.fs`, `ConfigTests.fs`.

---

## 5. Build & Test

```text
dotnet build FShot.sln   → Build succeeded. 0 Warning(s), 0 Error(s)

dotnet test FShot.sln:
  FShot.Core.Tests.dll           Passed: 199
  FShot.Rendering.Skia.Tests.dll Passed:  22
  FShot.UI.Tests.dll             Passed:  50
  ─────────────────────────────────────────────
  TỔNG CỘNG: 271 / 271 PASSED
```

- **HotkeyParserTests:** 11 test (parse hợp lệ, phím không hợp lệ, hoa thường, modifiers, format, dangerous).
- **GlobalHotkeyTests:** 3 test (SnippingTool registry, cờ modifier, VK code).
- **ConfigStore/ConfigTests:** cập nhật round-trip trường `Hotkeys`.

---

## 6. Nợ test (treo lại, chờ UI / runtime)

> Các mục dưới đây chưa thể verify tự động vì cần chạy app GUI thật. Tạm ghi nợ, sẽ thực hiện khi có UI cài đặt hoặc khi test thủ công.

| # | Nợ test | Cách verify | Lý do treo |
| - | ------- | ----------- | ---------- |
| 1 | Nhấn `PrintScreen` mở overlay FShot từ mọi app | Chạy `fshot` (daemon), nhấn PrtSc ở Edge/VS Code/Desktop | Cần GUI + bàn phím vật lý |
| 2 | Snipping Tool không còn chiếm PrtSc | Kiểm tra registry `PrintScreenKeyForSnippingEnabled = 0` + nhấn PrtSc | Cần môi trường Windows 11 thật |
| 3 | Dynamic rebinding hot-reload | Sửa `config.json` (vd `captureFullScreen = "Ctrl+Shift+F"`), nhấn tổ hợp mới không restart | Cần chạy daemon |
| 4 | `RegisterHotKey` cho phím không phải PrintScreen | Gán `captureFullScreen` / `captureScreenAtCursor` tổ hợp bất kỳ | Cần runtime |
| 5 | Phát hiện trùng lặp giữa 2 hành động | (chưa implement) | Nợ code nhỏ — bổ sung sau khi có UI cấu hình |
| 6 | Hotkey hoạt động khi ẩn tray icon (`disabledTrayIcon = true`) | Chạy daemon + tắt tray | Cần runtime |

---

## 7. Tiếp theo

- Người dùng thực hiện tài liệu thiết kế **Epic 8: Real Capture Backend & Mixed DPI**.
- Khi có UI cài đặt, quay lại giải quyết các mục nợ test ở mục 6.
