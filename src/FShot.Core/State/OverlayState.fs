namespace FShot.Core.State

open System
open FShot.Core.Geometry
open FShot.Core.Geometry.Operations
open FShot.Core.Domain

/// Loại con trỏ gợi ý để UI hiển thị.
/// Xem 08_05_RenderModel.md, mục 9.
type CursorHint =
    | Crosshair
    | Default
    | Move
    | ResizeNS
    | ResizeEW
    | ResizeNESW
    | ResizeNWSE

/// Thông tin về control nhập liệu text tạm thời.
/// Xem 08_05_RenderModel.md, mục 10.
type TextInputInfo = {
    Position: Point
    Content: string
    Style: AnnotationStyle
}

/// Dữ liệu đầu ra để UI vẽ scene.
/// Xem 08_05_RenderModel.md.
type RenderModel = {
    /// Kích thước toàn màn hình ảo.
    VirtualBounds: Rect

    /// Vùng chọn hiện tại. Nếu None, overlay đang ở Idle.
    Selection: Selection option

    /// Danh sách annotation đã commit.
    Annotations: Annotation list

    /// Annotation tạm thời đang vẽ.
    Preview: Annotation option

    /// Có hiển thị toolbar không.
    ToolbarVisible: bool

    /// Công cụ đang được chọn.
    CurrentTool: ToolKind

    /// Có thể undo hay không.
    CanUndo: bool

    /// Có thể redo hay không.
    CanRedo: bool

    /// Gợi ý con trỏ.
    Cursor: CursorHint

    /// Thông tin TextBox tạm, nếu đang nhập text.
    TextInput: TextInputInfo option
}

/// Các sự kiện đầu vào mà OverlayState xử lý.
/// Xem 08_03_Events.md.
type OverlayEvent =
    | PointerPressed of Point
    | PointerMoved of Point
    | PointerReleased
    | KeyDown of key: string
    | SelectTool of ToolKind
    | SetColor of Color
    | SetStrokeWidth of StrokeWidth
    | Undo
    | Redo
    | Copy
    | Save
    | Cancel
    | TextCommitted of string
    | ExportCompleted of success: bool
    /// Trạng thái phím Ctrl (hoặc phím modifier khóa tỉ lệ 1:1 cho Circle).
    | CtrlModifier of bool

/// Các lệnh UI cần thực hiện sau khi xử lý sự kiện.
/// Xem 08_06_Integration.md.
type OverlayCommand =
    | CloseOverlay
    | StartExport of ExportTarget
    | ShowTextInput of Point
    | HideTextInput

/// Trạng thái tương tác annotation đang diễn ra.
/// Xem 08_02_States.md, mục 2.4.
type AnnotationInteraction =
    | NoAnnotation
    | DrawingPreview of start: Point * current: Point * ToolKind
    | FreehandDrawing of points: Point list * ToolKind
    | EditingText of position: Point * content: string

/// Trạng thái tổng thể của overlay.
/// Xem 08_02_States.md, mục 4.
type OverlayState = {
    Capture: CaptureResult
    Config: ConfigSnapshot
    Selection: Selection
    AnnotationInteraction: AnnotationInteraction
    History: HistoryStack
    CurrentTool: ToolKind
    CurrentStyle: AnnotationStyle
    CtrlPressed: bool
}

/// Kết quả sau khi xử lý một sự kiện.
type OverlayResult = {
    State: OverlayState
    RenderModel: RenderModel
    Commands: OverlayCommand list
}

/// Module chứa logic xử lý trạng thái overlay.
/// Không phụ thuộc UI framework.
module OverlayStateLogic =

    /// Danh sách annotation đã commit từ snapshot hiện tại.
    let committedAnnotations (state: OverlayState) = state.History.Current.Annotations

    /// Kiểm tra key có phải là phím Cancel (Esc / Ctrl+Backspace) hay không.
    /// Chấp nhận các dạng: Escape, Esc, Ctrl+Backspace, Control+Backspace, Ctrl+Back.
    let isCancelKey (key: string) : bool =
        let normalized = key.ToLowerInvariant()
        normalized.Contains("esc")
        || normalized.Contains("ctrl+back")
        || normalized.Contains("control+back")

    /// Tạo annotation style từ trạng thái hiện tại.
    let private currentAnnotationStyle (state: OverlayState) = {
        Color = state.CurrentStyle.Color
        StrokeWidth = state.CurrentStyle.StrokeWidth
        FontSize = state.CurrentStyle.FontSize
        FontName = state.CurrentStyle.FontName
        FontStyle = state.CurrentStyle.FontStyle
    }

    /// Tạo Tool từ ToolKind và điểm tương tác.
    let private buildTool (kind: ToolKind) (start: Point) (current: Point) (ctrlPressed: bool) : Tool option =
        match kind with
        | SelectionTool -> None
        | PencilTool -> Some (Tool.Pencil [ start; current ])
        | LineTool -> Some (Tool.Line(start, current))
        | ArrowTool -> Some (Tool.Arrow(start, current, ArrowStyle.Standard))
        | RectangleTool -> Some (Tool.Rectangle(start, current, 0.0))
        | CircleTool -> Some (Tool.Circle(start, current, ctrlPressed))
        | MarkerTool -> Some (Tool.Marker [ start; current ])
        | PixelateTool -> Some (Tool.Pixelate(start, current, 10))
        | TextTool -> Some (Tool.Text(start, "", TextAlignment.Left))

    /// Cập nhật preview từ điểm hiện tại.
    let private updatePreview (current: Point) (interaction: AnnotationInteraction) : AnnotationInteraction =
        match interaction with
        | DrawingPreview(start, _, kind) -> DrawingPreview(start, current, kind)
        | FreehandDrawing(points, kind) ->
            match points with
            | [] -> FreehandDrawing([ current ], kind)
            | _ -> FreehandDrawing(points @ [ current ], kind)
        | other -> other

    /// Chuyển preview thành annotation hoàn chỉnh.
    let private commitPreview (state: OverlayState) : Annotation option =
        match state.AnnotationInteraction with
        | DrawingPreview(start, current, kind) ->
            buildTool kind start current state.CtrlPressed
            |> Option.map (fun tool -> Annotation.FromPreview(tool, currentAnnotationStyle state))
        | FreehandDrawing(points, kind) ->
            let tool =
                match kind with
                | PencilTool -> Some (Tool.Pencil points)
                | MarkerTool -> Some (Tool.Marker points)
                | _ -> None
            tool |> Option.map (fun t -> Annotation.FromPreview(t, currentAnnotationStyle state))
        | _ -> None

    /// Chuyển annotation Text đã nhập thành annotation hoàn chỉnh.
    let private commitText (content: string) (state: OverlayState) : Annotation option =
        match state.AnnotationInteraction with
        | EditingText(position, _) ->
            let tool = Tool.Text(position, content, TextAlignment.Left)
            Some (Annotation.FromPreview(tool, currentAnnotationStyle state))
        | _ -> None

    /// Tạo preview annotation từ trạng thái tương tác.
    let private currentPreview (state: OverlayState) : Annotation option =
        match state.AnnotationInteraction with
        | DrawingPreview(start, current, kind) ->
            buildTool kind start current state.CtrlPressed
            |> Option.map (fun tool -> Annotation.FromPreview(tool, currentAnnotationStyle state))
        | FreehandDrawing(points, kind) ->
            let tool =
                match kind with
                | PencilTool -> Some (Tool.Pencil points)
                | MarkerTool -> Some (Tool.Marker points)
                | _ -> None
            tool |> Option.map (fun t -> Annotation.FromPreview(t, currentAnnotationStyle state))
        | _ -> None

    /// Bắt đầu tương tác annotation tại điểm đã chọn.
    let private startAnnotation (point: Point) (state: OverlayState) : OverlayState =
        match state.CurrentTool with
        | TextTool ->
            { state with AnnotationInteraction = EditingText(point, "") }
        | PencilTool
        | MarkerTool ->
            { state with AnnotationInteraction = FreehandDrawing([ point ], state.CurrentTool) }
        | _ ->
            { state with AnnotationInteraction = DrawingPreview(point, point, state.CurrentTool) }

    /// Commit annotation hiện tại và push vào HistoryStack.
    let private commitAndPush (maker: OverlayState -> Annotation option) (state: OverlayState) : OverlayState =
        match maker state with
        | Some annotation ->
            let newAnnotations = committedAnnotations state @ [ annotation ]
            let snapshot = Snapshot.Create newAnnotations
            let newHistory = state.History.Push snapshot
            { state with
                History = newHistory
                AnnotationInteraction = NoAnnotation }
        | None ->
            { state with AnnotationInteraction = NoAnnotation }

    /// Xử lý Undo.
    let private performUndo (state: OverlayState) : OverlayState =
        let newHistory, _ = state.History.Undo()
        { state with History = newHistory }

    /// Xử lý Redo.
    let private performRedo (state: OverlayState) : OverlayState =
        let newHistory, _ = state.History.Redo()
        { state with History = newHistory }

    /// Hủy thao tác annotation đang diễn ra.
    let private cancelAnnotation (state: OverlayState) : OverlayState =
        { state with AnnotationInteraction = NoAnnotation }

    /// Xây dựng RenderModel từ trạng thái hiện tại.
    let buildRenderModel (state: OverlayState) : RenderModel =
        let toolbarVisible =
            match state.Selection.State with
            | SelectionState.Selected
            | SelectionState.Moving
            | SelectionState.Resizing _ -> true
            | _ -> false

        let cursor =
            match state.AnnotationInteraction with
            | EditingText _ -> CursorHint.Default
            | DrawingPreview _
            | FreehandDrawing _ -> CursorHint.Crosshair
            | NoAnnotation ->
                match state.Selection.State with
                | SelectionState.Idle -> CursorHint.Crosshair
                | SelectionState.Selecting -> CursorHint.Crosshair
                | SelectionState.Moving -> CursorHint.Move
                | SelectionState.Resizing handle ->
                    match handle with
                    | ResizeHandle.Top | ResizeHandle.Bottom -> CursorHint.ResizeNS
                    | ResizeHandle.Left | ResizeHandle.Right -> CursorHint.ResizeEW
                    | ResizeHandle.TopLeft | ResizeHandle.BottomRight -> CursorHint.ResizeNWSE
                    | ResizeHandle.TopRight | ResizeHandle.BottomLeft -> CursorHint.ResizeNESW
                | SelectionState.Selected ->
                    // Trong thuc te UI se hit-test de chon cursor chinh xac.
                    // O day tra ve Default; UI co the bo sung logic rieng.
                    CursorHint.Default

        let textInput =
            match state.AnnotationInteraction with
            | EditingText(position, content) ->
                Some {
                    Position = position
                    Content = content
                    Style = currentAnnotationStyle state
                }
            | _ -> None

        {
          VirtualBounds = state.Capture.VirtualBounds
          Selection =
              if state.Selection.State = SelectionState.Idle then None else Some state.Selection
          Annotations = committedAnnotations state
          Preview = currentPreview state
          ToolbarVisible = toolbarVisible
          CurrentTool = state.CurrentTool
          CanUndo = state.History.CanUndo
          CanRedo = state.History.CanRedo
          Cursor = cursor
          TextInput = textInput
        }

    /// Bắt đầu xuất ảnh với target tương ứng.
    let private startExport (target: ExportTarget) (state: OverlayState) : OverlayResult =
        let newState = { state with AnnotationInteraction = NoAnnotation }
        {
          State = newState
          RenderModel = buildRenderModel newState
          Commands = [ StartExport target ]
        }

    /// Khởi tạo overlay từ kết quả chụp và cấu hình.
    /// Xem 08_04_Transitions.md, mục 4.1.
    let init (capture: CaptureResult) (config: ConfigSnapshot) : OverlayState =
        let style = {
            Color = config.DefaultColor
            StrokeWidth = config.DefaultStrokeWidth
            FontSize = config.DefaultFontSize
            FontName = None
            FontStyle = []
        }
        {
          Capture = capture
          Config = config
          Selection = Selection.Empty
          AnnotationInteraction = NoAnnotation
          History = HistoryStack.Empty config.HistoryLimit
          CurrentTool = config.DefaultTool
          CurrentStyle = style
          CtrlPressed = false
        }

    /// Tạo OverlayResult từ state và commands.
    let private result (commands: OverlayCommand list) (state: OverlayState) : OverlayResult =
        {
          State = state
          RenderModel = buildRenderModel state
          Commands = commands
        }

    /// Tạo OverlayResult không có commands.
    let private emptyResult (state: OverlayState) : OverlayResult =
        result [] state

    /// Xử lý một sự kiện và trả về OverlayResult.
    /// Xem 08_04_Transitions.md.
    let update (event: OverlayEvent) (state: OverlayState) : OverlayResult =
        match state.Selection.State, state.AnnotationInteraction, event with

        // --- Idle ---
        | SelectionState.Idle, _, PointerPressed point ->
            let newState = { state with Selection = Selection.StartSelecting point }
            emptyResult newState

        | SelectionState.Idle, _, Cancel ->
            let newState = { state with Selection = Selection.Empty; AnnotationInteraction = NoAnnotation }
            result [ CloseOverlay ] newState

        | SelectionState.Idle, _, KeyDown key when isCancelKey key ->
            let newState = { state with Selection = Selection.Empty; AnnotationInteraction = NoAnnotation }
            result [ CloseOverlay ] newState

        | SelectionState.Idle, _, Save ->
            let fullScreen =
                { X = 0.0
                  Y = 0.0
                  Width = state.Capture.VirtualBounds.Width
                  Height = state.Capture.VirtualBounds.Height }
            let newState = { state with Selection = { Selection.Empty with State = SelectionState.Selected; Bounds = fullScreen } }
            startExport (ExportTarget.SaveToFile None) newState

        | SelectionState.Idle, _, Copy ->
            let fullScreen =
                { X = 0.0
                  Y = 0.0
                  Width = state.Capture.VirtualBounds.Width
                  Height = state.Capture.VirtualBounds.Height }
            let newState = { state with Selection = { Selection.Empty with State = SelectionState.Selected; Bounds = fullScreen } }
            startExport ExportTarget.CopyToClipboard newState

        // Ctrl modifier được xử lý ở mọi trạng thái để khóa tỉ lệ 1:1 cho Circle.
        | _, _, CtrlModifier ctrl ->
            emptyResult { state with CtrlPressed = ctrl }

        // --- Selecting ---
        | SelectionState.Selecting, _, PointerMoved point ->
            let newSelection = state.Selection.UpdateSelecting point
            let clamped = newSelection.ApplyConstraints state.Capture.VirtualBounds
            emptyResult { state with Selection = clamped }

        | SelectionState.Selecting, _, PointerReleased ->
            let finished = state.Selection.FinishSelecting()
            emptyResult { state with Selection = finished }

        | SelectionState.Selecting, _, Cancel ->
            emptyResult { state with Selection = Selection.Empty }

        | SelectionState.Selecting, _, KeyDown key when isCancelKey key ->
            emptyResult { state with Selection = Selection.Empty }

        // --- Selected ---
        | SelectionState.Selected, _, PointerPressed point ->
            match state.Selection.HitTestHandle point with
            | Some handle ->
                let newSelection = state.Selection.StartResizing handle point
                emptyResult { state with Selection = newSelection }
            | None ->
                if not (state.Selection.Contains point) then
                    // Hành vi Flameshot: nếu đã có annotations, click ngoài = copy rồi close overlay.
                    // Nếu chưa có annotations, cho phép reselect bình thường.
                    if not (List.isEmpty state.History.Current.Annotations) then
                        startExport ExportTarget.CopyToClipboard state
                    else
                        let newSelection = Selection.StartSelecting point
                        emptyResult { state with Selection = newSelection }
                elif state.CurrentTool = SelectionTool then
                    let newSelection = state.Selection.StartMoving point
                    emptyResult { state with Selection = newSelection }
                else
                    let newState = startAnnotation point state
                    let commands =
                        match newState.AnnotationInteraction with
                        | EditingText(position, _) -> [ ShowTextInput position ]
                        | _ -> []
                    result commands newState

        | SelectionState.Selected, NoAnnotation, KeyDown key when isCancelKey key ->
            let newState = { state with Selection = Selection.Empty }
            result [ CloseOverlay ] newState

        | SelectionState.Selected, NoAnnotation, KeyDown key ->
            let isShift = key.IndexOf("Shift", StringComparison.OrdinalIgnoreCase) >= 0

            let arrowDxDy =
                match key.ToLowerInvariant() with
                | k when k.Contains("left") -> Some(-1.0, 0.0)
                | k when k.Contains("right") -> Some(1.0, 0.0)
                | k when k.Contains("up") -> Some(0.0, -1.0)
                | k when k.Contains("down") -> Some(0.0, 1.0)
                | _ -> None

            match arrowDxDy with
            | Some(dx, dy) ->
                let rawSelection =
                    if isShift then
                        state.Selection.KeyboardResize(dx, dy)
                    else
                        state.Selection.Nudge(dx, dy)

                let clamped = rawSelection.ApplyConstraints state.Capture.VirtualBounds
                let finalSelection =
                    if clamped.IsValid then
                        clamped
                    else
                        Selection.Empty

                emptyResult { state with Selection = finalSelection }
            | None ->
                emptyResult state

        | SelectionState.Selected, NoAnnotation, SelectTool tool ->
            emptyResult { state with CurrentTool = tool }

        | SelectionState.Selected, NoAnnotation, SetColor color ->
            let newStyle = { state.CurrentStyle with Color = color }
            emptyResult { state with CurrentStyle = newStyle }

        | SelectionState.Selected, NoAnnotation, SetStrokeWidth width ->
            let newStyle = { state.CurrentStyle with StrokeWidth = width }
            emptyResult { state with CurrentStyle = newStyle }

        | SelectionState.Selected, NoAnnotation, Undo ->
            emptyResult (performUndo state)

        | SelectionState.Selected, NoAnnotation, Redo ->
            emptyResult (performRedo state)

        | SelectionState.Selected, NoAnnotation, Save ->
            startExport (ExportTarget.SaveToFile None) state

        | SelectionState.Selected, NoAnnotation, Copy ->
            startExport ExportTarget.CopyToClipboard state

        | SelectionState.Selected, NoAnnotation, Cancel ->
            let newState = { state with Selection = Selection.Empty }
            result [ CloseOverlay ] newState

        // --- MovingSelection ---
        | SelectionState.Moving, _, KeyDown key when isCancelKey key ->
            let restored =
                { state.Selection with
                    State = SelectionState.Selected
                    Bounds = state.Selection.OriginalBounds }
            emptyResult { state with Selection = restored }

        | SelectionState.Moving, _, PointerMoved point ->
            let newSelection = state.Selection.UpdateMoving point
            let clamped = newSelection.ApplyConstraints state.Capture.VirtualBounds
            emptyResult { state with Selection = clamped }

        | SelectionState.Moving, _, PointerReleased ->
            let newSelection = state.Selection.FinishInteraction()
            emptyResult { state with Selection = newSelection }

        // --- ResizingSelection ---
        | SelectionState.Resizing _, _, KeyDown key when isCancelKey key ->
            let restored =
                { state.Selection with
                    State = SelectionState.Selected
                    Bounds = state.Selection.OriginalBounds }
            emptyResult { state with Selection = restored }

        | SelectionState.Resizing _, _, PointerMoved point ->
            let newSelection = state.Selection.UpdateResizing point
            let clamped = newSelection.ApplyConstraints state.Capture.VirtualBounds
            emptyResult { state with Selection = clamped }

        | SelectionState.Resizing _, _, PointerReleased ->
            let newSelection = state.Selection.FinishInteraction()
            emptyResult { state with Selection = newSelection }

        // --- Annotating ---
        | _, DrawingPreview _, KeyDown key when isCancelKey key ->
            let newState = cancelAnnotation state
            emptyResult newState

        | _, FreehandDrawing _, KeyDown key when isCancelKey key ->
            let newState = cancelAnnotation state
            emptyResult newState

        | _, DrawingPreview _, PointerMoved point ->
            let newState = { state with AnnotationInteraction = updatePreview point state.AnnotationInteraction }
            emptyResult newState

        | _, FreehandDrawing _, PointerMoved point ->
            let newState = { state with AnnotationInteraction = updatePreview point state.AnnotationInteraction }
            emptyResult newState

        | _, DrawingPreview _, PointerReleased ->
            emptyResult (commitAndPush commitPreview state)

        | _, FreehandDrawing _, PointerReleased ->
            emptyResult (commitAndPush commitPreview state)

        | _, DrawingPreview _, Cancel
        | _, FreehandDrawing _, Cancel ->
            emptyResult (cancelAnnotation state)

        // --- TextEditing ---
        | _, EditingText _, KeyDown key when isCancelKey key ->
            let newState = cancelAnnotation state
            result [ HideTextInput ] newState

        | _, EditingText _, KeyDown key ->
            if key.Equals("Escape", StringComparison.OrdinalIgnoreCase) then
                let newState = cancelAnnotation state
                result [ HideTextInput ] newState
            else
                emptyResult state

        | _, EditingText _, TextCommitted content ->
            let newState = commitAndPush (commitText content) state
            result [ HideTextInput ] newState

        // --- Exporting ---
        | SelectionState.Selected, NoAnnotation, ExportCompleted success ->
            if success && state.Config.CloseAfterExport then
                let newState = { state with Selection = Selection.Empty }
                result [ CloseOverlay ] newState
            else
                emptyResult state

        // --- Tool selection (bắt kỳ trạng thái nào ngoài TextEditing đều chuyển được tool) ---
        | _, NoAnnotation, SelectTool tool ->
            emptyResult { state with CurrentTool = tool }

        | _, (DrawingPreview _ | FreehandDrawing _), SelectTool tool ->
            emptyResult { state with AnnotationInteraction = NoAnnotation; CurrentTool = tool }

        | _, EditingText _, SelectTool tool ->
            let newState = { state with AnnotationInteraction = NoAnnotation; CurrentTool = tool }
            result [ HideTextInput ] newState

        // --- Fallback: giữ nguyên trạng thái ---
        | _ ->
            emptyResult state
