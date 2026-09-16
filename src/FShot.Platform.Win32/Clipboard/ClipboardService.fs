namespace FShot.Platform.Win32.Clipboard

open System
open System.Runtime.InteropServices

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

module private Native =
    [<DllImport("user32.dll", SetLastError = true)>]
    extern bool OpenClipboard(IntPtr hWndNewOwner)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern bool CloseClipboard()

    [<DllImport("user32.dll", SetLastError = true)>]
    extern bool EmptyClipboard()

    [<DllImport("user32.dll", SetLastError = true)>]
    extern IntPtr SetClipboardData(uint32 uFormat, IntPtr hMem)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern uint32 RegisterClipboardFormat(string lpszFormat)

    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern IntPtr GlobalAlloc(uint32 uFlags, unativeint dwBytes)

    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern IntPtr GlobalLock(IntPtr hMem)

    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern bool GlobalUnlock(IntPtr hMem)

    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern IntPtr GlobalFree(IntPtr hMem)

    [<DllImport("ole32.dll")>]
    extern int OleFlushClipboard()

    [<DllImport("user32.dll")>]
    extern bool MessageBeep(uint32 uType)

/// Dịch vụ sao chép hình ảnh vào Clipboard chuẩn Win32.
/// Hỗ trợ cả 2 định dạng phổ biến: CF_DIB (tương thích mọi app Windows) và PNG (hỗ trợ alpha).
/// Bộ nhớ được chuyển giao cho hệ điều hành quản lý, nên dữ liệu clipboard vẫn tồn tại
/// ngay cả sau khi tiến trình FShot đóng lại.
module ClipboardService =

    let private GMEM_MOVEABLE = 0x0002u
    let private CF_DIB = 8u

    /// Sao chép ảnh vào Windows Clipboard với cả định dạng PNG và CF_DIB.
    let copyImageToClipboard (width: int) (height: int) (pngBytes: byte[]) (bgraPixels: byte[]) : bool =
        if width <= 0 || height <= 0 || isNull bgraPixels || bgraPixels.Length = 0 then
            false
        else
            let rowBytes = width * 4
            let pixelBytesLength = rowBytes * height
            let headerSize = Marshal.SizeOf(typeof<BITMAPINFOHEADER>)
            let totalSize = headerSize + pixelBytesLength

            // 1. Chuẩn bị global memory cho định dạng PNG
            let pngFormat = Native.RegisterClipboardFormat("PNG")
            let hPngGlobal =
                if not (isNull pngBytes) && pngBytes.Length > 0 then
                    let h = Native.GlobalAlloc(GMEM_MOVEABLE, unativeint pngBytes.Length)
                    if h <> IntPtr.Zero then
                        let ptr = Native.GlobalLock(h)
                        if ptr <> IntPtr.Zero then
                            Marshal.Copy(pngBytes, 0, ptr, pngBytes.Length)
                            Native.GlobalUnlock(h) |> ignore
                            h
                        else
                            Native.GlobalFree(h) |> ignore
                            IntPtr.Zero
                    else
                        IntPtr.Zero
                else
                    IntPtr.Zero

            // 2. Chuẩn bị global memory cho định dạng CF_DIB (bottom-up DIB chuẩn Windows)
            let hDibGlobal = Native.GlobalAlloc(GMEM_MOVEABLE, unativeint totalSize)
            let dibReady =
                if hDibGlobal <> IntPtr.Zero then
                    let ptr = Native.GlobalLock(hDibGlobal)
                    if ptr <> IntPtr.Zero then
                        let mutable header = BITMAPINFOHEADER()
                        header.biSize <- uint32 headerSize
                        header.biWidth <- width
                        header.biHeight <- height // dương = bottom-up DIB cho CF_DIB
                        header.biPlanes <- 1us
                        header.biBitCount <- 32us
                        header.biCompression <- 0u // BI_RGB
                        header.biSizeImage <- uint32 pixelBytesLength

                        Marshal.StructureToPtr(header, ptr, false)

                        let destPixelsPtr = IntPtr.Add(ptr, headerSize)

                        // Lật dòng từ top-down sang bottom-up khi copy vào DIB
                        for row in 0 .. height - 1 do
                            let srcRowOffset = row * rowBytes
                            let destRowOffset = (height - 1 - row) * rowBytes
                            let dst = IntPtr.Add(destPixelsPtr, destRowOffset)
                            let bytesToCopy = min rowBytes (bgraPixels.Length - srcRowOffset)
                            if bytesToCopy > 0 then
                                Marshal.Copy(bgraPixels, srcRowOffset, dst, bytesToCopy)

                        Native.GlobalUnlock(hDibGlobal) |> ignore
                        true
                    else
                        Native.GlobalFree(hDibGlobal) |> ignore
                        false
                else
                    false

            // 3. Mở Clipboard và chuyển giao dữ liệu cho Windows
            if not (Native.OpenClipboard(IntPtr.Zero)) then
                if hPngGlobal <> IntPtr.Zero then Native.GlobalFree(hPngGlobal) |> ignore
                if hDibGlobal <> IntPtr.Zero then Native.GlobalFree(hDibGlobal) |> ignore
                false
            else
                try
                    Native.EmptyClipboard() |> ignore
                    if hPngGlobal <> IntPtr.Zero then
                        Native.SetClipboardData(pngFormat, hPngGlobal) |> ignore
                    if dibReady && hDibGlobal <> IntPtr.Zero then
                        Native.SetClipboardData(CF_DIB, hDibGlobal) |> ignore
                    Native.CloseClipboard() |> ignore
                    try Native.OleFlushClipboard() |> ignore with _ -> ()
                    true
                with _ ->
                    Native.CloseClipboard() |> ignore
                    false

    /// Phát âm thanh thông báo ngắn gọn của Windows xác nhận đã copy thành công.
    let playNotificationSound () =
        try Native.MessageBeep(0x00000040u) |> ignore with _ -> ()
