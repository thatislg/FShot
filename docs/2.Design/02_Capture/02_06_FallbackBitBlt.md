# FallbackBitBlt — Thiết kế chi tiết

> Tài liệu này mô tả phương án dự phòng khi Windows.Graphics.Capture không khả dụng.
> Fallback này dùng GDI BitBlt, một API Win32 cũ nhưng ổn định trên hầu hết Windows.

---

## 1. BitBlt là gì?

`BitBlt` là hàm GDI (Graphics Device Interface) của Windows để sao chép pixel từ một device context sang device context khác. Trong F-Shot, nó dùng để sao chép nội dung màn hình vào một bitmap trong bộ nhớ.

Ưu điểm:

- Chạy trên hầu hết mọi phiên bản Windows.
- Đơn giản, ít phụ thuộc hơn Windows.Graphics.Capture.

Nhược điểm:

- Không xử lý tốt Mixed DPI và cửa sổ hiện đại.
- Có thể bị các ứng dụng bảo vệ màn hình chặn.
- Hiệu năng thấp hơn API mới.

---

## 2. Luồng hoạt động

Luồng chụp bằng BitBlt:

1. Lấy device context của màn hình desktop (`GetDC(NULL)`).
2. Tạo một bitmap tương thích trong bộ nhớ (`CreateCompatibleBitmap`).
3. Tạo memory device context (`CreateCompatibleDC`).
4. Chọn bitmap vào memory DC (`SelectObject`).
5. Sao chép pixel từ screen DC sang memory DC (`BitBlt`).
6. Giải phóng các DC và đối tượng GDI.
7. Trích xuất mảng byte từ bitmap.
8. Trả về `CaptureResult`.

---

## 3. Tính toán kích thước và vị trí chụp

### 3.1 Chụp một màn hình

Cho một màn hình có Virtual Bounds logical `(left, top, right, bottom)`. Khi dùng BitBlt, cần xác định vị trí và kích thước để sao chép.

Nếu dùng logical pixel:

`x = left`
`y = top`
`width = right - left`
`height = bottom - top`

Nếu dùng physical pixel, nhân với scale factor:

`physicalWidth = round(width * s)`
`physicalHeight = round(height * s)`

Ví dụ: màn hình logical `(0, 0, 1920, 1080)`, scale `1.5`.

- Logical: width = 1920, height = 1080.
- Physical: width = 2880, height = 1620.

BitBlt sao chép theo logical coordinate của screen DC. Tuy nhiên, trên màn hình High-DPI, screen DC có thể đã được Windows scale tự động tùy cách ứng dụng được đánh dấu DPI aware. Đây là lý do BitBlt dễ gặp lỗi Mixed DPI.

### 3.2 Chụp toàn bộ Virtual Screen

Virtual Screen bounds được tính từ tất cả các màn hình:

`virtualLeft = min(left của mọi màn hình)`
`virtualTop = min(top của mọi màn hình)`
`virtualRight = max(right của mọi màn hình)`
`virtualBottom = max(bottom của mọi màn hình)`

`virtualWidth = virtualRight - virtualLeft`
`virtualHeight = virtualBottom - virtualTop`

BitBlt toàn bộ Virtual Screen có thể cho ra ảnh bị lệch trên Mixed DPI vì mỗi màn hình có scale khác nhau. Do đó, fallback BitBlt phù hợp hơn cho chế độ chụp một màn hình hoặc khi Mixed DPI không quan trọng.

---

## 4. Vấn đề với Mixed DPI

Khi hai màn hình có scale khác nhau, BitBlt sao chép theo coordinate space của desktop DC. Kết quả có thể:

- Một màn hình bị co hoặc giãn.
- Một phần màn hình bị cắt hoặc thừa.
- Ảnh bị mờ nếu Windows tự động scale.

Cách giảm thiệt hại:

- Chụp từng màn hình riêng lẻ thay vì toàn Virtual Screen.
- Ghi rõ `ScaleFactor` cho từng màn hình.
- Khuyến cáo người dùng dùng Windows.Graphics.Capture nếu có Mixed DPI.

---

## 5. Các câu hỏi cần quyết định

| Câu hỏi | Tác động |
|---------|----------|
| Có thực sự cần fallback BitBlt trong MVP? | Tăng độ phức tạp, nhưng mở rộng compatibility |
| Fallback này hỗ trợ chế độ nào? | Có thể chỉ `FullScreen` và `SingleScreen`, không hỗ trợ overlay chính xác |
| Có dùng BitBlt cho chụp toàn Virtual Screen không? | Rủi ro Mixed DPI cao |
| Làm thế nào phát hiện Windows.Graphics.Capture không khả dụng? | Kiểm tra OS version hoặc bắt lỗi khi tạo GraphicsCaptureSession |

---

## 6. Kết nối với phần triển khai

Những quyết định trong tài liệu này sẽ được đưa vào:

- `src/FShot.Platform.Win32/Capture/FallbackBitBlt.fs` (hoặc `.cs`): triển khai BitBlt.
- `src/FShot.Platform.Win32/Capture/CaptureService.fs`: chọn backend chính hoặc fallback.

---

*FallbackBitBlt là phương án dự phòng. Sau khi xem xét, chúng ta chuyển sang ScreenEnumeration — cách liệt kê màn hình và lấy thông tin DPI.*
