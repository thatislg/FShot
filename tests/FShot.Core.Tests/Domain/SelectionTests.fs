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

let private captureBounds = rect 0.0 0.0 1920.0 1080.0

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

/// Kiểm tra kéo từ phải sang trái vẫn tạo vùng dương.
[<Fact>]
let ``Kéo từ phải sang trái tạo vùng dương`` () =
    let sel = Selection.StartSelecting(point 400.0 300.0)
    let updated = sel.UpdateSelecting(point 100.0 100.0)
    let finished = updated.FinishSelecting()
    Assert.Equal(Selected, finished.State)
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(100.0, finished.Bounds.Y)
    Assert.Equal(300.0, finished.Bounds.Width)
    Assert.Equal(200.0, finished.Bounds.Height)

/// Kiểm tra kéo từ dưới lên trên vẫn tạo vùng dương.
[<Fact>]
let ``Kéo từ dưới lên trên tạo vùng dương`` () =
    let sel = Selection.StartSelecting(point 100.0 300.0)
    let updated = sel.UpdateSelecting(point 400.0 100.0)
    let finished = updated.FinishSelecting()
    Assert.Equal(Selected, finished.State)
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(100.0, finished.Bounds.Y)
    Assert.Equal(300.0, finished.Bounds.Width)
    Assert.Equal(200.0, finished.Bounds.Height)

/// Kiểm tra kéo chéo ngược hướng (góc phải-trên sang trái-dưới).
[<Fact>]
let ``Kéo chéo ngược hướng tạo vùng dương`` () =
    let sel = Selection.StartSelecting(point 400.0 100.0)
    let updated = sel.UpdateSelecting(point 100.0 300.0)
    let finished = updated.FinishSelecting()
    Assert.Equal(Selected, finished.State)
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(100.0, finished.Bounds.Y)
    Assert.Equal(300.0, finished.Bounds.Width)
    Assert.Equal(200.0, finished.Bounds.Height)

/// Kiểm tra nhiều lần cập nhật chỉ lấy điểm chuột cuối cùng so với điểm bắt đầu.
[<Fact>]
let ``Cập nhật nhiều lần lấy điểm chuột cuối cùng`` () =
    let sel = Selection.StartSelecting(point 100.0 100.0)
    let step1 = sel.UpdateSelecting(point 200.0 150.0)
    Assert.Equal(Selecting, step1.State)
    Assert.Equal(100.0, step1.Bounds.Width)
    Assert.Equal(50.0, step1.Bounds.Height)

    let step2 = step1.UpdateSelecting(point 300.0 250.0)
    Assert.Equal(Selecting, step2.State)
    Assert.Equal(200.0, step2.Bounds.Width)
    Assert.Equal(150.0, step2.Bounds.Height)

    let finished = step2.FinishSelecting()
    Assert.Equal(Selected, finished.State)
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(100.0, finished.Bounds.Y)
    Assert.Equal(200.0, finished.Bounds.Width)
    Assert.Equal(150.0, finished.Bounds.Height)

/// Kiểm tra vùng chọn quá nhỏ bị hủy.
[<Fact>]
let ``Vùng chọn quá nhỏ bị hủy`` () =
    let sel = Selection.StartSelecting(point 100.0 100.0)
    let updated = sel.UpdateSelecting(point 105.0 105.0)
    let finished = updated.FinishSelecting()
    Assert.Equal(Idle, finished.State)

/// Kiểm tra vùng chọn nhỏ hơn tối thiểu một pixel cũng bị hủy.
[<Fact>]
let ``Vùng chọn nhỏ hơn tối thiểu một pixel bị hủy`` () =
    let sel = Selection.StartSelecting(point 100.0 100.0)
    let updated = sel.UpdateSelecting(point 109.0 109.0)
    let finished = updated.FinishSelecting()
    Assert.Equal(Idle, finished.State)

/// Kiểm tra vùng chọn đúng kích thước tối thiểu được giữ lại.
[<Fact>]
let ``Vùng chọn đúng kích thước tối thiểu được giữ`` () =
    let sel = Selection.StartSelecting(point 100.0 100.0)
    let updated = sel.UpdateSelecting(point 110.0 110.0)
    let finished = updated.FinishSelecting()
    Assert.Equal(Selected, finished.State)
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(100.0, finished.Bounds.Y)
    Assert.Equal(10.0, finished.Bounds.Width)
    Assert.Equal(10.0, finished.Bounds.Height)

/// Kiểm tra vùng chọn tại biên trái/trên của capture area.
[<Fact>]
let ``Vùng chọn tại biên trái trên`` () =
    let sel = Selection.StartSelecting(point 0.0 0.0)
    let updated = sel.UpdateSelecting(point 50.0 50.0)
    let clamped = updated.ApplyConstraints captureBounds
    let finished = clamped.FinishSelecting()
    Assert.Equal(Selected, finished.State)
    Assert.Equal(0.0, finished.Bounds.X)
    Assert.Equal(0.0, finished.Bounds.Y)
    Assert.Equal(50.0, finished.Bounds.Width)
    Assert.Equal(50.0, finished.Bounds.Height)

/// Kiểm tra vùng chọn bị giới hạn khi kéo ra ngoài capture area.
[<Fact>]
let ``Vùng chọn bị giới hạn khi kéo ra ngoài capture area`` () =
    let sel = Selection.StartSelecting(point 1800.0 900.0)
    let updated = sel.UpdateSelecting(point 2500.0 1300.0)
    let clamped = updated.ApplyConstraints captureBounds
    Assert.Equal(Selecting, clamped.State)
    Assert.Equal(1220.0, clamped.Bounds.X)
    Assert.Equal(680.0, clamped.Bounds.Y)
    Assert.Equal(700.0, clamped.Bounds.Width)
    Assert.Equal(400.0, clamped.Bounds.Height)

    let finished = clamped.FinishSelecting()
    Assert.Equal(Selected, finished.State)

/// Kiểm tra kéo vượt quá toàn bộ capture area bị ép vừa khung.
[<Fact>]
let ``Vùng chọn vượt quá capture area bị ép vừa khung`` () =
    let sel = Selection.StartSelecting(point -100.0 -100.0)
    let updated = sel.UpdateSelecting(point 2000.0 1200.0)
    let clamped = updated.ApplyConstraints captureBounds
    Assert.Equal(0.0, clamped.Bounds.X)
    Assert.Equal(0.0, clamped.Bounds.Y)
    Assert.Equal(1920.0, clamped.Bounds.Width)
    Assert.Equal(1080.0, clamped.Bounds.Height)

    let finished = clamped.FinishSelecting()
    Assert.Equal(Selected, finished.State)

/// Kiểm tra UpdateSelecting bỏ qua khi không ở trạng thái Selecting.
[<Fact>]
let ``UpdateSelecting bỏ qua khi không ở trạng thái Selecting`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let updated = sel.UpdateSelecting(point 500.0 500.0)
    Assert.Equal(Selected, updated.State)
    Assert.Equal(100.0, updated.Bounds.X)
    Assert.Equal(80.0, updated.Bounds.Y)
    Assert.Equal(300.0, updated.Bounds.Width)
    Assert.Equal(200.0, updated.Bounds.Height)

/// Kiểm tra hoàn tất từ trạng thái không phải Selecting giữ nguyên.
[<Fact>]
let ``FinishSelecting giữ nguyên khi không ở Selecting`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let finished = sel.FinishSelecting()
    Assert.Equal(Selected, finished.State)
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(80.0, finished.Bounds.Y)
    Assert.Equal(300.0, finished.Bounds.Width)
    Assert.Equal(200.0, finished.Bounds.Height)

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

/// Kiểm tra di chuyển vùng chọn ra sát biên phải/dưới bị giới hạn.
[<Fact>]
let ``Di chuyển vùng chọn bị giới hạn ở biên phải dưới`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 1800.0 900.0 300.0 200.0 }
    let moving = sel.StartMoving(point 1900.0 1000.0)
    let updated = moving.UpdateMoving(point 2000.0 1100.0)
    let clamped = updated.ApplyConstraints captureBounds
    Assert.Equal(Moving, clamped.State)
    Assert.Equal(1620.0, clamped.Bounds.X)
    Assert.Equal(880.0, clamped.Bounds.Y)
    Assert.Equal(300.0, clamped.Bounds.Width)
    Assert.Equal(200.0, clamped.Bounds.Height)

    let finished = clamped.FinishInteraction()
    Assert.Equal(Selected, finished.State)

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

/// Kiểm tra ràng buộc giới hạn capture area trực tiếp qua ApplyConstraints.
[<Fact>]
let ``Giới hạn vùng chọn trong capture area`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 1800.0 900.0 300.0 200.0 }
    let constrained = sel.ApplyConstraints(rect 0.0 0.0 1920.0 1080.0)
    Assert.Equal(1620.0, constrained.Bounds.X)
    Assert.Equal(880.0, constrained.Bounds.Y)
