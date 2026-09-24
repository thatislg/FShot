namespace FShot.UI.Services

open System
open System.IO
open System.Diagnostics
open System.Threading
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Markup.Xaml
open Avalonia.Threading

/// Loại thông báo F-Shot có thể phát.
type NotificationKind =
    | CaptureSuccess of filePath: string option
    | CopySuccess
    | CaptureAborted

/// Cửa sổ thông báo nhỏ hiển thị góc màn hình, tự đóng sau vài giây.
/// Không phụ thuộc Windows Toast API hay AUMID.
type NotificationWindow() as this =
    inherit Window()

    let mutable notificationTitle = ""
    let mutable notificationMessage = ""
    let mutable notificationFilePath: string option = None

    do
        this.InitializeComponent()
        this.ConfigureWindow()
        this.PositionWindow()

    /// Cài đặt nội dung vào cửa sổ rồi hiển thị.
    member this.Show(title: string, message: string, filePath: string option) =
        notificationTitle <- title
        notificationMessage <- message
        notificationFilePath <- filePath
        this.ConfigureWindow()
        this.PositionWindow()
        this.Show()
        this.StartAutoClose()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

    member private this.ConfigureWindow() =
        let titleText = this.FindControl<TextBlock>("TitleText")
        let messageText = this.FindControl<TextBlock>("MessageText")
        if not (isNull titleText) then titleText.Text <- notificationTitle
        if not (isNull messageText) then messageText.Text <- notificationMessage

        this.WindowState <- WindowState.Normal
        this.ShowInTaskbar <- false
        this.Topmost <- true
        this.CanResize <- false
        this.Focusable <- false

        this.PointerPressed.Add(fun _ ->
            match notificationFilePath with
            | Some path ->
                NotificationService.HighlightFileInExplorer(path) |> ignore
            | None -> ()
            this.Close())

    /// Đặt cửa sổ vào góc dưới phải màn hình chính.
    member private this.PositionWindow() =
        try
            let screens = this.Screens
            let primary =
                if isNull screens then null
                else screens.Primary
            let workArea =
                if isNull primary then
                    new PixelRect(0, 0, 1920, 1080)
                else
                    primary.WorkingArea

            let w = int this.Width
            let h = int this.Height
            let x = workArea.X + workArea.Width - w - 16
            let y = workArea.Y + workArea.Height - h - 16
            this.Position <- new PixelPoint(x, y)
        with _ ->
            ()

    /// Tự động đóng sau 4 giây.
    member private this.StartAutoClose() =
        let closeOp = async {
            do! Async.Sleep 4000
            do! Async.Sleep 250
            Dispatcher.UIThread.Post(fun () ->
                try
                    if this.IsVisible then
                        this.Close()
                with _ -> ())
        }
        Async.Start closeOp

/// Dịch vụ thông báo desktop đơn giản cho F-Shot.
/// Thay vì dựa vào Windows Toast/Balloon API (dễ bị chặn khi thiếu AUMID/shortcut),
/// dịch vụ này tự vẽ một cửa sổ Avalonia nhỏ ở góc màn hình, hiển thị vài giây rồi tự đóng.
and NotificationService() =

    let mutable lastWindow: NotificationWindow option = None

    let buildContent (kind: NotificationKind) : string * string * string option =
        match kind with
        | CaptureSuccess (Some path) ->
            let fileName = Path.GetFileName path
            ("Đã lưu ảnh chụp", fileName, Some path)
        | CaptureSuccess None ->
            ("Đã lưu ảnh chụp", "", None)
        | CopySuccess ->
            ("Đã sao chép", "Ảnh chụp đã được đưa vào clipboard", None)
        | CaptureAborted ->
            ("Đã hủy", "Thao tác chụp màn hình bị hủy", None)

    /// Hiển thị thông báo theo cấu hình.
    /// `enabled` cho phép tắt từ cấu hình; `force` bỏ qua `enabled` khi cần.
    member this.ShowNotification(kind: NotificationKind, enabled: bool, ?force: bool) : bool =
        let force = defaultArg force false
        if not enabled && not force then
            false
        else
            let (title, message, filePath) = buildContent kind
            try
                lastWindow |> Option.iter (fun w ->
                    try w.Close() with _ -> ())
                let window = new NotificationWindow()
                lastWindow <- Some window
                window.Closed.Add(fun _ ->
                    if lastWindow = Some window then
                        lastWindow <- None)
                Dispatcher.UIThread.Post(fun () -> window.Show(title, message, filePath))
                true
            with _ ->
                false

    /// Mở file ảnh trong Windows Explorer và highlight.
    /// Trả về true nếu khởi chạy explorer thành công.
    static member HighlightFileInExplorer(filePath: string) : bool =
        if not (File.Exists filePath) then false
        else
            try
                let args = sprintf "/select,\"%s\"" filePath
                let psi = ProcessStartInfo("explorer.exe", args)
                psi.UseShellExecute <- true
                Process.Start(psi) |> ignore
                true
            with _ ->
                false

    /// Đóng thông báo hiện tại nếu có.
    member this.Dispose() =
        lastWindow |> Option.iter (fun w ->
            try w.Close() with _ -> ())
        lastWindow <- None

    interface IDisposable with
        member this.Dispose() = this.Dispose()
