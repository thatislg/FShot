module FShot.UI.Tests.GlobalHotkeyTests

open FShot.Platform.Win32.Hotkeys
open Xunit

[<Fact>]
let ``SnippingTool redirect toggles registry value``() =
    // Lưu trạng thái hiện tại để khôi phục sau test.
    let original = SnippingTool.isPrintScreenRedirectEnabled()

    let disable = SnippingTool.setPrintScreenRedirect false
    Assert.Equal(Ok (), disable)
    Assert.False(SnippingTool.isPrintScreenRedirectEnabled())

    let enable = SnippingTool.setPrintScreenRedirect true
    Assert.Equal(Ok (), enable)
    Assert.True(SnippingTool.isPrintScreenRedirectEnabled())

    // Khôi phục trạng thái ban đầu.
    SnippingTool.setPrintScreenRedirect original |> ignore

[<Fact>]
let ``HotkeyModifiers flags have distinct bit values``() =
    Assert.Equal(0x0001, int HotkeyModifiers.Alt)
    Assert.Equal(0x0002, int HotkeyModifiers.Control)
    Assert.Equal(0x0004, int HotkeyModifiers.Shift)
    Assert.Equal(0x0008, int HotkeyModifiers.Win)
    Assert.Equal(0x4000, int HotkeyModifiers.NoRepeat)

[<Fact>]
let ``VirtualKeyCodes snapshot maps to PrintScreen``() =
    // VK_SNAPSHOT (0x2C) là mã phím ảo của PrintScreen.
    Assert.Equal(0x2C, int VirtualKeyCodes.VK_SNAPSHOT)
