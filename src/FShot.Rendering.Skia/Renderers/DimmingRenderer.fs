namespace FShot.Rendering.Skia.Renderers

open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Rendering.Skia.Converters
open SkiaSharp

/// Renderer cho lớp phủ tối mờ ngoài vùng chọn.
/// Xem tài liệu 03_07_OverlayDimming.md.
module DimmingRenderer =

    /// Màu dimming mặc định: đen với alpha 50%.
    /// Xem 03_07_OverlayDimming.md, mục 8.
    let DefaultOpacity = 0.5

    /// Vẽ lớp tối phủ toàn màn hình, sau đó khoét lỗ vùng chọn bằng Clear blend mode.
    /// Xem 03_07_OverlayDimming.md, mục 4.1.
    let render
        (canvas: SKCanvas)
        (captureResult: CaptureResult)
        (selectionBounds: Rect)
        (opacity: float)
        =

        let screenRect =
            DomainToSkia.rectToSkPhysical captureResult.ScaleFactor captureResult.VirtualBounds

        let selectionRect =
            DomainToSkia.rectToSkPhysical captureResult.ScaleFactor selectionBounds

        let alphaByte =
            let raw = int (opacity * 255.0)
            if raw < 0 then 0uy elif raw > 255 then 255uy else byte raw

        use dimmingPaint =
            new SKPaint(
                Color = SKColor(0uy, 0uy, 0uy, alphaByte),
                IsStroke = false
            )

        // Vẽ toàn màn hình tối.
        canvas.DrawRect(screenRect, dimmingPaint)

        // Khoét lỗ vùng chọn bằng Clear.
        use clearPaint =
            new SKPaint(
                BlendMode = SKBlendMode.Clear
            )

        canvas.DrawRect(selectionRect, clearPaint)

    /// Vẽ dimming với opacity mặc định.
    let renderDefault (canvas: SKCanvas) (captureResult: CaptureResult) (selectionBounds: Rect) =
        render canvas captureResult selectionBounds DefaultOpacity
