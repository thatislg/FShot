namespace FShot.UI

open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices
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
open FShot.Platform.Win32.Screen
open FShot.Platform.Win32.Tray
open FShot.Rendering.Skia.Renderers
open FShot.UI.Windows
open FShot.UI.Logging
open SkiaSharp

/// Khởi tạo ứng dụng Avalonia, quản lý overlay chụp và tray icon.
type App() =
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

    let mutable currentOverlay: CaptureOverlayWindow option = None
    let mutable trayService: TrayIconService option = None

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
                    | None ->
                        use data = exportBitmap.Encode(SKEncodedImageFormat.Png, 100)
                        let pngBytes = data.ToArray()
                        let pixelBytes = Array.zeroCreate<byte> (exportBitmap.Width * exportBitmap.Height * 4)
                        let ptr = exportBitmap.GetPixels()
                        if ptr <> IntPtr.Zero then
                            Marshal.Copy(ptr, pixelBytes, 0, pixelBytes.Length)
                        let ok = ClipboardService.copyImageToClipboard exportBitmap.Width exportBitmap.Height pngBytes pixelBytes
                        FShotLog.write (sprintf "[Tray] Screen %d copied to clipboard: %b" screenIndex ok)
                | Error err ->
                    FShotLog.write (sprintf "[Tray] Screen %d capture failed: %A" screenIndex err)
            with ex ->
                FShotLog.writeEx "[Tray] Screen capture failed" ex
        } |> Async.Start

    /// Dispatcher cho các lệnh phát sinh từ tray.
    let dispatchTrayCommand (desktop: IClassicDesktopStyleApplicationLifetime) (cmd: TrayCommand) =
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
            trayService |> Option.iter (fun t -> t.Dispose())
            desktop.Shutdown()

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

        AppDomain.CurrentDomain.UnhandledException.AddHandler(
            new UnhandledExceptionEventHandler(fun _ e ->
                match e.ExceptionObject with
                | :? exn as ex -> FShotLog.writeEx "Unhandled exception" ex
                | _ -> FShotLog.write (sprintf "Unhandled non-exception: %A" e.ExceptionObject)
            )
        )

        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            // Đăng ký tray icon ngay khi lifetime sẵn sàng (FR-SYS-001).
            let service = TrayIconService.Create(this, dispatchTrayCommand desktop)
            trayService <- Some service

            if App.IsDaemon then
                // Chế độ nền: không mở overlay, giữ app sống bằng OnExplicitShutdown.
                desktop.ShutdownMode <- ShutdownMode.OnExplicitShutdown
                desktop.MainWindow <- null
                FShotLog.write "[App] Daemon mode active: tray icon only"
            else
                // Chế độ GUI: mở overlay ngay theo CaptureRequest được truyền vào.
                let request = App.CaptureRequest
                if request.DelayMs = 0 && request.Mode = GuiInteractive then
                    showCaptureOverlay desktop request
                else
                    showCaptureOverlay desktop request

        | _ ->
            FShotLog.write "Unknown application lifetime"

        base.OnFrameworkInitializationCompleted()
