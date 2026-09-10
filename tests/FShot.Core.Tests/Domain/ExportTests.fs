module FShot.Core.Tests.Domain.ExportTests

open FShot.Core.Geometry
open FShot.Core.Domain
open Xunit

let private rect x y w h =
    {
      X = x
      Y = y
      Width = w
      Height = h
    }

let private captureResult scale =
    {
      Pixels = Array.empty
      Width = 2880
      Height = 1620
      Stride = 11520
      PixelFormat = PixelFormat.Bgra32
      VirtualBounds = rect 0.0 0.0 1920.0 1080.0
      ScaleFactor = ScaleFactor.Create scale
      ScreenIndex = 0
    }

/// Kiểm tra SaveOptions mặc định.
[<Fact>]
let ``SaveOptions mặc định`` () =
    let options = SaveOptions.Default
    Assert.True(options.Path.IsNone)
    Assert.Equal("fshot_%Y-%m-%d-%H%M%S", options.FileNamePattern)
    Assert.Equal(Png, options.Format)
    Assert.Equal(90, options.NormalizedJpegQuality)

/// Kiểm tra JpegQuality bị giới hạn trong [0, 100].
[<Fact>]
let ``JpegQuality bị giới hạn`` () =
    let tooHigh = { SaveOptions.Default with JpegQuality = 150 }
    Assert.Equal(100, tooHigh.NormalizedJpegQuality)

    let tooLow = { SaveOptions.Default with JpegQuality = -10 }
    Assert.Equal(0, tooLow.NormalizedJpegQuality)

/// Kiểm tra tính kích thước output.
/// Công thức: round(w * s), round(h * s).
[<Fact>]
let ``OutputSize tính đúng với scale 1.5`` () =
    let request =
        {
          CaptureResult = captureResult 1.5
          SelectionBounds = rect 100.0 80.0 300.0 200.0
          Annotations = []
          Target = CopyToClipboard
          SaveOptions = SaveOptions.Default
        }
    let width, height = request.OutputSize
    Assert.Equal(450, width)
    Assert.Equal(300, height)

/// Kiểm tra chuỗi geometry WxH+X+Y.
[<Fact>]
let ``GeometryString đúng định dạng`` () =
    let request =
        {
          CaptureResult = captureResult 1.0
          SelectionBounds = rect 100.0 80.0 300.0 200.0
          Annotations = []
          Target = PrintGeometry
          SaveOptions = SaveOptions.Default
        }
    Assert.Equal("300x200+100+80", request.GeometryString)
