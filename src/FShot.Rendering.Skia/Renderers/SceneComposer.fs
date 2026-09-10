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
        canvas.DrawBitmap(bitmap, SKPoint(0.0f, 0.0f))

    /// Vẽ lớp tối ngoài vùng chọn.
    /// Xem 09_07_SceneComposer.md, mục 4.
    let private renderDimming (canvas: SKCanvas) (selection: Selection) (captureResult: CaptureResult) =
        let screenRect =
            DomainToSkia.rectToSkPhysical captureResult.ScaleFactor captureResult.VirtualBounds

        let selectionRect =
            DomainToSkia.rectToSkPhysical captureResult.ScaleFactor selection.Bounds

        use paint =
            new SKPaint(
                Color = SKColor(0uy, 0uy, 0uy, 128uy),
                IsStroke = false
            )

        // Vẽ toàn màn hình.
        canvas.DrawRect(screenRect, paint)

        // Cắt (clear) phần vùng chọn bằng cách vẽ trong chế độ clear.
        use clearPaint =
            new SKPaint(
                BlendMode = SKBlendMode.Clear
            )

        canvas.DrawRect(selectionRect, clearPaint)

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

        // 4. Annotations (tối thiểu: vẽ bounding box để kiểm tra pipeline).
        use annotationPaint = DomainToSkia.createStrokePaint FShot.Core.Geometry.Color.Red 2.0f
        for annotation in annotations do
            let box = annotation.BoundingBox
            let skBox = DomainToSkia.rectToSkPhysical captureResult.ScaleFactor box
            canvas.DrawRect(skBox, annotationPaint)

    /// Render ảnh cuối để xuất (crop theo selection).
    let renderExport
        (captureResult: CaptureResult)
        (selection: Selection)
        (annotations: Annotation list)
        : SKBitmap =

        let physicalSelection = captureResult.LogicalSelectionToPhysical selection.Bounds
        let width = int physicalSelection.Width
        let height = int physicalSelection.Height

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
        canvas.DrawBitmap(fullBitmap, sourceRect, destRect)

        // Vẽ annotations (bounding box stub).
        use annotationPaint = DomainToSkia.createStrokePaint FShot.Core.Geometry.Color.Red 2.0f
        for annotation in annotations do
            let box = annotation.BoundingBox
            let shiftedBox =
                {
                  box with
                      X = box.X - selection.Bounds.X
                      Y = box.Y - selection.Bounds.Y
                }

            let skBox = DomainToSkia.rectToSkPhysical captureResult.ScaleFactor shiftedBox
            canvas.DrawRect(skBox, annotationPaint)

        use image = surface.Snapshot()
        SKBitmap.FromImage(image)
