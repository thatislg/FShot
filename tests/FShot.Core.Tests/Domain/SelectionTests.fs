module FShot.Core.Tests.Domain.SelectionTests

open FShot.Core.Geometry
open FShot.Core.Domain
open Xunit

let private point x y =
    {
      X = x
      Y = y
    }

let private rect x y w h =
    {
      X = x
      Y = y
      Width = w
      Height = h
    }

/// Kiểm tra tạo vùng chọn từ hai điểm.
/// Công thức: X = min(Ax, Bx), Y = min(Ay, By), W = |Bx - Ax|, H = |By - Ay|.
[<Fact>]
let ``Tạo vùng chọn từ hai điểm`` () =
    let sel = Selection.StartSelecting(point 100.0 100.0)
    let updated = sel.UpdateSelecting(point 400.0 300.0)
    let finished = updated.FinishSelecting()
    Assert.Equal(Selected, finished.State)
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(100.0, finished.Bounds.Y)
    Assert.Equal(300.0, finished.Bounds.Width)
    Assert.Equal(200.0, finished.Bounds.Height)

/// Kiểm tra vùng chọn quá nhỏ bị hủy.
[<Fact>]
let ``Vùng chọn quá nhỏ bị hủy`` () =
    let sel = Selection.StartSelecting(point 100.0 100.0)
    let updated = sel.UpdateSelecting(point 105.0 105.0)
    let finished = updated.FinishSelecting()
    Assert.Equal(Idle, finished.State)

/// Kiểm tra di chuyển vùng chọn.
/// Công thức: newX = originalX + dx, newY = originalY + dy.
[<Fact>]
let ``Di chuyển vùng chọn`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let moving = sel.StartMoving(point 150.0 150.0)
    let updated = moving.UpdateMoving(point 200.0 130.0)
    let finished = updated.FinishInteraction()
    Assert.Equal(150.0, finished.Bounds.X)
    Assert.Equal(60.0, finished.Bounds.Y)
    Assert.Equal(300.0, finished.Bounds.Width)
    Assert.Equal(200.0, finished.Bounds.Height)

/// Kiểm tra co giãn bằng handle BottomRight.
/// Công thức: W = mx - X, H = my - Y.
[<Fact>]
let ``Co giãn vùng chọn BottomRight`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let resizing = sel.StartResizing BottomRight (point 400.0 280.0)
    let updated = resizing.UpdateResizing(point 450.0 350.0)
    let finished = updated.FinishInteraction()
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(80.0, finished.Bounds.Y)
    Assert.Equal(350.0, finished.Bounds.Width)
    Assert.Equal(270.0, finished.Bounds.Height)

/// Kiểm tra hit-test handle.
[<Fact>]
let ``Hit-test handle BottomRight`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let handle = sel.HitTestHandle(point 405.0 285.0)
    Assert.Equal(Some BottomRight, handle)

/// Kiểm tra ràng buộc kích thước tối thiểu và giới hạn capture area.
[<Fact>]
let ``Giới hạn vùng chọn trong capture area`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 1800.0 900.0 300.0 200.0 }
    let constrained = sel.ApplyConstraints(rect 0.0 0.0 1920.0 1080.0)
    Assert.Equal(1620.0, constrained.Bounds.X)
    Assert.Equal(880.0, constrained.Bounds.Y)
