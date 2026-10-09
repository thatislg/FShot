# ConfigWindow — Thiết kế chi tiết Cửa sổ Cài đặt F-Shot

> Tài liệu này thiết kế chi tiết giao diện người dùng (UI/UX), kiến trúc phân nhóm tab, hành vi tương tác và luồng dữ liệu cho Cửa sổ Cài đặt của F-Shot (`FR-SYS-006`, `FR-CFG-100`–`FR-CFG-209`, `FR-CFG-003`, `FR-SYS-014`).  
> **Nguyên tắc tài liệu:** Thiết kế thuần kiến trúc, layout thành phần, ma trận điều khiển và sơ đồ tương tác — **hoàn toàn không sử dụng code mẫu**.

---

## 1. Triết lý Thiết kế và Thông số Cửa sổ

- **Phong cách thẩm mỹ:** Tuân thủ hệ thống thiết kế Kawaii Claymorphism (các khối card bo góc mềm mại, độ nổi viền đa sắc thái, màu sắc pastel thanh lịch, hỗ trợ cả giao diện Sáng và Tối).
- **Kích thước chuẩn:** Chiều rộng 720 pixel logic, chiều cao 540 pixel logic.
- **Giới hạn kích thước:** Kích thước tối thiểu 640x480 pixel, cho phép co giãn linh hoạt.
- **Vị trí xuất hiện:** Nằm ở chính giữa màn hình hoạt động chính của người dùng khi được mở.
- **Quy tắc Single-Instance:** Tại một thời điểm chỉ tồn tại tối đa một cửa sổ cài đặt. Nếu người dùng tiếp tục click "Cài đặt" từ khay hệ thống Tray hoặc chạy lệnh `fshot config`, ứng dụng kích hoạt đưa cửa sổ đang mở lên trên cùng (`Topmost` tạm thời) và lấy tiêu điểm (`Focus`) thay vì mở thêm cửa sổ mới.

---

## 2. Bố cục Tổng thể (Window Layout Architecture)

Cửa sổ được chia làm 3 khu vực chính theo chiều dọc:

```
┌────────────────────────────────────────────────────────────────────────┐
│ 1. Header: Tiêu đề F-Shot Settings & Nút đóng                          │
├────────────────────────────────────────────────────────────────────────┤
│ 2. Nội dung chính: Điều hướng Tab (TabControl)                          │
│ ┌──────────────┬──────────────┬──────────────────┬──────────────────┐  │
│ │ Cài đặt chung│  Giao diện   │Công cụ mặc định  │     Phím tắt     │  │
│ └──────────────┴──────────────┴──────────────────┴──────────────────┘  │
│                                                                        │
│  [Khu vực nội dung tương ứng của Tab được chọn - Có thanh cuộn dọc]    │
│                                                                        │
├────────────────────────────────────────────────────────────────────────┤
│ 3. Footer Bar:                                                         │
│ [ Khôi phục mặc định ]           [ Hủy ]  [ Áp dụng ]  [ Lưu & Đóng ]  │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Đặc tả Chi tiết Các Tab Chức năng

### 3.1 Tab 1: Cài đặt Chung (General Settings)

Tab này quản lý các hành vi xuất tệp, lưu trữ và tích hợp hệ điều hành:

| Nhóm chức năng | Tên điều khiển (Control) | Kiểu điều khiển | Đặc tả hành vi & Ràng buộc |
| :--- | :--- | :--- | :--- |
| **Thư mục lưu ảnh** | Đường dẫn lưu mặc định | Text Box + Nút Duyệt thư mục | Hiển thị đường dẫn thư mục hiện tại. Bấm Duyệt mở hộp thoại chọn thư mục Windows (`FolderPicker`). |
| | Lưu tức thì không hỏi lại | Toggle Switch | Khi bật (`savePathFixed = true`), bấm Lưu trên thanh công cụ sẽ ghi thẳng file vào thư mục mà không hiện Save Dialog. |
| **Mẫu tên file** | Trình biên soạn tên tệp | Component `FileNameEditor` | Xem đặc tả riêng tại Mục 4 bên dưới. |
| **Định dạng file xuất** | Định dạng mặc định | ComboBox / Radio Buttons | Cho phép chọn giữa `PNG` (chuẩn không nén dữ liệu) và `JPG` (nén có tổn hao). |
| | Chất lượng ảnh JPG | Slider (Thanh trượt) | Phạm vi từ 1 đến 100%. Hiển thị nhãn số bên cạnh. Chỉ kích hoạt khi định dạng là JPG. |
| **Vòng đời & Hệ thống** | Khởi động cùng Windows | Toggle Switch | Ghi/xóa Registry key `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. |
| | Hiển thị icon trên khay hệ thống | Toggle Switch | Bật/tắt biểu tượng Tray trên Windows Taskbar. |
| | Thông báo khi chụp/lưu thành công | Toggle Switch | Bật/tắt thông báo desktop nhỏ sau khi lưu hoặc copy ảnh. |
| | Thông báo khi hủy thao tác chụp | Toggle Switch | Bật/tắt thông báo khi người dùng nhấn Esc để hủy overlay. |
| | Sao chép khi Double-click | Toggle Switch | Tự động copy vùng chọn vào clipboard khi nhấp đúp chuột trái vào trong vùng chọn. |

---

### 3.2 Tab 2: Giao diện & Hiển thị (Interface Settings)

Tab này điều chỉnh màu sắc ứng dụng và mức độ hiển thị các thành phần:

| Nhóm chức năng | Tên điều khiển | Kiểu điều khiển | Đặc tả hành vi |
| :--- | :--- | :--- | :--- |
| **Bảng màu giao diện** | Màu Accent chính | Color Picker Button | Màu chủ đạo của thanh công cụ, viền vùng chọn đang kích hoạt. Mở bảng chọn màu trực quan. |
| | Màu tương phản phụ | Color Picker Button | Màu nền phụ và các điểm nhấn viền thứ cấp. |
| **Độ mờ lớp phủ** | Độ mờ vùng tối ngoài vùng chọn | Slider (0% – 100%) | Tương ứng giá trị `contrastOpacity` từ 0 (trong suốt) đến 255 (tối đen hoàn toàn). Giá trị mặc định 190 (~75%). |
| **Ngôn ngữ** | Ngôn ngữ giao diện | ComboBox | Danh sách chọn: Tự động theo hệ điều hành (Auto), Tiếng Việt (vi), English (en). |
| **Thanh công cụ Toolbar** | Tùy biến nút công cụ | Reorderable Checkbox List | Danh sách tất cả các công cụ vẽ và nút hành động. Cho phép bỏ tích để ẩn bớt nút không dùng và di chuyển nút lên/xuống để đổi thứ tự trên thanh công cụ. |

---

### 3.3 Tab 3: Công cụ Mặc định (Tool Defaults)

Tab này cấu hình các giá trị khởi tạo sẵn mỗi khi người dùng bắt đầu vẽ:

| Thuộc tính | Kiểu điều khiển | Khoảng giá trị | Giá trị mặc định | Mô tả |
| :--- | :--- | :--- | :--- | :--- |
| **Màu vẽ mặc định** | Component `ColorSelector` | Mã màu hex `#RRGGBB` | `#FF0000` | Màu chú thích mặc định (`drawColor`, `FR-CFG-200`) áp dụng cho mọi công cụ vẽ (Line, Rect, Text…). Chọn qua ô nhập hex, nút bánh xe màu hoặc 8 ô màu sẵn. |
| **Bảng màu vẽ tùy chọn** | Component `ColorSelector` | Bảng 8 ô màu tròn | 8 ô màu có sẵn | Cho phép bấm vào từng ô để đổi màu, gõ hex hoặc chọn trên bánh xe màu rồi bấm "Thêm màu" để thêm màu mới (thay màu cũ nhất khi đủ 8 ô). |
| **Độ dày nét vẽ chung** | Slider kèm số hiển thị | 1.0 – 50.0 px | 2.0 px | Áp dụng cho Bút vẽ (Pencil), Đường thẳng (Line), Mũi tên (Arrow), Hình chữ nhật và Hình tròn. |
| **Cỡ chữ Text mặc định** | Slider kèm số hiển thị | 10.0 – 72.0 pt | 18.0 pt | Cỡ chữ ban đầu khi gõ hộp văn bản Text. |
| **Kích thước huy hiệu số** | Slider kèm số hiển thị | 16.0 – 64.0 px | 28.0 px | Đường kính vòng tròn đếm số tự động tăng (Circle Counter). |
| **Kích thước khối Mosaic** | Slider kèm số hiển thị | 4 – 32 px | 10 px | Độ to của ô vuông che mờ (Pixelate). |
| **Bán kính bo góc hình chữ nhật** | Slider kèm số hiển thị | 0.0 – 40.0 px | 0.0 px | Độ cong góc khi vẽ công cụ Rectangle (0 là góc vuông). |

---

### 3.4 Tab 4: Phím tắt Hệ thống (Shortcuts Settings)

Tab này cho phép xem và tùy biến toàn bộ phím tắt trong ứng dụng:

- **Cấu trúc bảng phím tắt:**
  - Cột 1: Tên hành động (ví dụ: "Chụp tương tác GUI", "Chụp toàn màn hình", "Công cụ Bút vẽ", "Hoàn tác").
  - Cột 2: Phím tắt hiện tại (Hiển thị dạng thẻ pill trực quan, ví dụ `Ctrl` + `Z`, `PrintScreen`).
  - Cột 3: Nút "Đổi phím" và nút "Đặt lại mặc định".
- **Hành vi Ghi nhận Phím mới (Key Recording Flow):**
  1. Người dùng bấm "Đổi phím", thẻ phím chuyển sang trạng thái nhấp nháy "Đang chờ bấm phím...".
  2. Ứng dụng lắng nghe tổ hợp phím thực tế người dùng ấn xuống bàn phím.
  3. Kiểm tra tính hợp lệ:
     - Nếu phím thuộc danh mục cấm của hệ thống (`Ctrl+Alt+Del`, `Win+L`, `Alt+Tab`): Báo đỏ cảnh báo "Phím tắt bị hệ điều hành bảo vệ".
     - Nếu phím bị trùng với một hành động khác trong F-Shot: Hiển thị cảnh báo xung đột kèm tùy chọn hoán đổi phím.
  4. Người dùng bấm ra ngoài hoặc nhấn Enter để xác nhận phím mới.

---

## 3.5. Thành phần `ColorSelector` (dùng lại)

`ColorSelector` là UserControl chọn màu dùng chung cho Tab 2 (màu accent chính / màu tương phản phụ) và Tab 3 (màu vẽ mặc định / bảng màu tự chọn). Gồm đúng 3 thành phần, không được phép bỏ bất kỳ thành phần nào:

```
[ ColorPicker (mở bánh xe màu) ] [ ô nhập hex #RRGGBB ] [ Thêm màu ]
[ 8 ô màu tròn (swatch) ]
```

1. **Nút "ColorPicker":** mở Flyout chứa `ColorView` (bánh xe màu RGB + slider) để chọn màu trực quan, trả về mã hex.
2. **Ô nhập hex:** nhập mã `#RRGGBB` trực tiếp; placeholder `#RRGGBB`, mặc định trống.
3. **Nút "Thêm màu":** thêm màu hiện tại (từ bánh xe/ô hex) vào bảng 8 ô; màu mới chèn lên đầu và thay màu cũ nhất khi đủ 8 ô.
4. **Bảng 8 ô màu:** 8 màu cơ bản mặc định (Đỏ, Cam đậm, Vàng, Xanh lá, Ngọc lam, Xanh lam, Tím, Hồng). **Ô màu đang được chọn** có viền accent `#38BDF8` dày 2px để phân biệt với các ô khác (viền mảnh mờ 1px).

Mã nguồn: `src/FShot.UI/Windows/ColorSelector.axaml(.fs)` (`FR-CFG-100/101/104/200`).

---

## 4. Đặc tả Chi tiết Thành phần `FileNameEditor` (P2.14, `FR-CFG-003`)

Thành phần `FileNameEditor` là một điều khiển độc lập được tích hợp trong Tab 1, bao gồm 4 lớp thành phần:

```
┌────────────────────────────────────────────────────────────────────────┐
│ Nhãn: Mẫu định dạng tên tệp                                            │
│ [ fshot_%Y-%m-%d-%H%M%S                              ] [ Nút xóa ]     │
├────────────────────────────────────────────────────────────────────────┤
│ Thẻ token chèn nhanh:                                                  │
│ [ + %Y (Năm) ] [ + %m (Tháng) ] [ + %d (Ngày) ]                        │
│ [ + %H (Giờ) ] [ + %M (Phút)  ] [ + %S (Giây) ]                        │
├────────────────────────────────────────────────────────────────────────┤
│ Live Preview:                                                          │
│ 👁 Xem trước: fshot_2026-10-08-130545.png                              │
├────────────────────────────────────────────────────────────────────────┤
│ [Vùng hiển thị lỗi xác thực nếu có ký tự cấm \ / : * ? " < > |]        │
└────────────────────────────────────────────────────────────────────────┘
```

### Quy tắc Hoạt động của Live Preview:
1. **Bộ đếm thời gian thực:** Cập nhật nội dung chuỗi xem trước mỗi giây theo đồng hồ hệ thống.
2. **Chèn token thông minh:** Khi người dùng bấm nút thẻ `+ %Y`, chuỗi `%Y` được chèn đúng vào vị trí con trỏ văn bản (`CaretIndex`) đang đứng trong ô nhập.
3. **Bộ lọc ký tự cấm (Validation Engine):**
   - Kiểm tra chuỗi nhập với tập ký tự cấm của Windows: `\`, `/`, `:`, `*`, `?`, `"`, `<`, `>`, `|`.
   - Nếu phát hiện ký tự cấm, viền ô nhập chuyển sang màu đỏ cảnh báo, dòng Live Preview tạm thời thay bằng thông báo lỗi giải thích ký tự không hợp lệ và vô hiệu hóa nút "Lưu".

---

## 5. Thanh Điều khiển Dưới chân (Footer Actions & State Lifecycle)

- **Trạng thái Dữ liệu Thay đổi (Dirty State Tracking):**
  - Cửa sổ theo dõi xem người dùng có chỉnh sửa bất kỳ trường nào so với cấu hình đang lưu hay không.
  - Nếu chưa có thay đổi: Nút "Áp dụng" bị làm mờ (Disabled).
  - Khi có thay đổi: Nút "Áp dụng" và "Lưu & Đóng" sáng lên.
  - Nếu người dùng bấm nút Đóng cửa sổ (X) hoặc nút "Hủy" trong khi có thay đổi chưa lưu: Hiển thị hộp thoại xác nhận "Bạn có các thay đổi chưa được lưu. Bạn có muốn thoát không?".
- **Hành vi các nút:**
  - **Khôi phục mặc định (`Reset to Defaults`):** Hiển thị hộp thoại xác nhận cảnh báo mọi cài đặt cá nhân sẽ quay về giá trị ban đầu của F-Shot. Nếu người dùng chọn Đồng ý, nạp lại toàn bộ trường trên giao diện theo `AppConfig.Default`.
  - **Áp dụng (`Apply`):** Ghi cấu hình vào đĩa qua `ConfigStore`, phát thông báo cập nhật toàn hệ thống nhưng vẫn giữ cửa sổ cài đặt mở để người dùng tiếp tục tinh chỉnh.
  - **Lưu & Đóng (`Save & Close`):** Ghi cấu hình vào đĩa, phát thông báo cập nhật và đóng cửa sổ.
  - **Hủy (`Cancel`):** Hủy bỏ các sửa đổi tạm thời trong bộ nhớ và đóng cửa sổ.
