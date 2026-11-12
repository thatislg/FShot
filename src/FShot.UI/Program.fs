namespace FShot.UI

open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices
open System.Threading
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Threading
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Platform.Win32.Capture
open FShot.Platform.Win32.Clipboard
open FShot.Platform.Win32.Config
open FShot.Platform.Win32.Lifecycle
open FShot.Platform.Win32.Screen
open FShot.Rendering.Skia.Renderers
open FShot.UI.Cli
open FShot.UI.Logging
open SkiaSharp

module Program =

    /// P/Invoke để gắn console của tiến trình cha (PowerShell/CMD) cho ứng dụng GUI SubSystem.
    module private NativeConsole =
        [<DllImport("kernel32.dll", SetLastError = true)>]
        extern bool AttachConsole(uint32 dwProcessId)

        [<DllImport("kernel32.dll", SetLastError = true)>]
        extern bool FreeConsole()

    let private ATTACH_PARENT_PROCESS = 0xFFFFFFFFu

    /// Gắn console của tiến trình cha và điều hướng lại stdout/stderr sang console (FR-WIN-004, FR-SYS-021).
    /// Ứng dụng GUI SubSystem trên Windows mặc định không in text ra PowerShell/CMD; hàm này khắc phục.
    let private tryAttachConsole () : bool =
        try
            if NativeConsole.AttachConsole(ATTACH_PARENT_PROCESS) then
                let stdout = Console.OpenStandardOutput()
                let outWriter = new StreamWriter(stdout, Text.Encoding.UTF8)
                outWriter.AutoFlush <- true
                Console.SetOut(outWriter)

                let stderr = Console.OpenStandardError()
                let errWriter = new StreamWriter(stderr, Text.Encoding.UTF8)
                errWriter.AutoFlush <- true
                Console.SetError(errWriter)
                true
            else
                false
        with _ ->
            false

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

    let runDaemon () : int =
        let app = buildAvaloniaApp()
        App.IsDaemon <- true
        FShotLog.write (sprintf "[Program] Starting daemon mode on Thread ID=%d, Apartment=%A" 
            Threading.Thread.CurrentThread.ManagedThreadId 
            (Threading.Thread.CurrentThread.GetApartmentState()))
        app.StartWithClassicDesktopLifetime([||])

    let runGui (request: ParsedCliRequest) : int =
        let app = buildAvaloniaApp()
        App.OpenSettingsOnStartup <- request.OpenSettings
        App.CaptureRequest <-
            {
              CaptureRequest.Default with
                  Mode = request.Mode
                  DelayMs = request.DelayMs
                  OutputTarget = request.OutputTarget
            }
        FShotLog.write (sprintf "[Program] Starting GUI mode on Thread ID=%d, Apartment=%A" 
            Threading.Thread.CurrentThread.ManagedThreadId 
            (Threading.Thread.CurrentThread.GetApartmentState()))
        app.StartWithClassicDesktopLifetime([||])

    /// Chụp màn hình theo CaptureMode và độ trễ, sau đó xuất ra OutputTarget.
    /// Trả về exit code 0 nếu thành công, 1 nếu thất bại.
    let runHeadlessCaptureAsync (request: ParsedCliRequest) : Async<int> =
        async {
            try
                if request.DelayMs > 0 then
                    do! Async.Sleep request.DelayMs

                let captureService = CompositeCaptureService() :> ICaptureService
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

                        | OutputTarget.Stdout ->
                            use data = exportBitmap.Encode(SKEncodedImageFormat.Png, 100)
                            let bytes = data.ToArray()
                            use stdout = Console.OpenStandardOutput()
                            stdout.Write(bytes, 0, bytes.Length)
                            stdout.Flush()
                            FShotLog.write (sprintf "Headless raw PNG to stdout: %d bytes" bytes.Length)
                            0

                        | OutputTarget.PrintGeometry ->
                            let g = sprintf "%dx%d+%.0f+%.0f"
                                        (int virtualBounds.Width)
                                        (int virtualBounds.Height)
                                        virtualBounds.X
                                        virtualBounds.Y
                            Console.Out.WriteLine(g)
                            FShotLog.write (sprintf "Headless print geometry: %s" g)
                            0

                        | OutputTarget.OpenGui ->
                            FShotLog.write "Headless mode requires -p or -c. Falling back to GUI."
                            // Không thể gọi return! trong expression, nên chạy GUI ở ngoài.
                            -1

                    if exitCode = -1 then
                        return runGui { request with OutputTarget = OutputTarget.OpenGui }
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

    /// Thực thi hành động cấu hình headless từ subcommand `config` (P2.43) và trả về exit code.
    let runConfigAction (action: ConfigAction) : int =
        match action with
        | ConfigAction.OpenSaveDir ->
            let cfg = ConfigStore.loadSnapshot()
            match cfg.SaveOptions.Path with
            | Some path ->
                try
                    if not (Directory.Exists path) then Directory.CreateDirectory(path) |> ignore
                    let psi = ProcessStartInfo("explorer.exe", path)
                    psi.UseShellExecute <- true
                    Process.Start(psi) |> ignore
                    printfn "Đã mở thư mục lưu ảnh: %s" path
                    0
                with ex ->
                    printfn "Lỗi khi mở thư mục: %s" ex.Message
                    1
            | None ->
                printfn "Chưa cấu hình thư mục lưu ảnh."
                1
        | ConfigAction.ExportConfig path ->
            match ConfigStore.exportConfigTo path with
            | Ok () ->
                printfn "Đã xuất cấu hình ra %s" path
                0
            | Error err ->
                printfn "%s" err
                1
        | ConfigAction.ImportConfig path ->
            match ConfigStore.importConfigFrom path with
            | Ok () ->
                printfn "Đã nhập cấu hình từ %s" path
                0
            | Error err ->
                printfn "%s" err
                1
        | ConfigAction.ResetConfig ->
            match ConfigStore.resetConfig() with
            | Ok () ->
                printfn "Đã khôi phục cấu hình mặc định."
                0
            | Error err ->
                printfn "%s" err
                1

    [<EntryPoint; STAThread>]
    let main argv =
        // Gắn console cha để in --help/--version/--raw/--print-geometry ra PowerShell/CMD (FR-WIN-004).
        tryAttachConsole() |> ignore

        let appConfig = ConfigStore.loadConfig()
        let config = appConfig.ToSnapshot()
        App.ConfigSnapshot <- config
        FShotLog.write (sprintf "Main entry: Config loaded: Tool=%A, Color=%s, Thickness=%.1f, SavePath=%A, Close=%b, Startup=%b, ShowDesktopNotification=%b, ShowAbortNotification=%b, DisabledTrayIcon=%b"
            config.DefaultTool
            (config.DefaultColor.ToHex())
            config.DefaultStrokeWidth.Value
            config.SaveOptions.Path
            config.CloseAfterExport
            appConfig.StartupLaunch
            appConfig.ShowDesktopNotification
            appConfig.ShowAbortNotification
            appConfig.DisabledTrayIcon)
        let request = CliParser.parse argv

        // Ép backend chụp qua cờ CLI (FR-WIN-001, P2.09): --force-gdi hoặc --backend gdi/wgc.
        let forceGdi =
            argv |> Array.exists (fun a -> a.Equals("--force-gdi", StringComparison.OrdinalIgnoreCase))
        if forceGdi then
            CompositeCaptureService.ForceGdi <- true
        else
            match argv |> Array.tryFindIndex (fun a -> a.Equals("--backend", StringComparison.OrdinalIgnoreCase)) with
            | Some i when i + 1 < argv.Length ->
                let v = argv[i + 1].ToLowerInvariant()
                if v = "gdi" then CompositeCaptureService.ForceGdi <- true
                elif v = "wgc" || v = "auto" then CompositeCaptureService.ForceGdi <- false
            | _ -> ()

        // Nhập cấu hình Flameshot (headless) — FR-WIN-006, FR-SYS-013.
        match request.ImportFlameshotPath with
        | Some path ->
            match FlameshotConfigMigrator.migrateFromFile path with
            | Some cfg ->
                ConfigStore.saveConfig cfg
                FShotLog.write (sprintf "[Config] Imported Flameshot config from %s" path)
                printfn "Đã nhập cấu hình Flameshot từ %s" path
            | None ->
                FShotLog.write (sprintf "[Config] Flameshot config not found at %s" path)
                printfn "Không tìm thấy file flameshot.ini tại %s" path
            System.Environment.Exit(0)
        | None -> ()

        // Hành động cấu hình headless (P2.43): --export / --import / --reset / --open-dir.
        match request.ConfigAction with
        | Some action -> System.Environment.Exit(runConfigAction action)
        | None -> ()

        use cts = new CancellationTokenSource()
        let instanceResult = SingleInstance.enforce argv request.AllowMultiple cts.Token App.HandleIpcCommand

        match instanceResult with
        | SingleInstanceCheckResult.FirstInstance mutex ->
                FShotLog.write "[Program] First instance: continuing"
                App.SingleInstanceMutex <- mutex
                App.IpcCancellation <- cts

                let exitCode =
                    if request.RunAsDaemon then
                        runDaemon ()
                    elif request.Mode = GuiInteractive then
                        runGui request
                    else
                        runHeadlessCaptureAsync request |> Async.RunSynchronously
                try cts.Cancel() with _ -> ()
                exitCode
        | SingleInstanceCheckResult.SecondaryInstance ->
            FShotLog.write "[Program] Secondary instance: command forwarded; exiting"
            0
        | SingleInstanceCheckResult.MultipleAllowed ->
            FShotLog.write "[Program] Multiple instances allowed"
            let exitCode =
                if request.RunAsDaemon then
                    runDaemon ()
                elif request.Mode = GuiInteractive then
                    runGui request
                else
                    runHeadlessCaptureAsync request |> Async.RunSynchronously
            try cts.Cancel() with _ -> ()
            exitCode
