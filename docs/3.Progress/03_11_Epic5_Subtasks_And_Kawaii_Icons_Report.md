# Báo cáo: Chi tiết Subtask Epic 5 & Tích hợp Icon Kawaii (Arrow, Pixelate, Save)

- **Ngày thực hiện:** 2026-09-15
- **Phạm vi:** 
  - Hoàn thiện chi tiết subtask cho Epic 5: Xuất dữ liệu & CLI (P1.24–P1.29) trong tiến độ tổng quan.
  - Tích hợp thử nghiệm và render vector đa màu sắc các icon Kawaii Claymorphism từ `docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/` vào toolbar thực tế của phần mềm (`ArrowTool`, `PixelateTool`, `SaveAction`).
- **Trạng thái:** Hoàn thành, build thành công, 198 / 198 tests pass (174 Core + 21 Skia + 3 UI).

---

## 1. Mục tiêu công việc

1. **Quy hoạch chi tiết Epic 5:** Phân rã toàn bộ 6 task cấp cao của Epic 5 (P1.24 đến P1.29) thành các subtask rõ ràng, bao gồm các bước từ hoàn thiện tài liệu thiết kế, triển khai logic domain, platform IO, tích hợp giao diện và kịch bản verify runtime trên Windows.
2. **Tích hợp Icon Kawaii vào Runtime:** Chuyển đổi các icon vector Kawaii 2.5D nhiều màu sắc từ catalog thiết kế (`kawaii/`) sang hệ tọa độ lưới 32×32 của toolbar, đưa vào `ToolbarIcons.fs` và nâng cấp bộ render trong `CaptureCanvas.axaml.fs` để hiển thị màu sắc đầy đủ thay vì đơn sắc.

---

## 2. Chi tiết công việc đã thực hiện

### 2.1 Chi tiết hóa Subtask Epic 5 trong `03_00_Progres_Overview.md`
- **P1.24 (Lưu file `Ctrl+S`):** Bổ sung subtask phân giải mẫu tên file ngày tháng (`resolveFileName`), lưu tức thì khi có `savePath` cố định (`FR-OUT-02`), mã hóa PNG/JPG và unit tests.
- **P1.25 (Save As Fallback):** Chuẩn hóa luồng `SaveFilePickerAsync`, sinh `SuggestedFileName` động từ pattern, cấu hình filter đuôi file, bắt lỗi IO và xử lý Cancel.
- **P1.26 (Sao chép Clipboard PNG):** Chuẩn hóa đưa PNG bytes vào clipboard, bổ sung fallback DIB/Bitmap cho ứng dụng Windows bên thứ ba, tự động đóng overlay theo cấu hình.
- **P1.27 (CLI parser với Argu):** Thêm package `Argu`, khai báo Arguments DU, xử lý subcommand `gui` và `full` (headless direct capture), hiển thị trợ giúp chuẩn `--help`.
- **P1.28 (Cấu hình độ trễ `-d / --delay`):** Ánh xạ tham số CLI vào `CaptureRequest.DelayMs`, chèn `Async.Sleep` trước khi chụp và xử lý validation.
- **P1.29 (Nạp & lưu cấu hình JSON):** Xây dựng `AppConfig` trong Core, triển khai `ConfigStore` đọc/ghi `%APPDATA%\FShot\config.json`, tự động sinh file mặc định và kết nối vào startup.

### 2.2 Tích hợp Icon Kawaii vào Toolbar Runtime

| Icon | File SVG gốc | Kỹ thuật chuyển đổi & Render |
|------|-------------|------------------------------|
| **Arrow (Mũi tên)** | `kawaii/arrow.svg` (64×64) | • Tọa độ Bézier bậc 3 chia đôi về lưới 32×32: `M7 23 C 7 15, 12 9, 19 9...`<br>• Thân mũi tên tô màu **Vàng bơ** `#FDE047`, viền **Nâu hạt óc chó** `#3D2B1F` bo tròn.<br>• Vệt sáng phản chiếu (specular highlight) `M21 10 L 24.5 13` màu trắng `#FFFFFF`. |
| **Pixelate (Làm mờ mosaic)** | `kawaii/pixelate.svg` (64×64) | • Ma trận 3×3 gồm 9 khối kẹo dẻo `6×6px`, bo góc tròn `rx = 2.0`.<br>• Phối màu xen kẽ pastel: San hô `#FF7A70`, Vàng bơ `#FDE047`, Bạc hà `#86EFAC`, Xanh biển `#7BD5F5`.<br>• Viền nâu đậm `#3D2B1F` 1.4px đồng bộ. |
| **Save (Lưu file)** | `kawaii/save.svg` (64×64) | • Mô phỏng chiếc đĩa mềm mini 3.5 inch Kawaii.<br>• Thân đĩa: **Xanh bạc hà** `#86EFAC`, viền xanh rừng `#15803D`.<br>• Cửa trượt kim loại trắng `#FFFFFF` kèm khe trượt xanh rừng.<br>• Nhãn dán kem trắng `#FFFDF9`. |

---

## 3. Các file thay đổi

- `docs/3.Progress/03_00_Progres_Overview.md`: Cập nhật chi tiết các subtask cho P1.24–P1.29, chuẩn hóa phạm vi Epic 5.
- `src/FShot.UI/SkiaCanvas/ToolbarIcons.fs`: Bổ sung path vector 32×32 cho `arrow`, `arrowHighlight`, `pixelate`, `save`.
- `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`: Mở rộng phương thức `DrawToolbar` với nhánh render đa màu sắc cho `ArrowTool`, `PixelateTool`, `SaveAction`.
- `docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/`: Lưu trữ các file SVG vector Kawaii gốc.

---

## 4. Kết quả kiểm thử

```text
FShot.Core.Tests.dll        Passed: 174 / 174
FShot.Rendering.Skia.Tests  Passed:  21 /  21
FShot.UI.Tests.dll          Passed:   3 /   3
Tổng cộng:                  Passed: 198 / 198 (100%)
```

Biên dịch: `0 Warning(s), 0 Error(s)` trên toàn solution.

---

## 5. Danh mục toàn bộ Icon cần tạo ảnh / vector tiếp theo

Dựa trên tài liệu thiết kế `12_06_Icon_Asset_Production_Guide.md`, `12_04_ToolbarLayout.md`, SRS và lộ trình Phase 1–Phase 2, danh mục đầy đủ các icon cần sản xuất asset bao gồm:

### Nhóm 1: Icon trên Toolbar Chú thích (Annotation Tools)
1. `selection.svg` — Vùng chọn / Crop (Khung nét đứt bo tròn góc, 4 nút quăn tai thỏ).
2. `pencil.svg` — Bút vẽ tự do (Bút chì sáp lùn màu đỏ cam `#FF7A70`, đầu gỗ be).
3. `line.svg` — Đường thẳng (Thanh kẹo dẻo tròn 2 đầu màu xanh bạc hà `#86EFAC`).
4. `arrow.svg` — Mũi tên (*Đã có mẫu Kawaii và tích hợp*).
5. `rectangle.svg` — Khung chữ nhật (Khung tranh phồng bong bóng màu xanh da trời `#7BD5F5`).
6. `circle.svg` — Hình tròn / Elip (Bánh donut tròn phồng màu hồng đào `#F472B6`).
7. `marker.svg` — Bút dạ quang (Bút nhớ béo ú pastel vàng neon `#FACC15`, vát chéo 45°).
8. `text.svg` — Chèn chữ (Chữ 'A' béo tròn phồng như gối hơi màu cam đào `#FDBA74`).
9. `pixelate.svg` — Che mờ Mosaic (*Đã có mẫu Kawaii và tích hợp*).
10. `counter-bubble.svg` — Đánh số bước thứ tự (Bong bóng thoại xanh tuyết `#BAE6FD` chứa số ① trắng — *đã có SVG draft*).
11. `invert.svg` — Đảo ngược màu (Âm bản mặt trời / mặt trăng chia đôi đen trắng).
12. `blur.svg` — Làm mờ mịn Gaussian (Đám mây nhỏ màu tím lavender `#DDD6FE`).

### Nhóm 2: Icon Thao tác Toolbar (Action Buttons)
13. `undo.svg` — Hoàn tác (Mũi tên vòng cung móng ngựa tím pastel `#C4B5FD`).
14. `redo.svg` — Làm lại (Mũi tên vòng cung hướng phải tím pastel `#C4B5FD`).
15. `copy.svg` — Sao chép vào clipboard (Hai tờ giấy bo góc kẹp bằng ghim tròn vàng bơ `#FEF08A`).
16. `save.svg` — Lưu ra file đĩa (*Đã có mẫu Kawaii và tích hợp*).
17. `cancel.svg` — Hủy bỏ / Đóng overlay (Dấu X mập mạp màu đỏ dâu tây `#FB7185`).
18. `pin.svg` — Ghim ảnh nổi trên màn hình (Đinh ghim bảng đầu nhựa tròn xanh `#7BD5F5`).
19. `open-app.svg` — Mở ảnh bằng ứng dụng ngoài (Cửa sổ có mũi tên bật ra ngoài).
20. `upload-imgur.svg` — Tải ảnh lên đám mây Imgur (Đám mây Kawaii có mũi tên hướng lên).

### Nhóm 3: Icon Hệ thống & Ứng dụng (App Branding & System Tray)
21. `tray-icon.ico` / `tray-icon.svg` — Biểu tượng Khay hệ thống Windows (Logo F-Shot thu nhỏ rõ nét ở 16×16, 24×24, 32×32).
22. `app-logo.svg` / `master-512.png` — Logo ứng dụng F-Shot Master (Ống kính máy ảnh Kawaii kết hợp tia sét sáng).
23. `Square44x44Logo.png` — Icon Taskbar / Start Menu (các tỷ lệ scale 100%, 125%, 150%, 200%, 400%).
24. `Square150x150Logo.png` — Medium Tile / Search preview (các tỷ lệ scale 100%, 125%, 150%, 200%, 400%).
25. `StoreLogo.png` — Icon trang Microsoft Store / Installer.
26. `SplashScreen.png` — Màn hình khởi động ứng dụng (620×300 px nền tối `#111827`).
