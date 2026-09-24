namespace FShot.Platform.Win32.Notifications

open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices

/// Win32 NOTIFYICONDATA structure dùng cho Shell_NotifyIcon.
/// Định nghĩa ở top-level trước module P/Invoke để module có thể tham chiếu.
[<Struct; StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)>]
type private NOTIFYICONDATA =
    val mutable cbSize: uint32
    val mutable hWnd: IntPtr
    val mutable uID: uint32
    val mutable uFlags: uint32
    val mutable uCallbackMessage: uint32
    val mutable hIcon: IntPtr
    [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)>]
    val mutable szTip: string
    val mutable dwState: uint32
    val mutable dwStateMask: uint32
    [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)>]
    val mutable szInfo: string
    val mutable uVersion: uint32
    [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)>]
    val mutable szInfoTitle: string
    val mutable dwInfoFlags: uint32
    val mutable guidItem: Guid
    val mutable hBalloonIcon: IntPtr

/// Module nội bộ chứa các P/Invoke binding Win32 cho thông báo.
module private NotificationPInvoke =
    [<DllImport("shell32.dll", CharSet = CharSet.Unicode)>]
    extern bool Shell_NotifyIcon(uint32 dwMessage, NOTIFYICONDATA& lpdata)

    [<DllImport("user32.dll")>]
    extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName)

    [<DllImport("kernel32.dll")>]
    extern IntPtr GetConsoleWindow()

/// Loại thông báo F-Shot có thể phát.
type NotificationKind =
    | CaptureSuccess of filePath: string option
    | CopySuccess
    | CaptureAborted

/// Dịch vụ thông báo Windows cho F-Shot.
/// Hỗ trợ fallback Balloon Notification qua Win32 API (NotifyIcon / Shell_NotifyIcon).
/// Toast Notification thực sự sẽ cần WinRT / Windows Community Toolkit; phần này để sẵn interface.
/// Xem tài liệu 10_08_Notifications.md.
type NotificationService() =

    // Constants cho Balloon Notification fallback.
    let NIM_ADD = 0x00000000u
    let NIM_DELETE = 0x00000002u
    let NIF_INFO = 0x00000010u
    let NIF_ICON = 0x00000002u
    let NIF_MESSAGE = 0x00000001u
    let NIF_TIP = 0x00000004u
    let NIIF_INFO = 0x00000001u
    let NIIF_WARNING = 0x00000002u
    let WM_USER = 0x0400u
    let TRAY_ICON_ID = 0xF1u

    let mutable lastHwnd: IntPtr option = None

    let getDefaultIcon () : IntPtr =
        // IDI_APPLICATION = 32512
        NotificationPInvoke.LoadIcon(IntPtr.Zero, nativeint 32512)

    /// Lấy HWND để làm owner cho balloon tip.
    let ensureHwnd () : IntPtr =
        match lastHwnd with
        | Some h when h <> IntPtr.Zero -> h
        | _ ->
            let consoleHwnd = NotificationPInvoke.GetConsoleWindow()
            lastHwnd <- Some consoleHwnd
            consoleHwnd

    let buildContent (kind: NotificationKind) : string * string * uint32 =
        match kind with
        | CaptureSuccess (Some path) ->
            let fileName = Path.GetFileName path
            ("Đã lưu ảnh chụp", fileName, NIIF_INFO)
        | CaptureSuccess None ->
            ("Đã lưu ảnh chụp", "", NIIF_INFO)
        | CopySuccess ->
            ("Đã sao chép", "Ảnh chụp đã được đưa vào clipboard", NIIF_INFO)
        | CaptureAborted ->
            ("Đã hủy", "Thao tác chụp màn hình bị hủy", NIIF_WARNING)

    let createNotifyIconData hwnd =
        let mutable data = NOTIFYICONDATA()
        data.cbSize <- uint32 (Marshal.SizeOf(typeof<NOTIFYICONDATA>))
        data.hWnd <- hwnd
        data.uID <- TRAY_ICON_ID
        data.uFlags <- NIF_INFO ||| NIF_ICON ||| NIF_MESSAGE ||| NIF_TIP
        data.uCallbackMessage <- WM_USER + 1u
        data.hIcon <- getDefaultIcon()
        data.szTip <- "F-Shot"
        data.dwState <- 0u
        data.dwStateMask <- 0u
        data.szInfo <- ""
        data.uVersion <- 0u
        data.szInfoTitle <- ""
        data.dwInfoFlags <- 0u
        data

    /// Phát thông báo Windows Balloon Tip.
    /// Trả về true nếu gọi Win32 thành công.
    let showBalloon (title: string) (text: string) (infoFlags: uint32) : bool =
        let hwnd = ensureHwnd()
        if hwnd = IntPtr.Zero then false
        else
            let mutable data = createNotifyIconData hwnd
            data.szInfo <- text
            data.szInfoTitle <- title
            data.dwInfoFlags <- infoFlags
            NotificationPInvoke.Shell_NotifyIcon(NIM_ADD, &data)

    /// Phát thông báo theo cấu hình.
    /// `force` cho phép bỏ qua cờ cấu hình (dùng cho abort nếu `showAbortNotification` false).
    member _.ShowNotification(kind: NotificationKind, enabled: bool, ?force: bool) : bool =
        let force = defaultArg force false
        if not enabled && not force then
            false
        else
            let (title, text, flags) = buildContent kind
            showBalloon title text flags

    /// Mở file ảnh trong Windows Explorer và highlight.
    /// Trả về true nếu khởi chạy explorer thành công.
    static member HighlightFileInExplorer(filePath: string) : bool =
        if not (File.Exists filePath) then false
        else
            try
                let args = sprintf "/select,\"%s\"" filePath
                let psi = ProcessStartInfo("explorer.exe", args)
                psi.UseShellExecute <- true
                Process.Start(psi) |> ignore
                true
            with _ ->
                false

    /// Dispose: xóa icon khỏi tray nếu đã thêm.
    member this.Dispose() =
        match lastHwnd with
        | Some hwnd when hwnd <> IntPtr.Zero ->
            let mutable data = createNotifyIconData hwnd
            NotificationPInvoke.Shell_NotifyIcon(NIM_DELETE, &data) |> ignore
            lastHwnd <- None
        | _ -> ()

    interface IDisposable with
        member this.Dispose() = this.Dispose()
