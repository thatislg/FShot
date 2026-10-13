namespace FShot.Core.Domain

open System
open FShot.Core.Geometry
open FShot.Core.Geometry.Operations

/// Giải thuật kiểm tra va chạm hình học giữa con trỏ chuột và chú thích.
/// Xem tài liệu 04_11_ObjectSelectionAndEditing.md, mục 2.
/// Đặt trong Domain vì phụ thuộc kiểu Annotation (Geometry không thể tham chiếu Domain).
module HitTesting =

    /// Bán kính dung sai mặc định (FR-ANN-19).
    let defaultTolerance = 6.0

    /// Khoảng cách từ điểm tới một chú thích.
    /// - Line/Arrow: khoảng cách tới đoạn thẳng.
    /// - Pencil/Marker: khoảng cách nhỏ nhất tới từng đoạn con.
    /// - Các loại còn lại: khoảng cách tới bounding box (0 nếu nằm trong).
    let distanceToAnnotation (p: Point) (annotation: Annotation) : float =
        match annotation.Tool with
        | Line (a, b)
        | Arrow (a, b, _) -> distanceToSegment p a b
        | Pencil points
        | Marker points ->
            match points with
            | [] -> Double.PositiveInfinity
            | _ ->
                points
                |> List.pairwise
                |> List.map (fun (a, b) -> distanceToSegment p a b)
                |> List.min
        | _ ->
            let box = annotation.BoundingBox
            let dx = max 0.0 (max (box.X - p.X) (p.X - box.Right))
            let dy = max 0.0 (max (box.Y - p.Y) (p.Y - box.Bottom))
            Math.Sqrt(dx * dx + dy * dy)

    /// Kiểm tra điểm có chạm chú thích trong bán kính dung sai không.
    /// Ngưỡng = StrokeWidth / 2 + tolerance.
    let hitAnnotation (tolerance: float) (p: Point) (annotation: Annotation) : bool =
        let threshold = annotation.Style.StrokeWidth.Value / 2.0 + tolerance
        distanceToAnnotation p annotation <= threshold

    /// Quét danh sách chú thích theo thứ tự Z-order (từ mới nhất đến cũ nhất),
    /// trả về chú thích đầu tiên bị chạm (lớp trên cùng ưu tiên).
    let hitTestTopmost (tolerance: float) (p: Point) (annotations: Annotation list) : Annotation option =
        annotations
        |> List.rev
        |> List.tryFind (fun a -> hitAnnotation tolerance p a)

    /// Tịnh tiến toàn bộ tọa độ của một chú thích theo vector (dx, dy).
    let translateAnnotation (dx: float) (dy: float) (annotation: Annotation) : Annotation =
        let translatePoint (p: Point) : Point = { X = p.X + dx; Y = p.Y + dy }
        let newTool =
            match annotation.Tool with
            | Pencil points -> Pencil(List.map translatePoint points)
            | Line (a, b) -> Line(translatePoint a, translatePoint b)
            | Arrow (a, b, s) -> Arrow(translatePoint a, translatePoint b, s)
            | Rectangle (a, b, r) -> Rectangle(translatePoint a, translatePoint b, r)
            | Circle (a, b, locked) -> Circle(translatePoint a, translatePoint b, locked)
            | Marker points -> Marker(List.map translatePoint points)
            | Text (p, c, al) -> Text(translatePoint p, c, al)
            | Pixelate (a, b, bs) -> Pixelate(translatePoint a, translatePoint b, bs)
            | Invert (a, b) -> Invert(translatePoint a, translatePoint b)
            | CircleCounter (c, i, r) -> CircleCounter(translatePoint c, i, r)
            | Icon (p, w, h, id) -> Icon(translatePoint p, w, h, id)
        { annotation with Tool = newTool }
