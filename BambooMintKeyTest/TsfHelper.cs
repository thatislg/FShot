using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BambooMintKeyTest;

public static class TsfHelper
{
    [DllImport("msctf.dll")]
    private static extern int TF_CreateThreadMgr(out IntPtr ppThreadMgr);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int ActivateDelegate(IntPtr thisPtr, out uint clientId);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int DeactivateDelegate(IntPtr thisPtr);

    private static IntPtr _threadMgr = IntPtr.Zero;
    private static uint _clientId = 0;
    private static bool _isActivated = false;

    public static bool IsActivated => _isActivated;
    public static uint ClientId => _clientId;

    public static (bool Success, string Message) Activate()
    {
        if (_isActivated)
            return (true, $"TSF đã kích hoạt trước đó (ClientId={_clientId})");

        try
        {
            int hr = TF_CreateThreadMgr(out _threadMgr);
            if (hr != 0 || _threadMgr == IntPtr.Zero)
            {
                return (false, $"TF_CreateThreadMgr thất bại (hr=0x{hr:X8})");
            }

            IntPtr vtable = Marshal.ReadIntPtr(_threadMgr);
            IntPtr activatePtr = Marshal.ReadIntPtr(vtable, 3 * IntPtr.Size);
            var activateFunc = Marshal.GetDelegateForFunctionPointer<ActivateDelegate>(activatePtr);

            int actHr = activateFunc(_threadMgr, out _clientId);
            if (actHr != 0)
            {
                return (false, $"ITfThreadMgr::Activate thất bại (hr=0x{actHr:X8})");
            }

            _isActivated = true;
            return (true, $"Kích hoạt TSF thành công! ClientId={_clientId}");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi kích hoạt TSF: {ex.Message}");
        }
    }
}

public static class Win32EditHelper
{
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_BORDER = 0x00800000;
    private const uint WS_EX_TOPMOST = 0x00000008;
    private const uint ES_MULTILINE = 0x0004;
    private const uint ES_AUTOVSCROLL = 0x0040;
    private const uint ES_WANTRETURN = 0x1000;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int w, int h, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("imm32.dll")]
    private static extern bool ImmAssociateContextEx(IntPtr hWnd, IntPtr hIMC, uint dwFlags);

    private static IntPtr _currentEditHwnd = IntPtr.Zero;

    public static IntPtr CreatePopupEdit(int x, int y, int w, int h)
    {
        if (_currentEditHwnd != IntPtr.Zero)
        {
            DestroyWindow(_currentEditHwnd);
            _currentEditHwnd = IntPtr.Zero;
        }

        uint style = WS_POPUP | WS_VISIBLE | WS_BORDER | ES_MULTILINE | ES_AUTOVSCROLL | ES_WANTRETURN;
        uint exStyle = WS_EX_TOPMOST;

        IntPtr hwnd = CreateWindowExW(exStyle, "EDIT", "Gõ thử vào Win32 EDIT này...", style, x, y, w, h, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (hwnd != IntPtr.Zero)
        {
            ImmAssociateContextEx(hwnd, IntPtr.Zero, 0x10); // IACE_DEFAULT
            SetForegroundWindow(hwnd);
            SetFocus(hwnd);
            _currentEditHwnd = hwnd;
        }

        return hwnd;
    }

    public static void ClosePopupEdit()
    {
        if (_currentEditHwnd != IntPtr.Zero)
        {
            DestroyWindow(_currentEditHwnd);
            _currentEditHwnd = IntPtr.Zero;
        }
    }
}
