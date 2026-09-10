# UI.Avalonia — Tổng quan thiết kế

> Thư mục này thiết kế giao diện người dùng của F-Shot bằng Avalonia.  
> UI chỉ làm 3 việc: nhận input, hiển thị state, gọi platform service. Không chứa domain logic.

---

## 1. Mục đích

`UI.Avalonia` định nghĩa:

- **Cửa sổ overlay**: borderless, topmost, phủ toàn Virtual Screen.
- **Custom render control**: hiển thị bitmap từ `FShot.Rendering.Skia`.
- **Cửa sổ config** (v1.0).
- **Cửa sổ pin widget** (v1.0).
- **ViewModels** cho config, tray menu.

UI phải:

- Không tự ý thay đổi domain state.
- Chuyển input events thành commands gửi xuống `FShot.Core.State`.
- Hiển thị `RenderModel` từ Core.

---

## 2. Phạm vi thiết kế

### 2.1 Cửa sổ overlay

- Borderless, topmost.
- Phủ toàn Virtual Screen.
- Nhận tất cả input (mouse, keyboard).
- Hiển thị bitmap backing store.

### 2.2 Custom render control

- Dùng `Avalonia.Skia` hoặc `Image` control với `WriteableBitmap`.
- Cập nhật mỗi frame khi state thay đổi.
- Hỗ trợ high-DPI.

### 2.3 Input handling

- Mouse: press/move/release, wheel.
- Keyboard: key down/up, phím tắt.
- Double-click để copy (v1.0).

### 2.4 Cửa sổ khác

- **ConfigWindow**: v1.0.
- **PinWindow**: v1.0.
- **Tray icon**: hợp tác với `Platform.Win32`.

### 2.5 Không thuộc phạm vi MVP

- Config UI.
- Pin widget.
- Tray menu phức tạp.

---

## 3. Liên hệ với các phần khác

```
UI.Avalonia
    ├── Core.State: gửi input, nhận RenderModel
    ├── Rendering.Skia: hiển thị bitmap
    ├── Platform.Win32: capture, hotkey, clipboard, file dialog
    └── Core.Config: đọc cấu hình hiển thị
```

---

## 4. Dùng trong PoC

Trong Phase 0, UI.Avalonia cần kiểm chứng:

1. Mở cửa sổ borderless phủ Virtual Screen.
2. Hiển thị screenshot.
3. Vẽ overlay tối và vùng chọn.
4. Đạt ≥ 60 FPS.

---

## 5. Dùng trong MVP

Trong Phase 1, UI.Avalonia cần hỗ trợ:

- Overlay với selection.
- Nhận mouse/keyboard input.
- Gọi `FShot.Core.State` để cập nhật.
- Hiển thị RenderModel.
- Gọi save dialog khi cần.
- Hiển thị kết quả export.

---

## 6. Câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Custom control dùng `SKCanvasView` hay `Image` + `WriteableBitmap`? | Ảnh hưởng cách tích hợp Skia |
| Có dùng MVVM cho overlay không? | Overlay có thể dùng code-behind đơn giản hơn |
| Có dùng ReactiveUI/CommunityToolkit.Mvvm? | Ảnh hưởng complexity |
| Có cần message loop riêng cho hotkey? | Ảnh hưởng app lifecycle |
| Có dùng Avalonia styles hay custom theme? | Ảnh hưởng look & feel |

---

## 7. Các file sẽ được thiết kế trong thư mục này

| File | Nội dung |
|------|----------|
| `11_02_AppLifecycle.md` | Application lifetime, CLI entry, single instance |
| `11_03_OverlayWindow.md` | CaptureOverlayWindow design |
| `11_04_RenderControl.md` | Custom Skia/Avalonia render control |
| `11_05_InputHandling.md` | Mouse, keyboard, shortcuts |
| `11_06_ConfigWindow.md` | Config UI (v1.0) |
| `11_07_PinWindow.md` | Pin widget (v1.0) |
| `11_08_ViewModels.md` | MVVM cho config và tray |

---

## 8. Kết nối với code

Triển khai trong:

- `src/FShot.UI/App.axaml`
- `src/FShot.UI/App.axaml.fs`
- `src/FShot.UI/Program.fs`
- `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`
- `src/FShot.UI/Windows/CaptureOverlayWindow.axaml`
- `src/FShot.UI/Windows/CaptureOverlayWindow.axaml.fs`
- `src/FShot.UI/ViewModels/*.fs`

---

*UI.Avalonia là chủ đề cuối cùng trong phần Design. Sau khi chốt, chúng ta bắt đầu triển khai code từng phần một.*
