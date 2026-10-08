# Báo cáo: Chi tiết Subtask Epic 5 & Tích hợp Toàn diện Bộ Icon Kawaii Claymorphism

- **Ngày thực hiện:** 2026-09-15
- **Phạm vi:**
  - Hoàn thiện chi tiết subtask cho Epic 5: Xuất dữ liệu & CLI (P1.24–P1.29) trong tiến độ tổng quan.
  - Tiếp nhận toàn bộ bộ sưu tập 27 vector SVG Kawaii Claymorphism tại `docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/`.
  - Tích hợp và nâng cấp toàn bộ hệ thống biểu tượng thanh công cụ (Toolbar: 10 Annotation Tools + 5 Actions) trong ứng dụng FShot sang giao diện Kawaii Claymorphism đa màu sắc, mềm mại, có vệt phản chiếu (specular highlight) và đường viền đậm nét đặc trưng.
- **Trạng thái:** Hoàn thành xuất sắc, biên dịch 0 lỗi 0 cảnh báo, 198 / 198 unit tests Passed (174 Core + 21 Skia + 3 UI). Đã commit và push nhánh `main` (`bd0e30e`).

---

## 1. Mục tiêu công việc

1. **Quy hoạch chi tiết Epic 5 (P1.24–P1.29):** Phân rã 6 task cấp cao thành các subtask rõ ràng, bao gồm phân giải tên file ngày giờ, logic lưu tức thì / Save As, copy PNG vào clipboard với DIB fallback, CLI parser với Argu, độ trễ chụp (`-d / --delay`) và cấu hình `%APPDATA%\FShot\config.json`.
2. **Tiếp nhận & Chuẩn hóa bộ Vector Kawaii:** Đọc và phân loại 27 file SVG Kawaii Claymorphism được bổ sung vào `docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/`.
3. **Hiện thực hóa giao diện Toolbar Kawaii trên Runtime:** Chuyển đổi toàn bộ icon từ phong cách đơn sắc (monochrome) sang cơ chế render đa lớp (multi-color / multi-layer) với bảng màu pastel, viền nét nâu đậm `#3D2B1F` bo tròn và điểm sáng 2.5D trong [ToolbarIcons.fs](file:///d:/Kojin/FShot/src/FShot.UI/SkiaCanvas/ToolbarIcons.fs) và [CaptureCanvas.axaml.fs](file:///d:/Kojin/FShot/src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs).

---

## 2. Chi tiết công việc đã thực hiện

### 2.1 Chi tiết hóa Subtask Epic 5 trong `03_00_Progres_Overview.md`

- **P1.24 (Lưu file `Ctrl+S`):** Bổ sung subtask phân giải mẫu tên file ngày tháng (`resolveFileName`), lưu tức thì khi có `savePath` cố định (`FR-OUT-02`), mã hóa PNG/JPG và unit tests.
- **P1.25 (Save As Fallback):** Chuẩn hóa luồng `SaveFilePickerAsync`, sinh `SuggestedFileName` động từ pattern, cấu hình filter đuôi file, bắt lỗi IO và xử lý Cancel.
- **P1.26 (Sao chép Clipboard PNG):** Chuẩn hóa đưa PNG bytes vào clipboard, bổ sung fallback DIB/Bitmap cho ứng dụng Windows bên thứ ba, tự động đóng overlay theo cấu hình.
- **P1.27 (CLI parser với Argu):** Thêm package `Argu`, khai báo Arguments DU, xử lý subcommand `gui` và `full` (headless direct capture), hiển thị trợ giúp chuẩn `--help`.
- **P1.28 (Cấu hình độ trễ `-d / --delay`):** Ánh xạ tham số CLI vào `CaptureRequest.DelayMs`, chèn `Async.Sleep` trước khi chụp và xử lý validation.
- **P1.29 (Nạp & lưu cấu hình JSON):** Xây dựng `AppConfig` trong Core, triển khai `ConfigStore` đọc/ghi `%APPDATA%\FShot\config.json`, tự động sinh file mặc định và kết nối vào startup.

---

### 2.2 Tích hợp Bộ Icon Kawaii vào Runtime Thanh công cụ

Trước đây, thanh công cụ sử dụng cơ chế vẽ đơn sắc (tất cả biểu tượng dùng chung màu `#3D2B1F` hoặc `#8A7B70`). Trong đợt cập nhật này, toàn bộ 15 thành phần thanh công cụ đã được chuyển đổi sang hình học vector 32×32 và cơ chế render đa màu đặc trưng phong cách Kawaii Claymorphism:

#### 10 Annotation Tools (Công cụ vẽ & chú thích):

| Tool                    | File SVG nguồn   | Màu sắc chủ đạo                            | Đặc trưng trực quan Kawaii Claymorphism                                                        |
| ----------------------- | ----------------- | ----------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| **SelectionTool** | `selection.svg` | Xám`#9CA3AF` / Trắng `#FFFFFF`            | Khung đứt bo tròn 4 góc mềm, con trỏ chuột mập lùn trắng viền nâu kèm vệt highlight. |
| **PencilTool**    | `pencil.svg`    | Vàng mật`#FDE047` / Hồng phấn `#F472B6` | Thân bút chì mập ú, đầu gôm hồng kẹo ngọt, ngòi chì gỗ viền đậm nét mượt.      |
| **LineTool**      | `line.svg`      | Vàng be pastel`#FDE68A`                      | Thước kẻ bo tròn góc, vạch đo độ dài xinh xắn, chấm tròn điểm nhấn đầu cuối.    |
| **ArrowTool**     | `arrow.svg`     | Cam san hô`#FB923C` / Trắng `#FFFFFF`     | Mũi tên uốn lượn mềm mại thân thiện, điểm nhấn vệt sáng phản chiếu (specular).     |
| **RectangleTool** | `rectangle.svg` | Xanh da trời`#93C5FD`                        | Khối chữ nhật bo tròn góc lớn phồng như kẹo dẻo, viền nâu hạt dẻ`#3D2B1F`.         |
| **CircleTool**    | `circle.svg`    | Vàng đào`#FBBF24`                          | Quả cầu tròn trịa phong cách đất sét nặn clay, đốm sáng bóng góc 45 độ.            |
| **MarkerTool**    | `marker.svg`    | Xanh bạc hà`#34D399`                        | Bút dạ quang nắp cài vát chéo, thân hình trụ ngắn bo tròn đáng yêu.                  |
| **TextTool**      | `text.svg`      | Tím oải hương`#C084FC`                    | Khối chữ "T" mập mạp, các góc bo tròn lớn không góc cạnh sắc nhọn.                    |
| **PixelateTool**  | `pixelate.svg`  | Tím pastel`#C084FC` & Tím đậm `#7C3AED` | Ma trận các ô vuông mosaic so le 2 tone tím phong cách pixel art dễ thương.               |
| **IconTool**      | `peeling-sticker.svg` | Vàng bơ `#FDE047` & Hồng pastel `#F472B6` | Nhãn dán ngôi sao bo phồng có khuôn mặt cười Kawaii, góc dưới phải hé bóc lớp dán. |

#### 5 Toolbar Actions (Thao tác thanh công cụ):

| Action                 | File SVG nguồn | Màu sắc chủ đạo                          | Đặc trưng trực quan Kawaii Claymorphism                                                                |
| ---------------------- | --------------- | --------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| **UndoAction**   | `undo.svg`    | Xanh dương`#60A5FA`                       | Mũi tên uốn cong 180° quay về trước, đầu mũi tên to bè mềm mại.                              |
| **RedoAction**   | `redo.svg`    | Tím lavender`#A78BFA`                      | Mũi tên uốn cong tiến về trước đối xứng, tạo nhịp điệu tương phản màu sắc.              |
| **CopyAction**   | `copy.svg`    | Trắng kem & Vàng nhạt`#FEF08A`           | Hai tờ giấy bo góc xếp chồng lệch, kẹp tài liệu vàng xinh xắn.                                  |
| **SaveAction**   | `save.svg`    | Xanh mint`#86EFAC` / Xanh rừng `#15803D` | Đĩa mềm mini 3.5 inch, cửa trượt kim loại trắng có rãnh, nhãn dán kem và dòng kẻ dữ liệu. |
| **CancelAction** | `cancel.svg`  | Đỏ dâu`#F87171`                          | Nút tròn dấu X phồng bong bóng, vệt sáng góc trên bên trái thể hiện độ bóng 2.5D.          |

---

## 3. Tổng hợp 27 Asset Kawaii SVG đã tiếp nhận & lưu trữ

Toàn bộ các tệp vector thiết kế đã được tổ chức và quản lý tại [docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/](file:///d:/Kojin/FShot/docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii):

```text
docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/
├── [Nhóm 1: Toolbar Annotation Tools]
│   ├── selection.svg       (Crop / Selection tool)
│   ├── pencil.svg          (Bút vẽ tự do)
│   ├── line.svg            (Đường thẳng)
│   ├── arrow.svg           (Mũi tên chỉ hướng)
│   ├── rectangle.svg       (Hình chữ nhật)
│   ├── circle.svg          (Hình tròn / elip)
│   ├── marker.svg          (Bút dạ quang)
│   ├── text.svg            (Chèn văn bản)
│   ├── pixelate.svg        (Làm mờ pixelate mosaic)
│   ├── blur.svg            (Làm mờ mịn Gaussian - chuẩn bị Phase 2)
│   ├── counter-bubble.svg  (Đánh số bước tuần tự - chuẩn bị Phase 2)
│   └── invert.svg          (Đảo màu âm bản - chuẩn bị Phase 2)
│
├── [Nhóm 2: Toolbar Action Buttons]
│   ├── undo.svg            (Hoàn tác)
│   ├── redo.svg            (Làm lại)
│   ├── copy.svg            (Sao chép clipboard)
│   ├── save.svg            (Lưu file đĩa)
│   ├── cancel.svg          (Đóng / Hủy chụp)
│   ├── peeling-sticker.svg (Dán sticker/nhãn dán Kawaii / IconTool)
│   ├── pin.svg             (Ghim ảnh nổi màn hình)
│   ├── open-app.svg        (Mở ảnh trong ứng dụng ngoài)
│   └── upload-img.svg      (Tải ảnh lên Imgur / Cloud)
│
└── [Nhóm 3: App Mascot, Branding & Windows Packaging]
    ├── fshot-bird.svg      (Linh vật chú chim FShot Kawaii)
    ├── app-logo.svg        (Logo ống kính máy ảnh tia sét)
    ├── tray-icon.svg       (Biểu tượng khay hệ thống Windows Taskbar)
    ├── Square44x44Logo.svg (Logo ứng dụng Start Menu / Taskbar)
    ├── Square150x150Logo.svg (Logo ứng dụng Medium Tile)
    ├── StoreLogo.svg       (Logo trang Microsoft Store)
    └── SplashScreen.svg    (Màn hình chào khởi động splash)
```

---

## 4. Các file mã nguồn thay đổi

1. [ToolbarIcons.fs](file:///d:/Kojin/FShot/src/FShot.UI/SkiaCanvas/ToolbarIcons.fs):
   - Cung cấp path vector 32×32 chuẩn hóa cho toàn bộ các công cụ và thao tác.
   - Bổ sung các đường path phụ trợ (highlights, details) phục vụ render đa lớp.
2. [CaptureCanvas.axaml.fs](file:///d:/Kojin/FShot/src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs):
   - Thay thế nhánh render đơn sắc cũ bằng các nhánh pattern match riêng biệt theo từng `item.Label`.
   - Sử dụng các lệnh vẽ trực tiếp của Avalonia `DrawingContext` (`DrawRectangle`, `DrawEllipse`, `DrawGeometry`, `DrawLine`) phối hợp các cọ vẽ `SolidColorBrush` và bút vẽ `Pen` có bo góc (`LineCap.Round`, `LineJoin.Round`).
   - Tự động hạ độ mờ (`alpha = 0x66uy`) khi nút ở trạng thái `disabled`.
3. [03_00_Progres_Overview.md](file:///d:/Kojin/FShot/docs/3.Progress/03_00_Progres_Overview.md):
   - Cập nhật liên kết tài liệu báo cáo và chi tiết 6 task Epic 5.

---

## 5. Kết quả kiểm thử & Build

### 5.1 Kiểm thử tự động (Unit Tests)

```text
Test run for FShot.Rendering.Skia.Tests.dll:
  Passed!  - Failed: 0, Passed:  21, Skipped: 0, Total:  21, Duration: 267 ms

Test run for FShot.UI.Tests.dll:
  Passed!  - Failed: 0, Passed:   3, Skipped: 0, Total:   3, Duration: 345 ms

Test run for FShot.Core.Tests.dll:
  Passed!  - Failed: 0, Passed: 174, Skipped: 0, Total: 174, Duration:  68 ms

================================================================================
TỔNG CỘNG: 198 / 198 tests PASSED (Tỉ lệ thành công: 100%)
================================================================================
```

### 5.2 Lịch sử Git

- **Commit:** `bd0e30e` (`feat(ui): integrate Kawaii Claymorphism icon set into toolbar rendering and assets`)
- **Remote:** Đã đẩy thành công lên nhánh `origin/main`.
