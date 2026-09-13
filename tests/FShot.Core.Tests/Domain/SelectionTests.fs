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

/// Kiểm tra di chuyển vùng chọn theo hướng âm.
[<Fact>]
let ``Di chuyển vùng chọn theo hướng âm`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 300.0 250.0 200.0 150.0 }
    let moving = sel.StartMoving(point 400.0 325.0)
    let updated = moving.UpdateMoving(point 350.0 275.0)
    let finished = updated.FinishInteraction()
    Assert.Equal(250.0, finished.Bounds.X)
    Assert.Equal(200.0, finished.Bounds.Y)
    Assert.Equal(200.0, finished.Bounds.Width)
    Assert.Equal(150.0, finished.Bounds.Height)

/// Kiểm tra di chuyển vùng chọn ra sát biên trái/trên bị giới hạn.
[<Fact>]
let ``Di chuyển vùng chọn bị giới hạn ở biên trái trên`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 50.0 50.0 200.0 150.0 }
    let moving = sel.StartMoving(point 100.0 100.0)
    let updated = moving.UpdateMoving(point -50.0 -30.0)
    let clamped = updated.ApplyConstraints captureBounds
    Assert.Equal(Moving, clamped.State)
    Assert.Equal(0.0, clamped.Bounds.X)
    Assert.Equal(0.0, clamped.Bounds.Y)
    Assert.Equal(200.0, clamped.Bounds.Width)
    Assert.Equal(150.0, clamped.Bounds.Height)

    let finished = clamped.FinishInteraction()
    Assert.Equal(Selected, finished.State)

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

/// Kiểm tra nhấn và nhả tại cùng điểm bên trong vùng không di chuyển.
[<Fact>]
let ``Nhấn nhả tại cùng điểm không di chuyển vùng`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let moving = sel.StartMoving(point 150.0 150.0)
    let updated = moving.UpdateMoving(point 150.0 150.0)
    let finished = updated.FinishInteraction()
    Assert.Equal(Selected, finished.State)
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(80.0, finished.Bounds.Y)

/// Kiểm tra UpdateMoving bỏ qua khi không ở trạng thái Moving.
[<Fact>]
let ``UpdateMoving bỏ qua khi không ở trạng thái Moving`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let updated = sel.UpdateMoving(point 500.0 500.0)
    Assert.Equal(Selected, updated.State)
    Assert.Equal(100.0, updated.Bounds.X)
    Assert.Equal(80.0, updated.Bounds.Y)

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

/// Kiểm tra co giãn bằng handle TopLeft.
/// Công thức: X = mx, Y = my, W = right - mx, H = bottom - my.
[<Fact>]
let ``Co giãn vùng chọn TopLeft`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let resizing = sel.StartResizing TopLeft (point 100.0 80.0)
    let updated = resizing.UpdateResizing(point 50.0 30.0)
    let finished = updated.FinishInteraction()
    Assert.Equal(50.0, finished.Bounds.X)
    Assert.Equal(30.0, finished.Bounds.Y)
    Assert.Equal(350.0, finished.Bounds.Width)
    Assert.Equal(250.0, finished.Bounds.Height)

/// Kiểm tra co giãn bằng handle Top.
/// Công thức: Y = my, H = bottom - my.
[<Fact>]
let ``Co giãn vùng chọn Top`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let resizing = sel.StartResizing Top (point 250.0 80.0)
    let updated = resizing.UpdateResizing(point 250.0 30.0)
    let finished = updated.FinishInteraction()
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(30.0, finished.Bounds.Y)
    Assert.Equal(300.0, finished.Bounds.Width)
    Assert.Equal(250.0, finished.Bounds.Height)

/// Kiểm tra co giãn bằng handle Bottom.
/// Công thức: H = my - Y.
[<Fact>]
let ``Co giãn vùng chọn Bottom`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let resizing = sel.StartResizing Bottom (point 250.0 280.0)
    let updated = resizing.UpdateResizing(point 250.0 350.0)
    let finished = updated.FinishInteraction()
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(80.0, finished.Bounds.Y)
    Assert.Equal(300.0, finished.Bounds.Width)
    Assert.Equal(270.0, finished.Bounds.Height)

/// Kiểm tra co giãn bằng handle Left.
/// Công thức: X = mx, W = right - mx.
[<Fact>]
let ``Co giãn vùng chọn Left`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let resizing = sel.StartResizing Left (point 100.0 180.0)
    let updated = resizing.UpdateResizing(point 50.0 180.0)
    let finished = updated.FinishInteraction()
    Assert.Equal(50.0, finished.Bounds.X)
    Assert.Equal(80.0, finished.Bounds.Y)
    Assert.Equal(350.0, finished.Bounds.Width)
    Assert.Equal(200.0, finished.Bounds.Height)

/// Kiểm tra co giãn bằng handle Right.
/// Công thức: W = mx - X.
[<Fact>]
let ``Co giãn vùng chọn Right`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let resizing = sel.StartResizing Right (point 400.0 180.0)
    let updated = resizing.UpdateResizing(point 450.0 180.0)
    let finished = updated.FinishInteraction()
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(80.0, finished.Bounds.Y)
    Assert.Equal(350.0, finished.Bounds.Width)
    Assert.Equal(200.0, finished.Bounds.Height)

/// Kiểm tra co giãn bằng handle TopRight.
/// Công thức: X = orig.X, Y = my, W = mx - X, H = bottom - my.
[<Fact>]
let ``Co giãn vùng chọn TopRight`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let resizing = sel.StartResizing TopRight (point 400.0 80.0)
    let updated = resizing.UpdateResizing(point 450.0 30.0)
    let finished = updated.FinishInteraction()
    Assert.Equal(100.0, finished.Bounds.X)
    Assert.Equal(30.0, finished.Bounds.Y)
    Assert.Equal(350.0, finished.Bounds.Width)
    Assert.Equal(250.0, finished.Bounds.Height)

/// Kiểm tra co giãn bằng handle BottomLeft.
/// Công thức: X = mx, Y = orig.Y, W = right - mx, H = my - Y.
[<Fact>]
let ``Co giãn vùng chọn BottomLeft`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let resizing = sel.StartResizing BottomLeft (point 100.0 280.0)
    let updated = resizing.UpdateResizing(point 50.0 350.0)
    let finished = updated.FinishInteraction()
    Assert.Equal(50.0, finished.Bounds.X)
    Assert.Equal(80.0, finished.Bounds.Y)
    Assert.Equal(350.0, finished.Bounds.Width)
    Assert.Equal(270.0, finished.Bounds.Height)

/// Kiểm tra hit-test tất cả 8 handle tại trung tâm.
[<Fact>]
let ``Hit-test tất cả 8 handle`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }

    let cases =
        [
            (point 100.0 80.0, TopLeft)
            (point 250.0 80.0, Top)
            (point 400.0 80.0, TopRight)
            (point 100.0 180.0, Left)
            (point 400.0 180.0, Right)
            (point 100.0 280.0, BottomLeft)
            (point 250.0 280.0, Bottom)
            (point 400.0 280.0, BottomRight)
        ]

    for (p, expected) in cases do
        let actual = sel.HitTestHandle p
        Assert.Equal(Some expected, actual)

/// Kiểm tra hit-test tolerance mở rộng vùng hiệu lực.
[<Fact>]
let ``Hit-test tolerance mở rộng vùng hiệu lực`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }

    // BottomRight center (400, 280), halfSize = 4, tolerance = 6 => effective radius = 10
    let inside = point 405.0 285.0
    let outside = point 411.0 281.0

    Assert.Equal(Some BottomRight, sel.HitTestHandle inside)
    Assert.Equal(None, sel.HitTestHandle outside)

/// Kiểm tra hit-test ưu tiên góc trước cạnh.
[<Fact>]
let ``Hit-test ưu tiên góc trước cạnh`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }

    // Điểm (100, 80) nằm trong vùng hiệu lực của cả TopLeft, Top và Left
    // Nhưng HandleCenters duyệt TopLeft đầu tiên nên phải trả về TopLeft.
    Assert.Equal(Some TopLeft, sel.HitTestHandle(point 100.0 80.0))

/// Kiểm tra resize vùng chọn quá nhỏ bị hủy khi hoàn tất.
[<Fact>]
let ``Resize quá nhỏ bị hủy khi hoàn tất`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let resizing = sel.StartResizing Right (point 400.0 180.0)
    let updated = resizing.UpdateResizing(point 105.0 180.0)
    let finished = updated.FinishInteraction()
    Assert.Equal(Idle, finished.State)

/// Kiểm tra resize ra ngoài capture area bị clamp.
[<Fact>]
let ``Resize ra ngoài capture area bị clamp`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 1800.0 900.0 100.0 100.0 }
    let resizing = sel.StartResizing BottomRight (point 1900.0 1000.0)
    let updated = resizing.UpdateResizing(point 2000.0 1200.0)
    let clamped = updated.ApplyConstraints captureBounds
    let finished = clamped.FinishInteraction()
    Assert.Equal(Selected, finished.State)
    Assert.Equal(1720.0, finished.Bounds.X)
    Assert.Equal(780.0, finished.Bounds.Y)
    Assert.Equal(200.0, finished.Bounds.Width)
    Assert.Equal(300.0, finished.Bounds.Height)

/// Kiểm tra UpdateResizing bỏ qua khi không ở trạng thái Resizing.
[<Fact>]
let ``UpdateResizing bỏ qua khi không ở trạng thái Resizing`` () =
    let sel =
        { Selection.Empty with
            State = Selected
            Bounds = rect 100.0 80.0 300.0 200.0 }
    let updated = sel.UpdateResizing(point 500.0 500.0)
    Assert.Equal(Selected, updated.State)
    Assert.Equal(100.0, updated.Bounds.X)
    Assert.Equal(80.0, updated.Bounds.Y)
    Assert.Equal(300.0, updated.Bounds.Width)
    Assert.Equal(200.0, updated.Bounds.Height)

/// Kiểm tra hit-test handle.

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
