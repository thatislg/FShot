namespace FShot.Platform.Win32.Notifications

open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices

/// Loại thông báo F-Shot có thể phát.
type NotificationKind =
    | CaptureSuccess of filePath: string option
    | CopySuccess
    | CaptureAborted

/// Module nội bộ chứa các helper logging.
module private NotificationLog =
    let write message = Trace.WriteLine(sprintf "[FShot.Notification] %s" message)
    let writeEx message (ex: exn) =
        Trace.WriteLine(sprintf "[FShot.Notification] %s" message)
        Trace.WriteLine(sprintf "[FShot.Notification] EXCEPTION: %s" (ex.ToString()))

/// Module nội bộ chứa các P/Invoke binding Win32 cho balloon notification fallback.
module private NotificationPInvoke =
    // Win32 constants for creating a message-only window to own the tray icon.
    let HWND_MESSAGE = nativeint (-3)
    let WM_DESTROY = 0x0002u
    let IDC_ARROW = nativeint 32512

    /// Win32 NOTIFYICONDATA structure dùng cho Shell_NotifyIcon.
    [<Struct; StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)>]
    type NOTIFYICONDATA =
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

    [<Struct; StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)>]
    type WNDCLASSEX =
        val mutable cbSize: uint32
        val mutable style: uint32
        val mutable lpfnWndProc: IntPtr
        val mutable cbClsExtra: int
        val mutable cbWndExtra: int
        val mutable hInstance: IntPtr
        val mutable hIcon: IntPtr
        val mutable hCursor: IntPtr
        val mutable hbrBackground: IntPtr
        val mutable lpszMenuName: string
        [<MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)>]
        val mutable lpszClassName: string
        val mutable hIconSm: IntPtr

    // Delegate type that matches Win32 WNDPROC signature.
    type WndProcDelegate = delegate of IntPtr * uint32 * IntPtr * IntPtr -> IntPtr

    [<DllImport("shell32.dll", CharSet = CharSet.Unicode)>]
    extern bool Shell_NotifyIcon(uint32 dwMessage, NOTIFYICONDATA& lpdata)

    [<DllImport("user32.dll")>]
    extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName)

    [<DllImport("kernel32.dll")>]
    extern IntPtr GetConsoleWindow()

    [<DllImport("user32.dll", CharSet = CharSet.Unicode)>]
    extern IntPtr CreateWindowEx(uint32 dwExStyle, string lpClassName, string lpWindowName, uint32 dwStyle, int X, int Y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam)

    [<DllImport("user32.dll", CharSet = CharSet.Unicode)>]
    extern IntPtr DefWindowProc(IntPtr hWnd, uint32 uMsg, IntPtr wParam, IntPtr lParam)

    [<DllImport("user32.dll", CharSet = CharSet.Unicode)>]
    extern uint16 RegisterClassEx(WNDCLASSEX& lpwcx)

    [<DllImport("user32.dll")>]
    extern bool DestroyWindow(IntPtr hWnd)

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
    // Giữ delegate sống để GC không thu hồi khi window procedure vẫn được Win32 gọi.
    let mutable wndProcDelegate: NotificationPInvoke.WndProcDelegate option = None

    let getDefaultIcon () : IntPtr =
        // IDI_APPLICATION = 32512
        NotificationPInvoke.LoadIcon(IntPtr.Zero, nativeint 32512)

    /// Tạo message-only window để làm owner cho tray icon/balloon notification.
    /// Message-only window không có UI, phù hợp cho ứng dụng WinExe không có console.
    let createMessageOnlyWindow () : IntPtr =
        try
            let className = "FShotNotificationWindowClass"
            let mutable wcex = NotificationPInvoke.WNDCLASSEX()
            wcex.cbSize <- uint32 (Marshal.SizeOf(typeof<NotificationPInvoke.WNDCLASSEX>))
            wcex.style <- 0u
            // Lưu delegate trong biến mutable để giữ alive.
            let delegateInst =
                NotificationPInvoke.WndProcDelegate(fun hWnd uMsg _wParam _lParam ->
                    match uMsg with
                    | _ when uMsg = NotificationPInvoke.WM_DESTROY -> IntPtr.Zero
                    | _ -> NotificationPInvoke.DefWindowProc(hWnd, uMsg, _wParam, _lParam))
            wcex.lpfnWndProc <- Marshal.GetFunctionPointerForDelegate(delegateInst)
            wcex.cbClsExtra <- 0
            wcex.cbWndExtra <- 0
            wcex.hInstance <- Process.GetCurrentProcess().Handle
            wcex.hIcon <- IntPtr.Zero
            wcex.hCursor <- NotificationPInvoke.LoadIcon(IntPtr.Zero, NotificationPInvoke.IDC_ARROW)
            wcex.hbrBackground <- IntPtr.Zero
            wcex.lpszMenuName <- null
            wcex.lpszClassName <- className
            wcex.hIconSm <- IntPtr.Zero
            let atom = NotificationPInvoke.RegisterClassEx(&wcex)
            if atom = 0us then
                let err = Marshal.GetLastWin32Error()
                NotificationLog.write (sprintf "RegisterClassEx failed: %d" err)
                IntPtr.Zero
            else
                wndProcDelegate <- Some delegateInst
                let hwnd =
                    NotificationPInvoke.CreateWindowEx(
                        0u,
                        className,
                        "FShot Notification Window",
                        0u,
                        0, 0, 0, 0,
                        NotificationPInvoke.HWND_MESSAGE,
                        IntPtr.Zero,
                        wcex.hInstance,
                        IntPtr.Zero)
                if hwnd = IntPtr.Zero then
                    let err = Marshal.GetLastWin32Error()
                    NotificationLog.write (sprintf "CreateWindowEx failed: %d" err)
                else
                    NotificationLog.write (sprintf "Message-only window created: %A" hwnd)
                hwnd
        with ex ->
            NotificationLog.writeEx "Failed to create message-only window" ex
            IntPtr.Zero

    /// Lấy HWND để làm owner cho balloon tip.
    let ensureHwnd () : IntPtr =
        match lastHwnd with
        | Some h when h <> IntPtr.Zero -> h
        | _ ->
            let consoleHwnd = NotificationPInvoke.GetConsoleWindow()
            let hwnd =
                if consoleHwnd <> IntPtr.Zero then consoleHwnd
                else createMessageOnlyWindow()
            lastHwnd <- Some hwnd
            hwnd

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
        let mutable data = NotificationPInvoke.NOTIFYICONDATA()
        data.cbSize <- uint32 (Marshal.SizeOf(typeof<NotificationPInvoke.NOTIFYICONDATA>))
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
        data.guidItem <- Guid.Empty
        data.hBalloonIcon <- IntPtr.Zero
        data

    /// Phát thông báo Windows Balloon Tip.
    /// Trả về true nếu gọi Win32 thành công.
    let showBalloon (title: string) (text: string) (infoFlags: uint32) : bool =
        let hwnd = ensureHwnd()
        if hwnd = IntPtr.Zero then
            NotificationLog.write "Cannot show balloon: no valid HWND"
            false
        else
            let mutable data = createNotifyIconData hwnd
            data.szInfo <- text
            data.szInfoTitle <- title
            data.dwInfoFlags <- infoFlags
            let ok = NotificationPInvoke.Shell_NotifyIcon(NIM_ADD, &data)
            NotificationLog.write (sprintf "Shell_NotifyIcon ADD returned %b (hwnd=%A)" ok hwnd)
            ok

    /// Phát thông báo Windows Toast Notification qua Windows Community Toolkit.
    /// Trả về true nếu hiển thị thành công.
    let showToast (title: string) (text: string) : bool =
        try
            let builder =
                Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                    .AddText(title)
                    .AddText(text)
            builder.Show()
            NotificationLog.write (sprintf "Toast shown: %s - %s" title text)
            true
        with ex ->
            NotificationLog.writeEx "Toast notification failed" ex
            false

    /// Phát thông báo theo cấu hình.
    /// Thử Toast Notification trước; nếu lỗi thì fallback sang Balloon Notification.
    /// `force` cho phép bỏ qua cờ cấu hình (dùng cho abort nếu `showAbortNotification` false).
    member _.ShowNotification(kind: NotificationKind, enabled: bool, ?force: bool) : bool =
        let force = defaultArg force false
        NotificationLog.write (sprintf "ShowNotification called: kind=%A enabled=%b force=%b" kind enabled force)
        if not enabled && not force then
            NotificationLog.write "Notification skipped by config"
            false
        else
            let (title, text, balloonFlags) = buildContent kind
            NotificationLog.write (sprintf "Showing notification: %s - %s" title text)
            let toastOk = showToast title text
            if toastOk then
                NotificationLog.write "Toast notification succeeded"
                true
            else
                NotificationLog.write "Falling back to balloon notification"
                showBalloon title text balloonFlags

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
            NotificationPInvoke.Shell_NotifyIcon(NIM_DELETE, &data) |> ignore |> ignore
            // Nếu hwnd là message-only window do chúng ta tạo thì destroy nó.
            match wndProcDelegate with
            | Some _ ->
                try NotificationPInvoke.DestroyWindow(hwnd) |> ignore
                with _ -> ()
                wndProcDelegate <- None
            | None -> ()
            lastHwnd <- None
        | _ -> ()

    interface IDisposable with
        member this.Dispose() = this.Dispose()
