namespace FShot.Rendering.Skia.Renderers

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

        | _ ->
            // Các tool khác sẽ được triển khai sau.
            ()
