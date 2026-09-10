namespace FShot.Platform.Win32.Capture

open System
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Platform.Win32.Screen

/// Triển khai stub của ICaptureService.
/// Trong PoC này không dùng Windows.Graphics.Capture thực, mà tạo ảnh giả lập
/// để kiểm tra pipeline capture → render → UI.
/// Xem tài liệu 10_02_CaptureAdapter.md.
type StubCaptureService() =
    interface ICaptureService with

        /// Chụp một màn hình theo chỉ số (stub).
        member _.CaptureScreenAsync(screenIndex: int) =
            async {
                match ScreenEnumeration.getScreenByIndex screenIndex with
                | Some screen ->
                    let fillColor =
                        if screen.IsPrimary then
                            (255uy, 0uy, 0uy, 255uy) // Đỏ
                        else
                            (0uy, 0uy, 255uy, 255uy) // Xanh lam

                    let result =
                        BitmapAdapter.createStubCaptureResult
                            screen.VirtualBounds
                            screen.ScaleFactor
                            screen.Index
                            fillColor

                    return Ok result
                | None ->
                    return Error(CaptureError.ScreenNotFound screenIndex)
            }

        /// Chụp màn hình có con trỏ chuột (stub).
        /// Tìm màn hình chứa điểm (0, 0) hoặc dùng màn hình đầu tiên.
        member _.CaptureCursorScreenAsync() =
            async {
                let screens = ScreenEnumeration.getScreens ()

                if List.isEmpty screens then
                    return Error CaptureError.CaptureApiNotAvailable
                else
                    let screen =
                        ScreenEnumeration.getScreenContainingPoint { X = 0.0; Y = 0.0 }
                        |> Option.defaultValue (List.head screens)

                    let fillColor = (0uy, 255uy, 0uy, 255uy) // Xanh lục

                    let result =
                        BitmapAdapter.createStubCaptureResult
                            screen.VirtualBounds
                            screen.ScaleFactor
                            screen.Index
                            fillColor

                    return Ok result
            }

        /// Chụp toàn bộ Virtual Screen để dùng cho overlay (stub).
        member _.CaptureVirtualScreenAsync() =
            async {
                let bounds = ScreenEnumeration.getVirtualScreenBounds ()

                if bounds.Width <= 0.0 || bounds.Height <= 0.0 then
                    return Error CaptureError.CaptureApiNotAvailable
                else
                    let fillColor = (64uy, 64uy, 64uy, 255uy) // Xám đậm

                    let result =
                        BitmapAdapter.createStubCaptureResult
                            bounds
                            (ScaleFactor.Create 1.0)
                            0
                            fillColor

                    return Ok result
            }
