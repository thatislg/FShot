namespace FShot.UI

open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices
open System.Threading
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Avalonia.Threading

open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Platform.Win32.Capture
open FShot.Platform.Win32.Clipboard
open FShot.Platform.Win32.Config
open FShot.Platform.Win32.Lifecycle
open FShot.Platform.Win32.Notifications
open FShot.Platform.Win32.Screen
open FShot.Platform.Win32.Startup
open FShot.Platform.Win32.Tray
open FShot.Rendering.Skia.Renderers
open FShot.UI.Cli
open FShot.UI.SkiaCanvas
open FShot.UI.Windows
open FShot.UI.Logging
open SkiaSharp

/// Khởi tạo ứng dụng Avalonia, quản lý overlay chụp và tray icon.
type App() as this =
    inherit Application()

    [<DefaultValue>]
    static val mutable private _captureRequest: CaptureRequest

    static member CaptureRequest
        with get() = App._captureRequest
        and set(value) = App._captureRequest <- value

    [<DefaultValue>]
    static val mutable private _isDaemon: bool

    static member IsDaemon
        with get() = App._isDaemon
        and set(value) = App._isDaemon <- value

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

    [<DefaultValue>]
    static val mutable private _singleInstanceMutex: Mutex

    static member SingleInstanceMutex
        with get() = App._singleInstanceMutex
        and set(value) = App._singleInstanceMutex <- value

    [<DefaultValue>]
    static val mutable private _ipcCancellation: CancellationTokenSource

    static member IpcCancellation
        with get() = App._ipcCancellation
        and set(value) = App._ipcCancellation <- value

    let mutable currentOverlay: CaptureOverlayWindow option = None
    let mutable trayService: TrayIconService option = None
    let mutable desktopLifetime: IClassicDesktopStyleApplicationLifetime option = None
    let mutable notificationService: NotificationService option = None

    /// Giải phóng tài nguyên tập trung khi thoát ứng dụng.
    member private this.DisposeResources() =
        FShotLog.write "[AppLifecycle] Disposing resources"
        trayService |> Option.iter (fun t -> t.Dispose())
        trayService <- None
        notificationService |> Option.iter (fun n -> n.Dispose())
        notificationService <- None
        match App.IpcCancellation with
        | null -> ()
        | cts ->
            try cts.Cancel() with _ -> ()
            try cts.Dispose() with _ -> ()
            App.IpcCancellation <- null
        match App.SingleInstanceMutex with
        | null -> ()
        | m ->
            try m.ReleaseMutex() with _ -> ()
            try m.Dispose() with _ -> ()
            App.SingleInstanceMutex <- null

    /// Mở file cấu hình bằng ứng dụng mặc định của hệ thống (FR-SYS-006).
    let openSettingsFile () =
        try
            let path = ConfigStore.defaultConfigFile
            let psi = ProcessStartInfo(path)
            psi.UseShellExecute <- true
            Process.Start(psi) |> ignore
            FShotLog.write (sprintf "[Tray] Opened settings file: %s" path)
        with ex ->
            FShotLog.writeEx "[Tray] Open settings failed" ex

    /// Mở thư mục lưu ảnh chụp trong Windows Explorer (FR-SYS-007).
    let openSaveFolder () =
        let config = ConfigStore.loadSnapshot()
        match config.SaveOptions.Path with
        | Some path when Directory.Exists path ->
            try
                let psi = ProcessStartInfo("explorer.exe", path)
                psi.UseShellExecute <- true
                Process.Start(psi) |> ignore
                FShotLog.write (sprintf "[Tray] Opened save folder: %s" path)
            with ex ->
                FShotLog.writeEx "[Tray] Open save folder failed" ex
        | Some path ->
            FShotLog.write (sprintf "[Tray] Save path does not exist: %s" path)
        | None ->
            FShotLog.write "[Tray] No save path configured"

    /// Hiển thị cửa sổ Thông tin & Phím tắt (FR-SYS-005).
    let showAboutWindow () =
        Dispatcher.UIThread.InvokeAsync(fun () ->
            try
                let about = AboutWindow()
                about.Show()
            with ex ->
                FShotLog.writeEx "[Tray] Show about window failed" ex
        ) |> ignore

    /// Tạo và hiển thị overlay chụp màn hình.
    let showCaptureOverlay (desktop: IClassicDesktopStyleApplicationLifetime) (request: CaptureRequest) =
        Dispatcher.UIThread.InvokeAsync(fun () ->
            try
                // Nếu overlay đã hiển thị thì chỉ focus lại.
                match currentOverlay with
                | Some overlay when overlay.IsVisible ->
                    overlay.Focus() |> ignore
                    overlay.Activate() |> ignore
                    FShotLog.write "[Tray] Existing overlay focused"
                | _ ->
                    let overlay = CaptureOverlayWindow()
                    overlay.ConfigSnapshot <- App.ConfigSnapshot
                    desktop.MainWindow <- overlay
                    currentOverlay <- Some overlay

                    overlay.Closed.Add(fun _ ->
                        FShotLog.write "[App] Overlay closed"
                        currentOverlay <- None
                        if App.IsDaemon then
                            desktop.MainWindow <- null
                            FShotLog.write "[App] Returning to daemon mode"
                        else
                            desktop.Shutdown()
                    )

                    // Subscribe abort event từ CaptureCanvas để hiển thị thông báo hủy nếu cấu hình bật.
                    CaptureCanvasEvents.abortRequested.Publish.Add(fun () ->
                        if App.IsDaemon then
                            this.ShowAbortNotification())

                    overlay.Show()
                    overlay.Focus() |> ignore
                    overlay.Activate() |> ignore

                    async {
                        try
                            if request.DelayMs > 0 then
                                do! Async.Sleep request.DelayMs
                            do! overlay.ShowOverlayAsync()
                            FShotLog.write "[App] Overlay capture completed"
                        with ex ->
                            FShotLog.writeEx "[App] Overlay capture failed" ex
                    }
                    |> Async.Start

                    FShotLog.write "[App] Capture overlay created"
            with ex ->
                FShotLog.writeEx "[Tray] Show capture overlay failed" ex
        ) |> ignore

    /// Chụp headless một màn hình cụ thể từ tray menu (FR-SYS-003).
    /// Nếu có `savePath` thì lưu file, ngược lại copy vào clipboard.
    let captureScreenHeadless (screenIndex: int) =
        async {
            try
                let config = ConfigStore.loadSnapshot()
                let captureService = WindowsCaptureService() :> ICaptureService
                let! result = captureService.CaptureScreenAsync screenIndex
                match result with
                | Ok captureResult ->
                    let selection =
                        { Selection.Empty with
                            State = Selected
                            Bounds = captureResult.VirtualBounds }
                    use exportBitmap = SceneComposer.renderExport captureResult selection []
                    let format =
                        match config.SaveOptions.Format with
                        | Png -> SKEncodedImageFormat.Png
                        | Jpg -> SKEncodedImageFormat.Jpeg
                    let quality = config.SaveOptions.NormalizedJpegQuality

                    match config.SaveOptions.Path with
                    | Some savePath ->
                        if not (Directory.Exists savePath) then
                            Directory.CreateDirectory(savePath) |> ignore
                        let fileName = config.SaveOptions.ResolveFileName(DateTime.Now)
                        let fullPath = Path.Combine(savePath, fileName)
                        use data = exportBitmap.Encode(format, quality)
                        use stream = File.Create(fullPath)
                        data.SaveTo(stream)
                        stream.Flush()
                        FShotLog.write (sprintf "[Tray] Screen %d saved to %s" screenIndex fullPath)
                        notificationService |> Option.iter (fun n ->
                            n.ShowNotification(CaptureSuccess (Some fullPath), config.ShowDesktopNotification) |> ignore)
                    | None ->
                        use data = exportBitmap.Encode(SKEncodedImageFormat.Png, 100)
                        let pngBytes = data.ToArray()
                        let pixelBytes = Array.zeroCreate<byte> (exportBitmap.Width * exportBitmap.Height * 4)
                        let ptr = exportBitmap.GetPixels()
                        if ptr <> IntPtr.Zero then
                            Marshal.Copy(ptr, pixelBytes, 0, pixelBytes.Length)
                        let ok = ClipboardService.copyImageToClipboard exportBitmap.Width exportBitmap.Height pngBytes pixelBytes
                        FShotLog.write (sprintf "[Tray] Screen %d copied to clipboard: %b" screenIndex ok)
                        notificationService |> Option.iter (fun n ->
                            n.ShowNotification(CopySuccess, config.ShowDesktopNotification) |> ignore)
                | Error err ->
                    FShotLog.write (sprintf "[Tray] Screen %d capture failed: %A" screenIndex err)
            with ex ->
                FShotLog.writeEx "[Tray] Screen capture failed" ex
        } |> Async.Start

    /// Hiển thị thông báo hủy thao tác chụp (FR-CFG-008).
    member this.ShowAbortNotification () =
        let config = ConfigStore.loadSnapshot()
        notificationService |> Option.iter (fun n ->
            n.ShowNotification(CaptureAborted, config.ShowAbortNotification) |> ignore)

    /// Dispatcher cho các lệnh phát sinh từ tray.
    member private this.DispatchTrayCommand (desktop: IClassicDesktopStyleApplicationLifetime) (cmd: TrayCommand) =
        match cmd with
        | GuiCapture ->
            FShotLog.write "[Tray] GuiCapture requested"
            showCaptureOverlay desktop { CaptureRequest.Default with Mode = GuiInteractive }
        | CaptureScreen index ->
            FShotLog.write (sprintf "[Tray] CaptureScreen %d requested" index)
            captureScreenHeadless index
        | LaunchWithDelay delayMs ->
            FShotLog.write (sprintf "[Tray] LaunchWithDelay %d requested" delayMs)
            async {
                if delayMs > 0 then
                    do! Async.Sleep delayMs
                showCaptureOverlay desktop { CaptureRequest.Default with Mode = GuiInteractive }
            } |> Async.Start
        | OpenAbout ->
            FShotLog.write "[Tray] OpenAbout requested"
            showAboutWindow()
        | OpenSettings ->
            FShotLog.write "[Tray] OpenSettings requested"
            openSettingsFile()
        | OpenSaveFolder ->
            FShotLog.write "[Tray] OpenSaveFolder requested"
            openSaveFolder()
        | Exit ->
            FShotLog.write "[Tray] Exit requested"
            this.DisposeResources()
            desktop.Shutdown()

    /// Xử lý lệnh IPC từ instance thứ hai.
    member private this.HandleIpcCommand (args: string[]) =
        let request = CliParser.parse args
        FShotLog.write (sprintf "[IPC] Handling command: %A" args)
        let isHeadless =
            match request.Mode with
            | FullScreen | SingleScreen _ -> true
            | _ -> false

        match desktopLifetime with
        | Some desktop when request.Mode = GuiInteractive || request.OutputTarget = OpenGui ->
            FShotLog.write "[IPC] Opening GUI overlay from IPC command"
            showCaptureOverlay desktop { CaptureRequest.Default with Mode = request.Mode; DelayMs = request.DelayMs; OutputTarget = request.OutputTarget }
        | Some _ when isHeadless ->
            // TODO: Chạy headless capture trong nền mà không thoát app daemon.
            FShotLog.write "[IPC] Headless capture commands via IPC not yet implemented"
        | _ ->
            FShotLog.write "[IPC] App not ready or unknown command"

    /// Xử lý lệnh IPC từ instance thứ hai (static entry point, thread-safe).
    static member HandleIpcCommand (args: string[]) =
        match Application.Current with
        | :? App as app ->
            Dispatcher.UIThread.InvokeAsync(fun () -> app.HandleIpcCommand(args)) |> ignore
        | _ ->
            FShotLog.write "[IPC] No running App instance to handle command"

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

    override this.Initialize() =
        this.InitializeComponent()

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

        // Đồng bộ khởi động cùng Windows với cấu hình (FR-CFG-006, FR-WIN-005).
        let appConfig = ConfigStore.loadConfig()
        match StartupRegistration.syncStartup appConfig.StartupLaunch with
        | Ok () -> FShotLog.write "[Startup] Synced startup registration"
        | Error err -> FShotLog.write (sprintf "[Startup] %s" err)

        AppDomain.CurrentDomain.UnhandledException.AddHandler(
            new UnhandledExceptionEventHandler(fun _ e ->
                match e.ExceptionObject with
                | :? exn as ex -> FShotLog.writeEx "Unhandled exception" ex
                | _ -> FShotLog.write (sprintf "Unhandled non-exception: %A" e.ExceptionObject)
            )
        )

        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            desktopLifetime <- Some desktop

            // Đăng ký sự kiện hệ thống để thoát graceful (P2.03).
            AppDomain.CurrentDomain.ProcessExit.Add(fun _ ->
                FShotLog.write "[AppLifecycle] ProcessExit event received"
                this.DisposeResources()
            )
            Console.CancelKeyPress.Add(fun e ->
                FShotLog.write "[AppLifecycle] CancelKeyPress event received"
                e.Cancel <- true
                this.DisposeResources()
                desktop.Shutdown()
            )

            // Đăng ký tray icon nếu không bị tắt trong cấu hình (FR-CFG-007).
            let appConfig = ConfigStore.loadConfig()
            if appConfig.DisabledTrayIcon then
                FShotLog.write "[App] Tray icon disabled by configuration"
                if App.IsDaemon then
                    FShotLog.write "[App] WARNING: daemon mode without tray icon; use global hotkeys or edit config.json to restore"
                    desktop.ShutdownMode <- ShutdownMode.OnExplicitShutdown
                    desktop.MainWindow <- null
                else
                    showCaptureOverlay desktop App.CaptureRequest
            else
                let service = TrayIconService.Create(this, this.DispatchTrayCommand desktop)
                trayService <- Some service

                if App.IsDaemon then
                    desktop.ShutdownMode <- ShutdownMode.OnExplicitShutdown
                    desktop.MainWindow <- null
                    FShotLog.write "[App] Daemon mode active: tray icon only"
                else
                    let request = App.CaptureRequest
                    showCaptureOverlay desktop request

        | _ ->
            FShotLog.write "Unknown application lifetime"

        base.OnFrameworkInitializationCompleted()
