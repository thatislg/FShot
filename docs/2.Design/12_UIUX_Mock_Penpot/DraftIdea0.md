Để thiết kế UI/UX cho F-Shot trên Penpot phục vụ chuyển đổi sang Avalonia XAML và SkiaSharp, tổ chức không gian làm việc theo cấu trúc chuẩn hoá dưới đây:  

### 1. Tổ chức Trang (Pages)

Chia dự án thành 3 Page riêng biệt ở cột bên trái:

- **Page 1: `00_CommonComponent` (Thư viện dùng chung)**
  - Chứa toàn bộ Design Tokens (màu sắc, spacing, typography) và Master Components.  
  - Đây là "kho phụ tùng" để lắp ráp các màn hình mà không sợ lệch kích thước.
- **Page 2: `01_CaptureOverlay` (Trọng tâm Phase 0 & Phase 1)**
  - Thể hiện toàn bộ vòng đời trạng thái của màn hình chụp: từ lúc phủ mờ, kéo chuột, hiện toolbar đến khi vẽ chú thích.  
- **Page 3: `02_WindowsShellAndSettings` (Dành cho Phase 2)**
  - Chứa mockup cho Pin Window, Tray Context Menu và Cửa sổ Settings.  

### 2. Thiết lập Design Tokens (Nhóm biến dùng chung)

Thiết lập sẵn ở tab **Tokens** để khi xuất sang XAML, bạn chỉ cần map 1:1 sang `ResourceDictionary`:

- **Color Tokens:**
  - `Primary/Accent`: Màu tím/cam nhận diện thương hiệu Flameshot gốc (ví dụ: `#7C3AED` hoặc `#FF0055`).  
  - `Overlay/Darken`: Màu đen mờ ngoài vùng chọn: `#000000` với Alpha `0.5` hoặc `0.6`.  
  - `Selection/Border`: Viền khung chọn: `#FFFFFF` (1px/2px dashed hoặc solid) và 8 điểm neo `#00E5FF`.  
  - `Surface/Toolbar`: Nền toolbar: `#1E1E2E` (bo góc 8px, đổ bóng nhẹ).
  - `Annotation/Colors`: Bảng màu vẽ nhanh mặc định: Đỏ (`#EF4444`), Vàng (`#F59E0B`), Xanh lá (`#10B981`), Xanh dương (`#3B82F6`), Trắng, Đen.  
- **Size & Radius Tokens:**
  - `Toolbar/Height`: `40px` (đủ chuẩn touch/click).
  - `Toolbar/ItemSize`: `32x32px` (icon `18x18px`).
  - `Handle/Size`: `8x8px` hoặc `10x10px` cho 8 điểm neo.  
  - `Radius/Toolbar`: `6px` hoặc `8px`.

### 3. Cấu trúc Boards trên Page `01_Capture_Overlay_Flow`

Tạo 5 Board nằm cạnh nhau (kích thước chuẩn `1920x1080` hoặc `1280x720`) đại diện cho từng trạng thái của State Machine:  

- **Board 1: `State_01_Idle_Dimmed`**
  - Nền: Ảnh chụp desktop giả lập.  
  - Lớp phủ: Một hình chữ nhật full board, màu `#000000` với opacity 40%.  
  - Con trỏ chuột: Dấu crosshair (`+`).
- **Board 2: `State_02_Dragging_Selection`**
  - Lớp phủ: Bị khoét thủng một vùng $600 \times 400$ px (thấy rõ ảnh nền desktop bên dưới).  
  - Viền vùng chọn: Đường nét đứt màu trắng bao quanh.  
  - Tag kích thước: Một badge nhỏ hiển thị `600 x 400` ở góc trên vùng chọn.  
- **Board 3: `State_03_Selected_With_Toolbar`**
  - Vùng chọn hoàn tất: Xuất hiện 8 điểm neo hình vuông nhỏ ở 4 góc và 4 trung điểm cạnh.  
  - Component `Bottom_Toolbar`: Đặt nổi ngay sát mép dưới bên phải vùng chọn.  
- **Board 4: `State_04_Annotating_Mode`**
  - Vùng chọn có các hình vẽ mẫu: 1 nét Pencil uốn lượn, 1 mũi tên Arrow, 1 khung Rectangle bo góc, 1 vùng Pixelate dạng ô vuông bàn cờ.  
  - Nút công cụ tương ứng trên Toolbar đổi sang trạng thái `Active/Selected`.
- **Board 5: `State_05_Color_Picker_Popup`**
  - Mockup vòng tròn chọn màu (Color Wheel) hoặc popup bảng palette nhỏ bung ra khi click chuột phải.  

### 4. Xây dựng Components tại `00_Design_Tokens_&_Components`

Tạo các component nhỏ bằng **Flex Layout** để tái sử dụng:

- **Component: `ToolButton`**
  - Board nhỏ $32 \times 32$ px, Flex layout căn giữa.
  - Gồm 3 biến thể (Variants): `Default` (nền trong suốt), `Hover` (nền xám mờ `#FFFFFF20`), `Active` (nền màu Accent).
- **Component: `BottomToolbar_Container`**
  - Flex layout: Horizontal (hàng ngang), `gap: 4px`, `padding: 4px 8px`.
  - Nhét các `ToolButton` vào theo 2 nhóm nút rõ ràng (dùng 1 đường kẻ dọc 1px để phân tách):
    - **Nhóm 1 (Annotation Tools):** Pencil, Line, Arrow, Rectangle, Circle, Marker, Text, Pixelate.  
    - **Nhóm 2 (Actions):** Undo, Redo, Copy, Save, Cancel (Esc).  
- **Component: `ResizeHandle`**
  - Hình chữ nhật $8 \times 8$ px, fill màu `#00E5FF`, viền `#000000` 1px để luôn nổi trên mọi màu nền.  

### 5. Cấu trúc cây thư mục Layer mẫu trong Penpot

Plaintext

```
📁 Page: 01_Capture_Overlay_Flow
  ├── 🖼️ Board: State_03_Selected_With_Toolbar (1920x1080)
  │     ├── 📄 Background_Screenshot (Ảnh desktop)
  │     ├── ⬛ Dimmed_Mask (Khu vực phủ tối)
  │     ├── 🔲 Selection_Area (Vùng nhìn thấy ảnh rõ)
  │     │     ├── 🔲 Selection_Border
  │     │     └── 🏷️ Dimension_Badge ("800 x 500")
  │     ├── 🔘 Selection_Handles (Group 8 điểm neo)
  │     │     ├── 📍 Handle_TopLeft
  │     │     ├── 📍 Handle_TopCenter
  │     │     └── ...
  │     └── 🧩 Bottom_Toolbar (Instance từ Master Component)
```

Tổ chức này giúp bạn có cái nhìn trực quan về luồng tương tác của F-Shot và xuất từng icon SVG hoặc lấy thông số Flex padding/gap đưa vào Avalonia XAML mà không cần đo đạc lại.  