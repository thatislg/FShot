namespace FShot.UI.Windows

open System
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Layout
open Avalonia.Markup.Xaml
open Avalonia.Media
open Avalonia.Platform.Storage
open FShot.Core.Domain
open FShot.Platform.Win32.Config
open FShot.UI
open FShot.UI.Logging

/// Cửa sổ cài đặt (Config Editor) gồm 4 tab + footer actions.
/// Xem tài liệu 11_06_ConfigWindow.md (P2.13, FR-SYS-006, FR-CFG-100–FR-CFG-209).
type SettingsWindow() as this =
    inherit Window()

    let mutable originalConfig: AppConfig = AppConfig.Default

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        this.ConfigureWindow()
        this.LoadConfig()

    member private this.ConfigureWindow() =
        this.ShowInTaskbar <- true

    // ---------- Helper đọc/ghi điều khiển ----------
    member private this.Toggle(name: string) = this.FindControl<ToggleSwitch>(name)
    member private this.Text(name: string) = this.FindControl<TextBox>(name)
    member private this.Slider(name: string) = this.FindControl<Slider>(name)
    member private this.Label(name: string) = this.FindControl<TextBlock>(name)
    member private this.Combo(name: string) = this.FindControl<ComboBox>(name)

    member private this.ToggleValue(name: string) =
        let t = this.Toggle(name)
        if isNull t then false else t.IsChecked.GetValueOrDefault(false)

    member private this.SetToggle(name: string) (value: bool) =
        let t = this.Toggle(name)
        if not (isNull t) then t.IsChecked <- Nullable<bool>(value)

    member private this.SetSlider(name: string) (labelName: string) (value: float) (suffix: string) =
        let s = this.Slider(name)
        let l = this.Label(labelName)
        if not (isNull s) then
            s.Value <- value
            // Cập nhật nhãn theo giá trị mỗi khi người dùng kéo thanh trượt.
            s.ValueChanged.Add(fun e ->
                if not (isNull l) then l.Text <- sprintf "%.0f%s" e.NewValue suffix)
            |> ignore
        if not (isNull l) then l.Text <- sprintf "%.0f%s" value suffix

    member private this.SliderValue(name: string) =
        let s = this.Slider(name)
        if isNull s then 0.0 else s.Value

    member private this.SetComboItems(name: string) (items: (string * string) list) (selectedValue: string) =
        let combo = this.Combo(name)
        if not (isNull combo) then
            combo.Items.Clear()
            items
            |> List.iter (fun (value, label) ->
                let item = ComboBoxItem()
                item.Content <- label
                item.Tag <- value
                combo.Items.Add(item) |> ignore)
            let selected =
                items
                |> List.tryFindIndex (fun (value, _) -> value = selectedValue)
                |> Option.defaultValue 0
            combo.SelectedIndex <- selected

    member private this.ComboValue(name: string) =
        let combo = this.Combo(name)
        if isNull combo then ""
        else
            match combo.SelectedItem with
            | :? ComboBoxItem as item -> if isNull item.Tag then "" else string item.Tag
            | _ -> ""

    // ---------- Nạp cấu hình vào giao diện ----------
    member private this.LoadConfig() =
        originalConfig <- ConfigStore.loadConfig()
        let c = originalConfig

        this.Text("SavePathBox").Text <- c.SavePath
        this.SetToggle "SavePathFixedToggle" c.SavePathFixed
        (this.FindControl<FileNameEditor>("FileNameEditorControl")).Pattern <- c.FilenamePattern
        this.SetComboItems "ExtensionCombo" [ ("png", "PNG"); ("jpg", "JPG") ] c.SaveAsFileExtension
        this.SetSlider "JpegQualitySlider" "JpegQualityLabel" (float c.JpegQuality) "%"
        this.SetToggle "StartupToggle" c.StartupLaunch
        this.SetToggle "TrayIconToggle" (not c.DisabledTrayIcon)
        this.SetToggle "ShowDesktopNotificationToggle" c.ShowDesktopNotification
        this.SetToggle "ShowAbortToggle" c.ShowAbortNotification
        this.SetToggle "CopyOnDoubleClickToggle" c.CopyOnDoubleClick
        this.SetToggle "SaveAfterCopyToggle" c.SaveAfterCopy
        this.SetToggle "CopyPathAfterSaveToggle" c.CopyPathAfterSave
        this.SetSlider "UndoLimitSlider" "UndoLimitLabel" (float c.UndoLimit) " bước"

        (this.FindControl<ColorSelector>("UiColorSelector")).Color <- c.UiColor
        (this.FindControl<ColorSelector>("ContrastUiColorSelector")).Color <- c.ContrastUiColor
        this.SetSlider "ContrastOpacitySlider" "ContrastOpacityLabel" (float (int c.ContrastOpacity)) ""
        this.SetComboItems "LanguageCombo" [ ("auto", "Tự động"); ("vi", "Tiếng Việt"); ("en", "English") ] c.UiLanguage

        this.SetSlider "DrawThicknessSlider" "DrawThicknessLabel" c.DrawThickness " px"
        this.SetSlider "DrawFontSizeSlider" "DrawFontSizeLabel" c.DrawFontSize " pt"
        (this.FindControl<ColorSelector>("DrawColorSelector")).Color <- c.DrawColor
        this.SetSlider "CircleCounterSlider" "CircleCounterLabel" c.DrawCircleCounterSize " px"
        this.SetSlider "PixelateSlider" "PixelateLabel" (float c.DrawPixelateSize) " px"
        this.SetSlider "RectangleRadiusSlider" "RectangleRadiusLabel" c.DrawRectangleRadius " px"
        this.SetSlider "MarkerSizeSlider" "MarkerSizeLabel" c.DrawMarkerSize " px"

        let keyFor action = HotkeyConfigs.tryKey action c.Hotkeys |> Option.defaultValue ""
        this.Text("CaptureGuiKeyBox").Text <- keyFor HotkeyAction.CaptureGui
        this.Text("CaptureFullScreenKeyBox").Text <- keyFor HotkeyAction.CaptureFullScreen
        this.Text("CaptureCursorKeyBox").Text <- keyFor HotkeyAction.CaptureScreenAtCursor

        (this.FindControl<ColorSelector>("UserColorsSelector")).Palette <- c.UserColors

        FShotLog.write "[Settings] Config loaded into settings window"

    // ---------- Thu thập cấu hình từ giao diện ----------
    member private this.CollectConfig() : AppConfig =
        let c = originalConfig
        let editor = this.FindControl<FileNameEditor>("FileNameEditorControl")
        let filenamePattern =
            if obj.ReferenceEquals(editor, null) then c.FilenamePattern
            else editor.ValidPattern

        let hotkeys =
            HotkeyConfigs.normalize [
                { Action = HotkeyAction.CaptureGui; Key = this.Text("CaptureGuiKeyBox").Text; Enabled = true }
                { Action = HotkeyAction.CaptureFullScreen; Key = this.Text("CaptureFullScreenKeyBox").Text; Enabled = true }
                { Action = HotkeyAction.CaptureScreenAtCursor; Key = this.Text("CaptureCursorKeyBox").Text; Enabled = true }
            ]

        {
            c with
                SavePath = this.Text("SavePathBox").Text
                FilenamePattern = filenamePattern
                SavePathFixed = this.ToggleValue "SavePathFixedToggle"
                SaveAsFileExtension = this.ComboValue "ExtensionCombo"
                JpegQuality = int (this.SliderValue "JpegQualitySlider")
                StartupLaunch = this.ToggleValue "StartupToggle"
                DisabledTrayIcon = not (this.ToggleValue "TrayIconToggle")
                ShowDesktopNotification = this.ToggleValue "ShowDesktopNotificationToggle"
                ShowAbortNotification = this.ToggleValue "ShowAbortToggle"
                CopyOnDoubleClick = this.ToggleValue "CopyOnDoubleClickToggle"
                SaveAfterCopy = this.ToggleValue "SaveAfterCopyToggle"
                CopyPathAfterSave = this.ToggleValue "CopyPathAfterSaveToggle"
                UndoLimit = int (this.SliderValue "UndoLimitSlider")
                UiColor = (this.FindControl<ColorSelector>("UiColorSelector")).Color
                ContrastUiColor = (this.FindControl<ColorSelector>("ContrastUiColorSelector")).Color
                ContrastOpacity = byte (int (this.SliderValue "ContrastOpacitySlider"))
                UiLanguage = this.ComboValue "LanguageCombo"
                DrawThickness = this.SliderValue "DrawThicknessSlider"
                DrawFontSize = this.SliderValue "DrawFontSizeSlider"
                DrawColor = (this.FindControl<ColorSelector>("DrawColorSelector")).Color
                DrawCircleCounterSize = this.SliderValue "CircleCounterSlider"
                DrawPixelateSize = int (this.SliderValue "PixelateSlider")
                DrawRectangleRadius = this.SliderValue "RectangleRadiusSlider"
                DrawMarkerSize = this.SliderValue "MarkerSizeSlider"
                Hotkeys = hotkeys
                UserColors = (this.FindControl<ColorSelector>("UserColorsSelector")).Palette
        }

    // ---------- Thông báo trạng thái Apply ----------
    member private this.ShowStatus(message: string) (isError: bool) =
        let label = this.Label("StatusText")
        if not (isNull label) then
            label.Text <- message
            label.Foreground <- (if isError then Brushes.IndianRed else Brushes.SeaGreen)

    /// Lưu cấu hình và áp dụng ngay cho instance đang chạy, trả về kết quả để hiển thị thông báo.
    member private this.Save(?closeAfter: bool) =
        let closeAfter = defaultArg closeAfter false
        let config = this.CollectConfig()
        match ConfigStore.trySaveConfig config with
        | Error err ->
            this.ShowStatus (sprintf "Lỗi: %s" err) true
            FShotLog.write (sprintf "[Settings] Save failed: %s" err)
        | Ok () ->
            FShotLog.write "[Settings] Config saved"
            match this.TryApplyRuntime() with
            | Ok () ->
                this.ShowStatus "Đã áp dụng thành công" false
                if closeAfter then this.Close()
            | Error err ->
                this.ShowStatus (sprintf "Đã lưu nhưng áp dụng thất bại: %s" err) true
                FShotLog.write (sprintf "[Settings] Apply failed: %s" err)

    /// Gọi App.ApplyConfigAfterSave() qua cầu nối ConfigRuntime để áp dụng cho instance đang chạy.
    member private this.TryApplyRuntime() : Result<unit, string> =
        ConfigRuntime.apply()

    // ---------- Phím tắt: click để ghi phím ----------
    static member private KeyName(k: Key) : string option =
        let name = k.ToString()
        match name with
        | "D0" | "NumPad0" -> Some "0"
        | "D1" | "NumPad1" -> Some "1"
        | "D2" | "NumPad2" -> Some "2"
        | "D3" | "NumPad3" -> Some "3"
        | "D4" | "NumPad4" -> Some "4"
        | "D5" | "NumPad5" -> Some "5"
        | "D6" | "NumPad6" -> Some "6"
        | "D7" | "NumPad7" -> Some "7"
        | "D8" | "NumPad8" -> Some "8"
        | "D9" | "NumPad9" -> Some "9"
        | "Back" -> Some "Backspace"
        | "LControl" | "RControl" | "LShift" | "RShift" | "LAlt" | "RAlt" | "LWin" | "RWin" -> None
        | "None" -> None
        | _ -> Some name

    static member private RecordKey(e: KeyEventArgs) : string option =
        match SettingsWindow.KeyName e.Key with
        | None -> None
        | Some key ->
            let mods =
                [
                    if (e.KeyModifiers &&& KeyModifiers.Meta) <> KeyModifiers.None then yield "Win"
                    if (e.KeyModifiers &&& KeyModifiers.Control) <> KeyModifiers.None then yield "Ctrl"
                    if (e.KeyModifiers &&& KeyModifiers.Alt) <> KeyModifiers.None then yield "Alt"
                    if (e.KeyModifiers &&& KeyModifiers.Shift) <> KeyModifiers.None then yield "Shift"
                ]
            if List.isEmpty mods then Some key
            else Some (String.concat "+" (mods @ [ key ]))

    member private this.OnShortcutKeyDown(sender: obj, e: KeyEventArgs) =
        match sender with
        | :? TextBox as box ->
            match SettingsWindow.RecordKey e with
            | Some text ->
                e.Handled <- true
                box.Text <- text
            | None ->
                // Nuốt phím modifier đơn lẻ để tránh chèn ký tự vào ô.
                e.Handled <- true
        | _ -> ()

    // ---------- Event handlers ----------
    member private this.OnBrowseClick(_: obj, _: RoutedEventArgs) =
        // Không chặn luồng UI bằng .GetResult() — gọi bất đồng bộ để tránh deadlock/crash.
        let runBrowse =
            async {
                try
                    let topLevel = TopLevel.GetTopLevel(this)
                    if not (isNull topLevel) then
                        let options = FolderPickerOpenOptions(Title = "Chọn thư mục lưu ảnh", AllowMultiple = false)
                        let! result = topLevel.StorageProvider.OpenFolderPickerAsync(options) |> Async.AwaitTask
                        if result.Count > 0 then
                            let path = result[0].TryGetLocalPath()
                            if not (isNull path) then
                                this.Text("SavePathBox").Text <- path
                with ex ->
                    FShotLog.writeEx "[Settings] Browse folder failed" ex
            }
        Async.StartImmediate(runBrowse)

    member private this.OnApplyClick(_: obj, _: RoutedEventArgs) =
        this.Save(false)

    member private this.OnSaveCloseClick(_: obj, _: RoutedEventArgs) =
        this.Save(true)

    member private this.OnCancelClick(_: obj, _: RoutedEventArgs) =
        this.Close()

    member private this.OnResetClick(_: obj, _: RoutedEventArgs) =
        // Xác nhận trước khi khôi phục mặc định (FR-SYS-014).
        Async.StartImmediate(async {
            let! confirmed = this.ConfirmAsync "Khôi phục cấu hình mặc định?" "Thao tác này sẽ ghi đè toàn bộ cấu hình hiện tại về giá trị xuất xưởng. Bạn có chắc chắn?"
            if confirmed then
                match ConfigStore.resetConfig() with
                | Ok () ->
                    originalConfig <- AppConfig.Default
                    this.LoadConfig()
                    this.TryApplyRuntime() |> ignore
                    this.ShowStatus "Đã khôi phục cấu hình mặc định" false
                    FShotLog.write "[Settings] Config reset to defaults"
                | Error err ->
                    this.ShowStatus (sprintf "Lỗi: %s" err) true
                    FShotLog.write (sprintf "[Settings] Reset failed: %s" err)
        })

    member private this.OnImportClick(_: obj, _: RoutedEventArgs) =
        let runImport =
            async {
                try
                    let topLevel = TopLevel.GetTopLevel(this)
                    if not (isNull topLevel) then
                        let options = FilePickerOpenOptions(Title = "Chọn file cấu hình để nhập", AllowMultiple = false)
                        let jsonType = FilePickerFileType("JSON (*.json)")
                        jsonType.Patterns <- ResizeArray [ "*.json" ]
                        options.FileTypeFilter <- ResizeArray [ jsonType ]
                        let! result = topLevel.StorageProvider.OpenFilePickerAsync(options) |> Async.AwaitTask
                        if result.Count > 0 then
                            let path = result[0].TryGetLocalPath()
                            if not (isNull path) then
                                match ConfigStore.importConfigFrom path with
                                | Ok () ->
                                    this.LoadConfig()
                                    this.TryApplyRuntime() |> ignore
                                    this.ShowStatus "Đã nhập cấu hình thành công" false
                                    FShotLog.write (sprintf "[Settings] Imported config from %s" path)
                                | Error err ->
                                    this.ShowStatus (sprintf "Lỗi: %s" err) true
                                    FShotLog.write (sprintf "[Settings] Import failed: %s" err)
                with ex ->
                    FShotLog.writeEx "[Settings] Import config failed" ex
            }
        Async.StartImmediate(runImport)

    member private this.OnExportClick(_: obj, _: RoutedEventArgs) =
        let runExport =
            async {
                try
                    let topLevel = TopLevel.GetTopLevel(this)
                    if not (isNull topLevel) then
                        let options = FilePickerSaveOptions()
                        options.SuggestedFileName <- "fshot-config.json"
                        let jsonType = FilePickerFileType("JSON (*.json)")
                        jsonType.Patterns <- ResizeArray [ "*.json" ]
                        options.FileTypeChoices <- ResizeArray [ jsonType ]
                        let! file = topLevel.StorageProvider.SaveFilePickerAsync(options) |> Async.AwaitTask
                        if not (isNull file) then
                            let path =
                                match file.TryGetLocalPath() with
                                | null
                                | "" -> if file.Path.IsFile then file.Path.LocalPath else file.Path.ToString()
                                | p -> p
                            match ConfigStore.exportConfigTo path with
                            | Ok () ->
                                this.ShowStatus (sprintf "Đã xuất cấu hình ra %s" path) false
                                FShotLog.write (sprintf "[Settings] Exported config to %s" path)
                            | Error err ->
                                this.ShowStatus (sprintf "Lỗi: %s" err) true
                                FShotLog.write (sprintf "[Settings] Export failed: %s" err)
                with ex ->
                    FShotLog.writeEx "[Settings] Export config failed" ex
            }
        Async.StartImmediate(runExport)

    /// Hiển thị hộp thoại xác nhận Yes/No và trả về lựa chọn của người dùng.
    member private this.ConfirmAsync(title: string) (message: string) : Async<bool> =
        async {
            let win = Window()
            win.Title <- title
            win.Width <- 400.0
            win.Height <- 170.0
            win.WindowStartupLocation <- WindowStartupLocation.CenterOwner
            win.CanResize <- false

            let panel = StackPanel()
            panel.Margin <- Thickness(20.0)
            panel.Spacing <- 12.0

            let msg = TextBlock()
            msg.Text <- message
            msg.TextWrapping <- TextWrapping.Wrap

            let btns = StackPanel()
            btns.Orientation <- Orientation.Horizontal
            btns.HorizontalAlignment <- HorizontalAlignment.Center
            btns.Spacing <- 12.0

            let yes = Button()
            yes.Content <- "Có"
            yes.Click.Add(fun _ -> win.Close(true))

            let no = Button()
            no.Content <- "Không"
            no.Click.Add(fun _ -> win.Close(false))

            btns.Children.Add(yes)
            btns.Children.Add(no)
            panel.Children.Add(msg)
            panel.Children.Add(btns)
            win.Content <- panel

            let! result = win.ShowDialog<bool>(this) |> Async.AwaitTask
            return result
        }
