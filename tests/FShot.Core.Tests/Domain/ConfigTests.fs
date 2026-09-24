module FShot.Core.Tests.Domain.ConfigTests

open System
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

/// Kiểm tra AppConfig.Default chứa đúng các giá trị mặc định theo MVP.
[<Fact>]
let ``AppConfig default có đúng các trường MVP`` () =
    let cfg = AppConfig.Default
    Assert.Equal("", cfg.SavePath)
    Assert.Equal("fshot_%Y-%m-%d-%H%M%S", cfg.FilenamePattern)
    Assert.Equal("#FF0000", cfg.DrawColor)
    Assert.Equal(2.0, cfg.DrawThickness)
    Assert.Equal("SelectionTool", cfg.DefaultTool)
    Assert.True(cfg.CloseAfterExport)
    Assert.False(cfg.StartupLaunch)

/// Kiểm tra AppConfig chuyển sang ConfigSnapshot chính xác.
[<Fact>]
let ``AppConfig ToSnapshot chuyển đổi đúng`` () =
    let appConfig = {
        SavePath = "D:\\Screenshots"
        FilenamePattern = "capture_%Y%m%d_%H%M%S"
        DrawColor = "#0000FFFF" // Blue
        DrawThickness = 4.0
        DefaultTool = "ArrowTool"
        CloseAfterExport = false
        StartupLaunch = true
    }
    let snapshot = appConfig.ToSnapshot()
    Assert.Equal(ArrowTool, snapshot.DefaultTool)
    Assert.Equal(Color.Blue, snapshot.DefaultColor)
    Assert.Equal(4.0, snapshot.DefaultStrokeWidth.Value)
    Assert.False(snapshot.CloseAfterExport)
    Assert.Equal(Some "D:\\Screenshots", snapshot.SaveOptions.Path)
    Assert.Equal("capture_%Y%m%d_%H%M%S", snapshot.SaveOptions.FileNamePattern)

/// Kiểm tra AppConfig chuyển đổi linh hoạt các tên tool viết tắt.
[<Theory>]
[<InlineData("selection", "SelectionTool")>]
[<InlineData("pencil", "PencilTool")>]
[<InlineData("line", "LineTool")>]
[<InlineData("arrow", "ArrowTool")>]
[<InlineData("rectangle", "RectangleTool")>]
[<InlineData("circle", "CircleTool")>]
[<InlineData("marker", "MarkerTool")>]
[<InlineData("text", "TextTool")>]
[<InlineData("pixelate", "PixelateTool")>]
[<InlineData("icon", "IconTool")>]
[<InlineData("invalid_tool", "SelectionTool")>]
let ``AppConfig ToSnapshot phân giải đúng tool names`` (inputTool: string, expectedToolKind: string) =
    let cfg = { AppConfig.Default with DefaultTool = inputTool }
    let snapshot = cfg.ToSnapshot()
    Assert.Equal(expectedToolKind, string snapshot.DefaultTool)

/// Kiểm tra FromSnapshot chuyển từ ConfigSnapshot sang AppConfig.
[<Fact>]
let ``AppConfig FromSnapshot và ToSnapshot tương thích hai chiều`` () =
    let snapshot = {
        ConfigSnapshot.Default with
            DefaultTool = RectangleTool
            DefaultColor = Color.Green
            DefaultStrokeWidth = StrokeWidth.Create 3.5
            CloseAfterExport = false
            SaveOptions = {
                SaveOptions.Default with
                    Path = Some "C:\\Pictures"
                    FileNamePattern = "shot_%Y"
            }
    }
    let appCfg = AppConfig.FromSnapshot(snapshot)
    Assert.Equal("C:\\Pictures", appCfg.SavePath)
    Assert.Equal("shot_%Y", appCfg.FilenamePattern)
    Assert.Equal("#00FF00FF", appCfg.DrawColor)
    Assert.Equal(3.5, appCfg.DrawThickness)
    Assert.Equal("RectangleTool", appCfg.DefaultTool)
    Assert.False(appCfg.CloseAfterExport)
    Assert.False(appCfg.StartupLaunch)

    let backToSnapshot = appCfg.ToSnapshot()
    Assert.Equal(snapshot.DefaultTool, backToSnapshot.DefaultTool)
    Assert.Equal(snapshot.DefaultColor, backToSnapshot.DefaultColor)
    Assert.Equal(snapshot.DefaultStrokeWidth.Value, backToSnapshot.DefaultStrokeWidth.Value)
    Assert.Equal(snapshot.CloseAfterExport, backToSnapshot.CloseAfterExport)
    Assert.Equal(snapshot.SaveOptions.Path, backToSnapshot.SaveOptions.Path)

/// Kiểm tra AppConfig.Normalized() xử lý dữ liệu bất thường an toàn.
[<Fact>]
let ``AppConfig Normalized làm sạch dữ liệu bất thường`` () =
    let dirty = {
        SavePath = "   "
        FilenamePattern = ""
        DrawColor = "invalid_hex"
        DrawThickness = -10.0
        DefaultTool = "unknown_tool"
        CloseAfterExport = true
        StartupLaunch = false
    }
    let clean = dirty.Normalized()
    Assert.Equal("", clean.SavePath)
    Assert.Equal("fshot_%Y-%m-%d-%H%M%S", clean.FilenamePattern)
    Assert.Equal("#FF0000", clean.DrawColor)
    Assert.Equal(2.0, clean.DrawThickness)
    Assert.Equal("SelectionTool", clean.DefaultTool)
    Assert.False(clean.StartupLaunch)

/// Kiểm tra tuần tự hóa JSON bằng ConfigJson.serialize.
[<Fact>]
let ``ConfigJson serialize tạo chuỗi JSON đúng định dạng`` () =
    let cfg = {
        SavePath = "C:\\Captures"
        FilenamePattern = "test_%Y"
        DrawColor = "#38BDF8"
        DrawThickness = 3.0
        DefaultTool = "PencilTool"
        CloseAfterExport = true
        StartupLaunch = false
    }
    let json = ConfigJson.serialize cfg
    Assert.Contains("\"savePath\": \"C:\\\\Captures\"", json)
    Assert.Contains("\"filenamePattern\": \"test_%Y\"", json)
    Assert.Contains("\"drawColor\": \"#38BDF8\"", json)
    Assert.Contains("\"drawThickness\": 3", json)
    Assert.Contains("\"defaultTool\": \"PencilTool\"", json)
    Assert.Contains("\"closeAfterExport\": true", json)
    Assert.Contains("\"startupLaunch\": false", json)

/// Kiểm tra giải tuần tự hóa JSON bằng ConfigJson.deserialize.
[<Fact>]
let ``ConfigJson deserialize nạp đúng JSON`` () =
    let json = """
    {
        "savePath": "E:\\Screenshots",
        "filenamePattern": "custom_%Y-%m-%d",
        "drawColor": "#00FF00",
        "drawThickness": 4.5,
        "defaultTool": "MarkerTool",
        "closeAfterExport": false,
        "startupLaunch": true
    }
    """
    let cfgOpt = ConfigJson.deserialize json
    Assert.True(cfgOpt.IsSome)
    let cfg = cfgOpt.Value
    Assert.Equal("E:\\Screenshots", cfg.SavePath)
    Assert.Equal("custom_%Y-%m-%d", cfg.FilenamePattern)
    Assert.Equal("#00FF00", cfg.DrawColor)
    Assert.Equal(4.5, cfg.DrawThickness)
    Assert.Equal("MarkerTool", cfg.DefaultTool)
    Assert.False(cfg.CloseAfterExport)
    Assert.True(cfg.StartupLaunch)

/// Kiểm tra deserialize chịu lỗi khi JSON thiếu trường hoặc rỗng.
[<Fact>]
let ``ConfigJson deserializeOrDefault fallback an toàn khi JSON lỗi hoặc thiếu trường`` () =
    // JSON rỗng
    let emptyResult = ConfigJson.deserializeOrDefault ""
    Assert.Equal(AppConfig.Default, emptyResult)

    // JSON sai cú pháp
    let malformedResult = ConfigJson.deserializeOrDefault "{ not a json }"
    Assert.Equal(AppConfig.Default, malformedResult)

    // JSON thiếu một số trường
    let partialJson = """{ "savePath": "D:\\MyCaptures" }"""
    let partialResult = ConfigJson.deserializeOrDefault partialJson
    Assert.Equal("D:\\MyCaptures", partialResult.SavePath)
    Assert.Equal("fshot_%Y-%m-%d-%H%M%S", partialResult.FilenamePattern)
    Assert.Equal("#FF0000", partialResult.DrawColor)
    Assert.Equal(2.0, partialResult.DrawThickness)
    Assert.Equal("SelectionTool", partialResult.DefaultTool)
    Assert.True(partialResult.CloseAfterExport)
    Assert.False(partialResult.StartupLaunch)
