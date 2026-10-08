module FShot.UI.Tests.CaptureBackendTests

open FShot.Platform.Win32.Capture
open Xunit

/// Kiểm tra phân giải chuỗi cấu hình CaptureBackend.
[<Theory>]
[<InlineData("Auto", "Auto")>]
[<InlineData("auto", "Auto")>]
[<InlineData("Wgc", "Wgc")>]
[<InlineData("wgc", "Wgc")>]
[<InlineData("Gdi", "Gdi")>]
[<InlineData("gdi", "Gdi")>]
[<InlineData("", "Auto")>]
[<InlineData("invalid", "Auto")>]
let ``CaptureBackendChoice FromString phân giải đúng`` (input: string, expected: string) =
    let choice = CaptureBackendChoice.FromString input
    Assert.Equal(expected, string choice)
