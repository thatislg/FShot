namespace FShot.Rendering.Skia.Renderers

open System
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Rendering.Skia.Converters
open SkiaSharp

/// Vẽ các annotation lên Skia canvas.
/// Xem tài liệu 09_04_AnnotationRenderer.md.
module AnnotationRenderer =

    /// Vẽ một Annotation lên canvas tại tọa độ vật lý.
    /// Hiện tại triển khai đầy đủ cho Pencil; các tool khác được bỏ qua
    /// và sẽ bổ sung trong các task P1.13–P1.19 tiếp theo.
    let renderAnnotation (canvas: SKCanvas) (scale: ScaleFactor) (annotation: Annotation) : unit =
        match annotation.Tool with
        | Tool.Pencil points ->
            if List.isEmpty points then
                ()
            else
                let segments = PathSmoothing.toSmoothedSegments annotation.Style.StrokeWidth points

                if List.isEmpty segments then
                    ()
                else
                    use path = new SKPath()
                    let (start, ctrl, target) = List.head segments
                    path.MoveTo(DomainToSkia.pointToSkPhysical scale start)

                    for (_, c, e) in segments do
                        path.QuadTo(
                            DomainToSkia.pointToSkPhysical scale c,
                            DomainToSkia.pointToSkPhysical scale e
                        )
                        |> ignore

                    let strokeWidth =
                        DomainToSkia.strokeWidthToSkPhysical scale annotation.Style.StrokeWidth

                    use paint = DomainToSkia.createStrokePaint annotation.Style.Color strokeWidth
                    canvas.DrawPath(path, paint)

        | Tool.Line (startPoint, endPoint) ->
            let a = DomainToSkia.pointToSkPhysical scale startPoint
            let b = DomainToSkia.pointToSkPhysical scale endPoint
            let strokeWidth =
                DomainToSkia.strokeWidthToSkPhysical scale annotation.Style.StrokeWidth
            use paint = DomainToSkia.createStrokePaint annotation.Style.Color strokeWidth
            canvas.DrawLine(a, b, paint)

        | Tool.Arrow (startPoint, endPoint, _) ->
            let a = DomainToSkia.pointToSkPhysical scale startPoint
            let b = DomainToSkia.pointToSkPhysical scale endPoint
            let strokeWidth =
                DomainToSkia.strokeWidthToSkPhysical scale annotation.Style.StrokeWidth
            use paint = DomainToSkia.createStrokePaint annotation.Style.Color strokeWidth
            canvas.DrawLine(a, b, paint)

            let dx = float (b.X - a.X)
            let dy = float (b.Y - a.Y)
            let len = Math.Sqrt(dx * dx + dy * dy)
            if len > 1e-6 then
                let ux = dx / len
                let uy = dy / len
                let arrowLength = float strokeWidth * 4.0
                let theta = Math.PI / 6.0
                let cosTheta = Math.Cos(theta)
                let sinTheta = Math.Sin(theta)

                let p1x = float b.X - arrowLength * (ux * cosTheta - uy * sinTheta)
                let p1y = float b.Y - arrowLength * (ux * sinTheta + uy * cosTheta)
                let p2x = float b.X - arrowLength * (ux * cosTheta + uy * sinTheta)
                let p2y = float b.Y - arrowLength * (-ux * sinTheta + uy * cosTheta)

                canvas.DrawLine(b, SKPoint(float32 p1x, float32 p1y), paint)
                canvas.DrawLine(b, SKPoint(float32 p2x, float32 p2y), paint)

        | Tool.Rectangle (startPoint, endPoint, cornerRadius) ->
            let x = Math.Min(startPoint.X, endPoint.X) * scale.Value
            let y = Math.Min(startPoint.Y, endPoint.Y) * scale.Value
            let w = Math.Abs(endPoint.X - startPoint.X) * scale.Value
            let h = Math.Abs(endPoint.Y - startPoint.Y) * scale.Value
            let rect = SKRect(float32 x, float32 y, float32 (x + w), float32 (y + h))
            let r = Math.Min(cornerRadius * scale.Value, Math.Min(w / 2.0, h / 2.0)) |> float32
            let strokeWidth = DomainToSkia.strokeWidthToSkPhysical scale annotation.Style.StrokeWidth
            use paint = DomainToSkia.createStrokePaint annotation.Style.Color strokeWidth
            canvas.DrawRoundRect(rect, r, r, paint)

        | Tool.Circle (startPoint, endPoint, aspectLocked) ->
            let x = Math.Min(startPoint.X, endPoint.X)
            let y = Math.Min(startPoint.Y, endPoint.Y)
            let w = Math.Abs(endPoint.X - startPoint.X)
            let h = Math.Abs(endPoint.Y - startPoint.Y)
            let side = Math.Min(w, h)
            let rect =
                if aspectLocked then
                    SKRect(
                        float32 (x * scale.Value),
                        float32 (y * scale.Value),
                        float32 ((x + side) * scale.Value),
                        float32 ((y + side) * scale.Value)
                    )
                else
                    SKRect(
                        float32 (x * scale.Value),
                        float32 (y * scale.Value),
                        float32 ((x + w) * scale.Value),
                        float32 ((y + h) * scale.Value)
                    )
            let strokeWidth = DomainToSkia.strokeWidthToSkPhysical scale annotation.Style.StrokeWidth
            use paint = DomainToSkia.createStrokePaint annotation.Style.Color strokeWidth
            canvas.DrawOval(rect, paint)

        | _ ->
            // Các tool khác sẽ được triển khai sau.
            ()
