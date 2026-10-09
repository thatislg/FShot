# Constraints & Tool Sizing — Thiết kế chi tiết Ràng buộc Hình học và Điều chỉnh Kích thước Công cụ

> Tài liệu này thiết kế chi tiết cơ chế ràng buộc góc và tỉ lệ hình học khi giữ phím `Ctrl`, cùng với các cơ chế điều chỉnh kích thước công cụ vẽ bằng bàn phím và con lăn chuột (`FR-ANN-11`–`FR-ANN-14`, `FR-TB-07`).  
> **Nguyên tắc tài liệu:** Thiết kế thuần giải thuật hình học/lượng giác, xử lý sự kiện đầu vào UI và phản hồi trực quan — **không dùng placeholder**.

---

## 1. Ma trận Yêu cầu Kỹ thuật

| Mã SRS | Mã gốc | Tính năng & Đặc tả chi tiết | Mức độ | Cơ chế triển khai kỹ thuật (Domain F# / Avalonia / Skia) |
| :--- | :--- | :--- | :--- | :--- |
| **FR-ANN-11** | ANN-011 | Giữ `Ctrl` khi vẽ line/arrow/marker để ràng buộc góc vuông (90°) hoặc chéo (45°). | S | Giải thuật lượng giác `atan2`, làm tròn góc tới bội số của $\pi/4$ ($45^\circ$). |
| **FR-ANN-12** | ANN-012 | Giữ `Ctrl` khi vẽ rectangle/circle để giữ tỉ lệ 1:1 (hình vuông / hình tròn). | S | Ràng buộc kích thước: $\text{Width} = \text{Height} = \max(\Delta x, \Delta y)$ bảo toàn hướng kéo. |
| **FR-ANN-13** | ANN-013 | Gõ số trên bàn phím để đặt chính xác kích thước công cụ. | S | Đệm phím số (`DigitBuffer`) trong `CaptureCanvas` hoặc phím tắt số 1–9. |
| **FR-ANN-14** | ANN-014 | Lăn con lăn chuột để tăng/giảm độ dày nét vẽ (Stroke thickness). | S | Bắt sự kiện `PointerWheelChanged`, tính bước nhảy và clamp trong khoảng $[1.0 .. 50.0]$. |
| **FR-TB-07** | TB-007 | Hiển thị tạm thời kích thước công cụ khi thay đổi bằng bàn phím/lăn chuột. | C | Chỉ báo trực quan nổi `SizeIndicatorBox` hiển thị số pixel trong 1.2 giây rồi mờ dần. |

---

## 2. Ràng buộc Góc Lượng giác 45°/90° (Angle Constraints — `FR-ANN-11`)

### 2.1 Phạm vi Áp dụng

Áp dụng cho các công cụ định hướng hai điểm hoặc danh sách điểm định hướng:
- **Line:** Đường thẳng từ điểm bắt đầu $A(x_1, y_1)$ đến điểm kết thúc $B(x_2, y_2)$.
- **Arrow:** Mũi tên định hướng từ $A(x_1, y_1)$ đến $B(x_2, y_2)$.
- **Marker:** Đoạn thẳng highlight khi vẽ dạng thước kẻ.

### 2.2 Giải thuật Toán học (Trigonometric Snapping)

Cho vector dịch chuyển từ điểm đặt bút $A$ đến vị trí con trỏ chuột hiện tại $B$:
$$\Delta x = x_2 - x_1, \quad \Delta y = y_2 - y_1$$
Độ dài vector (khoảng cách thực tế):
$$L = \sqrt{\Delta x^2 + \Delta y^2}$$
Nếu $L < 2.0$ pixel: giữ nguyên điểm $B$ để tránh chia cho 0 hoặc rung lắc nhỏ.

Góc thực tế của con trỏ chuột:
$$\theta = \operatorname{atan2}(\Delta y, \Delta x) \quad (\theta \in [-\pi, \pi])$$
Chia đường tròn thành 8 cung góc $45^\circ$ ($\pi/4$ radian):
$$\text{snapStep} = \frac{\pi}{4} = 45^\circ$$
Góc sau khi làm tròn (Snapping):
$$\theta_{\text{snapped}} = \operatorname{round}\left(\frac{\theta}{\text{snapStep}}\right) \times \text{snapStep}$$
Tọa độ điểm kết thúc bị ràng buộc $B'(x'_2, y'_2)$:
$$x'_2 = x_1 + L \times \cos(\theta_{\text{snapped}})$$
$$y'_2 = y_1 + L \times \sin(\theta_{\text{snapped}})$$

**Các góc chuẩn đạt được:**
- $0^\circ$ (nằm ngang sang phải): $\Delta y = 0, \Delta x > 0$
- $45^\circ$ (chéo xuống dưới phải): $\Delta x = \Delta y > 0$
- $90^\circ$ (thẳng đứng xuống dưới): $\Delta x = 0, \Delta y > 0$
- $135^\circ$ (chéo xuống dưới trái)
- $180^\circ$ / $-180^\circ$ (nằm ngang sang trái): $\Delta y = 0, \Delta x < 0$
- $-135^\circ$ (chéo lên trên trái)
- $-90^\circ$ (thẳng đứng lên trên): $\Delta x = 0, \Delta y < 0$
- $-45^\circ$ (chéo lên trên phải)

### 2.3 Tương tác Người dùng

- Người dùng có thể nhấn giữ phím `Ctrl` trước khi bắt đầu kéo chuột hoặc bấm phím `Ctrl` ngay giữa chừng khi đang kéo: nét vẽ tức thời "hít" (snap) vào phương góc gần nhất.
- Khi người dùng nhả phím `Ctrl`, nét vẽ lập tức trả về theo vị trí tự do của con trỏ chuột.

---

## 3. Ràng buộc Tỉ lệ 1:1 (Aspect-Ratio Constraints — `FR-ANN-12`)

### 3.1 Phạm vi Áp dụng

- **Rectangle:** Ép thành hình vuông hoàn hảo ($\text{Width} = \text{Height}$).
- **Circle:** Ép từ hình elip thành hình tròn hoàn hảo ($R_x = R_y$).

### 3.2 Giải thuật Hình học

Cho điểm bắt đầu $A(x_1, y_1)$ và điểm con trỏ hiện tại $B(x_2, y_2)$:
$$dx = x_2 - x_1, \quad dy = y_2 - y_1$$
$$\text{signX} = \operatorname{sign}(dx), \quad \text{signY} = \operatorname{sign}(dy)$$
Xác định độ dài cạnh lớn nhất để hình vuông mở rộng tự nhiên theo cử động chuột:
$$\text{side} = \max(|dx|, |dy|)$$
Tọa độ điểm kết thúc ràng buộc $B'(x'_2, y'_2)$:
$$x'_2 = x_1 + \text{signX} \times \text{side}$$
$$y'_2 = y_1 + \text{signY} \times \text{side}$$

Bounding box hình học kết quả có $\text{Width} = \text{Height} = \text{side}$, bảo toàn hoàn toàn hướng kéo của người dùng (kéo sang 4 góc phần tư: Đông Bắc, Đông Nam, Tây Nam, Tây Bắc).

---

## 4. Điều chỉnh Kích thước Công cụ bằng Bàn phím (Keyboard Tool Sizing — `FR-ANN-13`)

### 4.1 Cơ chế Đệm Phím Số (Numeric Input Buffer)

Khi người dùng đang ở chế độ vẽ (Annotation Mode) và gõ các phím số từ `0` đến `9`:
1. `CaptureCanvas` lắng nghe sự kiện `KeyDown`.
2. Nếu ký tự là chữ số và không đi kèm modifier nguy hiểm (`Ctrl`/`Alt`):
   - Đưa chữ số vào bộ đệm `numericBuffer: string`.
   - Khởi tạo bộ đếm thời gian debounce (Timer 800ms).
   - Nếu trong vòng 800ms người dùng gõ thêm chữ số (ví dụ: gõ `2` rồi gõ tiếp `4`), bộ đệm gom thành `"24"`.
   - Khi hết thời gian debounce hoặc người dùng nhấn phím `Enter`:
     - Phân giải chuỗi thành số nguyên: `newSize = Int32.Parse(numericBuffer)`.
     - Giới hạn phạm vi hợp lệ: $\text{clampedSize} = \operatorname{clamp}(1.0, 50.0, \text{float}(newSize))$.
     - Gửi sự kiện `ChangeToolSize(clampedSize)` tới `OverlayState`.
     - Xóa bộ đệm.
3. Kích hoạt hiển thị chỉ báo kích thước `SizeIndicatorBox` (`FR-TB-07`).

---

## 5. Điều chỉnh Kích thước Công cụ bằng Con lăn Chuột (Mouse Wheel Sizing — `FR-ANN-14`)

### 5.1 Xử lý Sự kiện `PointerWheelChanged`

- Khi con trỏ chuột nằm trong vùng chọn hoặc đang chọn công cụ vẽ (không ở trạng thái kéo chỉnh kích thước vùng chọn):
  - Lăn chuột lên trên ($\text{Delta.Y} > 0$): Tăng kích thước công cụ.
  - Lăn chuột xuống dưới ($\text{Delta.Y} < 0$): Giảm kích thước công cụ.
- **Bước nhảy thích ứng (Adaptive Step Sizing):**
  - Nếu kích thước hiện tại $< 10.0\text{ px}$: mỗi nấc lăn thay đổi $\pm 1.0\text{ px}$.
  - Nếu kích thước hiện tại $\ge 10.0\text{ px}$: mỗi nấc lăn thay đổi $\pm 2.0\text{ px}$.
- **Ràng buộc phạm vi:**
  $$\text{StrokeWidth} \in [1.0 .. 50.0]\text{ px}$$
- **Đối tượng áp dụng:**
  - Với công cụ vẽ nét (`Pencil`, `Line`, `Arrow`, `Rectangle`, `Circle`, `Marker`): thay đổi độ dày nét vẽ `StrokeWidth`.
  - Với công cụ văn bản `Text`: thay đổi cỡ chữ `FontSize` trong khoảng $[10.0 .. 96.0]$.
  - Với công cụ `Pixelate`: thay đổi kích thước khối mosaic `BlockSize` trong khoảng $[4 .. 40]$.
  - Với công cụ `CircleCounter`: thay đổi đường kính huy hiệu trong khoảng $[16.0 .. 64.0]$.

---

## 6. Chỉ báo Kích thước Nổi Tạm thời (Size Indicator Box — `FR-TB-07`)

### 6.1 Giao diện Trực quan Kawaii Claymorphism

Khi kích thước công cụ thay đổi qua bàn phím hoặc con lăn chuột, một huy hiệu thông báo nhỏ xuất hiện ngay cạnh con trỏ chuột hoặc ở tâm vùng chọn:

```
┌───────────────────────────────┐
│  🖌️  Nét vẽ: 8 px             │
│  [●═════════════════]         │
└───────────────────────────────┘
```

- **Đặc tả hình ảnh:**
  - Nền đen mờ cao cấp `#E01E293B`, viền sáng bo tròn mềm mại `CornerRadius="16"`.
  - Hiển thị chấm tròn có đường kính đúng bằng kích thước nét vẽ thực tế để người dùng dễ hình dung.
  - Văn bản hiển thị số pixel rõ ràng (ví dụ: `"12 px"`).
- **Vòng đời hiển thị:**
  - Xuất hiện tức thì khi có sự kiện lăn chuột / gõ số.
  - Sau 1.2 giây không có thao tác cuộn mới, tự động mờ dần (Fade out trong 300ms) rồi biến mất để không che khuất màn hình.

---

## 7. Kết nối Mã nguồn & Kế hoạch Kiểm thử

- **Thuật toán hình học:** `src/FShot.Core/Geometry/AngleSnapping.fs` (các hàm `snapAngle45`, `snapSquare`).
- **State Machine:** `src/FShot.Core/State/OverlayState.fs` (nhận `SetStrokeWidth`, `SetFontSize`, xử lý cờ `Ctrl` trong `UpdatePointer`).
- **Canvas Interaction:** `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs` (xử lý `PointerWheelChanged`, `KeyDown`, hiển thị `SizeIndicatorBox`).
- **Unit Tests:** `tests/FShot.Core.Tests/Geometry/AngleSnappingTests.fs` (kiểm thử 8 góc chuẩn, kiểm thử bảo toàn cạnh vuông 1:1, kiểm thử clamp kích thước).
