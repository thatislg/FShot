namespace FShot.Platform.Win32.Hotkeys

open System
open System.Runtime.InteropServices
open System.Threading
open Microsoft.Win32
open Avalonia.Threading
open FShot.Core.Domain

/// Mã phím ảo Win32 (Virtual-Key Codes).
module VirtualKeyCodes =
    /// PrintScreen / PrtSc.
    let VK_SNAPSHOT = 0x2Cu

/// Các thông điệp Windows liên quan tới bàn phím và message loop.
module WindowsMessages =
    let WM_KEYDOWN = 0x0100u
    let WM_KEYUP = 0x0101u
    let WM_SYSKEYDOWN = 0x0104u
    let WM_SYSKEYUP = 0x0105u
    let WM_HOTKEY = 0x0312u
    let WM_QUIT = 0x0012u

    /// Thông điệp nội bộ báo thread message loop đăng ký lại hotkey (dynamic rebinding).
    let WM_APP_REBIND = 0x8001u

    /// WH_KEYBOARD_LL: low-level keyboard hook.
    let WH_KEYBOARD_LL = 13

/// Cờ modifier dùng cho RegisterHotKey.
[<Flags>]
type HotkeyModifiers =
    | None = 0
    | Alt = 0x0001
    | Control = 0x0002
    | Shift = 0x0004
    | Win = 0x0008
    | NoRepeat = 0x4000

/// P/Invoke declarations cho Win32 keyboard hook và message loop.
module private NativeMethods =
    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type KBDLLHOOKSTRUCT =
        val vkCode: uint32
        val scanCode: uint32
        val flags: uint32
        val time: uint32
        val dwExtraInfo: nativeint

    /// MSG Win32: bố cục 48 byte đúng trên x64 (kèm padding tường minh).
    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type NativeMessage =
        val Hwnd: nativeint
        val Msg: uint32
        val _padding1: uint32
        val WParam: nativeint
        val LParam: nativeint
        val Time: uint32
        val _padding2: uint32
        val PtX: int32
        val PtY: int32

    [<UnmanagedFunctionPointer(CallingConvention.Winapi)>]
    type LowLevelKeyboardProc =
        delegate of nCode: int * wParam: nativeint * lParam: nativeint -> nativeint

    [<DllImport("user32.dll", SetLastError = true)>]
    extern nativeint SetWindowsHookExW(int idHook, LowLevelKeyboardProc lpfn, nativeint hMod, uint32 dwThreadId)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern bool UnhookWindowsHookEx(nativeint hhk)

    [<DllImport("user32.dll")>]
    extern nativeint CallNextHookEx(nativeint hhk, int nCode, nativeint wParam, nativeint lParam)

    [<DllImport("kernel32.dll", CharSet = CharSet.Unicode)>]
    extern nativeint GetModuleHandleW(string lpModuleName)

    [<DllImport("kernel32.dll")>]
    extern uint32 GetCurrentThreadId()

    [<DllImport("user32.dll")>]
    extern int GetMessageW(NativeMessage& lpMsg, nativeint hWnd, uint32 wMsgFilterMin, uint32 wMsgFilterMax)

    [<DllImport("user32.dll")>]
    extern bool TranslateMessage(NativeMessage& lpMsg)

    [<DllImport("user32.dll")>]
    extern nativeint DispatchMessageW(NativeMessage& lpMsg)

    [<DllImport("user32.dll")>]
    extern bool PostThreadMessageW(uint32 idThread, uint32 Msg, nativeint wParam, nativeint lParam)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern bool RegisterHotKey(nativeint hWnd, int id, uint32 fsModifiers, uint32 vk)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern bool UnregisterHotKey(nativeint hWnd, int id)

/// Vô hiệu hóa / khôi phục việc Windows 11 chuyển hướng phím PrintScreen sang Snipping Tool.
/// Xem tài liệu 10_04_GlobalHotkey.md (FR-SYS-020, FR-WIN-003).
module SnippingTool =
    let private keyPath = "Control Panel\\Keyboard"
    let private valueName = "PrintScreenKeyForSnippingEnabled"

    /// Kiểm tra xem Windows có đang tự chuyển PrintScreen sang Snipping Tool hay không.
    let isPrintScreenRedirectEnabled () : bool =
        try
            use key = Registry.CurrentUser.OpenSubKey(keyPath)
            if isNull key then false
            else
                match key.GetValue(valueName) with
                | :? int as v -> v <> 0
                | _ -> false
        with _ ->
            false

    /// Bật/tắt việc Windows tự chuyển PrintScreen sang Snipping Tool.
    /// `enable = false` để F-Shot toàn quyền chiếm phím PrintScreen.
    let setPrintScreenRedirect (enable: bool) : Result<unit, string> =
        try
            use key = Registry.CurrentUser.CreateSubKey(keyPath)
            if isNull key then
                Error (sprintf "Cannot open registry key: %s" keyPath)
            else
                key.SetValue(valueName, (if enable then 1 else 0), RegistryValueKind.DWord)
                Ok ()
        with ex ->
            Error (sprintf "Failed to update Snipping Tool PrintScreen redirect: %s" ex.Message)

/// Một phím nóng cần đăng ký qua RegisterHotKey (không phải PrintScreen).
type HotkeyBinding = {
    Action: HotkeyAction
    Modifiers: HotkeyModifiers
    VirtualKey: uint32
}

/// Dịch vụ đăng ký phím nóng toàn cục.
/// - Phím PrintScreen (không modifier) được bắt bằng low-level keyboard hook (WH_KEYBOARD_LL)
///   vì RegisterHotKey với VK_SNAPSHOT hay bị Windows 11 chặn.
/// - Các phím nóng còn lại được đăng ký qua RegisterHotKey trên thread message loop.
/// `onHotkey` được gọi trên Avalonia UI Thread với hành động tương ứng.
type GlobalHotkeyService(onHotkey: HotkeyAction -> unit) =

    let mutable hookHandle = IntPtr.Zero
    let mutable threadId = 0u
    let mutable disposed = false
    let mutable keyDownHandled = false

    // Trạng thái đăng ký hotkey (RegisterHotKey), bảo vệ bằng lock.
    let gate = obj()
    let mutable pendingBindings : HotkeyBinding list = []
    let mutable registeredIds : Map<int, HotkeyBinding> = Map.empty
    let mutable printScreenAction : HotkeyAction option = None

    let dispatchAction (action: HotkeyAction) =
        try
            Dispatcher.UIThread.InvokeAsync(Action(fun () -> onHotkey action)) |> ignore
        with _ ->
            ()

    // Delegate được giữ làm field để tránh bị GC trong lúc hook còn hoạt động.
    let hookProc =
        NativeMethods.LowLevelKeyboardProc(fun nCode wParam lParam ->
            if nCode < 0 then
                NativeMethods.CallNextHookEx(hookHandle, nCode, wParam, lParam)
            else
                let kbd = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam)
                if kbd.vkCode = VirtualKeyCodes.VK_SNAPSHOT then
                    let msg = uint32 (wParam.ToInt64())
                    if msg = WindowsMessages.WM_KEYDOWN || msg = WindowsMessages.WM_SYSKEYDOWN then
                        if not keyDownHandled then
                            keyDownHandled <- true
                            printScreenAction |> Option.iter dispatchAction
                        nativeint 1
                    elif msg = WindowsMessages.WM_KEYUP || msg = WindowsMessages.WM_SYSKEYUP then
                        keyDownHandled <- false
                        nativeint 1
                    else
                        NativeMethods.CallNextHookEx(hookHandle, nCode, wParam, lParam)
                else
                    NativeMethods.CallNextHookEx(hookHandle, nCode, wParam, lParam))

    /// Đăng ký lại toàn bộ hotkey. Chỉ được gọi trên thread message loop.
    let registerCurrentBindings () =
        registeredIds
        |> Map.iter (fun id _ -> NativeMethods.UnregisterHotKey(IntPtr.Zero, id) |> ignore)
        registeredIds <- Map.empty

        let bindings = lock gate (fun () -> pendingBindings)
        let printScreen, normal =
            bindings |> List.partition (fun b -> b.VirtualKey = VirtualKeyCodes.VK_SNAPSHOT)

        // Phím PrintScreen (không modifier) do low-level hook xử lý.
        printScreenAction <-
            printScreen
            |> List.tryPick (fun b -> if b.Modifiers = HotkeyModifiers.None then Some b.Action else None)

        // Các phím khác đăng ký qua RegisterHotKey (kèm MOD_NOREPEAT để tránh auto-repeat).
        let mutable id = 1
        for b in normal do
            let modifiers = uint32 (int b.Modifiers) ||| uint32 (int HotkeyModifiers.NoRepeat)
            if NativeMethods.RegisterHotKey(IntPtr.Zero, id, modifiers, b.VirtualKey) then
                registeredIds <- registeredIds.Add(id, b)
                id <- id + 1

    // Message loop chạy trên thread riêng để OS gọi về low-level hook và gửi WM_HOTKEY.
    let messageLoop () =
        threadId <- NativeMethods.GetCurrentThreadId()
        registerCurrentBindings ()
        let mutable msg = Unchecked.defaultof<NativeMethods.NativeMessage>
        let mutable result = 1
        while result > 0 && not disposed do
            result <- NativeMethods.GetMessageW(&msg, IntPtr.Zero, 0u, 0u)
            if result > 0 then
                if msg.Msg = WindowsMessages.WM_HOTKEY then
                    let id = int (msg.WParam.ToInt64())
                    registeredIds
                    |> Map.tryFind id
                    |> Option.iter (fun b -> dispatchAction b.Action)
                elif msg.Msg = WindowsMessages.WM_APP_REBIND then
                    registerCurrentBindings ()
                else
                    NativeMethods.TranslateMessage(&msg) |> ignore
                    NativeMethods.DispatchMessageW(&msg) |> ignore

    /// Cài hook và khởi động message loop.
    member _.Start() : Result<unit, string> =
        if disposed then
            Error "Global hotkey service has been disposed"
        elif hookHandle <> IntPtr.Zero then
            Ok ()
        else
            try
                let moduleHandle = NativeMethods.GetModuleHandleW(null)
                let hook = NativeMethods.SetWindowsHookExW(WindowsMessages.WH_KEYBOARD_LL, hookProc, moduleHandle, 0u)
                if hook = IntPtr.Zero then
                    Error (sprintf "SetWindowsHookEx failed (error %d)" (Marshal.GetLastWin32Error()))
                else
                    hookHandle <- hook
                    let t = Thread(ThreadStart(fun () -> messageLoop()))
                    t.IsBackground <- true
                    t.Start()
                    Ok ()
            with ex ->
                Error (sprintf "Failed to start global hotkey service: %s" ex.Message)

    /// Cập nhật danh sách phím nóng và đăng ký lại mà không cần khởi động lại ứng dụng.
    member this.Rebind(bindings: HotkeyBinding list) : Result<unit, string> =
        if disposed then
            Error "Global hotkey service has been disposed"
        else
            lock gate (fun () -> pendingBindings <- bindings)
            if threadId <> 0u then
                NativeMethods.PostThreadMessageW(threadId, WindowsMessages.WM_APP_REBIND, IntPtr.Zero, IntPtr.Zero) |> ignore
            Ok ()

    /// Gỡ hook, hủy đăng ký hotkey và dừng message loop.
    member _.Dispose() =
        if not disposed then
            disposed <- true
            if hookHandle <> IntPtr.Zero then
                try
                    NativeMethods.UnhookWindowsHookEx(hookHandle) |> ignore
                with _ ->
                    ()
                hookHandle <- IntPtr.Zero
            registeredIds
            |> Map.iter (fun id _ ->
                try
                    NativeMethods.UnregisterHotKey(IntPtr.Zero, id) |> ignore
                with _ ->
                    ())
            registeredIds <- Map.empty
            if threadId <> 0u then
                try
                    NativeMethods.PostThreadMessageW(threadId, WindowsMessages.WM_QUIT, IntPtr.Zero, IntPtr.Zero) |> ignore
                with _ ->
                    ()

    interface IDisposable with
        member this.Dispose() = this.Dispose()
