namespace FShot.UI.Windows

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open FShot.Core.Domain
open FShot.Platform.Win32.Capture
open FShot.Platform.Win32.Screen
open FShot.UI.SkiaCanvas

/// Cửa sổ overlay chụp màn hình.
/// Xem tài liệu 11_03_OverlayWindow.md.
type CaptureOverlayWindow() as this =
    inherit Window()

    let mutable canvas: CaptureCanvas option = None

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        canvas <- Some (this.FindControl<CaptureCanvas>("CaptureCanvas"))

    /// Mở cửa sổ phủ toàn Virtual Screen và chụp ảnh nền.
    member this.ShowOverlayAsync() =
        async {
            let bounds = ScreenEnumeration.getVirtualScreenBounds ()
            let width = int bounds.Width
            let height = int bounds.Height

            this.Position <- PixelPoint(int bounds.X, int bounds.Y)
            this.Width <- float width
            this.Height <- float height

            let service = StubCaptureService() :> ICaptureService
            let! result = service.CaptureVirtualScreenAsync()

            match result with
            | Ok captureResult ->
                canvas |> Option.iter (fun c -> c.SetCaptureResult captureResult)
                this.Show()
            | Error err ->
                // TODO: hiển thị lỗi.
                System.Diagnostics.Debug.WriteLine(sprintf "Capture error: %A" err)
        }
