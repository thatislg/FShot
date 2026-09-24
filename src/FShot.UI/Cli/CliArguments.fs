namespace FShot.UI.Cli

open Argu
open Microsoft.FSharp.Quotations
open FShot.Core.Domain


/// Subcommand "gui": mở overlay tương tác để người dùng chọn vùng và chú thích.
type GuiArgs =
    | [<AltCommandLine("-d")>] Delay_Ms of int

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Delay_Ms _ -> "Độ trễ trước khi chụp (mili-giây)."

/// Subcommand "full": chụp toàn bộ màn hình hiện tại (headless/direct).
type FullArgs =
    | [<AltCommandLine("-p")>] Path of string
    | [<AltCommandLine("-c")>] Clipboard
    | [<AltCommandLine("-d")>] Delay_Ms of int

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Path _ -> "Đường dẫn file để lưu ảnh."
            | Clipboard -> "Sao chép ảnh vào clipboard thay vì lưu file."
            | Delay_Ms _ -> "Độ trễ trước khi chụp (mili-giây)."

/// Subcommand "screen": chụp một màn hình chỉ định theo chỉ số.
/// Chỉ số màn hình được parse thủ công từ raw args để hỗ trợ dạng positional `screen 0`.
type ScreenArgs =
    | [<AltCommandLine("-p")>] Path of string
    | [<AltCommandLine("-c")>] Clipboard
    | [<AltCommandLine("-d")>] Delay_Ms of int

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Path _ -> "Đường dẫn file để lưu ảnh."
            | Clipboard -> "Sao chép ảnh vào clipboard thay vì lưu file."
            | Delay_Ms _ -> "Độ trễ trước khi chụp (mili-giây)."

/// Cây tham số dòng lệnh gốc.
type CliArguments =
    | [<CliPrefix(CliPrefix.None)>] Gui of ParseResults<GuiArgs>
    | [<CliPrefix(CliPrefix.None)>] Full of ParseResults<FullArgs>
    | [<CliPrefix(CliPrefix.None)>] Screen of ParseResults<ScreenArgs>
    | Version

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Gui _ -> "Mở overlay tương tác."
            | Full _ -> "Chụp toàn bộ màn hình hiện tại và xuất ra file hoặc clipboard."
            | Screen _ -> "Chụp màn hình chỉ định theo chỉ số và xuất ra file hoặc clipboard."
            | Version -> "Hiển thị phiên bản."

/// Kết quả parse dòng lệnh.
type ParsedCliRequest =
    {
      Mode: CaptureMode
      DelayMs: int
      OutputTarget: OutputTarget
      RunAsDaemon: bool
    }

module CliParser =

    /// Parser toàn cục cho ứng dụng.
    let parser = ArgumentParser.Create<CliArguments>(programName = "fshot")

    /// Lấy giá trị delay an toàn (clamp >= 0).
    let private clampDelay (d: int) = max 0 d

    let private tryGetValue (results: ParseResults<FullArgs>) (expr: Expr<string -> FullArgs>) : string option =
        results.GetResults expr |> List.tryHead

    let private tryGetScreenValue (results: ParseResults<ScreenArgs>) (expr: Expr<string -> ScreenArgs>) : string option =
        results.GetResults expr |> List.tryHead

    let private getDelay (results: ParseResults<FullArgs>) : int =
        results.GetResults (<@ FullArgs.Delay_Ms @>) |> List.tryHead |> Option.defaultValue 0 |> clampDelay

    let private getGuiDelay (results: ParseResults<GuiArgs>) : int =
        results.GetResults (<@ GuiArgs.Delay_Ms @>) |> List.tryHead |> Option.defaultValue 0 |> clampDelay

    let private getScreenDelay (results: ParseResults<ScreenArgs>) : int =
        results.GetResults (<@ ScreenArgs.Delay_Ms @>) |> List.tryHead |> Option.defaultValue 0 |> clampDelay

    /// Lấy chỉ số màn hình từ args sau subcommand `screen`.
    /// Ví dụ: `fshot screen 1 -c` → 1.
    let private parseScreenIndex (args: string[]) : int option =
        let screenIndex = Array.tryFindIndex ((=) "screen") args
        match screenIndex with
        | Some i when i + 1 < args.Length ->
            match System.Int32.TryParse args.[i + 1] with
            | true, idx -> Some idx
            | _ -> None
        | _ -> None

    /// Parse mảng args thành ParsedCliRequest.
    /// Nếu args rỗng hoặc không khớp subcommand, mặc định là daemon (tray icon).
    let parse (args: string[]) : ParsedCliRequest =
        try
            let results = parser.Parse(args, ignoreUnrecognized = true, raiseOnUsage = false)

            if results.IsUsageRequested then
                printfn "%s" (parser.PrintUsage())
                System.Environment.Exit(0)

            if results.Contains Version then
                printfn "FShot 0.1.0"
                System.Environment.Exit(0)

            if results.Contains Gui then
                let gui = results.GetResult Gui
                {
                  Mode = GuiInteractive
                  DelayMs = getGuiDelay gui
                  OutputTarget = OpenGui
                  RunAsDaemon = false
                }
            elif results.Contains Full then
                let full = results.GetResult Full
                let outputTarget =
                    if full.Contains <@ FullArgs.Clipboard @> then
                        OutputTarget.Clipboard
                    else
                        match tryGetValue full <@ FullArgs.Path @> with
                        | Some p -> OutputTarget.File p
                        | None -> OutputTarget.OpenGui

                {
                  Mode = FullScreen
                  DelayMs = getDelay full
                  OutputTarget = outputTarget
                  RunAsDaemon = false
                }
            elif results.Contains Screen then
                let screen = results.GetResult Screen
                let screenIndex = parseScreenIndex args |> Option.defaultValue 0
                let outputTarget =
                    if screen.Contains <@ ScreenArgs.Clipboard @> then
                        OutputTarget.Clipboard
                    else
                        match tryGetScreenValue screen <@ ScreenArgs.Path @> with
                        | Some p -> OutputTarget.File p
                        | None -> OutputTarget.OpenGui

                {
                  Mode = SingleScreen screenIndex
                  DelayMs = getScreenDelay screen
                  OutputTarget = outputTarget
                  RunAsDaemon = false
                }
            else
                // Mặc định: chạy nền với tray icon.
                {
                  Mode = GuiInteractive
                  DelayMs = 0
                  OutputTarget = OpenGui
                  RunAsDaemon = true
                }
        with
        | :? ArguParseException as ex ->
            printfn "%s" ex.Message
            System.Environment.Exit(1)
            // never reached
            { Mode = GuiInteractive; DelayMs = 0; OutputTarget = OpenGui; RunAsDaemon = true }
        | ex ->
            printfn "Lỗi khi phân tích tham số dòng lệnh: %s" ex.Message
            System.Environment.Exit(1)
            { Mode = GuiInteractive; DelayMs = 0; OutputTarget = OpenGui; RunAsDaemon = true }
