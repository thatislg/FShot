namespace FShot.UI.SkiaCanvas

open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.VisualTree
open Avalonia.Platform.Storage
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Core.State
open FShot.Core.State.OverlayStateLogic

open System
open System.IO
open System.Runtime.InteropServices
open System.Diagnostics

open Avalonia.Threading
open Avalonia.Controls.ApplicationLifetimes
open FShot.UI
open FShot.UI.Logging
open FShot.UI.SkiaCanvas
open FShot.Platform.Win32.Clipboard

open FShot.Rendering.Skia.Renderers
open SkiaSharp

/// Sự kiện toàn cục báo hiệu người dùng yêu cầu hủy thao tác chụp (nhấn Esc trên overlay).
/// App.axaml.fs (compile sau) sẽ đăng ký để phát thông báo phù hợp với cấu hình.
module CaptureCanvasEvents =
    let abortRequested = Event<unit>()

/// Vị trí đặt toolbar quanh vùng chọn.
type ToolbarPlacement =
    | BottomHorizontal
    | TopHorizontal
    | RightVertical
    | LeftVertical

/// Toolbar đơn giản bám quanh vùng chọn.
/// Tách ra module riêng để tránh xung đột indentation trong class body.
module Toolbar =

    let toolbarButtonSize = 36.0
    let toolbarGap = 4.0
    let toolbarPadding = 4.0
    let toolbarOffset = 8.0
    let groupGap = 12.0

    let tools = [
        SelectionTool
        PencilTool
        LineTool
        ArrowTool
        RectangleTool
        CircleTool
        MarkerTool
        TextTool
        PixelateTool
        IconTool
    ]

    let actions = [
        UndoAction
        RedoAction
        CopyAction
        SaveAction
        CancelAction
    ]

    let toolCount = List.length tools
    let actionCount = List.length actions
    let totalItemCount = toolCount + actionCount

    let hasSeparator = actionCount > 0

    let sizeHorizontal () =
        let count = float totalItemCount
        let width =
            float toolCount * toolbarButtonSize + float (toolCount - 1) * toolbarGap
            + (if hasSeparator then groupGap * 2.0 + 1.0 else 0.0)
            + float actionCount * toolbarButtonSize + float (actionCount - 1) * toolbarGap
            + 2.0 * toolbarPadding
        let height = toolbarButtonSize + 2.0 * toolbarPadding
        Avalonia.Size(width, height)

    let sizeVertical () =
        let count = float totalItemCount
        let width = toolbarButtonSize + 2.0 * toolbarPadding
        let height =
            float toolCount * toolbarButtonSize + float (toolCount - 1) * toolbarGap
            + (if hasSeparator then groupGap * 2.0 + 1.0 else 0.0)
            + float actionCount * toolbarButtonSize + float (actionCount - 1) * toolbarGap
            + 2.0 * toolbarPadding
        Avalonia.Size(width, height)

    let physicalRect (scale: float) (rect: FShot.Core.Geometry.Rect) = {
        X = rect.X * scale
        Y = rect.Y * scale
        Width = rect.Width * scale
        Height = rect.Height * scale
    }

    let choosePlacement
        (selection: FShot.Core.Geometry.Rect)
        (scale: float)
        (canvasWidth: float)
        (canvasHeight: float)
        : ToolbarPlacement =

        let p = physicalRect scale selection
        let hSize = sizeHorizontal()
        let vSize = sizeVertical()

        let fitsBottom = p.Bottom + toolbarOffset + hSize.Height <= canvasHeight
        let fitsTop = p.Y - toolbarOffset - hSize.Height >= 0.0
        let fitsRight = p.Right + toolbarOffset + vSize.Width <= canvasWidth
        let fitsLeft = p.X - toolbarOffset - vSize.Width >= 0.0

        // Nếu vùng chọn hẹp hơn toolbar ngang, ưu tiên dọc.
        let useVertical = p.Width < hSize.Width

        if useVertical then
            if fitsRight then RightVertical
            elif fitsLeft then LeftVertical
            elif fitsBottom then BottomHorizontal
            else TopHorizontal
        else
            if fitsBottom then BottomHorizontal
            elif fitsTop then TopHorizontal
            elif fitsRight then RightVertical
            else LeftVertical

    let bounds
        (scale: float)
        (selection: FShot.Core.Geometry.Rect)
        (canvasWidth: float)
        (canvasHeight: float)
        : Avalonia.Rect =

        let placement = choosePlacement selection scale canvasWidth canvasHeight
        let p = physicalRect scale selection
        let hSize = sizeHorizontal()
        let vSize = sizeVertical()

        match placement with
        | BottomHorizontal ->
            let x = p.X + p.Width / 2.0 - hSize.Width / 2.0
            let y = p.Bottom + toolbarOffset
            Avalonia.Rect(max 0.0 x, y, hSize.Width, hSize.Height)
        | TopHorizontal ->
            let x = p.X + p.Width / 2.0 - hSize.Width / 2.0
            let y = p.Y - toolbarOffset - hSize.Height
            Avalonia.Rect(max 0.0 x, max 0.0 y, hSize.Width, hSize.Height)
        | RightVertical ->
            let x = p.Right + toolbarOffset
            let y = p.Y + p.Height / 2.0 - vSize.Height / 2.0
            Avalonia.Rect(x, max 0.0 y, vSize.Width, vSize.Height)
        | LeftVertical ->
            let x = p.X - toolbarOffset - vSize.Width
            let y = p.Y + p.Height / 2.0 - vSize.Height / 2.0
            Avalonia.Rect(max 0.0 x, max 0.0 y, vSize.Width, vSize.Height)

    type ToolbarHit =
        | ToolHit of ToolKind
        | ActionHit of ToolbarAction

    let itemRect (tb: Avalonia.Rect) (i: int) : Avalonia.Rect =
        let isHorizontal = tb.Width >= tb.Height
        let isAction = i >= toolCount
        let localIndex = if isAction then i - toolCount else i
        let separatorThickness = 1.0
        let toolSpan = float toolCount * toolbarButtonSize + float (toolCount - 1) * toolbarGap
        let offset =
            if isAction then
                toolSpan + groupGap * 2.0 + separatorThickness + float localIndex * (toolbarButtonSize + toolbarGap)
            else
                float localIndex * (toolbarButtonSize + toolbarGap)

        if isHorizontal then
            Avalonia.Rect(tb.X + toolbarPadding + offset, tb.Y + toolbarPadding, toolbarButtonSize, toolbarButtonSize)
        else
            Avalonia.Rect(tb.X + toolbarPadding, tb.Y + toolbarPadding + offset, toolbarButtonSize, toolbarButtonSize)

    let hitTool (tb: Avalonia.Rect) (point: Avalonia.Point) : ToolbarHit option =
        if not (tb.Contains point) then
            None
        else
            let hitIndex =
                [ 0 .. totalItemCount - 1 ]
                |> List.tryFind (fun i -> (itemRect tb i).Contains(point))
            match hitIndex with
            | Some i when i < toolCount ->
                Some (ToolbarHit.ToolHit (List.item i tools))
            | Some i when i >= toolCount && i < totalItemCount ->
                Some (ToolbarHit.ActionHit (List.item (i - toolCount) actions))
            | _ ->
                None

    let draw
        (context: DrawingContext)
        (tb: Avalonia.Rect)
        (currentTool: ToolKind)
        (canUndo: bool)
        (canRedo: bool) =

        // Màu theo Design Tokens (12_01_DesignTokens.md).
        let bgColor = Avalonia.Media.Color.FromArgb(0xFAuy, 0xFFuy, 0xFDuy, 0xF9uy)  // Toolbar.BackgroundColor #FFFDF9, opacity 0.98
        let borderColor = Avalonia.Media.Color.FromRgb(0xE2uy, 0xE8uy, 0xF0uy)          // Toolbar.BorderColor
        let shadowColor = Avalonia.Media.Color.FromArgb(0x1Auy, 0uy, 0uy, 0uy)         // Toolbar.ShadowColor, opacity 10%
        let activeAccent = Avalonia.Media.Color.FromRgb(0x38uy, 0xBDuy, 0xF8uy)      // ToolButton.Active.Background #38BDF8
        let activeBorderColor = Avalonia.Media.Color.FromRgb(0x1Duy, 0x4Euy, 0xD8uy)   // ToolButton.Active.BorderColor #1D4ED8

        let backgroundBrush = new SolidColorBrush(bgColor)
        let borderPen = new Pen(new SolidColorBrush(borderColor), 1.0)
        let shadowBrush = new SolidColorBrush(shadowColor)
        let activeBorderPen = new Pen(new SolidColorBrush(activeBorderColor), 2.0)

        // Bóng đổ mềm (hộp bóng đơn giản, lệch xuống 4px).
        let shadowRect = Avalonia.Rect(tb.X + 2.0, tb.Y + 4.0, tb.Width, tb.Height)
        context.FillRectangle(shadowBrush, shadowRect)
        context.FillRectangle(backgroundBrush, tb)
        context.DrawRectangle(null, borderPen, tb)

        let isHorizontal = tb.Width >= tb.Height

        let drawItem (i: int) (pathOpt: string option) (isActive: bool) (enabled: bool) (label: string) =
            let buttonRect = itemRect tb i

            if isActive && enabled then
                let activeBg = new SolidColorBrush(activeAccent)
                let cornerRadius = 6.0f
                context.FillRectangle(activeBg, buttonRect, cornerRadius)

                let borderRect = Avalonia.Rect(buttonRect.X - 1.0, buttonRect.Y - 1.0, buttonRect.Width + 2.0, buttonRect.Height + 2.0)
                let roundedActiveBorderPen = new Pen(new SolidColorBrush(activeBorderColor), 2.0, lineJoin = PenLineJoin.Round)
                context.DrawRectangle(null, roundedActiveBorderPen, borderRect, float cornerRadius, float cornerRadius)

            match pathOpt with
            | Some pathData ->
                let geometry = StreamGeometry.Parse(pathData)
                let bounds = geometry.Bounds
                FShotLog.write (sprintf "[Toolbar] Drawing icon %s bounds=%A button=%A" label bounds buttonRect)
                use _transform =
                    let iconSize = 24.0
                    let iconOffsetX = buttonRect.Center.X - iconSize / 2.0
                    let iconOffsetY = buttonRect.Center.Y - iconSize / 2.0
                    let scale = iconSize / 32.0
                    let scaleMatrix = Matrix.CreateScale(scale, scale)
                    let translateMatrix = Matrix.CreateTranslation(iconOffsetX, iconOffsetY)
                    context.PushTransform(scaleMatrix * translateMatrix)

                let alpha = if not enabled then 0x66uy else 0xFFuy
                let darkWalnut = Avalonia.Media.Color.FromArgb(alpha, 0x3Duy, 0x2Buy, 0x1Fuy)
                let darkWalnutPen (w: float) = new Pen(new SolidColorBrush(darkWalnut), w, lineCap = PenLineCap.Round, lineJoin = PenLineJoin.Round)
                let whiteHighlightPen (w: float) = new Pen(new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFFuy, 0xFFuy, 0xFFuy)), w, lineCap = PenLineCap.Round)

                match label with
                | "SelectionTool" ->
                    // Khung nét đứt bo tròn 4 góc + 4 nút xoắn tròn xanh bạc hà (#86EFAC)
                    let dashPen = new Pen(new SolidColorBrush(darkWalnut), 1.8, lineCap = PenLineCap.Round, dashStyle = DashStyle.Dash)
                    context.DrawRectangle(null, dashPen, Avalonia.Rect(7.0, 7.0, 18.0, 18.0), 4.0, 4.0)
                    let handleBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0x86uy, 0xEFuy, 0xACuy))
                    let handlePen = darkWalnutPen 1.4
                    let pts = [| (7.0, 7.0); (25.0, 7.0); (7.0, 25.0); (25.0, 25.0) |]
                    for (hx, hy) in pts do
                        context.DrawEllipse(handleBrush, handlePen, Avalonia.Point(hx, hy), 2.2, 2.2)

                | "PencilTool" ->
                    // Thân bút đỏ cam (#FF7A70), đầu ngòi gỗ (#FEF3C7), ngòi chì (#3D2B1F), mắt chibi & má hồng (#F472B6)
                    let bodyBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFFuy, 0x7Auy, 0x70uy))
                    let bodyGeom = StreamGeometry.Parse("M10 11 C 10 8, 12 6, 16 6 C 20 6, 22 8, 22 11 L 22 19 L 10 19 Z")
                    context.DrawGeometry(bodyBrush, darkWalnutPen 1.8, bodyGeom)

                    let woodBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFEuy, 0xF3uy, 0xC7uy))
                    let woodGeom = StreamGeometry.Parse("M10 19 L 16 26 L 22 19 Z")
                    context.DrawGeometry(woodBrush, darkWalnutPen 1.8, woodGeom)

                    let leadBrush = new SolidColorBrush(darkWalnut)
                    let leadGeom = StreamGeometry.Parse("M14 23.7 L 16 26 L 18 23.7 Z")
                    context.DrawGeometry(leadBrush, null, leadGeom)

                    // Mắt chibi
                    context.DrawEllipse(leadBrush, null, Avalonia.Point(13.5, 12.5), 0.9, 0.9)
                    context.DrawEllipse(leadBrush, null, Avalonia.Point(18.5, 12.5), 0.9, 0.9)
                    // Miệng cười
                    let mouthGeom = StreamGeometry.Parse("M15 14 Q 16 15.2 17 14")
                    context.DrawGeometry(null, darkWalnutPen 0.8, mouthGeom)
                    // Má hồng
                    let blushBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xF4uy, 0x72uy, 0xB6uy))
                    context.DrawEllipse(blushBrush, null, Avalonia.Point(11.5, 14.0), 0.8, 0.8)
                    context.DrawEllipse(blushBrush, null, Avalonia.Point(20.5, 14.0), 0.8, 0.8)

                | "LineTool" ->
                    // Đường thẳng xanh bạc hà (#86EFAC), viền nâu đậm (#3D2B1F), highlight trắng
                    let basePen = new Pen(new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0x86uy, 0xEFuy, 0xACuy)), 4.8, lineCap = PenLineCap.Round)
                    context.DrawLine(basePen, Avalonia.Point(8.0, 24.0), Avalonia.Point(24.0, 8.0))
                    context.DrawLine(darkWalnutPen 1.8, Avalonia.Point(8.0, 24.0), Avalonia.Point(24.0, 8.0))
                    let whiteBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFFuy, 0xFFuy, 0xFFuy))
                    context.DrawEllipse(whiteBrush, null, Avalonia.Point(21.0, 11.0), 1.2, 1.2)

                | "ArrowTool" ->
                    // Thân mũi tên uốn lượn vàng bơ (#FDE047), viền nâu hạt dẻ (#3D2B1F), highlight trắng
                    let bodyBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFDuy, 0xE0uy, 0x47uy))
                    context.DrawGeometry(bodyBrush, darkWalnutPen 1.8, geometry)
                    let highlightGeometry = StreamGeometry.Parse(ToolbarIcons.arrowHighlight)
                    context.DrawGeometry(null, whiteHighlightPen 1.4, highlightGeometry)

                | "RectangleTool" ->
                    // Khung chữ nhật phồng xanh da trời (#7BD5F5), viền nâu (#3D2B1F), highlight cong trắng
                    let puffyPen = new Pen(new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0x7Buy, 0xD5uy, 0xF5uy)), 4.2, lineCap = PenLineCap.Round, lineJoin = PenLineJoin.Round)
                    context.DrawRectangle(null, puffyPen, Avalonia.Rect(5.0, 7.0, 22.0, 18.0), 6.0, 6.0)
                    context.DrawRectangle(null, darkWalnutPen 1.8, Avalonia.Rect(5.0, 7.0, 22.0, 18.0), 6.0, 6.0)
                    let hlGeom = StreamGeometry.Parse("M8 10 C 8 8, 10 8, 13 8")
                    context.DrawGeometry(null, whiteHighlightPen 1.4, hlGeom)

                | "CircleTool" ->
                    // Bánh donut tròn phồng hồng đào (#F472B6), viền nâu (#3D2B1F), highlight cong trắng
                    let donutPen = new Pen(new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xF4uy, 0x72uy, 0xB6uy)), 4.2, lineCap = PenLineCap.Round)
                    context.DrawEllipse(null, donutPen, Avalonia.Point(16.0, 16.0), 10.0, 10.0)
                    context.DrawEllipse(null, darkWalnutPen 1.8, Avalonia.Point(16.0, 16.0), 10.0, 10.0)
                    let hlGeom = StreamGeometry.Parse("M12 9 C 14 7.5, 17 7.5, 19 8.5")
                    context.DrawGeometry(null, whiteHighlightPen 1.4, hlGeom)

                | "MarkerTool" ->
                    // Thân bút dạ quang béo vàng bơ (#FDE047), cổ bút đen, ngòi vát neon (#FACC15), highlight trắng
                    let bodyBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFDuy, 0xE0uy, 0x47uy))
                    context.DrawGeometry(bodyBrush, darkWalnutPen 1.8, geometry)
                    let neckGeom = StreamGeometry.Parse("M10.5 10.5 L 7.5 13.5 L 5 11 L 8 8 Z")
                    context.DrawGeometry(new SolidColorBrush(darkWalnut), null, neckGeom)
                    let tipBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFAuy, 0xCCuy, 0x15uy))
                    let tipGeom = StreamGeometry.Parse("M6 10 L 4 12 L 3 10 L 4.5 8.5 Z")
                    context.DrawGeometry(tipBrush, darkWalnutPen 1.2, tipGeom)
                    let hlGeom = StreamGeometry.Parse("M14 11 L 19 16")
                    context.DrawGeometry(null, whiteHighlightPen 1.4, hlGeom)

                | "TextTool" ->
                    // Chữ 'A' béo tròn màu cam đào (#FDBA74), 2 mắt chibi, 2 má hồng xinh (#F472B6)
                    let bodyBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFDuy, 0xBAuy, 0x74uy))
                    context.DrawGeometry(bodyBrush, darkWalnutPen 1.8, geometry)
                    let darkBrush = new SolidColorBrush(darkWalnut)
                    context.DrawEllipse(darkBrush, null, Avalonia.Point(13.0, 13.0), 0.8, 0.8)
                    context.DrawEllipse(darkBrush, null, Avalonia.Point(19.0, 13.0), 0.8, 0.8)
                    let blushBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xF4uy, 0x72uy, 0xB6uy))
                    context.DrawEllipse(blushBrush, null, Avalonia.Point(11.5, 14.5), 0.7, 0.7)
                    context.DrawEllipse(blushBrush, null, Avalonia.Point(20.5, 14.5), 0.7, 0.7)

                | "PixelateTool" ->
                    // 9 viên kẹo dẻo mosaic tròn góc trong bảng màu pastel kẹo ngọt
                    let blockPen = darkWalnutPen 1.4
                    let colors = [|
                        [| Avalonia.Media.Color.FromArgb(alpha, 0xFFuy, 0x7Auy, 0x70uy); Avalonia.Media.Color.FromArgb(alpha, 0xFDuy, 0xE0uy, 0x47uy); Avalonia.Media.Color.FromArgb(alpha, 0x86uy, 0xEFuy, 0xACuy) |]
                        [| Avalonia.Media.Color.FromArgb(alpha, 0xFDuy, 0xE0uy, 0x47uy); Avalonia.Media.Color.FromArgb(alpha, 0x86uy, 0xEFuy, 0xACuy); Avalonia.Media.Color.FromArgb(alpha, 0x7Buy, 0xD5uy, 0xF5uy) |]
                        [| Avalonia.Media.Color.FromArgb(alpha, 0x86uy, 0xEFuy, 0xACuy); Avalonia.Media.Color.FromArgb(alpha, 0x7Buy, 0xD5uy, 0xF5uy); Avalonia.Media.Color.FromArgb(alpha, 0xFFuy, 0x7Auy, 0x70uy) |]
                    |]
                    let xs = [| 5.0; 13.0; 21.0 |]
                    let ys = [| 5.0; 13.0; 21.0 |]
                    for r in 0 .. 2 do
                        for c in 0 .. 2 do
                            let brush = new SolidColorBrush(colors.[r].[c])
                            context.DrawRectangle(brush, blockPen, Avalonia.Rect(xs.[c], ys.[r], 6.0, 6.0), 2.0, 2.0)

                | "IconTool" ->
                    // Thân nhãn dán ngôi sao bo phồng màu vàng bơ (#FDE047), viền nâu (#3D2B1F)
                    let starBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFDuy, 0xE0uy, 0x47uy))
                    context.DrawGeometry(starBrush, darkWalnutPen 1.8, geometry)

                    // Góc bóc nhãn dán hé ra (Peel Corner) màu hồng pastel (#F472B6)
                    let peelBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xF4uy, 0x72uy, 0xB6uy))
                    let peelGeom = StreamGeometry.Parse(ToolbarIcons.stickerPeelCorner)
                    context.DrawGeometry(peelBrush, darkWalnutPen 1.6, peelGeom)

                    // Khuôn mặt Kawaii (2 mắt chấm + miệng cười)
                    let darkBrush = new SolidColorBrush(darkWalnut)
                    context.DrawEllipse(darkBrush, null, Avalonia.Point(13.0, 14.0), 0.9, 0.9)
                    context.DrawEllipse(darkBrush, null, Avalonia.Point(18.0, 14.0), 0.9, 0.9)
                    let smileGeom = StreamGeometry.Parse("M 14.2 16 Q 15.5 17.2 16.8 16")
                    context.DrawGeometry(null, darkWalnutPen 0.8, smileGeom)

                    // Vệt sáng phản chiếu góc trên bên trái
                    let whiteBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFFuy, 0xFFuy, 0xFFuy))
                    context.DrawEllipse(whiteBrush, null, Avalonia.Point(15.0, 8.5), 1.1, 1.1)

                | "UndoAction" ->
                    // Mũi tên cong móng ngựa tím pastel (#C4B5FD), viền nâu, highlight trắng
                    let bodyBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xC4uy, 0xB5uy, 0xFDuy))
                    context.DrawGeometry(bodyBrush, darkWalnutPen 1.8, geometry)
                    let hlGeom = StreamGeometry.Parse("M8 14 L 11 11.5")
                    context.DrawGeometry(null, whiteHighlightPen 1.2, hlGeom)

                | "RedoAction" ->
                    // Mũi tên cong hướng phải tím pastel (#C4B5FD), viền nâu, highlight trắng
                    let bodyBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xC4uy, 0xB5uy, 0xFDuy))
                    context.DrawGeometry(bodyBrush, darkWalnutPen 1.8, geometry)
                    let hlGeom = StreamGeometry.Parse("M24 14 L 21 11.5")
                    context.DrawGeometry(null, whiteHighlightPen 1.2, hlGeom)

                | "CopyAction" ->
                    // Hai tờ giấy bo góc kẹp nhau: giấy sau vàng bơ (#FEF9C3), giấy trước kem sáng (#FFFDF9), vạch text vàng
                    let backBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFEuy, 0xF9uy, 0xC3uy))
                    context.DrawRectangle(backBrush, darkWalnutPen 1.8, Avalonia.Rect(6.0, 6.0, 14.0, 16.0), 3.0, 3.0)
                    let frontBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFFuy, 0xFDuy, 0xF9uy))
                    context.DrawRectangle(frontBrush, darkWalnutPen 1.8, Avalonia.Rect(12.0, 10.0, 14.0, 16.0), 3.0, 3.0)
                    let linePen = new Pen(new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFDuy, 0xE0uy, 0x47uy)), 1.8, lineCap = PenLineCap.Round)
                    context.DrawLine(linePen, Avalonia.Point(16.0, 15.0), Avalonia.Point(22.0, 15.0))
                    context.DrawLine(linePen, Avalonia.Point(16.0, 19.0), Avalonia.Point(22.0, 19.0))

                | "SaveAction" ->
                    // Đĩa mềm Kawaii xanh bạc hà (#86EFAC), cửa kim loại trắng, khe trượt xanh rừng (#15803D), nhãn dán kem
                    let forestColor = Avalonia.Media.Color.FromArgb(alpha, 0x15uy, 0x80uy, 0x3Duy)
                    let outlinePen = new Pen(new SolidColorBrush(forestColor), 1.8, lineCap = PenLineCap.Round, lineJoin = PenLineJoin.Round)
                    let innerPen = new Pen(new SolidColorBrush(forestColor), 1.4, lineCap = PenLineCap.Round, lineJoin = PenLineJoin.Round)

                    let bodyBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0x86uy, 0xEFuy, 0xACuy))
                    context.DrawRectangle(bodyBrush, outlinePen, Avalonia.Rect(5.0, 5.0, 22.0, 22.0), 4.0, 4.0)

                    let sliderBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFFuy, 0xFFuy, 0xFFuy))
                    context.DrawRectangle(sliderBrush, innerPen, Avalonia.Rect(10.0, 5.0, 12.0, 8.0), 1.5, 1.5)

                    let notchBrush = new SolidColorBrush(forestColor)
                    context.FillRectangle(notchBrush, Avalonia.Rect(17.0, 6.5, 3.0, 5.0))

                    let labelBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFFuy, 0xFDuy, 0xF9uy))
                    context.DrawRectangle(labelBrush, innerPen, Avalonia.Rect(9.0, 16.0, 14.0, 11.0), 1.5, 1.5)

                    let decoPen = new Pen(new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0x86uy, 0xEFuy, 0xACuy)), 1.4, lineCap = PenLineCap.Round)
                    context.DrawLine(decoPen, Avalonia.Point(12.0, 19.0), Avalonia.Point(20.0, 19.0))
                    context.DrawLine(decoPen, Avalonia.Point(12.0, 22.0), Avalonia.Point(20.0, 22.0))

                | "CancelAction" ->
                    // Dấu X phồng béo đỏ dâu tây (#FB7185), viền nâu (#3D2B1F), highlight trắng
                    let xBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFBuy, 0x71uy, 0x85uy))
                    context.DrawGeometry(xBrush, darkWalnutPen 1.8, geometry)
                    let whiteBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(alpha, 0xFFuy, 0xFFuy, 0xFFuy))
                    context.DrawEllipse(whiteBrush, null, Avalonia.Point(16.0, 14.0), 1.2, 1.2)

                | _ ->
                    let iconColor =
                        if not enabled then
                            Avalonia.Media.Color.FromArgb(0x66uy, 0x94uy, 0xA3uy, 0xB8uy)
                        elif isActive then
                            ToolbarIcons.activeIconColor
                        else
                            ToolbarIcons.defaultIconColor

                    let fillBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(0xCCuy, iconColor.R, iconColor.G, iconColor.B))
                    let strokePen = new Pen(new SolidColorBrush(Avalonia.Media.Color.FromArgb(0xE6uy, iconColor.R, iconColor.G, iconColor.B)), 1.5)
                    context.DrawGeometry(fillBrush, strokePen, geometry)
            | None ->
                FShotLog.write (sprintf "[Toolbar] No icon path for %s" label)

        // Vẽ separator giữa 2 nhóm.
        let separatorThickness = 1.0
        let toolSpan = float toolCount * toolbarButtonSize + float (toolCount - 1) * toolbarGap
        let sepOffset = toolSpan + groupGap
        let sepColor = Avalonia.Media.Color.FromRgb(0xE2uy, 0xE8uy, 0xF0uy)
        let sepBrush = new SolidColorBrush(sepColor)
        let sepLength = toolbarButtonSize + toolbarPadding * 2.0 - 8.0
        if isHorizontal then
            let sepX = tb.X + toolbarPadding + sepOffset
            let sepY = tb.Y + (tb.Height - sepLength) / 2.0
            context.FillRectangle(sepBrush, Avalonia.Rect(sepX, sepY, separatorThickness, sepLength))
        else
            let sepX = tb.X + (tb.Width - sepLength) / 2.0
            let sepY = tb.Y + toolbarPadding + sepOffset
            context.FillRectangle(sepBrush, Avalonia.Rect(sepX, sepY, sepLength, separatorThickness))

        tools
        |> List.iteri (fun i tool ->
            let isActive = currentTool = tool
            drawItem i (ToolbarIcons.pathFor tool) isActive true (string tool)
        )

        actions
        |> List.iteri (fun i action ->
            let enabled =
                match action with
                | UndoAction -> canUndo
                | RedoAction -> canRedo
                | _ -> true
            drawItem (toolCount + i) (ToolbarIcons.actionPathFor action) false enabled (string action)
        )

/// Helper vẽ preview Pencil trong UI layer.
module PencilPreview =

    /// Chuyển điểm domain sang Avalonia Point tại tọa độ vật lý.
    let private toAvPoint (scale: float) (p: FShot.Core.Geometry.Point) =
        Avalonia.Point(p.X * scale, p.Y * scale)

    /// Tạo StreamGeometry cho nét Pencil đã làm mịn bằng đường cong bậc hai.
    let buildGeometry (scale: float) (strokeWidth: float) (points: FShot.Core.Geometry.Point list) : Geometry option =
        let minDistance = max (strokeWidth * 0.25) 0.5
        let simplified = PathSmoothing.simplifyPoints minDistance points
        let segments = PathSmoothing.toQuadraticSegments simplified

        if List.isEmpty segments then
            None
        else
            let geometry = new StreamGeometry()
            use context = geometry.Open()
            let (start, ctrl, target) = List.head segments
            context.BeginFigure(toAvPoint scale start, false)
            context.QuadraticBezierTo(toAvPoint scale ctrl, toAvPoint scale target) |> ignore

            for (_, c, e) in List.tail segments do
                context.QuadraticBezierTo(toAvPoint scale c, toAvPoint scale e) |> ignore

            context.EndFigure(false)
            Some (geometry :> Geometry)

/// Custom control vẽ overlay và chuyển input đến OverlayState.
/// Trong PoC này dùng WriteableBitmap để hiển thị screenshot.
/// Selection và annotations được vẽ bằng Avalonia DrawingContext.
/// Xem tài liệu 11_03_OverlayWindow.md, 11_05_InputHandling.md và 11_09_OverlayStateIntegration.md.
type CaptureCanvas() as this =
    inherit Control()

    do
        this.Focusable <- true
        this.Cursor <- new Avalonia.Input.Cursor(StandardCursorType.Cross)

    let mutable captureResult: CaptureResult option = None
    let mutable cachedBitmap: WriteableBitmap option = None
    let mutable overlayState: OverlayState option = None
    let mutable textBox: TextBox option = None

    let mutable frameCount = 0
    let mutable lastFpsUpdate = Stopwatch.GetTimestamp()
    let mutable currentFps = 0.0
    let mutable lastFrameTimeMs = 0.0
    let mutable averageFrameTimeMs = 0.0

    /// Sự kiện khi state thay đổi.
    let stateChanged = Event<unit>()
    member this.StateChanged = stateChanged.Publish

    /// Đặt dữ liệu capture để vẽ.
    /// Tạo WriteableBitmap một lần và cache lại để tránh tạo lại mỗi frame.
    /// Lưu ý: phải gọi InvalidateVisual trên UI thread.
    member this.SetCaptureResult(result: CaptureResult) =
        FShotLog.write (sprintf "[CaptureCanvas] SetCaptureResult: %dx%d" result.Width result.Height)

        cachedBitmap |> Option.iter (fun b ->
            try b.Dispose() with _ -> ()
        )
        cachedBitmap <- None
        captureResult <- Some result

        cachedBitmap <- Some (this.CreateBitmap(result))

        // InvalidateVisual phải chạy trên UI thread.
        Avalonia.Threading.Dispatcher.UIThread.Post(fun () ->
            this.InvalidateVisual()
            FShotLog.write "[CaptureCanvas] InvalidateVisual posted to UI thread"
        )

    /// Đặt OverlayState để control dùng làm single source of truth.
    /// Phải gọi sau khi đã có CaptureResult.
    member this.SetOverlayState(state: OverlayState) =
        FShotLog.write "[CaptureCanvas] SetOverlayState"
        overlayState <- Some state
        Avalonia.Threading.Dispatcher.UIThread.Post(fun () ->
            this.InvalidateVisual()
            stateChanged.Trigger()
        )

    /// Lấy vùng chọn hiện tại từ OverlayState.
    member this.Selection =
        overlayState |> Option.map (fun s -> s.Selection) |> Option.defaultValue Selection.Empty

    /// FPS hiện tại (tính từ số lần render mỗi giây).
    member this.CurrentFps = currentFps

    /// Thời gian render frame gần nhất (ms).
    member this.LastFrameTimeMs = lastFrameTimeMs

    /// Thời gian render frame trung bình (ms).
    member this.AverageFrameTimeMs = averageFrameTimeMs

    /// Đặt lại toàn bộ state.
    member this.Reset() =
        overlayState <-
            overlayState
            |> Option.map (fun s -> { s with Selection = Selection.Empty })
        this.InvalidateVisual()

    /// Chuyển tọa độ pointer sang Virtual Screen space.
    /// Công thức: virtualX = controlX + windowX, virtualY = controlY + windowY.
    /// Xem 11_05_InputHandling.md, mục 4.
    member private this.ToVirtualPoint(e: PointerEventArgs) =
        let pos = e.GetPosition(this)
        let windowPos =
            match this.VisualRoot with
            | :? Window as w -> w.Position
            | _ -> PixelPoint(0, 0)

        {
            X = float pos.X + float windowPos.X
            Y = float pos.Y + float windowPos.Y
        }

    /// Chuyển KeyEventArgs sang chuỗi key dùng trong OverlayEvent.
    /// Format: "Ctrl+Shift+KeyName".
    /// Xem 11_09_OverlayStateIntegration.md, mục 4.2.
    member private this.ToKeyString(e: KeyEventArgs) : string =
        let modifiers = ResizeArray<string>()
        if e.KeyModifiers.HasFlag(KeyModifiers.Control) then modifiers.Add("Ctrl")
        if e.KeyModifiers.HasFlag(KeyModifiers.Shift) then modifiers.Add("Shift")
        if e.KeyModifiers.HasFlag(KeyModifiers.Alt) then modifiers.Add("Alt")

        let keyName = e.Key.ToString()

        if modifiers.Count = 0 then
            keyName
        else
            String.Join("+", modifiers) + "+" + keyName

    /// Thực thi các commands trả về từ OverlayState.
    member private this.ExecuteCommands(commands: OverlayCommand list) =
        for cmd in commands do
            match cmd with
            | CloseOverlay ->
                FShotLog.write "[CaptureCanvas] CloseOverlay command: closing overlay window"
                this.HideTextInput()

                let winOpt =
                    match TopLevel.GetTopLevel(this) with
                    | :? Window as w -> Some w
                    | _ ->
                        match this.VisualRoot with
                        | :? Window as w -> Some w
                        | _ ->
                            match Application.Current.ApplicationLifetime with
                            | :? IClassicDesktopStyleApplicationLifetime as desktop ->
                                Option.ofObj desktop.MainWindow
                            | _ -> None

                match winOpt with
                | Some w ->
                    FShotLog.write (sprintf "[CaptureCanvas] Found window (%s), invoking Close()..." (w.GetType().Name))
                    Dispatcher.UIThread.Post(fun () ->
                        try
                            // Thông báo hủy nếu đang ở chế độ daemon trước khi đóng overlay.
                            CaptureCanvasEvents.abortRequested.Trigger()
                            w.Close()
                        with ex ->
                            FShotLog.writeEx "Failed to close overlay window" ex
                    )
                | None ->
                    FShotLog.write "[CaptureCanvas] Could not find Window to close! Invoking desktop shutdown."
                    match Application.Current.ApplicationLifetime with
                    | :? IClassicDesktopStyleApplicationLifetime as desktop ->
                        Dispatcher.UIThread.Post(fun () -> desktop.Shutdown())
                    | _ -> ()
            | StartExport target ->
                FShotLog.write (sprintf "[CaptureCanvas] StartExport command: %A" target)
                this.ExecuteStartExport(target)
            | ShowTextInput position ->
                FShotLog.write (sprintf "[CaptureCanvas] ShowTextInput command at %A" position)
                this.ShowTextInput(position)
            | HideTextInput ->
                FShotLog.write "[CaptureCanvas] HideTextInput command"
                this.HideTextInput()

    /// Thực hiện xuất ảnh: Save mở SaveFileDialog hoặc lưu tức thì, Copy đưa bitmap vào Clipboard.
    /// Chạy async để không block UI thread; sau khi xong dispatch ExportCompleted.
    member private this.ExecuteStartExport(target: ExportTarget) =
        let topLevel =
            match this.VisualRoot with
            | :? TopLevel as tl -> Some tl
            | _ ->
                match TopLevel.GetTopLevel(this) with
                | null -> None
                | tl -> Some tl

        match captureResult, overlayState, topLevel with
        | Some result, Some state, Some tl ->
            let runExport = async {
                try
                    FShotLog.write (sprintf "[CaptureCanvas] Starting export: target=%A, selection=%A" target state.Selection.Bounds)
                    use exportBitmap = SceneComposer.renderExport result state.Selection (committedAnnotations state)
                    FShotLog.write (sprintf "[CaptureCanvas] exportBitmap rendered: %dx%d" exportBitmap.Width exportBitmap.Height)
                    match target with
                    | SaveToFile (Some specifiedPath) ->
                        // Lưu tức thì không hiện dialog khi đường dẫn đã được chỉ định (FR-OUT-02)
                        let defaultFolder =
                            let pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                            if String.IsNullOrWhiteSpace pictures then
                                Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                            else
                                Path.Combine(pictures, "Screenshots")

                        let resolvedPath = state.Config.SaveOptions.ResolveSavePath(Some specifiedPath, defaultFolder, DateTime.Now)
                        let dir = Path.GetDirectoryName(resolvedPath)
                        if not (String.IsNullOrEmpty dir) && not (Directory.Exists dir) then
                            Directory.CreateDirectory(dir) |> ignore

                        let format =
                            if resolvedPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                               resolvedPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) then
                                SKEncodedImageFormat.Jpeg
                            else
                                SKEncodedImageFormat.Png

                        let quality = state.Config.SaveOptions.NormalizedJpegQuality
                        use data = exportBitmap.Encode(format, quality)
                        use stream = File.Create(resolvedPath)
                        data.SaveTo(stream)
                        stream.Flush()
                        FShotLog.write (sprintf "[CaptureCanvas] Instant save succeeded: %s" resolvedPath)
                        this.Dispatch(ExportCompleted true)

                    | SaveToFile None ->
                        // Nếu config đã có sẵn Path cố định thì lưu tức thì (FR-OUT-02)
                        match state.Config.SaveOptions.Path with
                        | Some configuredPath when not (String.IsNullOrWhiteSpace configuredPath) ->
                            let defaultFolder =
                                let pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                                if String.IsNullOrWhiteSpace pictures then
                                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                                else
                                    Path.Combine(pictures, "Screenshots")

                            let resolvedPath = state.Config.SaveOptions.ResolveSavePath(Some configuredPath, defaultFolder, DateTime.Now)
                            let dir = Path.GetDirectoryName(resolvedPath)
                            if not (String.IsNullOrEmpty dir) && not (Directory.Exists dir) then
                                Directory.CreateDirectory(dir) |> ignore

                            let format =
                                if resolvedPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                   resolvedPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) then
                                    SKEncodedImageFormat.Jpeg
                                else
                                    SKEncodedImageFormat.Png

                            let quality = state.Config.SaveOptions.NormalizedJpegQuality
                            use data = exportBitmap.Encode(format, quality)
                            use stream = File.Create(resolvedPath)
                            data.SaveTo(stream)
                            stream.Flush()
                            FShotLog.write (sprintf "[CaptureCanvas] Instant save (from config) succeeded: %s" resolvedPath)
                            this.Dispatch(ExportCompleted true)

                        | _ ->
                            // Fallback Save As Dialog (FR-OUT-04)
                            let options = FilePickerSaveOptions()
                            let suggestedName = state.Config.SaveOptions.ResolveFileName(DateTime.Now)
                            options.SuggestedFileName <- suggestedName

                            let defaultPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                            if not (String.IsNullOrWhiteSpace defaultPictures) && Directory.Exists defaultPictures then
                                let! folder = tl.StorageProvider.TryGetFolderFromPathAsync(defaultPictures) |> Async.AwaitTask
                                if not (isNull folder) then
                                    options.SuggestedStartLocation <- folder

                            let pngType = FilePickerFileType("PNG Image (*.png)")
                            pngType.Patterns <- ResizeArray[ "*.png" ]
                            let jpgType = FilePickerFileType("JPEG Image (*.jpg; *.jpeg)")
                            jpgType.Patterns <- ResizeArray[ "*.jpg"; "*.jpeg" ]
                            options.FileTypeChoices <- ResizeArray[ pngType; jpgType ]

                            let! file = tl.StorageProvider.SaveFilePickerAsync(options) |> Async.AwaitTask
                            match file with
                            | null ->
                                FShotLog.write "[CaptureCanvas] Save cancelled by user"
                                this.Dispatch(ExportCompleted false)
                            | f ->
                                let localPath =
                                    match f.TryGetLocalPath() with
                                    | null | "" ->
                                        if f.Path.IsFile then f.Path.LocalPath else f.Path.ToString()
                                    | p -> p

                                let format =
                                    if localPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                       localPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) then
                                        SKEncodedImageFormat.Jpeg
                                    else
                                        SKEncodedImageFormat.Png

                                let quality = state.Config.SaveOptions.NormalizedJpegQuality
                                use data = exportBitmap.Encode(format, quality)
                                use! stream = f.OpenWriteAsync() |> Async.AwaitTask
                                data.SaveTo(stream)
                                stream.Flush()
                                FShotLog.write (sprintf "[CaptureCanvas] Saved to %s" localPath)
                                this.Dispatch(ExportCompleted true)

                    | CopyToClipboard ->
                        use data = exportBitmap.Encode(SKEncodedImageFormat.Png, 100)
                        let pngBytes = data.ToArray()

                        // Lấy mảng byte pixel BGRA32 từ exportBitmap
                        let pixelBytes = Array.zeroCreate<byte> (exportBitmap.Width * exportBitmap.Height * 4)
                        let ptr = exportBitmap.GetPixels()
                        if ptr <> IntPtr.Zero then
                            Marshal.Copy(ptr, pixelBytes, 0, pixelBytes.Length)

                        // Ghi vào Win32 Clipboard (CF_DIB + PNG) để OS lưu giữ dữ liệu vĩnh viễn trên hệ thống
                        let win32Ok = ClipboardService.copyImageToClipboard exportBitmap.Width exportBitmap.Height pngBytes pixelBytes
                        FShotLog.write (sprintf "[CaptureCanvas] Win32 Clipboard copy result: %b" win32Ok)

                        // Phát âm thanh thông báo ngắn gọn xác nhận đã copy thành công
                        ClipboardService.playNotificationSound ()

                        FShotLog.write "[CaptureCanvas] Copy completed successfully, dispatching ExportCompleted true"
                        this.Dispatch(ExportCompleted true)
                    | _ ->
                        FShotLog.write (sprintf "[CaptureCanvas] Unsupported export target: %A" target)
                        this.Dispatch(ExportCompleted false)
                with ex ->
                    FShotLog.writeEx "[CaptureCanvas] Export failed" ex
                    this.Dispatch(ExportCompleted false)
            }
            Async.StartImmediate(runExport)
        | _ ->
            FShotLog.write "[CaptureCanvas] Cannot export: missing capture result, overlay state, or toplevel"

    /// Gửi event đến OverlayState, cập nhật state và vẽ lại.
    member private this.Dispatch(event: OverlayEvent) =
        match overlayState with
        | Some state ->
            let result = OverlayStateLogic.update event state
            overlayState <- Some result.State
            this.ExecuteCommands result.Commands
            this.InvalidateVisual()
            stateChanged.Trigger()
        | None ->
            FShotLog.write "[CaptureCanvas] OverlayState not set; event ignored"

    /// Hiển thị TextBox tạm tại vị trí (virtual screen space) để nhập text.
    member private this.ShowTextInput(position: FShot.Core.Geometry.Point) =
        this.HideTextInput()

        let tb = new TextBox()
        tb.AcceptsReturn <- false
        tb.AcceptsTab <- false
        tb.Text <- ""
        tb.FontSize <-
            overlayState
            |> Option.map (fun s -> s.CurrentStyle.FontSize)
            |> Option.defaultValue 14.0

        let color =
            overlayState
            |> Option.map (fun s -> s.CurrentStyle.Color)
            |> Option.defaultValue FShot.Core.Geometry.Color.Red

        let mediaColor = Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B)
        tb.Foreground <- new SolidColorBrush(mediaColor)
        // Caret phải dùng brush riêng; nếu dùng cùng brush với Foreground có thể bị ẩn.
        tb.CaretBrush <- new SolidColorBrush(mediaColor)
        // Nền hơi tối để text nổi bật trên ảnh chụp; vẫn đủ trong suốt để nhìn xuyên.
        tb.Background <- new SolidColorBrush(Avalonia.Media.Color.FromArgb(64uy, 0uy, 0uy, 0uy))
        tb.BorderBrush <- new SolidColorBrush(mediaColor)
        tb.BorderThickness <- Avalonia.Thickness(1.0)
        tb.Padding <- Avalonia.Thickness(4.0)
        tb.MinWidth <- 80.0
        tb.MinHeight <- 28.0
        // Bắt buộc hiển thị và nhận input.
        tb.IsVisible <- true
        tb.IsHitTestVisible <- true
        tb.Focusable <- true

        let scale =
            match captureResult with
            | Some r -> r.ScaleFactor.Value
            | None -> 1.0

        let windowPos =
            match this.VisualRoot with
            | :? Window as w -> w.Position
            | _ -> PixelPoint(0, 0)

        let x = position.X * scale - float windowPos.X
        let y = position.Y * scale - float windowPos.Y
        Canvas.SetLeft(tb, x)
        Canvas.SetTop(tb, y)

        tb.KeyDown.Add(fun e ->
            match e.Key with
            | Key.Enter
            | Key.Return ->
                e.Handled <- true
                this.Dispatch(TextCommitted tb.Text)
            | Key.Escape ->
                e.Handled <- true
                this.Dispatch(Cancel)
            | _ -> ()
        )

        tb.LostFocus.Add(fun _ ->
            if String.IsNullOrWhiteSpace tb.Text then
                this.Dispatch(Cancel)
            else
                this.Dispatch(TextCommitted tb.Text)
        )

        // Ưu tiên: thêm TextBox vào Canvas cha trực tiếp (RootCanvas trong XAML).
        // Nếu vì lý do gì cha không phải Canvas, dùng TopLevel.GetTopLevel để
        // tìm Window và fallback bọc Content trong Canvas tạm thời.
        match this.Parent with
        | :? Canvas as canvas ->
            canvas.Children.Add(tb) |> ignore
            textBox <- Some tb
            tb.Focus() |> ignore
            FShotLog.write "[CaptureCanvas] TextBox added to Canvas (Parent)"
        | _ ->
            FShotLog.write "[CaptureCanvas] ShowTextInput: Parent is not Canvas, using fallback"
            let topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this)
            FShotLog.write (sprintf "[CaptureCanvas] ShowTextInput fallback: TopLevel type = %s" (if isNull topLevel then "null" else topLevel.GetType().Name))

            match topLevel with
            | :? Window as window ->
                let contentTypeName =
                    if isNull window.Content then "null"
                    else window.Content.GetType().Name
                let refEquals = Object.ReferenceEquals(window.Content, this)
                FShotLog.write (sprintf "[CaptureCanvas] ShowTextInput fallback: Window.Content = %s, ReferenceEquals(this) = %b" contentTypeName refEquals)

                match window.Content with
                | :? Canvas as canvas when canvas.Children.Contains(this) ->
                    canvas.Children.Add(tb) |> ignore
                    textBox <- Some tb
                    tb.Focus() |> ignore
                    FShotLog.write "[CaptureCanvas] TextBox added to existing Canvas wrapper"
                | content when Object.ReferenceEquals(content, this) ->
                    let canvas = new Canvas()
                    canvas.Background <- Brushes.Transparent
                    canvas.HorizontalAlignment <- Avalonia.Layout.HorizontalAlignment.Stretch
                    canvas.VerticalAlignment <- Avalonia.Layout.VerticalAlignment.Stretch
                    canvas.Width <- window.Bounds.Width
                    canvas.Height <- window.Bounds.Height

                    window.Content <- null
                    canvas.Children.Add(this) |> ignore
                    this.Width <- canvas.Width
                    this.Height <- canvas.Height
                    canvas.Children.Add(tb) |> ignore
                    textBox <- Some tb
                    window.Content <- canvas
                    tb.Focus() |> ignore
                    FShotLog.write "[CaptureCanvas] TextBox added to new Canvas wrapper (CaptureCanvas as root)"
                | _ ->
                    FShotLog.write "[CaptureCanvas] Window.Content không phải CaptureCanvas hoặc wrapper Canvas"
            | _ ->
                FShotLog.write "[CaptureCanvas] Không tìm thấy Panel/Canvas để thêm TextBox"

    /// Ẩn và xóa TextBox tạm.
    member private this.HideTextInput() =
        match textBox with
        | Some tb ->
            textBox <- None
            try
                match tb.Parent with
                | :? Canvas as canvas ->
                    canvas.Children.Remove(tb) |> ignore
                    // Nếu Canvas này là wrapper tạm thời trong Window, khôi phục lại Window.Content.
                    match canvas.Parent with
                    | :? Window as window when window.Content = (canvas :> obj) && canvas.Children.Contains(this) ->
                        canvas.Children.Remove(this) |> ignore
                        this.Width <- Double.NaN
                        this.Height <- Double.NaN
                        window.Content <- this
                    | _ -> ()
                | :? Panel as panel ->
                    panel.Children.Remove(tb) |> ignore
                | _ -> ()
            with ex ->
                FShotLog.writeEx "HideTextInput failed" ex
        | None -> ()

    override this.OnAttachedToVisualTree(e: Avalonia.VisualTreeAttachmentEventArgs) =
        base.OnAttachedToVisualTree(e)
        FShotLog.write "[CaptureCanvas] Attached to visual tree"
        this.Focus() |> ignore

    override this.OnPointerPressed(e: PointerPressedEventArgs) =
        base.OnPointerPressed(e)

        let avPoint = e.GetPosition(this)
        let point = this.ToVirtualPoint(e)
        let scale =
            match captureResult with
            | Some result -> result.ScaleFactor.Value
            | None -> 1.0

        // Ưu tiên kiểm tra click vào toolbar trước.
        let toolbarBounds =
            overlayState
            |> Option.map buildRenderModel
            |> Option.bind (fun rm -> rm.Selection)
            |> Option.map (fun sel -> Toolbar.bounds scale sel.Bounds this.Bounds.Width this.Bounds.Height)

        match toolbarBounds with
        | Some tb when tb.Contains avPoint ->
            match Toolbar.hitTool tb avPoint with
            | Some hit ->
                match hit with
                | Toolbar.ToolHit tool ->
                    FShotLog.write (sprintf "[CaptureCanvas] Toolbar click at %A -> SelectTool %A" avPoint tool)
                    this.Dispatch(SelectTool tool)
                | Toolbar.ActionHit action ->
                    FShotLog.write (sprintf "[CaptureCanvas] Toolbar click at %A -> ToolbarAction %A" avPoint action)
                    this.Dispatch(ToolbarAction action)
            | None ->
                // Click vào toolbar nhưng không trúng nút; log để debug.
                FShotLog.write (sprintf "[CaptureCanvas] Toolbar click at %A inside tb=%A but hit no button" avPoint tb)
                ()
        | _ ->
            // Đảm bảo control có focus để nhận phím tắt.
            this.Focus() |> ignore

            FShotLog.write (sprintf "[CaptureCanvas] PointerPressed at (%.1f, %.1f)" point.X point.Y)

            // Capture pointer để nhận sự kiện moved/released ngay cả khi chuột ra ngoài control.
            e.Pointer.Capture(this) |> ignore

            this.Dispatch(PointerPressed(point))

    override this.OnPointerMoved(e: PointerEventArgs) =
        base.OnPointerMoved(e)
        let point = this.ToVirtualPoint(e)

        this.Dispatch(PointerMoved(point))

        // Cập nhật con trỏ chuột linh hoạt theo vị trí:
        let sel = this.Selection
        let isInside =
            point.X >= sel.Bounds.Left && point.X <= sel.Bounds.Right
            && point.Y >= sel.Bounds.Top && point.Y <= sel.Bounds.Bottom

        let isSelectionTool =
            match overlayState with
            | Some s -> s.CurrentTool = SelectionTool
            | None -> true

        let cursor =
            match sel.State with
            | Selected when isSelectionTool ->
                match sel.HitTestHandle(point) with
                | Some TopLeft | Some BottomRight -> new Avalonia.Input.Cursor(StandardCursorType.TopLeftCorner)
                | Some TopRight | Some BottomLeft -> new Avalonia.Input.Cursor(StandardCursorType.TopRightCorner)
                | Some Top | Some Bottom -> new Avalonia.Input.Cursor(StandardCursorType.SizeNorthSouth)
                | Some Left | Some Right -> new Avalonia.Input.Cursor(StandardCursorType.SizeWestEast)
                | None ->
                    if isInside then
                        new Avalonia.Input.Cursor(StandardCursorType.SizeAll)
                    else
                        new Avalonia.Input.Cursor(StandardCursorType.Cross)
            | Resizing _ ->
                match sel.HitTestHandle(point) with
                | Some TopLeft | Some BottomRight -> new Avalonia.Input.Cursor(StandardCursorType.TopLeftCorner)
                | Some TopRight | Some BottomLeft -> new Avalonia.Input.Cursor(StandardCursorType.TopRightCorner)
                | Some Top | Some Bottom -> new Avalonia.Input.Cursor(StandardCursorType.SizeNorthSouth)
                | Some Left | Some Right -> new Avalonia.Input.Cursor(StandardCursorType.SizeWestEast)
                | None -> new Avalonia.Input.Cursor(StandardCursorType.Cross)
            | Moving ->
                new Avalonia.Input.Cursor(StandardCursorType.SizeAll)
            | _ ->
                new Avalonia.Input.Cursor(StandardCursorType.Cross)
        this.Cursor <- cursor

        // Giảm log spam: chỉ log moved khi vùng chọn thay đổi đáng kể.
        if sel.State <> SelectionState.Idle then
            if int sel.Bounds.Width % 20 = 0 || int sel.Bounds.Height % 20 = 0 then
                FShotLog.write (sprintf "[CaptureCanvas] PointerMoved -> bounds: %A" sel.Bounds)

    override this.OnPointerReleased(e: PointerReleasedEventArgs) =
        base.OnPointerReleased(e)
        FShotLog.write "[CaptureCanvas] PointerReleased"

        // Release pointer capture.
        e.Pointer.Capture(null) |> ignore

        this.Dispatch(PointerReleased)

        let sel = this.Selection
        let point = this.ToVirtualPoint(e)
        let isInside =
            point.X >= sel.Bounds.Left && point.X <= sel.Bounds.Right
            && point.Y >= sel.Bounds.Top && point.Y <= sel.Bounds.Bottom

        let isSelectionTool =
            match overlayState with
            | Some s -> s.CurrentTool = SelectionTool
            | None -> true

        this.Cursor <-
            match sel.State with
            | Selected ->
                if isInside && isSelectionTool then
                    new Avalonia.Input.Cursor(StandardCursorType.SizeAll)
                else
                    new Avalonia.Input.Cursor(StandardCursorType.Cross)
            | _ ->
                new Avalonia.Input.Cursor(StandardCursorType.Cross)

        match sel.State with
        | Selecting ->
            FShotLog.write (sprintf "[CaptureCanvas] Vùng chọn hoàn tất: %A" sel.Bounds)
        | Moving
        | Resizing _ ->
            FShotLog.write (sprintf "[CaptureCanvas] Tương tác hoàn tất: %A" sel.Bounds)
        | _ -> ()

    override this.OnKeyDown(e: KeyEventArgs) =
        base.OnKeyDown(e)
        let keyString = this.ToKeyString(e)
        let isFocused = this.IsFocused
        FShotLog.write (sprintf "[CaptureCanvas] KeyDown: %s | Key: %A | Focused: %b" keyString e.Key isFocused)

        // Phím tắt chuyển nhanh công cụ annotation và thao tác xuất ảnh.
        // Dùng e.Key (phím vật lý) thay vì chuỗi IME để tránh bị bộ gõ tiếng Việt bắt mất.
        match e.Key with
        | Key.C when e.KeyModifiers.HasFlag(KeyModifiers.Control) ->
            this.Dispatch(Copy)
        | Key.S when e.KeyModifiers.HasFlag(KeyModifiers.Control) ->
            this.Dispatch(Save)
        | Key.P -> this.Dispatch(SelectTool PencilTool)
        | Key.L -> this.Dispatch(SelectTool LineTool)
        | Key.A -> this.Dispatch(SelectTool ArrowTool)
        | Key.R -> this.Dispatch(SelectTool RectangleTool)
        | Key.C -> this.Dispatch(SelectTool CircleTool)
        | Key.M -> this.Dispatch(SelectTool MarkerTool)
        | Key.T -> this.Dispatch(SelectTool TextTool)
        | Key.B -> this.Dispatch(SelectTool PixelateTool)
        | Key.I -> this.Dispatch(SelectTool IconTool)
        | Key.S -> this.Dispatch(SelectTool SelectionTool)
        | Key.Z when e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Shift) ->
            this.Dispatch(Redo)
        | Key.Z when e.KeyModifiers.HasFlag(KeyModifiers.Control) ->
            this.Dispatch(Undo)
        | Key.Y when e.KeyModifiers.HasFlag(KeyModifiers.Control) ->
            this.Dispatch(Redo)
        // Ctrl modifier được gửi riêng để state machine biết khi nào đang khóa tỉ lệ.
        // Sử dụng e.Key thay vì modifier string để tránh IME nuốt mất sự kiện Ctrl khi đang gõ tiếng Việt.
        | Key.LeftCtrl
        | Key.RightCtrl -> this.Dispatch(CtrlModifier true)
        | _ -> this.Dispatch(KeyDown(keyString))

    override this.OnKeyUp(e: KeyEventArgs) =
        base.OnKeyUp(e)
        // Chỉ cần reset Ctrl modifier; các phím tắt tool đã xử lý ở OnKeyDown.
        match e.Key with
        | Key.LeftCtrl
        | Key.RightCtrl -> this.Dispatch(CtrlModifier false)
        | _ -> ()

    /// Tạo WriteableBitmap từ CaptureResult.
    member private this.CreateBitmap(result: CaptureResult) : WriteableBitmap =
        let bitmap = new WriteableBitmap(
            PixelSize(result.Width, result.Height),
            Vector(96.0, 96.0),
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul
        )

        use framebuffer = bitmap.Lock()
        let ptr = framebuffer.Address
        System.Runtime.InteropServices.Marshal.Copy(
            result.Pixels,
            0,
            ptr,
            result.TotalBytes
        )

        bitmap

    /// Cập nhật FPS counter và đo thời gian render.
    /// Lưu ý: FPS chỉ có ý nghĩa khi có tương tác liên tục; khi idle, Avalonia không render.
    member private this.UpdateFps(frameTimeMs: float) =
        frameCount <- frameCount + 1
        let now = Stopwatch.GetTimestamp()
        let elapsedSeconds = float (now - lastFpsUpdate) / float Stopwatch.Frequency

        // Cập nhật trung bình động thời gian render frame.
        if averageFrameTimeMs = 0.0 then
            averageFrameTimeMs <- frameTimeMs
        else
            averageFrameTimeMs <- averageFrameTimeMs * 0.9 + frameTimeMs * 0.1

        lastFrameTimeMs <- frameTimeMs

        if elapsedSeconds >= 1.0 then
            currentFps <- float frameCount / elapsedSeconds
            frameCount <- 0
            lastFpsUpdate <- now

            // Đánh giá hiệu năng dựa trên AvgFrameTime thay vì FPS khi idle.
            let fpsNote =
                if averageFrameTimeMs <= 16.67 then "[PASS]"
                elif averageFrameTimeMs <= 33.33 then "[OK]"
                else "[SLOW]"

            FShotLog.write(
                sprintf "[CaptureCanvas] FPS: %.1f | FrameTime: %.2f ms | AvgFrameTime: %.2f ms %s"
                    currentFps
                    lastFrameTimeMs
                    averageFrameTimeMs
                    fpsNote
            )

    /// Vẽ dimming layer ngoài vùng chọn.
    /// Flameshot-style: nền tối trung tính #000000 với alpha ~70% (180/255).
    /// Trước khi chọn vùng: toàn màn hình được phủ mờ tối êm dịu, không gắt mắt.
    /// Sau khi chọn vùng: 4 strips ngoài vùng chọn được phủ mờ tối; capture region trong suốt sáng rõ.
    /// Xem 11_09_OverlayStateIntegration.md, mục 5.2.
    member private this.RenderDimming(context: DrawingContext, selectionOption: Selection option) =
        let fullBounds = this.Bounds
        // Nền tối Flameshot: đen với alpha 180 (khoảng 70.5%).
        let outerBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(180uy, 0uy, 0uy, 0uy))

        match selectionOption with
        | Some selection when selection.State <> SelectionState.Idle ->
            match captureResult with
            | Some result ->
                let scale = result.ScaleFactor.Value
                let selectionRect =
                    Rect(
                        selection.Bounds.X * scale,
                        selection.Bounds.Y * scale,
                        selection.Bounds.Width * scale,
                        selection.Bounds.Height * scale
                    )

                // Vẽ 4 strips đen mờ xung quanh vùng chọn.
                context.FillRectangle(outerBrush, Rect(0.0, 0.0, fullBounds.Width, selectionRect.Y))
                context.FillRectangle(outerBrush, Rect(0.0, selectionRect.Bottom, fullBounds.Width, fullBounds.Height - selectionRect.Bottom))
                context.FillRectangle(outerBrush, Rect(0.0, selectionRect.Y, selectionRect.X, selectionRect.Height))
                context.FillRectangle(outerBrush, Rect(selectionRect.Right, selectionRect.Y, fullBounds.Width - selectionRect.Right, selectionRect.Height))
            | None -> ()
        | _ ->
            // Chưa có vùng chọn: dimming tối toàn màn hình.
            context.FillRectangle(outerBrush, fullBounds)

    /// Vẽ toolbar đơn giản quanh vùng chọn.
    member private this.RenderToolbar(context: DrawingContext, renderModel: RenderModel) =
        if not renderModel.ToolbarVisible then
            ()
        else
            match renderModel.Selection with
            | None -> ()
            | Some selection ->
                let scale =
                    match captureResult with
                    | Some result -> result.ScaleFactor.Value
                    | None -> 1.0

                let tb = Toolbar.bounds scale selection.Bounds this.Bounds.Width this.Bounds.Height
                Toolbar.draw context tb renderModel.CurrentTool renderModel.CanUndo renderModel.CanRedo

    /// Vẽ vùng chọn và các handle lên overlay theo thiết kế Claymorphism / SVG Knob.
    member private this.RenderSelectionOverlay(context: DrawingContext, selection: Selection) =
        match captureResult with
        | Some result ->
            let scale = result.ScaleFactor.Value
            let selectionRect =
                Rect(
                    selection.Bounds.X * scale,
                    selection.Bounds.Y * scale,
                    selection.Bounds.Width * scale,
                    selection.Bounds.Height * scale
                )

            // Capture region hiển thị desktop gốc rõ; bắt buộc fill brush trong suốt (alpha 0)
            // để Avalonia HitTest nhận diện vùng chọn và bắt trọn các sự kiện chuột khi vẽ annotation.
            let innerBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(0uy, 0uy, 150uy, 255uy))
            context.FillRectangle(innerBrush, selectionRect)

            // Viền ngoài nét liền màu xanh pastel #5FA8D3, độ dày 2.0px.
            let borderColor = Avalonia.Media.Color.FromRgb(95uy, 168uy, 211uy) // #5fa8d3
            let outerPen = new Pen(new SolidColorBrush(borderColor), 2.0)
            context.DrawRectangle(null, outerPen, selectionRect)

            // Viền trong nét đứt: lùi vào 7.0px, nét đứt 10px / 8px bo tròn.
            if selectionRect.Width > 22.0 && selectionRect.Height > 22.0 then
                let innerRect = selectionRect.Inflate(-7.0)
                let dashStyle = new DashStyle([| 10.0; 8.0 |], 0.0)
                let innerPen = new Pen(new SolidColorBrush(borderColor), 2.0, lineCap = PenLineCap.Round, dashStyle = dashStyle)
                context.DrawRectangle(null, innerPen, innerRect)

            // Vẽ 8 knob handle khi vùng đã chọn hoặc đang tương tác.
            let shouldDrawHandles =
                match selection.State with
                | Selected
                | Moving
                | Resizing _ -> true
                | _ -> false

            if shouldDrawHandles then
                let knobRadius = 6.5
                let shadowBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(90uy, 43uy, 109uy, 153uy)) // #2b6d99 alpha 35%
                let knobBrush =
                    let b = new LinearGradientBrush()
                    b.StartPoint <- RelativePoint(0.2, 0.2, RelativeUnit.Relative)
                    b.EndPoint <- RelativePoint(0.8, 0.8, RelativeUnit.Relative)
                    b.GradientStops.Add(GradientStop(Avalonia.Media.Color.FromRgb(191uy, 227uy, 245uy), 0.0)) // #bfe3f5
                    b.GradientStops.Add(GradientStop(Avalonia.Media.Color.FromRgb(107uy, 183uy, 222uy), 1.0)) // #6bb7de
                    b
                let knobPen = new Pen(Brushes.White, 2.5)
                let highlightBrush = Brushes.White

                for (_, center) in selection.HandleCenters do
                    let cx = center.X * scale
                    let cy = center.Y * scale

                    // Drop shadow dưới knob
                    context.DrawEllipse(shadowBrush, null, Avalonia.Point(cx, cy + 1.8), knobRadius, knobRadius)
                    // Thân knob gradient có viền trắng 2.5px
                    context.DrawEllipse(knobBrush, knobPen, Avalonia.Point(cx, cy), knobRadius, knobRadius)
                    // Điểm phản chiếu ánh sáng trắng (specular highlight)
                    context.DrawEllipse(highlightBrush, null, Avalonia.Point(cx - 2.0, cy - 2.0), 1.8, 1.8)
        | None -> ()

    override this.Render(context: DrawingContext) =
        base.Render(context)
        let frameStart = Stopwatch.GetTimestamp()

        // Phủ toàn bộ Bounds bằng brush trong suốt để đảm bảo CaptureCanvas luôn bắt 100% sự kiện chuột
        // trên toàn bộ màn hình, tránh việc click chuột bị lọt xuống ứng dụng phía sau.
        let hitTestBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(0uy, 0uy, 150uy, 255uy))
        context.FillRectangle(hitTestBrush, this.Bounds)

        // Flameshot-style: cửa sổ trong suốt, không vẽ screenshot stub đè lên desktop.
        // Capture result vẫn được lưu để export pipeline sử dụng.
        // Dimming và annotations được vẽ trên nền trong suốt.
        match cachedBitmap with
        | Some _ ->
            // Không vẽ screenshot lên overlay; desktop gốc hiển thị phía sau cửa sổ.
            ()
        | None ->
            // Chưa có capture result: vẽ nền xám nhạt để debug.
            let brush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(32uy, 128uy, 128uy, 128uy))
            context.FillRectangle(brush, this.Bounds)

        // Lấy RenderModel từ OverlayState hiện tại.
        let renderModel =
            overlayState
            |> Option.map buildRenderModel
            |> Option.defaultValue {
                VirtualBounds = { X = 0.0; Y = 0.0; Width = this.Bounds.Width; Height = this.Bounds.Height }
                Selection = None
                Annotations = []
                Preview = None
                ToolbarVisible = false
                CurrentTool = SelectionTool
                CanUndo = false
                CanRedo = false
                Cursor = CursorHint.Crosshair
                TextInput = None
            }

        // Vẽ dimming layer.
        this.RenderDimming(context, renderModel.Selection)

        // Vẽ vùng chọn nếu có.
        renderModel.Selection |> Option.iter (fun sel -> this.RenderSelectionOverlay(context, sel))

        // Xác định hệ số phóng to từ CaptureResult.
        let scale =
            match captureResult with
            | Some result -> result.ScaleFactor.Value
            | None -> 1.0

        // Helpers dùng chung cho việc vẽ annotation lên Avalonia DrawingContext.
        let avaloniaColor (color: Color) =
            Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B)

        let annotationPen (style: AnnotationStyle) : Pen =
            let mediaColor = avaloniaColor style.Color
            let brush = new SolidColorBrush(mediaColor)
            let thickness = style.StrokeWidth.Value * scale
            new Pen(brush, thickness)

        let avPoint (p: Point) = Avalonia.Point(p.X * scale, p.Y * scale)

        // Vẽ một annotation đã commit hoặc đang preview.
        let drawAnnotation (annotation: Annotation) =
            match annotation.Tool with
            | Tool.Pencil points ->
                PencilPreview.buildGeometry scale annotation.Style.StrokeWidth.Value points
                |> Option.iter (fun (geometry: Geometry) ->
                    let pen = annotationPen annotation.Style
                    context.DrawGeometry(null, pen, geometry)
                )

            | Tool.Line (startPoint, endPoint) ->
                let pen = annotationPen annotation.Style
                context.DrawLine(pen, avPoint startPoint, avPoint endPoint)

            | Tool.Arrow (startPoint, endPoint, _) ->
                let pen = annotationPen annotation.Style
                let a = avPoint startPoint
                let b = avPoint endPoint
                context.DrawLine(pen, a, b)

                // Vẽ mũi tên ở đầu B theo cùng công thức hình học với Skia renderer.
                // Dùng Avalonia DrawingContext để preview trên UI có cùng hình dạng với committed.
                let dx = b.X - a.X
                let dy = b.Y - a.Y
                let len = Math.Sqrt(dx * dx + dy * dy)
                if len > 1e-6 then
                    let ux = dx / len
                    let uy = dy / len
                    let arrowLength = float annotation.Style.StrokeWidth.Value * scale * 4.0
                    let theta = Math.PI / 6.0 // 30°
                    let cosTheta = Math.Cos(theta)
                    let sinTheta = Math.Sin(theta)

                    let p1 =
                        Avalonia.Point(
                            float (b.X - arrowLength * (ux * cosTheta - uy * sinTheta)),
                            float (b.Y - arrowLength * (ux * sinTheta + uy * cosTheta))
                        )

                    let p2 =
                        Avalonia.Point(
                            float (b.X - arrowLength * (ux * cosTheta + uy * sinTheta)),
                            float (b.Y - arrowLength * (-ux * sinTheta + uy * cosTheta))
                        )

                    context.DrawLine(pen, b, p1)
                    context.DrawLine(pen, b, p2)

            | Tool.Rectangle (startPoint, endPoint, cornerRadius) ->
                let pen = annotationPen annotation.Style
                // Normalize về góc trên-trái vì người dùng có thể kéo ngược hướng.
                let x = Math.Min(startPoint.X, endPoint.X) * scale
                let y = Math.Min(startPoint.Y, endPoint.Y) * scale
                let w = Math.Abs(endPoint.X - startPoint.X) * scale
                let h = Math.Abs(endPoint.Y - startPoint.Y) * scale
                let rect = Avalonia.Rect(x, y, w, h)
                // Giới hạn bo góc để không vượt quá nửa cạnh ngắn hơn.
                let r = Math.Min(cornerRadius * scale, Math.Min(w / 2.0, h / 2.0))
                context.DrawRectangle(null, pen, rect, r, r)

            | Tool.Circle (startPoint, endPoint, aspectLocked) ->
                let pen = annotationPen annotation.Style
                let x = Math.Min(startPoint.X, endPoint.X)
                let y = Math.Min(startPoint.Y, endPoint.Y)
                let w = Math.Abs(endPoint.X - startPoint.X)
                let h = Math.Abs(endPoint.Y - startPoint.Y)
                // aspectLocked từ Ctrl: dùng cạnh ngắn hơn làm width/height → hình tròn.
                let side = Math.Min(w, h)
                let rect =
                    if aspectLocked then
                        Avalonia.Rect(x * scale, y * scale, side * scale, side * scale)
                    else
                        Avalonia.Rect(x * scale, y * scale, w * scale, h * scale)
                context.DrawEllipse(null, pen, rect.Center, rect.Width / 2.0, rect.Height / 2.0)

            | Tool.Marker points ->
                // Marker preview: nét bán trong suốt, độ dày gấp 3, alpha 35%.
                if not (List.isEmpty points) then
                    let thickness = annotation.Style.StrokeWidth.Value * scale * 3.0
                    let mediaColor = avaloniaColor annotation.Style.Color
                    let markerColor = Avalonia.Media.Color.FromArgb(byte (255.0 * 0.35), mediaColor.R, mediaColor.G, mediaColor.B)
                    let brush = new SolidColorBrush(markerColor)
                    let pen = new Pen(brush, thickness)
                    let start = avPoint (List.head points)
                    let mutable current = start
                    for p in List.tail points do
                        let next = avPoint p
                        context.DrawLine(pen, current, next)
                        current <- next

            | Tool.Pixelate (startPoint, endPoint, _) ->
                // Preview: hình chữ nhật mờ bán trong suốt chỉ vùng sẽ pixelate.
                let x = Math.Min(startPoint.X, endPoint.X) * scale
                let y = Math.Min(startPoint.Y, endPoint.Y) * scale
                let w = Math.Abs(endPoint.X - startPoint.X) * scale
                let h = Math.Abs(endPoint.Y - startPoint.Y) * scale
                let rect = Avalonia.Rect(x, y, w, h)
                let brush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(128uy, 0uy, 0uy, 0uy))
                context.DrawRectangle(brush, null, rect)

            | Tool.Text (position, content, alignment) when not (String.IsNullOrWhiteSpace content) ->
                // Text đã commit: vẽ trực tiếp bằng Avalonia FormattedText.
                let text = content.Trim()
                let fontSize = annotation.Style.FontSize * scale
                if fontSize > 0.0 then
                    let ft =
                        new FormattedText(
                            text,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal),
                            fontSize,
                            new SolidColorBrush(avaloniaColor annotation.Style.Color)
                        )

                    let origin = avPoint position
                    let x =
                        match alignment with
                        | TextAlignment.Left -> origin.X
                        | TextAlignment.Center -> origin.X - ft.Width / 2.0
                        | TextAlignment.Right -> origin.X - ft.Width

                    context.DrawText(ft, Avalonia.Point(x, origin.Y))

            | Tool.Icon (position, width, height, iconId) ->
                // MVP placeholder: vẽ hình chữ nhật nét đứt với chữ "ICON".
                let x = position.X * scale
                let y = position.Y * scale
                let w = width * scale
                let h = height * scale
                let rect = Avalonia.Rect(x, y, w, h)
                let mediaColor = avaloniaColor annotation.Style.Color
                let dashedPen =
                    let dashStyle = new DashStyle([| 10.0; 5.0 |], 0.0)
                    new Pen(new SolidColorBrush(mediaColor), 2.0, DashStyle = dashStyle)
                context.DrawRectangle(null, dashedPen, rect)

                let label = if String.IsNullOrWhiteSpace iconId then "ICON" else iconId
                let fontSize = Math.Min(w / 4.0, h / 4.0)
                if fontSize > 4.0 then
                    let ft =
                        new FormattedText(
                            label,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal),
                            fontSize,
                            new SolidColorBrush(mediaColor)
                        )
                    let textX = rect.Center.X - ft.Width / 2.0
                    let textY = rect.Center.Y - ft.Height / 2.0
                    context.DrawText(ft, Avalonia.Point(textX, textY))

            | _ -> ()

        // Clip annotations bên trong capture region để không tràn ra ngoài vùng chọn.
        // Scope riêng: clip chỉ áp dụng cho annotations, không ảnh hưởng toolbar.
        do
            let clipRect =
                match renderModel.Selection with
                | Some sel ->
                    Rect(sel.Bounds.X * scale, sel.Bounds.Y * scale, sel.Bounds.Width * scale, sel.Bounds.Height * scale)
                | None -> Rect(0.0, 0.0, this.Bounds.Width, this.Bounds.Height)

            use _clip = context.PushClip(clipRect)

            // Vẽ annotations đã commit.
            renderModel.Annotations |> List.iter drawAnnotation

            // Vẽ preview annotation đang vẽ.
            renderModel.Preview |> Option.iter drawAnnotation

        // Vẽ toolbar.
        this.RenderToolbar(context, renderModel)

        // Tính toán và cập nhật FPS.
        let frameEnd = Stopwatch.GetTimestamp()
        let frameTimeMs = float (frameEnd - frameStart) * 1000.0 / float Stopwatch.Frequency
        this.UpdateFps(frameTimeMs)
