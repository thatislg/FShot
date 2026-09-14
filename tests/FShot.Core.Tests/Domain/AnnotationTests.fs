module FShot.Core.Tests.Domain.AnnotationTests

open FShot.Core.Geometry
open FShot.Core.Domain
open Xunit

let private point x y =
    {
      X = x
      Y = y
    }

let private defaultStyle color strokeWidth =
    {
      Color = color
      StrokeWidth = strokeWidth
      FontSize = 20.0
      FontName = None
      FontStyle = []
    }

/// Kiểm tra tạo annotation từ preview.
[<Fact>]
let ``Tạo annotation từ preview`` () =
    let tool = Line(point 100.0 100.0, point 400.0 100.0)
    let style = defaultStyle Color.Black (StrokeWidth.Create 3.0)
    let annotation = Annotation.FromPreview(tool, style)
    Assert.Equal(tool, annotation.Tool)
    Assert.Equal(style, annotation.Style)
    Assert.Equal(3.0, annotation.Style.StrokeWidth.Value)

/// Kiểm tra bounding box của Line.
[<Fact>]
let ``Bounding box của Line`` () =
    let style = defaultStyle Color.Black (StrokeWidth.Create 3.0)
    let annotation =
        Annotation.FromPreview(
            Line(point 100.0 100.0, point 400.0 250.0),
            style
        )
    let box = annotation.BoundingBox
    Assert.Equal(100.0, box.X)
    Assert.Equal(100.0, box.Y)
    Assert.Equal(300.0, box.Width)
    Assert.Equal(150.0, box.Height)

/// Kiểm tra bounding box của Rectangle.
[<Fact>]
let ``Bounding box của Rectangle`` () =
    let style = defaultStyle Color.Black (StrokeWidth.Create 3.0)
    let annotation =
        Annotation.FromPreview(
            Rectangle(point 100.0 80.0, point 400.0 300.0, 0.0),
            style
        )
    let box = annotation.BoundingBox
    Assert.Equal(100.0, box.X)
    Assert.Equal(80.0, box.Y)
    Assert.Equal(300.0, box.Width)
    Assert.Equal(220.0, box.Height)

/// Kiểm tra bounding box của Pencil.
[<Fact>]
let ``Bounding box của Pencil`` () =
    let style = defaultStyle Color.Black (StrokeWidth.Create 3.0)
    let points = [ point 100.0 100.0; point 200.0 150.0; point 50.0 200.0 ]
    let annotation =
        Annotation.FromPreview(
            Pencil points,
            style
        )
    let box = annotation.BoundingBox
    Assert.Equal(50.0, box.X)
    Assert.Equal(100.0, box.Y)
    Assert.Equal(150.0, box.Width)
    Assert.Equal(100.0, box.Height)

/// Kiểm tra bounding box của Marker dùng danh sách điểm.
[<Fact>]
let ``Bounding box của Marker`` () =
    let style = defaultStyle Color.Red (StrokeWidth.Create 12.0)
    let points = [ point 10.0 10.0; point 60.0 80.0; point 30.0 100.0 ]
    let annotation = Annotation.FromPreview(Marker points, style)
    let box = annotation.BoundingBox
    Assert.Equal(10.0, box.X)
    Assert.Equal(10.0, box.Y)
    Assert.Equal(50.0, box.Width)
    Assert.Equal(90.0, box.Height)

/// Kiểm tra Text annotation lưu đúng alignment.
[<Fact>]
let ``Text annotation lưu alignment`` () =
    let style = defaultStyle Color.Black (StrokeWidth.Create 2.0)
    let annotation =
        Annotation.FromPreview(
            Text(point 200.0 150.0, "Lỗi ở đây", Center),
            style
        )
    match annotation.Tool with
    | Text (_, _, alignment) -> Assert.Equal(Center, alignment)
    | _ -> Assert.True(false, "Tool phải là Text")

/// Kiểm tra bounding box của Icon.
[<Fact>]
let ``Bounding box của Icon`` () =
    let style = defaultStyle Color.Black (StrokeWidth.Create 2.0)
    let annotation =
        Annotation.FromPreview(
            Icon(point 100.0 80.0, 64.0, 48.0, ""),
            style
        )
    let box = annotation.BoundingBox
    Assert.Equal(100.0, box.X)
    Assert.Equal(80.0, box.Y)
    Assert.Equal(64.0, box.Width)
    Assert.Equal(48.0, box.Height)
