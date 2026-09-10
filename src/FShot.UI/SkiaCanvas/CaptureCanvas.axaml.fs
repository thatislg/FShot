namespace FShot.UI.SkiaCanvas

open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.VisualTree
open FShot.Core.Domain
open FShot.Core.Geometry

open System
open System.Diagnostics

open FShot.UI.Logging

/// Custom control vẽ overlay.
/// Trong PoC này dùng WriteableBitmap để hiển thị screenshot.
/// Selection và annotations được vẽ bằng Avalonia DrawingContext.
/// Xem tài liệu 11_03_OverlayWindow.md và 11_05_InputHandling.md.
type CaptureCanvas() as this =
    inherit Control()

    do
        this.Focusable <- true

    let mutable captureResult: CaptureResult option = None
    let mutable cachedBitmap: WriteableBitmap option = None
    let mutable selection: Selection = Selection.Empty

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

    /// Lấy vùng chọn hiện tại.
    member this.Selection = selection

    /// FPS hiện tại (tính từ số lần render mỗi giây).
    member this.CurrentFps = currentFps

    /// Thời gian render frame gần nhất (ms).
    member this.LastFrameTimeMs = lastFrameTimeMs

    /// Thời gian render frame trung bình (ms).
    member this.AverageFrameTimeMs = averageFrameTimeMs

    /// Đặt lại toàn bộ state.
    member this.Reset() =
        selection <- Selection.Empty
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

        match selection.State with
        | Idle ->
            selection <- Selection.StartSelecting(point)
        | Selected ->
            match selection.HitTestHandle(point) with
            | Some handle ->
                selection <- selection.StartResizing handle point
                FShotLog.write (sprintf "[CaptureCanvas] Bắt đầu resize handle: %A" handle)
            | None ->
                if selection.Contains(point) then
                    selection <- selection.StartMoving(point)
                    FShotLog.write "[CaptureCanvas] Bắt đầu di chuyển vùng chọn"
                else
                    selection <- Selection.StartSelecting(point)
        | _ -> ()

        this.InvalidateVisual()
        stateChanged.Trigger()

    override this.OnPointerMoved(e: PointerEventArgs) =
        base.OnPointerMoved(e)
        let point = this.ToVirtualPoint(e)

        match selection.State with
        | Selecting ->
            selection <- selection.UpdateSelecting(point)
        | Moving ->
            selection <- selection.UpdateMoving(point)
        | Resizing _ ->
            selection <- selection.UpdateResizing(point)
        | _ -> ()

        if selection.State <> Idle then
            this.InvalidateVisual()
            stateChanged.Trigger()
            // Giảm log spam: chỉ log moved khi vùng chọn thay đổi đáng kể.
            if int selection.Bounds.Width % 20 = 0 || int selection.Bounds.Height % 20 = 0 then
                FShotLog.write (sprintf "[CaptureCanvas] PointerMoved -> bounds: %A" selection.Bounds)

    override this.OnPointerReleased(e: PointerReleasedEventArgs) =
        base.OnPointerReleased(e)
        FShotLog.write "[CaptureCanvas] PointerReleased"

        // Release pointer capture.
        e.Pointer.Capture(null) |> ignore

        match selection.State with
        | Selecting ->
            selection <- selection.FinishSelecting()
            FShotLog.write (sprintf "[CaptureCanvas] Vùng chọn hoàn tất: %A" selection.Bounds)
            this.InvalidateVisual()
            stateChanged.Trigger()
        | Moving
        | Resizing _ ->
            selection <- selection.FinishInteraction()
            FShotLog.write (sprintf "[CaptureCanvas] Tương tác hoàn tất: %A" selection.Bounds)
            this.InvalidateVisual()
            stateChanged.Trigger()
        | _ -> ()

    override this.OnKeyDown(e: KeyEventArgs) =
        base.OnKeyDown(e)
        FShotLog.write (sprintf "[CaptureCanvas] KeyDown: %A" e.Key)

        match e.Key with
        | Key.Escape ->
            // Esc luôn thoát app trong PoC, bất kể có vùng chọn hay không.
            FShotLog.write "[CaptureCanvas] Esc pressed - closing overlay"
            match this.VisualRoot with
            | :? Window as w ->
                try w.Close() with ex -> FShotLog.writeEx "Failed to close overlay window" ex
            | _ -> ()
        | Key.Enter ->
            FShotLog.write "[CaptureCanvas] Enter pressed - TODO: trigger export"
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

    /// Vẽ vùng chọn và các handle lên overlay.
    /// Sử dụng pattern Flameshot:
    /// - Dim outer area (tối phần ngoài vùng chọn).
    /// - Fill inner area với màu xanh mờ.
    /// - Border trắng + handles.
    member private this.RenderSelectionOverlay(context: DrawingContext) =
        if selection.State <> Idle then
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

                let fullBounds = this.Bounds
                let outerBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(128uy, 0uy, 0uy, 0uy))
                let innerBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(40uy, 0uy, 150uy, 255uy))

                // Vẽ 4 strips tối xung quanh vùng chọn.
                // Top strip.
                context.FillRectangle(outerBrush, Rect(0.0, 0.0, fullBounds.Width, selectionRect.Y))
                // Bottom strip.
                context.FillRectangle(outerBrush, Rect(0.0, selectionRect.Bottom, fullBounds.Width, fullBounds.Height - selectionRect.Bottom))
                // Left strip.
                context.FillRectangle(outerBrush, Rect(0.0, selectionRect.Y, selectionRect.X, selectionRect.Height))
                // Right strip.
                context.FillRectangle(outerBrush, Rect(selectionRect.Right, selectionRect.Y, fullBounds.Width - selectionRect.Right, selectionRect.Height))

                // Tô màu xanh mờ bên trong vùng chọn.
                context.FillRectangle(innerBrush, selectionRect)

                // Border vùng chọn màu trắng dày 2.5px + bóng mờ đen để nổi.
                let shadowPen = new Pen(Brushes.Black, 5.0)
                let pen = new Pen(Brushes.White, 2.5)
                context.DrawRectangle(null, shadowPen, selectionRect)
                context.DrawRectangle(null, pen, selectionRect)

                // Vẽ 8 handle vuông nhỏ.
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

        // Vẽ overlay vùng chọn.
        this.RenderSelectionOverlay(context)

        // Tính toán và cập nhật FPS.
        let frameEnd = Stopwatch.GetTimestamp()
        let frameTimeMs = float (frameEnd - frameStart) * 1000.0 / float Stopwatch.Frequency
        this.UpdateFps(frameTimeMs)
