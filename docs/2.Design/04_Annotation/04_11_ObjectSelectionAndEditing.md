# Object Selection & Editing — Thiết kế chi tiết Chọn, Di chuyển và Chỉnh sửa Chú thích

> Tài liệu này thiết kế chi tiết chế độ Chọn đối tượng chú thích (Object Selection & Edit Mode), giải thuật kiểm tra va chạm hình học (Hit-Testing) kèm bán kính dung sai, các thao tác di chuyển, xóa đối tượng và phím tắt kết thúc chỉnh sửa văn bản (`FR-ANN-18`–`FR-ANN-21`).  
> **Nguyên tắc tài liệu:** Thiết kế thuần giải thuật khoảng cách hình học, kiến trúc cấu trúc dữ liệu bất biến (Immutable State) và tương tác người dùng — **không dùng placeholder**.

---

## 1. Ma trận Yêu cầu Kỹ thuật

| Mã SRS | Mã gốc | Tính năng & Đặc tả chi tiết | Mức độ | Cơ chế triển khai kỹ thuật (Domain F# / SkiaSharp) |
| :--- | :--- | :--- | :--- | :--- |
| **FR-ANN-18** | ANN-018 | Click vào chú thích cũ để chọn, di chuyển hoặc sửa nội dung. | S | Bổ sung trạng thái `SelectedAnnotation: Annotation option` trong `OverlayState`. |
| **FR-ANN-19** | ANN-019 | Tìm chú thích gần con trỏ ngay cả khi không click trúng pixel (Dung sai). | S | Giải thuật Hit-Testing khoảng cách điểm tới đoạn thẳng/đường cong với dung sai $\text{tolerance} \ge 6.0\text{ px}$. |
| **FR-ANN-20** | ANN-020 | Xóa đối tượng chú thích đang chọn bằng phím `Delete` hoặc `Backspace`. | S | Lọc phần tử khỏi danh sách `Annotations` bất biến và ghi snapshot vào `HistoryStack`. |
| **FR-ANN-21** | ANN-021 | Kết thúc chỉnh sửa văn bản tại chỗ bằng tổ hợp phím `Ctrl+Return`. | S | Bắt sự kiện phím trong TextBox inline, commit phẳng vào danh sách chú thích. |

---

## 2. Giải thuật Kiểm tra Va chạm Hình học (Hit-Testing Engine — `FR-ANN-19`)

### 2.1 Bán kính Dung sai (Tolerance Radius)

Trên màn hình độ phân giải cao hoặc khi nét vẽ mảnh (ví dụ: line dày 2px), người dùng rất khó click chuột trúng pixel của nét vẽ.  
Hệ thống áp dụng bán kính dung sai:
$$\text{Tolerance} = 6.0\text{ px (logical)}$$
Khoảng cách cho phép click trúng:
$$D_{\text{threshold}} = \frac{\text{StrokeWidth}}{2} + \text{Tolerance}$$

### 2.2 Công thức Khoảng cách cho Từng Loại Công cụ

1. **Điểm tới Đoạn thẳng (Line & Arrow):**
   - Cho đoạn thẳng nối từ $A(x_1, y_1)$ đến $B(x_2, y_2)$ và vị trí click chuột $P(x_0, y_0)$.
   - Vector $\vec{v} = B - A$, vector $\vec{w} = P - A$.
   - Chiếu vô hướng chuẩn hóa:
     $$t = \frac{\vec{w} \cdot \vec{v}}{\|\vec{v}\|^2} = \frac{(x_0 - x_1)(x_2 - x_1) + (y_0 - y_1)(y_2 - y_1)}{(x_2 - x_1)^2 + (y_2 - y_1)^2}$$
   - Giới hạn $t$ trong đoạn $[0, 1]$: $t_{\text{clamped}} = \max(0, \min(1, t))$.
   - Điểm gần nhất trên đoạn thẳng: $Q = A + t_{\text{clamped}} \times \vec{v}$.
   - Khoảng cách từ $P$ đến đoạn thẳng:
     $$d(P, AB) = \|P - Q\| = \sqrt{(x_0 - x_Q)^2 + (y_0 - y_Q)^2}$$
   - Thỏa mãn va chạm khi $d(P, AB) \le D_{\text{threshold}}$.

2. **Điểm tới Chu vi Hình chữ nhật (Rectangle):**
   - Khoảng cách từ điểm $P$ đến 4 cạnh của hình chữ nhật: $d = \min(d_{\text{top}}, d_{\text{bottom}}, d_{\text{left}}, d_{\text{right}})$.
   - Nếu hình chữ nhật được tô đặc (Fill): điểm $P$ nằm trong Rect là trúng.

3. **Điểm tới Chu vi Hình tròn/Elip (Circle):**
   - Với hình tròn tâm $C$, bán kính $R$:
     $$d(P, \text{Circle}) = |\|P - C\| - R|$$
   - Thỏa mãn va chạm khi $d(P, \text{Circle}) \le D_{\text{threshold}}$.

4. **Điểm tới Đường cong Bút vẽ tự do (Pencil / Marker):**
   - Danh sách điểm $P_1, P_2, \dots, P_n$.
   - Tính khoảng cách từ $P$ tới từng đoạn con $P_i P_{i+1}$:
     $$d_{\min} = \min_{i=1}^{n-1} d(P, P_i P_{i+1})$$
   - Thỏa mãn va chạm khi $d_{\min} \le D_{\text{threshold}}$.

5. **Bounding Box Hit-Testing (Text, Invert, Pixelate, CircleCounter, Icon):**
   - Kiểm tra điểm $P$ có nằm trong bounding box mở rộng của đối tượng:
     $$x_{\min} - \text{Tolerance} \le x_0 \le x_{\max} + \text{Tolerance}$$
     $$y_{\min} - \text{Tolerance} \le y_0 \le y_{\max} + \text{Tolerance}$$

### 2.3 Thứ tự Ưu tiên Lớp (Z-Order Selection Priority)

Khi người dùng click vào vị trí có nhiều chú thích chồng lên nhau:
- Quét danh sách chú thích theo thứ tự từ **mới nhất đến cũ nhất** (từ đỉnh stack xuống đáy).
- Đối tượng được vẽ sau cùng (nằm ở lớp trên cùng) sẽ được chọn trước tiên.

---

## 3. Chế độ Chọn và Khung Điều khiển Đối tượng (Selection & Transform — `FR-ANN-18`)

### 3.1 Khung Bao Lựa chọn (Selection Bounding Box & Handles)

Khi một chú thích được chọn:
- Vẽ khung chữ nhật đứt nét màu xanh accent (Kawaii Blue `#38BDF8`) bao quanh Bounding Box của đối tượng.
- Vẽ 4 điểm neo tròn nhỏ (Control Handles) tại 4 góc với nền trắng, viền xanh dương đậm 2px để báo hiệu đối tượng đang ở trạng thái kích hoạt.

```
┌───○───────────────────────────○───┐
│   ┊                           ┊   │
│   ┊   [ ĐỐI TƯỢNG ĐƯỢC CHỌN ] ┊   │
│   ┊                           ┊   │
└───○───────────────────────────○───┘
```

### 3.2 Di chuyển Đối tượng (Translation Dragging)

- Khi người dùng nhấn chuột trái vào bên trong khung bao của đối tượng đang chọn và kéo chuột:
  - Tính vector dịch chuyển: $\vec{\Delta} = (\Delta x, \Delta y) = (x_{\text{curr}} - x_{\text{start}}, y_{\text{curr}} - y_{\text{start}})$.
  - Tịnh tiến toàn bộ tọa độ các điểm thành phần của đối tượng theo vector $\vec{\Delta}$:
    - `Line(A, B)` $\to$ `Line(A + \vec{\Delta}, B + \vec{\Delta})`.
    - `CircleCounter(C, idx, r)` $\to$ `CircleCounter(C + \vec{\Delta}, idx, r)`.
    - `Pencil(points)` $\to$ `Pencil(points |> List.map (fun p -> p + \vec{\Delta}))`.
  - Hiển thị vị trí mới trong thời gian thực trong khi kéo.
  - Khi người dùng thả chuột (`PointerReleased`): commit vị trí mới vào danh sách chú thích và đẩy một Snapshot vào `HistoryStack` (cho phép hoàn tác di chuyển bằng `Ctrl+Z`).

---

## 4. Xóa Chú thích Đang chọn (Delete Selected Annotation — `FR-ANN-20`)

- **Kích hoạt:** Khi có đối tượng đang chọn (`SelectedAnnotation = Some ann`), người dùng nhấn phím `Delete` hoặc `Backspace`.
- **Hành vi State Machine:**
  1. Loại bỏ chú thích có `Id = ann.Id` khỏi danh sách `Annotations`:
     $$\text{Annotations}_{\text{new}} = \text{Annotations} \mid> \text{List.filter} (\lambda a. a.Id \ne ann.Id)$$
  2. Đặt lại `SelectedAnnotation = None`.
  3. Đẩy một Snapshot mới vào `HistoryStack` để người dùng có thể khôi phục lại đối tượng nếu bấm nhầm bằng `Ctrl+Z`.
  4. Yêu cầu vẽ lại Canvas (`InvalidateVisual`).

---

## 5. Kết thúc Chỉnh sửa Văn bản Bằng Phím Tắt (Commit Text — `FR-ANN-21`)

- **Bối cảnh:** Khi người dùng sử dụng công cụ `TextTool` hoặc click đúp vào chú thích văn bản cũ để sửa nội dung, một TextBox overlay inline xuất hiện trên Canvas.
- **Quy tắc phím tắt:**
  - Nhấn `Enter` thông thường: Xuống dòng mới (hỗ trợ văn bản nhiều dòng).
  - Nhấn `Ctrl + Enter` (hoặc `Ctrl + Return`): Kết thúc nhập liệu và commit ngay lập tức (`CommitCurrentTool`).
  - Khi commit: nội dung trong TextBox được phẳng hóa thành `Annotation` với kiểu `Text(position, content, alignment)`, TextBox được ẩn đi, và tiêu điểm bàn phím trả về cho Canvas.
  - Nhấn `Escape` khi đang gõ text: Hủy bỏ chỉnh sửa (bỏ qua nội dung vừa gõ nếu là text mới, hoặc giữ nguyên nội dung cũ).

---

## 6. Kết nối Mã nguồn & Kế hoạch Kiểm thử

- **Thuật toán hình học:** `src/FShot.Core/Geometry/HitTesting.fs` (hiện thực các hàm `distancePointToSegment`, `isPointNearAnnotation`).
- **Domain State:** `src/FShot.Core/State/OverlayState.fs`:
  - Mở rộng kiểu `AnnotationInteraction` với trạng thái `AnnotationSelected of Annotation * isDragging: bool * dragStart: Point`.
  - Bổ sung sự kiện `SelectAnnotation of Point`, `MoveSelectedAnnotation of Point`, `DeleteSelectedAnnotation`, `CommitInlineText`.
- **UI Canvas:** `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs` (bắt sự kiện phím `Delete`, `Backspace`, `Ctrl+Enter`, vẽ selection handles).
- **Unit Tests:** `tests/FShot.Core.Tests/Geometry/HitTestingTests.fs` (kiểm thử khoảng cách va chạm với dung sai cho từng tool, kiểm thử chọn theo Z-order, kiểm thử tịnh tiến tọa độ).
