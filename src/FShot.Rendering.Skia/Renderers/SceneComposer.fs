namespace FShot.Rendering.Skia.Renderers

open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Rendering.Skia.Converters
open SkiaSharp

/// Điều phối việc vẽ toàn bộ scene trong overlay.
/// Xem tài liệu 09_07_SceneComposer.md.
module SceneComposer =

    /// Vẽ screenshot bitmap lên canvas tại gốc (0, 0).
    /// Xem 09_07_SceneComposer.md, mục 3.
    let private renderScreenshot (canvas: SKCanvas) (captureResult: CaptureResult) =
        use bitmap = captureResult |> DomainToSkia.captureResultToBitmap
        let sampling = new SKSamplingOptions(SKCubicResampler.Mitchell)
        canvas.DrawBitmap(bitmap, SKPoint(0.0f, 0.0f), sampling)

    /// Vẽ lớp tối ngoài vùng chọn.
    /// Xem 03_07_OverlayDimming.md và 09_07_SceneComposer.md, mục 4.
    let private renderDimming (canvas: SKCanvas) (selection: Selection) (captureResult: CaptureResult) =
        DimmingRenderer.renderDefault canvas captureResult selection.Bounds

    /// Vẽ đường viền vùng chọn.
    let private renderSelectionBorder
        (canvas: SKCanvas)
        (selection: Selection)
        (captureResult: CaptureResult)
        =

        let rect = DomainToSkia.rectToSkPhysical captureResult.ScaleFactor selection.Bounds
        use paint = DomainToSkia.createStrokePaint FShot.Core.Geometry.Color.White 2.0f
        canvas.DrawRect(rect, paint)

    /// Vẽ 8 handles của vùng chọn.
    let private renderSelectionHandles
        (canvas: SKCanvas)
        (selection: Selection)
        (captureResult: CaptureResult)
        =

        let halfSize = float32 Selection.HandleSize / 2.0f
        use paint = DomainToSkia.createFillPaint FShot.Core.Geometry.Color.White

        for (_, center) in selection.HandleCenters do
            let skCenter = DomainToSkia.pointToSkPhysical captureResult.ScaleFactor center
            let handleRect =
                SKRect(
                    skCenter.X - halfSize,
                    skCenter.Y - halfSize,
                    skCenter.X + halfSize,
                    skCenter.Y + halfSize
                )

            canvas.DrawRect(handleRect, paint)

    /// Vẽ toàn bộ scene overlay.
    let renderOverlay
        (canvas: SKCanvas)
        (captureResult: CaptureResult)
        (selection: Selection)
        (annotations: Annotation list)
        =

        // 1. Screenshot.
        renderScreenshot canvas captureResult

        // 2. Dimming overlay.
        if selection.State <> Idle then
            renderDimming canvas selection captureResult

        // 3. Selection border và handles.
        if selection.State <> Idle then
            renderSelectionBorder canvas selection captureResult
            renderSelectionHandles canvas selection captureResult

        // 4. Annotations đã commit.
        for annotation in annotations do
            AnnotationRenderer.renderAnnotation canvas captureResult.ScaleFactor annotation

        // 5. Preview annotation đang vẽ.
        ()

    /// Render ảnh cuối để xuất (crop theo selection).
    let renderExport
        (captureResult: CaptureResult)
        (selection: Selection)
        (annotations: Annotation list)
        : SKBitmap =

        let physicalSelection = captureResult.LogicalSelectionToPhysical selection.Bounds
        let width = max 1 (int physicalSelection.Width)
        let height = max 1 (int physicalSelection.Height)

        let info =
            new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul)

        use surface = SKSurface.Create(info)
        let canvas = surface.Canvas

        // Vẽ phần screenshot đã crop.
        use fullBitmap = captureResult |> DomainToSkia.captureResultToBitmap
        let sourceRect =
            SKRect(
                float32 physicalSelection.X,
                float32 physicalSelection.Y,
                float32 physicalSelection.Right,
                float32 physicalSelection.Bottom
            )

        let destRect = SKRect(0.0f, 0.0f, float32 width, float32 height)
        let sampling = new SKSamplingOptions(SKCubicResampler.Mitchell)
        canvas.DrawBitmap(fullBitmap, sourceRect, destRect, sampling)

        // Vẽ annotations, dịch gốc tọa độ để nằm trong vùng crop.
        let physicalSelection = captureResult.LogicalSelectionToPhysical selection.Bounds
        canvas.Save() |> ignore
        canvas.Translate(-float32 physicalSelection.X, -float32 physicalSelection.Y) |> ignore

        for annotation in annotations do
            AnnotationRenderer.renderAnnotation canvas captureResult.ScaleFactor annotation

        canvas.Restore() |> ignore

        use image = surface.Snapshot()
        SKBitmap.FromImage(image)
