namespace FShot.Platform.Win32.Capture

open System
open FShot.Core.Domain
open FShot.Core.Geometry

/// Helpers tạo và chuyển đổi bitmap đơn giản.
/// Trong PoC này dùng stub: tạo mảng byte BGRA32 giả lập.
/// Xem tài liệu 10_02_CaptureAdapter.md.
module BitmapAdapter =

    /// Tạo mảng byte BGRA32 từ thông tin kích thước.
    /// Công thức: totalBytes = width * height * 4.
    /// Xem 10_02_CaptureAdapter.md, mục 4.
    let createBgra32Pixels (width: int) (height: int) (fillColor: byte * byte * byte * byte) : byte[] =
        let (b, g, r, a) = fillColor
        let pixels = Array.zeroCreate (width * height * 4)

        for i in 0 .. width * height - 1 do
            pixels.[i * 4 + 0] <- b
            pixels.[i * 4 + 1] <- g
            pixels.[i * 4 + 2] <- r
            pixels.[i * 4 + 3] <- a

        pixels

    /// Tạo CaptureResult từ kích thước và màu lấp đầy.
    /// Dùng trong stub để kiểm tra pipeline.
    let createStubCaptureResult
        (virtualBounds: Rect)
        (scaleFactor: ScaleFactor)
        (screenIndex: int)
        (fillColor: byte * byte * byte * byte)
        : CaptureResult =

        let physicalWidth = int (Math.Round(virtualBounds.Width * scaleFactor.Value))
        let physicalHeight = int (Math.Round(virtualBounds.Height * scaleFactor.Value))
        let stride = physicalWidth * 4
        let pixels = createBgra32Pixels physicalWidth physicalHeight fillColor

        {
          Pixels = pixels
          Width = physicalWidth
          Height = physicalHeight
          Stride = stride
          PixelFormat = PixelFormat.Bgra32
          VirtualBounds = virtualBounds
          ScaleFactor = scaleFactor
          ScreenIndex = screenIndex
        }
