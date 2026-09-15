namespace FShot.UI.SkiaCanvas

open Avalonia.Media

/// Icon path data cho toolbar (Kawaii Lite style, viewBox 32x32).
/// Mỗi icon là chuỗi SVG path data có thể parse thành StreamGeometry.
module ToolbarIcons =

    /// Selection/Crop icon (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/selection.svg).
    let selection =
        "M 11 7 h 10 a 4 4 0 0 1 4 4 v 10 a 4 4 0 0 1 -4 4 h -10 a 4 4 0 0 1 -4 -4 v -10 a 4 4 0 0 1 4 -4 z " +
        "M 7 7 a 2.2 2.2 0 1 0 0.001 0 M 25 7 a 2.2 2.2 0 1 0 0.001 0 M 7 25 a 2.2 2.2 0 1 0 0.001 0 M 25 25 a 2.2 2.2 0 1 0 0.001 0"

    /// Pencil icon (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/pencil.svg).
    let pencil =
        "M10 11 C 10 8, 12 6, 16 6 C 20 6, 22 8, 22 11 L 22 19 L 10 19 Z M10 19 L 16 26 L 22 19 Z"

    /// Arrow icon (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/arrow.svg).
    let arrow =
        "M7 23 C 7 15, 12 9, 19 9 L 19 6 C 19 5, 21 4, 22.5 5.5 L 28 11 C 29 12, 29 14, 28 15 L 22.5 20.5 C 21 22, 19 21, 19 20 L 19 17 C 14 17, 11 20, 10 24 C 9.5 25.5, 7 25, 7 23 Z"

    /// Specular highlight path cho Arrow icon (từ kawaii/arrow.svg).
    let arrowHighlight = "M21 10 L 24.5 13"

    /// Line icon (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/line.svg).
    let line = "M8 24 L24 8"

    /// Rectangle icon (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/rectangle.svg).
    let rectangle =
        "M11 7 h10 a6 6 0 0 1 6 6 v6 a6 6 0 0 1 -6 6 h-10 a6 6 0 0 1 -6 -6 v-6 a6 6 0 0 1 6 -6 z"

    /// Circle icon (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/circle.svg).
    let circle =
        "M16 6 a10 10 0 0 1 0 20 a10 10 0 0 1 0 -20 z"

    /// Marker icon (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/marker.svg).
    let marker =
        "M13 8 L 22 17 C 23.5 18.5, 23.5 21, 22 22.5 L 20 24.5 C 18.5 26, 16 26, 14.5 24.5 L 7.5 17.5 C 6 16, 6 13.5, 7.5 12 Z"

    /// Text icon (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/text.svg).
    let text =
        "M16 5 C 19 5, 21 7, 22 10 L 25 21 C 26 23.5, 24 26, 21.5 26 C 19.5 26, 18.5 24.5, 18 22.5 L 17.5 20 L 14.5 20 L 14 22.5 C 13.5 24.5, 12.5 26, 10.5 26 C 8 26, 6 23.5, 7 21 L 10 10 C 11 7, 13 5, 16 5 Z M 16 11 L 15 16 L 17 16 Z"

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

    /// Pin / Icon placeholder (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/pin.svg).
    let iconTool =
        "M10 9 C 10 7, 12 5, 16 5 C 20 5, 22 7, 22 9 C 22 11, 20 12, 19 14 C 21 16, 23 18, 23 20 C 23 22, 21 23, 16 23 C 11 23, 9 22, 9 20 C 9 18, 11 16, 13 14 C 12 12, 10 11, 10 9 Z M15 22 L 15 28 L 17 28 L 17 22 Z"

    /// Undo (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/undo.svg).
    let undo =
        "M12 9 L 6 14 L 12 19 L 12 15 C 17 15, 23 17, 24 23 C 25 16, 20 10, 12 10 Z"

    /// Redo (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/redo.svg).
    let redo =
        "M20 9 L 26 14 L 20 19 L 20 15 C 15 15, 9 17, 8 23 C 7 16, 12 10, 20 10 Z"

    /// Copy (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/copy.svg).
    let copy =
        "M9 6 h10 a3 3 0 0 1 3 3 v12 a3 3 0 0 1 -3 3 h-10 a3 3 0 0 1 -3 -3 v-12 a3 3 0 0 1 3 -3 z M13 10 h10 a3 3 0 0 1 3 3 v12 a3 3 0 0 1 -3 3 h-10 a3 3 0 0 1 -3 -3 v-12 a3 3 0 0 1 3 -3 z"

    /// Save - floppy disk (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/save.svg).
    let save =
        "M9 5 h14 a4 4 0 0 1 4 4 v14 a4 4 0 0 1 -4 4 h-14 a4 4 0 0 1 -4 -4 v-14 a4 4 0 0 1 4 -4 z " +
        "M10 5 h12 v8 h-12 z M17 6.5 h3 v5 h-3 z M9 16 h14 v11 h-14 z"

    /// Cancel - puffed X (viewBox 32x32, converted from docs/2.Design/12_UIUX_Mock_Penpot/assets/icon/kawaii/cancel.svg).
    let cancel =
        "M9 7 C 8 6, 6 8, 7 9 L 14 16 L 7 23 C 6 24, 8 26, 9 25 L 16 18 L 23 25 C 24 26, 26 24, 25 23 L 18 16 L 25 9 C 26 8, 24 6, 23 7 L 16 14 Z"

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
