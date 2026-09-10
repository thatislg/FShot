namespace FShot.Core.Geometry

open System

/// Điểm trong không gian Virtual Screen, dùng tọa độ logical pixel.
/// Một điểm gồm hai thành phần: X (ngang) và Y (dọc).
/// Cả hai giá trị đều dùng số thập phân để giữ độ chính xác khi chuyển đổi DPI.
[<Struct>]
type Point =
    {
      X: float
      Y: float
    }

    /// Điểm gốc (0, 0), tương ứng với góc trên bên trái của Virtual Screen.
    static member Zero = { X = 0.0; Y = 0.0 }

/// Vector dịch chuyển giữa các điểm.
/// Tương tự Point nhưng mang ý nghĩa là sự thay đổi vị trí, không phải vị trí tuyệt đối.
[<Struct>]
type Vector =
    {
      Dx: float
      Dy: float
    }

    /// Vector không đổi (0, 0).
    static member Zero = { Dx = 0.0; Dy = 0.0 }

/// Hình chữ nhật trong Virtual Screen space.
/// Lưu dưới dạng gốc trên bên trái (X, Y) và kích thước (Width, Height).
/// Cách này tránh dữ liệu không nhất quán giữa bốn góc.
[<Struct>]
type Rect =
    {
      X: float
      Y: float
      Width: float
      Height: float
    }

    /// Tính cạnh trái: Left = X.
    member this.Left = this.X

    /// Tính cạnh trên: Top = Y.
    member this.Top = this.Y

    /// Tính cạnh phải: Right = X + Width.
    /// Công thức suy ra từ định nghĩa gốc + kích thước trong tài liệu 01_03_Rect.md.
    member this.Right = this.X + this.Width

    /// Tính cạnh dưới: Bottom = Y + Height.
    /// Công thức suy ra từ định nghĩa gốc + kích thước trong tài liệu 01_03_Rect.md.
    member this.Bottom = this.Y + this.Height

    /// Góc trên bên trái của hình chữ nhật, là một Point.
    member this.TopLeft = { X = this.X; Y = this.Y }

    /// Góc dưới bên phải của hình chữ nhật.
    /// Công thức: (X + Width, Y + Height).
    member this.BottomRight = { X = this.X + this.Width; Y = this.Y + this.Height }

    /// Trung tâm của hình chữ nhật.
    /// Công thức: (X + Width / 2, Y + Height / 2).
    /// Xem ví dụ trong tài liệu 01_03_Rect.md, phần 4.1.
    member this.Center =
        {
          X = this.X + this.Width / 2.0
          Y = this.Y + this.Height / 2.0
        }

/// Màu gồm bốn kênh: đỏ, lục, lam, alpha.
/// Mỗi kênh là số nguyên từ 0 đến 255, phù hợp với định dạng hex #RRGGBBAA.
[<Struct>]
type Color =
    {
      R: byte
      G: byte
      B: byte
      A: byte
    }

    /// Màu trong suốt hoàn toàn.
    static member Transparent = { R = 0uy; G = 0uy; B = 0uy; A = 0uy }

    /// Màu đen đậm.
    static member Black = { R = 0uy; G = 0uy; B = 0uy; A = 255uy }

    /// Màu trắng đậm.
    static member White = { R = 255uy; G = 255uy; B = 255uy; A = 255uy }

    /// Chuyển màu sang chuỗi hex dạng #RRGGBBAA.
    /// Công thức: ghép từng kênh thành hai chữ số thập lục phân theo thứ tự R, G, B, A.
    /// Xem tài liệu 01_04_Color.md, mục 3.1.
    member this.ToHex() =
        sprintf "#%02X%02X%02X%02X" this.R this.G this.B this.A

    /// Tính độ sáng tương đối (luminance) theo công thức Rec. 601.
    /// Công thức: L = (0.299 * R + 0.587 * G + 0.114 * B) / 255.
    /// Kết quả nằm trong khoảng [0, 1]. Xem tài liệu 01_04_Color.md, mục 3.4.
    member this.Luminance() =
        let r = float this.R
        let g = float this.G
        let b = float this.B
        (0.299 * r + 0.587 * g + 0.114 * b) / 255.0

/// Độ dày nét vẽ, tính theo logical pixel.
/// Giá trị luôn dương và bị giới hạn trong khoảng cho phép.
[<Struct>]
type StrokeWidth =
    private | StrokeWidth of float

    /// Giá trị tối thiểu cho phép.
    static member MinValue = 1.0

    /// Giá trị tối đa cho phép.
    static member MaxValue = 50.0

    /// Tạo độ dày nét từ giá trị float, tự động giới hạn trong [MinValue, MaxValue].
    /// Công thức clamp: value' = max(MinValue, min(value, MaxValue)).
    /// Xem tài liệu 01_05_StrokeWidth.md, mục 3.1 và 3.2.
    static member Create(value: float) =
        let clamped = Math.Max(StrokeWidth.MinValue, Math.Min(value, StrokeWidth.MaxValue))
        StrokeWidth clamped

    /// Trả về giá trị float bên trong.
    member this.Value =
        let (StrokeWidth v) = this
        v

/// Hệ số phóng to DPI.
/// Giá trị 1.0 tương ứng 100%, 1.5 tương ứng 150%.
[<Struct>]
type ScaleFactor =
    private | ScaleFactor of float

    /// Hệ số mặc định 100%.
    static member OneHundred = ScaleFactor 1.0

    /// Tạo hệ số phóng to từ giá trị float. Giá trị phải lớn hơn 0.
    static member Create(value: float) =
        if value <= 0.0 then
            invalidArg (nameof value) "Hệ số phóng to phải lớn hơn 0."
        ScaleFactor value

    /// Trả về giá trị float bên trong.
    member this.Value =
        let (ScaleFactor v) = this
        v
