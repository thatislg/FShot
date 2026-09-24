namespace FShot.UI

open System
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Threading
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Platform.Win32.Capture
open FShot.Platform.Win32.Clipboard
open FShot.Platform.Win32.Config
open FShot.Platform.Win32.Screen
open FShot.Rendering.Skia.Renderers
open FShot.UI.Cli
open FShot.UI.Logging
open SkiaSharp

module Program =

    [<CompiledName "BuildAvaloniaApp">]
    let buildAvaloniaApp() =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace(areas = Array.empty)

    /// Chụp màn hình theo CaptureMode và độ trễ, sau đó xuất ra OutputTarget.
    /// Trả về exit code 0 nếu thành công, 1 nếu thất bại.
    let rec runHeadlessCaptureAsync (request: ParsedCliRequest) : Async<int> =
        async {
            try
                if request.DelayMs > 0 then
                    do! Async.Sleep request.DelayMs

                let captureService = WindowsCaptureService() :> ICaptureService
                let! captureResultOpt =
                    async {
                        let! res =
                            match request.Mode with
                            | FullScreen -> captureService.CaptureCursorScreenAsync()
                            | SingleScreen index -> captureService.CaptureScreenAsync index
                            | _ -> captureService.CaptureCursorScreenAsync()
                        match res with
                        | Ok r -> return Ok r
                        | Error err ->
                            FShotLog.write (sprintf "WindowsCaptureService failed (%A), falling back to StubCaptureService" err)
                            let stubService = StubCaptureService() :> ICaptureService
                            return! stubService.CaptureVirtualScreenAsync()
                    }

                match captureResultOpt with
                | Ok captureResult ->
                    let virtualBounds = captureResult.VirtualBounds
                    let selection =
                        {
                          Selection.Empty with
                              State = Selected
                              Bounds =
                                {
                                  X = virtualBounds.X
                                  Y = virtualBounds.Y
                                  Width = virtualBounds.Width
                                  Height = virtualBounds.Height
                                }
                        }

                    use exportBitmap = SceneComposer.renderExport captureResult selection []

                    let exitCode =
                        match request.OutputTarget with
                        | OutputTarget.File path ->
                            let format =
                                if path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                   path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) then
                                    SKEncodedImageFormat.Jpeg
                                else
                                    SKEncodedImageFormat.Png

                            let config = ConfigStore.loadSnapshot()
                            let quality = config.SaveOptions.NormalizedJpegQuality
                            use data = exportBitmap.Encode(format, quality)
                            use stream = System.IO.File.Create(path)
                            data.SaveTo(stream)
                            stream.Flush()
                            FShotLog.write (sprintf "Headless save succeeded: %s" path)
                            printfn "Saved to %s" path
                            0

                        | OutputTarget.Clipboard ->
                            use data = exportBitmap.Encode(SKEncodedImageFormat.Png, 100)
                            let pngBytes = data.ToArray()
                            let pixelBytes = Array.zeroCreate<byte> (exportBitmap.Width * exportBitmap.Height * 4)
                            let ptr = exportBitmap.GetPixels()
                            if ptr <> IntPtr.Zero then
                                Runtime.InteropServices.Marshal.Copy(ptr, pixelBytes, 0, pixelBytes.Length)
                            let ok = ClipboardService.copyImageToClipboard exportBitmap.Width exportBitmap.Height pngBytes pixelBytes
                            ClipboardService.playNotificationSound()
                            FShotLog.write (sprintf "Headless clipboard copy result: %b" ok)
                            printfn "Copied to clipboard"
                            0

                        | OutputTarget.OpenGui ->
                            FShotLog.write "Headless mode requires -p or -c. Falling back to GUI."
                            // Không thể gọi return! trong expression, nên chạy GUI ở ngoài.
                            -1

                        | _ ->
                            FShotLog.write (sprintf "Unsupported headless output target: %A" request.OutputTarget)
                            1

                    if exitCode = -1 then
                        return! runGuiAsync { request with OutputTarget = OutputTarget.OpenGui }
                    else
                        return exitCode

                | Error err ->
                    FShotLog.write (sprintf "Headless capture failed: %A" err)
                    printfn "Capture failed: %A" err
                    return 1
            with ex ->
                FShotLog.writeEx "Headless capture failed" ex
                printfn "Error: %s" ex.Message
                return 1
        }

    and runDaemonAsync () : Async<int> =
        async {
            let app = buildAvaloniaApp()
            App.IsDaemon <- true
            FShotLog.write "[Program] Starting daemon mode (tray only)"
            return app.StartWithClassicDesktopLifetime([||])
        }

    and runGuiAsync (request: ParsedCliRequest) : Async<int> =
        async {
            let app = buildAvaloniaApp()
            // Truyền request vào App thông qua static field đơn giản.
            App.CaptureRequest <-
                {
                  CaptureRequest.Default with
                      Mode = request.Mode
                      DelayMs = request.DelayMs
                      OutputTarget = request.OutputTarget
                }
            return app.StartWithClassicDesktopLifetime([||])
        }

    [<EntryPoint; STAThread>]
    let main argv =
        let config = ConfigStore.loadSnapshot()
        App.ConfigSnapshot <- config
        FShotLog.write (sprintf "Main entry: Config loaded: Tool=%A, Color=%s, Thickness=%.1f, SavePath=%A, Close=%b"
            config.DefaultTool
            (config.DefaultColor.ToHex())
            config.DefaultStrokeWidth.Value
            config.SaveOptions.Path
            config.CloseAfterExport)
        let request = CliParser.parse argv

        let exitCode =
            if request.RunAsDaemon then
                runDaemonAsync () |> Async.RunSynchronously
            elif request.Mode = GuiInteractive then
                runGuiAsync request |> Async.RunSynchronously
            else
                runHeadlessCaptureAsync request |> Async.RunSynchronously

        exitCode
