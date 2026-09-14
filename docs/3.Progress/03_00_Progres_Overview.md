# F-Shot Master Progress Tracking

- **Cập nhật gần nhất:** 2026-09-14
- **Tiến độ tổng quan:** `[ 24 / 84 ] Tasks hoàn thành (~28.6%)` (Phase 0 PoC đã verify xong; Epic 1 P1.01–P1.04, Epic 2 P1.05–P1.11, và Epic 2.5 E2.5.01–E2.5.05 đã hoàn thiện)
- **Mục tiêu hiện tại:** Bắt đầu Epic 3: triển khai bộ công cụ chú thích (Annotations) từ P1.12

---

## 1. Tóm tắt trạng thái các Phase

| Phase | Mục tiêu | Trạng thái | Hoàn thành |
| :--- | :--- | :---: | :---: |
| **Phase 0: PoC** | Khung Solution, Screen Capture, Overlay Canvas, đo 60 FPS | **DONE** | **8 / 8** (đã verify trên desktop Windows) |
| **Phase 1: MVP Core** | Bounding box, 8 Annotation tools, Undo/Redo, Save/Clipboard | **IN PROGRESS** | 16 / 29 (~55%) |
| **Phase 2: Windows v1.0** | Tray, Hotkeys, Real Capture, Config UI, Pin Widget, Advanced tools | **PENDING** | 0 / 39 (0%) |
| **Phase 3: Advanced** | Imgur upload, Snap-to-grid, Tùy biến nâng cao | **PENDING** | 0 / 8 (0%) |

---

## 2. Checklist chi tiết Phase 0: Spike & PoC (Target: 10/09 → 16/09/2026)

- [x] **P0.1** Tạo Solution FShot chuẩn 4 projects (`Core`, `Rendering.Skia`, `Platform.Win32`, `UI`) + 1 test project.
- [x] **P0.2** Khai báo các Primitive Types thuần khiết (`Point`, `Rect`, `ColorRgba`) dạng struct F#.
- [x] **P0.3** Triển khai `ScreenEnumeration` lấy thông số Virtual Desktop và danh sách màn hình.
- [x] **P0.4** Triển khai `CaptureAdapterStub` trả về raw byte frame giả lập phục vụ thông luồng.
- [x] **P0.5** Xây dựng `SkiaCanvas` control trong Avalonia bọc `SKCanvas` hiển thị frame ảnh.
- [x] **P0.6** Mở cửa sổ Avalonia Window không viền (`Borderless Topmost`) bao trọn Virtual Screen — **verify trên desktop Windows: phủ full 2 màn hình 3840x1200**.
- [x] **P0.7** Tích hợp xử lý chuột (`PointerPressed/Moved/Released`) kéo vùng chọn Selection giả lập — **verify trên desktop Windows: vùng chọn hiển thị rõ với dim ngoài, xanh mờ trong, viền trắng, 8 handle**.
- [x] **P0.8** Đo kiểm hiệu năng render canvas đạt chuẩn $\ge$ 60 FPS khi kéo chuột — **verify trên desktop Windows: AvgFrameTime ~0.10 ms, [PASS] liên tục**.

---

## 3. Checklist chi tiết Phase 1: MVP Core (Target: 17/09 → 10/10/2026)

### Epic 1: Core Domain & State Machine
- [x] **P1.01** Domain model đầy đủ (`Annotation` DU, `CaptureMode`, `ExportTarget`).
  - [x] Hoàn thiện `docs/2.Design/04_Annotation/04_03_Pencil.md` (thiết kế Pencil + smoothing).
  - [x] Hoàn thiện `docs/2.Design/04_Annotation/04_06_MarkerAndPixelate.md` (thiết kế Marker + Pixelate).
  - [x] Hoàn thiện `docs/2.Design/04_Annotation/04_07_TextTool.md` (thiết kế Text tool + commit).
  - [x] Rà soát `docs/2.Design/02_Capture/02_03_CaptureRequest.md` (đảm bảo đủ cho `CaptureMode`, `OutputTarget`).
  - [x] Rà soát `docs/2.Design/06_Export/06_02_ExportTarget.md` (đảm bảo đủ cho `ExportTarget`).
  - [x] Refactor `src/FShot.Core/Domain/Annotation.fs` theo P1.01: `AnnotationStyle` chung, `Marker` dùng `Point list`, `Text` mang `TextAlignment`.
  - [x] Cập nhật `tests/FShot.Core.Tests/Domain/AnnotationTests.fs` theo API mới; tất cả tests pass.
- [x] **P1.02** Immutable `HistoryStack` phục vụ hoàn tác không giới hạn.
  - [x] Hoàn thiện `docs/2.Design/05_History/05_02_Snapshot.md`.
  - [x] Hoàn thiện `docs/2.Design/05_History/05_03_HistoryStack.md`.
  - [x] Hoàn thiện `docs/2.Design/05_History/05_04_Integration.md`.
  - [x] Triển khai `src/FShot.Core/Domain/History.fs`: `Snapshot`, `HistoryStack`, Push/Undo/Redo/SetLimit.
  - [x] Viết / cập nhật `tests/FShot.Core.Tests/Domain/HistoryTests.fs`; tất cả tests pass.
  - [x] Bỏ qua `05_05_CounterRestoration.md` trong MVP (dành cho v1.x).
- [x] **P1.03** F# Overlay State Machine (`Idle` → `Selecting` → `Selected` → `Annotating`).
  - [x] Hoàn thiện `docs/2.Design/08_OverlayState/08_02_States.md`.
  - [x] Hoàn thiện `docs/2.Design/08_OverlayState/08_03_Events.md`.
  - [x] Hoàn thiện `docs/2.Design/08_OverlayState/08_04_Transitions.md`.
  - [x] Hoàn thiện `docs/2.Design/08_OverlayState/08_05_RenderModel.md`.
  - [x] Hoàn thiện `docs/2.Design/08_OverlayState/08_06_Integration.md`.
  - [x] Triển khai `src/FShot.Core/State/OverlayState.fs`: pure F# state machine, không phụ thuộc UI.
  - [x] Bổ sung `ToolKind.SelectionTool` trong `src/FShot.Core/Domain/Annotation.fs` để phân biệt di chuyển vùng chọn và vẽ annotation.
  - [x] Triển khai `ConfigSnapshot` trong `src/FShot.Core/Domain/Config.fs`.
- [x] **P1.04** Unit tests cho Geometry (Nudge, Resize, Clamp) & History trong `FShot.Core.Tests`.
  - [x] Bổ sung / cập nhật `tests/FShot.Core.Tests/Domain/AnnotationTests.fs`.
  - [x] Bổ sung `tests/FShot.Core.Tests/Domain/ConfigTests.fs`.
  - [x] Bổ sung `tests/FShot.Core.Tests/Domain/HistoryTests.fs`.
  - [x] Bổ sung `tests/FShot.Core.Tests/State/OverlayStateTests.fs`.

### Epic 2: Vùng chọn (Selection Engine)
- [x] **P1.05** Lớp phủ tối mờ Skia (`FR-SEL-01`) ngoài vùng chọn.
  - [x] Hoàn thiện `docs/2.Design/03_Selection/03_07_OverlayDimming.md`.
  - [x] Triển khai `src/FShot.Rendering.Skia/Renderers/DimmingRenderer.fs`.
  - [x] Cập nhật `src/FShot.Rendering.Skia/Renderers/SceneComposer.fs` gọi `DimmingRenderer`.
  - [x] Thêm tests `tests/FShot.Rendering.Skia.Tests/DimmingTests.fs`.
- [x] **P1.06** Kéo bounding box tự do (`FR-SEL-02`).
  - [x] Rà soát `docs/2.Design/03_Selection/03_04_MouseOperations.md` mục tạo vùng chọn.
  - [x] Đảm bảo `Selection.StartSelecting` + `UpdateSelecting` + `FinishSelecting` hoạt động đúng với mọi hướng kéo.
  - [x] Kiểm thử kéo vùng chọn từ mọi hướng (trái→phải, phải→trái, trên→dưới, dưới→trên).
  - [x] Kiểm thử clamp và kích thước tối thiểu khi hoàn tất.
- [x] **P1.07** 8 điểm neo co giãn vùng chọn (`FR-SEL-03`).
  - [x] Rà soát `docs/2.Design/03_Selection/03_03_ResizeHandles.md` (vị trí, hit-test, thứ tự ưu tiên).
  - [x] Đảm bảo `Selection.HandleCenters`, `HitTestHandle`, `StartResizing`, `UpdateResizing`, `FinishInteraction` hỗ trợ đủ 8 handles.
  - [x] Kiểm thử hit-test tolerance và resize theo 8 hướng.
  - [x] Kiểm thử kích thước tối thiểu sau khi resize.
- [x] **P1.08** Kéo rê di chuyển toàn bộ vùng chọn (`FR-SEL-04`).
  - [x] Rà soát `docs/2.Design/03_Selection/03_04_MouseOperations.md` mục di chuyển vùng chọn.
  - [x] Đảm bảo `Selection.StartMoving` + `UpdateMoving` + clamp hoạt động trong `OverlayState`.
  - [x] Kiểm thử di chuyển vùng chọn ra sát biên và bị giới hạn.
- [x] **P1.09** Dịch chuyển vùng chọn 1px bằng phím mũi tên (`FR-SEL-05`).
  - [x] Hoàn thiện `docs/2.Design/03_Selection/03_05_KeyboardOperations.md` (nudge bằng phím mũi tên).
  - [x] Thêm xử lý `KeyDown` cho phím mũi tên trong `OverlayState.update`.
  - [x] Triển khai nudge trong `OverlayState`: dịch vùng chọn 1px theo hướng, clamp vào capture bounds.
  - [x] Kiểm thử từng phím mũi tên và kết hợp với modifier.
- [x] **P1.10** Co giãn 1px bằng `Shift + Arrow` (`FR-SEL-06`).
  - [x] Hoàn thiện `docs/2.Design/03_Selection/03_05_KeyboardOperations.md` (keyboard resize bằng Shift + Arrow).
  - [x] Xử lý `KeyDown` kèm Shift trong `OverlayState.update`.
  - [x] Triển khai resize 1px theo hướng mũi tên, điều chỉnh cạnh tương ứng của vùng chọn.
  - [x] Kiểm thử 4 hướng co giãn với Shift.
- [x] **P1.11** Phím tắt `Esc` / `Ctrl+Backspace` hủy vùng chọn hoặc thoát app (`FR-SEL-11`).
  - [x] Rà soát xử lý `Cancel` trong `OverlayState.update` và `Selection.Cancel`.
  - [x] Đảm bảo Esc ở `Idle` đóng overlay, ở `Selected` hủy vùng chọn về `Idle`, ở `Selecting` hủy tạo vùng.
  - [x] Thêm / cập nhật kiểm thử cho từng trường hợp.
  - [x] `Ctrl+Backspace` tương đương Cancel khi có vùng chọn.
  - [x] `Ctrl+Backspace` trong `Annotating` hủy preview và quay về `Selected`.
  - [x] Tích hợp UI đúng nghĩa: `CaptureCanvas` dùng `OverlayState` cho input chuột/bàn phím, vẽ theo `RenderModel`, thực thi `CloseOverlay`.

### Epic 2.5: UI/UX Design với Penpot
- [x] **E2.5.01** Design tokens (màu sắc, spacing, typography, handle size).
  - [x] Hoàn thiện `docs/2.Design/12_UIUX_Mock_Penpot/12_01_DesignTokens.md`.
  - [x] Định nghĩa đủ tokens để map 1:1 sang Avalonia ResourceDictionary.
- [x] **E2.5.02** Common components (ToolButton, BottomToolbar, ResizeHandle).
  - [x] Hoàn thiện `docs/2.Design/12_UIUX_Mock_Penpot/12_02_CommonComponents.md`.
  - [x] Mô tả variants: Default, Hover, Active, Disabled.
- [x] **E2.5.03** Capture overlay states.
  - [x] Hoàn thiện `docs/2.Design/12_UIUX_Mock_Penpot/12_03_CaptureOverlayStates.md`.
  - [x] 5 boards: Idle Dimmed, Dragging Selection, Selected with Toolbar, Annotating Mode, Color Picker Popup.
- [x] **E2.5.04** Toolbar layout.
  - [x] Hoàn thiện `docs/2.Design/12_UIUX_Mock_Penpot/12_04_ToolbarLayout.md`.
  - [x] Chi tiết vị trí, nhóm tool, icon, shortcut, hover/active state.
- [x] **E2.5.05** Export guidelines.
  - [x] Hoàn thiện `docs/2.Design/12_UIUX_Mock_Penpot/12_05_ExportGuidelines.md`.
  - [x] Quy tắc xuất từ Penpot sang XAML / Skia rendering.

### Epic 3: Bộ công cụ chú thích (Annotations)
- [x] **P1.12** Bút vẽ tự do (Pencil) tích hợp thuật toán làm mịn Bézier (`FR-ANN-01`).
  - [x] Rà soát `docs/2.Design/04_Annotation/04_03_Pencil.md` đảm bảo thuật toán smoothing phù hợp MVP.
  - [x] Triển khai `PencilAnnotation` trong `src/FShot.Core/Domain/Annotation.fs` (nếu chưa có).
  - [x] Triển khai `PencilRenderer` trong `src/FShot.Rendering.Skia/Renderers/`.
  - [x] Tích hợp `OverlayState`: nhận diện tool Pencil, bắt đầu vẽ, cập nhật preview, commit annotation.
  - [x] Thêm xử lý phím tắt `P` trong `OverlayState.update`.
  - [x] Thêm tests `tests/FShot.Rendering.Skia.Tests/PencilTests.fs` hoặc `AnnotationTests.fs`.
- [ ] **P1.13** Vẽ đường thẳng Line (`FR-ANN-02`).
  - [ ] Hoàn thiện `docs/2.Design/04_Annotation/04_01_Line.md` nếu chưa đủ, hoặc rà soát.
  - [ ] Triển khai `LineAnnotation` trong `Annotation.fs`.
  - [ ] Triển khai `LineRenderer` trong Skia.
  - [ ] Tích hợp `OverlayState`: tool Line, preview khi kéo, commit khi thả.
  - [ ] Thêm phím tắt `L`.
  - [ ] Thêm tests.
- [ ] **P1.14** Vẽ mũi tên Arrow có chóp định hướng (`FR-ANN-03`).
  - [ ] Hoàn thiện `docs/2.Design/04_Annotation/04_02_Arrow.md`.
  - [ ] Triển khai `ArrowAnnotation` với đầu mũi tên tam giác.
  - [ ] Triển khai `ArrowRenderer`.
  - [ ] Tích hợp `OverlayState` tool Arrow.
  - [ ] Thêm phím tắt `A`.
  - [ ] Thêm tests.
- [ ] **P1.15** Vẽ hình chữ nhật Rectangle hỗ trợ bo góc (`FR-ANN-04`).
  - [ ] Rà soát `docs/2.Design/04_Annotation/04_04_Rectangle.md`.
  - [ ] Triển khai `RectangleAnnotation` với thuộc tính `CornerRadius`.
  - [ ] Triển khai `RectangleRenderer`.
  - [ ] Tích hợp `OverlayState` tool Rectangle.
  - [ ] Thêm phím tắt `R`.
  - [ ] Thêm tests.
- [ ] **P1.16** Vẽ hình tròn/elip Circle giữ `Ctrl` khóa tỉ lệ 1:1 (`FR-ANN-05`).
  - [ ] Rà soát `docs/2.Design/04_Annotation/04_05_Circle.md`.
  - [ ] Triển khai `CircleAnnotation`.
  - [ ] Triển khai `CircleRenderer`.
  - [ ] Tích hợp `OverlayState` tool Circle, xử lý modifier `Ctrl` để khóa tỉ lệ 1:1.
  - [ ] Thêm phím tắt `C`.
  - [ ] Thêm tests.
- [ ] **P1.17** Bút nhớ Marker bán trong suốt Alpha Blend (`FR-ANN-06`).
  - [ ] Rà soát `docs/2.Design/04_Annotation/04_06_MarkerAndPixelate.md` phần Marker.
  - [ ] Triển khai `MarkerAnnotation` với điểm path và alpha blend.
  - [ ] Triển khai `MarkerRenderer`.
  - [ ] Tích hợp `OverlayState` tool Marker.
  - [ ] Thêm phím tắt `M`.
  - [ ] Thêm tests.
- [ ] **P1.18** Chèn văn bản Text (gõ qua Avalonia TextBox, commit phẳng vào Skia) (`FR-ANN-07`).
  - [ ] Rà soát `docs/2.Design/04_Annotation/04_07_TextTool.md`.
  - [ ] Triển khai `TextAnnotation` với `TextAlignment`, `FontSize`.
  - [ ] Triển khai `TextRenderer` vẽ text phẳng bằng Skia.
  - [ ] Thiết kế cơ chế edit: `OverlayState` chuyển sang `TextEditing`, UI hiện Avalonia TextBox tạm thời.
  - [ ] Tích hợp UI `CaptureCanvas` để chỉ định vị trí TextBox overlay.
  - [ ] Thêm phím tắt `T`.
  - [ ] Thêm tests.
- [ ] **P1.19** Che mờ Pixelate xử lý trực tiếp trên mảng byte (`FR-ANN-08`).
  - [ ] Rà soát `docs/2.Design/04_Annotation/04_06_MarkerAndPixelate.md` phần Pixelate.
  - [ ] Triển khai `PixelateAnnotation` với `BlockSize`.
  - [ ] Triển khai `PixelateRenderer` xử lý trên raw byte (hoặc Skia bitmap sampling nếu đơn giản hơn).
  - [ ] Tích hợp `OverlayState` tool Pixelate.
  - [ ] Thêm phím tắt `B`.
  - [ ] Thêm tests.
- [ ] **P1.20** Phím tắt chuyển nhanh công cụ (`P/D/A/S/R/C/M/T/B/I`).
  - [ ] Rà soát `docs/2.Design/08_OverlayState/08_03_Events.md` và `08_04_Transitions.md`.
  - [ ] Đảm bảo `OverlayState.update` xử lý đủ phím tắt cho 9 công cụ annotation.
  - [ ] Mapping phím: `P` Pencil, `L` Line, `A` Arrow, `R` Rectangle, `C` Circle, `M` Marker, `T` Text, `B` Pixelate, `Esc` hoặc `S` Selection tool.
  - [ ] Kiểm thử từng phím tắt chuyển tool.

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

---

## 4. Checklist chi tiết Phase 2: Windows v1.0 (Target: 11/10 → 07/11/2026)

### Epic 6: System Tray & App Lifecycle
- [ ] **P2.01** Biểu tượng tray liên tục với menu ngữ cảnh: chụp GUI, chụp màn hình, mở cài đặt, mở thư mục lưu, thoát (`FR-SYS-001`–`FR-SYS-008`).
- [ ] **P2.02** Giới hạn single-instance và khởi động cùng Windows (`FR-SYS-010`, `FR-CFG-006`, `FR-WIN-005`).
- [ ] **P2.03** Thoát graceful, thông báo thành công / hủy, tùy chọn ẩn tray icon (`FR-CFG-007`, `FR-CFG-008`).

### Epic 7: Global Hotkeys
- [ ] **P2.04** Đăng ký phím nóng toàn hệ thống `Win+Shift+X` để kích hoạt chụp (`FR-SYS-009`, `FR-WIN-002`, `FR-SH-028`).
- [ ] **P2.05** Tích hợp phím `PrintScreen` và xử lý xung đột với Windows Snipping Tool (`FR-SYS-020`, `FR-WIN-003`, `FR-SH-030`).
- [ ] **P2.06** Cho phép cấu hình và thay đổi các phím tắt toàn cục (`FR-SH-028`–`FR-SH-030`).

### Epic 8: Real Capture Backend & Mixed DPI
- [ ] **P2.07** Triển khai backend `Windows.Graphics.Capture` cho chụp đa màn hình đúng mixed-DPI (`FR-WIN-001`).
- [ ] **P2.08** Hỗ trợ chụp một màn hình cụ thể và màn hình có con trỏ qua WinRT.
- [ ] **P2.09** Fallback `BitBlt` khi `Windows.Graphics.Capture` không khả dụng hoặc bị từ chối quyền (`FR-WIN-001`, fallback).
- [ ] **P2.10** Xử lý đúng Mixed DPI và Per-Monitor V2 DPI Awareness cho cả chụp và UI (`FR-SYS-017`, `FR-SYS-018`, `FR-WIN-001`).

### Epic 9: Config Persistence & Editor
- [ ] **P2.11** Đọc/ghi cấu hình dạng JSON tại `%APPDATA%\FShot\config.json` (`FR-CFG-001`, `FR-CFG-003`–`FR-CFG-005`).
- [ ] **P2.12** Migrate hoặc đọc cấu hình cũ từ `flameshot.ini` (`FR-WIN-006`, migration).
- [ ] **P2.13** Cửa sổ cài đặt (Config Editor) cho các tùy chọn chung, giao diện và giá trị mặc định công cụ (`FR-SYS-006`, `FR-CFG-100`–`FR-CFG-209`).
- [ ] **P2.14** Trình chỉnh sửa mẫu tên file có preview token strftime (`FR-CFG-003`).

### Epic 10: Pin Widget
- [ ] **P2.15** Cửa sổ ghim ảnh Topmost không viền (`FR-PIN-01`).
- [ ] **P2.16** Di chuyển, thu phóng, điều chỉnh độ trong suốt và xoay ảnh ghim (`FR-PIN-02`–`FR-PIN-05`).
- [ ] **P2.17** Menu ngữ cảnh của cửa sổ ghim: copy, save, close (`FR-PIN-06`–`FR-PIN-08`).

### Epic 11: Annotation Tools Advanced
- [ ] **P2.20** Công cụ đảo ngược màu (Invert) trong vùng chỉ định (`FR-ANN-09`).
- [ ] **P2.21** Bong bóng đếm số tự động tăng (Circle Counter) (`FR-ANN-10`, `FR-UNDO-005`).
- [ ] **P2.22** Ràng buộc góc 45°/90° khi vẽ line/arrow/marker bằng `Ctrl` (`FR-ANN-11`).
- [ ] **P2.23** Giữ tỉ lệ 1:1 khi vẽ rectangle/circle bằng `Ctrl` (`FR-ANN-12`).
- [ ] **P2.24** Nhập số để đặt chính xác kích thước công cụ (`FR-ANN-13`).
- [ ] **P2.25** Lăn chuột để tăng/giảm độ dày nét vẽ (`FR-ANN-14`).
- [ ] **P2.26** Chọn, di chuyển hoặc sửa chú thích cũ (`FR-ANN-18`, `FR-ANN-19`, `FR-ANN-20`, `FR-ANN-21`).

### Epic 12: Selection Engine Advanced
- [ ] **P2.27** Co giãn đối xứng 2 px bằng `Ctrl+Shift+Arrow` (`FR-SEL-07`).
- [ ] **P2.28** Ngăn vùng chọn thu nhỏ quá mức và giới hạn trong phạm vi chụp (`FR-SEL-08`, `FR-SEL-09`).
- [ ] **P2.29** Chọn toàn bộ màn hình chụp bằng `Ctrl+A` (`FR-SEL-10`).
- [ ] **P2.30** Hiển thị tọa độ và kích thước vùng chọn `WxH+X+Y` (`FR-SEL-12`).
- [ ] **P2.31** Co giãn đối xứng điểm đối diện khi giữ Shift kéo handle (`FR-SEL-16`).

### Epic 13: Undo/Redo Advanced
- [ ] **P2.32** Cấu hình giới hạn số bước lưu lịch sử (`FR-UNDO-03`).
- [ ] **P2.33** Lưu toàn bộ danh sách chú thích vào mỗi snapshot (`FR-UNDO-04`).
- [ ] **P2.34** Hoàn tác việc di chuyển lớp lên/xuống (`FR-UNDO-06`).

### Epic 14: Export & Shortcuts Full
- [ ] **P2.35** Double-click vùng chọn để copy, lưu sau khi copy, copy đường dẫn file (`FR-OUT-06`–`FR-OUT-08`).
- [ ] **P2.36** Xuất byte PNG thô ra stdout và in geometry ra stdout (`FR-OUT-09`, `FR-OUT-10`).
- [ ] **P2.37** Mở ảnh bằng ứng dụng mặc định và chọn định dạng lưu PNG/JPG (`FR-OUT-11`, `FR-OUT-13`).
- [ ] **P2.38** Hiển thị thông báo Windows Toast khi lưu/copy thành công (`FR-OUT-15`, `FR-CFG-008`).
- [ ] **P2.39** Hỗ trợ đầy đủ các phím tắt còn lại: mở app khác, upload Imgur, side panel, color picker, select-all, delete, commit (`FR-SH-017`–`FR-SH-027`, `FR-SH-029`).

### Epic 16: Windows Integration & CLI Polish
- [ ] **P2.42** Console output cho các lệnh CLI, tray launcher, mở thư mục lưu (`FR-WIN-004`, `FR-SYS-004`, `FR-SYS-007`, `FR-SYS-021`).
- [ ] **P2.43** Import/export/reset cấu hình và hot-reload khi file thay đổi (`FR-SYS-011`, `FR-SYS-013`, `FR-SYS-014`).

---

## 5. Checklist chi tiết Phase 3: Advanced (Target: 08/11 → 28/11/2026)

### Epic 12: Cloud Upload
- [ ] **P3.01** Upload ảnh ẩn danh lên Imgur, xác nhận trước khi upload, tự động copy URL (`FR-UP-01`–`FR-UP-04`, `FR-CFG-025`, `FR-CFG-026`).
- [ ] **P3.02** Lịch sử upload, cấu hình API key Imgur, xóa mục lịch sử (`FR-UP-05`–`FR-UP-07`, `FR-CFG-023`, `FR-CFG-024`, `FR-CFG-027`).

### Epic 13: Precision Tools
- [ ] **P3.03** Snap-to-grid / pixel-perfect selection, tỷ lệ cố định, co giãn đối xứng (`FR-SH-022`–`FR-SH-025`, `FR-SH-011`).
- [ ] **P3.04** Kính lúp và công cụ lấy màu từ màn hình (`FR-MAG`, `FR-SH-021`, `FR-CFG-011`, `FR-CFG-012`).

### Epic 14: Customization
- [ ] **P3.05** Bảng màu tùy chỉnh, thêm/xóa/sắp xếp màu (`FR-CFG-103`, `FR-CFG-104`, `FR-CFG-200`).
- [ ] **P3.06** Tùy chỉnh thanh công cụ: ẩn/hiện nút, sắp xếp công cụ (`FR-CFG-105`, `FR-TB-01`).
- [ ] **P3.07** Ngôn ngữ giao diện, font mặc định, màu accent, độ mờ ngoài vùng chọn (`FR-CFG-100`–`FR-CFG-107`, `FR-CFG-102`).

### Epic 15: Advanced Settings
- [ ] **P3.08** Các tùy chọn nâng cao: kiểm tra cập nhật, thông báo chào mừng, cho phép nhiều instance GUI, copy JPG vào clipboard, tự động đóng daemon (`FR-CFG-006`, `FR-CFG-016`–`FR-CFG-022`, `FR-CFG-028`).

---

*Cập nhật gần nhất: 2026-09-14*

---

## 6. Báo cáo chi tiết theo task

- **P1.01:** `docs/3.Progress/03_02_P1.01_Design_Report.md`
- **P1.03:** `docs/3.Progress/03_02_P1.03_Design_Report.md` (thiết kế + triển khai Overlay State Machine)
- **P1.05:** `docs/3.Progress/03_02_P1.05_Design_Report.md` (thiết kế + triển khai Dimming Overlay)

---

> **Ghi chú về số lượng task:** `03_00_Progres_Overview.md` liệt kê chi tiết 84 task cấp `P` (Phase 0: 8, Phase 1: 29, Phase 2: 39, Phase 3: 8). Các số `P` bị khuyết (P2.18, P2.19, P2.40, P2.41) chưa được gán tính năng cụ thể trong SRS, do đó chưa tính vào tổng số. Nếu sau này bổ sung, tổng số sẽ được điều chỉnh lại.
