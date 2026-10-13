module FShot.Core.Tests.Geometry.AngleSnappingTests

open System
open FShot.Core.Geometry
open FShot.Core.Geometry.AngleSnapping
open Xunit

let private point x y = { X = x; Y = y }

/// So sánh hai điểm gần đúng với dung sai epsilon.
let private assertPoint (expected: Point) (actual: Point) (epsilon: float) =
    Assert.True(
        abs (expected.X - actual.X) <= epsilon && abs (expected.Y - actual.Y) <= epsilon,
        sprintf "Expected ~(%f, %f) but got (%f, %f)" expected.X expected.Y actual.X actual.Y
    )

// ------------------------------------------------------------
// snapAngle45
// ------------------------------------------------------------

/// Góc 0° (nằm ngang) giữ nguyên.
[<Fact>]
let ``snapAngle45 giữ nguyên điểm nằm ngang`` () =
    let result = snapAngle45 (point 0.0 0.0) (point 100.0 0.0)
    assertPoint (point 100.0 0.0) result 0.01

/// Góc 45° giữ nguyên.
[<Fact>]
let ``snapAngle45 giữ nguyên điểm 45 độ`` () =
    let result = snapAngle45 (point 0.0 0.0) (point 50.0 50.0)
    assertPoint (point 50.0 50.0) result 0.01

/// Góc 90° (thẳng đứng) giữ nguyên.
[<Fact>]
let ``snapAngle45 giữ nguyên điểm 90 độ`` () =
    let result = snapAngle45 (point 0.0 0.0) (point 0.0 80.0)
    assertPoint (point 0.0 80.0) result 0.01

/// Góc gần ngang (~5.7°) bị hít về 0°, giữ độ dài vector.
[<Fact>]
let ``snapAngle45 hít góc gần ngang về 0 độ`` () =
    let result = snapAngle45 (point 0.0 0.0) (point 100.0 10.0)
    let length = Math.Sqrt(100.0 * 100.0 + 10.0 * 10.0)
    assertPoint (point length 0.0) result 0.01

/// Góc 135° giữ nguyên.
[<Fact>]
let ``snapAngle45 giữ nguyên góc 135 độ`` () =
    let result = snapAngle45 (point 0.0 0.0) (point -40.0 40.0)
    assertPoint (point -40.0 40.0) result 0.01

/// Góc -90° (thẳng đứng lên) giữ nguyên.
[<Fact>]
let ``snapAngle45 giữ nguyên góc âm 90 độ`` () =
    let result = snapAngle45 (point 0.0 0.0) (point 0.0 -60.0)
    assertPoint (point 0.0 -60.0) result 0.01

/// Khoảng cách quá nhỏ (< 2px) thì trả nguyên điểm hiện tại.
[<Fact>]
let ``snapAngle45 bỏ qua khi khoảng cách quá nhỏ`` () =
    let current = point 1.0 0.5
    let result = snapAngle45 (point 0.0 0.0) current
    assertPoint current result 0.0001

// ------------------------------------------------------------
// snapSquare
// ------------------------------------------------------------

/// Kéo sang Đông Bắc: side = max(dx, dy), về (100, 100).
[<Fact>]
let ``snapSquare kéo Đông Bắc`` () =
    let result = snapSquare (point 0.0 0.0) (point 100.0 50.0)
    assertPoint (point 100.0 100.0) result 0.0001

/// Kéo sang Tây Bắc: bảo toàn dấu X âm.
[<Fact>]
let ``snapSquare kéo Tây Bắc`` () =
    let result = snapSquare (point 0.0 0.0) (point -100.0 50.0)
    assertPoint (point -100.0 100.0) result 0.0001

/// Kéo sang Tây Nam: cả hai trục âm.
[<Fact>]
let ``snapSquare kéo Tây Nam`` () =
    let result = snapSquare (point 0.0 0.0) (point -100.0 -50.0)
    assertPoint (point -100.0 -100.0) result 0.0001

/// Kéo sang Đông Nam: X dương, Y âm.
[<Fact>]
let ``snapSquare kéo Đông Nam`` () =
    let result = snapSquare (point 0.0 0.0) (point 100.0 -50.0)
    assertPoint (point 100.0 -100.0) result 0.0001

/// Kéo lệch tâm (không từ gốc 0,0) vẫn đúng.
[<Fact>]
let ``snapSquare từ điểm bắt đầu không phải gốc`` () =
    let result = snapSquare (point 150.0 150.0) (point 300.0 250.0)
    assertPoint (point 300.0 300.0) result 0.0001
