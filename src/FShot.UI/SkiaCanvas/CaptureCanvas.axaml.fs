namespace FShot.UI.SkiaCanvas

open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.VisualTree
open FShot.Core.Domain
open FShot.Core.Geometry

/// Custom control vẽ overlay.
/// Trong PoC này dùng WriteableBitmap để hiển thị screenshot.
/// Selection và annotations được vẽ bằng Avalonia shapes đơn giản.
/// Xem tài liệu 11_03_OverlayWindow.md và 11_05_InputHandling.md.
type CaptureCanvas() as this =
    inherit Control()

    let mutable captureResult: CaptureResult option = None
    let mutable selection: Selection = Selection.Empty

    /// Sự kiện khi state thay đổi.
    let stateChanged = Event<unit>()
    member this.StateChanged = stateChanged.Publish

    /// Đặt dữ liệu capture để vẽ.
    member this.SetCaptureResult(result: CaptureResult) =
        captureResult <- Some result
        this.InvalidateVisual()

    /// Lấy vùng chọn hiện tại.
    member this.Selection = selection

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

    override this.OnPointerPressed(e: PointerPressedEventArgs) =
        base.OnPointerPressed(e)
        let point = this.ToVirtualPoint(e)

        match selection.State with
        | Idle ->
            selection <- Selection.StartSelecting(point)
        | Selected ->
            match selection.HitTestHandle(point) with
            | Some handle ->
                selection <- selection.StartResizing handle point
            | None ->
                if selection.Contains(point) then
                    selection <- selection.StartMoving(point)
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

    override this.OnPointerReleased(e: PointerReleasedEventArgs) =
        base.OnPointerReleased(e)

        match selection.State with
        | Selecting
        | Moving
        | Resizing _ ->
            selection <- selection.FinishInteraction()
            this.InvalidateVisual()
            stateChanged.Trigger()
        | _ -> ()

    override this.OnKeyDown(e: KeyEventArgs) =
        base.OnKeyDown(e)

        match e.Key with
        | Key.Escape ->
            selection <- Selection.Empty
            this.InvalidateVisual()
            stateChanged.Trigger()
        | Key.Enter ->
            // TODO: trigger export
            ()
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

    override this.Render(context: DrawingContext) =
        base.Render(context)

        match captureResult with
        | Some result ->
            // Vẽ screenshot nền.
            use bitmap = this.CreateBitmap(result)
            let rect = Rect(0.0, 0.0, this.Bounds.Width, this.Bounds.Height)
            context.DrawImage(bitmap, rect)

            // Vẽ vùng chọn (stub: chỉ vẽ border màu trắng).
            if selection.State <> Idle then
                let scale = result.ScaleFactor.Value
                let selectionRect =
                    Rect(
                        selection.Bounds.X * scale,
                        selection.Bounds.Y * scale,
                        selection.Bounds.Width * scale,
                        selection.Bounds.Height * scale
                    )

                let pen = new Pen(Brushes.White, 2.0)
                context.DrawRectangle(null, pen, selectionRect)
        | None ->
            // Chưa có capture result: vẽ nền xám.
            let brush = new SolidColorBrush(Colors.DarkGray)
            context.FillRectangle(brush, this.Bounds)
