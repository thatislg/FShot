# SelectionBorderAndHandles — Thiết kế chi tiết Đường viền đơn mảnh & Điểm neo lệch ngoài (Flameshot / AoE IV Style)

> Tài liệu này thiết kế lại hệ thống hiển thị và hình học điểm neo (resize handles / pins) của vùng chọn (Selection Region) trong F-Shot.  
> **Nguyên tắc bảo toàn:** Tài liệu này là bản thiết kế mới độc lập, **không sửa đổi** các tài liệu cũ (`03_03_ResizeHandles.md`, `12_01_DesignTokens.md`, `12_02_CommonComponents.md`). Dùng làm chuẩn đặc tả thay thế cho phần render và tính toán điểm neo vùng chọn trong `FShot.Core` và `FShot.UI`.

---

## 1. Bối cảnh & Lý do Thiết kế lại

### 1.1 Hạn chế của thiết kế cũ (Double-Line & Centered Clay Knobs)
Trong bản triển khai thử nghiệm trước đó (`CaptureCanvas.RenderSelectionOverlay`), vùng chọn sử dụng phong cách Kawaii Claymorphism với:
1. **Đường viền kép (Double-line border):**
   - Một đường viền ngoài nét liền dày `2.0px`.
   - Một đường viền trong nét đứt (`10px / 8px`) lùi vào trong `7.0px` (`selectionRect.Inflate(-7.0)`).
   - **Vấn đề thực tế:** Khi chụp các vùng nhỏ hoặc chụp văn bản/UI có nhiều chi tiết sát mép, đường viền nét đứt bên trong chiếm mất `7px–9px` không gian hiển thị mỗi cạnh, gây rối mắt, che khuất nội dung bên trong vùng chụp và tạo cảm giác nặng nề, thiếu sắc sảo.
2. **Điểm neo (Pin / Handle) đặt đồng tâm trên viền:**
   - 8 nút tròn lớn (`knobRadius = 6.5px`, đường kính `13px`, kèm viền trắng `2.5px`, gradient và bóng đổ) được đặt **tâm nằm đè trực tiếp lên các góc và trung điểm cạnh** (`HandleCenters` tại `Left`, `Right`, `Top`, `Bottom`).
   - **Vấn đề thực tế:** Một nửa diện tích của từng điểm neo tràn vào bên trong vùng chọn (`~6.5px`), che khuất hoàn toàn góc chữ nhật của ảnh cần chụp. Người dùng không thể căn chỉnh chính xác pixel ở 4 góc vùng chọn.

### 1.2 Triết lý thiết kế mới: Sắc gọn, Chuẩn xác & Không che khuất
Thiết kế mới kết hợp ưu điểm từ hai nguồn tham chiếu:
- **Flameshot & Các ứng dụng chụp màn hình chuẩn:** Sử dụng **đường viền đơn mảnh (Single thin line)** sắc nét (`1px`–`1.5px`) và các **điểm pin nhỏ gọn nằm tiếp xúc phía ngoài đường viền** (Outward-offset handles) để toàn bộ 100% diện tích bên trong vùng chọn (`Selection.Bounds`) luôn hiển thị nguyên vẹn, không bị bất kỳ thành phần UI nào đè lên.
- **Khung quét chuột của Age of Empires IV (AoE IV Marquee Selection):** Khi đang nhấn giữ và kéo chuột quét vùng chọn (`Selecting`), khung quét mang cảm giác chiến thuật, thanh thoát với đường viền đơn mảnh sáng rõ, lớp phủ sáng siêu nhẹ (micro-tint) lúc đang quét, và nhấn góc tinh tế mà không làm rối tầm nhìn.

---

## 2. Thiết kế Đường viền Vùng chọn (Single Thin Border & AoE IV Marquee)

### 2.1 Phân tách trạng thái hiển thị: Đang quét chuột (`Selecting`) vs. Đã cố định (`Selected`)

Trải nghiệm thị giác được tách làm 2 giai đoạn rõ rệt theo cảm hứng từ **AoE IV** và **Flameshot**:

| Thuộc tính | Giai đoạn 1: Đang kéo quét chuột (`Selecting`) | Giai đoạn 2: Đã thả chuột cố định (`Selected` / `Annotating`) |
| :--- | :--- | :--- |
| **Kiểu đường viền** | Đường đơn mảnh liên tục (Solid Single Line), sắc nét tuyệt đối | Đường đơn mảnh liên tục (Solid Single Line) |
| **Độ dày (Stroke Width)** | `1.0 px` – `1.5 px` (chuẩn hóa pixel-snapping tại tọa độ `+0.5px`) | `1.5 px` (đủ mảnh để căn pixel, đủ rõ để phân tách vùng) |
| **Màu sắc đường viền** | Trắng sáng pha Vàng kim / Xanh băng nhẹ (`#E0F2FE` hoặc `uiColor` sáng `#7BD5F5`) kèm viền tương phản ngoại biên `1px` tối mờ (`#40000000`) | Màu chủ đạo `uiColor` (mặc định Sky Blue `#38BDF8` / `#5FA8D3` hoặc Tím Flameshot) |
| **Lớp nền trong vùng quét** | Pha sáng siêu nhẹ `2% – 3%` (`#06FFFFFF`) kiểu hộp quét AoE IV để tạo cảm giác "bắt mục tiêu" khi kéo | Hoàn toàn trong suốt `0%` (`#00000000`) — hiển thị 100% màu gốc của màn hình |
| **Góc nhấn (Corner Ticks)** | *(Tùy chọn AoE IV)* 4 dấu ngoặc vuông chữ L mảnh (`6px × 6px`, dày `1.5px`) ôm sát mép ngoài 4 góc khi đang kéo | Thay thế bằng **8 điểm Pin nhỏ đặt lệch ra ngoài** (Outward Pins) |
| **Đường viền kép bên trong** | **Loại bỏ hoàn toàn** (Không vẽ `innerRect`) | **Loại bỏ hoàn toàn** (Không vẽ `innerRect`) |

### 2.2 Quy tắc căn chỉnh Pixel (Half-Pixel Alignment) cho Đường đơn mảnh
Để đường viền đơn `1px`–`1.5px` hiển thị sắc nét như dao cạo trên màn hình (không bị nhòe thành 2px mờ do khử răng cưa Anti-aliasing của Skia/Avalonia):
- Đường viền phải được vẽ **nằm sát mép ngoài** của vùng chọn vật lý (`SelectionRect`), mở rộng ra ngoài `0.5px`:
  $$\text{BorderRect} = \text{ Rect}(X_{\text{phys}} - 0.5,\; Y_{\text{phys}} - 0.5,\; W_{\text{phys}} + 1.0,\; H_{\text{phys}} + 1.0)$$
- Nhờ dịch ra ngoài `0.5px`:
  1. Đường kẻ nằm đúng tâm lưới pixel vật lý $\to$ nét đơn sắc nét tuyệt đối.
  2. Không lấn vào hàng pixel ngoài cùng của vùng ảnh chụp (`0 .. W-1`, `0 .. H-1`).

### 2.3 Bổ trợ tương phản trên mọi nền sáng/tối (Contrast Hairline)
Khi chụp trên nền trắng tinh (trang web, tài liệu Word) hoặc nền trùng màu với `uiColor`, một đường đơn mảnh duy nhất có thể bị chìm. Giải pháp chuẩn (như Flameshot / ShareX):
- Vẽ một lớp lót bóng viền (Outer Contrast Hairline) mỏng `1px` màu đen bán trong suốt (`#55000000`) ôm ngay sát mép ngoài đường viền chính, hoặc dùng chính lớp phủ tối `DimmingOverlay` làm nền tương phản tự nhiên ở phía ngoài.
- Vì `DimmingOverlay` (`ContrastOpacity ~ 70%`) đã làm tối toàn bộ khu vực bên ngoài `SelectionRect`, một đường đơn màu sáng (`#38BDF8` hoặc `#FFFFFF`) đặt tại ranh giới luôn đạt độ tương phản cao mà không cần vẽ nét đôi.

---

## 3. Thiết kế Điểm neo Vùng chọn (Small Outward-Offset Pins — Flameshot Style)

### 3.1 Thay đổi cốt lõi: Từ "Đặt tại Tâm" sang "Đặt Tiếp xúc Ngoài" (Outward Tangential Placement)

Trong mã nguồn của **Flameshot** (`SelectionWidget::updateAreas`), 8 điểm neo không đặt tâm trùng với góc/cạnh của khung chọn, mà được **đẩy lệch ra phía ngoài đường viền** sao cho:
- **4 điểm neo ở góc (`TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`):** Nằm ở phía ngoài góc tương ứng, tiếp xúc đúng vào đỉnh góc của khung chọn.
- **4 điểm neo ở cạnh (`Top`, `Bottom`, `Left`, `Right`):** Căn giữa theo chiều dài cạnh, nhưng đẩy toàn bộ thân điểm neo nằm hoàn toàn về phía ngoài của cạnh đó (trong vùng bị làm tối `Dimming`).

```
         [TopLeft]                      [Top]                      [TopRight]
             ●                            ●                            ●
              ┌───────────────────────────┴───────────────────────────┐
              │                                                       │
              │                                                       │
       [Left] ●     100% KHÔNG GIAN BÊN TRONG VÙNG CHỌN SẠCH SẼ       ● [Right]
              │      (Không có đường nét đứt, không bị pin đè góc)    │
              │                                                       │
              └───────────────────────────┬───────────────────────────┘
             ●                            ●                            ●
        [BottomLeft]                   [Bottom]                  [BottomRight]
```

**Lợi ích trực tiếp:**
1. **Không che khuất góc ảnh:** Đỉnh góc `(Left, Top)` của vùng chụp lộ hoàn toàn, người dùng nhìn thấy từng pixel chữ hoặc đường viền cửa sổ cần chụp.
2. **Nổi bật trên nền tối:** Toàn bộ 8 điểm pin nằm bên trên vùng tối `DimmingOverlay`, giúp điểm pin nhỏ nhưng vẫn cực kỳ nổi bật và dễ nhìn.

### 3.2 Kích thước Điểm Pin mới (Nhỏ gọn & Tinh tế)

Thay vì nút tròn to `13px` (`radius = 6.5px`) với bóng đổ và đốm sáng phức tạp, kích thước điểm pin được thu nhỏ lại đúng chuẩn công cụ chuyên nghiệp:

| Thông số | Thiết kế cũ (Clay Knob) | Thiết kế mới (Flameshot Slim Pin) | Ghi chú |
| :--- | :---: | :---: | :--- |
| **Hình dạng** | Tròn 3 lớp (Shadow + Gradient + Specular) | Tròn phẳng sắc nét (hoặc Vuông bo góc nhẹ `R=2px`) | Gọn gàng, đồng bộ với đường viền mảnh |
| **Bán kính hiển thị (`PinRadius` / `r`)** | `6.5 px` (Đường kính `13 px`) | **`4.0 px`** (Đường kính **`8.0 px`**) | Nhỏ hơn ~40% chiều ngang, giảm ~62% diện tích che phủ |
| **Độ dày viền Pin (`PinStroke`)** | `2.5 px` trắng | **`1.0 px`** trắng (`#FFFFFF`) | Mảnh và thanh thoát |
| **Màu tô Pin (`PinFill`)** | Gradient 2 màu phức tạp | Màu đơn đặc `uiColor` (`#38BDF8` / `#5FA8D3`) | Dễ nhận diện tức thì, render cực nhẹ |
| **Vị trí tâm Pin so với cạnh** | Khoảng cách = `0 px` (Đè lên góc) | Đẩy ra ngoài một khoảng **`offset = r`** (`4.0 px`) | Mép trong của Pin vừa chạm đúng đường viền |
| **Vùng bắt chuột (`HitTestRect`)** | `20 × 20 px` (`size 8 + 2*6`) | **`16 × 16 px`** quanh tâm Pin mới | Dù hình vẽ nhỏ (`8×8`), vùng bắt chuột vẫn rộng rãi để kéo dễ dàng |

### 3.3 Công thức Toán học cho Tâm của 8 Điểm Pin (`HandleCenters` mới)

Cho vùng chọn `Bounds` có tọa độ biên:
- $L = \text{Bounds.Left}$
- $T = \text{Bounds.Top}$
- $R = \text{Bounds.Right}$
- $B = \text{Bounds.Bottom}$
- $M_x = \frac{L + R}{2.0}, \quad M_y = \frac{T + B}{2.0}$

Gọi $r = \text{HandleRadius} = 4.0\text{ px}$ (tức `HandleSize = 8.0 px`, $r = \text{HandleSize} / 2.0$).  
Để điểm pin nằm hoàn toàn ở **phía ngoài** vùng chọn và tiếp xúc vừa khít với đường viền (tương tự Flameshot):

| Handle | Tọa độ tâm cũ (Đồng tâm trên viền) | Tọa độ tâm mới (Đẩy lệch ra ngoài — Flameshot Style) | Hình chữ nhật bao của Pin (`8×8 px`) |
| :--- | :--- | :--- | :--- |
| **`TopLeft`** | $(L,\; T)$ | **$(L - r,\; T - r)$** | $[L - 2r \dots L] \times [T - 2r \dots T]$ |
| **`Top`** | $(M_x,\; T)$ | **$(M_x,\; T - r)$** | $[M_x - r \dots M_x + r] \times [T - 2r \dots T]$ |
| **`TopRight`** | $(R,\; T)$ | **$(R + r,\; T - r)$** | $[R \dots R + 2r] \times [T - 2r \dots T]$ |
| **`Left`** | $(L,\; M_y)$ | **$(L - r,\; M_y)$** | $[L - 2r \dots L] \times [M_y - r \dots M_y + r]$ |
| **`Right`** | $(R,\; M_y)$ | **$(R + r,\; M_y)$** | $[R \dots R + 2r] \times [M_y - r \dots M_y + r]$ |
| **`BottomLeft`** | $(L,\; B)$ | **$(L - r,\; B + r)$** | $[L - 2r \dots L] \times [B \dots B + 2r]$ |
| **`Bottom`** | $(M_x,\; B)$ | **$(M_x,\; B + r)$** | $[M_x - r \dots M_x + r] \times [B \dots B + 2r]$ |
| **`BottomRight`** | $(R,\; B)$ | **$(R + r,\; B + r)$** | $[R \dots R + 2r] \times [B \dots B + 2r]$ |

> [!IMPORTANT]
> **Tính chất hình học của công thức lệch ngoài:**
> - Với 4 góc: Điểm tiếp xúc giữa hình tròn Pin (bán kính $r$) hoặc khung vuông Pin với vùng chọn nằm đúng tại đỉnh góc $(L, T), (R, T), (L, B), (R, B)$. Không có bất kỳ pixel nào của Pin lọt vào vùng $(L \dots R, T \dots B)$.
> - Với 4 trung điểm cạnh: Cạnh trong của Pin tiếp xúc đúng vào đường biên $y = T$, $y = B$, $x = L$, $x = R$.

### 3.4 Tự động ẩn Pin cạnh khi vùng chọn rất nhỏ (Adaptive Handle Visibility)
Khi người dùng kéo một vùng chọn rất hẹp (ví dụ chỉ cao `20px` để chụp 1 dòng chữ), nếu vẽ đủ cả 3 pin trên cùng một cạnh thì chúng có thể nằm sát nhau. Quy tắc thích ứng:
- **4 Pin ở góc (`TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`):** Luôn hiển thị khi `State = Selected | Moving | Resizing`.
- **2 Pin cạnh ngang (`Top`, `Bottom`):** Chỉ hiển thị khi $\text{Bounds.Width} \ge 32\text{ px}$.
- **2 Pin cạnh dọc (`Left`, `Right`):** Chỉ hiển thị khi $\text{Bounds.Height} \ge 32\text{ px}$.

---

## 4. Cơ chế Bắt chuột (Hit-Testing) & Trải nghiệm Kéo (UX Ergonomics)

### 4.1 Vùng bắt chuột bất đối xứng / Mở rộng (`HitTestHandle`)
Vì điểm pin hiển thị nhỏ gọn (`8×8 px`, $r = 4\text{ px}$) và nằm lệch ra ngoài viền, vùng bắt chuột (`HitTest`) cần được tinh chỉnh để người dùng đưa chuột vào **góc viền** hoặc **nhích nhẹ ra ngoài viền** đều bắt dính mượt mà:
- Bán kính hiển thị: `HandleRadius = 4.0 px` (`HandleSize = 8.0 px`).
- Dung sai mở rộng (`HandleTolerance`): `5.0 px` mỗi phía quanh hình vuông Pin `8×8 px`.
- Tổng vùng bắt chuột hiệu dụng cho mỗi Pin:
  $$\text{HitBox} = (8 + 2 \times 5) \times (8 + 2 \times 5) = 18 \times 18\text{ px}$$
- Với tâm Pin đặt tại $(L - 4, T - 4)$, vùng bắt chuột của `TopLeft` trải từ $[L - 13 \dots L + 5]$. Điều này có nghĩa là:
  - Khi người dùng rê chuột chạm đúng vào vạch đường viền tại góc $(L, T)$, nó vẫn nằm trọn trong vùng bắt của Pin góc (`<= L + 5`), con trỏ đổi sang `nwse-resize` ngay lập tức!
  - Đồng thời phần bên trong vùng chọn từ $(L + 5 \dots R - 5)$ hoàn toàn dành cho thao tác `Move` hoặc vẽ `Annotation` mà không bị cướp chuột bởi Pin.

### 4.2 Xử lý khi vùng chọn chạm sát mép màn hình (Screen Edge Clamping)
Khi vùng chọn mở rộng chạm sát mép màn hình (ví dụ nhấn `Ctrl + A` chọn toàn màn hình, $L = 0, T = 0, R = W_{\text{screen}}, B = H_{\text{screen}}$):
- Nếu đẩy Pin ra ngoài ($L - r = -4$), Pin sẽ nằm ngoài phạm vi cửa sổ màn hình và không nhìn thấy được.
- **Quy tắc kẹp biên hiển thị (Edge-Aware Pin Clamping):**
  Khi tính tọa độ vẽ Pin hoặc tâm Pin:
  $$C_x' = \operatorname{clamp}(C_x,\; r,\; W_{\text{screen}} - r)$$
  $$C_y' = \operatorname{clamp}(C_y,\; r,\; H_{\text{screen}} - r)$$
  Như vậy, ở trạng thái bình thường Pin luôn nằm ngoài viền; riêng khi cạnh vùng chọn chạm kịch mép màn hình ($L < 2r$), Pin tự động trượt nhẹ vào sát mép màn hình để luôn nhìn thấy và nắm kéo được.

---

## 5. Đặc tả Hiệu ứng Quét chuột kiểu AoE IV (Marquee Selection Feedback)

Khi người dùng đang ở trạng thái `Selecting` (nhấn giữ chuột trái và quét trên màn hình):

```
   (Điểm bắt đầu P_start)
        ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
        ┃ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ ┃
        ┃ ░░ Lớp phủ sáng nhẹ 2% (#06FFFFFF) ░░ ┃
        ┃ ░░ (Làm nổi bật vùng đang quét)    ░░ ┃
        ┃ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ ┃
        ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
                                           (Con trỏ hiện tại P_current)
```

1. **Khung dây đơn mảnh (Crisp Wireframe):**
   - Vẽ hình chữ nhật đơn mảnh độ dày `1.0px` – `1.5px`, nét liền (không dùng nét đứt thô), màu sáng (`uiColor` pha sáng hoặc `#E0F2FE`).
2. **Đánh dấu 4 góc (Tactical Corner Brackets):**
   - Tại 4 góc của khung đang quét, vẽ 4 chữ L nhỏ ở sát mép ngoài (chiều dài nhánh `6px`, độ dày `2.0px`, màu trắng `#FFFFFF` hoặc `uiColor`). Chi tiết này lấy cảm hứng từ giao diện chọn quân/vùng của dòng game chiến thuật AoE IV, tạo cảm giác cực kỳ nhạy, hiện đại và chuẩn xác mà không cần hiển thị 8 cục pin tròn trong lúc đang kéo.
3. **Chuyển đổi mượt sang `Selected`:**
   - Ngay khi nhả chuột (`PointerReleased`), lớp phủ sáng 2% biến mất (trả về 100% ảnh gốc), 4 dấu góc chữ L chuyển thành 8 điểm Pin tròn nhỏ `8×8 px` nằm tiếp xúc phía ngoài 4 góc và 4 cạnh, đồng thời thanh công cụ `Toolbar` xuất hiện.

---

## 6. Bảng Đối chiếu Thông số Kỹ thuật (Cũ vs. Mới)

| Hạng mục | Thiết kế cũ (Double-Line & Centered Knobs) | Thiết kế mới (Single-Line & Outward Pins) |
| :--- | :--- | :--- |
| **Viền chính (Outer Border)** | Nét liền `2.0 px` | Nét đơn mảnh **`1.5 px`** (căn tọa độ `+0.5px` ngoài biên) |
| **Viền phụ bên trong (Inner Border)** | Nét đứt `2.0 px`, thụt vào `7.0 px` (`Inflate(-7.0)`) | **Bỏ hoàn toàn** (`0 px`) |
| **Hình dáng điểm Pin (Handle)** | Hình cầu Claymorphism (Shadow + Gradient + Highlight) | Hình tròn phẳng gọn gàng (Fill `uiColor` + Viền trắng `1.0 px`) |
| **Đường kính hiển thị Pin** | `13.0 px` (`knobRadius = 6.5`) + viền `2.5 px` ($\approx 15.5\text{ px}$) | **`8.0 px`** (`pinRadius = 4.0`) + viền `1.0 px` |
| **Vị trí đặt Pin** | Tâm đặt đè lên đường viền $(L, T)$ | Tâm đẩy ra ngoài đường viền **$(L - 4, T - 4)$** (Tiếp xúc ngoài) |
| **Mức độ che khuất vùng chụp** | Che mất `7px–9px` toàn bộ 4 cạnh và góc bên trong | **`0 px`** (100% vùng bên trong sạch sẽ hoàn toàn) |
| **Trạng thái đang quét (`Selecting`)** | Cùng kiểu viền kép với trạng thái `Selected` | Khung đơn mảnh `1.2 px` + 4 góc nhấn chữ L mảnh kiểu AoE IV |

---

## 7. Hướng dẫn Ánh xạ sang Mã nguồn (Khi triển khai)

Khi thực hiện cập nhật code theo tài liệu thiết kế này, các điểm cần điều chỉnh gồm:

1. **`src/FShot.Core/Domain/Selection.fs`:**
   - Giữ `Selection.HandleSize = 8.0` (tương ứng bán kính $r = 4.0\text{ px}$).
   - Cập nhật thuộc tính `Selection.HandleCenters`: dịch tâm của 8 handles ra phía ngoài một khoảng `r = Selection.HandleSize / 2.0`:
     - `TopLeft`: `{ X = left - r; Y = top - r }`
     - `Top`: `{ X = midX; Y = top - r }`
     - `TopRight`: `{ X = right + r; Y = top - r }`
     - `Left`: `{ X = left - r; Y = midY }`
     - `Right`: `{ X = right + r; Y = midY }`
     - `BottomLeft`: `{ X = left - r; Y = bottom + r }`
     - `Bottom`: `{ X = midX; Y = bottom + r }`
     - `BottomRight`: `{ X = right + r; Y = bottom + r }`
   - Cập nhật unit tests trong `tests/FShot.Core.Tests/Domain/SelectionTests.fs` để khớp với tọa độ tâm lệch ngoài mới.

2. **`src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs` (Hàm `RenderSelectionOverlay`):**
   - **Xóa bỏ** khối vẽ `innerRect = selectionRect.Inflate(-7.0)` và `innerPen` nét đứt.
   - Đổi `outerPen` thành nét đơn mảnh `1.5` (vẽ trên `selectionRect.Inflate(0.5)` để viền nằm sát mép ngoài không ăn vào ảnh chụp).
   - Khi `selection.State = Selecting`: tô nhẹ `selectionRect` với alpha `#06FFFFFF` và vẽ 4 góc chữ L mảnh `6px`; không vẽ 8 handles.
   - Khi `selection.State = Selected | Moving | Resizing`: vẽ 8 điểm Pin tròn bán kính `4.0px` (`pinRadius = 4.0`), nền đặc `borderColor` (`uiColor`), viền trắng mảnh `1.0px` tại các vị trí `selection.HandleCenters` (có kẹp biên màn hình `clamp` nếu vùng chọn sát mép `0` hoặc ` Bounds.Width/Height`).

3. **`src/FShot.Rendering.Skia/Renderers/SceneComposer.fs`:**
   - Đồng bộ `renderSelectionBorder` (độ dày `1.5f`) và `renderSelectionHandles` theo `Selection.HandleCenters` mới.

