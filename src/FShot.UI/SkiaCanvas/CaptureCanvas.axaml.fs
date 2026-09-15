namespace FShot.UI.SkiaCanvas

open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.VisualTree
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Core.State
open FShot.Core.State.OverlayStateLogic

open System
open System.Diagnostics

open FShot.UI.Logging
open FShot.UI.SkiaCanvas

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

    let toolCount = List.length tools

    let sizeHorizontal () =
        let count = float toolCount
        let width = count * toolbarButtonSize + (count - 1.0) * toolbarGap + 2.0 * toolbarPadding
        let height = toolbarButtonSize + 2.0 * toolbarPadding
        Avalonia.Size(width, height)

    let sizeVertical () =
        let count = float toolCount
        let width = toolbarButtonSize + 2.0 * toolbarPadding
        let height = count * toolbarButtonSize + (count - 1.0) * toolbarGap + 2.0 * toolbarPadding
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

    let hitTool (tb: Avalonia.Rect) (point: Avalonia.Point) : ToolKind option =
        if not (tb.Contains point) then
            None
        else
            let isHorizontal = tb.Width >= tb.Height
            let index =
                if isHorizontal then
                    int ((point.X - tb.X - toolbarPadding) / (toolbarButtonSize + toolbarGap))
                else
                    int ((point.Y - tb.Y - toolbarPadding) / (toolbarButtonSize + toolbarGap))

            if index >= 0 && index < toolCount then
                Some (List.item index tools)
            else
                None

    let draw
        (context: DrawingContext)
        (tb: Avalonia.Rect)
        (currentTool: ToolKind) =

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

        tools
        |> List.iteri (fun i tool ->
            let (x, y) =
                if isHorizontal then
                    (tb.X + toolbarPadding + float i * (toolbarButtonSize + toolbarGap),
                     tb.Y + toolbarPadding)
                else
                    (tb.X + toolbarPadding,
                     tb.Y + toolbarPadding + float i * (toolbarButtonSize + toolbarGap))

            let buttonRect = Avalonia.Rect(x, y, toolbarButtonSize, toolbarButtonSize)
            let isActive = currentTool = tool

            // Hover visual chưa có (cần track mouse tách biệt); hiện chỉ active.
            if isActive then
                let activeBg = new SolidColorBrush(activeAccent)
                let cornerRadius = 6.0f
                context.FillRectangle(activeBg, buttonRect, cornerRadius)

                // Vẽ viền active ra ngoài button 1px để cả 2px viền nằm trên nền toolbar,
                // tránh bị nền xanh active làm nhạt màu viền.
                let borderRect = Avalonia.Rect(buttonRect.X - 1.0, buttonRect.Y - 1.0, buttonRect.Width + 2.0, buttonRect.Height + 2.0)
                let roundedActiveBorderPen = new Pen(new SolidColorBrush(activeBorderColor), 2.0, lineJoin = PenLineJoin.Round)
                context.DrawRectangle(null, roundedActiveBorderPen, borderRect, float cornerRadius, float cornerRadius)

            match ToolbarIcons.pathFor tool with
            | Some pathData ->
                let geometry = StreamGeometry.Parse(pathData)
                let bounds = geometry.Bounds
                FShotLog.write (sprintf "[Toolbar] Drawing icon %A bounds=%A button=%A" tool bounds buttonRect)
                use _transform =
                    // Scale từ viewBox 32x32 về kích thước icon trong nút, rồi dịch vào giữa button.
                    // Trong Avalonia điểm được nhân theo row-vector (P * M), nên để scale trước rồi translate
                    // sau thì cần scaleMatrix * translateMatrix.
                    let iconSize = 24.0
                    let iconOffsetX = buttonRect.Center.X - iconSize / 2.0
                    let iconOffsetY = buttonRect.Center.Y - iconSize / 2.0
                    let scale = iconSize / 32.0
                    let scaleMatrix = Matrix.CreateScale(scale, scale)
                    let translateMatrix = Matrix.CreateTranslation(iconOffsetX, iconOffsetY)
                    context.PushTransform(scaleMatrix * translateMatrix)

                // Khi active: icon trắng đậm trên nền accent xanh.
                // Khi default: icon màu nâu đậm (#3D2B1F) theo token ToolButton.Default.IconColor,
                // với fill 80% và stroke 90% opacity để nổi trên nền toolbar kem trắng.
                let fillBrush, strokePen =
                    if isActive then
                        let white = new SolidColorBrush(ToolbarIcons.activeIconColor)
                        white, new Pen(white, 1.5)
                    else
                        new SolidColorBrush(ToolbarIcons.fillColor tool),
                        new Pen(new SolidColorBrush(ToolbarIcons.strokeColor tool), 1.5)

                context.DrawGeometry(fillBrush, strokePen, geometry)
            | None ->
                FShotLog.write (sprintf "[Toolbar] No icon path for tool %A" tool)
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
                FShotLog.write "[CaptureCanvas] CloseOverlay command"
                match this.VisualRoot with
                | :? Window as w ->
                    try w.Close() with ex -> FShotLog.writeEx "Failed to close overlay window" ex
                | _ -> ()
            | StartExport target ->
                FShotLog.write (sprintf "[CaptureCanvas] StartExport command: %A" target)
            | ShowTextInput position ->
                FShotLog.write (sprintf "[CaptureCanvas] ShowTextInput command at %A" position)
                this.ShowTextInput(position)
            | HideTextInput ->
                FShotLog.write "[CaptureCanvas] HideTextInput command"
                this.HideTextInput()

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
            canvas.Children.Add(tb)
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
                    canvas.Children.Add(tb)
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
                    canvas.Children.Add(this)
                    this.Width <- canvas.Width
                    this.Height <- canvas.Height
                    canvas.Children.Add(tb)
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
            | Some tool ->
                FShotLog.write (sprintf "[CaptureCanvas] Toolbar click -> SelectTool %A" tool)
                this.Dispatch(SelectTool tool)
            | None ->
                // Click vào toolbar nhưng không trúng nút; bỏ qua.
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

        // Giảm log spam: chỉ log moved khi vùng chọn thay đổi đáng kể.
        let sel = this.Selection
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

        // Phím tắt chuyển nhanh công cụ annotation.
        // Dùng e.Key (phím vật lý) thay vì chuỗi IME để tránh bị bộ gõ tiếng Việt bắt mất.
        match e.Key with
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
    /// Flameshot-style: màu xanh #45b6f7 với alpha 50%.
    /// Trước khi chọn vùng: toàn màn hình được phủ mờ.
    /// Sau khi chọn vùng: 4 strips ngoài vùng chọn được phủ mờ; capture region trong suốt.
    /// Xem 11_09_OverlayStateIntegration.md, mục 5.2.
    member private this.RenderDimming(context: DrawingContext, selectionOption: Selection option) =
        let fullBounds = this.Bounds
        // #45b6f7 với alpha 128 (~50%).
        let outerBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(128uy, 69uy, 182uy, 247uy))

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

                // Vẽ 4 strips xanh xung quanh vùng chọn.
                context.FillRectangle(outerBrush, Rect(0.0, 0.0, fullBounds.Width, selectionRect.Y))
                context.FillRectangle(outerBrush, Rect(0.0, selectionRect.Bottom, fullBounds.Width, fullBounds.Height - selectionRect.Bottom))
                context.FillRectangle(outerBrush, Rect(0.0, selectionRect.Y, selectionRect.X, selectionRect.Height))
                context.FillRectangle(outerBrush, Rect(selectionRect.Right, selectionRect.Y, fullBounds.Width - selectionRect.Right, selectionRect.Height))
            | None -> ()
        | _ ->
            // Chưa có vùng chọn: dimming xanh toàn màn hình.
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
                Toolbar.draw context tb renderModel.CurrentTool

    /// Vẽ vùng chọn và các handle lên overlay.
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

            // Capture region hiển thị desktop gốc rõ; không tô màu che phủ.
            // Chỉ vẽ viền và handle để đánh dấu vùng chọn.
            let innerBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(0uy, 0uy, 150uy, 255uy))
            context.FillRectangle(innerBrush, selectionRect)

            // Border vùng chọn màu trắng dày 2.5px + bóng mờ đen để nổi.
            let shadowPen = new Pen(Brushes.Black, 5.0)
            let pen = new Pen(Brushes.White, 2.5)
            context.DrawRectangle(null, shadowPen, selectionRect)
            context.DrawRectangle(null, pen, selectionRect)

            // Vẽ 8 handle vuông nhỏ khi vùng đã chọn hoặc đang tương tác.
            let shouldDrawHandles =
                match selection.State with
                | Selected
                | Moving
                | Resizing _ -> true
                | _ -> false

            if shouldDrawHandles then
                let handleHalfSize = Selection.HandleSize / 2.0
                let handleBrush = new SolidColorBrush(Colors.White)
                let handleShadow = new SolidColorBrush(Colors.Black)
                for (_, center) in selection.HandleCenters do
                    let handleRect =
                        Rect(
                            center.X * scale - handleHalfSize,
                            center.Y * scale - handleHalfSize,
                            Selection.HandleSize,
                            Selection.HandleSize
                        )

                    let shadowRect =
                        Rect(
                            center.X * scale - handleHalfSize + 1.0,
                            center.Y * scale - handleHalfSize + 1.0,
                            Selection.HandleSize,
                            Selection.HandleSize
                        )

                    context.FillRectangle(handleShadow, shadowRect)
                    context.FillRectangle(handleBrush, handleRect)
        | None -> ()

    override this.Render(context: DrawingContext) =
        base.Render(context)
        let frameStart = Stopwatch.GetTimestamp()

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
