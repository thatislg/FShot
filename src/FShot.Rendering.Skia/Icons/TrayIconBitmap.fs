namespace FShot.Rendering.Skia.Icons

open System.IO
open SkiaSharp

/// Render biểu tượng F-Shot Kawaii dạng vector sang bitmap bằng SkiaSharp.
/// Dùng làm icon cho system tray; kích thước tùy chỉnh để hỗ trợ mọi DPI.
module TrayIconBitmap =

    let private headColor = SKColor.Parse("#7BD5F5")
    let private strokeColor = SKColor.Parse("#3D2B1F")
    let private beakColor = SKColor.Parse("#FB923C")
    let private highlightColor = SKColor.Parse("#FFFFFF")

    /// Vẽ icon tray với kích thước `size x size` pixel, nền trong suốt.
    let create (size: int) : SKBitmap =
        let bitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul)
        use canvas = new SKCanvas(bitmap)
        canvas.Clear(SKColors.Transparent)

        // SVG gốc có viewBox 32x32; scale theo tỷ lệ mong muốn.
        let scale = float32 size / 32.0f
        canvas.Scale(scale)

        use paint = new SKPaint()
        paint.IsAntialias <- true

        // Đầu chim: hình tròn.
        paint.Color <- headColor
        paint.Style <- SKPaintStyle.Fill
        canvas.DrawCircle(16.0f, 16.0f, 12.0f, paint)

        paint.Color <- strokeColor
        paint.Style <- SKPaintStyle.Stroke
        paint.StrokeWidth <- 2.5f
        canvas.DrawCircle(16.0f, 16.0f, 12.0f, paint)

        // Mỏ: ellipse.
        let beakRect = SKRect(12.5f, 15.8f, 19.5f, 20.2f)
        paint.Color <- beakColor
        paint.Style <- SKPaintStyle.Fill
        paint.StrokeWidth <- 0.0f
        canvas.DrawOval(beakRect, paint)

        paint.Color <- strokeColor
        paint.Style <- SKPaintStyle.Stroke
        paint.StrokeWidth <- 1.8f
        canvas.DrawOval(beakRect, paint)

        // Mắt trái.
        paint.Color <- strokeColor
        paint.Style <- SKPaintStyle.Fill
        paint.StrokeWidth <- 0.0f
        canvas.DrawCircle(11.5f, 14.0f, 1.8f, paint)

        // Mắt phải.
        canvas.DrawCircle(20.5f, 14.0f, 1.8f, paint)

        // Điểm sáng mắt.
        paint.Color <- highlightColor
        canvas.DrawCircle(10.8f, 13.3f, 0.6f, paint)
        canvas.DrawCircle(19.8f, 13.3f, 0.6f, paint)

        canvas.Flush()
        bitmap

    /// Tạo PNG stream từ icon tray với kích thước `size x size` pixel.
    /// Stream đã được rewind về đầu, sẵn sàng đưa vào `WindowIcon`.
    let createPngStream (size: int) : MemoryStream =
        use bitmap = create size
        let data = bitmap.Encode(SKEncodedImageFormat.Png, 100)
        let bytes = data.ToArray()
        let stream = new MemoryStream(bytes)
        stream.Position <- 0L
        stream
