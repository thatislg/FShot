namespace FShot.Platform.Win32.Config

open System
open System.IO
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Core.Geometry.Operations

/// Bộ ánh xạ và chuẩn hóa cấu hình từ `flameshot.ini` sang `AppConfig` của F-Shot.
/// Xem tài liệu 07_05_Migration.md, mục 3, 4, 5.
[<RequireQualifiedAccess>]
module FlameshotConfigMigrator =

    let private clamp (min: float) (max: float) (v: float) = Math.Max(min, Math.Min(v, max))
    let private clampInt (min: int) (max: int) (v: int) = Math.Max(min, Math.Min(v, max))

    /// Biến thể trả về None khi không phân giải được (dùng để lọc danh sách màu).
    let tryNormalizeColor (input: string) : string option =
        let s = if isNull input then "" else input.Trim()
        if String.IsNullOrWhiteSpace s then
            None
        elif s.StartsWith("#", StringComparison.Ordinal) then
            let hex = s.Substring(1)
            match hex.Length with
            | 6 ->
                // #RRGGBB
                match colorFromHex s with
                | Some _ -> Some ("#" + hex.ToUpperInvariant())
                | None -> None
            | 8 ->
                // #AARRGGBB -> #RRGGBBAA (chuẩn CSS/Skia)
                let a = hex.Substring(0, 2)
                let rgb = hex.Substring(2, 6)
                let css = "#" + rgb + a
                match colorFromHex css with
                | Some _ -> Some (css.ToUpperInvariant())
                | None -> None
            | _ -> None
        elif s.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase)
             || s.StartsWith("rgba(", StringComparison.OrdinalIgnoreCase) then
            // rgb(R, G, B) / rgba(R, G, B, A)
            let openParen = s.IndexOf('(')
            let closeParen = s.IndexOf(')')
            if openParen < 0 || closeParen < 0 || closeParen < openParen then
                None
            else
                let inner = s.Substring(openParen + 1, closeParen - openParen - 1)
                let parts =
                    inner.Split(',')
                    |> Array.map (fun p -> p.Trim())
                if parts.Length >= 3 then
                    match Int32.TryParse(parts[0]), Int32.TryParse(parts[1]), Int32.TryParse(parts[2]) with
                    | (true, r), (true, g), (true, b) ->
                        Some (sprintf "#%02X%02X%02X"
                            (clampInt 0 255 r)
                            (clampInt 0 255 g)
                            (clampInt 0 255 b))
                    | _ -> None
                else
                    None
        else
            None

    /// Chuẩn hóa chuỗi màu từ Flameshot (hex / rgb() / rgba()) sang chuẩn `#RRGGBB` của F-Shot.
    /// Xem 07_05_Migration.md, mục 4.1.
    let normalizeColor (input: string) (fallback: string) : string =
        match tryNormalizeColor input with
        | Some c -> c
        | None -> fallback

    /// Ánh xạ danh sách ID công cụ Flameshot sang tên Tool của F-Shot.
    /// Xem 07_05_Migration.md, mục 4.2.
    let mapButtonIds (buttons: string list) : string list =
        let mapId (raw: string) =
            match Int32.TryParse(raw.Trim()) with
            | true, id ->
                match id with
                | 0 -> Some "SelectionTool"
                | 1 -> Some "PencilTool"
                | 2 -> Some "ArrowTool"
                | 3 -> Some "LineTool"
                | 4 -> Some "RectangleTool"
                | 5 -> Some "CircleTool"
                | 6 -> Some "MarkerTool"
                | 7 -> Some "TextTool"
                | 8 -> Some "PixelateTool"
                | 9 | 10 -> None // Invert / CircleCounter chưa được hỗ trợ -> bỏ qua
                | _ -> None
            | _ -> None

        buttons |> List.choose mapId

    /// Ánh xạ mã ngôn ngữ Flameshot sang mã của F-Shot.
    let private normalizeLanguage (s: string) : string =
        match s.Trim().ToLowerInvariant() with
        | "vi_vn" | "vi" -> "vi"
        | "en_us" | "en" -> "en"
        | _ -> "auto"

    /// Chuẩn hóa đường dẫn: đổi dấu gạch chéo sang chuẩn Windows, bỏ dấu gạch chéo cuối.
    let private normalizePath (s: string) : string =
        s.Trim().Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar)

    /// Chuyển đổi toàn bộ các entry INI thành AppConfig F-Shot.
    /// Không làm thay đổi file gốc; chỉ đọc và ánh xạ.
    let migrate (entries: FlameshotIniParser.IniEntry list) : AppConfig =
        let baseConfig = AppConfig.Default

        let findStr section key fallback =
            FlameshotIniParser.tryFind entries section key |> Option.defaultValue fallback

        let findBool section key fallback =
            FlameshotIniParser.tryFindBool entries section key |> Option.defaultValue fallback

        let findInt section key fallback =
            FlameshotIniParser.tryFindInt entries section key |> Option.defaultValue fallback

        let findFloat section key fallback =
            FlameshotIniParser.tryFindFloat entries section key |> Option.defaultValue fallback

        let findList section key =
            FlameshotIniParser.tryFindList entries section key

        let section = "General"

        // Chuẩn hóa phần mở rộng định dạng ảnh ("png" | "jpg").
        let saveAsFileExtension =
            let ext = findStr section "saveAsFileExtension" "png"
            match ext.Trim().TrimStart('.').ToLowerInvariant() with
            | "jpg" | "jpeg" -> "jpg"
            | _ -> "png"

        // Chuẩn hóa màu vẽ mặc định.
        let drawColor = normalizeColor (findStr section "drawColor" "#FF0000") "#FF0000"
        let uiColor = normalizeColor (findStr section "uiColor" "#38BDF8") "#38BDF8"
        let contrastUiColor = normalizeColor (findStr section "contrastUiColor" "#0F172A") "#0F172A"

        // Chuẩn hóa danh sách màu tùy chỉnh (chỉ giữ màu hợp lệ).
        let userColors =
            findList section "userColors"
            |> List.choose tryNormalizeColor

        {
            baseConfig with
                SavePath = normalizePath (findStr section "savePath" "")
                SavePathFixed = findBool section "savePathFixed" baseConfig.SavePathFixed
                FilenamePattern = findStr section "filenamePattern" baseConfig.FilenamePattern
                SaveAsFileExtension = saveAsFileExtension
                JpegQuality = clampInt 1 100 (findInt section "jpegQuality" 90)
                DrawColor = drawColor
                DrawThickness = clamp 1.0 50.0 (findFloat section "drawThickness" 2.0)
                DrawFontSize = clamp 8.0 72.0 (findFloat section "drawFontSize" 18.0)
                ContrastOpacity = byte (clampInt 0 255 (findInt section "contrastOpacity" 190))
                UiColor = uiColor
                ContrastUiColor = contrastUiColor
                UserColors = userColors
                Buttons = mapButtonIds (findList section "buttons")
                StartupLaunch = findBool section "startupLaunch" baseConfig.StartupLaunch
                DisabledTrayIcon = findBool section "disabledTrayIcon" baseConfig.DisabledTrayIcon
                ShowDesktopNotification = findBool section "showDesktopNotification" baseConfig.ShowDesktopNotification
                ShowAbortNotification = findBool section "showAbortNotification" baseConfig.ShowAbortNotification
                CopyOnDoubleClick = findBool section "copyOnDoubleClick" baseConfig.CopyOnDoubleClick
                UiLanguage = normalizeLanguage (findStr section "uiLanguage" "auto")
        }

    /// Chuyển đổi trực tiếp từ nội dung INI (string).
    let migrateFromString (content: string) : AppConfig =
        content |> FlameshotIniParser.parse |> migrate

    /// Đọc file `flameshot.ini` và chuyển đổi sang AppConfig.
    /// Trả về None nếu file không tồn tại hoặc không đọc được.
    let migrateFromFile (path: string) : AppConfig option =
        try
            if not (File.Exists path) then
                None
            else
                // Chỉ mở với quyền Read + FileShare.ReadWrite để không can thiệp file gốc.
                use stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                use reader = new StreamReader(stream, Text.Encoding.UTF8)
                let content = reader.ReadToEnd()
                Some (migrateFromString content)
        with _ ->
            None
