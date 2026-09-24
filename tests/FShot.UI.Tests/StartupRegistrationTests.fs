module FShot.UI.Tests.StartupRegistrationTests

open System
open Microsoft.Win32
open FShot.Platform.Win32.Startup
open Xunit

[<Fact>]
let ``buildStartupCommand quotes executable path``() =
    let cmd = StartupRegistration.buildStartupCommand "C:\\Apps\\FShot.exe"
    Assert.Equal("\"C:\\Apps\\FShot.exe\"", cmd)

[<Fact>]
let ``buildStartupCommand returns empty for empty path``() =
    let cmd = StartupRegistration.buildStartupCommand ""
    Assert.Equal("", cmd)

[<Fact>]
let ``setStartup and isStartupEnabled toggle registry value``() =
    // Xóa giá trị cũ nếu có.
    StartupRegistration.setStartup false |> ignore
    Assert.False(StartupRegistration.isStartupEnabled())

    let enableResult = StartupRegistration.setStartup true
    Assert.Equal(Ok (), enableResult)
    Assert.True(StartupRegistration.isStartupEnabled())

    use key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run")
    let value = key.GetValue("FShot") :?> string
    Assert.False(String.IsNullOrWhiteSpace value)
    Assert.Contains("FShot", value, StringComparison.OrdinalIgnoreCase)

    let disableResult = StartupRegistration.setStartup false
    Assert.Equal(Ok (), disableResult)
    Assert.False(StartupRegistration.isStartupEnabled())
