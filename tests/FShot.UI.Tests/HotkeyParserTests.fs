module FShot.UI.Tests.HotkeyParserTests

open FShot.Platform.Win32.Hotkeys
open Xunit

[<Fact>]
let ``parse bare PrintScreen`` () =
    match HotkeyParser.parse "PrintScreen" with
    | Ok parsed ->
        Assert.Equal(HotkeyModifiers.None, parsed.Modifiers)
        Assert.Equal(0x2Cu, parsed.VirtualKey)
    | Error err -> Assert.True(false, sprintf "Unexpected error: %s" err)

[<Fact>]
let ``parse Win+Shift+X`` () =
    match HotkeyParser.parse "Win+Shift+X" with
    | Ok parsed ->
        Assert.Equal(HotkeyModifiers.Win ||| HotkeyModifiers.Shift, parsed.Modifiers)
        Assert.Equal(0x58u, parsed.VirtualKey) // 'X'
    | Error err -> Assert.True(false, sprintf "Unexpected error: %s" err)

[<Fact>]
let ``parse Ctrl+Alt+S`` () =
    match HotkeyParser.parse "Ctrl+Alt+S" with
    | Ok parsed ->
        Assert.Equal(HotkeyModifiers.Control ||| HotkeyModifiers.Alt, parsed.Modifiers)
        Assert.Equal(0x53u, parsed.VirtualKey) // 'S'
    | Error err -> Assert.True(false, sprintf "Unexpected error: %s" err)

[<Fact>]
let ``parse F11 function key`` () =
    match HotkeyParser.parse "F11" with
    | Ok parsed ->
        Assert.Equal(HotkeyModifiers.None, parsed.Modifiers)
        Assert.Equal(0x7Au, parsed.VirtualKey) // 0x70 + 10
    | Error err -> Assert.True(false, sprintf "Unexpected error: %s" err)

[<Fact>]
let ``parse is case insensitive and trims whitespace`` () =
    match HotkeyParser.parse "  ctrl + shift + p  " with
    | Ok parsed ->
        Assert.Equal(HotkeyModifiers.Control ||| HotkeyModifiers.Shift, parsed.Modifiers)
        Assert.Equal(0x50u, parsed.VirtualKey) // 'P'
    | Error err -> Assert.True(false, sprintf "Unexpected error: %s" err)

[<Fact>]
let ``parse empty returns error`` () =
    match HotkeyParser.parse "" with
    | Error _ -> Assert.True(true)
    | Ok _ -> Assert.True(false, "Empty input should fail")

[<Fact>]
let ``parse unknown key returns error`` () =
    match HotkeyParser.parse "Win+FakeKey" with
    | Error _ -> Assert.True(true)
    | Ok _ -> Assert.True(false, "Unknown key should fail")

[<Fact>]
let ``format normalizes modifier order`` () =
    let modifiers = HotkeyModifiers.Shift ||| HotkeyModifiers.Win
    let formatted = HotkeyParser.format modifiers 0x58u // X
    Assert.Equal("Win+Shift+X", formatted)

[<Fact>]
let ``format PrintScreen`` () =
    Assert.Equal("PrintScreen", HotkeyParser.format HotkeyModifiers.None 0x2Cu)

[<Fact>]
let ``isDangerous detects Ctrl+Alt+Delete`` () =
    Assert.True(HotkeyParser.isDangerous "Ctrl+Alt+Delete")
    Assert.True(HotkeyParser.isDangerous "Win+L")
    Assert.True(HotkeyParser.isDangerous "Alt+Tab")

[<Fact>]
let ``isDangerous false for normal hotkeys`` () =
    Assert.False(HotkeyParser.isDangerous "PrintScreen")
    Assert.False(HotkeyParser.isDangerous "Win+Shift+X")
    Assert.False(HotkeyParser.isDangerous "Ctrl+Alt+S")
