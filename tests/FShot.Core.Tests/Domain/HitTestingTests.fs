module FShot.Core.Tests.Domain.HitTestingTests

open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Core.Domain.HitTesting
open Xunit

let private point x y = { X = x; Y = y }

let private defaultStyle =
    {
      Color = Color.Black
      StrokeWidth = StrokeWidth.Create 2.0
      FontSize = 14.0
      FontName = None
      FontStyle = []
    }

let private lineAnnotation x1 y1 x2 y2 =
    Annotation.FromPreview(Line(point x1 y1, point x2 y2), defaultStyle)

let private arrowAnnotation x1 y1 x2 y2 =
    Annotation.FromPreview(Arrow(point x1 y1, point x2 y2, ArrowStyle.Standard), defaultStyle)

let private markerAnnotation pts =
    Annotation.FromPreview(Marker pts, defaultStyle)

/// Kiểm tra khoảng cách từ điểm tới đoạn thẳng là 0 khi điểm nằm trên đoạn.
[<Fact>]
let ``distanceToAnnotation Line trả về 0 khi điểm nằm trên đoạn`` () =
    let ann = lineAnnotation 0.0 0.0 10.0 0.0
    Assert.Equal(0.0, distanceToAnnotation (point 5.0 0.0) ann, 5)

/// Kiểm tra khoảng cách tới đoạn thẳng dùng hình chiếu vuông góc.
[<Fact>]
let ``distanceToAnnotation Line dùng khoảng cách vuông góc`` () =
    let ann = lineAnnotation 0.0 0.0 10.0 0.0
    Assert.Equal(3.0, distanceToAnnotation (point 5.0 3.0) ann, 5)

/// Kiểm tra Arrow dùng cùng khoảng cách đoạn thẳng với Line.
[<Fact>]
let ``distanceToAnnotation Arrow dùng khoảng cách đoạn thẳng`` () =
    let ann = arrowAnnotation 0.0 0.0 10.0 0.0
    Assert.Equal(4.0, distanceToAnnotation (point 2.0 4.0) ann, 5)

/// Kiểm tra Marker dùng khoảng cách nhỏ nhất tới từng đoạn con.
[<Fact>]
let ``distanceToAnnotation Marker dùng khoảng cách nhỏ nhất`` () =
    let ann = markerAnnotation [ point 0.0 0.0; point 10.0 0.0; point 10.0 10.0 ]
    // Điểm gần đoạn thẳng nhất (0,0)-(10,0) ở khoảng cách 2.
    Assert.Equal(2.0, distanceToAnnotation (point 5.0 2.0) ann, 5)

/// Kiểm tra hit-test trả về None khi không có chú thích nào gần.
[<Fact>]
let ``hitTestTopmost trả về None khi không có gì gần`` () =
    let anns = [ lineAnnotation 0.0 0.0 10.0 0.0 ]
    Assert.Equal(None, hitTestTopmost defaultTolerance (point 100.0 100.0) anns)

/// Kiểm tra hit-test trả về Some khi điểm nằm trong bán kính dung sai.
[<Fact>]
let ``hitTestTopmost trả về Some khi trong dung sai`` () =
    let anns = [ lineAnnotation 0.0 0.0 10.0 0.0 ]
    // Dung sai 6.0 + nửa nét 1.0 = 7.0; điểm cách đoạn 5.0 → trúng.
    let result = hitTestTopmost defaultTolerance (point 5.0 5.0) anns
    Assert.True(result.IsSome)

/// Kiểm tra Z-order: chú thích mới hơn (lớp trên) được chọn trước khi chồng lấp.
[<Fact>]
let ``hitTestTopmost ưu tiên lớp trên cùng theo Z-order`` () =
    let older = lineAnnotation 0.0 0.0 10.0 0.0
    let newer = lineAnnotation 0.0 2.0 10.0 2.0
    // Điểm (5,1) nằm gần cả hai; newer vẽ sau nên được chọn.
    let result = hitTestTopmost defaultTolerance (point 5.0 1.0) [ older; newer ]
    match result with
    | Some ann -> Assert.Equal(newer.Id, ann.Id)
    | None -> Assert.True(false, "Expected Some annotation")

/// Kiểm tra translateAnnotation tịnh tiến tọa độ Line theo vector.
[<Fact>]
let ``translateAnnotation tịnh tiến Line theo vector`` () =
    let ann = lineAnnotation 0.0 0.0 10.0 0.0
    let moved = translateAnnotation 5.0 -3.0 ann
    match moved.Tool with
    | Line (a, b) ->
        Assert.Equal(point 5.0 -3.0, a)
        Assert.Equal(point 15.0 -3.0, b)
    | _ -> Assert.True(false, "Expected Line")

/// Kiểm tra translateAnnotation giữ nguyên Id và Style.
[<Fact>]
let ``translateAnnotation giữ nguyên Id và Style`` () =
    let ann = lineAnnotation 0.0 0.0 10.0 0.0
    let moved = translateAnnotation 1.0 1.0 ann
    Assert.Equal(ann.Id, moved.Id)
    Assert.Equal(ann.Style, moved.Style)

/// Kiểm tra translateAnnotation tịnh tiến Marker (danh sách điểm).
[<Fact>]
let ``translateAnnotation tịnh tiến Marker tất cả điểm`` () =
    let ann = markerAnnotation [ point 0.0 0.0; point 10.0 0.0 ]
    let moved = translateAnnotation 2.0 2.0 ann
    match moved.Tool with
    | Marker pts ->
        Assert.Equal(point 2.0 2.0, List.head pts)
        Assert.Equal(point 12.0 2.0, List.last pts)
    | _ -> Assert.True(false, "Expected Marker")
