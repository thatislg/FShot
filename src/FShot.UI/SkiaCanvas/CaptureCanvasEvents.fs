namespace FShot.UI.SkiaCanvas

/// Kết quả xuất ảnh để thông báo cho app bên ngoài.
type ExportOutcome =
    | Saved of filePath: string
    | Copied
    | Failed

/// Sự kiện toàn cục báo hiệu người dùng yêu cầu hủy thao tác chụp (nhấn Esc trên overlay),
/// và kết quả xuất ảnh để phát thông báo.
/// Đặt trong file riêng để PinWindow (compile trước CaptureCanvas) có thể tham chiếu.
type CaptureCanvasEvents() =
    static let abortRequested = Event<unit>()
    static let exportCompleted = Event<ExportOutcome>()

    static member AbortRequested = abortRequested.Publish
    static member ExportCompleted = exportCompleted.Publish

    static member TriggerAbortRequested() = abortRequested.Trigger()
    static member TriggerExportCompleted(outcome: ExportOutcome) = exportCompleted.Trigger(outcome)
