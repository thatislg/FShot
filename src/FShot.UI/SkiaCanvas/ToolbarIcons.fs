namespace FShot.UI.SkiaCanvas

open Avalonia.Media

/// Icon path data cho toolbar (Kawaii Lite style, viewBox 32x32).
/// Mỗi icon là chuỗi SVG path data có thể parse thành StreamGeometry.
module ToolbarIcons =

    /// Selection/Crop - Mint Macaron fill.
    let selection =
        "M8 4 h16 a4 4 0 0 1 4 4 v16 a4 4 0 0 1 -4 4 h-16 a4 4 0 0 1 -4 -4 v-16 a4 4 0 0 1 4 -4 z M8 8 v16 h16 v-16 z"

    /// Pencil - Coral Red body + Butter Yellow tip.
    let pencil =
        "M7 23 l12 -12 l4 4 -12 12 z M7 23 l-3 6 6 -3 z"

    /// Arrow - Butter Yellow.
    let arrow =
        "M8 22 Q8 12 18 12 l-4 -4 h10 v10 l-4 -4 Q22 18 14 18 z"

    /// Line - Mint Macaron.
    let line = "M6 25 L25 7"

    /// Rectangle - Sky Blue.
    let rectangle =
        "M7 6 h18 a3 3 0 0 1 3 3 v14 a3 3 0 0 1 -3 3 h-18 a3 3 0 0 1 -3 -3 v-14 a3 3 0 0 1 3 -3 z M7 9 v14 h18 v-14 z"

    /// Circle - Pastel Pink.
    let circle =
        "M16 6 a10 10 0 0 1 0 20 a10 10 0 0 1 0 -20 z"

    /// Marker - Yellow.
    let marker =
        "M22 6 l4 4 -10 10 -4 -4 z M12 16 l-6 10 10 -6 z"

    /// Text - Peach Orange.
    let text =
        "M8 7 h16 v4 h-6 v14 h-4 v-14 h-6 z"

    /// Pixelate - multi-color grid (4 rounded squares).
    let pixelate =
        "M6 6 h6 a1.5 1.5 0 0 1 1.5 1.5 v6 a1.5 1.5 0 0 1 -1.5 1.5 h-6 a1.5 1.5 0 0 1 -1.5 -1.5 v-6 a1.5 1.5 0 0 1 1.5 -1.5 z " +
        "M13 6 h6 a1.5 1.5 0 0 1 1.5 1.5 v6 a1.5 1.5 0 0 1 -1.5 1.5 h-6 a1.5 1.5 0 0 1 -1.5 -1.5 v-6 a1.5 1.5 0 0 1 1.5 -1.5 z " +
        "M6 13 h6 a1.5 1.5 0 0 1 1.5 1.5 v6 a1.5 1.5 0 0 1 -1.5 1.5 h-6 a1.5 1.5 0 0 1 -1.5 -1.5 v-6 a1.5 1.5 0 0 1 1.5 -1.5 z " +
        "M13 13 h6 a1.5 1.5 0 0 1 1.5 1.5 v6 a1.5 1.5 0 0 1 -1.5 1.5 h-6 a1.5 1.5 0 0 1 -1.5 -1.5 v-6 a1.5 1.5 0 0 1 1.5 -1.5 z"

    /// Icon placeholder - Lavender sticker.
    let iconTool =
        "M16 5 a11 11 0 0 1 0 22 a11 11 0 0 1 0 -22 z M12 12 h8 v8 h-8 z"

    let pathFor (tool: FShot.Core.Domain.ToolKind) : string option =
        match tool with
        | FShot.Core.Domain.SelectionTool -> Some selection
        | FShot.Core.Domain.PencilTool -> Some pencil
        | FShot.Core.Domain.LineTool -> Some line
        | FShot.Core.Domain.ArrowTool -> Some arrow
        | FShot.Core.Domain.RectangleTool -> Some rectangle
        | FShot.Core.Domain.CircleTool -> Some circle
        | FShot.Core.Domain.MarkerTool -> Some marker
        | FShot.Core.Domain.TextTool -> Some text
        | FShot.Core.Domain.PixelateTool -> Some pixelate
        | FShot.Core.Domain.IconTool -> Some iconTool

    let fillColor (tool: FShot.Core.Domain.ToolKind) : Avalonia.Media.Color =
        match tool with
        | FShot.Core.Domain.SelectionTool -> Avalonia.Media.Color.FromRgb(0x86uy, 0xEFuy, 0xACuy)
        | FShot.Core.Domain.PencilTool -> Avalonia.Media.Color.FromRgb(0xFFuy, 0x7Auy, 0x70uy)
        | FShot.Core.Domain.LineTool -> Avalonia.Media.Color.FromRgb(0x86uy, 0xEFuy, 0xACuy)
        | FShot.Core.Domain.ArrowTool -> Avalonia.Media.Color.FromRgb(0xFDuy, 0xE0uy, 0x47uy)
        | FShot.Core.Domain.RectangleTool -> Avalonia.Media.Color.FromRgb(0x7Buy, 0xD5uy, 0xF5uy)
        | FShot.Core.Domain.CircleTool -> Avalonia.Media.Color.FromRgb(0xF4uy, 0x72uy, 0xB6uy)
        | FShot.Core.Domain.MarkerTool -> Avalonia.Media.Color.FromRgb(0xFAuy, 0xCCuy, 0x15uy)
        | FShot.Core.Domain.TextTool -> Avalonia.Media.Color.FromRgb(0xFDuy, 0xBAuy, 0x74uy)
        | FShot.Core.Domain.PixelateTool -> Avalonia.Media.Color.FromRgb(0xDDuy, 0xD6uy, 0xFEuy)
        | FShot.Core.Domain.IconTool -> Avalonia.Media.Color.FromRgb(0xDDuy, 0xD6uy, 0xFEuy)

    let strokeColor (tool: FShot.Core.Domain.ToolKind) : Avalonia.Media.Color =
        match tool with
        | FShot.Core.Domain.SelectionTool -> Avalonia.Media.Color.FromRgb(0x15uy, 0x80uy, 0x3Duy)
        | FShot.Core.Domain.PencilTool -> Avalonia.Media.Color.FromRgb(0xB9uy, 0x1Cuy, 0x1Cuy)
        | FShot.Core.Domain.LineTool -> Avalonia.Media.Color.FromRgb(0x15uy, 0x80uy, 0x3Duy)
        | FShot.Core.Domain.ArrowTool -> Avalonia.Media.Color.FromRgb(0xB4uy, 0x53uy, 0x09uy)
        | FShot.Core.Domain.RectangleTool -> Avalonia.Media.Color.FromRgb(0x1Duy, 0x4Euy, 0xD8uy)
        | FShot.Core.Domain.CircleTool -> Avalonia.Media.Color.FromRgb(0x9Duy, 0x17uy, 0x4Duy)
        | FShot.Core.Domain.MarkerTool -> Avalonia.Media.Color.FromRgb(0xB4uy, 0x53uy, 0x09uy)
        | FShot.Core.Domain.TextTool -> Avalonia.Media.Color.FromRgb(0x9Auy, 0x34uy, 0x12uy)
        | FShot.Core.Domain.PixelateTool -> Avalonia.Media.Color.FromRgb(0x8Buy, 0x5Cuy, 0xF6uy)
        | FShot.Core.Domain.IconTool -> Avalonia.Media.Color.FromRgb(0x8Buy, 0x5Cuy, 0xF6uy)
