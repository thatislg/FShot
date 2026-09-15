namespace FShot.UI.SkiaCanvas

open Avalonia.Media

/// Icon path data cho toolbar (Kawaii Lite style, viewBox 32x32).
/// Mỗi icon là chuỗi SVG path data có thể parse thành StreamGeometry.
module ToolbarIcons =

    /// Selection/Crop icon (viewBox 32x32).
    let selection =
        "M8 4 h16 a4 4 0 0 1 4 4 v16 a4 4 0 0 1 -4 4 h-16 a4 4 0 0 1 -4 -4 v-16 a4 4 0 0 1 4 -4 z M8 8 v16 h16 v-16 z"

    /// Pencil icon (viewBox 32x32).
    let pencil =
        "M7 23 l12 -12 l4 4 -12 12 z M7 23 l-3 6 6 -3 z"

    /// Arrow icon (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/arrow.svg).
    let arrow =
        "M7 23 C 7 15, 12 9, 19 9 L 19 6 C 19 5, 21 4, 22.5 5.5 L 28 11 C 29 12, 29 14, 28 15 L 22.5 20.5 C 21 22, 19 21, 19 20 L 19 17 C 14 17, 11 20, 10 24 C 9.5 25.5, 7 25, 7 23 Z"

    /// Specular highlight path cho Arrow icon (từ kawaii/arrow.svg).
    let arrowHighlight = "M21 10 L 24.5 13"

    /// Line icon (viewBox 32x32).
    let line = "M6 25 L25 7"

    /// Rectangle icon (viewBox 32x32).
    let rectangle =
        "M7 6 h18 a3 3 0 0 1 3 3 v14 a3 3 0 0 1 -3 3 h-18 a3 3 0 0 1 -3 -3 v-14 a3 3 0 0 1 3 -3 z M7 9 v14 h18 v-14 z"

    /// Circle icon (viewBox 32x32).
    let circle =
        "M16 6 a10 10 0 0 1 0 20 a10 10 0 0 1 0 -20 z"

    /// Marker icon (viewBox 32x32).
    let marker =
        "M22 6 l4 4 -10 10 -4 -4 z M12 16 l-6 10 10 -6 z"

    /// Text icon (viewBox 32x32).
    let text =
        "M8 7 h16 v4 h-6 v14 h-4 v-14 h-6 z"

    /// Pixelate icon (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/pixelate.svg).
    let pixelate =
        "M7 5 h2 a2 2 0 0 1 2 2 v2 a2 2 0 0 1 -2 2 h-2 a2 2 0 0 1 -2 -2 v-2 a2 2 0 0 1 2 -2 z " +
        "M15 5 h2 a2 2 0 0 1 2 2 v2 a2 2 0 0 1 -2 2 h-2 a2 2 0 0 1 -2 -2 v-2 a2 2 0 0 1 2 -2 z " +
        "M23 5 h2 a2 2 0 0 1 2 2 v2 a2 2 0 0 1 -2 2 h-2 a2 2 0 0 1 -2 -2 v-2 a2 2 0 0 1 2 -2 z " +
        "M7 13 h2 a2 2 0 0 1 2 2 v2 a2 2 0 0 1 -2 2 h-2 a2 2 0 0 1 -2 -2 v-2 a2 2 0 0 1 2 -2 z " +
        "M15 13 h2 a2 2 0 0 1 2 2 v2 a2 2 0 0 1 -2 2 h-2 a2 2 0 0 1 -2 -2 v-2 a2 2 0 0 1 2 -2 z " +
        "M23 13 h2 a2 2 0 0 1 2 2 v2 a2 2 0 0 1 -2 2 h-2 a2 2 0 0 1 -2 -2 v-2 a2 2 0 0 1 2 -2 z " +
        "M7 21 h2 a2 2 0 0 1 2 2 v2 a2 2 0 0 1 -2 2 h-2 a2 2 0 0 1 -2 -2 v-2 a2 2 0 0 1 2 -2 z " +
        "M15 21 h2 a2 2 0 0 1 2 2 v2 a2 2 0 0 1 -2 2 h-2 a2 2 0 0 1 -2 -2 v-2 a2 2 0 0 1 2 -2 z " +
        "M23 21 h2 a2 2 0 0 1 2 2 v2 a2 2 0 0 1 -2 2 h-2 a2 2 0 0 1 -2 -2 v-2 a2 2 0 0 1 2 -2 z"

    /// Icon placeholder (viewBox 32x32).
    let iconTool =
        "M16 5 a11 11 0 0 1 0 22 a11 11 0 0 1 0 -22 z M12 12 h8 v8 h-8 z"

    /// Undo - curved arrow pointing left.
    let undo =
        "M10 18 A10 10 0 0 1 20 8 h4 v4 l6 -6 -6 -6 v4 h-4 A14 14 0 0 0 6 18 z"

    /// Redo - curved arrow pointing right.
    let redo =
        "M22 18 A10 10 0 0 0 12 8 h-4 v4 l-6 -6 6 -6 v4 h4 A14 14 0 0 1 26 18 z"

    /// Copy - two overlapping rectangles.
    let copy =
        "M8 12 h12 v12 h-12 z M12 8 h12 v12 h-2 v-10 h-10 z"

    /// Save - floppy disk (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/save.svg).
    let save =
        "M9 5 h14 a4 4 0 0 1 4 4 v14 a4 4 0 0 1 -4 4 h-14 a4 4 0 0 1 -4 -4 v-14 a4 4 0 0 1 4 -4 z " +
        "M10 5 h12 v8 h-12 z M17 6.5 h3 v5 h-3 z M9 16 h14 v11 h-14 z"

    /// Cancel - X cross.
    let cancel =
        "M8 8 l16 16 M24 8 l-16 16"

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

    let actionPathFor (action: FShot.Core.Domain.ToolbarAction) : string option =
        match action with
        | FShot.Core.Domain.UndoAction -> Some undo
        | FShot.Core.Domain.RedoAction -> Some redo
        | FShot.Core.Domain.CopyAction -> Some copy
        | FShot.Core.Domain.SaveAction -> Some save
        | FShot.Core.Domain.CancelAction -> Some cancel

    /// Màu icon mặc định trên nền toolbar kem trắng, theo token ToolButton.Default.IconColor (#3D2B1F).
    let defaultIconColor = Avalonia.Media.Color.FromRgb(0x3Duy, 0x2Buy, 0x1Fuy)

    /// Màu icon khi nút đang active, theo token ToolButton.Active.IconColor (#FFFFFF).
    let activeIconColor = Avalonia.Media.Color.FromRgb(0xFFuy, 0xFFuy, 0xFFuy)

    /// Alpha cho phần fill bên trong icon (80%) để tạo độ mềm, vẫn đủ tương phản với nền toolbar.
    let fillColor (tool: FShot.Core.Domain.ToolKind) : Avalonia.Media.Color =
        // Tất cả tool dùng chung màu icon đồng nhất; chỉ khác biệt ở alpha giữa fill và stroke.
        Avalonia.Media.Color.FromArgb(0xCCuy, 0x3Duy, 0x2Buy, 0x1Fuy)

    /// Alpha cho nét viền icon (90%), theo token ToolButton.Default.IconOpacity.
    let strokeColor (tool: FShot.Core.Domain.ToolKind) : Avalonia.Media.Color =
        Avalonia.Media.Color.FromArgb(0xE6uy, 0x3Duy, 0x2Buy, 0x1Fuy)
