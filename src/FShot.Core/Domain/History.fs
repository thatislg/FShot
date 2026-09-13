namespace FShot.Core.Domain

open System

/// Bản ghi trạng thái của overlay tại một thời điểm nhất định.
/// Xem tài liệu 05_02_Snapshot.md.
type Snapshot = {
    /// Danh sách chú thích đã được commit.
    Annotations: Annotation list
} with
    /// Tạo snapshot ban đầu với danh sách chú thích rỗng.
    static member Empty = { Annotations = [] }

    /// Tạo snapshot mới từ danh sách chú thích.
    static member Create(annotations: Annotation list) = { Annotations = annotations }

/// Stack lịch sử bất biến phục vụ hoàn tác và làm lại.
/// Xem tài liệu 05_03_HistoryStack.md.
type HistoryStack = {
    /// Danh sách snapshot đã ghi nhận theo thứ tự thời gian.
    /// Phần tử cuối cùng là trạng thái hiện tại.
    UndoStack: Snapshot list

    /// Danh sách snapshot đã bị đẩy ra khỏi undo stack khi hoàn tác.
    RedoStack: Snapshot list

    /// Giới hạn số lượng snapshot tối đa trong undo stack.
    Limit: int
} with
    /// Giới hạn mặc định theo yêu cầu SRS FR-UNDO-003.
    static member DefaultLimit = 100

    /// Giá trị giới hạn tối thiểu khuyến nghị trong MVP.
    /// Lưu ý: Core không cứng giới hạn này; giá trị thấp hơn vẫn được tôn trọng
    /// để đảm bảo khả năng kiểm thử và linh hoạt cấu hình.
    static member MinLimit = 10

    /// Đảm bảo giới hạn ít nhất là 1, tránh stack rỗng.
    static member private NormalizeLimit(limit: int) = max 1 limit

    /// Tạo HistoryStack rỗng với snapshot ban đầu.
    static member Empty(limit: int) =
        {
          UndoStack = [ Snapshot.Empty ]
          RedoStack = []
          Limit = HistoryStack.NormalizeLimit limit
        }

    /// Trạng thái hiện tại, là snapshot ở đỉnh undo stack.
    member this.Current =
        match this.UndoStack with
        | [] -> Snapshot.Empty
        | snapshots -> List.last snapshots

    /// Kiểm tra có thể hoàn tác hay không.
    /// Điều kiện: undo stack có nhiều hơn một snapshot.
    member this.CanUndo = List.length this.UndoStack > 1

    /// Kiểm tra có thể làm lại hay không.
    /// Điều kiện: redo stack không rỗng.
    member this.CanRedo = not (List.isEmpty this.RedoStack)

    /// Thêm snapshot mới vào undo stack, xóa redo stack.
    /// Nếu undo stack vượt quá giới hạn, các snapshot cũ nhất bị loại bỏ.
    /// Xem 05_03_HistoryStack.md, mục 5.1.
    member this.Push(snapshot: Snapshot) : HistoryStack =
        let newLimit = HistoryStack.NormalizeLimit this.Limit
        let combined = this.UndoStack @ [ snapshot ]
        let count = List.length combined
        let trimmed =
            if count > newLimit then
                let skipCount = count - newLimit
                combined |> List.skip skipCount
            else
                combined

        { this with UndoStack = trimmed; RedoStack = [] }

    /// Hoàn tác một bước, chuyển snapshot hiện tại sang redo stack.
    /// Trả về tuple chứa HistoryStack mới và snapshot hiện tại sau undo.
    /// Nếu không thể undo, trả về HistoryStack cũ và snapshot hiện tại.
    /// Xem 05_03_HistoryStack.md, mục 5.2.
    member this.Undo() : HistoryStack * Snapshot =
        match List.rev this.UndoStack with
        | current :: previousRev when this.CanUndo ->
            let newUndo = List.rev previousRev
            let newRedo = current :: this.RedoStack
            let newStack = { this with UndoStack = newUndo; RedoStack = newRedo }
            newStack, newStack.Current
        | _ ->
            this, this.Current

    /// Làm lại một bước, chuyển snapshot từ redo stack sang undo stack.
    /// Trả về tuple chứa HistoryStack mới và snapshot hiện tại sau redo.
    /// Nếu không thể redo, trả về HistoryStack cũ và snapshot hiện tại.
    /// Xem 05_03_HistoryStack.md, mục 5.3.
    member this.Redo() : HistoryStack * Snapshot =
        match this.RedoStack with
        | restored :: rest ->
            let newUndo = this.UndoStack @ [ restored ]
            let newStack = { this with UndoStack = newUndo; RedoStack = rest }
            newStack, restored
        | [] ->
            this, this.Current

    /// Thay đổi giới hạn stack. Cắt bớt chỉ xảy ra ở lần Push tiếp theo.
    /// Xem 05_04_Integration.md, mục 6.
    member this.SetLimit(limit: int) : HistoryStack =
        { this with Limit = HistoryStack.NormalizeLimit limit }
