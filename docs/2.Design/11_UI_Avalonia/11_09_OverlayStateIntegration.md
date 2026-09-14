# OverlayStateIntegration — Tích hợp UI với OverlayState

> Tài liệu này mô tả cách `CaptureCanvas` và `CaptureOverlayWindow` chuyển từ tự quản lý `Selection` sang dùng `OverlayState` làm single source of truth cho toàn bộ tương tác overlay.

---

## 1. Mục đích

Hiện tại `CaptureCanvas` tự giữ `Selection`, tự gọi các phương thức của `Selection`, và tự vẽ overlay bằng `Avalonia.DrawingContext`. Điều này khiến:

- Logic input bị nhân đôi với `OverlayState` đã được kiểm thử đầy đủ.
- Các tính năng P1.09–P1.11 (nudge, Shift+resize, Esc/Ctrl+Backspace theo trạng thái) không hoạt động vì UI không đưa keyboard events vào `OverlayState`.
- Render dimming của P1.05 không được sử dụng; overlay vẫn vẽ 4 strips bằng Avalonia.

Mục tiêu của tích hợp này:

- `CaptureCanvas` chỉ chuyển input thô thành `OverlayEvent` và gửi đến `OverlayState`.
- `CaptureCanvas` nhận `OverlayResult` (state + RenderModel + commands) sau mỗi sự kiện.
- `CaptureCanvas` vẽ theo `RenderModel.Selection`, `RenderModel.Annotations`, `RenderModel.Preview`.
- `CaptureOverlayWindow` khởi tạo `OverlayState` một lần khi có `CaptureResult` và truyền vào `CaptureCanvas`.

---

## 2. Kiến trúc sau tích hợp

```text
┌─────────────────────────────────────┐
│  CaptureOverlayWindow               │
│  - Khởi tạo OverlayState            │
│  - Truyền CaptureResult + Config    │
│  - Xử lý command CloseOverlay       │
│  - (Tương lai) xử lý StartExport    │
└──────────┬──────────────────────────┘
           │
           ▼
┌─────────────────────────────────────┐
│  CaptureCanvas                      │
│  - Nhận input chuột/bàn phím        │
│  - Chuyển thành OverlayEvent        │
│  - Gọi OverlayState.update          │
│  - Vẽ theo RenderModel                │
│  - Thực thi commands (CloseOverlay)   │
└─────────────────────────────────────┘
```

`OverlayState` trở thành single source of truth. `CaptureCanvas` không còn tự quyết định chuyển trạng thái.

---

## 3. Trách nhiệm phân chia

### 3.1 `CaptureOverlayWindow`

| Nhiệm vụ | Ghi chú |
|----------|---------|
| Khởi tạo `OverlayState` | Sau khi có `CaptureResult`, gọi `OverlayState.init capture config`. |
| Truyền state vào canvas | Gọi `canvas.SetOverlayState(state)`. |
| Xử lý `CloseOverlay` | Khi nhận command, đóng cửa sổ. |
| Xử lý `ShowTextInput` / `HideTextInput` | Tương lai (P1.18). |
| Xử lý `StartExport` | Tương lai (P1.24–P1.26). |

### 3.2 `CaptureCanvas`

| Nhiệm vụ | Ghi chú |
|----------|---------|
| Nhận input | `OnPointerPressed`, `OnPointerMoved`, `OnPointerReleased`, `OnKeyDown`. |
| Chuyển tọa độ | Từ control space sang Virtual Screen space: `virtual = control + window.Position`. |
| Tạo `OverlayEvent` | `PointerPressed point`, `PointerMoved point`, `PointerReleased`, `KeyDown keyString`. |
| Gọi `OverlayState.update` | Dùng state hiện tại, event mới. |
| Lưu state mới | `state <- result.State`. |
| Vẽ lại | Dùng `result.RenderModel`. |
| Thực thi commands | `CloseOverlay` → đóng window. |

### 3.3 Không thuộc `CaptureCanvas`

- Không tự quyết định khi nào bắt đầu selecting/moving/resizing.
- Không tự tính toán geometry.
- Không tự xử lý keyboard shortcut.

---

## 4. Chuyển đổi input sang OverlayEvent

### 4.1 Chuột

`OnPointerPressed`, `OnPointerMoved`, `OnPointerReleased` đều gọi `ToVirtualPoint` rồi tạo event:

```text
virtualX = controlX + window.X
virtualY = controlY + window.Y
```

Event mapping:

| Avalonia event | OverlayEvent |
|----------------|--------------|
| `PointerPressed` | `PointerPressed(virtualPoint)` |
| `PointerMoved` | `PointerMoved(virtualPoint)` |
| `PointerReleased` | `PointerReleased` |

### 4.2 Bàn phím

`OnKeyDown` chuyển `KeyEventArgs` thành chuỗi:

```text
modifiers = []
if Ctrl -> add "Ctrl"
if Shift -> add "Shift"
if Alt -> add "Alt"

keyName = Key.ToString()

if modifiers empty -> eventKey = keyName
else -> eventKey = String.Join("+", modifiers) + "+" + keyName
```

Ví dụ:

- Phím mũi tên Right → `"Right"`
- Shift + Down → `"Shift+Down"`
- Escape → `"Escape"`
- Ctrl + Backspace → `"Ctrl+Backspace"`
- Enter → `"Enter"`
- Ctrl + S → `"Ctrl+S"`
- Ctrl + C → `"Ctrl+C"`

Lưu ý: phím Escape cũng được `CaptureOverlayWindow.KeyDown` xử lý trước để đóng window. Sau tích hợp, handler ở window cần gỡ bỏ hoặc chuyển thành gửi `KeyDown "Escape"` đến canvas. Không nên đóng window trực tiếp từ window handler nữa.

### 4.3 Toolbar / phím tắt công cụ

Trong tương lai, khi có toolbar hoặc phím tắt chuyển công cụ, UI gửi:

- `SelectTool tool`
- `SetColor color`
- `SetStrokeWidth width`
- `Undo`, `Redo`, `Copy`, `Save`, `Cancel`

Trong phạm vi P1.11, tích hợp này chỉ bao gồm chuột và bàn phím cơ bản. Toolbar chưa được triển khai runtime; việc chuyển công cụ qua phím tắt được xử lý bằng `e.Key` để tránh xung đột với bộ gõ tiếng Việt. Xem `11_05_InputHandling.md`, mục 3.2.

---

## 5. Render theo RenderModel

### 5.1 Screenshot nền

Giữ nguyên `WriteableBitmap` cache. Vẽ toàn bộ bitmap ra control bounds.

### 5.2 Dimming

Có hai phương án:

**Phương án A: Dùng `DimmingRenderer` (khuyến nghị)**

- `DimmingRenderer` đã triển khai trong `FShot.Rendering.Skia`.
- Cần tạo `SKBitmap` từ `CaptureResult.Pixels` hoặc dùng `WriteableBitmap` đã có.
- Vẽ lên `SKSurface` trong `OnRender` nếu dùng Skia.
- Phức tạp hơn nhưng đúng với P1.05 và mở rộng tốt cho P1.12+.

**Phương án B: Vẽ bằng Avalonia DrawingContext (đơn giản, MVP)**

- Dựa trên `RenderModel.Selection` (nếu có) để vẽ 4 strips tối.
- Không cần động đến `FShot.Rendering.Skia` trong bước này.
- Dễ triển khai nhưng không dùng `DimmingRenderer` đã test.

Trong phạm vi P1.11, chọn **phương án B** để giảm rủi ro. P1.12+ sẽ chuyển sang Skia renderer khi cần vẽ annotation phức tạp.

### 5.3 Vùng chọn

Dùng `RenderModel.Selection`:

- Nếu `Some selection`, vẽ viền và 8 handles.
- Handles chỉ vẽ khi `selection.State = Selected | Moving | Resizing _`.
- Trong `Selecting`, chỉ vẽ viền (không handles).

### 5.4 Cursor

`RenderModel.Cursor` cung cấp gợi ý. UI có thể bỏ qua trong P1.11 vì Avalonia có thể dựa vào `Cursor` property hoặc giữ default. Cursor chính xác theo handle được xem xét ở P2.

### 5.5 Annotations và Preview

Từ P1.12, `RenderModel.Annotations` và `RenderModel.Preview` được vẽ bằng Avalonia `StreamGeometry` trong `CaptureCanvas`. Nút vẽ hiện tại (Pencil) dùng cùng thuật toán làm mịn quadratic Bézier như `FShot.Rendering.Skia`. Các annotation tool khác sẽ được bổ sung khi triển khai P1.13–P1.19.

---

## 6. Xử lý commands

`OverlayState.update` trả về danh sách `OverlayCommand`. `CaptureCanvas` thực thi:

| Command | Hành động |
|---------|-----------|
| `CloseOverlay` | Gọi `Window.Close()` nếu có visual root. |
| `ShowTextInput position` | Tương lai: tạo TextBox tại position. |
| `HideTextInput` | Tương lai: ẩn TextBox. |
| `StartExport target` | Tương lai: gọi export service. |

Trong P1.11 chỉ cần xử lý `CloseOverlay`.

Từ P1.12, `CaptureCanvas` cũng xử lý `SelectTool` từ toolbar tạm thời được vẽ trực tiếp trên overlay. Toolbar này hiển thị khi `RenderModel.ToolbarVisible = true`, bám quanh vùng chọn theo 4 cạnh (bottom, top, right, left) tùy không gian màn hình. Xem `12_04_ToolbarLayout.md` và phần "Toolbar runtime" dưới đây.

---

## 6. Toolbar runtime tạm thời (P1.12)

Trong MVP, toolbar được vẽ trực tiếp bên trong `CaptureCanvas.Render` thay vì dùng Avalonia controls. Cách này giúp nhanh chóng kiểm tra luồng chuyển công cụ trước khi triển khai toolbar đầy đủ bằng Penpot design.

### 6.1 Vị trí

Toolbar bám quanh vùng chọn với các placement:

- `BottomHorizontal`: mặc định, dưới đáy, căn giữa.
- `TopHorizontal`: khi dưới tràn màn hình.
- `RightVertical`: khi vùng chọn hẹp hơn toolbar ngang hoặc không đủ chỗ trên/dưới.
- `LeftVertical`: khi phải không đủ chỗ.

### 6.2 Hit-test

Khi `OnPointerPressed`, `CaptureCanvas` kiểm tra xem click có nằm trong toolbar không trước khi gửi `PointerPressed` vào `OverlayState`. Nếu trúng, gửi `SelectTool` tương ứng.

### 6.3 Render

Toolbar vẽ nền tối trong suốt, border mỏng, các nút `S P L A R C M T B`, và viền trắng cho nút active.

### 6.4 Giới hạn

- Chưa dùng icon SVG; hiện tại dùng chữ cái tạm thời.
- Chưa có nhóm Action (Undo, Redo, Copy, Save, Cancel).
- Icon Flameshot đã được sao chép sang `docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/` nhưng chưa được tích hợp vào runtime.

---

## 7. Quy trình khởi tạo

1. `App` tạo `CaptureOverlayWindow`.
2. `CaptureOverlayWindow.ShowOverlayAsync` chụp màn hình và hiển thị window.
3. Sau khi có `CaptureResult`, `CaptureOverlayWindow` gọi `OverlayState.init capture config`.
4. `CaptureOverlayWindow` gọi `captureCanvas.SetOverlayState(initialState)`.
5. `CaptureCanvas` vẽ theo `buildRenderModel initialState`.
6. Từ đây, mọi input đều đi qua `OverlayState.update`.

---

## 8. Thay đổi cần thiết trên code

### 8.1 `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`

- Bỏ trường `selection` riêng; thay bằng `overlayState: OverlayState option`.
- Bỏ logic `OnPointerPressed/Moved/Released` cũ; thay bằng gửi `OverlayEvent`.
- Sửa `OnKeyDown` để gửi `KeyDown keyString`.
- Thêm xử lý `SelectTool` từ phím tắt vật lý (`e.Key`) để tránh IME chặn phím.
- Thêm `SetOverlayState`.
- Sửa `Render` để vẽ theo `RenderModel`, bao gồm toolbar tạm thời quanh vùng chọn.
- Thêm helper chuyển `KeyEventArgs` sang chuỗi.
- (P1.12+) Tách logic toolbar vào module riêng để dễ bảo trì.

### 8.2 `src/FShot.UI/Windows/CaptureOverlayWindow.axaml.fs`

- Bỏ `KeyDown` handler đóng window trực tiếp; để `CaptureCanvas` xử lý qua `OverlayState`.
- Sau `SetCaptureResult`, gọi `OverlayState.init` và `canvas.SetOverlayState`.
- Lưu trữ `ConfigSnapshot` hoặc dùng `ConfigSnapshot.Default`.

### 8.3 `src/FShot.Core/State/OverlayState.fs`

- Không cần thay đổi nếu `RenderModel.Selection` đã đủ.
- Có thể cần điều chỉnh `buildRenderModel.ToolbarVisible` để ẩn toolbar trong `Moving`/`Resizing` nếu không muốn hiển thị.

### 8.4 `tests/FShot.Rendering.Skia.Tests`

- Không đụng chạm.

### 8.5 `tests/FShot.Core.Tests`

- Không đụng chạm.

---

## 9. Phạm vi P1.12 tích hợp UI

Trong P1.12, tích hợp UI đạt đến mức:

- [x] `CaptureCanvas` dùng `OverlayState` cho mọi input chuột.
- [x] `CaptureCanvas` gửi keyboard events đúng format đến `OverlayState`.
- [x] `CaptureCanvas` vẽ theo `RenderModel.Selection`, `RenderModel.Annotations`, `RenderModel.Preview`.
- [x] `Esc` / `Ctrl+Backspace` hoạt động đúng theo trạng thái.
- [x] Nudge và Shift+Arrow hoạt động trên overlay thực tế.
- [x] `CloseOverlay` command đóng cửa sổ.
- [x] Phím tắt chuyển công cụ `P/L/A/R/C/M/T/B/S` hoạt động, xử lý qua `e.Key` để tránh IME.
- [x] Toolbar tạm thời hiển thị quanh vùng chọn và cho phép click chọn tool.

Không bao gồm:

- Toolbar đầy đủ với icon SVG, nhóm Action (Undo, Redo, Copy, Save, Cancel).
- `DimmingRenderer` Skia (dùng Avalonia 4 strips đơn giản).
- `StartExport` command.
- `ShowTextInput` / `HideTextInput`.

---

## 10. Rủi ro và cách giảm thiểu

| Rủi ro | Giảm thiểu |
|--------|-----------|
| Threading: `SetCaptureResult` chạy trên background async, nhưng `SetOverlayState` cần UI thread. | Gọi `SetOverlayState` qua `Dispatcher.UIThread.Post`. |
| `CaptureCanvas` không focus nên không nhận `OnKeyDown`. | Gọi `this.Focus()` trong `OnAttachedToVisualTree` và sau khi window show. |
| `KeyDown` ở `CaptureOverlayWindow` đóng window trước khi canvas xử lý. | Gỡ bỏ handler ở window; để canvas xử lý. |
| Bộ gõ tiếng Việt chặn phím tắt công cụ (`P`, `L`, `A`, `R`, `C`, `M`, `T`, `B`, `S`). | Dùng `e.Key` thay vì chuỗi `ToKeyString()` cho các phím tắt tool; ghi nhận là tech debt cần giải quyết sau. Xem `11_05_InputHandling.md`, mục 3.2. |
| RenderModel có `Selection = None` trong `Idle` nhưng vẫn cần dimming toàn màn hình. | Vẽ 4 strips phủ toàn control khi không có selection. |

---

## 11. Kết nối với các file thiết kế khác

- **08_05_RenderModel.md**: dữ liệu UI dùng để vẽ.
- **08_06_Integration.md**: trách nhiệm UI/State.
- **11_04_RenderControl.md**: vẽ overlay trước tích hợp.
- **11_05_InputHandling.md**: mapping input sang events, phím tắt tool, và tech debt IME.
- **12_04_ToolbarLayout.md**: vị trí và hành vi toolbar bám quanh vùng chọn.
- **03_05_KeyboardOperations.md**: hành vi Esc/Ctrl+Backspace, nudge, Shift+Arrow.

---

*Tích hợp này biến `OverlayState` từ module đã test đơn lẻ thành trung tâm điều khiển thực tế của overlay. Sau khi hoàn thành, P1.05–P1.12 sẽ hoạt động đúng trên UI.*
