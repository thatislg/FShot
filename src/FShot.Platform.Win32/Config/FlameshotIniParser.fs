namespace FShot.Platform.Win32.Config

open System

/// Bộ phân giải cú pháp file INI `flameshot.ini` thuần F#.
/// Xem tài liệu 07_05_Migration.md, mục 2.
[<RequireQualifiedAccess>]
module FlameshotIniParser =

    /// Một cặp khóa - giá trị thuộc một section trong file INI.
    type IniEntry = {
        Section: string
        Key: string
        Value: string
    }

    /// Phân tích nội dung INI thành danh sách IniEntry.
    /// Bỏ qua dòng trống, comment (`;`, `#`) và dòng không có dấu `=`.
    /// Section được khai báo dạng `[General]` và áp dụng cho các entry theo sau.
    let parse (content: string) : IniEntry list =
        if String.IsNullOrWhiteSpace content then []
        else
            let mutable currentSection = ""

            content.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.choose (fun rawLine ->
                let line = rawLine.Trim()

                if String.IsNullOrWhiteSpace line then
                    None
                elif line.StartsWith(";", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal) then
                    None
                elif line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal) then
                    currentSection <- line.Substring(1, line.Length - 2).Trim()
                    None
                else
                    let eq = line.IndexOf('=')
                    if eq < 0 then
                        None
                    else
                        let key = line.Substring(0, eq).Trim()
                        let value = line.Substring(eq + 1).Trim()
                        Some { Section = currentSection; Key = key; Value = value })
            |> Array.toList

    /// Tìm giá trị theo section và key (không phân biệt hoa thường).
    let tryFind (entries: IniEntry list) (section: string) (key: string) : string option =
        entries
        |> List.tryPick (fun e ->
            if e.Section.Equals(section, StringComparison.OrdinalIgnoreCase)
               && e.Key.Equals(key, StringComparison.OrdinalIgnoreCase) then
                Some e.Value
            else
                None)

    /// Tìm giá trị boolean ("true"/"false", "1"/"0", "yes"/"no").
    let tryFindBool (entries: IniEntry list) (section: string) (key: string) : bool option =
        tryFind entries section key
        |> Option.map (fun s ->
            match s.Trim().ToLowerInvariant() with
            | "true" | "1" | "yes" | "on" -> true
            | "false" | "0" | "no" | "off" -> false
            | _ -> false)

    /// Tìm giá trị nguyên.
    let tryFindInt (entries: IniEntry list) (section: string) (key: string) : int option =
        tryFind entries section key
        |> Option.bind (fun s ->
            match Int32.TryParse(s.Trim()) with
            | true, v -> Some v
            | _ -> None)

    /// Tìm giá trị số thực.
    let tryFindFloat (entries: IniEntry list) (section: string) (key: string) : float option =
        tryFind entries section key
        |> Option.bind (fun s ->
            match Double.TryParse(s.Trim(), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture) with
            | true, v -> Some v
            | _ -> None)

    /// Tìm giá trị danh sách phân tách bằng dấu phẩy, bỏ khoảng trắng.
    /// Tách dấu phẩy nhưng tôn trọng dấu ngoặc đơn để không cắt nhầm `rgb(R, G, B)`.
    let tryFindList (entries: IniEntry list) (section: string) (key: string) : string list =
        let splitList (s: string) : string list =
            let result = ResizeArray<string>()
            let current = Text.StringBuilder()
            let mutable depth = 0
            for c in s do
                if c = '(' then
                    depth <- depth + 1
                    current.Append(c) |> ignore
                elif c = ')' then
                    if depth > 0 then depth <- depth - 1
                    current.Append(c) |> ignore
                elif c = ',' && depth = 0 then
                    result.Add(current.ToString().Trim())
                    current.Clear() |> ignore
                else
                    current.Append(c) |> ignore
            result.Add(current.ToString().Trim())
            result
            |> Seq.filter (fun item -> not (String.IsNullOrWhiteSpace item))
            |> Seq.toList

        tryFind entries section key
        |> Option.map splitList
        |> Option.defaultValue []
