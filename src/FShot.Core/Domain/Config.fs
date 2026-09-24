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
    }

/// Cấu hình lưu trữ của FShot (đọc/ghi JSON tại %APPDATA%\FShot\config.json).
/// Gồm các trường MVP: SavePath, FilenamePattern, DrawColor, DrawThickness, DefaultTool, CloseAfterExport (FR-CFG-001/003/200/201).
[<JsonConverter(typeof<AppConfigJsonConverter>)>]
type AppConfig = {
    SavePath: string
    FilenamePattern: string
    DrawColor: string
    DrawThickness: float
    DefaultTool: string
    CloseAfterExport: bool
} with
    /// Cấu hình mặc định của ứng dụng.
    static member Default = {
        SavePath = ""
        FilenamePattern = "fshot_%Y-%m-%d-%H%M%S"
        DrawColor = "#FF0000"
        DrawThickness = 2.0
        DefaultTool = "SelectionTool"
        CloseAfterExport = true
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

        {
            SavePath = savePath
            FilenamePattern = pattern
            DrawColor = color
            DrawThickness = thickness
            DefaultTool = tool
            CloseAfterExport = this.CloseAfterExport
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

        let saveOptions = {
            SaveOptions.Default with
                Path = savePathOpt
                FileNamePattern = norm.FilenamePattern
        }

        {
            DefaultTool = toolKind
            DefaultColor = color
            DefaultStrokeWidth = strokeWidth
            DefaultFontSize = 14.0
            HistoryLimit = HistoryStack.DefaultLimit
            CloseAfterExport = norm.CloseAfterExport
            SaveOptions = saveOptions
        }

    /// Tạo AppConfig từ ConfigSnapshot.
    static member FromSnapshot(snapshot: ConfigSnapshot) : AppConfig = {
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

        {
            SavePath = getString "savePath" def.SavePath
            FilenamePattern = getString "filenamePattern" def.FilenamePattern
            DrawColor = getString "drawColor" def.DrawColor
            DrawThickness = getFloat "drawThickness" def.DrawThickness
            DefaultTool = getString "defaultTool" def.DefaultTool
            CloseAfterExport = getBool "closeAfterExport" def.CloseAfterExport
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
