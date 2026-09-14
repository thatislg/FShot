module FShot.Rendering.Skia.Tests.AnnotationRendererTests

open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Rendering.Skia.Renderers
open SkiaSharp
open Xunit

let private createAnnotation (tool: Tool) (color: Color) (strokeWidth: float) : Annotation =
    let style = {
        Color = color
        StrokeWidth = StrokeWidth.Create strokeWidth
        FontSize = 14.0
        FontName = None
        FontStyle = []
    }

    {
      Id = System.Guid.NewGuid()
      Tool = tool
      Style = style
      CreatedAt = System.DateTime.UtcNow
    }

let private createPencilAnnotation (points: Point list) (strokeWidth: float) : Annotation =
    createAnnotation (Tool.Pencil points) Color.Red strokeWidth

let private createLineAnnotation (start: Point) (endPoint: Point) (color: Color) (strokeWidth: float) : Annotation =
    createAnnotation (Tool.Line(start, endPoint)) color strokeWidth

let private hasPixelWithRed (bitmap: SKBitmap) : bool =
    let mutable found = false
    for y in 0 .. bitmap.Height - 1 do
        for x in 0 .. bitmap.Width - 1 do
            let color = bitmap.GetPixel(x, y)
            if color.Red > 0uy then
                found <- true
    found

let private hasPixelWithBlue (bitmap: SKBitmap) : bool =
    let mutable found = false
    for y in 0 .. bitmap.Height - 1 do
        for x in 0 .. bitmap.Width - 1 do
            let color = bitmap.GetPixel(x, y)
            if color.Blue > 0uy then
                found <- true
    found

/// Kiểm tra renderAnnotation cho Pencil không ném lỗi và vẽ được pixel.
[<Fact>]
let ``Pencil render vẽ ít nhất một pixel màu đỏ`` () =
    let points =
        [
            { X = 5.0; Y = 5.0 }
            { X = 25.0; Y = 5.0 }
            { X = 45.0; Y = 25.0 }
        ]

    let annotation = createPencilAnnotation points 2.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(50, 50, SKColorType.Bgra8888, SKAlphaType.Premul)
    // Xóa nền trong suốt để dễ kiểm tra pixel.
    bitmap.Erase(SKColor.Empty)

    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation

    // Tìm ít nhất một pixel có kênh đỏ khác không.
    let mutable foundRed = false
    for y in 0 .. bitmap.Height - 1 do
        for x in 0 .. bitmap.Width - 1 do
            let color = bitmap.GetPixel(x, y)
            if color.Red > 0uy then
                foundRed <- true

    Assert.True(foundRed, "Không tìm thấy pixel màu đỏ sau khi render Pencil.")

/// Kiểm tra renderAnnotation cho Line vẽ được pixel màu xanh.
[<Fact>]
let ``Line render vẽ ít nhất một pixel màu xanh`` () =
    let start = { X = 5.0; Y = 5.0 }
    let endPoint = { X = 45.0; Y = 45.0 }
    let annotation = createLineAnnotation start endPoint Color.Blue 3.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(50, 50, SKColorType.Bgra8888, SKAlphaType.Premul)
    bitmap.Erase(SKColor.Empty)

    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation

    Assert.True(hasPixelWithBlue bitmap, "Không tìm thấy pixel màu xanh sau khi render Line.")

/// Kiểm tra annotation không có điểm không gây lỗi.
[<Fact>]
let ``Pencil rỗng không gây lỗi`` () =
    let annotation = createPencilAnnotation [] 2.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(10, 10, SKColorType.Bgra8888, SKAlphaType.Premul)
    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation
    Assert.True(true)

/// Kiểm tra Line với hai điểm trùng nhau không gây lỗi.
[<Fact>]
let ``Line điểm trùng nhau không gây lỗi`` () =
    let start = { X = 10.0; Y = 10.0 }
    let endPoint = { X = 10.0; Y = 10.0 }
    let annotation = createLineAnnotation start endPoint Color.Black 2.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(20, 20, SKColorType.Bgra8888, SKAlphaType.Premul)
    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation
    Assert.True(true)
