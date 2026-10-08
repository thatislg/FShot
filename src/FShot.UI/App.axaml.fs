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
open FShot.Platform.Win32.Hotkeys
open FShot.Platform.Win32.Lifecycle
open FShot.Platform.Win32.Screen
open FShot.Platform.Win32.Startup
open FShot.Platform.Win32.Tray
open FShot.Rendering.Skia.Renderers
open FShot.UI.Cli
open FShot.UI.SkiaCanvas
open FShot.UI.Services
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

    [<DefaultValue>]
    static val mutable private _openSettingsOnStartup: bool

    /// Khi true, app mở cửa sổ cài đặt thay vì overlay/daemon (lệnh `fshot config`).
    static member OpenSettingsOnStartup
        with get() = App._openSettingsOnStartup
        and set(value) = App._openSettingsOnStartup <- value

    let mutable currentOverlay: CaptureOverlayWindow option = None
    let mutable trayService: TrayIconService option = None
    let mutable desktopLifetime: IClassicDesktopStyleApplicationLifetime option = None
    let mutable notificationService: NotificationService option = None
    let mutable hotkeyService: GlobalHotkeyService option = None
    let mutable configWatcher: FileSystemWatcher option = None
    let mutable abortSubscription: obj option = None
    let mutable exportSubscription: obj option = None

    /// Giải phóng tài nguyên tập trung khi thoát ứng dụng.
    member private this.DisposeResources() =
        FShotLog.write "[AppLifecycle] Disposing resources"
        abortSubscription |> Option.iter (fun d ->
            try
                let t = d.GetType()
                let remove = t.GetMethod("RemoveHandler")
                if not (isNull remove) then
                    remove.Invoke(d, [| box (d) |]) |> ignore
            with _ -> ())
        abortSubscription <- None
        exportSubscription |> Option.iter (fun d ->
            try
                let t = d.GetType()
                let remove = t.GetMethod("RemoveHandler")
                if not (isNull remove) then
                    remove.Invoke(d, [| box (d) |]) |> ignore
            with _ -> ())
        exportSubscription <- None
        trayService |> Option.iter (fun t -> t.Dispose())
        trayService <- None
        hotkeyService |> Option.iter (fun h -> h.Dispose())
        hotkeyService <- None
        configWatcher |> Option.iter (fun w ->
            try w.Dispose() with _ -> ())
        configWatcher <- None
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

    /// Mở cửa sổ cài đặt (Config Editor) — FR-SYS-006.
    let showSettingsWindow () =
        Dispatcher.UIThread.InvokeAsync(fun () ->
            try
                let settings = SettingsWindow()
                settings.Show()
                FShotLog.write "[Tray] Settings window opened"
            with ex ->
                FShotLog.writeEx "[Tray] Show settings window failed" ex
        ) |> ignore

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
    /// `captureBounds` giới hạn overlay trên một màn hình cụ thể; None = toàn Virtual Screen.
    let showCaptureOverlay (desktop: IClassicDesktopStyleApplicationLifetime) (request: CaptureRequest) (captureBounds: Rect option) =
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
                    overlay.CaptureBounds <- captureBounds
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

    /// Mở overlay chụp cho một màn hình cụ thể từ tray menu (FR-SYS-003).
    /// Overlay sẽ phủ đúng màn hình đó, cho phép chọn vùng, save/copy, và thông báo.
    let captureScreenInteractive (screenIndex: int) =
        Dispatcher.UIThread.InvokeAsync(fun () ->
            try
                match ScreenEnumeration.getScreens() |> List.tryFind (fun s -> s.Index = screenIndex) with
                | Some screen ->
                    match desktopLifetime with
                    | Some desktop ->
                        let request =
                            { CaptureRequest.Default with
                                Mode = SingleScreen screenIndex
                                OutputTarget = OpenGui }
                        showCaptureOverlay desktop request (Some screen.VirtualBounds)
                        FShotLog.write (sprintf "[Tray] Opening interactive overlay for screen %d: %A" screenIndex screen.VirtualBounds)
                    | None ->
                        FShotLog.write "[Tray] No desktop lifetime available for interactive capture"
                | None ->
                    FShotLog.write (sprintf "[Tray] Screen %d not found for interactive capture" screenIndex)
            with ex ->
                FShotLog.writeEx "[Tray] Interactive screen capture failed" ex
        ) |> ignore

    /// Render và xuất captureResult (lưu file hoặc copy clipboard) kèm thông báo.
    let deliverCaptureResult (config: ConfigSnapshot) (captureResult: CaptureResult) (label: string) =
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
            FShotLog.write (sprintf "[Capture] %s saved to %s" label fullPath)
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
            FShotLog.write (sprintf "[Capture] %s copied to clipboard: %b" label ok)
            notificationService |> Option.iter (fun n ->
                n.ShowNotification(CopySuccess, config.ShowDesktopNotification) |> ignore)

    /// Chụp headless một màn hình (không qua overlay) từ tray menu.
    let captureScreenHeadless (screenIndex: int) =
        async {
            try
                FShotLog.write (sprintf "[Tray] Starting headless capture for screen %d" screenIndex)
                let config = ConfigStore.loadSnapshot()
                let captureService = CompositeCaptureService() :> ICaptureService
                let! result = captureService.CaptureScreenAsync screenIndex
                match result with
                | Ok captureResult -> deliverCaptureResult config captureResult (sprintf "Screen %d" screenIndex)
                | Error err -> FShotLog.write (sprintf "[Tray] Screen %d headless capture failed: %A" screenIndex err)
            with ex ->
                FShotLog.writeEx "[Tray] Screen capture failed" ex
        } |> Async.Start

    /// Chụp headless toàn bộ Virtual Screen (mọi màn hình).
    let captureFullScreenHeadless () =
        async {
            try
                FShotLog.write "[Hotkey] Starting headless full-screen capture"
                let config = ConfigStore.loadSnapshot()
                let captureService = CompositeCaptureService() :> ICaptureService
                let! result = captureService.CaptureVirtualScreenAsync()
                match result with
                | Ok captureResult -> deliverCaptureResult config captureResult "Full screen"
                | Error err -> FShotLog.write (sprintf "[Hotkey] Full-screen capture failed: %A" err)
            with ex ->
                FShotLog.writeEx "[Hotkey] Full-screen capture failed" ex
        } |> Async.Start

    /// Chụp headless màn hình đang chứa con trỏ chuột.
    let captureCursorScreenHeadless () =
        async {
            try
                FShotLog.write "[Hotkey] Starting headless cursor-screen capture"
                let config = ConfigStore.loadSnapshot()
                let captureService = CompositeCaptureService() :> ICaptureService
                let! result = captureService.CaptureCursorScreenAsync()
                match result with
                | Ok captureResult -> deliverCaptureResult config captureResult "Cursor screen"
                | Error err -> FShotLog.write (sprintf "[Hotkey] Cursor-screen capture failed: %A" err)
            with ex ->
                FShotLog.writeEx "[Hotkey] Cursor-screen capture failed" ex
        } |> Async.Start

    /// Hiển thị thông báo hủy thao tác chụp (FR-CFG-008).
    member this.ShowAbortNotification () =
        let config = ConfigStore.loadSnapshot()
        notificationService |> Option.iter (fun n ->
            n.ShowNotification(CaptureAborted, config.ShowAbortNotification) |> ignore)

    /// Dispatcher cho phím nóng toàn cục: map hành động sang overlay / headless capture.
    /// Được gọi trên UI Thread (đã dispatch qua Dispatcher.UIThread trong GlobalHotkeyService).
    member private this.DispatchHotkeyAction (action: HotkeyAction) =
        match action with
        | HotkeyAction.CaptureGui ->
            FShotLog.write "[Hotkey] CaptureGui requested"
            match desktopLifetime with
            | Some desktop -> showCaptureOverlay desktop { CaptureRequest.Default with Mode = GuiInteractive } None
            | None -> FShotLog.write "[Hotkey] No desktop lifetime available for capture"
        | HotkeyAction.CaptureFullScreen ->
            FShotLog.write "[Hotkey] CaptureFullScreen requested"
            captureFullScreenHeadless()
        | HotkeyAction.CaptureScreenAtCursor ->
            FShotLog.write "[Hotkey] CaptureScreenAtCursor requested"
            captureCursorScreenHeadless()

    /// Xây dựng danh sách HotkeyBinding từ cấu hình; bỏ qua phím rỗng/tắt,
    /// cảnh báo phím nguy hiểm hoặc không hợp lệ.
    let buildHotkeyBindings (config: AppConfig) : HotkeyBinding list =
        config.Hotkeys
        |> List.choose (fun h ->
            if not h.Enabled || String.IsNullOrWhiteSpace h.Key then
                None
            else
                match HotkeyParser.parse h.Key with
                | Ok parsed when not (HotkeyParser.isDangerous h.Key) ->
                    Some { Action = h.Action; Modifiers = parsed.Modifiers; VirtualKey = parsed.VirtualKey }
                | Ok _ ->
                    FShotLog.write (sprintf "[Hotkey] Skipped dangerous hotkey '%s' for %A" h.Key h.Action)
                    None
                | Error err ->
                    FShotLog.write (sprintf "[Hotkey] Skipped invalid hotkey '%s' for %A: %s" h.Key h.Action err)
                    None)

    /// Đọc cấu hình và cập nhật danh sách phím nóng đang đăng ký (dynamic rebinding).
    let applyHotkeyConfig () =
        let config = ConfigStore.loadConfig()
        let bindings = buildHotkeyBindings config
        hotkeyService |> Option.iter (fun service ->
            service.Rebind bindings |> ignore)
        FShotLog.write (sprintf "[Hotkey] Applied %d hotkey(s)" (List.length bindings))

    /// Theo dõi thay đổi của config.json để tự động cập nhật phím nóng (không cần restart).
    let startConfigWatcher () =
        try
            let dir = ConfigStore.defaultConfigDir
            let watcher = new FileSystemWatcher(dir, "config.json")
            watcher.NotifyFilter <- NotifyFilters.LastWrite ||| NotifyFilters.FileName
            watcher.EnableRaisingEvents <- true
            watcher.Changed.Add(fun _ ->
                FShotLog.write "[Hotkey] config.json changed; rebinding hotkeys"
                async {
                    do! Async.Sleep 300
                    applyHotkeyConfig()
                } |> Async.Start)
            configWatcher <- Some watcher
            FShotLog.write "[Hotkey] Config file watcher started"
        with ex ->
            FShotLog.writeEx "[Hotkey] Failed to start config watcher" ex

    /// Áp dụng lại cấu hình runtime sau khi lưu từ cửa sổ Settings (FR-SYS-011).
    /// Refresh snapshot cache, rebind hotkey toàn cục và đồng bộ khởi động cùng Windows.
    /// Trả về Ok khi thành công, Error kèm lý do khi thất bại (để hiển thị thông báo).
    member this.ApplyConfigAfterSave() : Result<unit, string> =
        try
            // Refresh snapshot để overlay/capture dùng giá trị mới ngay lập tức, không cần restart.
            let snapshot = ConfigStore.loadSnapshot()
            App.ConfigSnapshot <- snapshot

            // Rebind phím nóng toàn cục theo cấu hình mới.
            applyHotkeyConfig()

            // Đồng bộ khởi động cùng Windows (StartupLaunch).
            let appConfig = ConfigStore.loadConfig()
            match StartupRegistration.syncStartup appConfig.StartupLaunch with
            | Ok () -> FShotLog.write "[Settings] Startup registration synced"
            | Error err -> FShotLog.write (sprintf "[Settings] Startup sync warning: %s" err)

            FShotLog.write "[Settings] Runtime config applied"
            Ok ()
        with ex ->
            FShotLog.writeEx "[Settings] Apply runtime config failed" ex
            Error ex.Message

    /// Đăng ký phím nóng toàn cục và tắt Snipping Tool chiếm phím (P2.04–P2.06).
    member private this.RegisterGlobalHotkey () =
        // Vô hiệu hóa Windows 11 tự chuyển PrintScreen sang Snipping Tool (FR-SYS-020, FR-WIN-003).
        match SnippingTool.setPrintScreenRedirect false with
        | Ok () -> FShotLog.write "[Hotkey] Snipping Tool PrintScreen redirect disabled"
        | Error err -> FShotLog.write (sprintf "[Hotkey] %s" err)

        let service = new GlobalHotkeyService(this.DispatchHotkeyAction)
        hotkeyService <- Some service
        match service.Start() with
        | Ok () ->
            FShotLog.write "[Hotkey] Global hotkey service started"
            applyHotkeyConfig()
            startConfigWatcher()
        | Error err -> FShotLog.write (sprintf "[Hotkey] %s" err)

    /// Dispatcher cho các lệnh phát sinh từ tray.
    member private this.DispatchTrayCommand (desktop: IClassicDesktopStyleApplicationLifetime) (cmd: TrayCommand) =
        match cmd with
        | GuiCapture ->
            FShotLog.write "[Tray] GuiCapture requested"
            showCaptureOverlay desktop { CaptureRequest.Default with Mode = GuiInteractive } None
        | CaptureScreen index ->
            FShotLog.write (sprintf "[Tray] CaptureScreen %d requested" index)
            captureScreenInteractive index
        | LaunchWithDelay delayMs ->
            FShotLog.write (sprintf "[Tray] LaunchWithDelay %d requested" delayMs)
            async {
                if delayMs > 0 then
                    do! Async.Sleep delayMs
                showCaptureOverlay desktop { CaptureRequest.Default with Mode = GuiInteractive } None
            } |> Async.Start
        | OpenAbout ->
            FShotLog.write "[Tray] OpenAbout requested"
            showAboutWindow()
        | OpenSettings ->
            FShotLog.write "[Tray] OpenSettings requested"
            showSettingsWindow()
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
            showCaptureOverlay desktop { CaptureRequest.Default with Mode = request.Mode; DelayMs = request.DelayMs; OutputTarget = request.OutputTarget } None
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

        // Đăng ký cầu nối để SettingsWindow có thể yêu cầu áp dụng lại cấu hình khi bấm Apply.
        ConfigRuntime.applyCallback <- Some (fun () -> this.ApplyConfigAfterSave())

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

            // Khởi tạo dịch vụ thông báo desktop (toast/balloon fallback).
            notificationService <- Some (new NotificationService())
            FShotLog.write "[App] Notification service initialized"

            let mutable abortSub: obj = null
            let mutable exportSub: obj = null

            // Subscribe abort event từ CaptureCanvas để hiển thị thông báo hủy nếu cấu hình bật.
            // Chỉ subscribe một lần toàn cục để tránh spam notification khi mở overlay nhiều lần.
            CaptureCanvasEvents.AbortRequested.Add(fun () ->
                if App.IsDaemon then
                    this.ShowAbortNotification())
            |> fun d -> abortSub <- d

            abortSubscription <- Some abortSub

            // Subscribe sự kiện xuất ảnh từ CaptureCanvas để phát thông báo desktop.
            CaptureCanvasEvents.ExportCompleted.Add(fun outcome ->
                let config = ConfigStore.loadSnapshot()
                match outcome with
                | Saved path ->
                    notificationService |> Option.iter (fun n ->
                        n.ShowNotification(CaptureSuccess (Some path), config.ShowDesktopNotification) |> ignore)
                    FShotLog.write (sprintf "[App] Export saved notification: %s" path)
                | Copied ->
                    notificationService |> Option.iter (fun n ->
                        n.ShowNotification(CopySuccess, config.ShowDesktopNotification) |> ignore)
                    FShotLog.write "[App] Export copied notification"
                | Failed ->
                    FShotLog.write "[App] Export failed; no success notification")
            |> fun d -> exportSub <- d

            exportSubscription <- Some exportSub

            // Mở cửa sổ cài đặt nếu chạy `fshot config` (FR-CLI-05).
            if App.OpenSettingsOnStartup then
                desktop.ShutdownMode <- ShutdownMode.OnExplicitShutdown
                Dispatcher.UIThread.Post(fun () ->
                    try
                        let settings = SettingsWindow()
                        settings.Closed.Add(fun _ ->
                            this.DisposeResources()
                            desktop.Shutdown())
                        settings.Show()
                    with ex ->
                        FShotLog.writeEx "[App] Open settings on startup failed" ex)
            else
                // Đăng ký tray icon nếu không bị tắt trong cấu hình (FR-CFG-007).
                let appConfig = ConfigStore.loadConfig()
                if appConfig.DisabledTrayIcon then
                    FShotLog.write "[App] Tray icon disabled by configuration"
                    if App.IsDaemon then
                        FShotLog.write "[App] WARNING: daemon mode without tray icon; use global hotkeys or edit config.json to restore"
                        desktop.ShutdownMode <- ShutdownMode.OnExplicitShutdown
                        desktop.MainWindow <- null
                    else
                        showCaptureOverlay desktop App.CaptureRequest None
                else
                    let service = TrayIconService.Create(this, this.DispatchTrayCommand desktop)
                    trayService <- Some service

                    if App.IsDaemon then
                        desktop.ShutdownMode <- ShutdownMode.OnExplicitShutdown
                        desktop.MainWindow <- null
                        FShotLog.write "[App] Daemon mode active: tray icon only"
                    else
                        let request = App.CaptureRequest
                        showCaptureOverlay desktop request None

                // Đăng ký phím nóng toàn cục PrintScreen ở chế độ nền (P2.04, P2.05).
                if App.IsDaemon then
                    this.RegisterGlobalHotkey()

        | _ ->
            FShotLog.write "Unknown application lifetime"

        base.OnFrameworkInitializationCompleted()
