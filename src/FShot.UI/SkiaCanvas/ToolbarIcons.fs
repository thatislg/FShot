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

    /// Arrow icon (viewBox 32x32).
    let arrow =
        "M8 22 Q8 12 18 12 l-4 -4 h10 v10 l-4 -4 Q22 18 14 18 z"

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

    /// Pixelate icon (viewBox 32x32).
    let pixelate =
        "M6 6 h6 a1.5 1.5 0 0 1 1.5 1.5 v6 a1.5 1.5 0 0 1 -1.5 1.5 h-6 a1.5 1.5 0 0 1 -1.5 -1.5 v-6 a1.5 1.5 0 0 1 1.5 -1.5 z " +
        "M13 6 h6 a1.5 1.5 0 0 1 1.5 1.5 v6 a1.5 1.5 0 0 1 -1.5 1.5 h-6 a1.5 1.5 0 0 1 -1.5 -1.5 v-6 a1.5 1.5 0 0 1 1.5 -1.5 z " +
        "M6 13 h6 a1.5 1.5 0 0 1 1.5 1.5 v6 a1.5 1.5 0 0 1 -1.5 1.5 h-6 a1.5 1.5 0 0 1 -1.5 -1.5 v-6 a1.5 1.5 0 0 1 1.5 -1.5 z " +
        "M13 13 h6 a1.5 1.5 0 0 1 1.5 1.5 v6 a1.5 1.5 0 0 1 -1.5 1.5 h-6 a1.5 1.5 0 0 1 -1.5 -1.5 v-6 a1.5 1.5 0 0 1 1.5 -1.5 z"

    /// Icon placeholder (viewBox 32x32).
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
