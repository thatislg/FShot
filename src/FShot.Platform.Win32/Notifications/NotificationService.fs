namespace FShot.Platform.Win32.Notifications

open System
open System.Diagnostics
open System.IO

/// Helper tĩnh để mở và highlight file ảnh trong Windows Explorer.
/// Phần notification UI đã chuyển sang FShot.UI.Services (Avalonia Window).
module NotificationHelper =

    /// Mở file ảnh trong Windows Explorer và highlight.
    /// Trả về true nếu khởi chạy explorer thành công.
    let highlightFileInExplorer(filePath: string) : bool =
        if not (File.Exists filePath) then false
        else
            try
                let args = sprintf "/select,\"%s\"" filePath
                let psi = ProcessStartInfo("explorer.exe", args)
                psi.UseShellExecute <- true
                Process.Start(psi) |> ignore
                true
            with _ ->
                false
