namespace FShot.UI.Windows

open Avalonia
open Avalonia.Controls
open Avalonia.Interactivity
open Avalonia.Markup.Xaml

/// Cửa sổ "Thông tin & Phím tắt" hiển thị phiên bản F-Shot và cheat sheet phím tắt.
/// Xem tài liệu 10_05_TrayIcon.md, mục 4.
type AboutWindow() as this =
    inherit Window()

    do
        this.InitializeComponent()
        this.ConfigureWindow()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

    member private this.ConfigureWindow() =
        this.WindowState <- WindowState.Normal
        this.ShowInTaskbar <- false
        this.Topmost <- true

    member private this.OnCloseClick(_: obj, _: RoutedEventArgs) =
        this.Close()
