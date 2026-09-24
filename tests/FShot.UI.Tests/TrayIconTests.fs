module FShot.UI.Tests.TrayIconTests

open System
open FShot.Platform.Win32.Tray
open FShot.Rendering.Skia.Icons
open Xunit

[<Fact>]
let ``TrayIconBitmap creates non-empty bitmap of requested size``() =
    use bitmap = TrayIconBitmap.create 64
    Assert.Equal(64, bitmap.Width)
    Assert.Equal(64, bitmap.Height)
    Assert.False(bitmap.IsNull)

[<Fact>]
let ``TrayIconBitmap creates PNG stream with PNG header``() =
    use stream = TrayIconBitmap.createPngStream 64
    Assert.True(stream.Length > 0L)
    let header = Array.zeroCreate<byte> 8
    stream.Read(header, 0, 8) |> ignore
    Assert.Equal(0x89uy, header.[0])
    Assert.Equal(0x50uy, header.[1]) // 'P'
    Assert.Equal(0x4Euy, header.[2]) // 'N'
    Assert.Equal(0x47uy, header.[3]) // 'G'

[<Fact>]
let ``TrayCommand cases are distinct``() =
    let commands = [
        GuiCapture
        CaptureScreen 0
        LaunchWithDelay 3000
        OpenAbout
        OpenSettings
        OpenSaveFolder
        Exit
    ]
    Assert.Equal(7, commands |> List.distinct |> List.length)

[<Fact>]
let ``TrayMenuText default is Vietnamese``() =
    let text = TrayMenuText.defaultText
    Assert.Equal("Chụp màn hình (GUI)", text.CaptureGui)
    Assert.Equal("Chụp theo màn hình", text.CaptureScreen)
    Assert.Equal("Trình phóng nhanh...", text.Launcher)
    Assert.Equal("Thông tin & Phím tắt", text.About)
    Assert.Equal("Cài đặt", text.Settings)
    Assert.Equal("Mở thư mục ảnh chụp", text.OpenSaveFolder)
    Assert.Equal("Thoát F-Shot", text.Exit)
