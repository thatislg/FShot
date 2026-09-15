namespace FShot.Core.Domain

open System
open FShot.Core.Geometry

/// Định dạng file ảnh.
/// Xem tài liệu 06_03_SaveOptions.md.
type FileFormat =
    | Png
    | Jpg

    /// Phần mở rộng chuẩn tương ứng.
    member this.Extension =
        match this with
        | Png -> ".png"
        | Jpg -> ".jpg"

    /// Phân giải FileFormat từ đuôi file.
    static member FromExtension(ext: string) : FileFormat =
        if String.IsNullOrWhiteSpace ext then Png
        else
            match ext.ToLowerInvariant().TrimStart('.') with
            | "jpg" | "jpeg" -> Jpg
            | _ -> Png

/// Mô-đun phân giải chuỗi mẫu tên file theo thời gian.
/// Xem tài liệu 06_03_SaveOptions.md, mục 2.
[<RequireQualifiedAccess>]
module FileNamePattern =
    /// Phân giải chuỗi mẫu tên file theo thời gian thực tế.
    /// Hỗ trợ các token:
    /// - %Y: Năm 4 chữ số (vd: 2026)
    /// - %m: Tháng 2 chữ số (vd: 09)
    /// - %d: Ngày 2 chữ số (vd: 15)
    /// - %H: Giờ 2 chữ số 24h (vd: 16)
    /// - %M: Phút 2 chữ số (vd: 30)
    /// - %S: Giây 2 chữ số (vd: 45)
    let resolve (pattern: string) (time: DateTime) : string =
        if String.IsNullOrWhiteSpace pattern then
            sprintf "fshot_%s" (time.ToString("yyyy-MM-dd-HHmmss"))
        else
            pattern
                .Replace("%Y", time.ToString("yyyy"))
                .Replace("%m", time.ToString("MM"))
                .Replace("%d", time.ToString("dd"))
                .Replace("%H", time.ToString("HH"))
                .Replace("%M", time.ToString("mm"))
                .Replace("%S", time.ToString("ss"))

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

    /// Phân giải tên file đầy đủ kèm extension theo thời gian chỉ định.
    member this.ResolveFileName(time: DateTime) : string =
        let baseName = FileNamePattern.resolve this.FileNamePattern time
        if baseName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
           baseName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
           baseName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) then
            baseName
        else
            baseName + this.Format.Extension

    /// Phân giải đường dẫn lưu file hoàn chỉnh (FR-OUT-02/03).
    /// - Nếu targetPath được cung cấp:
    ///   + Nếu là file (có phần mở rộng): dùng trực tiếp.
    ///   + Nếu là thư mục: kết hợp targetPath với tên file sinh từ pattern.
    /// - Nếu targetPath là None: kết hợp defaultFolder với tên file sinh từ pattern.
    member this.ResolveSavePath(targetPath: string option, defaultFolder: string, time: DateTime) : string =
        let fileName = this.ResolveFileName(time)
        match targetPath with
        | Some p when not (String.IsNullOrWhiteSpace p) ->
            let ext = System.IO.Path.GetExtension(p)
            if not (String.IsNullOrEmpty ext) then
                p
            else
                System.IO.Path.Combine(p, fileName)
        | _ ->
            System.IO.Path.Combine(defaultFolder, fileName)

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
