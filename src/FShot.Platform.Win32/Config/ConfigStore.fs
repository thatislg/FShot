namespace FShot.Platform.Win32.Config

open System
open System.IO
open FShot.Core.Domain

/// Mô-đun đọc và ghi cấu hình người dùng tại %APPDATA%\FShot\config.json.
/// Xem tài liệu 07_04_FileStore.md.
module ConfigStore =

    /// Thư mục cấu hình chuẩn tại %APPDATA%\FShot.
    let defaultConfigDir =
        let appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        Path.Combine(appData, "FShot")

    /// Đường dẫn file cấu hình chuẩn tại %APPDATA%\FShot\config.json.
    let defaultConfigFile =
        Path.Combine(defaultConfigDir, "config.json")

    /// Nạp cấu hình từ đường dẫn chỉ định.
    /// Nếu file chưa tồn tại, tự động tạo file và thư mục cha với cấu hình mặc định.
    /// Nếu file hỏng hoặc lỗi JSON, fallback an toàn về cấu hình mặc định AppConfig.Default.
    let loadConfigFrom (filePath: string) : AppConfig =
        try
            let dir = Path.GetDirectoryName(filePath)
            if not (String.IsNullOrWhiteSpace dir) && not (Directory.Exists dir) then
                Directory.CreateDirectory(dir) |> ignore

            if not (File.Exists filePath) then
                let defaultConfig = AppConfig.Default
                let json = ConfigJson.serialize defaultConfig
                File.WriteAllText(filePath, json)
                defaultConfig
            else
                let json = File.ReadAllText(filePath)
                match ConfigJson.deserialize json with
                | Some cfg -> cfg.Normalized()
                | None ->
                    // File JSON lỗi cú pháp hoặc rỗng -> fallback về AppConfig.Default
                    AppConfig.Default
        with _ ->
            AppConfig.Default

    /// Ghi cấu hình ra file chỉ định với định dạng indented JSON.
    let saveConfigTo (filePath: string) (config: AppConfig) : unit =
        try
            let dir = Path.GetDirectoryName(filePath)
            if not (String.IsNullOrWhiteSpace dir) && not (Directory.Exists dir) then
                Directory.CreateDirectory(dir) |> ignore

            let json = ConfigJson.serialize (config.Normalized())
            File.WriteAllText(filePath, json)
        with _ ->
            ()

    /// Nạp cấu hình từ file mặc định %APPDATA%\FShot\config.json.
    let loadConfig () : AppConfig =
        loadConfigFrom defaultConfigFile

    /// Ghi cấu hình vào file mặc định %APPDATA%\FShot\config.json.
    let saveConfig (config: AppConfig) : unit =
        saveConfigTo defaultConfigFile config

    /// Nạp snapshot cấu hình từ %APPDATA%\FShot\config.json dùng cho OverlayState.
    let loadSnapshot () : ConfigSnapshot =
        (loadConfig()).ToSnapshot()

    /// Nạp snapshot cấu hình từ đường dẫn file chỉ định.
    let loadSnapshotFrom (filePath: string) : ConfigSnapshot =
        (loadConfigFrom filePath).ToSnapshot()
