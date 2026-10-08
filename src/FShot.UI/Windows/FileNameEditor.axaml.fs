namespace FShot.UI.Windows

open System
open Avalonia.Controls
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open Avalonia.Threading
open FShot.Core.Domain

/// UserControl chỉnh sửa mẫu tên file với preview token strftime và phát hiện ký tự cấm.
/// Xem tài liệu 11_06_ConfigWindow.md, mục 4 (P2.14, FR-CFG-003).
type FileNameEditor() as this =
    inherit UserControl()

    let mutable previewTimer: DispatcherTimer option = None

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

    /// Mẫu tên file hiện tại (đọc/ghi, đồng bộ với TextBox).
    member this.Pattern
        with get () =
            let box = this.FindControl<TextBox>("PatternBox")
            if isNull box then "" else if isNull box.Text then "" else box.Text
        and set (value: string) =
            let box = this.FindControl<TextBox>("PatternBox")
            if not (isNull box) then box.Text <- if isNull value then "" else value
            this.UpdatePreview()

    /// Trả về mẫu hợp lệ (fallback an toàn nếu không hợp lệ).
    member this.ValidPattern =
        FileNamePatternValidator.normalized this.Pattern

    /// Cập nhật live preview và trạng thái lỗi.
    member private this.UpdatePreview() =
        let preview = this.FindControl<TextBlock>("PreviewText")
        let error = this.FindControl<TextBlock>("ErrorText")
        let current = this.Pattern

        let forbidden = FileNamePatternValidator.findForbiddenChars current

        if not (isNull error) then
            if List.isEmpty forbidden then
                error.IsVisible <- false
            else
                error.IsVisible <- true
                error.Text <- sprintf "Ký tự không hợp lệ: %s" (String.concat " " (forbidden |> List.map string))

        if not (isNull preview) then
            let valid =
                if List.isEmpty forbidden && not (String.IsNullOrWhiteSpace current) then
                    current
                else
                    FileNamePatternValidator.safeFallbackPattern
            let resolved = FileNamePattern.resolve valid DateTime.Now
            preview.Text <- sprintf "Xem trước: %s" resolved

    /// Bắt đầu cập nhật preview theo thời gian thực (mỗi giây).
    member this.StartLivePreview() =
        let timer = DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(1.0)
        timer.Tick.Add(fun _ -> this.UpdatePreview())
        timer.Start()
        previewTimer <- Some timer
        this.UpdatePreview()

    member private this.OnPatternChanged(_: obj, _: TextChangedEventArgs) =
        this.UpdatePreview()

    member private this.OnTokenClick(sender: obj, _: RoutedEventArgs) =
        match sender with
        | :? Button as btn when not (isNull btn.Tag) ->
            let token = string btn.Tag
            let box = this.FindControl<TextBox>("PatternBox")
            if not (isNull box) then
                let caret = box.CaretIndex
                let text = if isNull box.Text then "" else box.Text
                box.Text <- text.Insert(caret, token)
                box.CaretIndex <- caret + token.Length
                this.UpdatePreview()
        | _ -> ()
