// Unit tests cho ColorSelector (component chọn màu trong Settings Window).
// Bao phủ:
// 1. Logic thuần: tryParseHex, toHex, addToPalette.
// 2. Cấu trúc UI (headless): nút ColorPicker + ô nhập hex + nút Thêm màu + bảng 8 ô màu
//    kèm title/default của từng thành phần.
module FShot.UI.Tests.ColorSelectorTests

open System
open System.Threading
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Headless
open Avalonia.Media
open Avalonia.Threading
open Avalonia.VisualTree
open FShot.UI.Windows
open Xunit

// ==========================================================================
// 1. tryParseHex — phân tích mã hex
// ==========================================================================

[<Fact>]
let ``tryParseHex phân tích chuỗi hợp lệ có dấu #`` () =
    let c = ColorSelector.tryParseHex "#FF8000"
    Assert.True(c.IsSome)
    Assert.Equal(255uy, c.Value.R)
    Assert.Equal(128uy, c.Value.G)
    Assert.Equal(0uy, c.Value.B)

[<Fact>]
let ``tryParseHex phân tích chuỗi không có dấu #`` () =
    let c = ColorSelector.tryParseHex "0A0B0C"
    Assert.True(c.IsSome)
    Assert.Equal(10uy, c.Value.R)
    Assert.Equal(11uy, c.Value.G)
    Assert.Equal(12uy, c.Value.B)

[<Fact>]
let ``tryParseHex chấp nhận chữ thường`` () =
    let c = ColorSelector.tryParseHex "#abcdef"
    Assert.True(c.IsSome)
    Assert.Equal(0xABuy, c.Value.R)
    Assert.Equal(0xCDuy, c.Value.G)
    Assert.Equal(0xEFuy, c.Value.B)

[<Fact>]
let ``tryParseHex trả về None cho chuỗi rỗng hoặc toàn khoảng trắng`` () =
    Assert.True((ColorSelector.tryParseHex "").IsNone)
    Assert.True((ColorSelector.tryParseHex "   ").IsNone)

[<Fact>]
let ``tryParseHex trả về None cho chuỗi sai độ dài`` () =
    Assert.True((ColorSelector.tryParseHex "#FFF").IsNone)
    Assert.True((ColorSelector.tryParseHex "#FF800000").IsNone)

[<Fact>]
let ``tryParseHex trả về None cho ký tự không hợp lệ`` () =
    Assert.True((ColorSelector.tryParseHex "#GGGGGG").IsNone)
    Assert.True((ColorSelector.tryParseHex "!@#$%^").IsNone)

// ==========================================================================
// 2. toHex — định dạng mã hex
// ==========================================================================

[<Fact>]
let ``toHex chuyển Color sang chuỗi #RRGGBB`` () =
    let c = Color.FromArgb(255uy, 255uy, 128uy, 0uy)
    Assert.Equal("#FF8000", ColorSelector.toHex c)

[<Fact>]
let ``toHex và tryParseHex khớp nhau (roundtrip)`` () =
    let hex = "#12AB34"
    let c = ColorSelector.tryParseHex hex
    Assert.True(c.IsSome)
    Assert.Equal(hex, ColorSelector.toHex c.Value)

// ==========================================================================
// 3. addToPalette — quản lý palette (tối đa 8 ô)
// ==========================================================================

[<Fact>]
let ``addToPalette thêm màu mới lên đầu`` () =
    let p = ColorSelector.addToPalette [ "#0000FF"; "#FF0000" ] "#00FF00"
    Assert.Equal<string list>([ "#00FF00"; "#0000FF"; "#FF0000" ], p)

[<Fact>]
let ``addToPalette bỏ qua màu trùng (không phân biệt hoa thường)`` () =
    let p = ColorSelector.addToPalette [ "#0000FF"; "#FF0000" ] "#0000ff"
    Assert.Equal<string list>([ "#0000FF"; "#FF0000" ], p)

[<Fact>]
let ``addToPalette giữ tối đa 8 màu và cắt màu cũ nhất`` () =
    let p = ColorSelector.addToPalette ColorSelector.PresetColors "#123456"
    Assert.Equal(8, List.length p)
    Assert.Equal("#123456", List.head p)
    Assert.False(p |> List.contains "#FF69B4")

// ==========================================================================
// 4. Cấu trúc UI (headless)
// ==========================================================================

/// Tạo một ColorSelector mới trên luồng UI headless, trả về nó cho hàm kiểm tra.
let private withSelector (check: ColorSelector -> bool * string) : bool * string =
    HeadlessTestSession.session.Value
        .Dispatch(
            (fun () ->
                let selector = ColorSelector()
                let window = Window()
                window.Content <- selector
                window.Show()
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Normal)
                check selector),
            CancellationToken.None
        )
        .GetAwaiter()
        .GetResult()

let private assertCheck (result: bool) (diag: string) =
    Assert.True(result, diag)

// --- Nút ColorPicker (bánh xe màu) ---

[<Fact>]
let ``Nút ColorPicker tồn tại`` () =
    let ok, diag =
        withSelector (fun s ->
            let btn = s.FindControl<Button>("ColorPickerButton")
            not (isNull btn), "Không tìm thấy ColorPickerButton")
    assertCheck ok diag

[<Fact>]
let ``Nút ColorPicker có title là "ColorPicker"`` () =
    let ok, diag =
        withSelector (fun s ->
            let btn = s.FindControl<Button>("ColorPickerButton")
            let text = if isNull btn then "(null)" else string btn.Content
            text = "ColorPicker", sprintf "Content=%s" text)
    assertCheck ok diag

[<Fact>]
let ``Nút ColorPicker có gắn Flyout chứa bánh xe màu (ColorView)`` () =
    let ok, diag =
        withSelector (fun s ->
            let btn = s.FindControl<Button>("ColorPickerButton")
            if isNull btn then false, "Không tìm thấy ColorPickerButton"
            elif isNull btn.Flyout then false, "ColorPickerButton không có Flyout"
            else
                match btn.Flyout with
                | :? Flyout as f -> (f.Content :? ColorView), "Flyout.Content không phải ColorView"
                | _ -> false, "Flyout sai kiểu")
    assertCheck ok diag

// --- Nút Thêm màu ---

[<Fact>]
let ``Nút Thêm màu tồn tại`` () =
    let ok, diag =
        withSelector (fun s ->
            let btn = s.FindControl<Button>("AddButton")
            not (isNull btn), "Không tìm thấy AddButton")
    assertCheck ok diag

[<Fact>]
let ``Nút Thêm màu có title là "Thêm màu"`` () =
    let ok, diag =
        withSelector (fun s ->
            let btn = s.FindControl<Button>("AddButton")
            let text = if isNull btn then "(null)" else string btn.Content
            text = "Thêm màu", sprintf "Content=%s" text)
    assertCheck ok diag

// --- Ô nhập hex ---

[<Fact>]
let ``Ô nhập hex tồn tại`` () =
    let ok, diag =
        withSelector (fun s ->
            let box = s.FindControl<TextBox>("HexBox")
            not (isNull box), "Không tìm thấy HexBox")
    assertCheck ok diag

[<Fact>]
let ``Ô nhập hex có placeholder mặc định "#RRGGBB"`` () =
    let ok, diag =
        withSelector (fun s ->
            let box = s.FindControl<TextBox>("HexBox")
            let ph = if isNull box then "(null)" else box.PlaceholderText
            ph = "#RRGGBB", sprintf "PlaceholderText=%s" ph)
    assertCheck ok diag

[<Fact>]
let ``Ô nhập hex mặc định trống`` () =
    let ok, diag =
        withSelector (fun s ->
            let box = s.FindControl<TextBox>("HexBox")
            let t = if isNull box then "(null)" else box.Text
            String.IsNullOrEmpty t, sprintf "Text=%s" t)
    assertCheck ok diag

// --- Bảng 8 ô màu ---

[<Fact>]
let ``Bảng ô màu tồn tại`` () =
    let ok, diag =
        withSelector (fun s ->
            let panel = s.FindControl<WrapPanel>("SwatchPanel")
            not (isNull panel), "Không tìm thấy SwatchPanel")
    assertCheck ok diag

[<Fact>]
let ``Bảng ô màu có đúng 8 ô mặc định`` () =
    let ok, diag =
        withSelector (fun s ->
            let panel = s.FindControl<WrapPanel>("SwatchPanel")
            let n = if isNull panel then -1 else panel.Children.Count
            n = 8, sprintf "SwatchPanel.Children.Count=%d" n)
    assertCheck ok diag

[<Fact>]
let ``Palette mặc định là đúng 8 màu cơ bản`` () =
    let ok, diag =
        withSelector (fun s ->
            let p = s.Palette
            p = ColorSelector.PresetColors, sprintf "Palette=%A" p)
    assertCheck ok diag

// --- Roundtrip màu hiện tại ---

[<Fact>]
let ``Thuộc tính Color set/get roundtrip đúng`` () =
    let ok, diag =
        withSelector (fun s ->
            s.Color <- "#123456"
            let got = s.Color
            got = "#123456", sprintf "Color=%s" got)
    assertCheck ok diag

// ==========================================================================
// 5. ColorView (bánh xe màu) được áp dụng theme — không render trắng tinh
// ==========================================================================

/// Tạo ColorView độc lập trên luồng UI, gắn vào Window để ApplyTemplate rồi chạy check.
let private withColorView (check: ColorView -> bool * string) : bool * string =
    HeadlessTestSession.session.Value
        .Dispatch(
            (fun () ->
                let view = ColorView()
                let window = Window()
                window.Content <- view
                window.Show()
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Normal)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Render)
                check view),
            CancellationToken.None
        )
        .GetAwaiter()
        .GetResult()

[<Fact>]
let ``ColorView nhận ControlTheme (template) từ ColorPicker theme — không còn trắng tinh`` () =
    let ok, diag =
        withColorView (fun v ->
            let has = not (isNull v.Template)
            has, sprintf "ColorView.Template=%s" (if has then "đã áp dụng" else "null (trắng tinh)"))
    assertCheck ok diag

[<Fact>]
let ``Template ColorView chứa bánh xe màu ColorSpectrum`` () =
    let ok, diag =
        withColorView (fun v ->
            let spectrum = v.FindDescendantOfType<ColorSpectrum>()
            not (isNull (box spectrum)), sprintf "tìm thấy ColorSpectrum=%b" (not (isNull (box spectrum))))
    assertCheck ok diag

// ==========================================================================
// 6. Highlight ô màu đang chọn trên bảng 8 ô màu
// ==========================================================================

/// Lấy danh sách các nút swatch (Button) trong bảng ô màu của ColorSelector.
let private swatches (s: ColorSelector) : Button list =
    let panel = s.FindControl<WrapPanel>("SwatchPanel")
    if isNull panel then []
    else
        panel.Children
        |> Seq.choose (fun c -> match c with :? Button as b -> Some b | _ -> None)
        |> List.ofSeq

[<Fact>]
let ``Ô màu đang chọn được highlight bằng viền dày (2px)`` () =
    let ok, diag =
        withSelector (fun s ->
            s.Color <- "#FF0000" // màu đầu tiên của PresetColors
            let btns = swatches s
            match btns with
            | first :: _ ->
                let highlighted = btns |> List.filter (fun b -> b.BorderThickness.Left >= 2.0)
                let isFirstHighlighted = first.BorderThickness.Left >= 2.0
                (List.length highlighted) = 1 && isFirstHighlighted,
                sprintf "highlighted=%d, first=%b" (List.length highlighted) isFirstHighlighted
            | [] -> false, "không có swatch nào")
    assertCheck ok diag

[<Fact>]
let ``Chỉ duy nhất một ô màu được highlight tại một thời điểm`` () =
    let ok, diag =
        withSelector (fun s ->
            s.Color <- "#0000FF" // màu thứ 6 (Xanh lam)
            let highlighted = swatches s |> List.filter (fun b -> b.BorderThickness.Left >= 2.0)
            (List.length highlighted) = 1, sprintf "highlighted=%d" (List.length highlighted))
    assertCheck ok diag
