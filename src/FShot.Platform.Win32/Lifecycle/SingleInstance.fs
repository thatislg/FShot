namespace FShot.Platform.Win32.Lifecycle

open System
open System.IO
open System.IO.Pipes
open System.Text
open System.Text.Json
open System.Threading

/// Message gửi qua named pipe từ instance thứ hai sang instance đang chạy.
type IpcMessage =
    {
      Arguments: string[]
      Timestamp: DateTime
    }

    static member Create(args: string[]) =
        { Arguments = args; Timestamp = DateTime.UtcNow }

/// Kết quả kiểm tra single-instance.
type SingleInstanceCheckResult =
    /// Đây là instance đầu tiên; cần bắt đầu lắng nghe IPC.
    | FirstInstance of mutex: Mutex
    /// Đã có instance khác đang chạy; lệnh đã được chuyển tiếp.
    | SecondaryInstance
    /// Bỏ qua kiểm tra (debug/tests).
    | MultipleAllowed

/// Module triển khai cơ chế single-instance và IPC cho F-Shot.
/// Xem tài liệu 10_10_SingleInstance.md.
module SingleInstance =

    let private mutexName = "Global\\FShot_SingleInstance_Mutex"
    let private pipeName = "FShot_Ipc_Pipe"
    let private pipeTimeoutMs = 5000

    /// Serialize IpcMessage sang JSON.
    let serializeMessage (msg: IpcMessage) : string =
        JsonSerializer.Serialize(msg)

    /// Deserialize JSON sang IpcMessage.
    let deserializeMessage (json: string) : IpcMessage option =
        try
            Some (JsonSerializer.Deserialize<IpcMessage>(json))
        with _ ->
            None

    /// Gửi tham số dòng lệnh sang instance đang chạy qua named pipe.
    /// Trả về Ok nếu gửi thành công.
    let sendCommandToFirstInstance (args: string[]) : Result<unit, string> =
        try
            use client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out)
            client.Connect(pipeTimeoutMs)
            let json = serializeMessage (IpcMessage.Create args)
            let bytes = Encoding.UTF8.GetBytes(json)
            client.Write(bytes, 0, bytes.Length)
            client.Flush()
            Ok ()
        with ex ->
            Error (sprintf "Failed to send IPC command: %s" ex.Message)

    /// Bắt đầu named pipe server trong luồng nền để lắng nghe lệnh từ instance khác.
    /// `onCommand` được gọi với mảng args khi nhận được message hợp lệ.
    let startIpcServer (cancellationToken: CancellationToken) (onCommand: string[] -> unit) : unit =
        let rec runServer () =
            async {
                try
                    use server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1)
                    do! server.WaitForConnectionAsync(cancellationToken) |> Async.AwaitTask

                    use reader = new StreamReader(server, Encoding.UTF8)
                    let! json = reader.ReadToEndAsync() |> Async.AwaitTask

                    match deserializeMessage json with
                    | Some msg ->
                        onCommand msg.Arguments
                    | None ->
                        ()

                    return! runServer ()
                with
                | :? OperationCanceledException ->
                    ()
                | ex ->
                    // Tránh vòng lặp chặt khi lỗi liên tục.
                    do! Async.Sleep 100
                    return! runServer ()
            }

        Async.Start(runServer (), cancellationToken)

    /// Thử giành quyền sở hữu mutex toàn cục để xác định instance đầu tiên.
    /// Trả về FirstInstance nếu thành công, SecondaryInstance nếu đã có instance khác.
    let tryAcquireFirstInstance () : Result<SingleInstanceCheckResult, string> =
        let mutable createdNew = false
        try
            let mutex = new Mutex(true, mutexName, &createdNew)
            if createdNew then
                Ok (FirstInstance mutex)
            else
                // Đã có instance khác nắm mutex; giải phóng ngay nếu ta vô tình mở.
                mutex.Dispose()
                Ok SecondaryInstance
        with ex ->
            Error (sprintf "Mutex acquisition failed: %s" ex.Message)

    /// Kiểm tra và xử lý single-instance.
    /// - Nếu `allowMultiple` là true: luôn trả về MultipleAllowed.
    /// - Nếu là instance thứ hai: chuyển tiếp args qua IPC và trả về SecondaryInstance.
    /// - Nếu là instance đầu tiên: khởi động IPC server và trả về FirstInstance kèm mutex.
    let enforce (args: string[]) (allowMultiple: bool) (cancellationToken: CancellationToken) (onCommand: string[] -> unit) : SingleInstanceCheckResult =
        if allowMultiple then
            MultipleAllowed
        else
            match tryAcquireFirstInstance() with
            | Error err ->
                // Fallback: cho phép chạy để tránh deadlock.
                MultipleAllowed
            | Ok SecondaryInstance ->
                match sendCommandToFirstInstance args with
                | Ok () ->
                    SecondaryInstance
                | Error _ ->
                    MultipleAllowed
            | Ok MultipleAllowed ->
                MultipleAllowed
            | Ok (FirstInstance mutex) ->
                startIpcServer cancellationToken onCommand
                // Giữ mutex sống bằng cách không dispose; nó sẽ được giải phóng khi process kết thúc.
                GC.KeepAlive(mutex)
                FirstInstance mutex
