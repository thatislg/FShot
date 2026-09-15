module FShot.Core.Tests.Domain.ConfigTests

open FShot.Core.Domain
open FShot.Core.Geometry
open Xunit

/// Kiểm tra ConfigSnapshot mặc định có giá trị hợp lý.
[<Fact>]
let ``ConfigSnapshot default có giá trị hợp lệ`` () =
    let config = ConfigSnapshot.Default
    Assert.Equal(SelectionTool, config.DefaultTool)
    Assert.Equal(Color.Red, config.DefaultColor)
    Assert.Equal(2.0, config.DefaultStrokeWidth.Value)
    Assert.Equal(14.0, config.DefaultFontSize)
    Assert.Equal(100, config.HistoryLimit)
    Assert.True(config.CloseAfterExport)
    Assert.Equal(SaveOptions.Default, config.SaveOptions)

/// Kiểm tra ConfigSnapshot có thể tạo với giá trị tùy chỉnh.
[<Fact>]
let ``ConfigSnapshot tùy chỉnh lưu đúng giá trị`` () =
    let customSave = {
        SaveOptions.Default with
            Path = Some "C:\\Screenshots"
            FileNamePattern = "custom_%Y%m%d"
            Format = Jpg
            JpegQuality = 85
    }
    let config = {
        DefaultTool = LineTool
        DefaultColor = Color.Blue
        DefaultStrokeWidth = StrokeWidth.Create 5.0
        DefaultFontSize = 20.0
        HistoryLimit = 50
        CloseAfterExport = false
        SaveOptions = customSave
    }
    Assert.Equal(LineTool, config.DefaultTool)
    Assert.Equal(Color.Blue, config.DefaultColor)
    Assert.Equal(5.0, config.DefaultStrokeWidth.Value)
    Assert.Equal(20.0, config.DefaultFontSize)
    Assert.Equal(50, config.HistoryLimit)
    Assert.False(config.CloseAfterExport)
    Assert.Equal(Some "C:\\Screenshots", config.SaveOptions.Path)
    Assert.Equal(Jpg, config.SaveOptions.Format)
    Assert.Equal(85, config.SaveOptions.JpegQuality)

/// Kiểm tra ConfigSnapshot dùng để khởi tạo OverlayState tạo đúng style.
[<Fact>]
let ``ConfigSnapshot khởi tạo OverlayState tạo đúng CurrentStyle`` () =
    let capture = {
        Pixels = Array.empty
        Width = 1920
        Height = 1080
        Stride = 1920 * 4
        PixelFormat = PixelFormat.Bgra32
        VirtualBounds = { X = 0.0; Y = 0.0; Width = 1920.0; Height = 1080.0 }
        ScaleFactor = ScaleFactor.Create 1.0
        ScreenIndex = 0
    }
    let config = {
        DefaultTool = ArrowTool
        DefaultColor = Color.Green
        DefaultStrokeWidth = StrokeWidth.Create 3.0
        DefaultFontSize = 18.0
        HistoryLimit = 20
        CloseAfterExport = true
        SaveOptions = SaveOptions.Default
    }
    let state = FShot.Core.State.OverlayStateLogic.init capture config
    Assert.Equal(ArrowTool, state.CurrentTool)
    Assert.Equal(Color.Green, state.CurrentStyle.Color)
    Assert.Equal(3.0, state.CurrentStyle.StrokeWidth.Value)
    Assert.Equal(18.0, state.CurrentStyle.FontSize)
