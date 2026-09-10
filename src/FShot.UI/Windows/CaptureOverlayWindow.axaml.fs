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
open FShot.Platform.Win32.Screen
open FShot.UI.SkiaCanvas
open FShot.UI.Logging

/// Cửa sổ overlay chụp màn hình.
/// Xem tài liệu 11_03_OverlayWindow.md.
type CaptureOverlayWindow() as this =
    inherit Window()

    let mutable canvas: CaptureCanvas option = None

    do
        this.InitializeComponent()
        this.ConfigureOverlayWindow()

        // Esc luôn đóng overlay ngay cả khi focus không nằm trên canvas.
        this.KeyDown.Add(fun e ->
            if e.Key = Key.Escape then
                FShotLog.write "[CaptureOverlayWindow] Esc pressed - closing overlay"
                e.Handled <- true
                try this.Close() with ex -> FShotLog.writeEx "Failed to close overlay window from KeyDown" ex
        )

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        canvas <- Some (this.FindControl<CaptureCanvas>("CaptureCanvas"))
        FShotLog.write "CaptureOverlayWindow initialized"

    /// Cấu hình cửa sổ borderless topmost phủ toàn Virtual Screen.
    member private this.ConfigureOverlayWindow() =
        this.WindowState <- WindowState.Normal
        this.WindowDecorations <- WindowDecorations.None
        this.Topmost <- true
        this.CanResize <- false
        this.ShowInTaskbar <- false
        this.Focusable <- true

        // Background đen mờ để người dùng thấy đang ở chế độ capture.
        this.Background <- Media.Brushes.Black
        this.Opacity <- 0.4

    /// Mở cửa sổ phủ toàn Virtual Screen và chụp ảnh nền.
    member this.ShowOverlayAsync() =
        async {
            FShotLog.write "ShowOverlayAsync starting"

            // Mọi thao tác UI phải chạy trên UI thread.
            // Dispatcher.InvokeAsync trả về Task; await để đảm bảo chạy xong.
            let uiTask =
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(fun () ->
                    FShotLog.write "ShowOverlayAsync: getting Virtual Screen bounds"
                    let bounds = ScreenEnumeration.getVirtualScreenBounds ()
                    let width = int bounds.Width
                    let height = int bounds.Height

                    FShotLog.write(
                        sprintf "Virtual Screen bounds: X=%.0f Y=%.0f W=%.0f H=%.0f" bounds.X bounds.Y bounds.Width bounds.Height
                    )

                    this.Position <- PixelPoint(int bounds.X, int bounds.Y)
                    this.Width <- float width
                    this.Height <- float height
                    this.IsVisible <- true

                    FShotLog.write "ShowOverlayAsync: showing window"
                    this.Show()
                    this.Focus() |> ignore
                    this.Activate() |> ignore
                    this.Topmost <- true
                )

            let! _ = uiTask.GetTask() |> Async.AwaitTask

            // Đợi một chút để cửa sổ hiển thị hoàn toàn.
            do! Async.Sleep(150)

            FShotLog.write "ShowOverlayAsync: starting stub capture"
            let service = StubCaptureService() :> ICaptureService
            let! result = service.CaptureVirtualScreenAsync()

            match result with
            | Ok captureResult ->
                FShotLog.write(
                    sprintf "Stub capture succeeded: %dx%d pixels" captureResult.Width captureResult.Height
                )
                canvas |> Option.iter (fun c -> c.SetCaptureResult captureResult)
            | Error err ->
                FShotLog.write (sprintf "Stub capture failed: %A" err)
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
