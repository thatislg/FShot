module FShot.UI.Tests.CliTests

open FShot.Core.Domain
open FShot.UI.Cli
open Xunit

[<Fact>]
let ``parse empty args defaults to daemon mode``() =
    let request = CliParser.parse [||]
    Assert.True(request.RunAsDaemon)
    Assert.Equal(GuiInteractive, request.Mode)
    Assert.Equal(0, request.DelayMs)
    Assert.Equal(OpenGui, request.OutputTarget)

[<Fact>]
let ``parse gui subcommand``() =
    let request = CliParser.parse [| "gui" |]
    Assert.False(request.RunAsDaemon)
    Assert.Equal(GuiInteractive, request.Mode)
    Assert.Equal(OpenGui, request.OutputTarget)

[<Fact>]
let ``parse gui with delay``() =
    let request = CliParser.parse [| "gui"; "-d"; "3000" |]
    Assert.Equal(GuiInteractive, request.Mode)
    Assert.Equal(3000, request.DelayMs)

[<Fact>]
let ``parse full with clipboard``() =
    let request = CliParser.parse [| "full"; "-c" |]
    Assert.Equal(FullScreen, request.Mode)
    Assert.Equal(OutputTarget.Clipboard, request.OutputTarget)

[<Fact>]
let ``parse full with path``() =
    let request = CliParser.parse [| "full"; "-p"; "C:\\temp\\shot.png" |]
    Assert.Equal(FullScreen, request.Mode)
    match request.OutputTarget with
    | File p -> Assert.Equal("C:\\temp\\shot.png", p)
    | _ -> Assert.Fail("Expected File output target")

[<Fact>]
let ``parse full with delay zero``() =
    let request = CliParser.parse [| "full"; "-d"; "0"; "-p"; "shot.png" |]
    Assert.Equal(FullScreen, request.Mode)
    Assert.Equal(0, request.DelayMs)

[<Fact>]
let ``parse gui with delay zero``() =
    let request = CliParser.parse [| "gui"; "-d"; "0" |]
    Assert.Equal(GuiInteractive, request.Mode)
    Assert.Equal(0, request.DelayMs)

[<Fact>]
let ``parse screen with delay zero``() =
    let request = CliParser.parse [| "screen"; "0"; "-d"; "0"; "-c" |]
    match request.Mode with
    | SingleScreen idx -> Assert.Equal(0, idx)
    | _ -> Assert.Fail("Expected SingleScreen mode")
    Assert.Equal(0, request.DelayMs)
    Assert.Equal(OutputTarget.Clipboard, request.OutputTarget)

[<Fact>]
let ``parse full with delay and path``() =
    let request = CliParser.parse [| "full"; "-d"; "1500"; "-p"; "shot.jpg" |]
    Assert.Equal(FullScreen, request.Mode)
    Assert.Equal(1500, request.DelayMs)
    match request.OutputTarget with
    | File p -> Assert.Equal("shot.jpg", p)
    | _ -> Assert.Fail("Expected File output target")

[<Fact>]
let ``parse screen with index and clipboard``() =
    let request = CliParser.parse [| "screen"; "1"; "-c" |]
    match request.Mode with
    | SingleScreen idx -> Assert.Equal(1, idx)
    | _ -> Assert.Fail("Expected SingleScreen mode")
    Assert.Equal(OutputTarget.Clipboard, request.OutputTarget)

[<Fact>]
let ``parse screen with index and path``() =
    let request = CliParser.parse [| "screen"; "0"; "-p"; "output.png" |]
    match request.Mode with
    | SingleScreen idx -> Assert.Equal(0, idx)
    | _ -> Assert.Fail("Expected SingleScreen mode")
    match request.OutputTarget with
    | File p -> Assert.Equal("output.png", p)
    | _ -> Assert.Fail("Expected File output target")

[<Fact>]
let ``negative delay is clamped to zero``() =
    let request = CliParser.parse [| "full"; "-d"; "-100"; "-c" |]
    Assert.Equal(0, request.DelayMs)
