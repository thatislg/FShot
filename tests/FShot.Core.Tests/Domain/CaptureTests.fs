module FShot.Core.Tests.Domain.CaptureTests

open System
open FShot.Core.Geometry
open FShot.Core.Domain
open Xunit

/// Kiểm tra tạo CaptureRequest mặc định.
[<Fact>]
let ``CaptureRequest mặc định có đúng giá trị`` () =
    let request = CaptureRequest.Default
    Assert.Equal(CaptureMode.GuiInteractive, request.Mode)
    Assert.Equal(0, request.DelayMs)
    Assert.True(request.InitialSelection.IsNone)
    Assert.False(request.AcceptOnSelect)
    Assert.Equal(OutputTarget.OpenGui, request.OutputTarget)

/// Kiểm tra tính thời điểm chụp.
/// Công thức: captureTime = requestTime + DelayMs.
[<Fact>]
let ``CaptureTime được tính đúng`` () =
    let request = { CaptureRequest.Default with DelayMs = 1500 }
    let requestTime = DateTime.UtcNow
    let captureTime = request.CaptureTime(requestTime)
    let expected = requestTime.AddMilliseconds(1500.0)
    Assert.Equal(expected, captureTime)

/// Kiểm tra CaptureResult tính đúng tổng số byte.
/// Công thức: totalBytes = Stride * Height.
[<Fact>]
let ``CaptureResult tính tổng số byte đúng`` () =
    let result =
        {
          Pixels = Array.zeroCreate (11520 * 1620)
          Width = 2880
          Height = 1620
          Stride = 11520
          PixelFormat = PixelFormat.Bgra32
          VirtualBounds =
            {
              X = 0.0
              Y = 0.0
              Width = 1920.0
              Height = 1080.0
            }
          ScaleFactor = ScaleFactor.Create 1.5
          ScreenIndex = 0
        }
    Assert.Equal(4, result.BytesPerPixel)
    Assert.Equal(11520 * 1620, result.TotalBytes)

/// Kiểm tra chuyển vùng chọn logical sang physical.
/// Công thức: round(virtual * scaleFactor).
[<Fact>]
let ``LogicalSelectionToPhysical đúng với scale 1.5`` () =
    let result =
        {
          Pixels = Array.empty
          Width = 2880
          Height = 1620
          Stride = 11520
          PixelFormat = PixelFormat.Bgra32
          VirtualBounds =
            {
              X = 0.0
              Y = 0.0
              Width = 1920.0
              Height = 1080.0
            }
          ScaleFactor = ScaleFactor.Create 1.5
          ScreenIndex = 0
        }
    let selection =
        {
          X = 100.0
          Y = 80.0
          Width = 300.0
          Height = 200.0
        }
    let physical = result.LogicalSelectionToPhysical selection
    Assert.Equal(150.0, physical.X)
    Assert.Equal(120.0, physical.Y)
    Assert.Equal(450.0, physical.Width)
    Assert.Equal(300.0, physical.Height)
