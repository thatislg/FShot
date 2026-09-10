module FShot.Core.Tests.Domain.AnnotationTests

open FShot.Core.Geometry
open FShot.Core.Domain
open Xunit

let private point x y =
    {
      X = x
      Y = y
    }

/// Kiểm tra tạo annotation từ preview.
[<Fact>]
let ``Tạo annotation từ preview`` () =
    let tool = Line(point 100.0 100.0, point 400.0 100.0)
    let color = Color.Black
    let stroke = StrokeWidth.Create 3.0
    let annotation = Annotation.FromPreview(tool, color, stroke)
    Assert.Equal(tool, annotation.Tool)
    Assert.Equal(color, annotation.Color)
    Assert.Equal(3.0, annotation.StrokeWidth.Value)

/// Kiểm tra bounding box của Line.
[<Fact>]
let ``Bounding box của Line`` () =
    let annotation =
        Annotation.FromPreview(
            Line(point 100.0 100.0, point 400.0 250.0),
            Color.Black,
            StrokeWidth.Create 3.0
        )
    let box = annotation.BoundingBox
    Assert.Equal(100.0, box.X)
    Assert.Equal(100.0, box.Y)
    Assert.Equal(300.0, box.Width)
    Assert.Equal(150.0, box.Height)

/// Kiểm tra bounding box của Rectangle.
[<Fact>]
let ``Bounding box của Rectangle`` () =
    let annotation =
        Annotation.FromPreview(
            Rectangle(point 100.0 80.0, point 400.0 300.0, 0.0),
            Color.Black,
            StrokeWidth.Create 3.0
        )
    let box = annotation.BoundingBox
    Assert.Equal(100.0, box.X)
    Assert.Equal(80.0, box.Y)
    Assert.Equal(300.0, box.Width)
    Assert.Equal(220.0, box.Height)

/// Kiểm tra bounding box của Pencil.
[<Fact>]
let ``Bounding box của Pencil`` () =
    let points = [ point 100.0 100.0; point 200.0 150.0; point 50.0 200.0 ]
    let annotation =
        Annotation.FromPreview(
            Pencil points,
            Color.Black,
            StrokeWidth.Create 3.0
        )
    let box = annotation.BoundingBox
    Assert.Equal(50.0, box.X)
    Assert.Equal(100.0, box.Y)
    Assert.Equal(150.0, box.Width)
    Assert.Equal(100.0, box.Height)
