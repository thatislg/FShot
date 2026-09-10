namespace FShot.UI

open System
open Avalonia
open FShot.Core.Domain

/// Parse tham số dòng lệnh đơn giản.
/// Xem tài liệu 11_02_AppLifecycle.md.
module CliParser =

    /// Tìm giá trị của một flag, ví dụ ["-d"; "3000"] → Some "3000".
    let tryGetFlagValue (flag: string) (args: string[]) : string option =
        let index = Array.tryFindIndex ((=) flag) args
        match index with
        | Some i when i + 1 < args.Length -> Some args.[i + 1]
        | _ -> None

    /// Kiểm tra flag có tồn tại không.
    let hasFlag (flag: string) (args: string[]) : bool =
        Array.contains flag args

    /// Parse args thành CaptureRequest.
    let parse (args: string[]) : CaptureRequest =
        let delay =
            match tryGetFlagValue "-d" args with
            | Some value ->
                match Int32.TryParse value with
                | true, d -> d
                | _ -> 0
            | None -> 0

        let outputTarget =
            if hasFlag "-c" args then
                Clipboard
            elif hasFlag "-p" args then
                match tryGetFlagValue "-p" args with
                | Some path -> File path
                | None -> OpenGui
            else
                OpenGui

        let mode =
            if hasFlag "full" args then FullScreen
            elif hasFlag "screen" args then
                match tryGetFlagValue "-n" args with
                | Some value ->
                    match Int32.TryParse value with
                    | true, id -> SingleScreen id
                    | _ -> FullScreen
                | None -> FullScreen
            elif hasFlag "gui" args then GuiInteractive
            else GuiInteractive

        {
          CaptureRequest.Default with
              Mode = mode
              DelayMs = delay
              OutputTarget = outputTarget
        }

module Program =

    [<CompiledName "BuildAvaloniaApp">]
    let buildAvaloniaApp() =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace(areas = Array.empty)

    [<EntryPoint; STAThread>]
    let main argv =
        let request = CliParser.parse argv

        match request.Mode with
        | GuiInteractive ->
            buildAvaloniaApp().StartWithClassicDesktopLifetime(argv)
        | _ ->
            // TODO: chụp trực tiếp full/screen và xuất.
            // Hiện tại mở GUI để test.
            buildAvaloniaApp().StartWithClassicDesktopLifetime(argv)
