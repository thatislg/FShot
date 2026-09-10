namespace FShot.Core.Domain

open System
open FShot.Core.Geometry

/// Định dạng file ảnh.
/// Xem tài liệu 06_03_SaveOptions.md.
type FileFormat =
    | Png
    | Jpg

/// Tùy chọn lưu file.
/// Xem tài liệu 06_03_SaveOptions.md.
type SaveOptions =
    {
      Path: string option
      FileNamePattern: string
      Format: FileFormat
      JpegQuality: int
    }

    /// SaveOptions mặc định.
    static member Default =
        {
          Path = None
          FileNamePattern = "fshot_%Y-%m-%d-%H%M%S"
          Format = Png
          JpegQuality = 90
        }

    /// Giới hạn JpegQuality trong [0, 100].
    member this.NormalizedJpegQuality =
        max 0 (min 100 this.JpegQuality)

/// Hình thức xuất ảnh.
/// Xem tài liệu 06_02_ExportTarget.md.
type ExportTarget =
    /// Lưu ảnh vào đường dẫn file.
    | SaveToFile of path: string option

    /// Sao chép ảnh vào clipboard.
    | CopyToClipboard

    /// Xuất byte PNG ra stdout.
    | RawPngToStdout

    /// In kích thước và vị trí vùng chọn ra stdout.
    | PrintGeometry

    /// Mở ảnh bằng ứng dụng mặc định.
    | OpenWithDefaultApp of path: string

/// Yêu cầu xuất ảnh.
/// Xem tài liệu 06_04_RenderPipeline.md.
type ExportRequest =
    {
      CaptureResult: CaptureResult
      SelectionBounds: Rect
      Annotations: Annotation list
      Target: ExportTarget
      SaveOptions: SaveOptions
    }

    /// Tính kích thước ảnh output trong physical pixels.
    /// Công thức: round(w * s), round(h * s).
    /// Xem 06_04_RenderPipeline.md, mục 2.
    member this.OutputSize =
        let s = this.CaptureResult.ScaleFactor.Value
        (
            int (Math.Round(this.SelectionBounds.Width * s)),
            int (Math.Round(this.SelectionBounds.Height * s))
        )

    /// In thông tin hình học vùng chọn dạng WxH+X+Y.
    /// Xem 06_02_ExportTarget.md, mục 5.
    member this.GeometryString =
        sprintf "%dx%d+%.0f+%.0f"
            (int this.SelectionBounds.Width)
            (int this.SelectionBounds.Height)
            this.SelectionBounds.X
            this.SelectionBounds.Y
