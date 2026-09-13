module FShot.Rendering.Skia.Tests.DimmingTests

open System
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Rendering.Skia.Renderers
open SkiaSharp
open Xunit

let private createCaptureResult(width: int, height: int) : CaptureResult =
    let bytes = width * height * 4
    {
        Pixels = Array.zeroCreate bytes
        Width = width
        Height = height
        Stride = width * 4
        PixelFormat = PixelFormat.Bgra32
        VirtualBounds = { X = 0.0; Y = 0.0; Width = float width; Height = float height }
        ScaleFactor = ScaleFactor.Create 1.0
        ScreenIndex = 0
    }

let private getPixelColor (bitmap: SKBitmap) (x: int) (y: int) : SKColor =
    bitmap.GetPixel(x, y)

/// Kiểm tra dimming phủ toàn màn hình ngoài vùng chọn.
[<Fact>]
let ``Dimming phủ toàn màn hình ngoài vùng chọn`` () =
    let capture = createCaptureResult(400, 300)
    let selectionBounds = { X = 100.0; Y = 80.0; Width = 200.0; Height = 150.0 }

    let info = new SKImageInfo(400, 300, SKColorType.Bgra8888, SKAlphaType.Premul)
    use surface = SKSurface.Create(info)
    let canvas = surface.Canvas

    // Vẽ nền trắng trước.
    canvas.Clear(SKColors.White)

    // Vẽ dimming.
    DimmingRenderer.renderDefault canvas capture selectionBounds

    use image = surface.Snapshot()
    use bitmap = SKBitmap.FromImage(image)

    // Pixel ngoài vùng chọn phải bị tối (khác với trắng tinh khiết).
    let outsideColor = getPixelColor bitmap 50 50
    Assert.NotEqual(SKColors.White, outsideColor)

    // Pixel trong vùng chọn phải khác với vùng ngoài (đã được clear/khoét).
    // Lưu ý: khi vẽ với Premul alpha, pixel trong vùng clear vẫn là trắng nhưng alpha có thể = 0.
    // Do đó chỉ cần kiểm tra inside != outside.
    let insideColor = getPixelColor bitmap 150 100
    Assert.NotEqual(outsideColor, insideColor)

/// Kiểm tra opacity 0.5 cho ra màu tối hơn nền trắng.
[<Fact>]
let ``Dimming với opacity 0.5 tạo màu tối hơn nền trắng`` () =
    let capture = createCaptureResult(200, 200)
    let selectionBounds = { X = 50.0; Y = 50.0; Width = 100.0; Height = 100.0 }

    let info = new SKImageInfo(200, 200, SKColorType.Bgra8888, SKAlphaType.Premul)
    use surface = SKSurface.Create(info)
    let canvas = surface.Canvas

    canvas.Clear(SKColors.White)
    DimmingRenderer.render canvas capture selectionBounds 0.5

    use image = surface.Snapshot()
    use bitmap = SKBitmap.FromImage(image)

    let dimmedColor = getPixelColor bitmap 10 10
    // Với nền trắng và màu đen alpha 128, kết quả pha trộn sẽ là màu xám.
    Assert.True(dimmedColor.Red < 255uy, sprintf "Red = %A" dimmedColor.Red)
    Assert.True(dimmedColor.Green < 255uy, sprintf "Green = %A" dimmedColor.Green)
    Assert.True(dimmedColor.Blue < 255uy, sprintf "Blue = %A" dimmedColor.Blue)

/// Kiểm tra opacity 0 không thay đổi màu.
[<Fact>]
let ``Dimming với opacity 0 giữ nguyên nền`` () =
    let capture = createCaptureResult(200, 200)
    let selectionBounds = { X = 50.0; Y = 50.0; Width = 100.0; Height = 100.0 }

    let info = new SKImageInfo(200, 200, SKColorType.Bgra8888, SKAlphaType.Premul)
    use surface = SKSurface.Create(info)
    let canvas = surface.Canvas

    canvas.Clear(SKColors.White)
    DimmingRenderer.render canvas capture selectionBounds 0.0

    use image = surface.Snapshot()
    use bitmap = SKBitmap.FromImage(image)

    let color = getPixelColor bitmap 10 10
    Assert.Equal(SKColors.White, color)
