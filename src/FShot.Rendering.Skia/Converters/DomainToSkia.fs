namespace FShot.Rendering.Skia.Converters

open FShot.Core.Geometry
open FShot.Core.Domain
open SkiaSharp

/// Chuyển đổi từ domain types của F-Shot sang Skia types.
/// Xem tài liệu 09_02_Converters.md.
module DomainToSkia =

    /// Chuyển Point sang SKPoint.
    /// Công thức: (X, Y) → SKPoint(X, Y).
    let pointToSk (p: Point) : SKPoint =
        SKPoint(float32 p.X, float32 p.Y)

    /// Chuyển logical Point sang physical SKPoint.
    /// Công thức: (X * scale, Y * scale).
    let pointToSkPhysical (scale: ScaleFactor) (p: Point) : SKPoint =
        let s = scale.Value
        SKPoint(float32 (p.X * s), float32 (p.Y * s))

    /// Chuyển Rect sang SKRect.
    /// Công thức: SKRect(left, top, right, bottom).
    let rectToSk (rect: Rect) : SKRect =
        SKRect(float32 rect.Left, float32 rect.Top, float32 rect.Right, float32 rect.Bottom)

    /// Chuyển logical Rect sang physical SKRect.
    let rectToSkPhysical (scale: ScaleFactor) (rect: Rect) : SKRect =
        let s = scale.Value
        SKRect(
            float32 (rect.Left * s),
            float32 (rect.Top * s),
            float32 (rect.Right * s),
            float32 (rect.Bottom * s)
        )

    /// Chuyển Color sang SKColor.
    /// Công thức: (R, G, B, A) → SKColor(R, G, B, A).
    let colorToSk (color: Color) : SKColor =
        SKColor(color.R, color.G, color.B, color.A)

    /// Chuyển StrokeWidth sang SKStrokeWidth.
    /// Công thức: logical width * scale.
    let strokeWidthToSkPhysical (scale: ScaleFactor) (strokeWidth: StrokeWidth) : float32 =
        float32 (strokeWidth.Value * scale.Value)

    /// Tạo SKPaint từ Color và StrokeWidth.
    let createStrokePaint (color: Color) (strokeWidth: float32) : SKPaint =
        new SKPaint(
            Color = colorToSk color,
            StrokeWidth = strokeWidth,
            IsStroke = true,
            IsAntialias = true
        )

    /// Tạo SKPaint fill từ Color.
    let createFillPaint (color: Color) : SKPaint =
        new SKPaint(
            Color = colorToSk color,
            IsStroke = false,
            IsAntialias = true
        )

    /// Tạo SKBitmap từ CaptureResult.
    /// Công thức: dùng Width, Height, Stride, Pixels.
    /// Xem 09_02_Converters.md, mục 5.
    let captureResultToBitmap (captureResult: CaptureResult) : SKBitmap =
        let colorType =
            match captureResult.PixelFormat with
            | PixelFormat.Bgra32 -> SKColorType.Bgra8888
            | PixelFormat.Rgba32 -> SKColorType.Rgba8888
            | PixelFormat.Rgb24 -> SKColorType.Rgb888x

        let info =
            new SKImageInfo(
                captureResult.Width,
                captureResult.Height,
                colorType,
                SKAlphaType.Premul
            )

        let bitmap = new SKBitmap(info)
        bitmap.SetPixels(System.IntPtr.Zero) |> ignore

        // Copy pixels từ mảng byte sang bitmap.
        let pixelPtr = bitmap.GetPixels()
        if pixelPtr <> System.IntPtr.Zero then
            System.Runtime.InteropServices.Marshal.Copy(
                captureResult.Pixels,
                0,
                pixelPtr,
                captureResult.TotalBytes
            )

        bitmap
