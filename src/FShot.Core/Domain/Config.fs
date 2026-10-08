namespace FShot.Core.Domain

open System
open System.Text.Json
open System.Text.Json.Serialization
open FShot.Core.Geometry
open FShot.Core.Geometry.Operations

/// Bản sao cấu hình tại thời điểm overlay mở.
/// OverlayState dùng bản sao này để tránh bị ảnh hưởng bởi thay đổi cấu hình trong lúc chụp.
/// Xem tài liệu 08_06_Integration.md, mục 8.
type ConfigSnapshot = {
    /// Công cụ mặc định khi overlay mở.
    DefaultTool: ToolKind

    /// Màu sắc mặc định cho nét vẽ.
    DefaultColor: Color

    /// Độ dày nét vẽ mặc định.
    DefaultStrokeWidth: StrokeWidth

    /// Cỡ chữ mặc định cho công cụ Text.
    DefaultFontSize: float

    /// Giới hạn số snapshot trong HistoryStack.
    HistoryLimit: int

    /// Có đóng overlay ngay sau khi xuất thành công không.
    CloseAfterExport: bool

    /// Tùy chọn lưu file mặc định (đường dẫn, mẫu tên file, format, chất lượng JPEG).
    SaveOptions: SaveOptions

    /// Có hiển thị thông báo desktop khi copy/save thành công không.
    ShowDesktopNotification: bool

    /// Có hiển thị thông báo khi hủy thao tác chụp không.
    ShowAbortNotification: bool

    /// Màu accent chính của giao diện, dùng cho viền/handle vùng chọn (FR-CFG-100).
    UiColor: Color

    /// Màu tương phản phụ (FR-CFG-101).
    ContrastUiColor: Color

    /// Độ mờ lớp phủ ngoài vùng chọn, 0–255 (FR-CFG-102).
    ContrastOpacity: byte
} with
    /// Cấu hình mặc định cho MVP.
    static member Default = {
        DefaultTool = SelectionTool
        DefaultColor = Color.Red
        DefaultStrokeWidth = StrokeWidth.Create 2.0
        DefaultFontSize = 14.0
        HistoryLimit = HistoryStack.DefaultLimit
        CloseAfterExport = true
        SaveOptions = SaveOptions.Default
        ShowDesktopNotification = true
        ShowAbortNotification = false
        UiColor = { R = 56uy; G = 189uy; B = 248uy; A = 255uy }   // #38BDF8
        ContrastUiColor = { R = 15uy; G = 23uy; B = 42uy; A = 255uy } // #0F172A
        ContrastOpacity = 190uy
    }

/// Hành động toàn cục có thể gán phím nóng (FR-SH-028–FR-SH-030).
[<RequireQualifiedAccess>]
type HotkeyAction =
    | CaptureGui
    | CaptureFullScreen
    | CaptureScreenAtCursor

/// Cấu hình một phím nóng toàn cục.
/// `Key` là chuỗi thân thiện (vd "PrintScreen", "Win+Shift+X", "Ctrl+Alt+S"); rỗng = tắt hành động.
type HotkeyConfig = {
    Action: HotkeyAction
    Key: string
    Enabled: bool
} with
    /// Cấu hình mặc định: chỉ bật chụp GUI bằng phím PrintScreen.
    static member Defaults : HotkeyConfig list = [
        { Action = HotkeyAction.CaptureGui; Key = "PrintScreen"; Enabled = true }
        { Action = HotkeyAction.CaptureFullScreen; Key = ""; Enabled = false }
        { Action = HotkeyAction.CaptureScreenAtCursor; Key = ""; Enabled = false }
    ]

/// Tiện ích làm việc với danh sách HotkeyConfig.
[<RequireQualifiedAccess>]
module HotkeyConfigs =
    let private allActions = [ HotkeyAction.CaptureGui; HotkeyAction.CaptureFullScreen; HotkeyAction.CaptureScreenAtCursor ]

    /// Lấy Key theo hành động; None nếu không có trong danh sách.
    let tryKey (action: HotkeyAction) (configs: HotkeyConfig list) : string option =
        configs |> List.tryPick (fun h -> if h.Action = action then Some h.Key else None)

    /// Chuẩn hóa: đảm bảo đủ 3 hành động, trim Key, Enabled = (Key <> "").
    let normalize (configs: HotkeyConfig list) : HotkeyConfig list =
        allActions
        |> List.map (fun action ->
            let key =
                match tryKey action configs with
                | Some k when not (String.IsNullOrWhiteSpace k) -> k.Trim()
                | _ -> ""
            { Action = action; Key = key; Enabled = key <> "" })

/// Các giá trị mặc định được dùng chung cho quá trình chuẩn hóa cấu hình.
module private ConfigDefaults =
    let defaultSaveAsFileExtension = "png"
    let defaultUiColor = "#38BDF8"
    let defaultContrastUiColor = "#0F172A"
    let defaultCaptureBackend = "Auto"
    let defaultUiLanguage = "auto"

    /// Chuẩn hóa chuỗi hex; trả về fallback nếu không hợp lệ.
    let normalizeHex (input: string) (fallback: string) : string =
        if String.IsNullOrWhiteSpace input then fallback
        else
            match colorFromHex input with
            | Some _ -> input.Trim().ToUpperInvariant()
            | None -> fallback

    /// Chuẩn hóa phần mở rộng định dạng ảnh ("png" | "jpg").
    let normalizeExtension (input: string) : string =
        if String.IsNullOrWhiteSpace input then defaultSaveAsFileExtension
        else
            match input.Trim().TrimStart('.').ToLowerInvariant() with
            | "jpg" | "jpeg" -> "jpg"
            | _ -> "png"

    /// Chuẩn hóa tên backend chụp ("Auto" | "Wgc" | "Gdi").
    let normalizeCaptureBackend (input: string) : string =
        if String.IsNullOrWhiteSpace input then defaultCaptureBackend
        else
            match input.Trim().ToLowerInvariant() with
            | "wgc" -> "Wgc"
            | "gdi" -> "Gdi"
            | _ -> "Auto"

    /// Clamp giá trị float trong [min, max]; trả về default nếu NaN/Infinity.
    let clampFloat (min: float) (max: float) (defaultVal: float) (value: float) : float =
        if Double.IsNaN value || Double.IsInfinity value then defaultVal
        else Math.Max(min, Math.Min(value, max))

    /// Clamp giá trị int trong [min, max].
    let clampInt (min: int) (max: int) (value: int) : int =
        Math.Max(min, Math.Min(value, max))

/// Cấu hình lưu trữ của FShot (đọc/ghi JSON tại %APPDATA%\FShot\config.json).
/// Gồm các trường MVP và toàn bộ trường v1.0 (Epic 9: FR-CFG-001–FR-CFG-209).
[<JsonConverter(typeof<AppConfigJsonConverter>)>]
type AppConfig = {
    SavePath: string
    FilenamePattern: string
    DrawColor: string
    DrawThickness: float
    DefaultTool: string
    CloseAfterExport: bool
    StartupLaunch: bool
    ShowDesktopNotification: bool
    ShowAbortNotification: bool
    DisabledTrayIcon: bool
    Hotkeys: HotkeyConfig list

    /// Lưu tức thì vào thư mục cố định, không hỏi lại (FR-CFG-002).
    SavePathFixed: bool

    /// Định dạng lưu mặc định "png" | "jpg" (FR-CFG-004).
    SaveAsFileExtension: string

    /// Chất lượng JPEG 1–100 (FR-CFG-005).
    JpegQuality: int

    /// Copy vào clipboard khi double-click vùng chọn (FR-CFG-014).
    CopyOnDoubleClick: bool

    /// Dùng lại vùng chọn từ lần chụp trước (FR-CFG-015).
    SaveLastRegion: bool

    /// Cho phép nhiều instance GUI đồng thời (FR-CFG-016).
    AllowMultipleGuiInstances: bool

    /// Backend chụp "Auto" | "Wgc" | "Gdi" (P2.09).
    CaptureBackend: string

    /// Màu accent chính của giao diện (FR-CFG-100).
    UiColor: string

    /// Màu tương phản phụ (FR-CFG-101).
    ContrastUiColor: string

    /// Độ mờ lớp phủ ngoài vùng chọn, 0–255 (FR-CFG-102).
    ContrastOpacity: byte

    /// Bảng màu mở rộng (FR-CFG-103).
    PredefinedColorPaletteLarge: bool

    /// Danh sách màu tùy chỉnh của người dùng (FR-CFG-104).
    UserColors: string list

    /// Thứ tự và trạng thái hiển thị nút công cụ (FR-CFG-105).
    Buttons: string list

    /// Ngôn ngữ giao diện "auto" | "vi" | "en" (FR-CFG-106).
    UiLanguage: string

    /// Font chữ giao diện (FR-CFG-107).
    FontFamily: string

    /// Cỡ chữ mặc định cho công cụ Text (FR-CFG-202).
    DrawFontSize: float

    /// Kích thước huy hiệu đếm số Circle Counter (FR-CFG-203).
    DrawCircleCounterSize: float

    /// Kích thước ô vuông mosaic Pixelate (FR-CFG-204).
    DrawPixelateSize: int

    /// Bán kính bo góc Rectangle (FR-CFG-205).
    DrawRectangleRadius: float

    /// Kích thước Marker (FR-CFG-206).
    DrawMarkerSize: float
} with
    /// Cấu hình mặc định của ứng dụng.
    static member Default = {
        SavePath = ""
        FilenamePattern = "fshot_%Y-%m-%d-%H%M%S"
        DrawColor = "#FF0000"
        DrawThickness = 2.0
        DefaultTool = "SelectionTool"
        CloseAfterExport = true
        StartupLaunch = false
        ShowDesktopNotification = true
        ShowAbortNotification = false
        DisabledTrayIcon = false
        Hotkeys = HotkeyConfig.Defaults
        SavePathFixed = false
        SaveAsFileExtension = ConfigDefaults.defaultSaveAsFileExtension
        JpegQuality = 90
        CopyOnDoubleClick = false
        SaveLastRegion = false
        AllowMultipleGuiInstances = false
        CaptureBackend = ConfigDefaults.defaultCaptureBackend
        UiColor = ConfigDefaults.defaultUiColor
        ContrastUiColor = ConfigDefaults.defaultContrastUiColor
        ContrastOpacity = 190uy
        PredefinedColorPaletteLarge = false
        UserColors = []
        Buttons = []
        UiLanguage = ConfigDefaults.defaultUiLanguage
        FontFamily = ""
        DrawFontSize = 18.0
        DrawCircleCounterSize = 28.0
        DrawPixelateSize = 10
        DrawRectangleRadius = 0.0
        DrawMarkerSize = 10.0
    }

    /// Làm sạch và chuẩn hóa dữ liệu, đảm bảo không có giá trị null hoặc không hợp lệ.
    member this.Normalized() : AppConfig =
        let savePath =
            if String.IsNullOrWhiteSpace this.SavePath then ""
            else this.SavePath.Trim()

        let pattern =
            if String.IsNullOrWhiteSpace this.FilenamePattern then "fshot_%Y-%m-%d-%H%M%S"
            else this.FilenamePattern.Trim()

        let color =
            if String.IsNullOrWhiteSpace this.DrawColor then "#FF0000"
            else
                match colorFromHex this.DrawColor with
                | Some _ -> this.DrawColor.Trim()
                | None -> "#FF0000"

        let thickness =
            if Double.IsNaN this.DrawThickness || Double.IsInfinity this.DrawThickness || this.DrawThickness < 1.0 then
                2.0
            else
                Math.Min(50.0, this.DrawThickness)

        let tool =
            if String.IsNullOrWhiteSpace this.DefaultTool then "SelectionTool"
            else
                match this.DefaultTool.Trim().ToLowerInvariant() with
                | "selection" | "selectiontool" -> "SelectionTool"
                | "pencil" | "penciltool" -> "PencilTool"
                | "line" | "linetool" -> "LineTool"
                | "arrow" | "arrowtool" -> "ArrowTool"
                | "rectangle" | "rectangletool" -> "RectangleTool"
                | "circle" | "circletool" -> "CircleTool"
                | "marker" | "markertool" -> "MarkerTool"
                | "text" | "texttool" -> "TextTool"
                | "pixelate" | "pixelatetool" -> "PixelateTool"
                | "icon" | "icontool" -> "IconTool"
                | _ -> "SelectionTool"

        let userColors =
            this.UserColors
            |> List.choose (fun c ->
                if String.IsNullOrWhiteSpace c then None
                else
                    match colorFromHex c with
                    | Some _ -> Some (c.Trim().ToUpperInvariant())
                    | None -> None)

        let buttons =
            this.Buttons
            |> List.choose (fun b ->
                if String.IsNullOrWhiteSpace b then None
                else Some (b.Trim()))

        {
            SavePath = savePath
            FilenamePattern = pattern
            DrawColor = color
            DrawThickness = thickness
            DefaultTool = tool
            CloseAfterExport = this.CloseAfterExport
            StartupLaunch = this.StartupLaunch
            ShowDesktopNotification = this.ShowDesktopNotification
            ShowAbortNotification = this.ShowAbortNotification
            DisabledTrayIcon = this.DisabledTrayIcon
            Hotkeys =
                if List.isEmpty this.Hotkeys then HotkeyConfig.Defaults
                else HotkeyConfigs.normalize this.Hotkeys
            SavePathFixed = this.SavePathFixed
            SaveAsFileExtension = ConfigDefaults.normalizeExtension this.SaveAsFileExtension
            JpegQuality = ConfigDefaults.clampInt 1 100 this.JpegQuality
            CopyOnDoubleClick = this.CopyOnDoubleClick
            SaveLastRegion = this.SaveLastRegion
            AllowMultipleGuiInstances = this.AllowMultipleGuiInstances
            CaptureBackend = ConfigDefaults.normalizeCaptureBackend this.CaptureBackend
            UiColor = ConfigDefaults.normalizeHex this.UiColor ConfigDefaults.defaultUiColor
            ContrastUiColor = ConfigDefaults.normalizeHex this.ContrastUiColor ConfigDefaults.defaultContrastUiColor
            ContrastOpacity = byte (ConfigDefaults.clampInt 0 255 (int this.ContrastOpacity))
            PredefinedColorPaletteLarge = this.PredefinedColorPaletteLarge
            UserColors = userColors
            Buttons = buttons
            UiLanguage =
                if String.IsNullOrWhiteSpace this.UiLanguage then ConfigDefaults.defaultUiLanguage
                else this.UiLanguage.Trim().ToLowerInvariant()
            FontFamily = if String.IsNullOrWhiteSpace this.FontFamily then "" else this.FontFamily.Trim()
            DrawFontSize = ConfigDefaults.clampFloat 8.0 72.0 18.0 this.DrawFontSize
            DrawCircleCounterSize = ConfigDefaults.clampFloat 16.0 64.0 28.0 this.DrawCircleCounterSize
            DrawPixelateSize = ConfigDefaults.clampInt 4 32 this.DrawPixelateSize
            DrawRectangleRadius = ConfigDefaults.clampFloat 0.0 40.0 0.0 this.DrawRectangleRadius
            DrawMarkerSize = ConfigDefaults.clampFloat 1.0 100.0 10.0 this.DrawMarkerSize
        }

    /// Chuyển đổi AppConfig sang ConfigSnapshot dùng cho OverlayState.
    member this.ToSnapshot() : ConfigSnapshot =
        let norm = this.Normalized()

        let toolKind =
            match norm.DefaultTool.ToLowerInvariant() with
            | "penciltool" | "pencil" -> PencilTool
            | "linetool" | "line" -> LineTool
            | "arrowtool" | "arrow" -> ArrowTool
            | "rectangletool" | "rectangle" -> RectangleTool
            | "circletool" | "circle" -> CircleTool
            | "markertool" | "marker" -> MarkerTool
            | "texttool" | "text" -> TextTool
            | "pixelatetool" | "pixelate" -> PixelateTool
            | "icontool" | "icon" -> IconTool
            | _ -> SelectionTool

        let color =
            colorFromHex norm.DrawColor
            |> Option.defaultValue Color.Red

        let strokeWidth = StrokeWidth.Create norm.DrawThickness

        let savePathOpt =
            if String.IsNullOrWhiteSpace norm.SavePath then None
            else Some norm.SavePath

        let format =
            match norm.SaveAsFileExtension with
            | "jpg" -> Jpg
            | _ -> Png

        let saveOptions = {
            SaveOptions.Default with
                Path = savePathOpt
                FileNamePattern = norm.FilenamePattern
                Format = format
                JpegQuality = norm.JpegQuality
        }

        let uiColor =
            colorFromHex norm.UiColor
            |> Option.defaultValue { R = 56uy; G = 189uy; B = 248uy; A = 255uy }

        let contrastUiColor =
            colorFromHex norm.ContrastUiColor
            |> Option.defaultValue { R = 15uy; G = 23uy; B = 42uy; A = 255uy }

        {
            DefaultTool = toolKind
            DefaultColor = color
            DefaultStrokeWidth = strokeWidth
            DefaultFontSize = norm.DrawFontSize
            HistoryLimit = HistoryStack.DefaultLimit
            CloseAfterExport = norm.CloseAfterExport
            SaveOptions = saveOptions
            ShowDesktopNotification = norm.ShowDesktopNotification
            ShowAbortNotification = norm.ShowAbortNotification
            UiColor = uiColor
            ContrastUiColor = contrastUiColor
            ContrastOpacity = norm.ContrastOpacity
        }

    /// Tạo AppConfig từ ConfigSnapshot.
    static member FromSnapshot(snapshot: ConfigSnapshot) : AppConfig =
        let toHexRgb (c: Color) = sprintf "#%02X%02X%02X" c.R c.G c.B

        {
        SavePath = snapshot.SaveOptions.Path |> Option.defaultValue ""
        FilenamePattern = snapshot.SaveOptions.FileNamePattern
        DrawColor = snapshot.DefaultColor.ToHex()
        DrawThickness = snapshot.DefaultStrokeWidth.Value
        DefaultTool =
            match snapshot.DefaultTool with
            | SelectionTool -> "SelectionTool"
            | PencilTool -> "PencilTool"
            | LineTool -> "LineTool"
            | ArrowTool -> "ArrowTool"
            | RectangleTool -> "RectangleTool"
            | CircleTool -> "CircleTool"
            | MarkerTool -> "MarkerTool"
            | TextTool -> "TextTool"
            | PixelateTool -> "PixelateTool"
            | IconTool -> "IconTool"
        CloseAfterExport = snapshot.CloseAfterExport
        StartupLaunch = false
        ShowDesktopNotification = snapshot.ShowDesktopNotification
        ShowAbortNotification = snapshot.ShowAbortNotification
        DisabledTrayIcon = false
        Hotkeys = HotkeyConfig.Defaults
        SavePathFixed = false
        SaveAsFileExtension =
            match snapshot.SaveOptions.Format with
            | Jpg -> "jpg"
            | Png -> "png"
        JpegQuality = snapshot.SaveOptions.JpegQuality
        CopyOnDoubleClick = false
        SaveLastRegion = false
        AllowMultipleGuiInstances = false
        CaptureBackend = ConfigDefaults.defaultCaptureBackend
        UiColor = toHexRgb snapshot.UiColor
        ContrastUiColor = toHexRgb snapshot.ContrastUiColor
        ContrastOpacity = snapshot.ContrastOpacity
        PredefinedColorPaletteLarge = false
        UserColors = []
        Buttons = []
        UiLanguage = ConfigDefaults.defaultUiLanguage
        FontFamily = ""
        DrawFontSize = snapshot.DefaultFontSize
        DrawCircleCounterSize = 28.0
        DrawPixelateSize = 10
        DrawRectangleRadius = 0.0
        DrawMarkerSize = 10.0
    }

/// Custom JSON Converter cho AppConfig để đảm bảo tương thích mọi định dạng,
/// tự động bù đắp giá trị mặc định khi thiếu trường, và ghi ra định dạng indented camelCase chuẩn.
and AppConfigJsonConverter() =
    inherit JsonConverter<AppConfig>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) : AppConfig =
        use doc = JsonDocument.ParseValue(&reader)
        let root = doc.RootElement
        let def = AppConfig.Default

        let tryFindProp (name: string) =
            let mutable prop = JsonElement()
            if root.TryGetProperty(name, &prop) then Some prop
            else
                let lower = name.ToLowerInvariant()
                let mutable found = None
                for p in root.EnumerateObject() do
                    if p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                       p.Name.Equals(lower, StringComparison.OrdinalIgnoreCase) then
                        found <- Some p.Value
                found

        let getString (name: string) (defaultVal: string) =
            match tryFindProp name with
            | Some p when p.ValueKind = JsonValueKind.String -> p.GetString()
            | _ -> defaultVal

        let getFloat (name: string) (defaultVal: float) =
            match tryFindProp name with
            | Some p when p.ValueKind = JsonValueKind.Number ->
                match p.TryGetDouble() with
                | true, v -> v
                | _ -> defaultVal
            | _ -> defaultVal

        let getBool (name: string) (defaultVal: bool) =
            match tryFindProp name with
            | Some p when p.ValueKind = JsonValueKind.True -> true
            | Some p when p.ValueKind = JsonValueKind.False -> false
            | _ -> defaultVal

        let getInt (name: string) (defaultVal: int) =
            match tryFindProp name with
            | Some p when p.ValueKind = JsonValueKind.Number ->
                match p.TryGetInt32() with
                | true, v -> v
                | _ -> defaultVal
            | _ -> defaultVal

        let getByte (name: string) (defaultVal: byte) =
            match tryFindProp name with
            | Some p when p.ValueKind = JsonValueKind.Number ->
                match p.TryGetInt32() with
                | true, v -> byte (max 0 (min 255 v))
                | _ -> defaultVal
            | _ -> defaultVal

        let getStringList (name: string) : string list =
            match tryFindProp name with
            | Some p when p.ValueKind = JsonValueKind.Array ->
                p.EnumerateArray()
                |> Seq.choose (fun e ->
                    if e.ValueKind = JsonValueKind.String then
                        let s = e.GetString()
                        if String.IsNullOrWhiteSpace s then None else Some (s.Trim())
                    else
                        None)
                |> Seq.toList
            | _ -> []

        let getHotkeys () : HotkeyConfig list =
            let mutable hotkeysObj = JsonElement()
            if root.TryGetProperty("hotkeys", &hotkeysObj) && hotkeysObj.ValueKind = JsonValueKind.Object then
                let readKey (name: string) =
                    let mutable v = JsonElement()
                    if hotkeysObj.TryGetProperty(name, &v) && v.ValueKind = JsonValueKind.String then
                        let s = v.GetString()
                        if String.IsNullOrWhiteSpace s then "" else s.Trim()
                    else
                        ""
                HotkeyConfigs.normalize [
                    { Action = HotkeyAction.CaptureGui; Key = readKey "captureGui"; Enabled = false }
                    { Action = HotkeyAction.CaptureFullScreen; Key = readKey "captureFullScreen"; Enabled = false }
                    { Action = HotkeyAction.CaptureScreenAtCursor; Key = readKey "captureScreenAtCursor"; Enabled = false }
                ]
            else
                HotkeyConfig.Defaults

        {
            SavePath = getString "savePath" def.SavePath
            FilenamePattern = getString "filenamePattern" def.FilenamePattern
            DrawColor = getString "drawColor" def.DrawColor
            DrawThickness = getFloat "drawThickness" def.DrawThickness
            DefaultTool = getString "defaultTool" def.DefaultTool
            CloseAfterExport = getBool "closeAfterExport" def.CloseAfterExport
            StartupLaunch = getBool "startupLaunch" def.StartupLaunch
            ShowDesktopNotification = getBool "showDesktopNotification" def.ShowDesktopNotification
            ShowAbortNotification = getBool "showAbortNotification" def.ShowAbortNotification
            DisabledTrayIcon = getBool "disabledTrayIcon" def.DisabledTrayIcon
            Hotkeys = getHotkeys()
            SavePathFixed = getBool "savePathFixed" def.SavePathFixed
            SaveAsFileExtension = getString "saveAsFileExtension" def.SaveAsFileExtension
            JpegQuality = getInt "jpegQuality" def.JpegQuality
            CopyOnDoubleClick = getBool "copyOnDoubleClick" def.CopyOnDoubleClick
            SaveLastRegion = getBool "saveLastRegion" def.SaveLastRegion
            AllowMultipleGuiInstances = getBool "allowMultipleGuiInstances" def.AllowMultipleGuiInstances
            CaptureBackend = getString "captureBackend" def.CaptureBackend
            UiColor = getString "uiColor" def.UiColor
            ContrastUiColor = getString "contrastUiColor" def.ContrastUiColor
            ContrastOpacity = getByte "contrastOpacity" def.ContrastOpacity
            PredefinedColorPaletteLarge = getBool "predefinedColorPaletteLarge" def.PredefinedColorPaletteLarge
            UserColors = getStringList "userColors"
            Buttons = getStringList "buttons"
            UiLanguage = getString "uiLanguage" def.UiLanguage
            FontFamily = getString "fontFamily" def.FontFamily
            DrawFontSize = getFloat "drawFontSize" def.DrawFontSize
            DrawCircleCounterSize = getFloat "drawCircleCounterSize" def.DrawCircleCounterSize
            DrawPixelateSize = getInt "drawPixelateSize" def.DrawPixelateSize
            DrawRectangleRadius = getFloat "drawRectangleRadius" def.DrawRectangleRadius
            DrawMarkerSize = getFloat "drawMarkerSize" def.DrawMarkerSize
        }.Normalized()

    override _.Write(writer: Utf8JsonWriter, value: AppConfig, _options: JsonSerializerOptions) =
        let norm = value.Normalized()
        writer.WriteStartObject()
        writer.WriteString("savePath", norm.SavePath)
        writer.WriteString("filenamePattern", norm.FilenamePattern)
        writer.WriteString("drawColor", norm.DrawColor)
        writer.WriteNumber("drawThickness", norm.DrawThickness)
        writer.WriteString("defaultTool", norm.DefaultTool)
        writer.WriteBoolean("closeAfterExport", norm.CloseAfterExport)
        writer.WriteBoolean("startupLaunch", norm.StartupLaunch)
        writer.WriteBoolean("showDesktopNotification", norm.ShowDesktopNotification)
        writer.WriteBoolean("showAbortNotification", norm.ShowAbortNotification)
        writer.WriteBoolean("disabledTrayIcon", norm.DisabledTrayIcon)
        let keyFor (action: HotkeyAction) =
            HotkeyConfigs.tryKey action norm.Hotkeys |> Option.defaultValue ""
        writer.WritePropertyName("hotkeys")
        writer.WriteStartObject()
        writer.WriteString("captureGui", keyFor HotkeyAction.CaptureGui)
        writer.WriteString("captureFullScreen", keyFor HotkeyAction.CaptureFullScreen)
        writer.WriteString("captureScreenAtCursor", keyFor HotkeyAction.CaptureScreenAtCursor)
        writer.WriteEndObject()
        writer.WriteBoolean("savePathFixed", norm.SavePathFixed)
        writer.WriteString("saveAsFileExtension", norm.SaveAsFileExtension)
        writer.WriteNumber("jpegQuality", norm.JpegQuality)
        writer.WriteBoolean("copyOnDoubleClick", norm.CopyOnDoubleClick)
        writer.WriteBoolean("saveLastRegion", norm.SaveLastRegion)
        writer.WriteBoolean("allowMultipleGuiInstances", norm.AllowMultipleGuiInstances)
        writer.WriteString("captureBackend", norm.CaptureBackend)
        writer.WriteString("uiColor", norm.UiColor)
        writer.WriteString("contrastUiColor", norm.ContrastUiColor)
        writer.WriteNumber("contrastOpacity", int norm.ContrastOpacity)
        writer.WriteBoolean("predefinedColorPaletteLarge", norm.PredefinedColorPaletteLarge)
        writer.WritePropertyName("userColors")
        writer.WriteStartArray()
        norm.UserColors |> List.iter (fun c -> writer.WriteStringValue(c))
        writer.WriteEndArray()
        writer.WritePropertyName("buttons")
        writer.WriteStartArray()
        norm.Buttons |> List.iter (fun b -> writer.WriteStringValue(b))
        writer.WriteEndArray()
        writer.WriteString("uiLanguage", norm.UiLanguage)
        writer.WriteString("fontFamily", norm.FontFamily)
        writer.WriteNumber("drawFontSize", norm.DrawFontSize)
        writer.WriteNumber("drawCircleCounterSize", norm.DrawCircleCounterSize)
        writer.WriteNumber("drawPixelateSize", norm.DrawPixelateSize)
        writer.WriteNumber("drawRectangleRadius", norm.DrawRectangleRadius)
        writer.WriteNumber("drawMarkerSize", norm.DrawMarkerSize)
        writer.WriteEndObject()

/// Các tiện ích tuần tự hóa JSON cho cấu hình FShot.
[<RequireQualifiedAccess>]
module ConfigJson =
    let jsonOptions =
        let opt = JsonSerializerOptions()
        opt.WriteIndented <- true
        opt.PropertyNameCaseInsensitive <- true
        opt.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        opt.Converters.Add(AppConfigJsonConverter())
        opt

    /// Tuần tự hóa AppConfig sang chuỗi JSON có thụt đầu dòng (indented).
    let serialize (config: AppConfig) : string =
        JsonSerializer.Serialize(config.Normalized(), jsonOptions)

    /// Giải tuần tự hóa chuỗi JSON sang AppConfig.
    /// Nếu chuỗi rỗng hoặc JSON sai cú pháp, trả về None.
    let deserialize (json: string) : AppConfig option =
        if String.IsNullOrWhiteSpace json then None
        else
            try
                let cfg = JsonSerializer.Deserialize<AppConfig>(json, jsonOptions)
                if box cfg = null then None
                else Some (cfg.Normalized())
            with _ ->
                None

    /// Giải tuần tự hóa chuỗi JSON sang AppConfig, fallback về AppConfig.Default nếu lỗi.
    let deserializeOrDefault (json: string) : AppConfig =
        deserialize json |> Option.defaultValue AppConfig.Default
