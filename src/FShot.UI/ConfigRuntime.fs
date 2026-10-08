namespace FShot.UI

open FShot.Core.Domain

/// Cầu nối cho các cửa sổ biên dịch trước `App` (như SettingsWindow) có thể yêu cầu
/// App áp dụng lại cấu hình runtime mà không tạo tham chiếu tới (forward reference).
module ConfigRuntime =

    /// Callback do App đăng ký lúc khởi động để áp dụng cấu hình cho instance đang chạy.
    let mutable applyCallback: (unit -> Result<unit, string>) option = None

    /// Yêu cầu App áp dụng cấu hình mới (refresh snapshot + rebind hotkey + sync startup).
    let apply() : Result<unit, string> =
        match applyCallback with
        | Some f -> f()
        | None -> Error "Ứng dụng chưa sẵn sàng để áp dụng cấu hình"
