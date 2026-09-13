namespace FShot.Core.Domain

open System
open FShot.Core.Geometry

/// Cách căn chỉnh của khối văn bản so với vị trí gốc.
/// Xem tài liệu 04_07_TextTool.md, mục 4.1.
type TextAlignment =
    /// Căn lề trái: vị trí gốc là góc trên bên trái của khối văn bản.
    | Left

    /// Căn giữa: vị trí gốc là điểm giữa trên cùng của khối văn bản.
    | Center

    /// Căn lề phải: vị trí gốc là góc trên bên phải của khối văn bản.
    | Right

/// Kiểu định dạng ký tự có thể kết hợp.
/// Xem tài liệu 04_07_TextTool.md, mục 5.2.
type FontStyle =
    /// In đậm.
    | Bold

    /// In nghiêng.
    | Italic

    /// Gạch chân.
    | Underline

/// Kiểu đầu mũi tên.
/// Xem tài liệu 04_04_LineAndArrow.md.
type ArrowStyle =
    | Standard

/// Tập hợp các thuộc tính hình ảnh chung cho một Annotation.
/// Thay vì mỗi tool tự lưu màu sắc và độ dày, tất cả các tool dùng chung
/// một style container để tránh lặp lại dữ liệu và logic.
/// Xem tài liệu 04_02_ToolModel.md, mục 3.1.
type AnnotationStyle = {
    /// Màu sắc dùng cho nét vẽ, viền hình học, hoặc chữ.
    Color: Color

    /// Độ dày nét vẽ hoặc viền hình học.
    StrokeWidth: StrokeWidth

    /// Cỡ chữ khi tool là Text.
    /// Các tool khác không dùng trường này nhưng vẫn được lưu trong
    /// style chung để đơn giản hóa việc chuyển đổi giữa các công cụ.
    FontSize: float

    /// Tên font tùy chọn. Nếu None thì renderer dùng font mặc định.
    FontName: string option

    /// Danh sách kiểu định dạng chữ có thể kết hợp.
    FontStyle: FontStyle list
}

/// Công cụ chú thích.
/// Xem tài liệu 04_02_ToolModel.md.
type Tool =
    /// Vẽ tự do bằng danh sách điểm.
    | Pencil of points: Point list

    /// Đường thẳng từ điểm bắt đầu đến điểm kết thúc.
    | Line of start: Point * endPoint: Point

    /// Đường thẳng có mũi tên ở đầu kết thúc.
    | Arrow of start: Point * endPoint: Point * style: ArrowStyle

    /// Hình chữ nhật với tùy chọn bo góc.
    | Rectangle of start: Point * endPoint: Point * cornerRadius: float

    /// Hình tròn hoặc elip trong bounding box.
    | Circle of start: Point * endPoint: Point * aspectLocked: bool

    /// Nét bán trong suốt làm nổi bật, thu thập bằng danh sách điểm.
    | Marker of points: Point list

    /// Văn bản tại vị trí với cách căn chỉnh.
    | Text of position: Point * content: string * alignment: TextAlignment

    /// Làm mờ pixelate vùng.
    | Pixelate of start: Point * endPoint: Point * blockSize: int

/// Một chú thích đã hoàn thành.
/// Xem tài liệu 04_02_ToolModel.md, mục 3.
type Annotation = {
    Id: Guid
    Tool: Tool
    Style: AnnotationStyle
    CreatedAt: DateTime
} with
    /// Tạo annotation mới từ preview.
    /// Xem 04_08_CommitAndPreview.md, mục 3.
    static member FromPreview(tool: Tool, style: AnnotationStyle) =
        {
          Id = Guid.NewGuid()
          Tool = tool
          Style = style
          CreatedAt = DateTime.UtcNow
        }

    /// Tính bounding box của chú thích.
    /// Hữu ích cho invalidation và hit-test.
    member this.BoundingBox : Rect =
        let rectFromPoints (points: Point list) : Rect =
            match points with
            | [] ->
                { X = 0.0; Y = 0.0; Width = 0.0; Height = 0.0 }
            | first :: rest ->
                let xs = first.X :: (rest |> List.map (fun p -> p.X))
                let ys = first.Y :: (rest |> List.map (fun p -> p.Y))
                {
                  X = List.min xs
                  Y = List.min ys
                  Width = List.max xs - List.min xs
                  Height = List.max ys - List.min ys
                }

        let rectFromTwoPoints (a: Point) (b: Point) : Rect =
            {
              X = min a.X b.X
              Y = min a.Y b.Y
              Width = Math.Abs(b.X - a.X)
              Height = Math.Abs(b.Y - a.Y)
            }

        match this.Tool with
        | Pencil points -> rectFromPoints points
        | Marker points -> rectFromPoints points
        | Line (a, b)
        | Arrow (a, b, _)
        | Rectangle (a, b, _)
        | Circle (a, b, _)
        | Pixelate (a, b, _) -> rectFromTwoPoints a b
        | Text (position, _, _) ->
            {
              X = position.X
              Y = position.Y
              Width = 0.0
              Height = 0.0
            }
