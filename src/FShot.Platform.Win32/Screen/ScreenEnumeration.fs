namespace FShot.Platform.Win32.Screen

open System
open System.Runtime.InteropServices
open FShot.Core.Geometry

/// P/Invoke và helper để liệt kê màn hình trên Windows.
/// Xem tài liệu 10_03_ScreenEnumeration.md.
module ScreenEnumeration =

    /// RECT struct dùng cho Win32 API.
    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type private Win32Rect =
        {
          Left: int
          Top: int
          Right: int
          Bottom: int
        }

    /// MONITORINFO struct.
    [<Struct; StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)>]
    type private MonitorInfo =
        {
          [<MarshalAs(UnmanagedType.U4)>]
          Size: uint32
          Monitor: Win32Rect
          WorkArea: Win32Rect
          [<MarshalAs(UnmanagedType.U4)>]
          Flags: uint32
        }

    /// Callback delegate cho EnumDisplayMonitors.
    type private MonitorEnumProc = delegate of IntPtr * IntPtr * Win32Rect * IntPtr -> bool

    [<DllImport("user32.dll")>]
    extern bool private EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData)

    [<DllImport("user32.dll", CharSet = CharSet.Auto)>]
    extern bool private GetMonitorInfo(IntPtr hMonitor, MonitorInfo& lpmi)

    [<DllImport("shcore.dll")>]
    extern int private GetDpiForMonitor(IntPtr hMonitor, int dpiType, uint32& dpiX, uint32& dpiY)

    /// DPI type: MDT_EFFECTIVE_DPI = 0.
    let private effectiveDpi = 0

    /// Callback duyệt qua từng màn hình.
    let private enumerateMonitors () : (IntPtr * MonitorInfo) list =
        let results = ResizeArray<IntPtr * MonitorInfo>()

        let callback =
            MonitorEnumProc(fun hMonitor _ _ _ ->
                let mutable info =
                    {
                      Size = uint32 (Marshal.SizeOf(typeof<MonitorInfo>))
                      Monitor = Unchecked.defaultof<Win32Rect>
                      WorkArea = Unchecked.defaultof<Win32Rect>
                      Flags = 0u
                    }

                if GetMonitorInfo(hMonitor, &info) then
                    results.Add(hMonitor, info)

                true)

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero) |> ignore
        results |> Seq.toList

    /// Lấy scale factor từ HMONITOR.
    let private getScaleFactor (hMonitor: IntPtr) : float =
        let mutable dpiX = 0u
        let mutable dpiY = 0u
        let hr = GetDpiForMonitor(hMonitor, effectiveDpi, &dpiX, &dpiY)

        if hr >= 0 && dpiX > 0u then
            float dpiX / 96.0
        else
            1.0

    /// Chuyển Win32Rect sang domain Rect.
    let private toDomainRect (r: Win32Rect) : Rect =
        {
          X = float r.Left
          Y = float r.Top
          Width = float (r.Right - r.Left)
          Height = float (r.Bottom - r.Top)
        }

    /// Lấy danh sách tất cả màn hình.
    let getScreens () : ScreenInfo list =
        enumerateMonitors ()
        |> List.mapi (fun index (hMonitor, info) ->
            let bounds = toDomainRect info.Monitor
            let scale = getScaleFactor hMonitor
            {
              Index = index
              Name = sprintf "\\\\.\\DISPLAY%d" (index + 1)
              IsPrimary = (info.Flags &&& 1u) <> 0u
              VirtualBounds = bounds
              ScaleFactor = ScaleFactor.Create scale
            })

    /// Lấy màn hình theo chỉ số.
    let getScreenByIndex (index: int) : ScreenInfo option =
        getScreens ()
        |> List.tryFind (fun s -> s.Index = index)

    /// Lấy màn hình chứa một điểm.
    let getScreenContainingPoint (point: Point) : ScreenInfo option =
        getScreens ()
        |> List.tryFind (fun s -> s.Contains point)

    /// Lấy Virtual Screen bounds từ danh sách màn hình.
    /// Công thức: min(left), min(top), max(right), max(bottom).
    let getVirtualScreenBounds () : Rect =
        let screens = getScreens ()
        if List.isEmpty screens then
            { X = 0.0; Y = 0.0; Width = 0.0; Height = 0.0 }
        else
            let left = screens |> List.minBy (fun s -> s.VirtualBounds.Left) |> fun s -> s.VirtualBounds.Left
            let top = screens |> List.minBy (fun s -> s.VirtualBounds.Top) |> fun s -> s.VirtualBounds.Top
            let right = screens |> List.maxBy (fun s -> s.VirtualBounds.Right) |> fun s -> s.VirtualBounds.Right
            let bottom = screens |> List.maxBy (fun s -> s.VirtualBounds.Bottom) |> fun s -> s.VirtualBounds.Bottom
            {
              X = left
              Y = top
              Width = right - left
              Height = bottom - top
            }
