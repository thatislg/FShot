namespace FShot.Rendering.Skia.Renderers

open System
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Rendering.Skia.Converters
open SkiaSharp

/// Vẽ các annotation lên Skia canvas.
/// Xem tài liệu 09_04_AnnotationRenderer.md.
/// Đã triển khai: Pencil, Line, Arrow, Rectangle, Circle.
/// Marker, Text, Pixelate còn lại cho P1.17–P1.19.
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
                    use pathBuilder = new SKPathBuilder()
                    let (start, ctrl, target) = List.head segments
                    pathBuilder.MoveTo(DomainToSkia.pointToSkPhysical scale start)
                    pathBuilder.QuadTo(
                        DomainToSkia.pointToSkPhysical scale ctrl,
                        DomainToSkia.pointToSkPhysical scale target
                    )

                    for (_, c, e) in List.tail segments do
                        pathBuilder.QuadTo(
                            DomainToSkia.pointToSkPhysical scale c,
                            DomainToSkia.pointToSkPhysical scale e
                        )

                    use path = pathBuilder.Detach()
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

            // Chóp mũi tên tam giác nằm ở đầu đích B.
            // Góc giữa mỗi cạnh chóp và trục chính là 30° (π/6); chiều dài chóp = 4 × stroke width.
            // Nếu start/end trùng nhau (len ≈ 0) thì bỏ qua để tránh chia cho 0.
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
            // Luôn vẽ từ góc trên-trái vì start/end có thể ở bất kỳ hướng kéo nào.
            let x = Math.Min(startPoint.X, endPoint.X) * scale.Value
            let y = Math.Min(startPoint.Y, endPoint.Y) * scale.Value
            let w = Math.Abs(endPoint.X - startPoint.X) * scale.Value
            let h = Math.Abs(endPoint.Y - startPoint.Y) * scale.Value
            let rect = SKRect(float32 x, float32 y, float32 (x + w), float32 (y + h))
            // Giới hạn bán kính bo góc không vượt quá nửa cạnh ngắn hơn để tránh lỗi hình học.
            let r = Math.Min(cornerRadius * scale.Value, Math.Min(w / 2.0, h / 2.0)) |> float32
            let strokeWidth = DomainToSkia.strokeWidthToSkPhysical scale annotation.Style.StrokeWidth
            use paint = DomainToSkia.createStrokePaint annotation.Style.Color strokeWidth
            canvas.DrawRoundRect(rect, r, r, paint)

        | Tool.Circle (startPoint, endPoint, aspectLocked) ->
            // Tương tự Rectangle: normalize về góc trên-trái.
            let x = Math.Min(startPoint.X, endPoint.X)
            let y = Math.Min(startPoint.Y, endPoint.Y)
            let w = Math.Abs(endPoint.X - startPoint.X)
            let h = Math.Abs(endPoint.Y - startPoint.Y)
            // Khi aspectLocked (Ctrl), dùng cạnh ngắn hơn cho cả width và height → hình tròn.
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

        | Tool.Marker points ->
            // Marker vẽ nét bán trong suốt phủ lên ảnh gốc.
            // MVP: alpha = 0.35, độ dày gấp 3 lần StrokeWidth hiện tại.
            if List.isEmpty points then
                ()
            else
                let strokeWidth =
                    DomainToSkia.strokeWidthToSkPhysical scale annotation.Style.StrokeWidth * 3.0f

                use paint = DomainToSkia.createStrokePaint annotation.Style.Color strokeWidth
                paint.Color <-
                    let c = DomainToSkia.colorToSk annotation.Style.Color
                    // Giữ 35% alpha so với alpha gốc; nếu màu gốc alpha < 255 thì tỉ lệ cũng giảm theo.
                    SKColor(c.Red, c.Green, c.Blue, byte (float c.Alpha * 0.35))

                use pathBuilder = new SKPathBuilder()
                let start = DomainToSkia.pointToSkPhysical scale (List.head points)
                pathBuilder.MoveTo(start)

                for p in List.tail points do
                    pathBuilder.LineTo(DomainToSkia.pointToSkPhysical scale p)

                use path = pathBuilder.Detach()
                canvas.DrawPath(path, paint)

        | Tool.Text (position, content, alignment) when not (String.IsNullOrWhiteSpace content) ->
            let text = content.Trim()
            let fontSize = float32 annotation.Style.FontSize * float32 scale.Value
            if fontSize > 0.0f then
                let font =
                    match annotation.Style.FontName with
                    | Some name -> new SKFont(SKTypeface.FromFamilyName(name), fontSize)
                    | None -> new SKFont(SKTypeface.Default, fontSize)

                use paint = new SKPaint()
                paint.IsAntialias <- true
                paint.Color <- DomainToSkia.colorToSk annotation.Style.Color

                let baseline = DomainToSkia.pointToSkPhysical scale position
                let textAlign =
                    match alignment with
                    | TextAlignment.Left -> SKTextAlign.Left
                    | TextAlignment.Center -> SKTextAlign.Center
                    | TextAlignment.Right -> SKTextAlign.Right

                canvas.DrawText(text, float32 baseline.X, float32 baseline.Y, textAlign, font, paint)

        | Tool.Pixelate (startPoint, endPoint, blockSize) ->
            // Pixelate tái tạo từ ảnh gốc trong CaptureResult.
            // MVP: blockSize mặc định 10, tính trung bình pixel trong mỗi ô.
            if blockSize <= 0 then
                ()
            else
                let x0 = int (Math.Min(startPoint.X, endPoint.X))
                let y0 = int (Math.Min(startPoint.Y, endPoint.Y))
                let x1 = int (Math.Max(startPoint.X, endPoint.X))
                let y1 = int (Math.Max(startPoint.Y, endPoint.Y))
                if x1 > x0 && y1 > y0 then
                    // MVP tạm thời: vẽ hình chữ nhật mờ bán trong suốt để chỉ vùng Pixelate.
                    // Lý do: renderer này không có truy cập trực tiếp đến backing bitmap của output;
                    // xử lý pixel thật cần snapshot ảnh gốc, sẽ làm trong export pipeline.
                    // TODO: thay bằng xử lý mosaic trên raw byte khi tích hợp export.
                    use overlayPaint = new SKPaint()
                    overlayPaint.Color <- SKColor(0uy, 0uy, 0uy, 128uy)
                    overlayPaint.Style <- SKPaintStyle.Fill
                    let rect =
                        SKRect(
                            float32 x0 * float32 scale.Value,
                            float32 y0 * float32 scale.Value,
                            float32 x1 * float32 scale.Value,
                            float32 y1 * float32 scale.Value
                        )
                    canvas.DrawRect(rect, overlayPaint)

        | _ ->
            // Các tool khác sẽ được triển khai sau.
            ()
