module FShot.Rendering.Skia.Tests.AnnotationRendererTests

open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Rendering.Skia.Renderers
open SkiaSharp
open Xunit

let private createPencilAnnotation (points: Point list) (strokeWidth: float) : Annotation =
    let style = {
        Color = Color.Red
        StrokeWidth = StrokeWidth.Create strokeWidth
        FontSize = 14.0
        FontName = None
        FontStyle = []
    }

    {
      Id = System.Guid.NewGuid()
      Tool = Tool.Pencil points
      Style = style
      CreatedAt = System.DateTime.UtcNow
    }

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

/// Kiểm tra annotation không có điểm không gây lỗi.
[<Fact>]
let ``Pencil rỗng không gây lỗi`` () =
    let annotation = createPencilAnnotation [] 2.0
    let scale = ScaleFactor.Create 1.0

    use bitmap = new SKBitmap(10, 10, SKColorType.Bgra8888, SKAlphaType.Premul)
    use canvas = new SKCanvas(bitmap)
    AnnotationRenderer.renderAnnotation canvas scale annotation
    Assert.True(true)
