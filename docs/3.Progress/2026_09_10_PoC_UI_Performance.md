# Báo cáo tiến độ: Phase 0 — Tối ưu Overlay UI và FPS Measurement

## Ngày

2026-09-10

## Người thực hiện

Kimi-2.7-Code

## Nội dung đã làm

- Tối ưu `CaptureCanvas` trong `FShot.UI`:
  - Cache `WriteableBitmap` của screenshot nền, chỉ tạo lại khi nhận `CaptureResult` mới.
  - Giải phóng bitmap cũ trước khi thay thế.
  - Tách vẽ overlay vùng chọn ra khỏi bitmap nền, vẽ bằng `DrawingContext` mỗi frame.
- Cải thiện đo lường hiệu năng:
  - Thêm `LastFrameTimeMs` và `AverageFrameTimeMs`.
  - Log FPS, frame time, và average frame time mỗi giây.
- Sửa `CaptureOverlayWindow`:
  - Hiển thị cửa sổ ngay sau khi đặt kích thước/vị trí, trước khi chạy capture async.
  - Sử dụng `WindowDecorations` thay cho `SystemDecorations` (deprecated trong Avalonia 12).
- Cập nhật tài liệu thiết kế:
  - `docs/2.Design/11_UI_Avalonia/11_04_RenderControl.md`: mô tả cache bitmap, công thức tọa độ, đo FPS.
- Cập nhật `docs/3.Progress/03_00_Progres_Overview.md`:
  - Phase 0: **5/8 task thực sự hoàn thành**.
  - P0.6, P0.7, P0.8 để `[ ]` với ghi chú "code đã viết, chưa verify trên desktop Windows".
  - Không đánh dấu xong khi chưa kiểm chứng thực tế.

## Vấn đề gặp phải

- `SolidColorBrush` và `Pen` trong Avalonia 12 không implement `IDisposable`.
  - Giải pháp: bỏ từ khóa `use`, để runtime tự quản lý.
- `SystemDecorations` property đã bị deprecated trong Avalonia 12.
  - Giải pháp: thay bằng `WindowDecorations.None`.
- Không thể chạy binary GUI trong môi trường headless hiện tại (`exit code 127`).
  - Cần chạy thử trên Windows desktop thật để verify P0.6/P0.7/P0.8.

## Build/Test

- `dotnet build FShot.sln`: **Pass** (0 warnings, 0 errors).
- `dotnet test`: **Pass** (44 tests passed, 0 failed).
- Binary manual test: **Không thực hiện được** trong môi trường headless; cần desktop Windows.

## Tiếp theo

- Chạy `FShot.UI.exe` trên Windows desktop thật để verify:
  - Cửa sổ borderless topmost phủ toàn Virtual Screen (P0.6).
  - Kéo chuột tạo vùng chọn, di chuyển, resize (P0.7).
  - FPS ≥ 60 và frame time ≤ 16.67 ms khi kéo chuột (P0.8).
- Sau khi verify thành công, chuyển sang **Phase 1: MVP Core**.
