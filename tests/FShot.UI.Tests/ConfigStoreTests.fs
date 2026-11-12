module FShot.UI.Tests.ConfigStoreTests

open System
open System.IO
open FShot.Core.Domain
open FShot.Platform.Win32.Config
open Xunit

/// Kiểm tra ConfigStore tự động tạo file và trả về cấu hình mặc định khi file chưa tồn tại.
[<Fact>]
let ``ConfigStore loadConfigFrom tu dong tao file va thu muc khi chua ton tai`` () =
    let tempDir = Path.Combine(Path.GetTempPath(), "FShot_Test_" + Guid.NewGuid().ToString("N"))
    let testConfigFile = Path.Combine(tempDir, "config.json")
    try
        Assert.False(Directory.Exists tempDir)
        Assert.False(File.Exists testConfigFile)

        let loaded = ConfigStore.loadConfigFrom testConfigFile
        Assert.Equal(AppConfig.Default, loaded)
        Assert.True(File.Exists testConfigFile)

        // Kiểm tra nội dung file được ghi đúng chuẩn indented JSON
        let content = File.ReadAllText testConfigFile
        Assert.Contains("\"savePath\": \"\"", content)
        Assert.Contains("\"filenamePattern\": \"fshot_%Y-%m-%d-%H%M%S\"", content)
        Assert.Contains("\"drawColor\": \"#FF0000\"", content)
        Assert.Contains("\"drawThickness\": 2", content)
    finally
        if Directory.Exists tempDir then
            Directory.Delete(tempDir, true)

/// Kiểm tra ConfigStore ghi và đọc lại cấu hình chính xác.
[<Fact>]
let ``ConfigStore saveConfigTo va loadConfigFrom doc ghi dung du lieu`` () =
    let tempDir = Path.Combine(Path.GetTempPath(), "FShot_Test_" + Guid.NewGuid().ToString("N"))
    let testConfigFile = Path.Combine(tempDir, "config.json")
    try
        let customConfig = {
            SavePath = "D:\\MyCaptures"
            FilenamePattern = "snap_%Y%m%d"
            DrawColor = "#38BDF8"
            DrawThickness = 4.0
            DefaultTool = "ArrowTool"
            CloseAfterExport = false
            StartupLaunch = true
            ShowDesktopNotification = false
            ShowAbortNotification = true
            DisabledTrayIcon = true
            Hotkeys = [
                { Action = HotkeyAction.CaptureGui; Key = "Ctrl+Shift+X"; Enabled = true }
            ]
            SavePathFixed = true
            SaveAsFileExtension = "jpg"
            JpegQuality = 82
            CopyOnDoubleClick = true
            SaveAfterCopy = true
            CopyPathAfterSave = true
            UndoLimit = 120
            SaveLastRegion = true
            AllowMultipleGuiInstances = false
            CaptureBackend = "Gdi"
            UiColor = "#38BDF8"
            ContrastUiColor = "#0F172A"
            ContrastOpacity = 200uy
            PredefinedColorPaletteLarge = true
            UserColors = [ "#FF0000"; "#00FF00" ]
            Buttons = [ "SelectionTool" ]
            UiLanguage = "en"
            FontFamily = "Segoe UI"
            DrawFontSize = 20.0
            DrawCircleCounterSize = 30.0
            DrawPixelateSize = 12
            DrawRectangleRadius = 5.0
            DrawMarkerSize = 12.0
        }

        ConfigStore.saveConfigTo testConfigFile customConfig
        Assert.True(File.Exists testConfigFile)

        let loaded = ConfigStore.loadConfigFrom testConfigFile
        Assert.Equal(customConfig.SavePath, loaded.SavePath)
        Assert.Equal(customConfig.FilenamePattern, loaded.FilenamePattern)
        Assert.Equal(customConfig.DrawColor, loaded.DrawColor)
        Assert.Equal(customConfig.DrawThickness, loaded.DrawThickness)
        Assert.Equal(customConfig.DefaultTool, loaded.DefaultTool)
        Assert.Equal(customConfig.CloseAfterExport, loaded.CloseAfterExport)
        Assert.Equal(customConfig.StartupLaunch, loaded.StartupLaunch)
        Assert.Equal(customConfig.ShowDesktopNotification, loaded.ShowDesktopNotification)
        Assert.Equal(customConfig.ShowAbortNotification, loaded.ShowAbortNotification)
        Assert.Equal(customConfig.DisabledTrayIcon, loaded.DisabledTrayIcon)

        // Hotkey round-trip (chuẩn hóa thành đủ 3 hành động).
        let gui = loaded.Hotkeys |> List.find (fun h -> h.Action = HotkeyAction.CaptureGui)
        Assert.Equal("Ctrl+Shift+X", gui.Key)
        Assert.True(gui.Enabled)
        let full = loaded.Hotkeys |> List.find (fun h -> h.Action = HotkeyAction.CaptureFullScreen)
        Assert.Equal("", full.Key)
        Assert.False(full.Enabled)

        // Kiểm tra loadSnapshotFrom
        let snapshot = ConfigStore.loadSnapshotFrom testConfigFile
        Assert.Equal(Some "D:\\MyCaptures", snapshot.SaveOptions.Path)
        Assert.Equal("snap_%Y%m%d", snapshot.SaveOptions.FileNamePattern)
        Assert.Equal(ArrowTool, snapshot.DefaultTool)
        Assert.Equal(4.0, snapshot.DefaultStrokeWidth.Value)
        Assert.False(snapshot.CloseAfterExport)
    finally
        if Directory.Exists tempDir then
            Directory.Delete(tempDir, true)

/// Kiểm tra ConfigStore fallback an toàn khi file JSON bị hỏng hoặc lỗi cú pháp.
[<Fact>]
let ``ConfigStore loadConfigFrom fallback ve Default khi file JSON hong`` () =
    let tempDir = Path.Combine(Path.GetTempPath(), "FShot_Test_" + Guid.NewGuid().ToString("N"))
    let testConfigFile = Path.Combine(tempDir, "config.json")
    try
        Directory.CreateDirectory(tempDir) |> ignore
        File.WriteAllText(testConfigFile, "{ corrupt broken json content !!!")

        let loaded = ConfigStore.loadConfigFrom testConfigFile
        Assert.Equal(AppConfig.Default, loaded)
    finally
        if Directory.Exists tempDir then
            Directory.Delete(tempDir, true)

/// Kiểm tra đường dẫn mặc định của ConfigStore trỏ tới %APPDATA%\FShot.
[<Fact>]
let ``ConfigStore default paths dung chuan APPDATA`` () =
    Assert.True(ConfigStore.defaultConfigDir.EndsWith("FShot", StringComparison.OrdinalIgnoreCase))
    Assert.True(ConfigStore.defaultConfigFile.EndsWith("config.json", StringComparison.OrdinalIgnoreCase))
    let appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
    Assert.StartsWith(appData, ConfigStore.defaultConfigFile, StringComparison.OrdinalIgnoreCase)

/// Kiểm tra transferConfig sao chép cấu hình từ nguồn sang đích (round-trip import/export).
[<Fact>]
let ``ConfigStore transferConfig sao chep cau hinh nguon sang dich`` () =
    let tempDir = Path.Combine(Path.GetTempPath(), "FShot_Test_" + Guid.NewGuid().ToString("N"))
    let source = Path.Combine(tempDir, "source.json")
    let target = Path.Combine(tempDir, "target.json")
    try
        Directory.CreateDirectory(tempDir) |> ignore
        let custom = { AppConfig.Default with SavePath = "C:\\Shots"; DrawColor = "#123456"; JpegQuality = 75 }
        ConfigStore.saveConfigTo source custom

        match ConfigStore.transferConfig source target with
        | Ok () ->
            Assert.True(File.Exists target)
            let loaded = ConfigStore.loadConfigFrom target
            Assert.Equal("C:\\Shots", loaded.SavePath)
            Assert.Equal("#123456", loaded.DrawColor)
            Assert.Equal(75, loaded.JpegQuality)
        | Error err -> Assert.Fail("transferConfig unexpectedly failed: " + err)
    finally
        if Directory.Exists tempDir then Directory.Delete(tempDir, true)

/// Kiểm tra transferConfig từ chối file JSON không hợp lệ.
[<Fact>]
let ``ConfigStore transferConfig tu choi file JSON khong hop le`` () =
    let tempDir = Path.Combine(Path.GetTempPath(), "FShot_Test_" + Guid.NewGuid().ToString("N"))
    let source = Path.Combine(tempDir, "broken.json")
    let target = Path.Combine(tempDir, "target.json")
    try
        Directory.CreateDirectory(tempDir) |> ignore
        File.WriteAllText(source, "{ not valid json !!!")
        match ConfigStore.transferConfig source target with
        | Ok () -> Assert.Fail("Expected error for invalid JSON")
        | Error _ -> ()
    finally
        if Directory.Exists tempDir then Directory.Delete(tempDir, true)

/// Kiểm tra resetConfigTo ghi cấu hình mặc định ra file chỉ định.
[<Fact>]
let ``ConfigStore resetConfigTo ghi cau hinh mac dinh`` () =
    let tempDir = Path.Combine(Path.GetTempPath(), "FShot_Test_" + Guid.NewGuid().ToString("N"))
    let target = Path.Combine(tempDir, "config.json")
    try
        match ConfigStore.resetConfigTo target with
        | Ok () ->
            let loaded = ConfigStore.loadConfigFrom target
            Assert.Equal(AppConfig.Default.SavePath, loaded.SavePath)
            Assert.Equal(AppConfig.Default.DrawColor, loaded.DrawColor)
            Assert.Equal(AppConfig.Default.DrawThickness, loaded.DrawThickness)
            Assert.Equal(AppConfig.Default.JpegQuality, loaded.JpegQuality)
        | Error err -> Assert.Fail("resetConfigTo unexpectedly failed: " + err)
    finally
        if Directory.Exists tempDir then Directory.Delete(tempDir, true)
