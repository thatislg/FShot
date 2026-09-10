# Lộ trình phát triển F-Shot (Roadmap)

> Tài liệu này định hướng phân kỳ phát triển dựa trên `003_SRS.md`.  
> Nguyên tắc: **PoC trước → MVP rút gọn → v1.0 hoàn thiện → v1.x mở rộng**.  
> Các tính năng không cần ở giai đoạn đầu vẫn được ghi nhận trong SRS và sẽ lên lịch sau.

---

## Quy trình 3 bước chính

```
[1. Chia Phase (Roadmap)]
       │ Lọc các item nhãn [M] làm Milestone 1
       ▼
[2. Thiết kế Architecture & Common Core]
       │ Xây dựng F# Domain types & Skia Canvas bridge
       ▼
[3. Scaffolding Solution & Proof of Concept (PoC)]
         Tạo repo, cấu trúc project, test chụp màn hình
```

---

## Bước 1: Chia Phase triển khai

### Phase 0: Kiểm chứng kỹ thuật nền tảng (Spike / PoC — 1 tuần)

Mục tiêu: loại bỏ rủi ro kỹ thuật lớn nhất trước khi viết domain model.

| Yêu cầu SRS | Nội dung kiểm chứng |
|-------------|---------------------|
| FR-WIN-001 | `Windows.Graphics.Capture` chụp đúng toàn bộ Virtual Screen trên Mixed DPI. |
| FR-NF-003, FR-NF-004 | Không lệch tọa độ con trỏ giữa các màn hình 100% / 125% / 150%. |
| FR-CAP-01 | Cửa sổ Avalonia borderless topmost phủ toàn Virtual Screen. |
| FR-SEL-01 | Vẽ overlay tối bằng SkiaSharp với alpha blend lên ảnh desktop. |
| FR-NF-001 | Render đạt ổn định ≥ 60 FPS khi kéo chuột / thay đổi vùng chọn. |

Deliverable: một PoC chạy được, chụp màn hình, vẽ overlay tối, kéo vùng chọn cơ bản.

---

### Phase 1: Minimum Viable Product (MVP Core — 2 đến 3 tuần)

Gom **toàn bộ mã có nhãn [M]** trong SRS. Đây là bộ tính năng nhỏ nhất để người dùng có thể chụp, vẽ và xuất ảnh.

#### Chụp màn hình [M]

| Mã SRS | Tính năng |
|--------|-----------|
| FR-CAP-01 | Overlay tương tác toàn màn hình |
| FR-CAP-02 | Chụp toàn bộ màn hình không hiện UI |
| FR-CAP-03 | Chụp một màn hình chỉ định / chứa con trỏ |
| FR-CAP-05 | Trì hoãn N mili-giây trước khi chụp |

#### Vùng chọn [M]

| Mã SRS | Tính năng |
|--------|-----------|
| FR-SEL-01 | Phủ tối phần ngoài vùng chọn |
| FR-SEL-02 | Kéo tạo bounding-box |
| FR-SEL-03 | 8 điểm neo co giãn |
| FR-SEL-04 | Di chuyển vùng chọn |
| FR-SEL-05 | Dịch chuyển 1 px bằng phím mũi tên |
| FR-SEL-06 | Co giãn 1 px bằng `Shift + Arrow` |
| FR-SEL-11 | Hủy chụp bằng `Esc` / `Ctrl+Backspace` |

#### Công cụ chú thích [M]

| Mã SRS | Tính năng |
|--------|-----------|
| FR-ANN-01 | Pencil |
| FR-ANN-02 | Line |
| FR-ANN-03 | Arrow |
| FR-ANN-04 | Rectangle (hỗ trợ bo góc) |
| FR-ANN-05 | Circle / Ellipse |
| FR-ANN-06 | Marker / Highlighter |
| FR-ANN-07 | Text |
| FR-ANN-08 | Pixelate |

#### Hoàn tác / Làm lại [M]

| Mã SRS | Tính năng |
|--------|-----------|
| FR-UNDO-01 | Undo (`Ctrl+Z`) |
| FR-UNDO-02 | Redo (`Ctrl+Shift+Z` / `Ctrl+Y`) |

#### Xuất dữ liệu [M]

| Mã SRS | Tính năng |
|--------|-----------|
| FR-OUT-01 | Lưu file (`Ctrl+S`) |
| FR-OUT-02 | Lưu vào đường dẫn cố định |
| FR-OUT-03 | Tự sinh tên file theo mẫu strftime |
| FR-OUT-04 | Save dialog khi chưa có đường dẫn cố định |
| FR-OUT-05 | Copy vào clipboard (`Ctrl+C`) |

#### Cấu hình tối thiểu [M]

| Mã SRS | Tính năng |
|--------|-----------|
| FR-CFG-001 | Đường dẫn lưu mặc định |
| FR-CFG-003 | Mẫu tên file |
| FR-CFG-200 | Màu vẽ mặc định |
| FR-CFG-201 | Độ dày nét mặc định |

#### Phím tắt tối thiểu [M]

| Mã SRS | Tính năng |
|--------|-----------|
| FR-SH-001 đến FR-SH-010 | Phím kích hoạt công cụ P/D/A/S/R/C/M/T/B/I |
| FR-SH-011 | Di chuyển vùng chọn `Ctrl+M` |
| FR-SH-012 | Undo `Ctrl+Z` |
| FR-SH-013 | Redo `Ctrl+Shift+Z` |
| FR-SH-014 | Copy `Ctrl+C` |
| FR-SH-015 | Save `Ctrl+S` |
| FR-SH-016 | Thoát / hủy `Ctrl+Q` |
| FR-SH-019 | Chấp nhận `Return` |
| FR-SH-022 | Dịch vùng chọn 1 px |
| FR-SH-023 | Co giãn vùng chọn 1 px |

#### Yêu cầu phi chức năng [M]

| Mã SRS | Tính năng |
|--------|-----------|
| FR-NF-001 | Render ≥ 60 FPS |
| FR-NF-002 | Phản hồi trong vòng một frame |
| FR-NF-003 | Xử lý Mixed DPI |
| FR-NF-004 | High-DPI awareness |
| FR-NF-008 | Xử lý lỗi graceful |

Deliverable: ứng dụng chạy độc lập, có thể chụp vùng, vẽ, undo, save/copy. Chưa cần tray icon, hotkey toàn cục, config UI.

---

### Phase 2: Trải nghiệm đầy đủ trên Windows (v1.0 — 2 đến 3 tuần)

Gom các mã **[S]** và **[P]** cần thiết cho trải nghiệm production trên Windows.

| Nhóm | Mã SRS tiêu biểu | Nội dung |
|------|------------------|----------|
| Tích hợp Windows | FR-WIN-001, FR-WIN-002, FR-WIN-005, FR-WIN-006 | `Windows.Graphics.Capture`, hotkey `Win+Shift+X`, startup, INI path |
| System tray | FR-SYS-001 đến FR-SYS-008 | Tray icon, menu Capture / Config / Open save path / Exit |
| Single instance | FR-SYS-010 | Mutex / Named Pipe |
| Config watcher | FR-SYS-011 | Reload khi INI thay đổi |
| Kính lúp & màu | FR-MAG-01, FR-MAG-03, FR-MAG-04, FR-MAG-05 | Magnifier, eyedropper, color wheel |
| Công cụ bổ sung | FR-ANN-09, FR-ANN-10, FR-ANN-11, FR-ANN-12, FR-ANN-13, FR-ANN-14, FR-ANN-18 | Invert, Circle Counter, constraints, tool size by keyboard/wheel, object edit |
| Pin Widget | FR-PIN-01 đến FR-PIN-08 | Ghim ảnh, di chuyển, zoom, opacity, rotate, copy/save/close |
| Xuất bổ sung | FR-OUT-06, FR-OUT-07, FR-OUT-08, FR-OUT-09, FR-OUT-10, FR-OUT-11, FR-OUT-13, FR-OUT-15 | Double-click copy, save-after-copy, raw PNG, geometry stdout, open app, toast |
| Vùng chọn nâng cao | FR-SEL-07, FR-SEL-08, FR-SEL-09, FR-SEL-10, FR-SEL-12, FR-SEL-16 | Symmetric resize, minimum size, constraint, select-all, XYWH display, mirror resize |
| Undo nâng cao | FR-UNDO-03 đến FR-UNDO-06 | Undo limit, object snapshots, counter restore, layer reordering |
| Cấu hình UI | FR-CFG-002, FR-CFG-004 đến FR-CFG-028, FR-CFG-100 đến FR-CFG-209 | Tab General / Interface / Tool defaults |
| Phím tắt đầy đủ | FR-SH-017 đến FR-SH-030 | Launcher, Imgur, side panel, global hotkey, PrintScreen |
| i18n & NFR | FR-NF-005, FR-NF-006 | Đa ngôn ngữ, khả năng tiếp cận bàn phím |

Deliverable: ứng dụng hoàn chỉnh, có tray, hotkey, config UI, pin widget, đủ để thay thế Flameshat cơ bản trên Windows.

---

### Phase 3: Mở rộng & Tùy biến nâng cao (v1.x — tùy chọn)

Gom các mã **[C]** và **[I]**.

| Nhóm | Mã SRS tiêu biểu | Nội dung |
|------|------------------|----------|
| Upload đám mây | FR-UP-01 đến FR-UP-07 | Imgur upload, history, client secret |
| Tùy biến nâng cao | FR-SEL-13, FR-SEL-14, FR-SEL-15 | XYWH timeout, snap-to-grid, grid display |
| Công cụ nâng cao | FR-ANN-15, FR-ANN-16, FR-ANN-17, FR-ANN-19, FR-ANN-21 | Size buttons, arrow style, reverse, hit-testing tolerance, commit tool |
| Toolbar nâng cao | FR-TB-03, FR-TB-05, FR-TB-06, FR-TB-07 | Hidden shortcut buttons, right-click size, icon contrast, size indicator |
| Pin nâng cao | FR-PIN-09, FR-PIN-10 | Anti-aliasing, drop shadow |
| Tích hợp nâng cao | FR-SYS-013, FR-SYS-014 | Import/export config, reset config |
| Windows nâng cao | FR-WIN-003, FR-WIN-004 | Ngăn Snipping Tool chiếm PrtSc, console CLI wrapper |
| Maintenance | FR-CFG-019, FR-CFG-020, FR-NF-010 | Update checker, migration INI cũ |

---

## Bước 2: Thiết kế Architecture & Common Core

Sau khi chốt Phase 1 (MVP), khoanh vùng các domain chung cần thiết:

| Layer | Nội dung | Ví dụ |
|-------|----------|-------|
| Primitives | Kiểu dữ liệu hình học và màu sắc | `Point`, `Rect`, `Color`, `StrokeWidth` |
| Capture Domain | Yêu cầu chụp và kết quả chụp | `CaptureRequest`, `CaptureResult`, `CaptureMode` |
| Selection Domain | Vùng chọn và thao tác | `Selection`, `ResizeHandle`, `SideType` |
| Annotation Domain | Các công cụ vẽ | `Tool` DU: `Pencil`, `Line`, `Arrow`, `Rectangle`, `Circle`, `Marker`, `Text`, `Pixelate` |
| History Domain | Undo / redo | `HistoryStack<'T>`, `ModificationCommand` |
| Export Domain | Xuất ảnh / clipboard / stdout | `ExportTask`, `SaveOptions`, `ClipboardTarget` |
| State Machine | Luồng trạng thái overlay | `Idle → Selecting → Selected → Annotating → Exporting` |

Chi tiết kiến trúc xem `005_Architecture.md`.

---

## Bước 3: Khởi tạo Solution (.NET / F#)

Cấu trúc solution đề xuất:

```
FShot/
├── src/
│   ├── FShot.Core/            (F# Class Library: Domain models, State, Undo logic, Geometry)
│   ├── FShot.Platform.Win32/  (C# hoặc F# Class Library: Windows.Graphics.Capture, User32 Hotkeys)
│   └── FShot.UI/              (F# Avalonia App: Custom Skia Canvas, Windows, TrayIcon)
└── tests/
    └── FShot.Core.Tests/      (F# xUnit: Test Undo/Redo stack, Bounding-box math)
```

Chi tiết scaffolding xem `008_Scaffolding_Plan.md`.

---

## Các tài liệu detail đi kèm

| File | Nội dung |
|------|----------|
| `005_Architecture.md` | Kiến trúc tổng thể, domain model F#, phân chia project, luồng dữ liệu |
| `006_Phase1_MVP_Detail.md` | Phạm vi MVP chi tiết, acceptance criteria, task breakdown |
| `007_TechStack.md` | Lựa chọn công nghệ: .NET, Avalonia, SkiaSharp, Windows.Graphics.Capture, xUnit, CI/CD |
| `008_Scaffolding_Plan.md` | Câu lệnh tạo solution, project references, file đầu tiên, PoC checklist |
