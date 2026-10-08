namespace FShot.UI.Windows

open System
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Media

/// UserControl chọn màu bằng bánh xe RGB (ColorView từ Avalonia.Controls.ColorPicker).
/// Xem tài liệu 11_06_ConfigWindow.md (FR-CFG-100/101).
type ColorWheel() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

    member private this.View =
        this.FindControl<ColorView>("ColorViewControl")

    /// Parse chuỗi `#RRGGBB` sang Avalonia Color (None nếu không hợp lệ).
    static member tryParseHex (hex: string) : Color option =
        let s = if isNull hex then "" else hex.Trim().TrimStart('#')
        if s.Length = 6 then
            match UInt32.TryParse(s, Globalization.NumberStyles.HexNumber, Globalization.CultureInfo.InvariantCulture) with
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

    /// Màu hiện tại dạng hex `#RRGGBB` (đọc/ghi đồng bộ với ColorView).
    member this.Color
        with get () =
            let view = this.View
            if isNull view then ""
            else ColorWheel.toHex view.Color
        and set (value: string) =
            let view = this.View
            if not (isNull view) then
                match ColorWheel.tryParseHex value with
                | Some c -> view.Color <- c
                | None -> ()
