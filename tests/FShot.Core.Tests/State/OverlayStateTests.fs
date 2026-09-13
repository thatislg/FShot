module FShot.Core.Tests.State.OverlayStateTests

open System
open FShot.Core.Geometry
open FShot.Core.Domain
open FShot.Core.State
open FShot.Core.State.OverlayStateLogic
open Xunit

let private captureResult : CaptureResult = {
    Pixels = Array.empty
    Width = 1920
    Height = 1080
    Stride = 1920 * 4
    PixelFormat = PixelFormat.Bgra32
    VirtualBounds = { X = 0.0; Y = 0.0; Width = 1920.0; Height = 1080.0 }
    ScaleFactor = ScaleFactor.Create 1.0
    ScreenIndex = 0
}

let private defaultConfig = ConfigSnapshot.Default

let private point x y = { X = x; Y = y }

let private initResult() = init captureResult defaultConfig

/// Kiểm tra khởi tạo overlay tạo trạng thái Idle.
[<Fact>]
let ``Init tạo trạng thái Idle`` () =
    let state = initResult()
    Assert.Equal(SelectionState.Idle, state.Selection.State)
    Assert.Equal(NoAnnotation, state.AnnotationInteraction)
    Assert.Equal(SelectionTool, state.CurrentTool)
    Assert.False(state.History.CanUndo)
    Assert.False(state.History.CanRedo)

/// Kiểm tra Idle + PointerPressed chuyển sang Selecting.
[<Fact>]
let ``Idle PointerPressed bắt đầu Selecting`` () =
    let result = initResult() |> update (PointerPressed(point 100.0 100.0))
    Assert.Equal(SelectionState.Selecting, result.State.Selection.State)
    Assert.Equal(point 100.0 100.0, result.State.Selection.DragStart)
    Assert.True(result.RenderModel.ToolbarVisible |> not)

/// Kiểm tra Selecting + PointerMoved cập nhật bounds.
[<Fact>]
let ``Selecting PointerMoved cập nhật vùng chọn`` () =
    let state = initResult() |> update (PointerPressed(point 100.0 100.0)) |> (fun r -> r.State)
    let result = state |> update (PointerMoved(point 300.0 200.0))
    Assert.Equal(SelectionState.Selecting, result.State.Selection.State)
    Assert.Equal(200.0, result.State.Selection.Bounds.Width)
    Assert.Equal(100.0, result.State.Selection.Bounds.Height)

/// Kiểm tra Selecting + PointerReleased vùng đủ lớn chuyển sang Selected.
[<Fact>]
let ``Selecting PointerReleased đủ lớn chuyển Selected`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)

    let result = state |> update PointerReleased
    Assert.Equal(SelectionState.Selected, result.State.Selection.State)
    Assert.True(result.RenderModel.ToolbarVisible)
    Assert.True(result.State.Selection.IsValid)

/// Kiểm tra Selecting + PointerReleased vùng quá nhỏ quay về Idle.
[<Fact>]
let ``Selecting PointerReleased quá nhỏ quay về Idle`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 101.0 101.0))
        |> (fun r -> r.State)

    let result = state |> update PointerReleased
    Assert.Equal(SelectionState.Idle, result.State.Selection.State)
    Assert.False(result.RenderModel.ToolbarVisible)

/// Kiểm tra Selected + PointerPressed trên handle bắt đầu Resizing.
[<Fact>]
let ``Selected PointerPressed trên handle bắt đầu Resizing`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)

    // BottomRight handle của vùng (100,100,200,100) nằm tại (300,200)
    let result = state |> update (PointerPressed(point 300.0 200.0))
    Assert.Equal(SelectionState.Resizing BottomRight, result.State.Selection.State)

/// Kiểm tra Moving + PointerReleased chuyển sang Selected.
[<Fact>]
let ``Moving PointerReleased chuyển sang Selected`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)

    let moving = state |> update (PointerPressed(point 150.0 150.0))
    Assert.Equal(SelectionState.Moving, moving.State.Selection.State)

    let moved = moving.State |> update (PointerMoved(point 200.0 180.0))
    Assert.Equal(150.0, moved.State.Selection.Bounds.X)
    Assert.Equal(130.0, moved.State.Selection.Bounds.Y)
    Assert.Equal(200.0, moved.State.Selection.Bounds.Width)
    Assert.Equal(100.0, moved.State.Selection.Bounds.Height)

    let finished = moved.State |> update PointerReleased
    Assert.Equal(SelectionState.Selected, finished.State.Selection.State)
    Assert.True(finished.RenderModel.ToolbarVisible)

/// Kiểm tra Resizing + PointerReleased chuyển sang Selected.
[<Fact>]
let ``Resizing PointerReleased chuyển sang Selected`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)

    let resizing = state |> update (PointerPressed(point 300.0 200.0))
    Assert.Equal(SelectionState.Resizing BottomRight, resizing.State.Selection.State)

    let resized = resizing.State |> update (PointerMoved(point 400.0 300.0))
    Assert.Equal(300.0, resized.State.Selection.Bounds.Width)
    Assert.Equal(200.0, resized.State.Selection.Bounds.Height)

    let finished = resized.State |> update PointerReleased
    Assert.Equal(SelectionState.Selected, finished.State.Selection.State)

/// Kiểm tra Selected + PointerPressed ngoài vùng bắt đầu vùng chọn mới.
[<Fact>]
let ``Selected PointerPressed ngoài vùng bắt đầu Selecting mới`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)

    let result = state |> update (PointerPressed(point 500.0 500.0))
    Assert.Equal(SelectionState.Selecting, result.State.Selection.State)
    Assert.Equal(point 500.0 500.0, result.State.Selection.DragStart)

/// Kiểm tra Selected + PointerPressed trong vùng với SelectionTool bắt đầu Moving.
[<Fact>]
let ``Selected PointerPressed trong vùng với SelectionTool di chuyển vùng chọn`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)

    Assert.Equal(SelectionTool, state.CurrentTool)
    let result = state |> update (PointerPressed(point 150.0 150.0))
    Assert.Equal(SelectionState.Moving, result.State.Selection.State)

/// Kiểm tra Selected + SelectTool + PointerPressed trong vùng bắt đầu Annotating.
[<Fact>]
let ``Selected chọn LineTool và nhấn trong vùng bắt đầu Annotating`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)
        |> update (SelectTool LineTool)
        |> (fun r -> r.State)

    let result = state |> update (PointerPressed(point 150.0 150.0))
    Assert.True(result.RenderModel.Preview.IsSome)
    match result.State.AnnotationInteraction with
    | DrawingPreview(start, _, kind) ->
        Assert.Equal(point 150.0 150.0, start)
        Assert.Equal(LineTool, kind)
    | _ -> Assert.True(false, "Expected DrawingPreview")

/// Kiểm tra Annotating + PointerMoved cập nhật preview.
[<Fact>]
let ``Annotating PointerMoved cập nhật preview`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)
        |> update (SelectTool LineTool)
        |> (fun r -> r.State)
        |> update (PointerPressed(point 150.0 150.0))
        |> (fun r -> r.State)

    let result = state |> update (PointerMoved(point 250.0 250.0))
    match result.State.AnnotationInteraction with
    | DrawingPreview(start, current, kind) ->
        Assert.Equal(point 150.0 150.0, start)
        Assert.Equal(point 250.0 250.0, current)
        Assert.Equal(LineTool, kind)
    | _ -> Assert.True(false, "Expected DrawingPreview")

/// Kiểm tra Annotating + PointerReleased commit annotation và push history.
[<Fact>]
let ``Annotating PointerReleased commit và push history`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)
        |> update (SelectTool LineTool)
        |> (fun r -> r.State)
        |> update (PointerPressed(point 150.0 150.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 250.0 250.0))
        |> (fun r -> r.State)

    let result = state |> update PointerReleased
    Assert.Equal(NoAnnotation, result.State.AnnotationInteraction)
    Assert.Equal(SelectionState.Selected, result.State.Selection.State)
    Assert.Equal(1, List.length result.State.History.Current.Annotations)
    Assert.True(result.State.History.CanUndo)
    Assert.False(result.RenderModel.Preview.IsSome)

/// Kiểm tra Annotating + Cancel hủy preview.
[<Fact>]
let ``Annotating Cancel hủy preview`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)
        |> update (SelectTool LineTool)
        |> (fun r -> r.State)
        |> update (PointerPressed(point 150.0 150.0))
        |> (fun r -> r.State)

    let result = state |> update Cancel
    Assert.Equal(NoAnnotation, result.State.AnnotationInteraction)
    Assert.True(result.RenderModel.Preview.IsNone)
    Assert.Equal(0, List.length result.State.History.Current.Annotations)

/// Kiểm tra Undo/Redo hoạt động sau khi commit annotation.
[<Fact>]
let ``Undo và Redo hoạt động sau commit`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)
        |> update (SelectTool LineTool)
        |> (fun r -> r.State)
        |> update (PointerPressed(point 150.0 150.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 250.0 250.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)

    Assert.Equal(1, List.length state.History.Current.Annotations)
    let afterUndo = state |> update Undo
    Assert.Equal(0, List.length afterUndo.State.History.Current.Annotations)
    Assert.True(afterUndo.State.History.CanRedo)

    let afterRedo = afterUndo.State |> update Redo
    Assert.Equal(1, List.length afterRedo.State.History.Current.Annotations)

/// Kiểm tra Selected + Save tạo command StartExport.
[<Fact>]
let ``Selected Save tạo StartExport command`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)

    let result = state |> update Save
    Assert.Single(result.Commands) |> ignore
    match result.Commands with
    | [ StartExport target ] ->
        match target with
        | SaveToFile _ -> ()
        | _ -> Assert.True(false, "Expected SaveToFile target")
    | _ -> Assert.True(false, "Expected StartExport command")

/// Kiểm tra Selected + Copy tạo command StartExport CopyToClipboard.
[<Fact>]
let ``Selected Copy tạo StartExport CopyToClipboard`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)

    let result = state |> update Copy
    match result.Commands with
    | [ StartExport CopyToClipboard ] -> ()
    | _ -> Assert.True(false, "Expected StartExport CopyToClipboard")

/// Kiểm tra Cancel trong Idle đóng overlay.
[<Fact>]
let ``Idle Cancel đóng overlay`` () =
    let result = initResult() |> update Cancel
    match result.Commands with
    | [ CloseOverlay ] -> ()
    | _ -> Assert.True(false, "Expected CloseOverlay command")

/// Kiểm tra Text tool tạo ShowTextInput command.
[<Fact>]
let ``Chọn TextTool và nhấn trong vùng tạo TextInput`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)
        |> update (SelectTool TextTool)
        |> (fun r -> r.State)

    let result = state |> update (PointerPressed(point 150.0 150.0))
    match result.State.AnnotationInteraction with
    | EditingText(position, _) ->
        Assert.Equal(point 150.0 150.0, position)
    | _ -> Assert.True(false, "Expected EditingText")

    match result.Commands with
    | [ ShowTextInput p ] -> Assert.Equal(point 150.0 150.0, p)
    | _ -> Assert.True(false, "Expected ShowTextInput command")

/// Kiểm tra TextCommitted commit text annotation.
[<Fact>]
let ``TextCommitted commit text annotation`` () =
    let state =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> r.State)
        |> update (SelectTool TextTool)
        |> (fun r -> r.State)
        |> update (PointerPressed(point 150.0 150.0))
        |> (fun r -> r.State)

    let result = state |> update (TextCommitted "Hello")
    Assert.Equal(NoAnnotation, result.State.AnnotationInteraction)
    Assert.Equal(1, List.length result.State.History.Current.Annotations)
    match result.Commands with
    | [ HideTextInput ] -> ()
    | _ -> Assert.True(false, "Expected HideTextInput command")

    match result.State.History.Current.Annotations with
    | [ annotation ] ->
        match annotation.Tool with
        | Text(position, content, _) ->
            Assert.Equal(point 150.0 150.0, position)
            Assert.Equal("Hello", content)
        | _ -> Assert.True(false, "Expected Text annotation")
    | _ -> Assert.True(false, "Expected one annotation")

/// Kiểm tra RenderModel phản ánh đúng trạng thái.
[<Fact>]
let ``RenderModel phản ánh đúng trạng thái Idle và Selected`` () =
    let idleModel = buildRenderModel (initResult())
    Assert.True(idleModel.Selection.IsNone)
    Assert.False(idleModel.ToolbarVisible)
    Assert.Equal(CursorHint.Crosshair, idleModel.Cursor)

    let selectedModel =
        initResult()
        |> update (PointerPressed(point 100.0 100.0))
        |> (fun r -> r.State)
        |> update (PointerMoved(point 300.0 200.0))
        |> (fun r -> r.State)
        |> update PointerReleased
        |> (fun r -> buildRenderModel r.State)

    Assert.True(selectedModel.Selection.IsSome)
    Assert.True(selectedModel.ToolbarVisible)
