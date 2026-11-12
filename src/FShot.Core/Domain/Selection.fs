namespace FShot.Core.Domain

open System
open FShot.Core.Geometry
open FShot.Core.Geometry.Operations

/// Vị trí của 8 điểm neo để co giãn vùng chọn.
/// Xem tài liệu 03_03_ResizeHandles.md.
type ResizeHandle =
    | TopLeft
    | Top
    | TopRight
    | Left
    | Right
    | BottomLeft
    | Bottom
    | BottomRight

/// Trạng thái tương tác của vùng chọn.
/// Xem tài liệu 03_02_SelectionModel.md, mục 2.
type SelectionState =
    | Idle
    | Selecting
    | Selected
    | Moving
    | Resizing of handle: ResizeHandle

/// Vùng chọn trong overlay.
/// Xem tài liệu 03_02_SelectionModel.md.
type Selection =
    {
      Bounds: Rect
      State: SelectionState
      OriginalBounds: Rect
      DragStart: Point
    }

    /// Kích thước tối thiểu của vùng chọn (FR-SEL-08).
    /// 20×20 px đảm bảo đủ diện tích hiển thị 8 handles và toolbar, tránh render 0-pixel.
    static member MinWidth = 20.0
    static member MinHeight = 20.0

    /// Bán kính hiển thị điểm pin (Flameshot slim pin) = HandleSize / 2.0 = 4.0 px.
    /// Xem 03_08_SelectionBorderAndHandles.md, mục 3.2.
    static member HandleRadius = Selection.HandleSize / 2.0

    /// Bán kính dung sai khi hit-test handle.
    /// Xem 03_08_SelectionBorderAndHandles.md, mục 4.1: tolerance 5.0px.
    static member HandleTolerance = 5.0

    /// Kích thước handle (hình vuông nhỏ) để vẽ.
    static member HandleSize = 8.0

    /// Tạo vùng chọn rỗng ở trạng thái Idle.
    static member Empty =
        {
          Bounds = { X = 0.0; Y = 0.0; Width = 0.0; Height = 0.0 }
          State = Idle
          OriginalBounds = { X = 0.0; Y = 0.0; Width = 0.0; Height = 0.0 }
          DragStart = Point.Zero
        }

    /// Kiểm tra vùng chọn có hợp lệ (đủ lớn) hay không.
    member this.IsValid =
        this.Bounds.Width >= Selection.MinWidth
        && this.Bounds.Height >= Selection.MinHeight

    /// Tính vị trí trung tâm của từng handle (điểm pin), đặt lệch ra phía ngoài đường viền.
    /// Công thức: pin nằm hoàn toàn ngoài vùng chọn, tiếp xúc vừa khít với viền.
    /// Xem 03_08_SelectionBorderAndHandles.md, mục 3.3.
    member this.HandleCenters =
        let r = Selection.HandleRadius
        let left = this.Bounds.Left
        let top = this.Bounds.Top
        let right = this.Bounds.Right
        let bottom = this.Bounds.Bottom
        let midX = (left + right) / 2.0
        let midY = (top + bottom) / 2.0

        [
            (TopLeft, { X = left - r; Y = top - r })
            (Top, { X = midX; Y = top - r })
            (TopRight, { X = right + r; Y = top - r })
            (Left, { X = left - r; Y = midY })
            (Right, { X = right + r; Y = midY })
            (BottomLeft, { X = left - r; Y = bottom + r })
            (Bottom, { X = midX; Y = bottom + r })
            (BottomRight, { X = right + r; Y = bottom + r })
        ]

    /// Tìm handle tại vị trí chuột, dùng bán kính dung sai.
    /// Xem 03_03_ResizeHandles.md, mục 3.
    member this.HitTestHandle(point: Point) : ResizeHandle option =
        let halfSize = Selection.HandleSize / 2.0
        let tolerance = Selection.HandleTolerance

        this.HandleCenters
        |> List.tryFind (fun (handle, center) ->
            let handleRect =
                {
                  X = center.X - halfSize
                  Y = center.Y - halfSize
                  Width = Selection.HandleSize
                  Height = Selection.HandleSize
                }
            hitHandle tolerance point handleRect)
        |> Option.map fst

    /// Kiểm tra điểm có nằm trong vùng chọn hay không.
    member this.Contains(point: Point) =
        rectContainsPoint this.Bounds point

    /// Bắt đầu tạo vùng chọn từ điểm nhấn chuột.
    /// Công thức: X = min(Ax, Bx), Y = min(Ay, By), W = |Bx - Ax|, H = |By - Ay|.
    /// Xem 03_02_SelectionModel.md, mục 3.
    static member StartSelecting(start: Point) =
        {
          Selection.Empty with
              State = Selecting
              DragStart = start
              Bounds =
                {
                  X = start.X
                  Y = start.Y
                  Width = 0.0
                  Height = 0.0
                }
        }

    /// Cập nhật vùng chọn khi đang kéo tạo vùng.
    member this.UpdateSelecting(current: Point) : Selection =
        match this.State with
        | Selecting ->
            let newBounds = rectFromPoints this.DragStart current
            { this with Bounds = newBounds }
        | _ -> this

    /// Hoàn tất tạo vùng chọn. Nếu quá nhỏ thì trở về Idle.
    member this.FinishSelecting() : Selection =
        match this.State with
        | Selecting ->
            if this.IsValid then
                { this with State = Selected }
            else
                Selection.Empty
        | _ -> this

    /// Bắt đầu di chuyển vùng chọn.
    /// Xem 03_04_MouseOperations.md, mục 1.1.
    member this.StartMoving(start: Point) : Selection =
        match this.State with
        | Selected ->
            {
              this with
                  State = Moving
                  DragStart = start
                  OriginalBounds = this.Bounds
            }
        | _ -> this

    /// Cập nhật vị trí khi đang di chuyển.
    /// Công thức: newX = originalX + dx, newY = originalY + dy.
    /// Xem 03_04_MouseOperations.md, mục 3.
    member this.UpdateMoving(current: Point) : Selection =
        match this.State with
        | Moving ->
            let dx = current.X - this.DragStart.X
            let dy = current.Y - this.DragStart.Y
            let newBounds =
                {
                  this.OriginalBounds with
                      X = this.OriginalBounds.X + dx
                      Y = this.OriginalBounds.Y + dy
                }
            { this with Bounds = newBounds }
        | _ -> this

    /// Bắt đầu co giãn vùng chọn từ một handle.
    /// Xem 03_04_MouseOperations.md, mục 1.1.
    member this.StartResizing(handle: ResizeHandle) (start: Point) : Selection =
        match this.State with
        | Selected ->
            {
              this with
                  State = Resizing handle
                  DragStart = start
                  OriginalBounds = this.Bounds
            }
        | _ -> this

    /// Cập nhật kích thước khi đang co giãn.
    /// Công thức tùy theo handle. Xem 03_04_MouseOperations.md, mục 2.
    member this.UpdateResizing(current: Point) : Selection =
        match this.State with
        | Resizing handle ->
            let orig = this.OriginalBounds
            let mx = current.X
            let my = current.Y

            let newBounds =
                match handle with
                | TopLeft ->
                    {
                      X = mx
                      Y = my
                      Width = orig.Right - mx
                      Height = orig.Bottom - my
                    }
                | Top ->
                    {
                      orig with
                          Y = my
                          Height = orig.Bottom - my
                    }
                | TopRight ->
                    {
                      X = orig.X
                      Y = my
                      Width = mx - orig.X
                      Height = orig.Bottom - my
                    }
                | Left ->
                    {
                        orig with
                            X = mx
                            Width = orig.Right - mx
                    }
                | Right ->
                    {
                        orig with
                            Width = mx - orig.X
                    }
                | BottomLeft ->
                    {
                      X = mx
                      Y = orig.Y
                      Width = orig.Right - mx
                      Height = my - orig.Y
                    }
                | Bottom ->
                    {
                        orig with
                            Height = my - orig.Y
                    }
                | BottomRight ->
                    {
                        orig with
                            Width = mx - orig.X
                            Height = my - orig.Y
                    }

            { this with Bounds = newBounds }
        | _ -> this

    /// Hoàn tất thao tác move hoặc resize.
    /// Khi co giãn quá nhỏ, ép về kích thước tối thiểu (FR-SEL-08) thay vì hủy vùng
    /// để tránh vùng bị lật ngược (inverted) hoặc biến mất khi kéo handle quá tay.
    member this.FinishInteraction() : Selection =
        match this.State with
        | Moving
        | Resizing _ ->
            let minBounds = Selection.EnsureMinimumSize Selection.MinWidth Selection.MinHeight this.Bounds
            { this with State = Selected; Bounds = minBounds }
        | _ -> this

    /// Hủy vùng chọn.
    member this.Cancel() =
        Selection.Empty

    /// Dịch chuyển vùng chọn đã chọn 1 pixel theo hướng cho trước.
    /// Công thức: X' = X + dx, Y' = Y + dy, W' = W, H' = H.
    /// Xem 03_05_KeyboardOperations.md, mục 2.
    member this.Nudge(dx: float, dy: float) : Selection =
        match this.State with
        | Selected ->
            let newBounds =
                {
                  this.Bounds with
                      X = this.Bounds.X + dx
                      Y = this.Bounds.Y + dy
                }
            { this with Bounds = newBounds }
        | _ -> this

    /// Co giãn vùng chọn đã chọn 1 pixel theo hướng cho trước.
    /// Shift + Arrow thay đổi cạnh ngoài theo hướng phím.
    /// Xem 03_05_KeyboardOperations.md, mục 3.
    member this.KeyboardResize(dx: float, dy: float) : Selection =
        match this.State with
        | Selected ->
            let newBounds =
                if dx < 0.0 then
                    // Shift + Left: kéo cạnh trái ra trái
                    {
                      this.Bounds with
                          X = this.Bounds.X + dx
                          Width = this.Bounds.Width - dx
                    }
                elif dx > 0.0 then
                    // Shift + Right: kéo cạnh phải ra phải
                    {
                      this.Bounds with
                          Width = this.Bounds.Width + dx
                    }
                elif dy < 0.0 then
                    // Shift + Up: kéo cạnh trên lên trên
                    {
                      this.Bounds with
                          Y = this.Bounds.Y + dy
                          Height = this.Bounds.Height - dy
                    }
                else
                    // Shift + Down: kéo cạnh dưới xuống dưới
                    {
                      this.Bounds with
                          Height = this.Bounds.Height + dy
                    }
            { this with Bounds = newBounds }
        | _ -> this

    /// Áp dụng ràng buộc giới hạn trong capture area.
    /// Trong khi đang kéo, chỉ clamp vị trí vào phạm vi chụp;
    /// không ép kích thước tối thiểu để cho phép người dùng thả chuột ở vùng nhỏ.
    /// Kích thước tối thiểu được kiểm tra khi hoàn tất tương tác.
    /// Xem 03_06_Constraints.md.
    member this.ApplyConstraints(captureBounds: Rect) : Selection =
        let clamped = rectClamp captureBounds this.Bounds
        { this with Bounds = clamped }

    /// Đảm bảo hình chữ nhật không nhỏ hơn kích thước tối thiểu (FR-SEL-08).
    /// Công thức: W' = max(W, minW), H' = max(H, minH).
    /// Giữ nguyên góc trên-trái; chỉ mở rộng kích thước nếu nhỏ hơn ngưỡng.
    static member EnsureMinimumSize (minW: float) (minH: float) (rect: Rect) : Rect =
        {
          rect with
              Width = Math.Max(rect.Width, minW)
              Height = Math.Max(rect.Height, minH)
        }

    /// Co giãn đối xứng 2px quanh tâm vùng chọn bằng Ctrl+Shift+Arrow (FR-SEL-07).
    /// Giữ cố định tâm (midX, midY), biến đổi đều 2 cạnh đối diện:
    /// - dx > 0 (Right): X' = X - dx/2, Width' = Width + dx.
    /// - dx < 0 (Left):  X' = X - dx/2 (= X + |dx|/2), Width' = Width + dx.
    /// - dy tương tự cho chiều dọc.
    member this.KeyboardSymmetricResize(dx: float, dy: float) : Selection =
        match this.State with
        | Selected ->
            let newBounds =
                Selection.EnsureMinimumSize Selection.MinWidth Selection.MinHeight
                    {
                      X = this.Bounds.X - dx / 2.0
                      Y = this.Bounds.Y - dy / 2.0
                      Width = this.Bounds.Width + dx
                      Height = this.Bounds.Height + dy
                    }
            { this with Bounds = newBounds }
        | _ -> this

    /// Co giãn đối xứng qua tâm khi giữ Shift kéo handle (Mirror Resize, FR-SEL-16).
    /// Tâm đối xứng ban đầu C = (X0 + W0/2, Y0 + H0/2) được giữ nguyên;
    /// cạnh/góc đối diện dịch chuyển đối xứng qua C khi kéo handle.
    member this.UpdateResizingSymmetric(handle: ResizeHandle) (current: Point) : Selection =
        match this.State with
        | Resizing _ ->
            let orig = this.OriginalBounds
            let centerX = orig.X + orig.Width / 2.0
            let centerY = orig.Y + orig.Height / 2.0
            let mx = current.X
            let my = current.Y

            // Bán kính (nửa kích thước) mới tính từ tâm C tới handle đang kéo.
            let halfW, halfH =
                match handle with
                | BottomRight -> (mx - centerX, my - centerY)
                | TopLeft -> (centerX - mx, centerY - my)
                | TopRight -> (mx - centerX, centerY - my)
                | BottomLeft -> (centerX - mx, my - centerY)
                | Right -> (mx - centerX, orig.Height / 2.0)
                | Left -> (centerX - mx, orig.Height / 2.0)
                | Top -> (orig.Width / 2.0, centerY - my)
                | Bottom -> (orig.Width / 2.0, my - centerY)

            let symmetric =
                {
                  X = centerX - halfW
                  Y = centerY - halfH
                  Width = halfW * 2.0
                  Height = halfH * 2.0
                }

            { this with Bounds = symmetric }
        | _ -> this

    /// Chọn toàn bộ màn hình chụp (FR-SEL-10).
    /// Tạo vùng chọn mới phủ trọn `virtualBounds` và chuyển sang Selected.
    static member SelectAll(virtualBounds: Rect) : Selection =
        {
          Selection.Empty with
              State = Selected
              Bounds = virtualBounds
        }
