namespace FShot.Platform.Win32.Config

open System
open System.IO
open System.Threading
open FShot.Core.Domain

/// Mô-đun đọc và ghi cấu hình người dùng tại %APPDATA%\FShot\config.json.
/// Xem tài liệu 07_04_FileStore.md và 10_08_ConfigStore.md.
module ConfigStore =

    /// Thư mục cấu hình chuẩn tại %APPDATA%\FShot.
    let defaultConfigDir =
        let appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        Path.Combine(appData, "FShot")

    /// Đường dẫn file cấu hình chuẩn tại %APPDATA%\FShot\config.json.
    let defaultConfigFile =
        Path.Combine(defaultConfigDir, "config.json")

    /// Ghi chuỗi nội dung vào file theo mẫu nguyên tử (Atomic Replace Pattern):
    /// ghi sang file .tmp rồi hoán đổi bằng File.Move(overwrite = true),
    /// chống hỏng file nếu mất điện / crash giữa chừng. Xem 10_08_ConfigStore.md, mục 3.
    let private writeAtomic (filePath: string) (content: string) : unit =
        let dir = Path.GetDirectoryName(filePath)
        if not (String.IsNullOrWhiteSpace dir) && not (Directory.Exists dir) then
            Directory.CreateDirectory(dir) |> ignore

        let tmpPath = filePath + ".tmp"
        use stream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)
        use writer = new StreamWriter(stream, Text.Encoding.UTF8)
        writer.Write(content)
        writer.Flush()
        stream.Flush(true) // Đảm bảo dữ liệu đã xuống đĩa (FlushBuffers)
        writer.Dispose()
        stream.Dispose()
        File.Move(tmpPath, filePath, true)

    /// Nạp cấu hình từ đường dẫn chỉ định.
    /// Nếu file chưa tồn tại, tự động tạo file và thư mục cha với cấu hình mặc định.
    /// Nếu file hỏng hoặc lỗi JSON, sao lưu sang .bad rồi fallback an toàn về AppConfig.Default.
    let loadConfigFrom (filePath: string) : AppConfig =
        try
            let dir = Path.GetDirectoryName(filePath)
            if not (String.IsNullOrWhiteSpace dir) && not (Directory.Exists dir) then
                Directory.CreateDirectory(dir) |> ignore

            if not (File.Exists filePath) then
                let defaultConfig = AppConfig.Default
                let json = ConfigJson.serialize defaultConfig
                writeAtomic filePath json
                defaultConfig
            else
                let json = File.ReadAllText(filePath)
                match ConfigJson.deserialize json with
                | Some cfg -> cfg.Normalized()
                | None ->
                    // File JSON lỗi cú pháp hoặc rỗng -> sao lưu hiện trường rồi fallback về Default.
                    try
                        File.Copy(filePath, filePath + ".bad", true)
                    with _ ->
                        ()
                    AppConfig.Default
        with _ ->
            AppConfig.Default

    /// Ghi cấu hình ra file chỉ định với định dạng indented JSON (ghi nguyên tử).
    let saveConfigTo (filePath: string) (config: AppConfig) : unit =
        try
            let json = ConfigJson.serialize (config.Normalized())
            writeAtomic filePath json
        with _ ->
            ()

    /// Nạp cấu hình từ file mặc định %APPDATA%\FShot\config.json.
    let loadConfig () : AppConfig =
        loadConfigFrom defaultConfigFile

    /// Ghi cấu hình vào file mặc định %APPDATA%\FShot\config.json.
    let saveConfig (config: AppConfig) : unit =
        saveConfigTo defaultConfigFile config

    /// Ghi cấu hình vào file mặc định và báo lỗi chi tiết nếu thất bại (không nuốt exception).
    /// Dùng cho luồng Apply từ cửa sổ Settings để hiển thị thông báo thành công/lỗi cho người dùng.
    let trySaveConfig (config: AppConfig) : Result<unit, string> =
        try
            let json = ConfigJson.serialize (config.Normalized())
            writeAtomic defaultConfigFile json
            Ok ()
        with ex ->
            Error (sprintf "Không thể ghi config.json: %s" ex.Message)

    /// Nạp snapshot cấu hình từ %APPDATA%\FShot\config.json dùng cho OverlayState.
    let loadSnapshot () : ConfigSnapshot =
        (loadConfig()).ToSnapshot()

    /// Nạp snapshot cấu hình từ đường dẫn file chỉ định.
    let loadSnapshotFrom (filePath: string) : ConfigSnapshot =
        (loadConfigFrom filePath).ToSnapshot()

    /// Theo dõi thay đổi file config.json và phát callback sau khoảng debounce (ms).
    /// Trả về IDisposable để hủy theo dõi khi không còn cần (FR-SYS-011).
    /// Callback chạy trên luồng thread-pool; caller chịu trách nhiệm dispatch về UI thread nếu cần.
    let watchConfig (debounceMs: int) (onChanged: unit -> unit) : IDisposable =
        let dir = defaultConfigDir
        let watcher = new FileSystemWatcher(dir, "config.json")
        watcher.NotifyFilter <- NotifyFilters.LastWrite ||| NotifyFilters.FileName
        watcher.EnableRaisingEvents <- true

        let mutable debounceTimer: Timer option = None

        let onFileChanged (_: FileSystemEventArgs) =
            // Bỏ qua sự kiện gây ra bởi chính tiến trình F-Shot ghi file .tmp / hoán đổi.
            match debounceTimer with
            | Some t ->
                t.Dispose()
                debounceTimer <- None
            | None -> ()

            let timer =
                new Timer(
                    (fun _ ->
                        match debounceTimer with
                        | Some t ->
                            t.Dispose()
                            debounceTimer <- None
                        | None -> ()
                        onChanged()),
                    null,
                    debounceMs,
                    Timeout.Infinite)

            debounceTimer <- Some timer

        watcher.Changed.Add(onFileChanged)

        { new IDisposable with
            member _.Dispose() =
                match debounceTimer with
                | Some t ->
                    t.Dispose()
                    debounceTimer <- None
                | None -> ()
                watcher.Dispose() }
