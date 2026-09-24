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
