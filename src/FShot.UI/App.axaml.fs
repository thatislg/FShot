namespace FShot.UI

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml

open FShot.UI.Windows

/// Khởi tạo ứng dụng Avalonia và mở overlay chụp.
type App() =
    inherit Application()

    override this.Initialize() =
        AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            let overlay = CaptureOverlayWindow()
            desktop.MainWindow <- overlay

            // Chụp Virtual Screen và hiển thị overlay.
            async {
                do! overlay.ShowOverlayAsync()
            }
            |> Async.Start
        | _ -> ()

        base.OnFrameworkInitializationCompleted()
