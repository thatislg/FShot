module FShot.Core.Tests.Domain.ExportTests

open FShot.Core.Geometry
open FShot.Core.Domain
open Xunit

let private rect x y w h =
    {
      X = x
      Y = y
      Width = w
      Height = h
    }

let private captureResult scale =
    {
      Pixels = Array.empty
      Width = 2880
      Height = 1620
      Stride = 11520
      PixelFormat = PixelFormat.Bgra32
      VirtualBounds = rect 0.0 0.0 1920.0 1080.0
      ScaleFactor = ScaleFactor.Create scale
      ScreenIndex = 0
    }

/// Kiểm tra SaveOptions mặc định.
[<Fact>]
let ``SaveOptions mặc định`` () =
    let options = SaveOptions.Default
    Assert.True(options.Path.IsNone)
    Assert.Equal("fshot_%Y-%m-%d-%H%M%S", options.FileNamePattern)
    Assert.Equal(Png, options.Format)
    Assert.Equal(90, options.NormalizedJpegQuality)

/// Kiểm tra JpegQuality bị giới hạn trong [0, 100].
[<Fact>]
let ``JpegQuality bị giới hạn`` () =
    let tooHigh = { SaveOptions.Default with JpegQuality = 150 }
    Assert.Equal(100, tooHigh.NormalizedJpegQuality)

    let tooLow = { SaveOptions.Default with JpegQuality = -10 }
    Assert.Equal(0, tooLow.NormalizedJpegQuality)

/// Kiểm tra tính kích thước output.
/// Công thức: round(w * s), round(h * s).
[<Fact>]
let ``OutputSize tính đúng với scale 1.5`` () =
    let request =
        {
          CaptureResult = captureResult 1.5
          SelectionBounds = rect 100.0 80.0 300.0 200.0
          Annotations = []
          Target = CopyToClipboard
          SaveOptions = SaveOptions.Default
        }
    let width, height = request.OutputSize
    Assert.Equal(450, width)
    Assert.Equal(300, height)

/// Kiểm tra chuỗi geometry WxH+X+Y.
[<Fact>]
let ``GeometryString đúng định dạng`` () =
    let request =
        {
          CaptureResult = captureResult 1.0
          SelectionBounds = rect 100.0 80.0 300.0 200.0
          Annotations = []
          Target = PrintGeometry
          SaveOptions = SaveOptions.Default
        }
    Assert.Equal("300x200+100+80", request.GeometryString)

/// Kiểm tra FileNamePattern phân giải đúng các token thời gian.
[<Fact>]
let ``FileNamePattern phân giải token thời gian chuẩn xác`` () =
    let dt = System.DateTime(2026, 9, 15, 16, 30, 45)
    let pattern = "fshot_%Y-%m-%d_%H%M%S"
    let resolved = FileNamePattern.resolve pattern dt
    Assert.Equal("fshot_2026-09-15_163045", resolved)

/// Kiểm tra FileNamePattern fallback khi pattern rỗng.
[<Fact>]
let ``FileNamePattern fallback khi pattern rỗng`` () =
    let dt = System.DateTime(2026, 9, 15, 16, 30, 45)
    let resolved = FileNamePattern.resolve "" dt
    Assert.Equal("fshot_2026-09-15-163045", resolved)

/// Kiểm tra SaveOptions.ResolveFileName thêm phần mở rộng phù hợp.
[<Fact>]
let ``SaveOptions ResolveFileName thêm phần mở rộng tự động`` () =
    let dt = System.DateTime(2026, 9, 15, 16, 30, 45)
    let pngOptions = { SaveOptions.Default with Format = Png }
    Assert.Equal("fshot_2026-09-15-163045.png", pngOptions.ResolveFileName dt)

    let jpgOptions = { SaveOptions.Default with Format = Jpg }
    Assert.Equal("fshot_2026-09-15-163045.jpg", jpgOptions.ResolveFileName dt)

    // Nếu pattern đã có sẵn đuôi file, không nối thừa
    let patternWithExt = { SaveOptions.Default with FileNamePattern = "capture_%Y%m%d.png"; Format = Png }
    Assert.Equal("capture_20260915.png", patternWithExt.ResolveFileName dt)

/// Kiểm tra SaveOptions.ResolveSavePath xử lý đúng đường dẫn file và thư mục.
[<Fact>]
let ``SaveOptions ResolveSavePath phân giải đúng đường dẫn`` () =
    let dt = System.DateTime(2026, 9, 15, 16, 30, 45)
    let options = SaveOptions.Default
    let defaultFolder = "C:\\Users\\Test\\Pictures"

    // 1. targetPath là None -> lưu vào defaultFolder với tên file sinh từ pattern
    let path1 = options.ResolveSavePath(None, defaultFolder, dt)
    Assert.Equal("C:\\Users\\Test\\Pictures\\fshot_2026-09-15-163045.png", path1)

    // 2. targetPath là thư mục (không có đuôi file) -> lưu vào thư mục đó với tên file sinh từ pattern
    let path2 = options.ResolveSavePath(Some "D:\\Captures", defaultFolder, dt)
    Assert.Equal("D:\\Captures\\fshot_2026-09-15-163045.png", path2)

    // 3. targetPath là file cụ thể có extension -> giữ nguyên đường dẫn chỉ định
    let path3 = options.ResolveSavePath(Some "D:\\Captures\\my_shot.png", defaultFolder, dt)
    Assert.Equal("D:\\Captures\\my_shot.png", path3)

/// Kiểm tra FileFormat.FromExtension phân giải đúng.
[<Fact>]
let ``FileFormat FromExtension nhận diện đúng định dạng`` () =
    Assert.Equal(Png, FileFormat.FromExtension(".png"))
    Assert.Equal(Png, FileFormat.FromExtension("png"))
    Assert.Equal(Jpg, FileFormat.FromExtension(".jpg"))
    Assert.Equal(Jpg, FileFormat.FromExtension(".jpeg"))
    Assert.Equal(Png, FileFormat.FromExtension(".bmp")) // fallback Png
