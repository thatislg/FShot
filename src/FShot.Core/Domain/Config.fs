namespace FShot.Core.Domain

open FShot.Core.Geometry

/// Bản sao cấu hình tại thời điểm overlay mở.
/// OverlayState dùng bản sao này để tránh bị ảnh hưởng bởi thay đổi cấu hình trong lúc chụp.
/// Xem tài liệu 08_06_Integration.md, mục 8.
type ConfigSnapshot = {
    /// Công cụ mặc định khi overlay mở.
    DefaultTool: ToolKind

    /// Màu sắc mặc định cho nét vẽ.
    DefaultColor: Color

    /// Độ dày nét vẽ mặc định.
    DefaultStrokeWidth: StrokeWidth

    /// Cỡ chữ mặc định cho công cụ Text.
    DefaultFontSize: float

    /// Giới hạn số snapshot trong HistoryStack.
    HistoryLimit: int

    /// Có đóng overlay ngay sau khi xuất thành công không.
    CloseAfterExport: bool
} with
    /// Cấu hình mặc định cho MVP.
    static member Default = {
        DefaultTool = SelectionTool
        DefaultColor = Color.Red
        DefaultStrokeWidth = StrokeWidth.Create 2.0
        DefaultFontSize = 14.0
        HistoryLimit = HistoryStack.DefaultLimit
        CloseAfterExport = true
    }
