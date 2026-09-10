namespace FShot.Platform.Win32.Screen

open System
open FShot.Core.Geometry

/// Thông tin về một màn hình trong hệ thống.
/// Xem tài liệu 10_03_ScreenEnumeration.md.
type ScreenInfo =
    {
      Index: int
      Name: string
      IsPrimary: bool
      VirtualBounds: Rect
      ScaleFactor: ScaleFactor
    }

    /// Tính kích thước physical pixel.
    /// Công thức: round(virtualWidth * scale), round(virtualHeight * scale).
    member this.PhysicalSize =
        let s = this.ScaleFactor.Value
        let width = int (Math.Round(this.VirtualBounds.Width * s))
        let height = int (Math.Round(this.VirtualBounds.Height * s))
        (width, height)

    /// Kiểm tra một điểm logical có nằm trong màn hình không.
    /// Công thức: x >= L && x < R && y >= T && y < B.
    member this.Contains(point: Point) =
        point.X >= this.VirtualBounds.Left
        && point.X < this.VirtualBounds.Right
        && point.Y >= this.VirtualBounds.Top
        && point.Y < this.VirtualBounds.Bottom
