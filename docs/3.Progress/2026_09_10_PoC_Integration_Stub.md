# Báo cáo tiến độ — 2026-09-10

## Người thực hiện

Agent triển khai theo yêu cầu.

## Nội dung đã làm

- Hoàn thiện design docs tối thiểu cho Selection, Annotation, Export, Rendering, Platform Win32, UI Avalonia.
- Triển khai code domain Selection, Annotation, Export trong `FShot.Core`.
- Triển khai `ScreenEnumeration` và `StubCaptureService` trong `FShot.Platform.Win32`.
- Triển khai `DomainToSkia` converters và `SceneComposer` trong `FShot.Rendering.Skia`.
- Triển khai `CaptureCanvas`, `CaptureOverlayWindow`, CLI parser, và app lifecycle trong `FShot.UI`.
- Cập nhật `03_00_Progres_Overview.md` với tiến độ Phase 0 hoàn thành 8/8.
- Tạo báo cáo thực thi theo template `099.Report/0001_Template.md`.

## Vấn đề gặp phải

- Ban đầu gặp lỗi khi dùng `Avalonia.Skia.ISkiaDrawingContextImpl` trực tiếp trong UI.
- Giải pháp: chuyển sang dùng `WriteableBitmap` để hiển thị screenshot, đơn giản và ổn định hơn cho PoC.

## Build/Test

- `dotnet build FShot.sln`: 0 warning, 0 error.
- `dotnet test`: 44 tests passed (43 Core + 1 Skia).

## Tiếp theo

- Chạy thử binary `FShot.UI.exe` trên Windows để kiểm tra overlay.
- Thay stub capture bằng `Windows.Graphics.Capture` thực.
- Tích hợp Export service (file, clipboard).
- Render annotations chi tiết bằng SkiaSharp.
