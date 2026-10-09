# PinWindow — Thiết kế chi tiết Cửa sổ Ghim Ảnh F-Shot (Pin Widget)

> Tài liệu này thiết kế chi tiết kiến trúc, vòng đời, hệ thống tương tác và luồng dữ liệu cho Cửa sổ Ghim Ảnh nổi (Pin Widget) của F-Shot trên nền tảng Avalonia UI & SkiaSharp (`FR-PIN-01`–`FR-PIN-10`).  
> **Nguyên tắc tài liệu:** Thiết kế thuần kiến trúc, layout thành phần, ma trận điều khiển, giải thuật hình học và sơ đồ tương tác — **không dùng placeholder, bám sát kiến trúc domain F#**.

---

## 1. Tổng quan & Ma trận Yêu cầu Kỹ thuật

Tính năng Pin Widget cho phép người dùng "ghim" một vùng ảnh chụp màn hình (kèm theo các chú thích đã vẽ) thành một cửa sổ nổi độc lập nằm thường trực trên màn hình máy tính (`Topmost`) để đối chiếu thông tin trong khi làm việc với các phần mềm khác.

| Mã SRS | Mã gốc | Tính năng & Đặc tả chi tiết | Mức độ | Cơ chế triển khai kỹ thuật (Avalonia / Skia / F#) |
| :--- | :--- | :--- | :--- | :--- |
| **FR-PIN-01** | PIN-001 | Ghim ảnh đã chụp lên một cửa sổ nổi Topmost không viền. | S | Avalonia Window: `Topmost=true`, `SystemDecorations=None`, `TransparencyLevelHint=Transparent`. |
| **FR-PIN-02** | PIN-002 | Kéo chuột để di chuyển tự do cửa sổ ghim trên desktop. | S | Xử lý `PointerPressed` / gọi `BeginMoveDrag` trên Window hoặc tính toán vị trí `Position`. |
| **FR-PIN-03** | PIN-003 | Lăn chuột để thu phóng (Zoom/Scale) cửa sổ ghim. | S | Lắng nghe `PointerWheelChanged`, cập nhật hệ số tỉ lệ `ScaleFactor` (10% – 500%). |
| **FR-PIN-04** | PIN-004 | Điều chỉnh độ trong suốt của ảnh ghim (Opacity). | S | `Ctrl + Mouse Wheel` hoặc phím tắt, gán giá trị `Window.Opacity` (0.1 – 1.0). |
| **FR-PIN-05** | PIN-005 | Xoay ảnh ghim 90° theo chiều kim đồng hồ hoặc ngược chiều. | S | Biến đổi ma trận `SKMatrix.CreateRotationDegrees`, hoán đổi kích thước cửa sổ (`Width` $\leftrightarrow$ `Height`). |
| **FR-PIN-06** | PIN-006 | Copy ảnh ghim vào clipboard qua menu ngữ cảnh. | S | ContextMenu "Sao chép ảnh" $\to$ Gọi `ClipboardService.copyToClipboardNative`. |
| **FR-PIN-07** | PIN-007 | Lưu ảnh ghim ra tệp ổ cứng qua menu ngữ cảnh. | S | ContextMenu "Lưu ảnh..." $\to$ Hộp thoại `StorageProvider.SaveFilePickerAsync` hoặc lưu nhanh. |
| **FR-PIN-08** | PIN-008 | Đóng cửa sổ ghim bằng double-click, phím Esc hoặc menu. | S | Bắt sự kiện Double-click, `KeyDown (Escape)`, ContextMenu "Đóng" $\to$ gọi `Window.Close()`. |
| **FR-PIN-09** | PIN-009 | Làm mịn ảnh chất lượng cao khi phóng to (Anti-aliasing). | C | Cấu hình `SKFilterQuality.High` / `SKPaint.IsAntialias = true` khi render Skia. |
| **FR-PIN-10** | PIN-010 | Hiệu ứng đổ bóng và viền nổi xung quanh cửa sổ ghim. | C | Bo góc viền nhẹ (Border radius 8px) + hiệu ứng Claymorphism Drop Shadow viền bán trong suốt. |

---

## 2. Kiến trúc Cửa sổ PinWindow (Window Architecture)

### 2.1 Cấu trúc Thành phần Giao diện

Cửa sổ `PinWindow` được thiết kế dưới dạng Avalonia Window không viền hệ thống, có nền trong suốt để chứa hiệu ứng đổ bóng:

```
┌────────────────────────────────────────────────────────┐
│ Avalonia Window (SystemDecorations=None, Topmost=true)  │
│ ┌────────────────────────────────────────────────────┐ │
│ │ Border (Claymorphism Shadow + 8px CornerRadius)    │ │
│ │ ┌────────────────────────────────────────────────┐ │ │
│ │ │ SKBitmapCanvas / Image Control                 │ │ │
│ │ │                                                │ │ │
│ │ │          [ NỘI DUNG ẢNH ĐƯỢC GHIM ]            │ │ │
│ │ │      (Hỗ trợ Zoom, Xoay, Tỉ lệ động)           │ │ │
│ │ │                                                │ │ │
│ │ └────────────────────────────────────────────────┘ │ │
│ └────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────┘
```

### 2.2 Thuộc tính Cấu hình Window

- `SystemDecorations`: `SystemDecorations.None` (triệt tiêu thanh tiêu đề, nút minimize/maximize/close chuẩn của Windows).
- `Topmost`: `true` (luôn luôn nổi trên tất cả các cửa sổ ứng dụng khác của hệ điều hành).
- `TransparencyLevelHint`: `WindowTransparencyLevel.Transparent` (cho phép vẽ bóng đổ mờ ra ngoài biên ảnh).
- `Background`: `Brushes.Transparent` (chỉ hiển thị nội dung bên trong Border).
- `ShowInTaskbar`: `false` (không chiếm không gian trên Windows Taskbar, tránh làm rối thanh tác vụ).
- `CanResize`: `false` (việc thay đổi kích thước được điều khiển chủ động qua sự kiện thu phóng Mouse Wheel).

### 2.3 Quản lý Tài nguyên Đồ họa (Unmanaged Resource Management)

Mỗi cửa sổ `PinWindow` nắm giữ một bản sao bitmap hoàn chỉnh đã bao gồm các nét vẽ chú thích (`SKBitmap` composite render từ `SceneComposer.renderExport`).  
Để tránh rò rỉ bộ nhớ GPU/RAM khi người dùng ghim nhiều ảnh liên tục:
1. `PinWindow` hiện thực giao diện `IDisposable` hoặc bắt sự kiện `Window.Closed`.
2. Khi cửa sổ đóng, gọi tường minh `bitmap.Dispose()` giải phóng pixel buffer unmanaged ngay lập tức.
3. Hỗ trợ mô hình đa cửa sổ (Multi-Instance): Mỗi thao tác Ghim từ thanh công cụ chụp sẽ sinh ra một thể hiện `PinWindow` độc lập; việc đóng một cửa sổ ghim không ảnh hưởng đến các cửa sổ ghim khác đang mở.

---

## 3. Hệ thống Tương tác Người dùng (Interaction Engine)

### 3.1 Kéo và Di chuyển Cửa sổ (Drag & Reposition — `FR-PIN-02`)

- Khi người dùng nhấn chuột trái (`PointerPressed`) vào bất kỳ vị trí nào trên ảnh ghim:
  - Nếu click vào không thuộc ContextMenu hay click đúp, kích hoạt phương thức kéo gốc `this.BeginMoveDrag(e)`.
  - Hỗ trợ di chuyển mượt mà trên toàn bộ không gian Virtual Screen (bao gồm di chuyển qua lại giữa các màn hình có DPI khác nhau).

### 3.2 Thu phóng Tỉ lệ động (Zoom / Scale — `FR-PIN-03`, `FR-PIN-09`)

- **Kích hoạt:** Lăn con lăn chuột (`PointerWheelChanged`) khi không giữ phím `Ctrl`.
- **Giải thuật tính tỉ lệ:**
  - Delta lăn chuột dương ($\Delta > 0$): tăng hệ số phóng đại: `scale = scale * 1.1`.
  - Delta lăn chuột âm ($\Delta < 0$): giảm hệ số phóng đại: `scale = scale / 1.1`.
  - Giới hạn tỉ lệ an toàn (Clamping): $0.1 \le \text{scale} \le 5.0$ (từ 10% đến 500% kích thước gốc).
- **Cập nhật hình học cửa sổ:**
  - Kích thước hiển thị mới: $W_{\text{new}} = W_{\text{base}} \times \text{scale}$, $H_{\text{new}} = H_{\text{base}} \times \text{scale}$.
  - Giữ tâm thu phóng tại vị trí con trỏ chuột (`Zoom toward cursor`):
    $$X_{\text{win, new}} = X_{\text{pointer, screen}} - (X_{\text{pointer, local}} \times \text{scale})$$
    $$Y_{\text{win, new}} = Y_{\text{pointer, screen}} - (Y_{\text{pointer, local}} \times \text{scale})$$
- **Khử răng cưa (`FR-PIN-09`):** Áp dụng chế độ vẽ Skia `SKFilterQuality.High` kết hợp `SKPaint.IsAntialias = true` để ảnh khi phóng to không bị vỡ hạt cục bộ.

### 3.3 Điều chỉnh Độ trong suốt (Opacity Adjustment — `FR-PIN-04`)

- **Kích hoạt:** Giữ phím `Ctrl` + Lăn con lăn chuột (`PointerWheelChanged`), hoặc nhấn phím tắt `[` / `]`.
- **Phạm vi điều chỉnh:** $0.1 \le \text{Opacity} \le 1.0$ (từ 10% mờ đến 100% đặc hoàn toàn).
- **Bước điều chỉnh:** Mỗi nấc cuộn chuột tăng hoặc giảm $0.05$ (5%).
- **Trải nghiệm trực quan:** Cửa sổ mờ dần giúp người dùng nhìn xuyên thấu xuống tài liệu / mã nguồn nằm ngay bên dưới ảnh ghim mà không cần thu nhỏ cửa sổ.

### 3.4 Xoay Ảnh Ghim 90° (Image Rotation — `FR-PIN-05`)

- **Kích hoạt:** Nhấn phím `R` (xoay 90° cùng chiều kim đồng hồ), `Shift + R` (xoay 90° ngược chiều kim đồng hồ), hoặc chọn mục từ ContextMenu.
- **Biến đổi hình học:**
  - Trạng thái góc xoay: `Angle = (Angle + 90) % 360`.
  - Khi góc xoay là $90^\circ$ hoặc $270^\circ$, chiều rộng và chiều cao của khung hiển thị tự động hoán đổi cho nhau ($W \leftrightarrow H$).
  - Ma trận Skia áp dụng: tịnh tiến về tâm, xoay theo góc, tịnh tiến ngược lại về gốc tọa độ.

### 3.5 Menu Ngữ cảnh (Context Menu — `FR-PIN-06`, `FR-PIN-07`, `FR-PIN-08`)

Nhấp chuột phải (`PointerReleased` với `PointerUpdateKind.RightButtonReleased`) vào cửa sổ ghim sẽ mở một ContextMenu Kawaii bo góc với các lệnh:

```
┌──────────────────────────────────────────┐
│  📋  Sao chép ảnh         Ctrl+C         │
│  💾  Lưu ảnh ra tệp...     Ctrl+S         │
├──────────────────────────────────────────┤
│  🔄  Xoay 90° phải         R              │
│  🔍  Khôi phục 100% zoom   Ctrl+0         │
│  👁️  Độ mờ đục: [ 100% | 75% | 50% ]      │
├──────────────────────────────────────────┤
│  ❌  Đóng ghim             Esc / Double   │
└──────────────────────────────────────────┘
```

1. **Sao chép ảnh (`FR-PIN-06`):**
   - Lấy bitmap hiện tại (áp dụng góc xoay nếu có).
   - Gọi `ClipboardService.copyToClipboardNative` xuất đồng thời định dạng `CF_DIB` và `PNG` lên clipboard hệ điều hành.
   - Phát âm thanh phản hồi `MessageBeep` và hiển thị thông báo desktop ngắn.
2. **Lưu ảnh ra tệp (`FR-PIN-07`):**
   - Mở hộp thoại `StorageProvider.SaveFilePickerAsync` cho phép người dùng chọn vị trí lưu và tên file (hoặc lưu nhanh vào `savePath` theo cấu hình).
   - Mã hóa bitmap sang PNG/JPG và ghi file an toàn.
3. **Đóng cửa sổ ghim (`FR-PIN-08`):**
   - Click đúp chuột trái vào ảnh ghim $\to$ Đóng ngay lập tức.
   - Nhấn phím `Escape` khi cửa sổ đang active $\to$ Đóng ngay lập tức.
   - Chọn "Đóng ghim" trên menu ngữ cảnh $\to$ Đóng ngay lập tức.

---

## 4. Tích hợp với Thanh Công cụ Chụp ảnh (Toolbar Integration)

1. **Thêm nút Pin trên Toolbar:**
   - Bổ sung `PinAction` vào tập `ToolbarAction` trong `src/FShot.Core/Domain/Annotation.fs`.
   - Bổ sung icon vector Kawaii Pin (ghim giấy) vào `ToolbarIcons.fs` và cập nhật danh sách nút trên thanh công cụ trong `CaptureCanvas.axaml.fs`.
2. **Kích hoạt luồng Ghim từ Overlay:**
   - Khi người dùng click nút Pin trên Toolbar hoặc nhấn phím tắt ghim:
     1. Khóa vùng chọn và tổng hợp hình ảnh (Composite image: crop screenshot gốc + render toàn bộ annotations).
     2. Tạo thể hiện mới của `PinWindow(compositeBitmap, initialRect)`.
     3. Đặt vị trí xuất hiện của `PinWindow` trùng khớp hoàn toàn với vị trí vùng chọn trên màn hình (`Position = PixelPoint(rect.X, rect.Y)`).
     4. Hiển thị cửa sổ ghim (`pinWindow.Show()`).
     5. Đóng cửa sổ interactive overlay chụp ảnh `CaptureOverlayWindow` (giải phóng toàn màn hình).

---

## 5. Quy chuẩn Đổ bóng Kawaii Claymorphism (`FR-PIN-10`)

Để cửa sổ ghim hòa hợp với phong cách chung của F-Shot và nổi bật trên mọi nền màn hình (nền sáng, nền tối, màn hình game/video):
- **Viền nổi:** `BorderThickness="1.5"`, `BorderBrush="#40FFFFFF"` kết hợp viền ngoài `#20000000`.
- **Bo góc:** `CornerRadius="8"`, giúp các góc ảnh không bị sắc nhọn.
- **Đổ bóng (Drop Shadow):** `BoxShadow="0 8 24 0 #40000000"`, tạo độ sâu trực quan tách biệt hoàn toàn cửa sổ ghim khỏi ứng dụng bên dưới.

---

## 6. Kết nối Mã nguồn & Kế hoạch Kiểm thử

- **Domain & Models:** `src/FShot.Core/Domain/Annotation.fs` (bổ sung `PinAction`), `src/FShot.Core/State/OverlayState.fs` (sự kiện `RequestPin`).
- **Giao diện Avalonia:** `src/FShot.UI/Windows/PinWindow.axaml` và `src/FShot.UI/Windows/PinWindow.axaml.fs`.
- **Tích hợp Canvas:** `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs` (xử lý dispatch `PinAction`).
- **Dịch vụ Clipboard & File:** `src/FShot.Platform.Win32/Clipboard/ClipboardService.fs`, `src/FShot.Core/Domain/Export.fs`.
- **Kiểm thử tự động:** `tests/FShot.UI.Tests/Windows/PinWindowTests.fs` (kiểm tra công thức scale, bounds clamping, góc xoay, giải phóng tài nguyên).
