namespace FShot.UI.Windows

open System
open System.Globalization
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open Avalonia.Media

/// UserControl chọn màu gồm 3 thành phần KHÔNG được bỏ cái nào:
/// 1. Nút "ColorPicker" (bấm mở bánh xe màu trực quan để tìm mã hex).
/// 2. Ô nhập mã hex (TextBox) + nút "Thêm màu" để thêm vào bảng 8 ô màu.
/// 3. Bảng 8 ô màu tròn; màu mới thêm sẽ thay dần màu cũ.
/// Dùng chung cho Tab Giao diện (accent/phụ) và Tab Công cụ mặc định (UserColors).
/// Xem tài liệu 11_06_ConfigWindow.md (FR-CFG-100/101/104).
type ColorSelector() as this =
    inherit UserControl()

    static let presetColors =
        [
            "#FF0000" // Đỏ
            "#FF8C00" // Cam đậm
            "#FFD700" // Vàng
            "#008000" // Xanh lá
            "#00CED1" // Ngọc lam
            "#0000FF" // Xanh lam
            "#8A2BE2" // Tím
            "#FF69B4" // Hồng
        ]

    let mutable palette: string list = presetColors
    let mutable syncing = false

    do this.InitializeComponent()

    /// 8 màu cơ bản mặc định cho bảng swatch.
    static member PresetColors : string list = presetColors

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        this.HookEvents()
        this.RenderSwatches()

    /// Lấy ColorView nằm bên trong Flyout của nút "ColorPicker".
    member private this.View : ColorView =
        let btn = this.FindControl<Button>("ColorPickerButton")
        if isNull btn || isNull btn.Flyout then null
        else
            match btn.Flyout with
            | :? Flyout as f ->
                match f.Content with
                | :? ColorView as cv -> cv
                | _ -> null
            | _ -> null

    member private this.HexBox = this.FindControl<TextBox>("HexBox")
    member private this.SwatchPanel = this.FindControl<WrapPanel>("SwatchPanel")

    /// Parse chuỗi `#RRGGBB` sang Avalonia Color (None nếu không hợp lệ).
    static member tryParseHex (hex: string) : Color option =
        let s = if isNull hex then "" else hex.Trim().TrimStart('#')
        if s.Length = 6 then
            match UInt32.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture) with
            | true, v ->
                let r = byte ((v >>> 16) &&& 0xFFu)
                let g = byte ((v >>> 8) &&& 0xFFu)
                let b = byte (v &&& 0xFFu)
                Some (Color.FromArgb(255uy, r, g, b))
            | _ -> None
        else
            None

    /// Chuyển Avalonia Color sang chuỗi `#RRGGBB`.
    static member toHex (c: Color) : string =
        sprintf "#%02X%02X%02X" c.R c.G c.B

    /// Thêm một màu vào danh sách palette (tối đa 8 ô):
    /// bỏ qua nếu đã có (so khớp không phân biệt hoa thường),
    /// chèn mới lên đầu và cắt bỏ màu cũ nhất nếu vượt quá 8.
    static member addToPalette (palette: string list) (hex: string) : string list =
        if palette |> List.exists (fun x -> String.Equals(x, hex, StringComparison.OrdinalIgnoreCase)) then
            palette
        else
            (hex :: palette) |> List.truncate 8

    /// Màu hiện tại dạng hex `#RRGGBB` (đọc/ghi đồng bộ ColorView + HexBox).
    member this.Color
        with get () =
            let v = this.View
            if isNull v then "" else ColorSelector.toHex v.Color
        and set (value: string) =
            syncing <- true
            try
                let v = this.View
                if not (isNull v) then
                    match ColorSelector.tryParseHex value with
                    | Some c ->
                        v.Color <- c
                        let box = this.HexBox
                        if not (isNull box) then box.Text <- ColorSelector.toHex c
                    | None -> ()
            finally
                syncing <- false

    /// Danh sách màu swatch (tối đa 8). Nếu rỗng sẽ nạp 8 màu cơ bản.
    member this.Palette
        with get () = palette
        and set (value: string list) =
            palette <-
                if List.isEmpty value then presetColors
                else value
            this.RenderSwatches()

    member private this.RenderSwatches() =
        let panel = this.SwatchPanel
        if not (isNull panel) then
            panel.Children.Clear()
            for hex in palette do
                let btn = Button()
                btn.Width <- 24.0
                btn.Height <- 24.0
                btn.CornerRadius <- Avalonia.CornerRadius(12.0)
                btn.Padding <- Avalonia.Thickness(0.0)
                btn.Margin <- Avalonia.Thickness(0.0, 0.0, 6.0, 6.0)
                btn.BorderThickness <- Avalonia.Thickness(1.0)
                btn.BorderBrush <- SolidColorBrush(Color.FromArgb(80uy, 0uy, 0uy, 0uy))
                match ColorSelector.tryParseHex hex with
                | Some c -> btn.Background <- SolidColorBrush(c)
                | None -> btn.Background <- Brushes.Gray
                ToolTip.SetTip(btn, hex)
                btn.Click.Add(fun _ -> this.Color <- hex)
                panel.Children.Add(btn) |> ignore

    /// Đồng bộ hai chiều giữa ColorView (bánh xe) và HexBox (có cờ chống vòng lặp).
    member private this.HookEvents() =
        let view = this.View
        let box = this.HexBox

        if not (isNull view) then
            view.PropertyChanged.Add(fun e ->
                if e.Property.Name = "Color" && not syncing then
                    syncing <- true
                    try
                        if not (isNull box) then box.Text <- ColorSelector.toHex view.Color
                    finally
                        syncing <- false)
            |> ignore

        if not (isNull box) then
            box.TextChanged.Add(fun _ ->
                if not syncing then
                    match ColorSelector.tryParseHex box.Text with
                    | Some c ->
                        syncing <- true
                        try
                            if not (isNull view) then view.Color <- c
                        finally
                            syncing <- false
                    | None -> ())
            |> ignore

    /// Thêm màu hiện tại (từ bánh xe/ô hex) vào bảng swatch (thay màu cũ nhất khi đủ 8 ô).
    member private this.OnAddClick(_: obj, _: RoutedEventArgs) =
        match ColorSelector.tryParseHex this.Color with
        | Some c ->
            palette <- ColorSelector.addToPalette palette (ColorSelector.toHex c)
            this.RenderSwatches()
        | None -> ()
