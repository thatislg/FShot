# Single-Instance Enforcement

Tài liệu này mô tả cơ chế đảm bảo chỉ có một instance F-Shot chạy đồng thời, tương ứng với `FR-SYS-010`.

## 1. Mục tiêu

- Khi người dùng chạy F-Shot lần đầu, ứng dụng khởi động bình thường và lắng nghe các lệnh từ instance tương lai.
- Khi chạy F-Shot lần thứ hai (ví dụ `fshot gui`), instance mới gửi tham số dòng lệnh sang instance đang chạy qua IPC, rồi tự thoát ngay lập tức.
- Hỗ trợ cờ `--allow-multiple` để bỏ qua kiểm tra single-instance khi debug / kiểm thử.

## 2. Kiến trúc

```
┌─────────────────────────────────────┐
│           Instance thứ hai          │
│  1. Thử giành Mutex                 │
│  2. Nếu thất bại -> NamedPipeClient │
│  3. Gửi args JSON                   │
│  4. Thoát (exit code 0)             │
└─────────────┬───────────────────────┘
              │ NamedPipe
┌─────────────▼───────────────────────┐
│          Instance đầu tiên        │
│  1. Giữ Mutex toàn cục             │
│  2. NamedPipeServer lắng nghe       │
│  3. Nhận args -> dispatch command  │
│     (mở overlay GUI nếu là "gui")   │
└─────────────────────────────────────┘
```

## 3. Mutex toàn cục

- Tên: `Global\FShot_SingleInstance_Mutex`
- Sử dụng `System.Threading.Mutex(true, name, &createdNew)`.
- Nếu `createdNew = true`, đây là instance đầu tiên.
- Nếu `createdNew = false`, đã có instance khác đang chạy.

## 4. IPC qua Named Pipe

- Tên pipe: `FShot_Ipc_Pipe`
- Message format: JSON của `IpcMessage`:

```json
{
  "arguments": ["gui", "-d", "3000"],
  "timestamp": "2026-09-24T10:30:00Z"
}
```

- Instance đầu tiên chạy `NamedPipeServerStream` trong luồng nền async.
- Instance thứ hai kết nối bằng `NamedPipeClientStream`, ghi JSON, flush, rồi đóng.

## 5. Cờ `--allow-multiple`

- Khi CLI parse thấy `--allow-multiple`, `SingleInstance.enforce` trả về `MultipleAllowed` và bỏ qua Mutex/IPC.
- Dùng cho môi trường debug hoặc kiểm thử song song.

## 6. Dispatch lệnh IPC

Khi instance đầu tiên nhận lệnh:

- `gui` hoặc bất kỳ lệnh nào có `OutputTarget = OpenGui` -> mở `CaptureOverlayWindow` qua `App.HandleIpcCommand`.
- `full` / `screen` (headless) -> hiện tại ghi log và chưa tự động chạy capture trong daemon (để tránh ảnh hưởng UI).

## 7. Vòng đời

- Mutex được giữ sống suốt vòng đời process; khi process kết thúc, Windows tự giải phóng.
- `CancellationTokenSource` được tạo trong `Program.main` để hủy IPC server khi ứng dụng thoát.

## 8. Tham khảo

- `src/FShot.Platform.Win32/Lifecycle/SingleInstance.fs`
- `src/FShot.UI/Program.fs` — gọi `SingleInstance.enforce`
- `src/FShot.UI/App.axaml.fs` — `App.HandleIpcCommand`
- `FR-SYS-010`: `docs/1.Investigation/003_SRS.md`
