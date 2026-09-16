namespace FShot.Platform.Win32.Capture

open System
open System.Runtime.InteropServices
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Platform.Win32.Screen

[<Struct; StructLayout(LayoutKind.Sequential)>]
type private BITMAPINFOHEADER =
    val mutable biSize: uint32
    val mutable biWidth: int32
    val mutable biHeight: int32
    val mutable biPlanes: uint16
    val mutable biBitCount: uint16
    val mutable biCompression: uint32
    val mutable biSizeImage: uint32
    val mutable biXPelsPerMeter: int32
    val mutable biYPelsPerMeter: int32
    val mutable biClrUsed: uint32
    val mutable biClrImportant: uint32

[<Struct; StructLayout(LayoutKind.Sequential)>]
type private BITMAPINFO =
    val mutable bmiHeader: BITMAPINFOHEADER

[<Struct; StructLayout(LayoutKind.Sequential)>]
type private Win32Point =
    val mutable X: int
    val mutable Y: int

module private Native =
    [<DllImport("user32.dll", SetLastError = true)>]
    extern IntPtr OpenWindowStation(string lpszWinSta, bool fInherit, uint32 dwDesiredAccess)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern bool SetProcessWindowStation(IntPtr hWinSta)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern IntPtr OpenDesktop(string lpszDesktop, uint32 dwFlags, bool fInherit, uint32 dwDesiredAccess)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern bool SetThreadDesktop(IntPtr hDesktop)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern IntPtr GetDC(IntPtr hWnd)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern int ReleaseDC(IntPtr hWnd, IntPtr hDC)

    [<DllImport("gdi32.dll", SetLastError = true)>]
    extern IntPtr CreateCompatibleDC(IntPtr hdc)

    [<DllImport("gdi32.dll", SetLastError = true)>]
    extern bool DeleteDC(IntPtr hdc)

    [<DllImport("gdi32.dll", SetLastError = true)>]
    extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight)

    [<DllImport("gdi32.dll", SetLastError = true)>]
    extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj)

    [<DllImport("gdi32.dll", SetLastError = true)>]
    extern bool DeleteObject(IntPtr hObject)

    [<DllImport("gdi32.dll", SetLastError = true)>]
    extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint32 dwRop)

    [<DllImport("gdi32.dll", SetLastError = true)>]
    extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint32 uStartScan, uint32 cScanLines, IntPtr lpvBits, BITMAPINFO& lpbi, uint32 uUsage)

    [<DllImport("user32.dll")>]
    extern int GetSystemMetrics(int nIndex)

    [<DllImport("user32.dll")>]
    extern bool GetCursorPos(Win32Point& lpPoint)

/// Triển khai chụp màn hình Windows thực tế thông qua Win32 GDI BitBlt.
/// Đảm bảo quyền truy cập window station/desktop và đọc pixel BGRA32 trực tiếp từ màn hình.
type WindowsCaptureService() =

    static let ensureInteractiveDesktop () =
        try
            let hw = Native.OpenWindowStation("winsta0", false, 0x0000037Fu)
            if hw <> IntPtr.Zero then Native.SetProcessWindowStation(hw) |> ignore
            let hd = Native.OpenDesktop("default", 0u, false, 0x000001FFu)
            if hd <> IntPtr.Zero then Native.SetThreadDesktop(hd) |> ignore
        with _ ->
            ()

    static let captureRect (srcX: int) (srcY: int) (width: int) (height: int) (virtualBounds: Rect) (scale: ScaleFactor) (screenIndex: int) : Result<CaptureResult, CaptureError> =
        if width <= 0 || height <= 0 then
            Error CaptureError.SurfaceIsEmpty
        else
            ensureInteractiveDesktop ()
            let hdcSrc = Native.GetDC(IntPtr.Zero)
            if hdcSrc = IntPtr.Zero then
                Error CaptureError.CaptureApiNotAvailable
            else
                try
                    let hdcMem = Native.CreateCompatibleDC(hdcSrc)
                    if hdcMem = IntPtr.Zero then
                        Error CaptureError.CaptureApiNotAvailable
                    else
                        try
                            let hBitmap = Native.CreateCompatibleBitmap(hdcSrc, width, height)
                            if hBitmap = IntPtr.Zero then
                                Error CaptureError.SurfaceIsEmpty
                            else
                                try
                                    let oldBmp = Native.SelectObject(hdcMem, hBitmap)
                                    let SRCCOPY = 0x00CC0020u
                                    let CAPTUREBLT = 0x40000000u
                                    let ok = Native.BitBlt(hdcMem, 0, 0, width, height, hdcSrc, srcX, srcY, SRCCOPY ||| CAPTUREBLT)
                                    let success = if ok then true else Native.BitBlt(hdcMem, 0, 0, width, height, hdcSrc, srcX, srcY, SRCCOPY)

                                    if not success then
                                        Native.SelectObject(hdcMem, oldBmp) |> ignore
                                        Error (CaptureError.Unknown (sprintf "BitBlt failed with code %d" (Marshal.GetLastWin32Error())))
                                    else
                                        let mutable bmi = BITMAPINFO()
                                        bmi.bmiHeader.biSize <- uint32 (Marshal.SizeOf(typeof<BITMAPINFOHEADER>))
                                        bmi.bmiHeader.biWidth <- width
                                        bmi.bmiHeader.biHeight <- -height // top-down
                                        bmi.bmiHeader.biPlanes <- 1us
                                        bmi.bmiHeader.biBitCount <- 32us
                                        bmi.bmiHeader.biCompression <- 0u // BI_RGB

                                        let totalBytes = width * height * 4
                                        let pixels = Array.zeroCreate<byte> totalBytes
                                        let handle = GCHandle.Alloc(pixels, GCHandleType.Pinned)
                                        try
                                            let lines = Native.GetDIBits(hdcMem, hBitmap, 0u, uint32 height, handle.AddrOfPinnedObject(), &bmi, 0u)
                                            Native.SelectObject(hdcMem, oldBmp) |> ignore
                                            if lines = 0 then
                                                Error (CaptureError.Unknown "GetDIBits returned 0 lines")
                                            else
                                                Ok {
                                                    Pixels = pixels
                                                    Width = width
                                                    Height = height
                                                    Stride = width * 4
                                                    PixelFormat = PixelFormat.Bgra32
                                                    VirtualBounds = virtualBounds
                                                    ScaleFactor = scale
                                                    ScreenIndex = screenIndex
                                                }
                                        finally
                                            handle.Free()
                                finally
                                    Native.DeleteObject(hBitmap) |> ignore
                        finally
                            Native.DeleteDC(hdcMem) |> ignore
                finally
                    Native.ReleaseDC(IntPtr.Zero, hdcSrc) |> ignore

    interface ICaptureService with

        member _.CaptureScreenAsync(screenIndex: int) =
            async {
                match ScreenEnumeration.getScreenByIndex screenIndex with
                | Some screen ->
                    let b = screen.VirtualBounds
                    let s = screen.ScaleFactor.Value
                    let x = int (Math.Round(b.X * s))
                    let y = int (Math.Round(b.Y * s))
                    let w = int (Math.Round(b.Width * s))
                    let h = int (Math.Round(b.Height * s))
                    return captureRect x y w h b screen.ScaleFactor screenIndex
                | None ->
                    return Error(CaptureError.ScreenNotFound screenIndex)
            }

        member _.CaptureCursorScreenAsync() =
            async {
                let mutable pt = Win32Point()
                let hasPt = Native.GetCursorPos(&pt)
                let point = if hasPt then { X = float pt.X; Y = float pt.Y } else { X = 0.0; Y = 0.0 }
                let screenOpt =
                    ScreenEnumeration.getScreenContainingPoint point
                    |> Option.orElseWith (fun () -> ScreenEnumeration.getScreens() |> List.tryHead)
                match screenOpt with
                | Some screen ->
                    let b = screen.VirtualBounds
                    let s = screen.ScaleFactor.Value
                    let x = int (Math.Round(b.X * s))
                    let y = int (Math.Round(b.Y * s))
                    let w = int (Math.Round(b.Width * s))
                    let h = int (Math.Round(b.Height * s))
                    return captureRect x y w h b screen.ScaleFactor screen.Index
                | None ->
                    return Error CaptureError.CaptureApiNotAvailable
            }

        member _.CaptureVirtualScreenAsync() =
            async {
                let SM_XVIRTUALSCREEN = 76
                let SM_YVIRTUALSCREEN = 77
                let SM_CXVIRTUALSCREEN = 78
                let SM_CYVIRTUALSCREEN = 79

                let x = Native.GetSystemMetrics(SM_XVIRTUALSCREEN)
                let y = Native.GetSystemMetrics(SM_YVIRTUALSCREEN)
                let w = Native.GetSystemMetrics(SM_CXVIRTUALSCREEN)
                let h = Native.GetSystemMetrics(SM_CYVIRTUALSCREEN)

                let vBounds = ScreenEnumeration.getVirtualScreenBounds()
                let (vx, vy, vw, vh) =
                    if w > 0 && h > 0 then
                        (float x, float y, float w, float h)
                    else
                        (vBounds.X, vBounds.Y, vBounds.Width, vBounds.Height)

                let bounds = { X = vx; Y = vy; Width = vw; Height = vh }
                let primaryScale =
                    ScreenEnumeration.getScreens()
                    |> List.tryFind (fun s -> s.IsPrimary)
                    |> Option.map (fun s -> s.ScaleFactor)
                    |> Option.defaultValue (ScaleFactor.Create 1.0)

                return captureRect (int vx) (int vy) (int vw) (int vh) bounds primaryScale 0
            }

/// Triển khai stub của ICaptureService (dùng cho unit tests và fallback).
type StubCaptureService() =
    interface ICaptureService with

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
