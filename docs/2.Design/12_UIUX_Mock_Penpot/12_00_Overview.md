# UI/UX Mock Penpot — Tổng quan

> Tài liệu này định nghĩa cấu trúc không gian làm việc Penpot để thiết kế UI/UX cho F-Shot. Các file con mô tả chi tiết từng phần: tokens, components, overlay states, toolbar layout, và export guidelines.

---

## 1. Mục đích

Mục tiêu của Epic 2.5 là tạo ra một bộ thiết kế visual đầy đủ trên Penpot để:

- Làm căn cứ cho việc viết XAML và Skia rendering trong Epic 3–5.
- Đảm bảo tính nhất quán về màu sắc, kích thước, spacing, iconography.
- Giảm thiểu việc đo đạc lại khi chuyển từ design sang code.

---

## 2. Cấu trúc không gian làm việc Penpot

Dự án Penpot được chia thành 3 Page chính:

### Page 1: `00_CommonComponent`

Chứa toàn bộ Design Tokens và Master Components. Đây là thư viện dùng chung để lắp ráp các màn hình.

Các Master Components bắt buộc cho Phase 1:

| Component | Mục đích | Ghi chú |
| :--- | :--- | :--- |
| `ToolButton` | Nút công cụ vuông `32×32` | Có 4 variants: Default, Hover, Active, Disabled. |
| `BottomToolbar_Container` | Vỏ bọc thanh công cụ `40px` cao | Chứa ToolButton instances, separator, shadow. |
| `ResizeHandle` | Điểm neo co giãn `8×8` hiển thị, hit area `16×16` | Dùng 8 instance quanh vùng chọn. |
| `DimensionBadge` | Badge kích thước `18px` cao | Hiển thị `Width × Height`. |

Các component phụ trợ:

| Component | Mục đích | Ghi chú |
| :--- | :--- | :--- |
| `ColorSwatch` | Ô màu tròn nhỏ trong Color Picker | Dùng trong `State_05_Color_Picker_Popup`. |
| `TooltipHint` | Tooltip tên công cụ + phím tắt | Không bắt buộc trong MVP. |

Icon SVG cho các nút toolbar là tài nguyên vector, không phải component. Danh sách icon nằm trong `12_02_CommonComponents.md`.

### Page 2: `01_CaptureOverlay`

Chứa các Board mô tả vòng đời trạng thái của màn hình chụp. Là trọng tâm cho Phase 1.

### Page 3: `02_WindowsShellAndSettings`

Chứa mockup cho System Tray, Pin Window, và Settings Window. Dành cho Phase 2.

---

## 3. Cây thư mục Layer mẫu

```text
📁 Page: 00_CommonComponent
  ├── 📁 Design Tokens
  │     ├── Color Tokens
  │     ├── Size & Radius Tokens
  │     └── Typography Tokens
  └── 📁 Master Components
        ├── ToolButton
        ├── BottomToolbar_Container
        ├── ResizeHandle
        ├── DimensionBadge
        ├── ColorSwatch
        └── TooltipHint

📁 Page: 01_CaptureOverlay
  ├── 🖼️ Board: State_01_Idle_Dimmed
  ├── 🖼️ Board: State_02_Dragging_Selection
  ├── 🖼️ Board: State_03_Selected_With_Toolbar
  ├── 🖼️ Board: State_04_Annotating_Mode
  └── 🖼️ Board: State_05_Color_Picker_Popup

📁 Page: 02_WindowsShellAndSettings
  ├── 🖼️ Board: Tray_Context_Menu
  ├── 🖼️ Board: Pin_Window
  └── 🖼️ Board: Settings_Window
```

---

## 4. Kết nối với các file thiết kế khác

| File | Nội dung chính | Dùng để làm gì |
| :--- | :--- | :--- |
| **12_01_DesignTokens.md** | Định nghĩa toàn bộ color, size, spacing, typography tokens. | Tạo tab Tokens trong Penpot; map 1:1 sang Avalonia ResourceDictionary. |
| **12_02_CommonComponents.md** | Mô tả Master Components: ToolButton, BottomToolbar_Container, ResizeHandle, DimensionBadge, ColorSwatch, TooltipHint. | Lắp ráp các board trạng thái; đảm bảo kích thước đồng nhất. |
| **12_03_CaptureOverlayStates.md** | 5 board: Idle Dimmed, Dragging Selection, Selected with Toolbar, Annotating Mode, Color Picker Popup. | Trực quan hóa vòng đời State Machine; làm căn cứ cho UI integration. |
| **12_04_ToolbarLayout.md** | Bố cục toolbar, nhóm công cụ, icon, phím tắt, responsive behavior. | Thiết kế thanh công cụ nổi sát vùng chọn. |
| **12_05_ExportGuidelines.md** | Quy tắc xuất tokens, icon SVG, layout metrics sang code. | Chuyển design sang Avalonia XAML và SkiaSharp rendering. |

---

## 5. Quy trình sử dụng bộ tài liệu này

1. **Tạo Penpot project** với 3 Page: `00_CommonComponent`, `01_CaptureOverlay`, `02_WindowsShellAndSettings`.
2. **Định nghĩa tokens** trong tab Tokens theo `12_01_DesignTokens.md`.
3. **Thiết kế Master Components** theo `12_02_CommonComponents.md`.
4. **Lắp ráp 5 Board** theo `12_03_CaptureOverlayStates.md`.
5. **Tinh chỉnh toolbar** theo `12_04_ToolbarLayout.md`.
6. **Xuất tài nguyên** theo `12_05_ExportGuidelines.md` khi sang Epic 3–5.

---

## 6. Liên kết với State Machine

Các board trong Page `01_CaptureOverlay` tương ứng với các trạng thái trong `docs/2.Design/08_OverlayState/`:

```text
State_01_Idle_Dimmed        ↔ Idle
State_02_Dragging_Selection ↔ Selecting
State_03_Selected_With_Toolbar ↔ Selected
State_04_Annotating_Mode    ↔ Annotating
State_05_Color_Picker_Popup ↔ Annotating với color picker mở
```

---

*Epic 2.5 là bước chuẩn bị UI/UX trước khi vào Epic 3: Annotations.*
