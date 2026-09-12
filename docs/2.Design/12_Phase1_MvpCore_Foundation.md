# Phase 1 MVP Core — Thiết kế nền tảng (P1.01 → P1.04)

> Tài liệu này tổng hợp thiết kế cho 4 task đầu Phase 1:
> - P1.01 Domain model đầy đủ.
> - P1.02 Immutable HistoryStack.
> - P1.03 Overlay State Machine.
> - P1.04 Unit tests.
>
> Tài liệu chỉ sử dụng diễn giải bằng lời, công thức toán và mô tả kiểu dữ liệu trừu tượng. Không chứa code mẫu cụ thể của bất kỳ ngôn ngữ lập trình nào.

---

## 1. Tóm tắt mục tiêu

Sau 4 task này, lớp Core của hệ thống sẽ sở hữu đầy đủ các kiểu dữ liệu thuần khiết cần thiết để điều phối toàn bộ luồng tương tác trong cửa sổ overlay.

Luồng điều phối mong muốn:

```text
Đầu vào (chuột, phím, công cụ) → Máy trạng thái overlay →
    (Vùng chọn | Bản xem trước chú thích | Lịch sử thao tác | Lệnh xuất ảnh) →
    Mô hình render → Giao diện vẽ
```

Giao diện người dùng chỉ đóng vai trò bộ chuyển đổi:
- Chuyển sự kiện phần cứng thành sự kiện miền.
- Gọi hàm cập nhật trạng thái.
- Nhận mô hình render và thực hiện vẽ.
- Thực thi các lệnh trả về (xuất ảnh, đóng overlay, v.v.).

---

## 2. Các tệp cần tạo hoặc cập nhật

### 2.1 Tầng miền (Domain)

| Tệp | Hành động | Nội dung |
|-----|-----------|----------|
| `Annotation` | Cập nhật | Bổ sung định danh chú thích, các phép biến đổi cơ bản (dịch chuyển, đổi màu, đổi độ dày nét). |
| `Capture` | Giữ nguyên | Các kiểu chế độ chụp, yêu cầu chụp, kết quả chụp, lỗi chụp, giao diện dịch vụ chụp đã ổn định. |
| `Config` | Tạo mới | Cấu hình mặc định cho MVP: đường dẫn lưu, quy tắc đặt tên file, màu mặc định, độ dày nét mặc định, định dạng file, chất lượng nén, giới hạn undo. |
| `Export` | Giữ nguyên | Các kiểu định dạng file, tùy chọn lưu, mục tiêu xuất, yêu cầu xuất đã ổn định. |
| `History` | Tạo mới | Ảnh chụp trạng thái và ngăn xếp lịch sử undo/redo bất biến. |
| `Selection` | Cập nhật nhỏ | Giữ nguyên hầu hết; chỉ rà soát tính nhất quán với máy trạng thái overlay. |

### 2.2 Tầng trạng thái (State)

| Tệp | Hành động | Nội dung |
|-----|-----------|----------|
| `OverlayState` | Tạo mới | Máy trạng thái chính: định nghĩa các trạng thái, sự kiện đầu vào, mô hình render, lệnh đầu ra, và hàm chuyển tiếp. |

### 2.3 Tầng kiểm thử (Tests)

| Tệp | Hành động | Nội dung |
|-----|-----------|----------|
| `Domain/AnnotationTests` | Cập nhật | Kiểm tra định danh, biến đổi hình học, hộp bao các công cụ. |
| `Domain/ConfigTests` | Tạo mới | Kiểm tra giá trị mặc định và các giá trị được chuẩn hóa. |
| `Domain/HistoryTests` | Tạo mới | Kiểm tra push, undo, redo, giới hạn ngăn xếp. |
| `State/OverlayStateTests` | Tạo mới | Kiểm tra các chuyển tiếp trạng thái chính, công cụ, undo/redo, lệnh xuất. |

---

## 3. Thiết kế chi tiết từng phần

### 3.1 P1.01 — Domain model đầy đủ

#### 3.1.1 Chú thích (Annotation)

Mỗi chú thích là một đối tượng bất biến gồm:
- Một định danh duy nhất.
- Một công cụ hình học, mô tả loại hình vẽ và các tham số cần thiết.
- Một màu sắc.
- Một độ dày nét.
- Một thời điểm tạo.

Các công cụ hình học trong MVP bao gồm:
- Bút tự do: danh sách các điểm theo thứ tự vẽ.
- Đường thẳng: điểm bắt đầu và điểm kết thúc.
- Mũi tên: điểm bắt đầu, điểm kết thúc, kiểu mũi tên.
- Hình chữ nhật: điểm góc đối, bán kính bo góc.
- Hình tròn/elip: điểm góc đối của hộp bao, cờ khóa tỷ lệ.
- Bút nhớ: điểm bắt đầu và kết thúc.
- Văn bản: vị trí gốc và nội dung.
- Làm mờ pixel: điểm góc đối và kích thước khối pixel.

Các phép biến đổi cơ bản trên chú thích:
- Dịch chuyển toàn bộ chú thích theo vector bù `(dx, dy)`.
- Thay đổi màu sắc.
- Thay đổi độ dày nét.
- Tính hộp bao nhỏ nhất chứa chú thích.

Việc tạo chú thích từ bản xem trước tuân theo công thức: sinh định danh mới, gán công cụ, gán màu, gán độ dày nét, gán thời điểm hiện tại.

#### 3.1.2 Cấu hình MVP (MvpConfig)

Cấu hình mặc định cần lưu trữ:
- Đường dẫn lưu file (có thể rỗng).
- Quy tắc đặt tên file dạng mẫu ngày tháng.
- Màu vẽ mặc định.
- Độ dày nét mặc định.
- Định dạng file mặc định.
- Chất lượng nén ảnh.
- Giới hạn số bước undo.

Các giá trị cần được chuẩn hóa:
- Giới hạn undo nằm trong khoảng từ 10 đến 1000.
- Chất lượng nén nằm trong khoảng từ 0 đến 100.

#### 3.1.3 Chụp và xuất

Các kiểu `CaptureMode`, `OutputTarget`, `ExportTarget`, `SaveOptions`, `CaptureResult`, `CaptureError`, `ICaptureService` đã được định nghĩa đầy đủ trong Phase 0 và không cần thay đổi. Chúng sẽ được máy trạng thái overlay sử dụng để tạo yêu cầu xuất.

### 3.2 P1.02 — Immutable HistoryStack

#### 3.2.1 Ảnh chụp trạng thái (Snapshot)

Mỗi snapshot lưu:
- Danh sách chú thích hiện tại.
- Vùng chọn hiện tại.
- Chỉ số counter tiếp theo (dành cho công cụ đánh số vòng tròn trong tương lai, trong MVP luôn bằng 0).

#### 3.2.2 Ngăn xếp lịch sử (HistoryStack)

Ngăn xếp lịch sử gồm:
- Ngăn xếp undo: danh sách các snapshot theo thứ tự thời gian, phần tử đầu là snapshot hiện tại.
- Ngăn xếp redo: danh sách các snapshot đã bị undo.
- Giới hạn kích thước.

Các thao tác:
- **Khởi tạo:** tạo ngăn xếp rỗng với giới hạn cho trước.
- **Push:** thêm snapshot mới vào đầu ngăn xếp undo, đồng thời xóa toàn bộ ngăn xếp redo. Nếu số lượng vượt quá giới hạn, loại bỏ snapshot cũ nhất ở cuối.
- **Undo:** di chuyển snapshot đầu của undo sang đầu redo, trả về snapshot mới đầu undo (nếu còn).
- **Redo:** di chuyển snapshot đầu của redo sang đầu undo, trả về snapshot đó.
- **Current:** lấy snapshot đầu tiên của ngăn xếp undo, hoặc rỗng nếu chưa có snapshot nào.
- **CanUndo / CanRedo:** kiểm tra ngăn xếp tương ứng có phần tử hay không.

#### 3.2.3 Khi nào push snapshot?

Trong P1.01–P1.04, chỉ push snapshot khi:
- Hoàn tất một chú thích (commit preview).
- Xóa một chú thích.

Không push khi:
- Đang kéo chuột tạo vùng chọn.
- Đang di chuyển hoặc co giãn vùng chọn.
- Đang vẽ bản xem trước.

### 3.3 P1.03 — Overlay State Machine

#### 3.3.1 Công cụ đang hoạt động

Danh sách công cụ có thể chọn trong overlay:
- Chọn vùng.
- Bút tự do.
- Đường thẳng.
- Mũi tên.
- Hình chữ nhật.
- Hình tròn/elip.
- Bút nhớ.
- Văn bản.
- Làm mờ pixel.

#### 3.3.2 Sự kiện đầu vào

UI chuyển các sự kiện phần cứng thành sự kiện miền:
- Nhấn chuột tại một điểm.
- Di chuyển chuột tới một điểm.
- Thả chuột tại một điểm.
- Nhấn phím, kèm danh sách phím modifier đang giữ.
- Chuyển công cụ.
- Đổi màu.
- Đổi độ dày nét.
- Xác nhận vùng chọn.
- Hủy overlay.
- Yêu cầu undo.
- Yêu cầu redo.

#### 3.3.3 Lệnh đầu ra

Máy trạng thái trả về các lệnh để UI thực thi:
- Vẽ lại: kèm theo mô hình render.
- Xuất ảnh: kèm theo yêu cầu xuất.
- Đóng overlay.
- Không làm gì.

#### 3.3.4 Bản xem trước chú thích

Khi đang vẽ, máy trạng thái lưu một bản xem trước gồm:
- Công cụ hình học đang được cập nhật theo con trỏ.
- Màu đang dùng.
- Độ dày nét đang dùng.

Bản xem trước chỉ hiển thị tạm thời, chưa đưa vào danh sách chú thích chính thức.

#### 3.3.5 Trạng thái overlay

Trạng thái tổng thể của overlay chứa:
- Kết quả chụp màn hình.
- Vùng chọn hiện tại.
- Danh sách chú thích đã commit.
- Công cụ đang chọn.
- Màu hiện tại.
- Độ dày nét hiện tại.
- Bản xem trước chú thích (nếu có).
- Ngăn xếp lịch sử.
- Cấu hình MVP.

Khởi tạo trạng thái ban đầu sau khi có kết quả chụp:
- Vùng chọn rỗng.
- Không có chú thích.
- Công cụ mặc định là công cụ chọn vùng.
- Màu và độ dày nét lấy từ cấu hình.
- Không có bản xem trước.
- Ngăn xếp lịch sử rỗng.

#### 3.3.6 Mô hình render

Mô hình render là đầu ra để UI vẽ một khung hình, bao gồm:
- Kết quả chụp màn hình.
- Vùng chọn hiện tại.
- Danh sách chú thích đã commit.
- Bản xem trước chú thích (nếu có).
- Công cụ đang chọn.
- Màu hiện tại.
- Độ dày nét hiện tại.
- Có thể undo hay không.
- Có thể redo hay không.

#### 3.3.7 Luồng chuyển tiếp chính

**Chọn vùng:**
1. Ở trạng thái rảnh, nhấn chuột → bắt đầu chọn vùng.
2. Kéo chuột → cập nhật kích thước vùng chọn.
3. Thả chuột → hoàn tất vùng chọn, chuyển sang trạng thái đã chọn.
4. Khi đã chọn, nhấn vào handle → bắt đầu co giãn.
5. Khi đã chọn, nhấn vào bên trong vùng → bắt đầu di chuyển.
6. Thả chuột → hoàn tất tương tác, giữ trạng thái đã chọn.

**Vẽ chú thích:**
1. Chuyển sang công cụ vẽ.
2. Nhấn chuột → tạo bản xem trước với điểm bắt đầu.
3. Kéo chuột → cập nhật tham số của bản xem trước.
4. Thả chuột → chuyển bản xem trước thành chú thích chính thức, thêm vào danh sách, push snapshot vào lịch sử.

**Phím tắt:**
- Phím hủy: nếu đang có vùng chọn, hủy vùng chọn; nếu không, đóng overlay.
- Phím xác nhận: khi đã chọn vùng, tạo lệnh xuất ảnh.
- Phím undo/redo: khôi phục snapshot từ ngăn xếp lịch sử.

### 3.4 P1.04 — Unit tests

#### 3.4.1 Kiểm tra miền chú thích

- Tạo chú thích từ bản xem trước phải giữ đúng công cụ, màu, độ dày nét.
- Dịch chuyển chú thích phải cộng đều `(dx, dy)` vào tất cả các điểm.
- Đổi màu và đổi độ dày nét phải cập nhật đúng trường.
- Hộp bao của từng loại công cụ phải tính đúng: bút tự do lấy min/max tập điểm, đường thẳng/hình chữ nhật/hình tròn lấy min/max hai góc, văn bản hộp bao rỗng tại vị trí gốc.

#### 3.4.2 Kiểm tra cấu hình

- Cấu hình mặc định có đầy đủ các trường và giá trị hợp lý.
- Giới hạn undo được chuẩn hóa về khoảng cho phép.
- Chất lượng nén được chuẩn hóa về khoảng cho phép.

#### 3.4.3 Kiểm tra lịch sử

- Push snapshot mới làm tăng ngăn xếp undo.
- Push sau khi undo xóa ngăn xếp redo.
- Undo chuyển snapshot hiện tại sang redo và trả về snapshot trước đó.
- Redo chuyển snapshot từ redo sang undo.
- Undo khi ngăn xếp rỗng không thay đổi trạng thái.
- Khi số snapshot vượt quá giới hạn, snapshot cũ nhất bị loại bỏ.

#### 3.4.4 Kiểm tra máy trạng thái

- Khởi tạo trạng thái từ kết quả chụp cho đúng giá trị ban đầu.
- Nhấn chuột ở trạng thái rảnh → bắt đầu chọn vùng.
- Kéo chuột → cập nhật hình chữ nhật vùng chọn.
- Thả chuột → vùng chọn hoàn tất, trạng thái chuyển sang đã chọn.
- Phím hủy khi đang chọn → về trạng thái rảnh.
- Phím hủy khi đang rảnh → lệnh đóng overlay.
- Chuyển công cụ → công cụ hiện tại được cập nhật.
- Chọn công cụ bút tự do, nhấn-thả chuột → danh sách chú thích tăng thêm một phần tử, bản xem trước bị xóa, ngăn xếp lịch sử tăng.
- Undo sau khi commit xóa chú thích vừa thêm.
- Redo sau undo khôi phục chú thích.
- Phím xác nhận khi đã chọn vùng → lệnh xuất ảnh.

---

## 4. Luồng dữ liệu tổng quan

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Layer                              │
│  - Bắt sự kiện chuột, phím, công cụ.                         │
│  - Chuyển tọa độ màn hình vật lý sang tọa độ miền.           │
│  - Gọi hàm cập nhật trạng thái overlay.                      │
│  - Nhận mô hình render và thực hiện vẽ.                      │
│  - Thực thi lệnh đầu ra (xuất, đóng, v.v.).                  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Overlay State Machine                     │
│  - Pure function: (State, Event) → (NewState, Command).      │
│  - Không log, không IO, không gọi API hệ thống.              │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
        ┌──────────┐   ┌────────────┐   ┌──────────┐
        │ Selection│   │ Annotation │   │ History  │
        │          │   │ + Preview  │   │ Stack    │
        └──────────┘   └────────────┘   └──────────┘
```

---

## 5. Nguyên tắc thiết kế

1. **Tầng Core phải thuần khiết.** Máy trạng thái không được chứa tác dụng phụ. Mọi tác vụ hệ thống đều do UI hoặc tầng Platform thực hiện thông qua lệnh đầu ra.
2. **Giao diện là bộ chuyển đổi.** UI không tự quyết định logic overlay, chỉ chuyển đổi định dạng dữ liệu.
3. **Lịch sử chỉ push sau commit.** Không ghi snapshot trong lúc kéo chuột hoặc trong quá trình vẽ bản xem trước.
4. **Cấu hình là phụ thuộc đầu vào.** Trạng thái overlay được khởi tạo với một cấu hình cụ thể, không dùng giá trị hardcoded.
5. **Kiểm thử bao phủ các chuyển tiếp chính.** Mỗi chuyển trạng thái quan trọng phải có ít nhất một bài kiểm tra đơn vị.

---

## 6. Các quyết định cần chốt trước khi triển khai

| Câu hỏi | Khuyến nghị | Lý do |
|---------|-------------|-------|
| Có lưu vùng chọn vào snapshot không? | Có | Khi undo, người dùng cần thấy lại đúng ngữ cảnh vùng chọn. |
| Có undo thao tác di chuyển/co giãn vùng chọn không? | Không trong phạm vi P1.01–P1.04 | Giữ cho ngăn xếp lịch sử đơn giản, chỉ ghi sau khi commit chú thích. Có thể bổ sung sau này. |
| Công cụ văn bản xử lý nhập liệu thế nào? | Tầng Core chỉ lưu vị trí và chuỗi rỗng; UI mở hộp nhập văn bản và gửi sự kiện cập nhật nội dung. | Core không xử lý trực tiếp bàn phím vật lý. |
| Công cụ đánh số vòng tròn có trong MVP không? | Không | Trường NextCounter trong snapshot luôn bằng 0 trong MVP. |
| Có cần lệnh riêng cho từng mục tiêu xuất không? | Không | Yêu cầu xuất đã chứa mục tiêu, một lệnh `Export` là đủ. |

---

## 7. Kế hoạch triển khai đề xuất

1. Cập nhật kiểu dữ liệu chú thích và viết kiểm thử.
2. Tạo kiểu dữ liệu cấu hình MVP và viết kiểm thử.
3. Tạo kiểu dữ liệu snapshot và ngăn xếp lịch sử, viết kiểm thử.
4. Tạo máy trạng thái overlay và viết kiểm thử.
5. Chạy toàn bộ bài kiểm thử, sửa lỗi.
6. Cập nhật tiến độ trong tài liệu progress.

---

## 8. Tài liệu liên quan

- `04_Annotation/04_02_ToolModel.md` — mô hình công cụ.
- `04_Annotation/04_08_CommitAndPreview.md` — phân biệt xem trước và commit.
- `05_History/05_01_Overview.md` — tổng quan undo/redo.
- `08_OverlayState/08_01_Overview.md` — tổng quan máy trạng thái.
- `08_OverlayState/08_05_RenderModel.md` — dữ liệu đầu ra cho giao diện.

---

*Thiết kế này chuẩn bị nền tảng để Phase 1 MVP Core triển khai các công cụ chú thích, undo/redo, và điều phối overlay một cách có thể kiểm thử đơn vị.*
