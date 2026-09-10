# AppLifecycle — Thiết kế chi tiết

> Tài liệu này mô tả vòng đời ứng dụng F-Shot UI và cách parse CLI.

---

## 1. Mục đích

`Program.fs` là điểm vào của ứng dụng. Nó:

- Parse tham số dòng lệnh.
- Quyết định chạy chế độ nào: chụp trực tiếp hoặc mở overlay.
- Khởi động Avalonia App nếu cần UI.

---

## 2. Các tham số CLI tối thiểu

| Tham số | Ý nghĩa |
|---------|---------|
| `full` | Chụp toàn màn hình có con trỏ |
| `screen -n ID` | Chụp màn hình theo ID |
| `gui` | Mở overlay tương tác |
| `-p PATH` | Lưu file |
| `-c` | Copy clipboard |
| `-d MS` | Delay milli-giây |
| `--last-region` | Dùng vùng cũ |
| `--accept-on-select` | Tự động xuất khi thả chuột |

---

## 3. Parse CLI thành CaptureRequest

Ví dụ:

```
fshot full -p screenshot.png -d 3000
```

→ `CaptureRequest`:
- Mode = FullScreen
- OutputTarget = File "screenshot.png"
- DelayMs = 3000

```
fshot gui
```

→ `CaptureRequest`:
- Mode = GuiInteractive
- OutputTarget = OpenGui

---

## 4. Luồng ứng dụng

### Chụp trực tiếp (full/screen)

1. Parse request.
2. Đợi delay.
3. Gọi `ICaptureService`.
4. Nếu `OutputTarget = File`: render và lưu.
5. Nếu `OutputTarget = Clipboard`: render và copy.
6. Thoát.

### Mở overlay (gui)

1. Parse request.
2. Khởi động Avalonia App.
3. Chụp toàn Virtual Screen.
4. Mở `CaptureOverlayWindow`.
5. Chờ người dùng tương tác.
6. Khi xuất: render và gọi Platform service.
7. Đóng cửa sổ, thoát.

---

## 5. Kết nối với code

- `src/FShot.UI/Program.fs`
- `src/FShot.UI/CLI/Args.fs` (nếu dùng Argu)
- `src/FShot.UI/App.fs` (khởi tạo Avalonia App)
