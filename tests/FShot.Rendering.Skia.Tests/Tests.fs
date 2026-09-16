module Tests

open System
open Xunit
open SkiaSharp
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Rendering.Skia.Converters
open FShot.Rendering.Skia.Renderers

let private createDummyCaptureResult (w: int) (h: int) : CaptureResult =
    let stride = w * 4
    let pixels = Array.create (stride * h) 200uy
    {
        Pixels = pixels
        Width = w
        Height = h
        Stride = stride
        PixelFormat = PixelFormat.Bgra32
        VirtualBounds = { X = 0.0; Y = 0.0; Width = float w; Height = float h }
        ScaleFactor = ScaleFactor.Create 1.0
        ScreenIndex = 0
    }

[<Fact>]
let ``captureResultToBitmap allocates pixel memory and copies bytes`` () =
    let capture = createDummyCaptureResult 50 50
    use bitmap = DomainToSkia.captureResultToBitmap capture
    Assert.NotNull(bitmap)
    Assert.Equal(50, bitmap.Width)
    Assert.Equal(50, bitmap.Height)
    Assert.NotEqual(IntPtr.Zero, bitmap.GetPixels())

[<Fact>]
let ``SceneComposer renderExport crops correctly and does not throw ArgumentNullException`` () =
    let capture = createDummyCaptureResult 100 100
    let selBounds = { X = 10.0; Y = 10.0; Width = 40.0; Height = 30.0 }
    let selection =
        {
            Bounds = selBounds
            State = SelectionState.Selected
            OriginalBounds = selBounds
            DragStart = Point.Zero
        }
    use exportBmp = SceneComposer.renderExport capture selection []
    Assert.NotNull(exportBmp)
    Assert.Equal(40, exportBmp.Width)
    Assert.Equal(30, exportBmp.Height)
    Assert.NotEqual(IntPtr.Zero, exportBmp.GetPixels())
