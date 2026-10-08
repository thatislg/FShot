namespace FShot.Platform.Win32.Hotkeys

open System

/// Kết quả parse một phím nóng: modifiers + mã phím ảo.
type ParsedHotkey = {
    Modifiers: HotkeyModifiers
    VirtualKey: uint32
}

/// Bộ phân giải chuỗi phím nóng thân thiện với người dùng.
/// Xem tài liệu 10_04_GlobalHotkey.md (P2.06).
module HotkeyParser =

    let private letterKeys =
        [ for i in 0 .. 25 -> (string (char (int 'A' + i)), uint32 (0x41 + i)) ]

    let private digitKeys =
        [ for i in 0 .. 9 -> (string (char (int '0' + i)), uint32 (0x30 + i)) ]

    let private functionKeys =
        [ for i in 1 .. 24 -> (sprintf "F%d" i, uint32 (0x70 + i - 1)) ]

    let private specialKeys : (string * uint32) list = [
        "PrintScreen", 0x2Cu; "PrtSc", 0x2Cu; "PrintScr", 0x2Cu
        "Pause", 0x13u; "Break", 0x13u
        "Insert", 0x2Du; "Ins", 0x2Du
        "Delete", 0x2Eu; "Del", 0x2Eu
        "Home", 0x24u; "End", 0x23u
        "PageUp", 0x21u; "PageDown", 0x22u
        "Left", 0x25u; "Right", 0x27u; "Up", 0x26u; "Down", 0x28u
        "Space", 0x20u
        "Enter", 0x0Du; "Return", 0x0Du
        "Escape", 0x1Bu; "Esc", 0x1Bu
        "Tab", 0x09u
        "Backspace", 0x08u
    ]

    let private allKeys = letterKeys @ digitKeys @ functionKeys @ specialKeys

    let private tryModifier (token: string) : HotkeyModifiers option =
        match token.ToLowerInvariant() with
        | "win" | "windows" | "super" -> Some HotkeyModifiers.Win
        | "ctrl" | "control" -> Some HotkeyModifiers.Control
        | "alt" -> Some HotkeyModifiers.Alt
        | "shift" -> Some HotkeyModifiers.Shift
        | _ -> None

    let private tryKey (token: string) : uint32 option =
        allKeys
        |> List.tryPick (fun (name, vk) ->
            if String.Equals(name, token, StringComparison.OrdinalIgnoreCase) then Some vk else None)

    /// Parse chuỗi phím nóng (vd "PrintScreen", "Win+Shift+X", "Ctrl+Alt+S", "F11") thành
    /// modifiers + virtual key. Không phân biệt hoa thường, chấp nhận khoảng trắng quanh dấu '+'.
    let parse (input: string) : Result<ParsedHotkey, string> =
        if String.IsNullOrWhiteSpace input then
            Error "Hotkey is empty"
        else
            let tokens =
                input.Split([| '+' |], StringSplitOptions.RemoveEmptyEntries)
                |> Array.map (fun t -> t.Trim())
                |> Array.filter (fun t -> t.Length > 0)
                |> Array.toList

            if List.isEmpty tokens then
                Error "Hotkey is empty"
            else
                let modifiers, keys =
                    List.fold (fun (mods, keys) token ->
                        match tryModifier token with
                        | Some m -> (mods ||| m, keys)
                        | None -> (mods, keys @ [ token ])) (HotkeyModifiers.None, []) tokens

                match keys with
                | [ key ] ->
                    match tryKey key with
                    | Some vk -> Ok { Modifiers = modifiers; VirtualKey = vk }
                    | None -> Error (sprintf "Unknown key: '%s'" key)
                | [] -> Error "Hotkey must include a key (e.g. PrintScreen, X, F11)"
                | _ -> Error (sprintf "Multiple keys specified: %s" (String.concat "+" keys))

    /// Lấy tên phím chuẩn hóa từ virtual key (vd 0x2C -> "PrintScreen").
    let tryNameOf (virtualKey: uint32) : string option =
        allKeys
        |> List.tryPick (fun (name, vk) -> if vk = virtualKey then Some name else None)

    /// Format modifiers + virtual key thành chuỗi thân thiện (vd "Win+Shift+X").
    /// Thứ tự chuẩn: Win, Ctrl, Alt, Shift.
    let format (modifiers: HotkeyModifiers) (virtualKey: uint32) : string =
        let parts = ResizeArray<string>()
        if (modifiers &&& HotkeyModifiers.Win) = HotkeyModifiers.Win then parts.Add "Win"
        if (modifiers &&& HotkeyModifiers.Control) = HotkeyModifiers.Control then parts.Add "Ctrl"
        if (modifiers &&& HotkeyModifiers.Alt) = HotkeyModifiers.Alt then parts.Add "Alt"
        if (modifiers &&& HotkeyModifiers.Shift) = HotkeyModifiers.Shift then parts.Add "Shift"
        match tryNameOf virtualKey with
        | Some name -> parts.Add name
        | None -> parts.Add (sprintf "0x%02X" (int virtualKey))
        String.Join("+", parts)

    /// Tổ hợp phím nguy hiểm của hệ điều hành, không được phép gán (dạng chuẩn hóa).
    let private dangerousHotkeys = [
        "Win+L"
        "Ctrl+Alt+Delete"
        "Alt+Tab"
        "Win+D"
        "Ctrl+Esc"
        "Win+Tab"
    ]

    /// Kiểm tra xem chuỗi phím nóng có phải tổ hợp nguy hiểm không (so sánh dạng chuẩn hóa).
    let isDangerous (input: string) : bool =
        match parse input with
        | Ok parsed ->
            let canonical = format parsed.Modifiers parsed.VirtualKey
            dangerousHotkeys
            |> List.exists (fun d -> String.Equals(d, canonical, StringComparison.OrdinalIgnoreCase))
        | Error _ ->
            false
