module FShot.UI.Tests.PinWindowTests

open System
open System.Threading
open Avalonia
open Avalonia.Controls
open Avalonia.Headless
open SkiaSharp
open FShot.UI.Windows
open Xunit

// ==========================================================================
// 1. PinMath — công thức scale / zoom
// ==========================================================================

[<Fact>]
let ``clampScale giới hạn trong [0.1, 5.0]`` () =
    Assert.Equal(0.1, PinMath.clampScale 0.0)
    Assert.Equal(5.0, PinMath.clampScale 100.0)
    Assert.Equal(1.0, PinMath.clampScale 1.0)

[<Fact>]
let ``zoom tăng tỉ lệ khi delta dương và giảm khi delta âm`` () =
    Assert.Equal(1.1, PinMath.zoom 1.0 1.0, 10)
    Assert.Equal(1.0, PinMath.zoom 1.1 -1.0, 10)

[<Fact>]
let ``zoom không vượt quá giới hạn 5.0`` () =
    Assert.Equal(5.0, PinMath.zoom 5.0 1.0)
    Assert.Equal(0.1, PinMath.zoom 0.1 -1.0)

// ==========================================================================
// 2. PinMath — độ mờ đục
// ==========================================================================

[<Fact>]
let ``adjustOpacity tăng giảm đúng bước 5%`` () =
    Assert.Equal(0.55, PinMath.adjustOpacity 0.5 1.0, 10)
    Assert.Equal(0.45, PinMath.adjustOpacity 0.5 -1.0, 10)

[<Fact>]
let ``adjustOpacity giới hạn trong [0.1, 1.0]`` () =
    Assert.Equal(1.0, PinMath.adjustOpacity 1.0 1.0)
    Assert.Equal(0.1, PinMath.adjustOpacity 0.1 -1.0)

// ==========================================================================
// 3. PinMath — xoay 90°
// ==========================================================================

[<Fact>]
let ``rotate xoay 90° đúng hướng và quấn vòng 360°`` () =
    Assert.Equal(90, PinMath.rotate 0 true)
    Assert.Equal(180, PinMath.rotate 90 true)
    Assert.Equal(270, PinMath.rotate 180 true)
    Assert.Equal(0, PinMath.rotate 270 true)
    Assert.Equal(270, PinMath.rotate 0 false)

[<Fact>]
let ``isVertical đúng cho góc 90 và 270`` () =
    Assert.True(PinMath.isVertical 90)
    Assert.True(PinMath.isVertical 270)
    Assert.False(PinMath.isVertical 0)
    Assert.False(PinMath.isVertical 180)

[<Fact>]
let ``displaySize hoán đổi chiều khi góc vuông`` () =
    Assert.Equal((100.0, 50.0), PinMath.displaySize 100.0 50.0 1.0 0)
    Assert.Equal((50.0, 100.0), PinMath.displaySize 100.0 50.0 1.0 90)
    Assert.Equal((100.0, 50.0), PinMath.displaySize 100.0 50.0 1.0 180)
    Assert.Equal((50.0, 100.0), PinMath.displaySize 100.0 50.0 1.0 270)

// ==========================================================================
// 4. PinMath — thu phóng hướng tâm con trỏ
// ==========================================================================

[<Fact>]
let ``zoomTowardCursor giữ điểm giữa đứng yên`` () =
    // Con trỏ nằm chính giữa ảnh (frac = 0.5); padding = 20.
    let (nx, ny) =
        PinMath.zoomTowardCursor 100.0 100.0 70.0 70.0 100.0 100.0 200.0 200.0 20.0
    // screenX = 170, fracX = (70-20)/100 = 0.5 => nx = 170 - (0.5*200 + 20) = 50.0
    Assert.Equal(50.0, nx, 6)
    Assert.Equal(50.0, ny, 6)

[<Fact>]
let ``zoomTowardCursor bám điểm gốc trái-trên khi con trỏ tại gốc ảnh`` () =
    let (nx, ny) =
        PinMath.zoomTowardCursor 100.0 100.0 20.0 20.0 100.0 100.0 200.0 200.0 20.0
    // frac = 0 => điểm gốc ảnh không dịch chuyển.
    Assert.Equal(100.0, nx, 6)
    Assert.Equal(100.0, ny, 6)

// ==========================================================================
// 5. PinBitmap — xoay bitmap 90°
// ==========================================================================

[<Fact>]
let ``PinBitmap.rotate hoán đổi kích thước khi xoay 90°`` () =
    use src = new SKBitmap(20, 10, SKColorType.Bgra8888, SKAlphaType.Premul)

    use r90 = PinBitmap.rotate src 90
    Assert.Equal(10, r90.Width)
    Assert.Equal(20, r90.Height)

    use r180 = PinBitmap.rotate src 180
    Assert.Equal(20, r180.Width)
    Assert.Equal(10, r180.Height)

    use r270 = PinBitmap.rotate src 270
    Assert.Equal(10, r270.Width)
    Assert.Equal(20, r270.Height)

    use r0 = PinBitmap.rotate src 0
    Assert.Equal(20, r0.Width)
    Assert.Equal(10, r0.Height)

// ==========================================================================
// 6. PinWindow — khởi tạo headless (không ném FailInit khi load XAML)
// ==========================================================================

[<Fact>]
let ``PinWindow khởi tạo không ném ngoại lệ và tìm thấy PinImage`` () =
    let ok =
        HeadlessTestSession.session.Value
            .Dispatch(
                (fun () ->
                    let bitmap = new SKBitmap(40, 30, SKColorType.Bgra8888, SKAlphaType.Premul)
                    let window = PinWindow()
                    window.Initialize(bitmap, PixelPoint(0, 0), Size(40.0, 30.0))
                    let image = window.FindControl<Image>("PinImage")
                    let hasImage = not (isNull image)
                    window.Close()
                    hasImage),
                CancellationToken.None
            )
            .GetAwaiter()
            .GetResult()
    Assert.True(ok, "PinWindow không khởi tạo được hoặc thiếu PinImage")
