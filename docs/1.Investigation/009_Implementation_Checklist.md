# Checklist kiểm soát tiến độ F-Shot

> Tài liệu này chia nhỏ từng phase thành các công việc cụ thể, gán ngày bắt đầu / kết thúc dự kiến và cột trạng thái để theo dõi.
> Ngày khởi đầu giả định: **10/09/2026**. Cập nhật lại nếu project bắt đầu muộn hơn.

---

## Hướng dẫn sử dụng

- `[ ]` — Chưa làm
- `[-]` — Đang làm
- `[x]` — Hoàn thành
- Cột **Ngày kết thúc dự kiến** dùng để kiểm soát deadline.
- Cột **Ngày kết thúc thực tế** điền khi hoàn thành.

---

## Phase 0: Kiểm chứng kỹ thuật nền tảng (Spike / PoC)

**Thời gian:** 10/09/2026 → 16/09/2026 (1 tuần)

| STT | Công việc | Mã SRS tham chiếu | Người phụ trách | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|-------------------|-----------------|------------|------------------|------------------|---------|
| 0.1 | Tạo solution, 3 project, references theo `008_Scaffolding_Plan.md` | — | | [ ] | 11/09/2026 | | |
| 0.2 | Viết `Point`, `Rect`, `Color`, `StrokeWidth` trong `FShot.Core` | — | | [ ] | 11/09/2026 | | |
| 0.3 | Viết adapter `Windows.Graphics.Capture`, trả về `CaptureResult` | FR-WIN-001, FR-CAP-02/03 | | [ ] | 12/09/2026 | | |
| 0.4 | Kiểm tra Mixed DPI: tọa độ con trỏ và ảnh chụp khớp nhau | FR-NF-003, FR-NF-004 | | [ ] | 13/09/2026 | | |
| 0.5 | Mở cửa sổ Avalonia borderless topmost phủ Virtual Screen | FR-CAP-01 | | [ ] | 14/09/2026 | | |
| 0.6 | Vẽ overlay tối bằng SkiaSharp lên ảnh desktop | FR-SEL-01 | | [ ] | 15/09/2026 | | |
| 0.7 | Đo FPS khi kéo chuột / thay đổi vùng chọn, đạt ≥ 60 FPS | FR-NF-001 | | [ ] | 16/09/2026 | | |
| 0.8 | Viết báo cáo PoC, quyết định điều chỉnh kiến trúc nếu cần | — | | [ ] | 16/09/2026 | | |

**Definition of Done Phase 0:**
- [ ] Solution build thành công.
- [ ] Capture trả về bitmap đúng Virtual Screen trên Mixed DPI.
- [ ] Overlay window hiển thị ảnh desktop và phủ tối.
- [ ] FPS đo được ≥ 60 trong môi trường dev.

---

## Phase 1: Minimum Viable Product (MVP Core)

**Thời gian:** 17/09/2026 → 06/10/2026 (3 tuần)

### Epic 1: Foundation (17/09 → 20/09)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 1.1 | Hoàn thiện domain model: `Capture`, `Selection`, `Annotation`, `History`, `Export`, `Config` | FR-CAP-01/02/03/05, FR-SEL-01~06/11, FR-ANN-01~08, FR-UNDO-01/02, FR-OUT-01~05, FR-CFG-001/003/200/201 | [ ] | 18/09/2026 | | |
| 1.2 | Implement immutable `HistoryStack<'T>` với limit | FR-UNDO-01/02 | [ ] | 19/09/2026 | | |
| 1.3 | Viết unit test cho geometry và history stack | — | [ ] | 20/09/2026 | | |
| 1.4 | Kiểm tra `FShot.Core.Tests` pass | — | [ ] | 20/09/2026 | | |

### Epic 2: Overlay Window & Selection (21/09 → 26/09)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 2.1 | Cửa sổ overlay phủ toàn Virtual Screen, nhận focus input | FR-CAP-01 | [ ] | 21/09/2026 | | |
| 2.2 | Hiển thị ảnh desktop đã chụp bằng SkiaSharp | FR-CAP-01 | [ ] | 22/09/2026 | | |
| 2.3 | Vẽ overlay tối ngoài vùng chọn | FR-SEL-01 | [ ] | 23/09/2026 | | |
| 2.4 | Kéo chuột tạo vùng chọn bounding-box | FR-SEL-02 | [ ] | 23/09/2026 | | |
| 2.5 | Vẽ 8 điểm neo xung quanh vùng chọn | FR-SEL-03 | [ ] | 24/09/2026 | | |
| 2.6 | Xử lý resize bằng các điểm neo | FR-SEL-03 | [ ] | 25/09/2026 | | |
| 2.7 | Di chuyển vùng chọn bằng kéo bên trong | FR-SEL-04 | [ ] | 25/09/2026 | | |
| 2.8 | Nudge 1 px bằng phím mũi tên | FR-SEL-05 | [ ] | 26/09/2026 | | |
| 2.9 | Resize 1 px bằng `Shift + Arrow` | FR-SEL-06 | [ ] | 26/09/2026 | | |
| 2.10 | Hủy chụp bằng `Esc` / `Ctrl+Backspace` | FR-SEL-11 | [ ] | 26/09/2026 | | |

### Epic 3: Annotation Tools (27/09 → 04/10)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 3.1 | Render `Tool` DU lên Skia canvas | FR-ANN-01~08 | [ ] | 28/09/2026 | | |
| 3.2 | Implement Pencil với path smoothing | FR-ANN-01 | [ ] | 28/09/2026 | | |
| 3.3 | Implement Line | FR-ANN-02 | [ ] | 29/09/2026 | | |
| 3.4 | Implement Arrow với arrow head | FR-ANN-03 | [ ] | 29/09/2026 | | |
| 3.5 | Implement Rectangle + corner radius | FR-ANN-04 | [ ] | 30/09/2026 | | |
| 3.6 | Implement Circle/Ellipse + Ctrl để khóa tỉ lệ | FR-ANN-05 | [ ] | 01/10/2026 | | |
| 3.7 | Implement Marker với alpha blend | FR-ANN-06 | [ ] | 02/10/2026 | | |
| 3.8 | Implement Text (inline edit + commit) | FR-ANN-07 | [ ] | 03/10/2026 | | |
| 3.9 | Implement Pixelate (block size) | FR-ANN-08 | [ ] | 04/10/2026 | | |
| 3.10 | Phím tắt chuyển công cụ P/D/A/S/R/C/M/T/B/I | FR-SH-001~010 | [ ] | 04/10/2026 | | |

### Epic 4: Undo / Redo (05/10 → 06/10)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 4.1 | Tích hợp push snapshot sau mỗi chú thích | FR-UNDO-01/02 | [ ] | 05/10/2026 | | |
| 4.2 | Phím tắt `Ctrl+Z` / `Ctrl+Shift+Z` | FR-SH-012/013 | [ ] | 05/10/2026 | | |
| 4.3 | Unit test undo/redo với nhiều bước | — | [ ] | 06/10/2026 | | |

### Epic 5: Export (05/10 → 07/10)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 5.1 | Render final image (selection + annotations) | FR-OUT-01~05 | [ ] | 05/10/2026 | | |
| 5.2 | Encode PNG/JPG | FR-OUT-01/05 | [ ] | 06/10/2026 | | |
| 5.3 | Save to file với filename pattern | FR-OUT-01/02/03 | [ ] | 06/10/2026 | | |
| 5.4 | Copy to clipboard | FR-OUT-05 | [ ] | 07/10/2026 | | |
| 5.5 | Save dialog fallback | FR-OUT-04 | [ ] | 07/10/2026 | | |

### Epic 6: CLI & Config (06/10 → 08/10)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 6.1 | CLI parser cho `fshot gui`, `fshot full`, `fshot screen` | FR-CLI-01/02/03 | [ ] | 06/10/2026 | | |
| 6.2 | Delayed capture (`-d`) | FR-CAP-05 | [ ] | 07/10/2026 | | |
| 6.3 | Đọc/ghi config tối thiểu: `savePath`, `filenamePattern`, `drawColor`, `drawThickness` | FR-CFG-001/003/200/201 | [ ] | 07/10/2026 | | |
| 6.4 | Kết nối config vào save/copy flow | FR-CFG-001/003 | [ ] | 08/10/2026 | | |

### Epic 7: Polish & NFR (07/10 → 10/10)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 7.1 | Đo FPS trong dev build | FR-NF-001 | [ ] | 08/10/2026 | | |
| 7.2 | Test Mixed DPI (100% + 150%) | FR-NF-003/004 | [ ] | 09/10/2026 | | |
| 7.3 | Xử lý lỗi capture graceful | FR-NF-008 | [ ] | 09/10/2026 | | |
| 7.4 | Smoke test end-to-end | — | [ ] | 10/10/2026 | | |
| 7.5 | Báo cáo MVP, quyết định chức năng cần chỉnh sửa | — | [ ] | 10/10/2026 | | |

**Definition of Done Phase 1:**
- [ ] `fshot full` và `fshot gui` chạy được trên Windows 10/11.
- [ ] Chụp vùng, vẽ 8 loại chú thích, undo/redo, save/copy hoạt động.
- [ ] Unit test `FShot.Core` pass.
- [ ] Không crash khi capture thất bại.
- [ ] FPS ≥ 60 khi kéo vùng chọn trên 1080p.

---

## Phase 2: Trải nghiệm đầy đủ trên Windows (v1.0)

**Thời gian:** 13/10/2026 → 31/10/2026 (3 tuần)

### Epic 8: Tích hợp Windows & Tray (13/10 → 20/10)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 8.1 | System tray icon với menu cơ bản | FR-SYS-001~008 | [ ] | 15/10/2026 | | |
| 8.2 | Global hotkey `Win+Shift+X` | FR-SYS-009, FR-WIN-002 | [ ] | 17/10/2026 | | |
| 8.3 | Xử lý phím `PrintScreen` | FR-SH-030, FR-SYS-030 | [ ] | 18/10/2026 | | |
| 8.4 | Single instance bằng Mutex / Named Pipe | FR-SYS-010 | [ ] | 19/10/2026 | | |
| 8.5 | Tự khởi động cùng Windows | FR-WIN-005 | [ ] | 20/10/2026 | | |

### Epic 9: Config UI & Config Watcher (21/10 → 24/10)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 9.1 | Màn hình cấu hình với tab General / Interface / Tool Defaults | FR-CFG-* | [ ] | 23/10/2026 | | |
| 9.2 | Lưu config tại `%APPDATA%\Roaming\FShot\` | FR-WIN-006 | [ ] | 23/10/2026 | | |
| 9.3 | File watcher reload config khi file thay đổi | FR-SYS-011 | [ ] | 24/10/2026 | | |
| 9.4 | Config error resolver cơ bản | FR-SYS-012 | [ ] | 24/10/2026 | | |

### Epic 10: Công cụ chú thích nâng cao (21/10 → 27/10)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 10.1 | Invert colors | FR-ANN-09 | [ ] | 22/10/2026 | | |
| 10.2 | Circle Counter tự động tăng | FR-ANN-10 | [ ] | 23/10/2026 | | |
| 10.3 | Orthogonal constraint (Ctrl khi vẽ line/arrow) | FR-ANN-11 | [ ] | 24/10/2026 | | |
| 10.4 | Aspect-ratio constraint (Ctrl khi vẽ rect/circle) | FR-ANN-12 | [ ] | 24/10/2026 | | |
| 10.5 | Tool size by keyboard | FR-ANN-13 | [ ] | 25/10/2026 | | |
| 10.6 | Tool size by mouse wheel | FR-ANN-14 | [ ] | 25/10/2026 | | |
| 10.7 | Object selection & edit mode | FR-ANN-18 | [ ] | 26/10/2026 | | |
| 10.8 | Delete selected annotation | FR-ANN-20 | [ ] | 26/10/2026 | | |
| 10.9 | Commit text với `Ctrl+Return` | FR-ANN-21 | [ ] | 27/10/2026 | | |

### Epic 11: Kính lúp & Thanh công cụ (25/10 → 28/10)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 11.1 | Magnifier widget với màu HEX/RGB | FR-MAG-01/03 | [ ] | 26/10/2026 | | |
| 11.2 | Eyedropper (`G`) | FR-MAG-04 | [ ] | 26/10/2026 | | |
| 11.3 | Right-click color wheel | FR-MAG-05 | [ ] | 27/10/2026 | | |
| 11.4 | Context-aware toolbar | FR-TB-001 | [ ] | 27/10/2026 | | |
| 11.5 | Configurable visible buttons | FR-TB-002 | [ ] | 28/10/2026 | | |
| 11.6 | Auto-position toolbar | FR-TB-004 | [ ] | 28/10/2026 | | |

### Epic 12: Pin Widget (26/10 → 30/10)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 12.1 | Floating topmost pin window | FR-PIN-01 | [ ] | 27/10/2026 | | |
| 12.2 | Drag to reposition | FR-PIN-02 | [ ] | 27/10/2026 | | |
| 12.3 | Zoom/Scale | FR-PIN-03 | [ ] | 28/10/2026 | | |
| 12.4 | Opacity adjustment | FR-PIN-04 | [ ] | 28/10/2026 | | |
| 12.5 | Rotate left/right | FR-PIN-05 | [ ] | 29/10/2026 | | |
| 12.6 | Copy/Save from pin context menu | FR-PIN-06/07 | [ ] | 29/10/2026 | | |
| 12.7 | Close pin | FR-PIN-08 | [ ] | 30/10/2026 | | |

### Epic 13: Xuất & NFR nâng cao (27/10 → 31/10)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 13.1 | Double-click to copy | FR-OUT-06 | [ ] | 28/10/2026 | | |
| 13.2 | Save-after-copy, copy-path-after-save | FR-OUT-07/08 | [ ] | 28/10/2026 | | |
| 13.3 | Raw PNG stdout | FR-OUT-09 | [ ] | 29/10/2026 | | |
| 13.4 | Print geometry stdout | FR-OUT-10 | [ ] | 29/10/2026 | | |
| 13.5 | Open with external app | FR-OUT-11 | [ ] | 30/10/2026 | | |
| 13.6 | File extension preference (PNG/JPG) | FR-OUT-13 | [ ] | 30/10/2026 | | |
| 13.7 | Windows Toast notifications | FR-OUT-15 | [ ] | 31/10/2026 | | |
| 13.8 | i18n framework | FR-NF-005 | [ ] | 31/10/2026 | | |
| 13.9 | Keyboard-only accessibility test | FR-NF-006 | [ ] | 31/10/2026 | | |

**Definition of Done Phase 2:**
- [ ] Ứng dụng chạy nền với tray icon.
- [ ] `Win+Shift+X` và `PrintScreen` mở overlay.
- [ ] Config UI hoàn chỉnh, lưu tại `%APPDATA%\Roaming\FShot\`.
- [ ] Pin widget hoạt động.
- [ ] Đủ tính năng để thay thế Flameshot cơ bản.

---

## Phase 3: Mở rộng & Tùy biến nâng cao (v1.x)

**Thời gian:** 03/11/2026 → 21/11/2026 (3 tuần, tùy chọn)

| STT | Công việc | Mã SRS | Trạng thái | Kết thúc dự kiến | Kết thúc thực tế | Ghi chú |
|-----|-----------|--------|------------|------------------|------------------|---------|
| 3.1 | Imgur upload ẩn danh | FR-UP-01 | [ ] | 06/11/2026 | | |
| 3.2 | Upload confirmation dialog | FR-UP-02 | [ ] | 06/11/2026 | | |
| 3.3 | Copy URL after upload | FR-UP-03 | [ ] | 07/11/2026 | | |
| 3.4 | Post-upload dialog | FR-UP-04 | [ ] | 07/11/2026 | | |
| 3.5 | Upload history | FR-UP-05 | [ ] | 10/11/2026 | | |
| 3.6 | Client secret config | FR-UP-06 | [ ] | 10/11/2026 | | |
| 3.7 | Snap-to-grid | FR-SEL-14 | [ ] | 12/11/2026 | | |
| 3.8 | Grid display | FR-SEL-15 | [ ] | 12/11/2026 | | |
| 3.9 | XYWH timeout | FR-SEL-13 | [ ] | 13/11/2026 | | |
| 3.10 | Arrow style + reverse | FR-ANN-16/17 | [ ] | 14/11/2026 | | |
| 3.11 | Hit-testing tolerance | FR-ANN-19 | [ ] | 14/11/2026 | | |
| 3.12 | Hidden shortcut buttons | FR-TB-003 | [ ] | 17/11/2026 | | |
| 3.13 | Button right-click size | FR-TB-005 | [ ] | 17/11/2026 | | |
| 3.14 | Icon contrast adapt | FR-TB-006 | [ ] | 18/11/2026 | | |
| 3.15 | Import/export config | FR-SYS-013 | [ ] | 19/11/2026 | | |
| 3.16 | Reset config | FR-SYS-014 | [ ] | 19/11/2026 | | |
| 3.17 | Ngăn Windows Snipping Tool chiếm PrtSc | FR-WIN-003 | [ ] | 20/11/2026 | | |
| 3.18 | Console CLI wrapper | FR-WIN-004 | [ ] | 20/11/2026 | | |
| 3.19 | Update checker | FR-CFG-019/020 | [ ] | 21/11/2026 | | |

---

## Tổng hợp timeline

| Phase | Bắt đầu | Kết thúc dự kiến | Trọng tâm |
|-------|---------|------------------|-----------|
| Phase 0: PoC | 10/09/2026 | 16/09/2026 | Kiểm chứng capture, overlay, FPS |
| Phase 1: MVP | 17/09/2026 | 10/10/2026 | Chụp, vẽ, undo, save/copy |
| Phase 2: v1.0 | 13/10/2026 | 31/10/2026 | Tray, hotkey, config UI, pin widget |
| Phase 3: v1.x | 03/11/2026 | 21/11/2026 | Imgur, grid, advanced tools, packaging |

---

## Ghi chú chung

- Cập nhật cột **Trạng thái** và **Kết thúc thực tế** sau mỗi ngày làm việc.
- Nếu công việc trễ quá 2 ngày, ghi rõ lý do và điều chỉnh timeline ở phần dưới.
- Các mục **tùy chọn** có thể dời sang phase sau nếu cần đảm bảo MVP đúng hạn.

---

*Checklist này sẽ được cập nhật khi project bắt đầu và khi phát sinh thay đổi phạm vi.*
