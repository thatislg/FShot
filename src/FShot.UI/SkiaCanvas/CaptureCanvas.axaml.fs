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

    let mutable frameCount = 0
    let mutable lastFpsUpdate = Stopwatch.GetTimestamp()
    let mutable currentFps = 0.0
    let mutable lastFrameTimeMs = 0.0
    let mutable averageFrameTimeMs = 0.0

    /// Chuyển điểm domain sang Avalonia Point tại tọa độ vật lý.
    let toAvPoint (scale: float) (p: FShot.Core.Geometry.Point) =
        Avalonia.Point(p.X * scale, p.Y * scale)

    /// Tạo StreamGeometry cho nét Pencil đã làm mịn bằng đường cong bậc hai.
    let buildPencilGeometry (scale: float) (strokeWidth: float) (points: FShot.Core.Geometry.Point list) =
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
            Some geometry

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
            | HideTextInput ->
                FShotLog.write "[CaptureCanvas] HideTextInput command"

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

    override this.OnAttachedToVisualTree(e: Avalonia.VisualTreeAttachmentEventArgs) =
        base.OnAttachedToVisualTree(e)
        FShotLog.write "[CaptureCanvas] Attached to visual tree"
        this.Focus() |> ignore

    override this.OnPointerPressed(e: PointerPressedEventArgs) =
        base.OnPointerPressed(e)
        let point = this.ToVirtualPoint(e)
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
        FShotLog.write (sprintf "[CaptureCanvas] KeyDown: %s" keyString)

        // Phím tắt chuyển nhanh công cụ annotation.
        if keyString.Equals("P", StringComparison.OrdinalIgnoreCase) then
            this.Dispatch(SelectTool PencilTool)
        else
            this.Dispatch(KeyDown(keyString))

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
    /// Trong MVP dùng 4 strips đơn giản bằng Avalonia DrawingContext.
    /// Xem 11_09_OverlayStateIntegration.md, mục 5.2.
    member private this.RenderDimming(context: DrawingContext, selectionOption: Selection option) =
        let fullBounds = this.Bounds
        let outerBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(128uy, 0uy, 0uy, 0uy))

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

                // Vẽ 4 strips tối xung quanh vùng chọn.
                context.FillRectangle(outerBrush, Rect(0.0, 0.0, fullBounds.Width, selectionRect.Y))
                context.FillRectangle(outerBrush, Rect(0.0, selectionRect.Bottom, fullBounds.Width, fullBounds.Height - selectionRect.Bottom))
                context.FillRectangle(outerBrush, Rect(0.0, selectionRect.Y, selectionRect.X, selectionRect.Height))
                context.FillRectangle(outerBrush, Rect(selectionRect.Right, selectionRect.Y, fullBounds.Width - selectionRect.Right, selectionRect.Height))
            | None -> ()
        | _ ->
            // Chưa có vùng chọn: dimming toàn màn hình.
            context.FillRectangle(outerBrush, fullBounds)

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

            let innerBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(40uy, 0uy, 150uy, 255uy))

            // Tô màu xanh mờ bên trong vùng chọn.
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

        match cachedBitmap with
        | Some bitmap ->
            // Vẽ screenshot nền từ bitmap đã cache.
            let rect = Rect(0.0, 0.0, this.Bounds.Width, this.Bounds.Height)
            context.DrawImage(bitmap, rect)
        | None ->
            // Chưa có capture result: vẽ nền xám.
            let brush = new SolidColorBrush(Colors.DarkGray)
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

        // Vẽ annotations đã commit.
        let drawPencilAnnotation (annotation: Annotation) =
            match annotation.Tool with
            | Tool.Pencil points ->
                buildPencilGeometry scale annotation.Style.StrokeWidth.Value points
                |> Option.iter (fun geometry ->
                    let color = annotation.Style.Color
                    let mediaColor = Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B)
                    let brush = new SolidColorBrush(mediaColor)
                    let thickness = annotation.Style.StrokeWidth.Value * scale
                    let pen = new Pen(brush, thickness)
                    context.DrawGeometry(null, pen, geometry)
                )
            | _ -> ()

        renderModel.Annotations |> List.iter drawPencilAnnotation

        // Vẽ preview annotation đang vẽ.
        renderModel.Preview |> Option.iter drawPencilAnnotation

        // Tính toán và cập nhật FPS.
        let frameEnd = Stopwatch.GetTimestamp()
        let frameTimeMs = float (frameEnd - frameStart) * 1000.0 / float Stopwatch.Frequency
        this.UpdateFps(frameTimeMs)
