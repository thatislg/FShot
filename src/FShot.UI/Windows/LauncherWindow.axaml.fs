namespace FShot.UI.Windows

open System
open Avalonia.Controls
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Platform.Win32.Screen
open FShot.UI.Logging

/// Cầu nối cho phép App đăng ký callback nhận yêu cầu chụp từ Trình phóng nhanh.
module LauncherRuntime =
    let mutable startCaptureCallback: (CaptureRequest * Rect option -> unit) option = None

/// Hộp thoại Trình phóng nhanh (FR-SYS-004): chọn chế độ chụp và độ trễ trước khi kích hoạt.
/// Xem tài liệu 10_05_TrayIcon.md, mục 4 và Epic 16 (P2.42).
type LauncherWindow() as this =
    inherit Window()

    do
        this.InitializeComponent()
        this.ConfigureWindow()
        this.PopulateControls()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

    member private this.ConfigureWindow() =
        this.WindowState <- WindowState.Normal
        this.ShowInTaskbar <- false
        this.Topmost <- true

    member private this.Combo(name: string) = this.FindControl<ComboBox>(name)

    /// Gán danh sách item (value, label) vào ComboBox và chọn item đầu tiên.
    member private this.SetComboItems(name: string) (items: (string * string) list) =
        let combo = this.Combo(name)
        if not (isNull combo) then
            combo.Items.Clear()
            items
            |> List.iter (fun (value, label) ->
                let item = ComboBoxItem()
                item.Content <- label
                item.Tag <- value
                combo.Items.Add(item) |> ignore)
            combo.SelectedIndex <- 0

    member private this.ComboValue(name: string) =
        let combo = this.Combo(name)
        if isNull combo then ""
        else
            match combo.SelectedItem with
            | :? ComboBoxItem as item -> if isNull item.Tag then "" else string item.Tag
            | _ -> ""

    member private this.PopulateControls() =
        this.SetComboItems "ModeCombo" [
            ("gui", "Vùng chọn (GUI)")
            ("full", "Toàn màn hình")
            ("screen", "Màn hình cụ thể")
        ]
        this.SetComboItems "DelayCombo" [
            ("0", "Chụp ngay (0 giây)")
            ("3000", "Trễ 3 giây")
            ("5000", "Trễ 5 giây")
            ("10000", "Trễ 10 giây")
        ]

        let monitorCombo = this.Combo("MonitorCombo")
        if not (isNull monitorCombo) then
            monitorCombo.Items.Clear()
            let screens = ScreenEnumeration.getScreens()
            if List.isEmpty screens then
                let item = ComboBoxItem()
                item.Content <- "(không phát hiện màn hình)"
                item.Tag <- "-1"
                monitorCombo.Items.Add(item) |> ignore
            else
                screens
                |> List.iter (fun s ->
                    let item = ComboBoxItem()
                    item.Content <- sprintf "Màn hình %d: %s" (s.Index + 1) s.Name
                    item.Tag <- string s.Index
                    monitorCombo.Items.Add(item) |> ignore)
            monitorCombo.SelectedIndex <- 0

    /// Chọn màn hình theo tag (hoặc None nếu không hợp lệ).
    member private this.SelectedMonitorIndex() : int option =
        match Int32.TryParse(this.ComboValue "MonitorCombo") with
        | true, idx when idx >= 0 -> Some idx
        | _ -> None

    member private this.OnStartClick(_: obj, _: RoutedEventArgs) =
        let mode = this.ComboValue "ModeCombo"
        let delayMs =
            match Int32.TryParse(this.ComboValue "DelayCombo") with
            | true, d -> max 0 d
            | _ -> 0

        let (captureMode, bounds) =
            match mode with
            | "full" -> (CaptureMode.FullScreen, None)
            | "screen" ->
                match this.SelectedMonitorIndex() with
                | Some idx ->
                    let b =
                        ScreenEnumeration.getScreens()
                        |> List.tryFind (fun s -> s.Index = idx)
                        |> Option.map (fun s -> s.VirtualBounds)
                    (CaptureMode.SingleScreen idx, b)
                | None -> (CaptureMode.FullScreen, None)
            | _ -> (CaptureMode.GuiInteractive, None)

        let request =
            { CaptureRequest.Default with
                Mode = captureMode
                DelayMs = delayMs }

        FShotLog.write (sprintf "[Launcher] Start capture: mode=%A delay=%d" captureMode delayMs)
        match LauncherRuntime.startCaptureCallback with
        | Some f -> f (request, bounds)
        | None -> FShotLog.write "[Launcher] No start-capture callback registered"
        this.Close()

    member private this.OnCancelClick(_: obj, _: RoutedEventArgs) =
        this.Close()
