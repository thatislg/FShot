# Capture — Tổng quan thiết kế

> Thư mục này thiết kế cách F-Shot chụp màn hình trên Windows.  
> Capture là khâu đầu tiên của mọi luồng chụp, và là rủi ro kỹ thuật lớn nhất cần kiểm chứng trong PoC.

---

## 1. Mục đích

`Capture` định nghĩa:

- Các **chế độ chụp**: toàn màn hình, một màn hình, overlay tương tác, vùng đã chọn trước.
- **Yêu cầu chụp** (`CaptureRequest`): chế độ, độ trễ, vùng chọn ban đầu, accept-on-select.
- **Kết quả chụp** (`CaptureResult`): ảnh bitmap abstraction + metadata (bounds, scale factor).
- **Abstraction** giữa backend chụp (Win32/WinRT) và domain model.

Capture phải:

- Xử lý đúng **Mixed DPI** và nhiều màn hình.
- Không phụ thuộc UI framework.
- Trả về dữ liệu đủ để UI hiển thị overlay chính xác.

---

## 2. Phạm vi thiết kế

### 2.1 Chế độ chụp

| Chế độ | Mô tả | Dùng trong |
|--------|-------|------------|
| `FullScreen` | Chụp toàn bộ màn hình (hoặc Virtual Screen) | CLI `fshot full`, tray menu |
| `SingleScreen` | Chụp một màn hình cụ thể theo index hoặc màn hình có con trỏ | CLI `fshot screen -n` |
| `GuiInteractive` | Mở overlay để người dùng chọn vùng và vẽ | CLI `fshot gui`, hotkey |
| `LastRegion` | Dùng lại vùng chọn từ lần trước | CLI `--last-region` |

### 2.2 CaptureRequest

Các thông tin cần thiết:

- `Mode`: chế độ chụp.
- `DelayMs`: độ trễ trước khi chụp.
- `InitialSelection`: vùng chọn ban đầu (tùy chọn).
- `AcceptOnSelect`: tự động chấp nhận khi thả chuột.
- `OutputTarget`: xuất ra file, clipboard, stdout, hay mở GUI.

### 2.3 CaptureResult

Abstraction bitmap:

- `Pixels`: byte array chứa pixel data.
- `Width`, `Height`: kích thước ảnh.
- `Stride`: số byte mỗi dòng.
- `PixelFormat`: định dạng pixel (ví dụ RGBA32).
- `VirtualBounds`: hình chữ nhật trong Virtual Screen space.
- `ScaleFactor`: DPI scale của màn hình đã chụp.

### 2.4 Không thuộc phạm vi

- Không xử lý clipboard/file save ở đây (thuộc Export).
- Không vẽ overlay (thuộc Rendering).
- Không đăng ký hotkey (thuộc Platform.Win32).

---

## 3. Liên hệ với các phần khác

```
Capture
    ├── Geometry: VirtualBounds, scale factor
    ├── Selection: cung cấp screenshot cho overlay
    ├── Annotation: nền ảnh để vẽ chú thích
    ├── Export: crop từ CaptureResult theo selection
    ├── Rendering.Skia: convert byte array → SKBitmap
    └── Platform.Win32: triển khai Windows.Graphics.Capture
```

---

## 4. Dùng trong PoC

Trong Phase 0, Capture cần kiểm chứng:

1. `Windows.Graphics.Capture` chụp đúng Virtual Screen.
2. Mixed DPI: ảnh và tọa độ con trỏ khớp nhau.
3. Có thể chụp một màn hình cụ thể.
4. Byte array abstraction đủ để `Rendering.Skia` hiển thị.

---

## 5. Dùng trong MVP

Trong Phase 1, Capture cần hỗ trợ:

- `fshot full -p path -c -d`.
- `fshot screen -n id -p -c -d`.
- `fshot gui -d --region --last-region --accept-on-select`.
- Trì hoãn chụp (`--delay`).
- Chụp và chuyển ảnh vào overlay.

---

## 6. Câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Backend chính là `Windows.Graphics.Capture` hay BitBlt? | Ảnh hưởng Mixed DPI, hiệu năng, yêu cầu Windows |
| Có cần fallback BitBlt cho Windows cũ? | Tăng độ phức tạp nhưng mở rộng compatibility |
| Pixel format nên là RGBA32 hay BGRA32? | Ảnh hưởng convert sang Skia |
| Có cần lưu cursor chuột trong ảnh chụp? | Flameshot gốc có tùy chọn này |
| Làm thế nào xử lý capture thất bại? | UX khi bị từ chối quyền |

---

## 7. Các file sẽ được thiết kế trong thư mục này

| File | Nội dung |
|------|----------|
| `02_02_CaptureMode.md` | Các chế độ chụp |
| `02_03_CaptureRequest.md` | Yêu cầu chụp |
| `02_04_CaptureResult.md` | Kết quả chụp và bitmap abstraction |
| `02_05_WindowsGraphicsCapture.md` | Thiết kế backend WinRT |
| `02_06_FallbackBitBlt.md` | Thiết kế fallback nếu cần |
| `02_07_ScreenEnumeration.md` | Liệt kê màn hình và DPI |

---

## 8. Kết nối với code

Triển khai trong:

- `src/FShot.Core/Domain/Capture.fs`
- `src/FShot.Platform.Win32/Capture/GraphicsCapture.fs`
- `src/FShot.Platform.Win32/Capture/BitmapAdapter.fs`
- `tests/FShot.Core.Tests/Domain/CaptureTests.fs`

---

*Capture là chủ đề thứ hai sau Geometry. Sau khi chốt Capture, chúng ta chuyển sang Selection — vùng chọn trên ảnh đã chụp.*
