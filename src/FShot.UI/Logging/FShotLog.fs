namespace FShot.UI.Logging

open System
open System.IO

/// Logger đơn giản ghi ra file trong %TEMP%\FShot\logs.
/// Dùng để debug khi chạy binary mà không có console (double-click .exe).
module FShotLog =

    let private logDir =
        let temp = Environment.GetEnvironmentVariable("TEMP")
        let baseDir =
            if String.IsNullOrWhiteSpace temp then
                Path.Combine(AppContext.BaseDirectory, "logs")
            else
                Path.Combine(temp, "FShot", "logs")

        Directory.CreateDirectory(baseDir) |> ignore
        baseDir

    let private logFile =
        Path.Combine(logDir, "FShot_Current.log")

    /// Xóa log cũ khi khởi động để mỗi lần chạy có log mới.
    do
        try
            if File.Exists(logFile) then
                File.Delete(logFile)
        with _ ->
            ()

    /// Ghi dòng log có timestamp.
    let write (message: string) =
        let line = sprintf "[%s] %s" (DateTime.Now.ToString("HH:mm:ss.fff")) message
        try
            File.AppendAllText(logFile, line + Environment.NewLine)
        with _ ->
            ()

    /// Ghi exception chi tiết.
    let writeEx (message: string) (ex: exn) =
        write (sprintf "%s" message)
        write (sprintf "EXCEPTION: %s" (ex.ToString()))

    /// Đường dẫn file log hiện tại.
    let currentLogFile = logFile
