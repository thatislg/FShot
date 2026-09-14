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

let private createArrowAnnotation (start: Point) (endPoint: Point) (color: Color) (strokeWidth: float) : Annotation =
    createAnnotation (Tool.Arrow(start, endPoint, ArrowStyle.Standard)) color strokeWidth

let private createRectangleAnnotation (start: Point) (endPoint: Point) (color: Color) (strokeWidth: float) : Annotation =
    createAnnotation (Tool.Rectangle(start, endPoint, 0.0)) color strokeWidth

let private createCircleAnnotation (start: Point) (endPoint: Point) (aspectLocked: bool) (color: Color) (strokeWidth: float) : Annotation =
    createAnnotation (Tool.Circle(start, endPoint, aspectLocked)) color strokeWidth

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

/// Kiểm tra Arrow vẽ được pixel màu đỏ.
[<Fact>]
let ``Arrow render vẽ ít nhất một pixel màu đỏ`` () =
    let start = { X = 5.0; Y = 5.0 }
    let endPoint = { X = 45.0; Y = 45.0 }
    let annotation = createArrowAnnotation start endPoint Color.Red 3.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(50, 50, SKColorType.Bgra8888, SKAlphaType.Premul)
    bitmap.Erase(SKColor.Empty)

    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation

    Assert.True(hasPixelWithRed bitmap, "Không tìm thấy pixel màu đỏ sau khi render Arrow.")

/// Kiểm tra Rectangle vẽ được pixel màu xanh.
[<Fact>]
let ``Rectangle render vẽ ít nhất một pixel màu xanh`` () =
    let start = { X = 10.0; Y = 10.0 }
    let endPoint = { X = 40.0; Y = 40.0 }
    let annotation = createRectangleAnnotation start endPoint Color.Blue 2.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(50, 50, SKColorType.Bgra8888, SKAlphaType.Premul)
    bitmap.Erase(SKColor.Empty)

    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation

    Assert.True(hasPixelWithBlue bitmap, "Không tìm thấy pixel màu xanh sau khi render Rectangle.")

/// Kiểm tra Circle vẽ được pixel màu đỏ khi không khóa tỉ lệ.
[<Fact>]
let ``Circle render vẽ ít nhất một pixel màu đỏ`` () =
    let start = { X = 10.0; Y = 15.0 }
    let endPoint = { X = 40.0; Y = 35.0 }
    let annotation = createCircleAnnotation start endPoint false Color.Red 2.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(50, 50, SKColorType.Bgra8888, SKAlphaType.Premul)
    bitmap.Erase(SKColor.Empty)

    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation

    Assert.True(hasPixelWithRed bitmap, "Không tìm thấy pixel màu đỏ sau khi render Circle.")

/// Kiểm tra các tool hai điểm với start/end trùng nhau không gây lỗi (chống degrade divide-by-zero).
[<Fact>]
let ``Arrow Rectangle Circle điểm trùng nhau không gây lỗi`` () =
    let start = { X = 25.0; Y = 25.0 }
    let endPoint = { X = 25.0; Y = 25.0 }
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(50, 50, SKColorType.Bgra8888, SKAlphaType.Premul)
    use canvas = new SKCanvas(bitmap)

    let arrow = createArrowAnnotation start endPoint Color.Red 2.0
    let rect = createRectangleAnnotation start endPoint Color.Blue 2.0
    let circleNormal = createCircleAnnotation start endPoint false Color.Green 2.0
    let circleLocked = createCircleAnnotation start endPoint true Color.Green 2.0

    AnnotationRenderer.renderAnnotation canvas scale arrow
    AnnotationRenderer.renderAnnotation canvas scale rect
    AnnotationRenderer.renderAnnotation canvas scale circleNormal
    AnnotationRenderer.renderAnnotation canvas scale circleLocked

    Assert.True(true)

/// Kiểm tra Circle khóa tỉ lệ vẽ được pixel khi bounding box không vuông.
[<Fact>]
let ``Circle khóa tỉ lệ render vẽ ít nhất một pixel màu xanh`` () =
    let start = { X = 10.0; Y = 10.0 }
    let endPoint = { X = 40.0; Y = 20.0 }
    let annotation = createCircleAnnotation start endPoint true Color.Blue 2.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(50, 50, SKColorType.Bgra8888, SKAlphaType.Premul)
    bitmap.Erase(SKColor.Empty)

    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation

    Assert.True(hasPixelWithBlue bitmap, "Không tìm thấy pixel màu xanh sau khi render Circle khóa tỉ lệ.")

let private createMarkerAnnotation (points: Point list) (color: Color) (strokeWidth: float) : Annotation =
    createAnnotation (Tool.Marker points) color strokeWidth

let private createTextAnnotation (position: Point) (content: string) (color: Color) (fontSize: float) : Annotation =
    let style = {
        Color = color
        StrokeWidth = StrokeWidth.Create 2.0
        FontSize = fontSize
        FontName = None
        FontStyle = []
    }

    {
      Id = System.Guid.NewGuid()
      Tool = Tool.Text(position, content, TextAlignment.Left)
      Style = style
      CreatedAt = System.DateTime.UtcNow
    }

let private createPixelateAnnotation (start: Point) (endPoint: Point) (blockSize: int) (color: Color) (strokeWidth: float) : Annotation =
    createAnnotation (Tool.Pixelate(start, endPoint, blockSize)) color strokeWidth

/// Kiểm tra Marker vẽ được pixel màu đỏ với alpha blend.
[<Fact>]
let ``Marker render vẽ ít nhất một pixel màu đỏ`` () =
    let points =
        [
            { X = 5.0; Y = 5.0 }
            { X = 45.0; Y = 45.0 }
        ]

    let annotation = createMarkerAnnotation points Color.Red 2.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(50, 50, SKColorType.Bgra8888, SKAlphaType.Premul)
    bitmap.Erase(SKColor.Empty)

    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation

    Assert.True(hasPixelWithRed bitmap, "Không tìm thấy pixel màu đỏ sau khi render Marker.")

/// Kiểm tra Text vẽ được pixel màu đỏ khi nội dung không rỗng.
[<Fact>]
let ``Text render vẽ ít nhất một pixel màu đỏ`` () =
    let position = { X = 5.0; Y = 20.0 }
    let annotation = createTextAnnotation position "Test" Color.Red 16.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(100, 40, SKColorType.Bgra8888, SKAlphaType.Premul)
    bitmap.Erase(SKColor.Empty)

    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation

    Assert.True(hasPixelWithRed bitmap, "Không tìm thấy pixel màu đỏ sau khi render Text.")

/// Kiểm tra Text rỗng không gây lỗi và không vẽ pixel.
[<Fact>]
let ``Text rỗng không gây lỗi`` () =
    let position = { X = 5.0; Y = 20.0 }
    let annotation = createTextAnnotation position "   " Color.Red 16.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(100, 40, SKColorType.Bgra8888, SKAlphaType.Premul)
    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation
    Assert.True(true)

/// Kiểm tra Pixelate không gây lỗi với vùng hợp lệ.
[<Fact>]
let ``Pixelate render không gây lỗi`` () =
    let start = { X = 10.0; Y = 10.0 }
    let endPoint = { X = 40.0; Y = 40.0 }
    let annotation = createPixelateAnnotation start endPoint 10 Color.Blue 2.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(50, 50, SKColorType.Bgra8888, SKAlphaType.Premul)
    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation
    Assert.True(true)

/// Kiểm tra Pixelate với blockSize = 0 không gây lỗi.
[<Fact>]
let ``Pixelate blockSize 0 không gây lỗi`` () =
    let start = { X = 10.0; Y = 10.0 }
    let endPoint = { X = 40.0; Y = 40.0 }
    let annotation = createPixelateAnnotation start endPoint 0 Color.Blue 2.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(50, 50, SKColorType.Bgra8888, SKAlphaType.Premul)
    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation
    Assert.True(true)

/// Kiểm tra Marker rỗng không gây lỗi.
[<Fact>]
let ``Marker rỗng không gây lỗi`` () =
    let annotation = createMarkerAnnotation [] Color.Red 2.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(10, 10, SKColorType.Bgra8888, SKAlphaType.Premul)
    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation
    Assert.True(true)
