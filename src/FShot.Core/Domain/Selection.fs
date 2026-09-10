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

    /// Kích thước tối thiểu của vùng chọn.
    static member MinWidth = 20.0
    static member MinHeight = 20.0

    /// Bán kính dung sai khi hit-test handle.
    static member HandleTolerance = 6.0

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

    /// Tính vị trí trung tâm của từng handle.
    /// Công thức: góc, giữa cạnh. Xem 03_03_ResizeHandles.md.
    member this.HandleCenters =
        let left = this.Bounds.Left
        let top = this.Bounds.Top
        let right = this.Bounds.Right
        let bottom = this.Bounds.Bottom
        let midX = (left + right) / 2.0
        let midY = (top + bottom) / 2.0

        [
            (TopLeft, { X = left; Y = top })
            (Top, { X = midX; Y = top })
            (TopRight, { X = right; Y = top })
            (Left, { X = left; Y = midY })
            (Right, { X = right; Y = midY })
            (BottomLeft, { X = left; Y = bottom })
            (Bottom, { X = midX; Y = bottom })
            (BottomRight, { X = right; Y = bottom })
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
    member this.FinishInteraction() : Selection =
        match this.State with
        | Moving
        | Resizing _ ->
            if this.IsValid then
                { this with State = Selected }
            else
                { this with State = Idle; Bounds = { X = 0.0; Y = 0.0; Width = 0.0; Height = 0.0 } }
        | _ -> this

    /// Hủy vùng chọn.
    member this.Cancel() =
        Selection.Empty

    /// Áp dụng ràng buộc kích thước tối thiểu và giới hạn trong capture area.
    /// Xem 03_06_Constraints.md.
    member this.ApplyConstraints(captureBounds: Rect) : Selection =
        let clamped =
            let minBounds =
                {
                  this.Bounds with
                      Width = Math.Max(this.Bounds.Width, Selection.MinWidth)
                      Height = Math.Max(this.Bounds.Height, Selection.MinHeight)
                }
            rectClamp captureBounds minBounds

        { this with Bounds = clamped }
