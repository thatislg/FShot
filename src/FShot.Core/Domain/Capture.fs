namespace FShot.Core.Domain

open System
open FShot.Core.Geometry

/// Các chế độ chụp màn hình.
/// Xem tài liệu 02_02_CaptureMode.md.
type CaptureMode =
    /// Chụp toàn bộ màn hình đang chứa con trỏ chuột.
    | FullScreen

    /// Chụp một màn hình cụ thể theo chỉ số.
    | SingleScreen of screenIndex: int

    /// Mở overlay tương tác để người dùng chọn vùng và vẽ chú thích.
    | GuiInteractive

    /// Dùng lại vùng chọn từ lần chụp trước.
    | LastRegion

/// Mục tiêu xuất ảnh sau khi chụp.
/// Xem tài liệu 02_03_CaptureRequest.md, mục 2.5.
type OutputTarget =
    /// Lưu ảnh vào đường dẫn file.
    | File of path: string

    /// Sao chép ảnh vào clipboard.
    | Clipboard

    /// Gửi ảnh dưới dạng binary ra stdout.
    | Stdout

    /// Mở overlay GUI để tiếp tục chỉnh sửa.
    | OpenGui

/// Yêu cầu chụp màn hình.
/// Xem tài liệu 02_03_CaptureRequest.md.
type CaptureRequest =
    {
      Mode: CaptureMode
      DelayMs: int
      InitialSelection: Rect option
      AcceptOnSelect: bool
      OutputTarget: OutputTarget
    }

    /// Tạo request mặc định với chế độ GuiInteractive và xuất ra GUI.
    static member Default =
        {
          Mode = GuiInteractive
          DelayMs = 0
          InitialSelection = None
          AcceptOnSelect = false
          OutputTarget = OpenGui
        }

    /// Tính thời điểm chụp thực tế từ thời điểm nhận request.
    /// Công thức: captureTime = requestTime + DelayMs.
    /// Xem 02_03_CaptureRequest.md, mục 3.
    member this.CaptureTime(requestTime: DateTime) =
        requestTime.AddMilliseconds(float this.DelayMs)

/// Định dạng pixel của bitmap.
/// Xem tài liệu 02_04_CaptureResult.md, mục 2.4.
type PixelFormat =
    | Bgra32
    | Rgba32
    | Rgb24

/// Kết quả chụp màn hình.
/// Xem tài liệu 02_04_CaptureResult.md.
type CaptureResult =
    {
      Pixels: byte[]
      Width: int
      Height: int
      Stride: int
      PixelFormat: PixelFormat
      VirtualBounds: Rect
      ScaleFactor: ScaleFactor
      ScreenIndex: int
    }

    /// Bytes mỗi pixel tùy theo định dạng.
    member this.BytesPerPixel =
        match this.PixelFormat with
        | Bgra32 -> 4
        | Rgba32 -> 4
        | Rgb24 -> 3

    /// Tổng số byte trong mảng Pixels.
    /// Công thức: totalBytes = Stride * Height.
    /// Xem 02_04_CaptureResult.md, mục 3.
    member this.TotalBytes = this.Stride * this.Height

    /// Chuyển vùng chọn logical sang physical để crop từ bitmap.
    /// Công thức: round(virtualX * s), round(virtualY * s),
    /// round(virtualWidth * s), round(virtualHeight * s).
    /// Xem 02_04_CaptureResult.md, mục 4.
    member this.LogicalSelectionToPhysical(selection: Rect) : Rect =
        let s = this.ScaleFactor.Value
        let left = Math.Round(selection.Left * s)
        let top = Math.Round(selection.Top * s)
        let right = Math.Round(selection.Right * s)
        let bottom = Math.Round(selection.Bottom * s)
        {
          X = left
          Y = top
          Width = right - left
          Height = bottom - top
        }

/// Lỗi có thể xảy ra khi chụp màn hình.
/// Xem tài liệu 02_05_WindowsGraphicsCapture.md, mục 6.
type CaptureError =
    | PermissionDenied
    | ScreenNotFound of index: int
    | CaptureApiNotAvailable
    | SurfaceIsEmpty
    | Unknown of message: string

/// Interface cho dịch vụ chụp màn hình.
/// Phần Platform.Win32 sẽ cung cấp triển khai cụ thể.
type ICaptureService =
    /// Chụp một màn hình theo chỉ số.
    abstract CaptureScreenAsync: screenIndex: int -> Async<Result<CaptureResult, CaptureError>>

    /// Chụp màn hình có con trỏ chuột.
    abstract CaptureCursorScreenAsync: unit -> Async<Result<CaptureResult, CaptureError>>

    /// Chụp toàn bộ Virtual Screen để dùng cho overlay.
    abstract CaptureVirtualScreenAsync: unit -> Async<Result<CaptureResult, CaptureError>>
