// Regression tests cho CaptureCanvas.
// Đây là smoke-test cấp UI để phát hiện degradation nghiêm trọng trong luồng headless:
// - Khởi tạo Avalonia headless session.
// - Tạo Window chứa CaptureCanvas.
// - Set capture result và overlay state.
// - Kiểm tra RenderModel sinh ra từ state có Selection, ToolbarVisible và Annotations.
module FShot.UI.Tests.CaptureCanvasRegressionTests

open System
open System.Threading
open Avalonia
open Avalonia.Controls
open Avalonia.Headless
open Avalonia.Media.Imaging
open Avalonia.Threading
open FShot.Core.Domain
open FShot.Core.Geometry
open FShot.Core.State
open FShot.Core.State.OverlayStateLogic
open FShot.UI.SkiaCanvas
open Xunit

/// Stub Application dùng để khởi tạo Avalonia headless session.
type private HeadlessApp() =
    inherit Application()

    static member BuildAvaloniaApp() : AppBuilder =
        AppBuilder.Configure<HeadlessApp>().UseSkia()

module private HeadlessPlatform =
    let session = lazy (
        HeadlessUnitTestSession.StartNew(typeof<HeadlessApp>, AvaloniaTestIsolationLevel.PerAssembly)
    )

let private makeCaptureResult (width: int) (height: int) (scale: float) : CaptureResult =
    let stride = width * 4
    {
      Pixels = Array.zeroCreate (stride * height)
      Width = width
      Height = height
      Stride = stride
      PixelFormat = PixelFormat.Bgra32
      VirtualBounds = { X = 0.0; Y = 0.0; Width = float width; Height = float height }
      ScaleFactor = ScaleFactor.Create scale
      ScreenIndex = 0
    }

let private makeOverlayState (bounds: Rect) (annotations: Annotation list) : OverlayState =
    let capture = makeCaptureResult 400 300 1.0
    let config = ConfigSnapshot.Default
    let state = init capture config
    let selection = { Selection.Empty with State = SelectionState.Selected; Bounds = bounds }
    { state with
        Selection = selection
        History = state.History.Push { Annotations = annotations }
        CurrentTool = LineTool }

let private runSmokeTest () : bool * string =
    let selectionBounds = { X = 50.0; Y = 100.0; Width = 400.0; Height = 60.0 }
    let annotation =
        Annotation.FromPreview(
            Tool.Line({ X = 60.0; Y = 110.0 }, { X = 260.0; Y = 150.0 }),
            { Color = Color.Red
              StrokeWidth = StrokeWidth.Create 3.0
              FontSize = 14.0
              FontName = None
              FontStyle = [] }
        )

    let capture = makeCaptureResult 400 300 1.0
    let state = makeOverlayState selectionBounds [ annotation ]
    let canvas = CaptureCanvas()

    let window = Window()
    window.Content <- canvas
    window.Width <- 400.0
    window.Height <- 300.0
    window.Show()

    canvas.SetCaptureResult(capture)
    canvas.SetOverlayState(state)

    // Đợi Dispatcher xử lý InvalidateVisual đã post và render timer.
    Dispatcher.UIThread.RunJobs(DispatcherPriority.Normal)
    AvaloniaHeadlessPlatform.ForceRenderTimerTick(1)
    Dispatcher.UIThread.RunJobs(DispatcherPriority.Render)

    // Kiểm tra RenderModel từ state có đầy đủ thông tin để render selection + toolbar + annotations.
    let renderModel = buildRenderModel state
    let hasSelection = renderModel.Selection.IsSome
    let hasToolbarVisible = renderModel.ToolbarVisible
    let hasAnnotations = not (List.isEmpty renderModel.Annotations)

    let diag =
        "HasSelection=" + string hasSelection
        + " ToolbarVisible=" + string hasToolbarVisible
        + " Annotations=" + string (List.length renderModel.Annotations)
    hasSelection && hasToolbarVisible && hasAnnotations, diag

[<Fact>]
let ``CaptureCanvas headless smoke test tạo RenderModel đầy đủ`` () =
    let task = HeadlessPlatform.session.Value.Dispatch(runSmokeTest, CancellationToken.None)
    let result, diag = task.GetAwaiter().GetResult()
    Assert.True(
        result,
        "Smoke test UI headless thất bại: " + diag
    )
