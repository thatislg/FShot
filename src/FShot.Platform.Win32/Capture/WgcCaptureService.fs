namespace FShot.Platform.Win32.Capture

open System
open System.Runtime.InteropServices
open Windows.Graphics.Capture
open Windows.Graphics.DirectX
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Platform.Win32.Screen

/// Struct vị trí con trỏ chuột (Win32 POINT).
[<Struct; StructLayout(LayoutKind.Sequential)>]
type private CursorPoint =
    val mutable X: int
    val mutable Y: int

/// Helper đọc vị trí con trỏ chuột hiện tại (Win32 GetCursorPos).
module private NativeCursor =
    [<DllImport("user32.dll")>]
    extern bool GetCursorPos(CursorPoint& lpPoint)

    let getPosition () : Point =
        let mutable pt = CursorPoint()
        if GetCursorPos(&pt) then
            { X = float pt.X; Y = float pt.Y }
        else
            Point.Zero

/// Backend chụp màn hình hiện đại bằng Windows.Graphics.Capture (WinRT) + Direct3D 11.
/// Chụp đúng độ phân giải native của từng màn hình, khắc phục triệt để Mixed DPI.
/// Xem tài liệu 02_05_WindowsGraphicsCapture.md.
type WgcCaptureService() =

    /// Device Direct3D 11 dùng chung (Lazy Singleton) theo 10_02_CaptureAdapter.md, mục 4.1.
    static let device = lazy (Direct3DInterop.createDevice())
    static let direct3dDevice = lazy (Direct3DInterop.createDirect3DDevice device.Value)

    /// Poll frame từ pool với timeout giới hạn (frame đầu tiên có thể chưa sẵn sàng ngay).
    let rec tryGetFrame (pool: Direct3D11CaptureFramePool) (remainingAttempts: int) : Direct3D11CaptureFrame =
        if remainingAttempts <= 0 then
            null
        else
            let frame = pool.TryGetNextFrame()
            if isNull frame then
                System.Threading.Thread.Sleep(10)
                tryGetFrame pool (remainingAttempts - 1)
            else
                frame

    /// Chụp một màn hình (HMONITOR) ở đúng độ phân giải native physical của nó.
    let captureMonitor (hmonitor: nativeint) (screen: ScreenInfo) : Result<CaptureResult, CaptureError> =
        try
            let dev = device.Value
            let d3dDevice = direct3dDevice.Value
            let width = int (Math.Round(screen.VirtualBounds.Width * screen.ScaleFactor.Value))
            let height = int (Math.Round(screen.VirtualBounds.Height * screen.ScaleFactor.Value))

            if width <= 0 || height <= 0 then
                Error CaptureError.SurfaceIsEmpty
            else
                let item = Direct3DInterop.createItemForMonitor hmonitor
                let size = Windows.Graphics.SizeInt32(Width = width, Height = height)

                use pool =
                    Direct3D11CaptureFramePool.CreateFreeThreaded(
                        d3dDevice,
                        DirectXPixelFormat.B8G8R8A8UIntNormalized,
                        1,
                        size)

                use session = pool.CreateCaptureSession(item)
                session.IsCursorCaptureEnabled <- false
                session.StartCapture()

                let frame = tryGetFrame pool 100 // tối đa ~1 giây
                if isNull frame then
                    Error (CaptureError.Unknown "WGC: không nhận được frame nào từ frame pool")
                else
                    use f = frame
                    let surface = f.Surface
                    let pixels = Direct3DInterop.readPixels dev surface width height

                    Ok {
                        Pixels = pixels
                        Width = width
                        Height = height
                        Stride = width * 4
                        PixelFormat = PixelFormat.Bgra32
                        VirtualBounds = screen.VirtualBounds
                        ScaleFactor = screen.ScaleFactor
                        ScreenIndex = screen.Index
                    }
        with ex ->
            Error (CaptureError.Unknown (sprintf "WGC capture failed: %s" ex.Message))

    /// Downscale ảnh BGRA32 về kích thước logical (nearest-neighbor, dùng cho ghép virtual screen).
    let downscaleBgra (src: byte[]) (srcW: int) (srcH: int) (dstW: int) (dstH: int) : byte[] =
        let dst = Array.zeroCreate<byte> (dstW * dstH * 4)
        for y in 0 .. dstH - 1 do
            let sy = min (srcH - 1) (y * srcH / dstH)
            for x in 0 .. dstW - 1 do
                let sx = min (srcW - 1) (x * srcW / dstW)
                let si = (sy * srcW + sx) * 4
                let di = (y * dstW + x) * 4
                dst[di] <- src[si]
                dst[di + 1] <- src[si + 1]
                dst[di + 2] <- src[si + 2]
                dst[di + 3] <- src[si + 3]
        dst

    /// Kiểm tra hỗ trợ WGC của hệ điều hành.
    static member IsSupported = Direct3DInterop.isSupported()

    interface ICaptureService with

        member _.CaptureScreenAsync(screenIndex: int) =
            async {
                match ScreenEnumeration.getScreenByIndex screenIndex with
                | Some screen ->
                    match ScreenEnumeration.getMonitorHandle screenIndex with
                    | Some hmonitor -> return captureMonitor hmonitor screen
                    | None -> return Error (CaptureError.ScreenNotFound screenIndex)
                | None ->
                    return Error (CaptureError.ScreenNotFound screenIndex)
            }

        member _.CaptureCursorScreenAsync() =
            async {
                let cursorPoint = NativeCursor.getPosition()
                match ScreenEnumeration.getScreenContainingPoint cursorPoint with
                | Some screen ->
                    match ScreenEnumeration.getMonitorHandle screen.Index with
                    | Some hmonitor -> return captureMonitor hmonitor screen
                    | None -> return Error (CaptureError.CaptureApiNotAvailable)
                | None ->
                    // Fallback: chụp màn hình chính.
                    match ScreenEnumeration.getScreens() |> List.tryHead with
                    | Some screen ->
                        match ScreenEnumeration.getMonitorHandle screen.Index with
                        | Some hmonitor -> return captureMonitor hmonitor screen
                        | None -> return Error (CaptureError.CaptureApiNotAvailable)
                    | None -> return Error CaptureError.CaptureApiNotAvailable
            }

        member _.CaptureVirtualScreenAsync() =
            async {
                let screens = ScreenEnumeration.getScreens()
                if List.isEmpty screens then
                    return Error CaptureError.CaptureApiNotAvailable
                else
                    // Chụp độc lập từng màn hình ở độ phân giải native, rồi ghép về canvas logical.
                    let captures =
                        screens
                        |> List.map (fun screen ->
                            match ScreenEnumeration.getMonitorHandle screen.Index with
                            | Some hmonitor ->
                                match captureMonitor hmonitor screen with
                                | Ok result -> Some result
                                | Error _ -> None
                            | None -> None)

                    if List.exists Option.isSome captures then
                        let virtualBounds = ScreenEnumeration.getVirtualScreenBounds()
                        let vw = int (Math.Round virtualBounds.Width)
                        let vh = int (Math.Round virtualBounds.Height)
                        let canvas = Array.zeroCreate<byte> (vw * vh * 4)

                        List.zip captures screens
                        |> List.iter (fun (captureOpt, screen) ->
                            match captureOpt with
                            | Some capture ->
                                // Downscale ảnh native về kích thước logical của màn hình.
                                let lw = int (Math.Round screen.VirtualBounds.Width)
                                let lh = int (Math.Round screen.VirtualBounds.Height)
                                let scaled =
                                    if lw = capture.Width && lh = capture.Height then
                                        capture.Pixels
                                    else
                                        downscaleBgra capture.Pixels capture.Width capture.Height lw lh

                                // Đặt vào đúng vị trí logical trong virtual canvas.
                                let ox = int (Math.Round(screen.VirtualBounds.X - virtualBounds.X))
                                let oy = int (Math.Round(screen.VirtualBounds.Y - virtualBounds.Y))
                                for y in 0 .. lh - 1 do
                                    for x in 0 .. lw - 1 do
                                        let si = (y * lw + x) * 4
                                        let di = ((oy + y) * vw + (ox + x)) * 4
                                        if di >= 0 && di + 3 < canvas.Length then
                                            canvas[di] <- scaled[si]
                                            canvas[di + 1] <- scaled[si + 1]
                                            canvas[di + 2] <- scaled[si + 2]
                                            canvas[di + 3] <- scaled[si + 3]
                            | None -> ())

                        return Ok {
                            Pixels = canvas
                            Width = vw
                            Height = vh
                            Stride = vw * 4
                            PixelFormat = PixelFormat.Bgra32
                            VirtualBounds = virtualBounds
                            ScaleFactor = ScaleFactor.Create 1.0
                            ScreenIndex = 0
                        }
                    else
                        return Error CaptureError.CaptureApiNotAvailable
            }
