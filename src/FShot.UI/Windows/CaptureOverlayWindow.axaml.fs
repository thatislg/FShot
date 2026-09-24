namespace FShot.UI.Windows

open System
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Markup.Xaml
open Avalonia.Threading
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Platform.Win32.Capture
open FShot.Platform.Win32.Config
open FShot.Platform.Win32.Screen
open FShot.UI.SkiaCanvas
open FShot.UI.Logging

/// Cửa sổ overlay chụp màn hình.
/// Xem tài liệu 11_03_OverlayWindow.md và 11_09_OverlayStateIntegration.md.
type CaptureOverlayWindow() as this =
    inherit Window()

    let mutable canvas: CaptureCanvas option = None
    let mutable rootCanvas: Canvas option = None
    let mutable overlayState: FShot.Core.State.OverlayState option = None
    let mutable configSnapshot: ConfigSnapshot = ConfigStore.loadSnapshot()
    let mutable captureBounds: Rect option = None

    do
        this.InitializeComponent()
        this.ConfigureOverlayWindow()

        // Key handling đã được chuyển xuống CaptureCanvas để OverlayState xử lý.
        // Window không còn đóng trực tiếp từ Esc.

    member this.ConfigSnapshot
        with get() = configSnapshot
        and set(v) = configSnapshot <- v

    /// Bounds của vùng chụp giới hạn. None = toàn Virtual Screen.
    member this.CaptureBounds
        with get() = captureBounds
        and set(v) = captureBounds <- v

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        canvas <- Some (this.FindControl<CaptureCanvas>("CaptureCanvas"))
        rootCanvas <- Some (this.FindControl<Canvas>("RootCanvas"))
        FShotLog.write "CaptureOverlayWindow initialized"

    /// Cấu hình cửa sổ borderless topmost phủ đúng vùng chụp.
    member private this.ConfigureOverlayWindow() =
        this.WindowState <- WindowState.Normal
        this.WindowDecorations <- WindowDecorations.None
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Topmost <- true
        this.CanResize <- false
        this.ShowInTaskbar <- false
        this.Focusable <- true

        // Flameshot-style: cửa sổ trong suốt hoàn toàn.
        // Dimming và selection được vẽ bởi CaptureCanvas trên nền trong suốt.
        // Không dùng Window.Opacity < 1.0 để tránh toàn bộ cửa sổ bị mờ.
        this.Background <- Media.Brushes.Transparent
        this.Opacity <- 1.0
        this.Cursor <- new Avalonia.Input.Cursor(StandardCursorType.Cross)

    /// Mở cửa sổ phủ vùng chụp và chụp ảnh nền thực tế.
    /// Nếu `CaptureBounds` được đặt, chỉ phủ và chụp màn hình đó; ngược lại phủ toàn Virtual Screen.
    member this.ShowOverlayAsync() =
        async {
            FShotLog.write "ShowOverlayAsync starting: capturing screen before showing window..."

            let targetBounds =
                match captureBounds with
                | Some b -> b
                | None -> ScreenEnumeration.getVirtualScreenBounds()

            // 1. Chụp màn hình thật TRƯỚC KHI hiển thị cửa sổ overlay để có ảnh sạch
            let! captureResultOpt =
                async {
                    let realService = WindowsCaptureService() :> ICaptureService
                    let! res =
                        match captureBounds with
                        | Some _ when targetBounds = ScreenEnumeration.getVirtualScreenBounds() ->
                            realService.CaptureVirtualScreenAsync()
                        | Some _ ->
                            // Tìm screen index tương ứng với bounds
                            let screens = ScreenEnumeration.getScreens()
                            let screenOpt =
                                screens
                                |> List.tryFind (fun s ->
                                    abs(s.VirtualBounds.X - targetBounds.X) < 0.001 &&
                                    abs(s.VirtualBounds.Y - targetBounds.Y) < 0.001)
                            match screenOpt with
                            | Some s -> realService.CaptureScreenAsync s.Index
                            | None -> realService.CaptureVirtualScreenAsync()
                        | None ->
                            realService.CaptureVirtualScreenAsync()
                    match res with
                    | Ok r -> return Ok r
                    | Error err ->
                        FShotLog.write (sprintf "WindowsCaptureService failed (%A), falling back to StubCaptureService" err)
                        let stubService = StubCaptureService() :> ICaptureService
                        return! stubService.CaptureVirtualScreenAsync()
                }

            match captureResultOpt with
            | Ok captureResult ->
                FShotLog.write (sprintf "Screen capture ready: %dx%d pixels" captureResult.Width captureResult.Height)

                // 2. Cấu hình canvas, khởi tạo state và hiển thị cửa sổ trên UI thread
                let uiTask =
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(fun () ->
                        let bounds = targetBounds
                        let width = int bounds.Width
                        let height = int bounds.Height

                        FShotLog.write (sprintf "Capture bounds: X=%.0f Y=%.0f W=%.0f H=%.0f" bounds.X bounds.Y bounds.Width bounds.Height)

                        this.Width <- float width
                        this.Height <- float height

                        rootCanvas |> Option.iter (fun rc ->
                            rc.Width <- this.Width
                            rc.Height <- this.Height
                        )
                        canvas |> Option.iter (fun c ->
                            c.Width <- this.Width
                            c.Height <- this.Height
                            c.SetCaptureResult captureResult
                        )

                        let config = this.ConfigSnapshot
                        let state = FShot.Core.State.OverlayStateLogic.init captureResult config
                        overlayState <- Some state

                        canvas |> Option.iter (fun c -> c.SetOverlayState state)

                        this.IsVisible <- true
                        FShotLog.write "ShowOverlayAsync: showing window"
                        this.Show()
                        // Đặt position SAU Show() để Avalonia tuân thủ Manual startup location.
                        this.Position <- PixelPoint(int bounds.X, int bounds.Y)
                        this.Focus() |> ignore
                        this.Activate() |> ignore
                        this.Topmost <- true
                        FShotLog.write (sprintf "ShowOverlayAsync: window position set to %A" this.Position)
                    )

                let! _ = uiTask.GetTask() |> Async.AwaitTask
                ()
            | Error err ->
                FShotLog.write (sprintf "Screen capture failed completely: %A" err)
        }

    /// Lấy FPS hiện tại từ canvas.
    member this.CurrentFps =
        canvas |> Option.map (fun c -> c.CurrentFps) |> Option.defaultValue 0.0

    /// Lấy thời gian render frame gần nhất (ms).
    member this.LastFrameTimeMs =
        canvas |> Option.map (fun c -> c.LastFrameTimeMs) |> Option.defaultValue 0.0

    /// Lấy thời gian render frame trung bình (ms).
    member this.AverageFrameTimeMs =
        canvas |> Option.map (fun c -> c.AverageFrameTimeMs) |> Option.defaultValue 0.0

    /// Lấy vùng chọn hiện tại.
    member this.SelectionBounds =
        canvas |> Option.map (fun c -> c.Selection.Bounds) |> Option.defaultValue { X = 0.0; Y = 0.0; Width = 0.0; Height = 0.0 }
