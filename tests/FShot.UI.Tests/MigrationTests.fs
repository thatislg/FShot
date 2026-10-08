module FShot.UI.Tests.MigrationTests

open System
open FShot.Core.Domain
open FShot.Platform.Win32.Config
open Xunit

/// Kiểm tra FlameshotIniParser phân tích section, key-value, comment và mảng.
[<Fact>]
let ``FlameshotIniParser parse dung section keyvalue comment va mang`` () =
    let content = """
; Chú thích dòng 1
# Chú thích dòng 2

[General]
savePath=C:/Screenshots
filenamePattern=flameshot_%Y%m%d
jpegQuality=85
userColors=#ff0000, #00ff00, #0000ff

[Shortcuts]
TYPE_DRAWER=Q

"""
    let entries = FlameshotIniParser.parse content

    Assert.Equal(Some "C:/Screenshots", FlameshotIniParser.tryFind entries "General" "savePath")
    Assert.Equal(Some "flameshot_%Y%m%d", FlameshotIniParser.tryFind entries "General" "filenamePattern")
    Assert.Equal(Some 85, FlameshotIniParser.tryFindInt entries "General" "jpegQuality")
    Assert.Equal(Some "Q", FlameshotIniParser.tryFind entries "Shortcuts" "TYPE_DRAWER")
    Assert.Equal<string list>([ "#ff0000"; "#00ff00"; "#0000ff" ], FlameshotIniParser.tryFindList entries "General" "userColors")

/// Kiểm tra chuẩn hóa màu từ nhiều định dạng.
[<Theory>]
[<InlineData("#FF0000", "#FF0000")>]
[<InlineData("#ff0000", "#FF0000")>]
[<InlineData("rgb(255, 0, 0)", "#FF0000")>]
[<InlineData("rgba(0, 255, 0, 128)", "#00FF00")>]
[<InlineData("#AA112233", "#112233AA")>]
[<InlineData("not-a-color", "#FFFFFF")>]
let ``FlameshotConfigMigrator normalizeColor chuan hoa dung`` (input: string, expected: string) =
    let result = FlameshotConfigMigrator.normalizeColor input "#FFFFFF"
    Assert.Equal(expected, result)

/// Kiểm tra ánh xạ danh sách ID nút công cụ.
[<Fact>]
let ``FlameshotConfigMigrator mapButtonIds anh xa dung`` () =
    let buttons = [ "0"; "1"; "2"; "8"; "9"; "10"; "99" ]
    let result = FlameshotConfigMigrator.mapButtonIds buttons
    Assert.Equal<string list>(
        [ "SelectionTool"; "PencilTool"; "ArrowTool"; "PixelateTool" ],
        result)

/// Kiểm tra migrate toàn bộ file INI mẫu thực tế.
[<Fact>]
let ``FlameshotConfigMigrator migrate tu file mau giu dung cac cai dat`` () =
    let content = """
[General]
savePath=D:/MyShots/
savePathFixed=true
filenamePattern=shot_%Y-%m-%d_%H%M%S
saveAsFileExtension=.jpg
jpegQuality=78
drawColor=rgb(0, 128, 255)
drawThickness=4
drawFontSize=20
contrastOpacity=200
uiColor=#38bdf8
contrastUiColor=#0f172a
userColors=#ff0000, #00ff00, rgb(0,0,255)
buttons=0,1,3,4,5,6,7,8
startupLaunch=true
disabledTrayIcon=true
showDesktopNotification=false
showAbortNotification=true
copyOnDoubleClick=true
uiLanguage=vi_VN
"""
    let config = FlameshotConfigMigrator.migrateFromString content

    Assert.Equal("D:\\MyShots", config.SavePath)
    Assert.True(config.SavePathFixed)
    Assert.Equal("shot_%Y-%m-%d_%H%M%S", config.FilenamePattern)
    Assert.Equal("jpg", config.SaveAsFileExtension)
    Assert.Equal(78, config.JpegQuality)
    Assert.Equal("#0080FF", config.DrawColor)
    Assert.Equal(4.0, config.DrawThickness)
    Assert.Equal(20.0, config.DrawFontSize)
    Assert.Equal(200uy, config.ContrastOpacity)
    Assert.Equal("#38BDF8", config.UiColor)
    Assert.Equal("#0F172A", config.ContrastUiColor)
    Assert.Equal<string list>([ "#FF0000"; "#00FF00"; "#0000FF" ], config.UserColors)
    Assert.Equal<string list>(
        [ "SelectionTool"; "PencilTool"; "LineTool"; "RectangleTool"; "CircleTool"; "MarkerTool"; "TextTool"; "PixelateTool" ],
        config.Buttons)
    Assert.True(config.StartupLaunch)
    Assert.True(config.DisabledTrayIcon)
    Assert.False(config.ShowDesktopNotification)
    Assert.True(config.ShowAbortNotification)
    Assert.True(config.CopyOnDoubleClick)
    Assert.Equal("vi", config.UiLanguage)

/// Kiểm tra migrateFromFile trả về None khi file không tồn tại.
[<Fact>]
let ``FlameshotConfigMigrator migrateFromFile tra ve None khi file khong ton tai`` () =
    let result = FlameshotConfigMigrator.migrateFromFile "Z:\\khong_ton_tai\\flameshot.ini"
    Assert.True(result.IsNone)
