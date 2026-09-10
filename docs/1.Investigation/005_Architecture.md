# Kiến trúc tổng thể F-Shot

> Tài liệu này mô tả kiến trúc phần mềm cho F-Shot, định hướng triển khai bằng **F# + Avalonia + SkiaSharp** trên Windows.  
> Chỉ chứa **tư duy thiết kế, nguyên tắc và phương châm** — không chứa code implementation mẫu.

---

## 1. Nguyên tắc thiết kế

| Nguyên tắc | Ý nghĩa |
|------------|---------|
| **Domain ở F#, Platform/UI tách biệt** | Logic chụp, vùng chọn, chú thích, undo, xuất dữ liệu đặt trong `FShot.Core`. Project này phải là pure F#, không phụ thuộc UI framework hay platform API. |
| **Rendering bằng bitmap backing store** | Không tạo nhiều `Shape` Avalonia cho từng nét vẽ. Toàn bộ scene được vẽ xuống một backing bitmap, sau đó hiển thị lên màn hình. Giúp kiểm soát pixel, tối ưu hiệu năng và dễ test. |
| **State Machine rõ ràng cho overlay** | Overlay có các trạng thái rõ ràng: Idle, Selecting, Selected, Annotating, Exporting. Mỗi trạng thái chỉ nhận một tập hợp input nhất định. |
| **Undo/Redo bằng immutable snapshots** | Mỗi thao tác tạo ra một snapshot toàn bộ trạng thái chú thích. Khi undo/redo, hệ thống thay thế trạng thái hiện tại bằng snapshot cũ. |
| **Platform-specific cô lập trong `FShot.Platform.Win32`** | Capture, hotkey, tray icon, startup registry, clipboard đều nằm trong project riêng. `FShot.Core` chỉ định nghĩa abstraction; project platform triển khai chi tiết Win32/WinRT. |
| **SkiaSharp tách riêng khỏi Core** | `FShot.Rendering.Skia` là project duy nhất trong nhóm Core được phép dùng SkiaSharp. `FShot.Core` giữ purity bằng cách dùng kiểu dữ liệu thuần. |

---

## 2. Phân chia project

```
FShot/
├── src/
│   ├── FShot.Core/                 (F# Class Library — pure domain)
│   │   ├── Domain/                 (Capture, Selection, Annotation, History, Export, Config)
│   │   ├── Geometry/               (Point, Rect, Color, Math helpers)
│   │   └── State/                  (OverlayState Machine, input transition logic)
│   ├── FShot.Rendering.Skia/       (F# Class Library — render bridge)
│   │   ├── Renderers/              (Screenshot, Selection, Annotation, Toolbar, Magnifier)
│   │   └── Converters/             (Domain → Skia types)
│   ├── FShot.Platform.Win32/       (F# hoặc C# Class Library — Windows-specific)
│   │   ├── Capture/                (Windows.Graphics.Capture, screen enumeration)
│   │   ├── Hotkeys/                (RegisterHotKey, low-level keyboard hook)
│   │   ├── Tray/                   (TrayIcon, native menu)
│   │   ├── Clipboard/              (Win32 clipboard operations)
│   │   ├── FileSystem/             (Save dialog, file write)
│   │   ├── Config/                 (Read/write config at %APPDATA%)
│   │   └── Startup/                (Registry Run key)
│   └── FShot.UI/                   (F# Avalonia Desktop App)
│       ├── SkiaCanvas/             (Custom Skia render control)
│       ├── Windows/                (Capture overlay window, config window, pin window)
│       ├── ViewModels/             (MVVM for config, tray menu)
│       └── App.axaml               (Avalonia application entry)
├── tests/
│   ├── FShot.Core.Tests/           (F# xUnit — domain, geometry, history)
│   └── FShot.Rendering.Skia.Tests/ (F# xUnit — reference image comparison)
└── FShot.sln
```

### Quy tắc phụ thuộc

```
FShot.UI
    ├──▶ FShot.Core
    ├──▶ FShot.Rendering.Skia
    └──▶ FShot.Platform.Win32

FShot.Rendering.Skia ──▶ FShot.Core
FShot.Platform.Win32 ──▶ FShot.Core
FShot.Core.Tests ──▶ FShot.Core
FShot.Rendering.Skia.Tests ──▶ FShot.Rendering.Skia
```

- `FShot.Core` không được phép phụ thuộc bất kỳ project nào khác.
- `FShot.Rendering.Skia` phụ thuộc `FShot.Core` và dùng SkiaSharp.

### Quyết định đã chốt

- **SkiaSharp location**: tách riêng `FShot.Rendering.Skia`, không để trong `FShot.Core`.

---

## 3. Domain model F#

### 3.1 Primitives

Các kiểu dữ liệu cơ bản trong `FShot.Core.Geometry`:

- **Point**: tọa độ 2D trong không gian Virtual Screen.
- **Rect**: hình chữ nhật với vị trí và kích thước.
- **Color**: màu RGBA.
- **StrokeWidth**: độ dày nét vẽ.

Nguyên tắc: dùng **immutable records**. Mọi phép toán hình học trả về giá trị mới.

### 3.2 Capture Domain

Các khái niệm chính:

- **ScreenId**: định danh màn hình.
- **CaptureMode**: toàn màn hình, một màn hình, overlay tương tác, vùng đã chọn trước.
- **CaptureRequest**: chế độ, độ trễ, vùng chọn ban đầu, accept-on-select.
- **CaptureResult**: ảnh bitmap abstraction (byte array + width + height + stride + pixel format), Virtual Bounds, scale factor.

Lưu ý: `CaptureResult` không chứa `System.Drawing.Bitmap` hay `SkiaSharp.SKBitmap`. Project platform chuyển đổi từ nguồn capture sang abstraction.

### 3.3 Selection Domain

- **Selection**: vùng chọn hiện tại.
- **ResizeHandle**: 8 vị trí neo (4 góc + 4 cạnh).
- **SelectionState**: đang di chuyển, đang resize handle nào.

Mọi thao tác trên selection (nudge, resize, move) đều là pure function nhận `Selection` + `Input` → `Selection` mới.

### 3.4 Annotation Domain

Mô hình hóa công cụ chú thích bằng **Discriminated Union (DU)**:

| Tool | Tham số chính |
|------|---------------|
| Pencil | danh sách điểm |
| Line | 2 điểm |
| Arrow | 2 điểm + kiểu mũi tên + cờ đảo hướng |
| Rectangle | 2 điểm + bán kính bo góc |
| Circle/Ellipse | 2 điểm + cờ khóa tỉ lệ |
| Marker | 2 điểm |
| Text | vị trí + nội dung + thuộc tính font |
| Pixelate | 2 điểm + kích thước ô + cờ insecure |
| Invert | 2 điểm |
| CircleCounter | vị trí + chỉ số |

Mỗi `Annotation` gồm: id duy nhất, tool, màu, độ dày, thời điểm tạo.

### 3.5 History Domain

- Mỗi bước lưu **snapshot toàn bộ danh sách chú thích và chỉ số counter**.
- `History` giữ hai stack: undo và redo.
- Khi có thao tác mới: push undo, xóa redo.
- Khi undo: chuyển snapshot hiện tại sang redo, khôi phục snapshot trước đó.
- Giới hạn stack (ví dụ 100).

### 3.6 Export Domain

- **FileFormat**: PNG / JPG.
- **SaveOptions**: đường dẫn, mẫu tên file, định dạng, chất lượng JPG.
- **ExportTarget**: lưu file, copy clipboard, raw PNG stdout, geometry stdout, mở app mặc định.

`FShot.Core` tính toán export target; `FShot.Platform.Win32` thực hiện IO.

### 3.7 Config Domain

Chia làm 3 nhóm:

- **General**: đường dẫn lưu, mẫu tên file, định dạng, startup, tray, notification, upload.
- **Interface**: màu sắc, độ mờ overlay, palette, ngôn ngữ, font.
- **Tool Defaults**: màu vẽ, độ dày, cỡ font, cỡ counter, cỡ pixelate, cỡ bo góc, cỡ marker, kiểu mũi tên.

`FShot.Core` định nghĩa schema và default values; platform đọc/ghi file.

---

## 4. State Machine cho overlay

| Trạng thái | Mô tả |
|------------|-------|
| **Idle** | Overlay hiển thị, chưa có vùng chọn. |
| **Selecting** | Đang kéo chuột tạo vùng chọn. |
| **Selected** | Đã có vùng chọn, có thể resize/move/chọn tool. |
| **Annotating** | Đang vẽ một chú thích cụ thể. |
| **Exporting** | Đang thực hiện lưu/copy/xuất. |

### Luồng chuyển trạng thái

```
Idle ──MouseDown──▶ Selecting
Selecting ──MouseUp──▶ Selected
Selected ──ToolShortcut──▶ Annotating
Selected ──Return──▶ Exporting
Selected ──Esc──▶ Idle
Annotating ──Commit──▶ Selected (push snapshot)
Exporting ──Complete──▶ Idle
```

Câu hỏi: có nên tách **Moving** và **Resizing** thành state riêng hay gộp trong **Selected**? Quyết định trong PoC.

---

## 5. Luồng dữ liệu tổng thể

```
[Input]
  │
  ▼
FShot.UI ──chuyển events──▶ FShot.Core.State
  │
  ▼
FShot.Core.State ──cập nhật──▶ Selection / Annotations / History
  │
  ▼
FShot.Core.State ──trả về──▶ RenderModel
  │
  ▼
FShot.Rendering.Skia ──vẽ──▶ Bitmap
  │
  ▼
FShot.UI ──hiển thị──▶ Screen
  │
  ▼
[Export command]
  ├── FShot.Core ──tính──▶ ExportTarget
  ├── FShot.Rendering.Skia ──encode──▶ PNG/JPG bytes
  └── FShot.Platform.Win32 ──thực hiện──▶ File / Clipboard / Stdout
```

Nguyên tắc: **one-way data flow**. UI không tự ý thay đổi domain state.

---

## 6. Rendering strategy

### 6.1 Backing store

- **Screenshot layer**: ảnh desktop gốc, bất biến trong phiên chụp.
- **Composition layer**: kết hợp screenshot, overlay tối, vùng chọn, chú thích, toolbar, kính lúp.

Có thể vẽ trực tiếp mỗi frame hoặc cache screenshot và chỉ vẽ phần thay đổi.

### 6.2 Thứ tự vẽ mỗi frame

1. Ảnh desktop gốc.
2. Overlay tối ngoài vùng chọn.
3. Đường viền vùng chọn.
4. Chú thích đã commit.
5. Chú thích đang vẽ dở (preview).
6. 8 điểm neo.
7. Thanh công cụ.
8. Kính lúp (nếu bật).

### 6.3 Tối ưu hiệu năng

- Cache geometry path của chú thích đã commit.
- Chỉ invalid vùng thay đổi khi di chuyển vùng chọn.
- Đo FPS trong dev build.

---

## 7. Module ngoài phạm vi MVP

Các tính năng sau thuộc Phase 2/v1.0:

- Tray icon và tray menu.
- Global hotkey.
- Pin Widget window.
- Config UI (MVP chỉ đọc/ghi file config).
- Imgur upload.

Tuy nhiên, nên để lại **extension point** trong kiến trúc, ví dụ `IUploadProvider` abstraction trong `FShot.Core`.

---

## 8. Câu hỏi cần quyết định trước khi scaffold

| Câu hỏi | Phương án cần cân nhắc |
|---------|------------------------|
| .NET version? | .NET 8 LTS vs .NET 9 |
| Avalonia minor version? | Chọn exact khi scaffold |
| `FShot.Platform.Win32` dùng F# hay C#? | F# đồng nhất; C# cho P/Invoke phức tạp |
| Capture strategy? | `Windows.Graphics.Capture` chính; fallback BitBlt nếu cần |
| Config format? | Đọc INI cũ Flameshot; ghi JSON cho F-Shot mới |
| State machine granularity? | Gộp Moving/Resizing trong Selected hay tách riêng? |
| Bitmap abstraction? | `byte[] + metadata` trong Core; Skia chỉ trong Rendering |

---

*Tài liệu này sẽ được cập nhật khi kiến trúc chi tiết hơn trong quá trình triển khai Phase 0/1.*

**Chọn Phương án B (Tách `FShot.Rendering.Skia` riêng)**, nhưng **giữ implementation cực mỏng**, tuyệt đối không over-engineer layer abstraction.  

### Phân tích so sánh 2 phương án

| **Tiêu chí**                       | **Phương án A: Skia nằm trong FShot.Core  MD**               | **Phương án B: Tách FShot.Rendering.Skia riêng  MD**         |
| ---------------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **Bản chất Domain**                | Bị "ô nhiễm" bởi C++ native wrapper (`SKPoint`, `SKRect`, `SKColor`, `SKBitmap`). | **Pure F# Domain**: Dùng F# Record thuần (`Point`, `Rect`, `ColorRgba`). |
| **Unit Test (`FShot.Core.Tests`)** | Test logic (nudge, resize, undo) phải load binary native `libSkiaSharp.dll`. Dễ lỗi platform/CI. | **Test chạy siêu tốc**: 100% test logic hình học, clipping, undo stack không cần nạp native C++ DLL. |
| **Quản lý bộ nhớ (Memory/GC)**     | `SKBitmap` dùng unmanaged memory (`IDisposable`). Nhét vào F# Immutable State rất dễ rò rỉ RAM (leak) hoặc lỗi use-after-free. | State F# chỉ giữ dữ liệu mô tả (Data Descriptor). Bộ nhớ đồ họa unmanaged được gom toàn bộ trong Rendering layer. |
| **Chi phí mapping**                | 0 (Dùng thẳng struct của Skia).                              | Cần vài hàm ánh xạ 1 dòng (`toSkPoint`, `toSkRect`, `toSkColor`). |

### Mô hình phân tầng thực tế

Tránh bẫy "tạo interface cho mọi thứ" (như `IRenderer`, `IDrawable`), kiến trúc tối ưu nhất gồm 4 project:

```
                  ┌──────────────────────┐
                  │      FShot.Core      │  (Pure F#: Types, Geometry, State, History)
                  └──────────┬───────────┘
                             │
            ┌────────────────┴────────────────┐
            ▼                                 ▼
┌────────────────────────┐       ┌────────────────────────┐
│ FShot.Platform.Win32   │       │  FShot.Rendering.Skia  │
│ (WGC/BitBlt, Hotkeys)  │       │  (F# + SkiaSharp)      │
└───────────┬────────────┘       └────────────┬───────────┘
            │                                 │
            └────────────────┬────────────────┘
                             ▼
                  ┌──────────────────────┐
                  │       FShot.UI       │  (Avalonia App Shell)
                  └──────────────────────┘
```

### Quyết định 8 câu hỏi kiến trúc trước khi Scaffold (Mục 8)

| **Vấn đề**             | **Quyết định chốt**                                  | **Lý do**                                                    |
| ---------------------- | ---------------------------------------------------- | ------------------------------------------------------------ |
| **.NET Version**       | **.NET 8 LTS**                                       | Ổn định dài hạn, hệ sinh thái SkiaSharp / Avalonia tương thích hoàn hảo. |
| **Avalonia Version**   | **11.1.x / 11.2.x**                                  | Phiên bản hoàn thiện nhất của Avalonia 11, hỗ trợ native AOT và GPU composition. |
| **SkiaSharp Location** | **Tách `FShot.Rendering.Skia`**                      | Giữ Core thuần khiết để unit test không phụ thuộc DLL unmanaged C++. |
| **Ngôn ngữ Win32**     | **F#** cho wrapper; **C#** nếu P/Invoke quá phức tạp | Ưu tiên **C# Class Library** cho `FShot.Platform.Win32` vì Windows WinRT/COM P/Invoke trên C# dễ dùng hơn nhiều so với F#. Core và UI vẫn dùng F#. |
| **Capture Strategy**   | **`Windows.Graphics.Capture` (WGC)**                 | Khắc phục triệt để lỗi Mixed DPI. Fallback sang GDI/BitBlt nếu WinRT capture thất bại. |
| **Config Format**      | **JSON** nội bộ; bổ sung parser đọc `flameshot.ini`  | JSON thân thiện với .NET, dễ mở rộng.                        |
| **State Machine**      | **Tách riêng** `Moving` và `Resizing`                | Giúp pattern matching trong F# rõ ràng, tránh lồng ghép quá nhiều `if/else` khi nhận event chuột. |
| **Bitmap Abstraction** | **`byte[]` + Metadata** trong Core                   | `SKBitmap` giữ ở Rendering layer để quản lý vòng đời unmanaged memory. |

