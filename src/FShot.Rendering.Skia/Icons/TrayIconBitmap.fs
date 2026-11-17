namespace FShot.Rendering.Skia.Icons

open System.IO
open SkiaSharp

/// Render biểu tượng F-Shot dạng vector sang bitmap bằng SkiaSharp.
/// Thiết kế: "Khung vùng chọn + Ống kính máy ảnh" (Selection Frame + Camera Aperture),
/// phản ánh đúng bản chất công cụ chụp/khoanh vùng màn hình của F-Shot.
/// Dùng làm icon cho system tray; kích thước tùy chỉnh để hỗ trợ mọi DPI.
module TrayIconBitmap =

    let private blueColor = SKColor.Parse("#7BD5F5")
    let private strokeColor = SKColor.Parse("#3D2B1F")
    let private orangeColor = SKColor.Parse("#FB923C")
    let private highlightColor = SKColor.Parse("#FFFFFF")
    let private tintColor = new SKColor(0x7Buy, 0xD5uy, 0xF5uy, 0x30uy)

    /// Vẽ icon với kích thước `size x size` pixel, nền trong suốt.
    /// SVG gốc dùng viewBox 32x32; scale theo tỷ lệ mong muốn.
    let create (size: int) : SKBitmap =
        let bitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul)
        use canvas = new SKCanvas(bitmap)
        canvas.Clear(SKColors.Transparent)

        let scale = float32 size / 32.0f
        canvas.Scale(scale)

        use paint = new SKPaint()
        paint.IsAntialias <- true

        // --- Khung vùng chọn (Selection Frame): viền nâu đậm + nền xanh nhạt. ---
        let frameRect = SKRect(2.5f, 2.5f, 29.5f, 29.5f)
        let cornerRadius = 4.5f
        paint.Color <- tintColor
        paint.Style <- SKPaintStyle.Fill
        canvas.DrawRoundRect(frameRect, cornerRadius, cornerRadius, paint)

        paint.Color <- strokeColor
        paint.Style <- SKPaintStyle.Stroke
        paint.StrokeWidth <- 2.2f
        canvas.DrawRoundRect(frameRect, cornerRadius, cornerRadius, paint)

        // --- Ống kính máy ảnh (Camera Aperture) ở tâm. ---
        // Thân kính ngoài: xanh nhạt + viền nâu.
        paint.Color <- blueColor
        paint.Style <- SKPaintStyle.Fill
        canvas.DrawCircle(16.0f, 16.0f, 6.4f, paint)

        paint.Color <- strokeColor
        paint.Style <- SKPaintStyle.Stroke
        paint.StrokeWidth <- 1.8f
        canvas.DrawCircle(16.0f, 16.0f, 6.4f, paint)

        // Vành kính trong: cam.
        paint.Color <- orangeColor
        paint.Style <- SKPaintStyle.Fill
        canvas.DrawCircle(16.0f, 16.0f, 3.4f, paint)

        // Khẩu độ (aperture opening): nâu đậm.
        paint.Color <- strokeColor
        canvas.DrawCircle(16.0f, 16.0f, 1.4f, paint)

        // Điểm sáng ống kính (glint): trắng, lệch góc trên-trái.
        paint.Color <- highlightColor
        canvas.DrawCircle(13.6f, 13.6f, 0.7f, paint)

        canvas.Flush()
        bitmap

    /// Tạo PNG stream từ icon với kích thước `size x size` pixel.
    /// Stream đã được rewind về đầu, sẵn sàng đưa vào `WindowIcon`.
    let createPngStream (size: int) : MemoryStream =
        use bitmap = create size
        let data = bitmap.Encode(SKEncodedImageFormat.Png, 100)
        let bytes = data.ToArray()
        let stream = new MemoryStream(bytes)
        stream.Position <- 0L
        stream
