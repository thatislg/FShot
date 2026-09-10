# F-Shot Master Progress Tracking

- **Cập nhật gần nhất:** 2026-09-10
- **Tiến độ tổng quan:** `[ 5 / 64 ] Tasks hoàn thành (~7.8%)` (Code sẵn sàng, 3 task chưa verify trên desktop)
- **Mục tiêu hiện tại:** Hoàn tất Phase 0 (Kiểm chứng kỹ thuật nền tảng - PoC)

---

## 1. Tóm tắt trạng thái các Phase

| Phase | Mục tiêu | Trạng thái | Hoàn thành |
| :--- | :--- | :---: | :---: |
| **Phase 0: PoC** | Khung Solution, Screen Capture, Overlay Canvas, đo 60 FPS | **IN-PROGRESS** | **5 / 8** (Code xong, 3 task cần verify trên Windows desktop) |
| **Phase 1: MVP Core** | Bounding box, 8 Annotation tools, Undo/Redo, Save/Clipboard | **PENDING** | 0 / 29 (0%) |
| **Phase 2: Windows v1.0** | Tray, Hotkeys, Config UI, Pin Widget, Mixed DPI | **PENDING** | 0 / 19 (0%) |
| **Phase 3: Advanced** | Imgur upload, Snap-to-grid, Tùy biến nâng cao | **PENDING** | 0 / 8 (0%) |

---

## 2. Checklist chi tiết Phase 0: Spike & PoC (Target: 10/09 → 16/09/2026)

- [x] **P0.1** Tạo Solution FShot chuẩn 4 projects (`Core`, `Rendering.Skia`, `Platform.Win32`, `UI`) + 1 test project.
- [x] **P0.2** Khai báo các Primitive Types thuần khiết (`Point`, `Rect`, `ColorRgba`) dạng struct F#.
- [x] **P0.3** Triển khai `ScreenEnumeration` lấy thông số Virtual Desktop và danh sách màn hình.
- [x] **P0.4** Triển khai `CaptureAdapterStub` trả về raw byte frame giả lập phục vụ thông luồng.
- [x] **P0.5** Xây dựng `SkiaCanvas` control trong Avalonia bọc `SKCanvas` hiển thị frame ảnh.
- [~] **P0.6** Mở cửa sổ Avalonia Window không viền (`Borderless Topmost`) bao trọn Virtual Screen — **code đã viết, chưa verify trên desktop Windows**.
- [~] **P0.7** Tích hợp xử lý chuột (`PointerPressed/Moved/Released`) kéo vùng chọn Selection giả lập — **code đã viết, chưa verify trên desktop Windows**.
- [ ] **P0.8** Đo kiểm hiệu năng render canvas đạt chuẩn $\ge$ 60 FPS khi kéo chuột — **chưa thực hiện**.

---

## 3. Checklist chi tiết Phase 1: MVP Core (Target: 17/09 → 10/10/2026)

### Epic 1: Core Domain & State Machine
- [ ] **P1.01** Domain model đầy đủ (`Annotation` DU, `CaptureMode`, `ExportTarget`).
- [ ] **P1.02** Immutable `HistoryStack` phục vụ hoàn tác không giới hạn.
- [ ] **P1.03** F# Overlay State Machine (`Idle` → `Selecting` → `Selected` → `Annotating`).
- [ ] **P1.04** Unit tests cho Geometry (Nudge, Resize, Clamp) & History trong `FShot.Core.Tests`.

### Epic 2: Vùng chọn (Selection Engine)
- [ ] **P1.05** Lớp phủ tối mờ Skia (`FR-SEL-01`) ngoài vùng chọn.
- [ ] **P1.06** Kéo bounding box tự do (`FR-SEL-02`).
- [ ] **P1.07** 8 điểm neo co giãn vùng chọn (`FR-SEL-03`).
- [ ] **P1.08** Kéo rê di chuyển toàn bộ vùng chọn (`FR-SEL-04`).
- [ ] **P1.09** Dịch chuyển vùng chọn 1px bằng phím mũi tên (`FR-SEL-05`).
- [ ] **P1.10** Co giãn 1px bằng `Shift + Arrow` (`FR-SEL-06`).
- [ ] **P1.11** Phím tắt `Esc` / `Ctrl+Backspace` hủy vùng chọn hoặc thoát app (`FR-SEL-11`).

### Epic 3: Bộ công cụ chú thích (Annotations)
- [ ] **P1.12** Bút vẽ tự do (Pencil) tích hợp thuật toán làm mịn Bézier (`FR-ANN-01`).
- [ ] **P1.13** Vẽ đường thẳng Line (`FR-ANN-02`).
- [ ] **P1.14** Vẽ mũi tên Arrow có chóp định hướng (`FR-ANN-03`).
- [ ] **P1.15** Vẽ hình chữ nhật Rectangle hỗ trợ bo góc (`FR-ANN-04`).
- [ ] **P1.16** Vẽ hình tròn/elip Circle giữ `Ctrl` khóa tỉ lệ 1:1 (`FR-ANN-05`).
- [ ] **P1.17** Bút nhớ Marker bán trong suốt Alpha Blend (`FR-ANN-06`).
- [ ] **P1.18** Chèn văn bản Text (gõ qua Avalonia TextBox, commit phẳng vào Skia) (`FR-ANN-07`).
- [ ] **P1.19** Che mờ Pixelate xử lý trực tiếp trên mảng byte (`FR-ANN-08`).
- [ ] **P1.20** Phím tắt chuyển nhanh công cụ (`P/D/A/S/R/C/M/T/B/I`).

### Epic 4: Undo, Redo & Toolbar
- [ ] **P1.21** Hoàn tác Undo `Ctrl+Z` (`FR-UNDO-01`).
- [ ] **P1.22** Làm lại Redo `Ctrl+Shift+Z` / `Ctrl+Y` (`FR-UNDO-02`).
- [ ] **P1.23** Toolbar tối giản nằm sát dưới vùng chọn (`FR-TB-01`).

### Epic 5: Xuất dữ liệu & CLI
- [ ] **P1.24** Lưu file ổ cứng `Ctrl+S` kèm cấu hình tên file ngày tháng (`FR-OUT-01/02/03`).
- [ ] **P1.25** Hộp thoại Save As Fallback khi chưa cấu hình đường dẫn (`FR-OUT-04`).
- [ ] **P1.26** Sao chép nhanh vào Windows Clipboard dạng PNG (`FR-OUT-05`).
- [ ] **P1.27** Parser lệnh dòng lệnh bằng Argu (`fshot gui`, `fshot full`) (`FR-CLI-01/02`).
- [ ] **P1.28** Cấu hình độ trễ chụp (`-d / --delay`) (`FR-CAP-05`).
- [ ] **P1.29** Lưu trữ và nạp cấu hình cơ bản từ JSON tại `%APPDATA%\FShot\config.json`.
