// Shared headless test session dùng chung cho các test cấp UI trong FShot.UI.Tests.
// Chỉ được StartNew MỘT lần cho cả assembly để tránh xung đột giữa nhiều session.
//
// HeadlessApp áp dụng đúng theme như app thật (FluentTheme + ColorPicker theme),
// để các test kiểm tra đúng trạng thái render thực tế — đặc biệt là ColorView/ColorSpectrum.
module FShot.UI.Tests.HeadlessTestSession

open System
open Avalonia
open Avalonia.Headless
open Avalonia.Markup.Xaml.Styling
open Avalonia.Themes.Fluent

type HeadlessApp() as this =
    inherit Application()

    do
        this.Styles.Add(FluentTheme()) |> ignore

        let colorPicker = StyleInclude(Unchecked.defaultof<Uri>)
        colorPicker.Source <- Uri("avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml")
        this.Styles.Add(colorPicker) |> ignore

    static member BuildAvaloniaApp() : AppBuilder =
        AppBuilder.Configure<HeadlessApp>().UseSkia()

let session =
    lazy (HeadlessUnitTestSession.StartNew(typeof<HeadlessApp>, AvaloniaTestIsolationLevel.PerAssembly))
