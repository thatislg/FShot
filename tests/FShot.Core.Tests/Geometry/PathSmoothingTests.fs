module FShot.Core.Tests.Geometry.PathSmoothingTests

open FShot.Core.Geometry
open Xunit

let private point x y = { X = x; Y = y }

/// Kiểm tra simplifyPoints không bỏ điểm khi khoảng cách đủ lớn.
[<Fact>]
let ``simplifyPoints giữ lại các điểm cách nhau đủ xa`` () =
    let points = [ point 0.0 0.0; point 10.0 0.0; point 20.0 0.0 ]
    let result = PathSmoothing.simplifyPoints 5.0 points
    Assert.Equal(3, List.length result)

/// Kiểm tra simplifyPoints bỏ điểm quá gần điểm cuối đã giữ.
[<Fact>]
let ``simplifyPoints bỏ các điểm quá gần nhau`` () =
    let points =
        [
            point 0.0 0.0
            point 0.3 0.0
            point 0.6 0.0
            point 10.0 0.0
        ]

    let result = PathSmoothing.simplifyPoints 1.0 points
    // Giữ 0.0 và 10.0; hai điểm giữa bị bỏ.
    Assert.Equal(2, List.length result)
    Assert.Equal(point 0.0 0.0, List.head result)
    Assert.Equal(point 10.0 0.0, List.last result)

/// Kiểm tra simplifyPoints với ngưỡng 0 trả về nguyên danh sách.
[<Fact>]
let ``simplifyPoints với minDistance bằng 0 giữ nguyên`` () =
    let points = [ point 0.0 0.0; point 0.1 0.0; point 0.2 0.0 ]
    let result = PathSmoothing.simplifyPoints 0.0 points
    Assert.Equal(3, List.length result)

/// Kiểm tra toQuadraticSegments với hai điểm tạo một đoạn cong.
[<Fact>]
let ``toQuadraticSegments với 2 điểm tạo 1 segment`` () =
    let a = point 0.0 0.0
    let b = point 10.0 0.0
    let segments = PathSmoothing.toQuadraticSegments [ a; b ]
    Assert.Equal(1, List.length segments)
    let (start, ctrl, target) = List.head segments
    Assert.Equal(a, start)
    Assert.Equal(point 5.0 0.0, ctrl)
    Assert.Equal(b, target)

/// Kiểm tra toQuadraticSegments với ba điểm tạo đoạn cong theo mô tả thiết kế.
[<Fact>]
let ``toQuadraticSegments với 3 điểm tạo segment đúng midpoint`` () =
    let p0 = point 0.0 0.0
    let p1 = point 10.0 10.0
    let p2 = point 20.0 0.0
    let segments = PathSmoothing.toQuadraticSegments [ p0; p1; p2 ]
    Assert.Equal(1, List.length segments)
    let (start, ctrl, target) = List.head segments
    Assert.Equal(point 5.0 5.0, start)
    Assert.Equal(p1, ctrl)
    Assert.Equal(point 15.0 5.0, target)

/// Kiểm tra toQuadraticSegments với bốn điểm tạo hai đoạn cong liên tiếp.
[<Fact>]
let ``toQuadraticSegments với 4 điểm tạo 2 segments`` () =
    let p0 = point 0.0 0.0
    let p1 = point 10.0 10.0
    let p2 = point 20.0 10.0
    let p3 = point 30.0 0.0
    let segments = PathSmoothing.toQuadraticSegments [ p0; p1; p2; p3 ]
    Assert.Equal(2, List.length segments)
    let (s1, c1, t1) = List.head segments
    let (s2, c2, t2) = List.last segments
    // Đoạn 1: từ mid(p0,p1) đến mid(p1,p2), điều khiển p1.
    Assert.Equal(point 5.0 5.0, s1)
    Assert.Equal(p1, c1)
    Assert.Equal(point 15.0 10.0, t1)
    // Đoạn 2: từ mid(p1,p2) đến mid(p2,p3), điều khiển p2.
    Assert.Equal(point 15.0 10.0, s2)
    Assert.Equal(p2, c2)
    Assert.Equal(point 25.0 5.0, t2)

/// Kiểm tra toSmoothedSegments kết hợp cả hai bước.
[<Fact>]
let ``toSmoothedSegments giảm dư thừa rồi tạo segments`` () =
    let style = StrokeWidth.Create 4.0
    let points =
        [
            point 0.0 0.0
            point 0.5 0.0
            point 1.0 0.0
            point 10.0 0.0
        ]

    let segments = PathSmoothing.toSmoothedSegments style points
    // Ngưỡng = max(4.0 * 0.25, 0.5) = 1.0.
    // Giữ 0.0 và 10.0; sau đó 2 điểm tạo 1 segment.
    Assert.Equal(1, List.length segments)
