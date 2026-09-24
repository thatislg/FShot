namespace FShot.Platform.Win32.Startup

open Microsoft.Win32
open System
open System.Diagnostics
open System.IO

/// Đăng ký / hủy đăng ký F-Shot khởi động cùng Windows.
/// Xem tài liệu 10_09_Startup.md.
module StartupRegistration =

    let private runKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run"
    let private valueName = "FShot"

    /// Lấy đường dẫn thực thi hiện tại của F-Shot.
    /// Ưu tiên `Process.GetCurrentProcess().MainModule.FileName`, fallback về entry assembly.
    let getExecutablePath () : string =
        try
            let processPath = Process.GetCurrentProcess().MainModule.FileName
            if File.Exists processPath then processPath
            else
                let entryAssembly = System.Reflection.Assembly.GetEntryAssembly()
                if isNull entryAssembly then ""
                else entryAssembly.Location
        with _ ->
            ""

    /// Xây dựng command line để ghi vào Registry, kèm cờ chạy nền.
    let buildStartupCommand (exePath: string) : string =
        if String.IsNullOrWhiteSpace exePath then ""
        else sprintf "\"%s\"" exePath

    /// Kiểm tra xem F-Shot đã được đăng ký khởi động cùng Windows chưa.
    let isStartupEnabled () : bool =
        try
            use key = Registry.CurrentUser.OpenSubKey(runKeyPath)
            if isNull key then false
            else
                let value = key.GetValue(valueName)
                not (isNull value)
        with _ ->
            false

    /// Đăng ký hoặc hủy đăng ký khởi động cùng Windows.
    /// `enable = true` để đăng ký; `false` để xóa.
    let setStartup (enable: bool) : Result<unit, string> =
        try
            use key = Registry.CurrentUser.OpenSubKey(runKeyPath, true)
            if isNull key then
                Error (sprintf "Registry key not found: HKCU\\%s" runKeyPath)
            elif enable then
                let exePath = getExecutablePath()
                if String.IsNullOrWhiteSpace exePath then
                    Error "Could not determine F-Shot executable path"
                else
                    let command = buildStartupCommand exePath
                    key.SetValue(valueName, command)
                    Ok ()
            else
                key.DeleteValue(valueName, false)
                Ok ()
        with ex ->
            Error (sprintf "Failed to update startup registration: %s" ex.Message)

    /// Đồng bộ trạng thái khởi động cùng Windows với cấu hình.
    /// Nếu `desired` khác với trạng thái hiện tại Registry thì thực hiện thay đổi.
    let syncStartup (desired: bool) : Result<unit, string> =
        let current = isStartupEnabled()
        if current = desired then Ok ()
        else setStartup desired
