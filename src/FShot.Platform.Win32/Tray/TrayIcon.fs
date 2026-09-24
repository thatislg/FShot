namespace FShot.Platform.Win32.Tray

open System
open Avalonia
open Avalonia.Controls
open FShot.Platform.Win32.Screen
open FShot.Rendering.Skia.Icons

/// Các lệnh có thể phát sinh từ tray menu hoặc click icon.
/// Caller (thường là App.axaml.fs) sẽ dispatch sang xử lý phù hợp.
type TrayCommand =
    | GuiCapture
    | CaptureScreen of screenIndex: int
    | LaunchWithDelay of delayMs: int
    | OpenAbout
    | OpenSettings
    | OpenSaveFolder
    | Exit

/// Text hiển thị trên menu tray.
type TrayMenuText =
    {
      CaptureGui: string
      CaptureScreen: string
      CaptureScreenFormat: string
      Launcher: string
      About: string
      Settings: string
      OpenSaveFolder: string
      Exit: string
    }

    /// Text mặc định tiếng Việt.
    static member Vietnamese =
        {
          CaptureGui = "Chụp màn hình (GUI)"
          CaptureScreen = "Chụp theo màn hình"
          CaptureScreenFormat = "Màn hình {0}: {1}"
          Launcher = "Trình phóng nhanh..."
          About = "Thông tin & Phím tắt"
          Settings = "Cài đặt"
          OpenSaveFolder = "Mở thư mục ảnh chụp"
          Exit = "Thoát F-Shot"
        }

/// Module tiện ích cho text menu.
module TrayMenuText =
    let defaultText = TrayMenuText.Vietnamese

/// Dịch vụ quản lý biểu tượng tray và menu ngữ cảnh.
/// Tách biệt hoàn toàn việc build menu / icon với việc xử lý business logic,
/// nên chỉ nhận một `dispatch` function từ caller.
type TrayIconService(application: Application, dispatch: TrayCommand -> unit, menuText: TrayMenuText) =

    let mutable trayIcon: TrayIcon option = None
    let mutable lastClickTime = DateTime.MinValue

    /// Tạo `WindowIcon` từ vector Kawaii đã render bằng SkiaSharp.
    let createWindowIcon () : WindowIcon =
        let stream = TrayIconBitmap.createPngStream 64
        new WindowIcon(stream)

    /// Build submenu "Chụp theo màn hình" dựa trên `ScreenEnumeration.getScreens()`.
    let buildScreenSubmenu () : NativeMenu =
        let submenu = new NativeMenu()
        let screens = ScreenEnumeration.getScreens()

        if List.isEmpty screens then
            let emptyItem = new NativeMenuItem("(không phát hiện màn hình)")
            emptyItem.IsEnabled <- false
            submenu.Items.Add(emptyItem)
        else
            screens
            |> List.iter (fun screen ->
                let label = String.Format(menuText.CaptureScreenFormat, screen.Index + 1, screen.Name)
                let item = new NativeMenuItem(label)
                item.Click.Add(fun _ -> dispatch (CaptureScreen screen.Index))
                submenu.Items.Add(item)
            )

        submenu

    /// Build toàn bộ menu ngữ cảnh.
    let buildMenu () : NativeMenu =
        let menu = new NativeMenu()

        // FR-SYS-002: Chụp màn hình (GUI).
        let captureGuiItem = new NativeMenuItem(menuText.CaptureGui)
        captureGuiItem.Click.Add(fun _ -> dispatch GuiCapture)
        menu.Items.Add(captureGuiItem)

        // FR-SYS-003: Chụp theo màn hình.
        let screenMenuItem = new NativeMenuItem(menuText.CaptureScreen)
        screenMenuItem.Menu <- buildScreenSubmenu()
        menu.Items.Add(screenMenuItem)

        // FR-SYS-004: Trình phóng nhanh.
        let launcherItem = new NativeMenuItem(menuText.Launcher)
        launcherItem.Click.Add(fun _ -> dispatch (LaunchWithDelay 3000))
        menu.Items.Add(launcherItem)

        menu.Items.Add(new NativeMenuItemSeparator())

        // FR-SYS-005: Thông tin & Phím tắt.
        let aboutItem = new NativeMenuItem(menuText.About)
        aboutItem.Click.Add(fun _ -> dispatch OpenAbout)
        menu.Items.Add(aboutItem)

        // FR-SYS-006: Cài đặt.
        let settingsItem = new NativeMenuItem(menuText.Settings)
        settingsItem.Click.Add(fun _ -> dispatch OpenSettings)
        menu.Items.Add(settingsItem)

        // FR-SYS-007: Mở thư mục ảnh chụp.
        let openFolderItem = new NativeMenuItem(menuText.OpenSaveFolder)
        openFolderItem.Click.Add(fun _ -> dispatch OpenSaveFolder)
        menu.Items.Add(openFolderItem)

        menu.Items.Add(new NativeMenuItemSeparator())

        // FR-SYS-008: Thoát F-Shot.
        let exitItem = new NativeMenuItem(menuText.Exit)
        exitItem.Click.Add(fun _ -> dispatch Exit)
        menu.Items.Add(exitItem)

        menu

    /// Xử lý click chuột trái vào tray icon.
    /// Click / double-click đều kích hoạt chụp GUI ngay lập tức.
    let onTrayClick (_: EventArgs) =
        let now = DateTime.UtcNow
        let _isDoubleClick = (now - lastClickTime).TotalMilliseconds < 500.0
        lastClickTime <- now
        dispatch GuiCapture

    /// Đảm bảo `TrayIcon.GetIcons` đã được khởi tạo.
    let ensureTrayIcons () : TrayIcons =
        let icons = TrayIcon.GetIcons(application)
        if isNull icons then
            let ti = new TrayIcons()
            TrayIcon.SetIcons(application, ti)
            ti
        else
            icons

    /// Tạo và đăng ký tray icon vào ứng dụng. Trả về `TrayIcon` vừa tạo.
    member this.CreateAndRegister() : TrayIcon =
        let icon = new TrayIcon()
        icon.Icon <- createWindowIcon()
        icon.ToolTipText <- "F-Shot"
        icon.Menu <- buildMenu()
        icon.Clicked.Add(onTrayClick)

        let icons = ensureTrayIcons()
        icons.Add(icon)
        trayIcon <- Some icon
        icon

    /// Làm mới menu (ví dụ sau khi cấu hình hoặc danh sách màn hình thay đổi).
    member this.RefreshMenu() =
        trayIcon |> Option.iter (fun ti -> ti.Menu <- buildMenu())

    /// Gỡ tray icon khỏi khay hệ thống và giải phóng tham chiếu.
    /// Gọi trong luồng shutdown để tránh ghost icon.
    member this.Dispose() =
        trayIcon |> Option.iter (fun ti ->
            try
                let icons = ensureTrayIcons()
                icons.Remove(ti) |> ignore
            with _ ->
                ()
            trayIcon <- None
        )

    interface IDisposable with
        member this.Dispose() = this.Dispose()

    /// Factory method tạo và đăng ký service.
    static member Create(application: Application, dispatch: TrayCommand -> unit, ?menuText: TrayMenuText) : TrayIconService =
        let text = defaultArg menuText TrayMenuText.defaultText
        let service = new TrayIconService(application, dispatch, text)
        service.CreateAndRegister() |> ignore
        service
