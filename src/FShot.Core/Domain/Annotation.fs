namespace FShot.Core.Domain

open System
open FShot.Core.Geometry

/// Kiểu đầu mũi tên.
/// Xem tài liệu 04_04_LineAndArrow.md.
type ArrowStyle =
    | Standard

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

    /// Nét bán trong suốt làm nổi bật.
    | Marker of start: Point * endPoint: Point

    /// Văn bản tại vị trí.
    | Text of position: Point * content: string

    /// Làm mờ pixelate vùng.
    | Pixelate of start: Point * endPoint: Point * blockSize: int

/// Một chú thích đã hoàn thành.
/// Xem tài liệu 04_02_ToolModel.md, mục 3.
type Annotation =
    {
      Id: Guid
      Tool: Tool
      Color: Color
      StrokeWidth: StrokeWidth
      CreatedAt: DateTime
    }

    /// Tạo annotation mới từ preview.
    /// Xem 04_08_CommitAndPreview.md, mục 3.
    static member FromPreview(tool: Tool, color: Color, strokeWidth: StrokeWidth) =
        {
          Id = Guid.NewGuid()
          Tool = tool
          Color = color
          StrokeWidth = strokeWidth
          CreatedAt = DateTime.UtcNow
        }

    /// Tính bounding box của chú thích.
    /// Hữu ích cho invalidation và hit-test.
    member this.BoundingBox : Rect =
        match this.Tool with
        | Pencil [] ->
            { X = 0.0; Y = 0.0; Width = 0.0; Height = 0.0 }
        | Pencil (first :: rest) ->
            let xs = first.X :: (rest |> List.map (fun p -> p.X))
            let ys = first.Y :: (rest |> List.map (fun p -> p.Y))
            {
              X = List.min xs
              Y = List.min ys
              Width = List.max xs - List.min xs
              Height = List.max ys - List.min ys
            }
        | Line (a, b)
        | Arrow (a, b, _)
        | Marker (a, b) ->
            {
              X = min a.X b.X
              Y = min a.Y b.Y
              Width = Math.Abs(b.X - a.X)
              Height = Math.Abs(b.Y - a.Y)
            }
        | Rectangle (a, b, _)
        | Circle (a, b, _)
        | Pixelate (a, b, _) ->
            {
              X = min a.X b.X
              Y = min a.Y b.Y
              Width = Math.Abs(b.X - a.X)
              Height = Math.Abs(b.Y - a.Y)
            }
        | Text (position, _) ->
            {
              X = position.X
              Y = position.Y
              Width = 0.0
              Height = 0.0
            }
