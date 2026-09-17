namespace FShot.UI

open System
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml

open FShot.Core.Domain
open FShot.UI.Windows
open FShot.UI.Logging

/// Khởi tạo ứng dụng Avalonia và mở overlay chụp.
type App() =
    inherit Application()

    [<DefaultValue>]
    static val mutable private _captureRequest: CaptureRequest

    static member CaptureRequest
        with get() = App._captureRequest
        and set(value) = App._captureRequest <- value

    override this.Initialize() =
        AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        FShotLog.write "=== F-Shot UI started ==="

        AppDomain.CurrentDomain.UnhandledException.AddHandler(
            new UnhandledExceptionEventHandler(fun _ e ->
                match e.ExceptionObject with
                | :? exn as ex -> FShotLog.writeEx "Unhandled exception" ex
                | _ -> FShotLog.write (sprintf "Unhandled non-exception: %A" e.ExceptionObject)
            )
        )

        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            let overlay = CaptureOverlayWindow()
            desktop.MainWindow <- overlay

            overlay.Closed.Add(fun _ ->
                FShotLog.write "[App] Overlay closed - shutting down"
                desktop.Shutdown()
            )

            // Chụp Virtual Screen và hiển thị overlay trong async.
            async {
                try
                    FShotLog.write "ShowOverlayAsync starting"
                    let request = App.CaptureRequest
                    if request.DelayMs > 0 then
                        do! Async.Sleep request.DelayMs
                    do! overlay.ShowOverlayAsync()
                    FShotLog.write "ShowOverlayAsync completed"
                with ex ->
                    FShotLog.writeEx "ShowOverlayAsync failed" ex
            }
            |> Async.Start
        | _ ->
            FShotLog.write "Unknown application lifetime"

        base.OnFrameworkInitializationCompleted()
