namespace FShot.UI

open System
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml

open FShot.Core.Domain
open FShot.Platform.Win32.Config
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

    [<DefaultValue>]
    static val mutable private _configSnapshot: ConfigSnapshot

    static member ConfigSnapshot
        with get() =
            if box App._configSnapshot = null then
                let cfg = ConfigStore.loadSnapshot()
                App._configSnapshot <- cfg
                cfg
            else
                App._configSnapshot
        and set(value) = App._configSnapshot <- value

    override this.Initialize() =
        AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        FShotLog.write "=== F-Shot UI started ==="

        // Nạp cấu hình từ %APPDATA%\FShot\config.json
        let configSnapshot = ConfigStore.loadSnapshot()
        App.ConfigSnapshot <- configSnapshot
        FShotLog.write (sprintf "Config loaded: Tool=%A, Color=%s, Thickness=%.1f, SavePath=%A"
            configSnapshot.DefaultTool
            (configSnapshot.DefaultColor.ToHex())
            configSnapshot.DefaultStrokeWidth.Value
            configSnapshot.SaveOptions.Path)

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
            overlay.ConfigSnapshot <- App.ConfigSnapshot
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
