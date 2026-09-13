module FShot.Core.Tests.Domain.HistoryTests

open FShot.Core.Domain
open FShot.Core.Geometry
open Xunit

let private point x y = { X = x; Y = y }

let private defaultStyle =
    {
      Color = Color.Black
      StrokeWidth = StrokeWidth.Create 2.0
      FontSize = 14.0
      FontName = None
      FontStyle = []
    }

let private lineAnnotation x1 y1 x2 y2 =
    Annotation.FromPreview(Line(point x1 y1, point x2 y2), defaultStyle)

let private buildSnapshot annotations =
    Snapshot.Create annotations

let private snapshotWithOneLine =
    buildSnapshot [ lineAnnotation 100.0 100.0 200.0 200.0 ]

let private snapshotWithTwoLines =
    buildSnapshot [
        lineAnnotation 100.0 100.0 200.0 200.0
        lineAnnotation 300.0 100.0 400.0 200.0
    ]

/// Kiểm tra HistoryStack rỗng có snapshot ban đầu.
[<Fact>]
let ``HistoryStack rỗng chứa snapshot ban đầu`` () =
    let history = HistoryStack.Empty HistoryStack.DefaultLimit
    Assert.Equal(1, List.length history.UndoStack)
    Assert.Empty(history.Current.Annotations)
    Assert.False(history.CanUndo)
    Assert.False(history.CanRedo)

/// Kiểm tra Push tăng undo stack và xóa redo stack.
[<Fact>]
let ``Push thêm snapshot và xóa redo stack`` () =
    let history =
        HistoryStack.Empty HistoryStack.DefaultLimit
        |> (fun h -> h.Push snapshotWithOneLine)

    Assert.Equal(2, List.length history.UndoStack)
    Assert.Equal(1, List.length history.Current.Annotations)
    Assert.True(history.CanUndo)
    Assert.False(history.CanRedo)

/// Kiểm tra Undo chuyển snapshot sang redo stack.
[<Fact>]
let ``Undo hoàn tác về trạng thái trước`` () =
    let history =
        HistoryStack.Empty HistoryStack.DefaultLimit
        |> (fun h -> h.Push snapshotWithOneLine)
        |> (fun h -> h.Push snapshotWithTwoLines)

    let afterUndo, current = history.Undo()
    Assert.Equal(2, List.length afterUndo.UndoStack)
    Assert.Equal(1, List.length current.Annotations)
    Assert.Equal(1, List.length afterUndo.RedoStack)
    Assert.True(afterUndo.CanUndo)
    Assert.True(afterUndo.CanRedo)

/// Kiểm tra Redo khôi phục snapshot đã undo.
[<Fact>]
let ``Redo khôi phục snapshot đã hoàn tác`` () =
    let history =
        HistoryStack.Empty HistoryStack.DefaultLimit
        |> (fun h -> h.Push snapshotWithOneLine)
        |> (fun h -> h.Push snapshotWithTwoLines)

    let afterUndo, _ = history.Undo()
    let afterRedo, current = afterUndo.Redo()
    Assert.Equal(3, List.length afterRedo.UndoStack)
    Assert.Equal(2, List.length current.Annotations)
    Assert.Empty(afterRedo.RedoStack)
    Assert.True(afterRedo.CanUndo)
    Assert.False(afterRedo.CanRedo)

/// Kiểm tra thao tác mới sau undo xóa redo stack.
[<Fact>]
let ``Thao tác mới sau undo xóa redo stack`` () =
    let history =
        HistoryStack.Empty HistoryStack.DefaultLimit
        |> (fun h -> h.Push snapshotWithOneLine)
        |> (fun h -> h.Push snapshotWithTwoLines)

    let afterUndo, _ = history.Undo()
    let anotherLine =
        buildSnapshot [ lineAnnotation 500.0 500.0 600.0 600.0 ]

    let afterPush = afterUndo.Push anotherLine
    Assert.Equal(3, List.length afterPush.UndoStack)
    Assert.Equal(1, List.length afterPush.Current.Annotations)
    Assert.Empty(afterPush.RedoStack)
    Assert.False(afterPush.CanRedo)

/// Kiểm tra cắt undo stack khi vượt giới hạn.
[<Fact>]
let ``Push vượt giới hạn cắt snapshot cũ nhất`` () =
    let history = HistoryStack.Empty 3
    let s1 = buildSnapshot [ lineAnnotation 0.0 0.0 1.0 1.0 ]
    let s2 = buildSnapshot [ lineAnnotation 0.0 0.0 2.0 2.0 ]
    let s3 = buildSnapshot [ lineAnnotation 0.0 0.0 3.0 3.0 ]
    let s4 = buildSnapshot [ lineAnnotation 0.0 0.0 4.0 4.0 ]

    let h1 = (HistoryStack.Empty 3).Push s1
    Assert.Equal(2, List.length h1.UndoStack)

    let h2 = h1.Push s2
    Assert.Equal(3, List.length h2.UndoStack)

    // Lúc này stack đã đạt limit 3: [Empty, s1, s2].
    // Push s3 phải cắt bỏ Empty.
    let h3 = h2.Push s3
    Assert.Equal(3, List.length h3.UndoStack)
    Assert.Equal(1, List.length h3.Current.Annotations)
    Assert.True(h3.CanUndo)

    let afterUndoH3, currentH3 = h3.Undo()
    Assert.Equal(2, List.length afterUndoH3.UndoStack)
    Assert.Equal(1, List.length currentH3.Annotations)

    // Push s4 phải cắt bỏ s1.
    let h4 = h3.Push s4
    Assert.Equal(3, List.length h4.UndoStack)
    Assert.Equal(1, List.length h4.Current.Annotations)
    Assert.True(h4.CanUndo)

    let afterUndo, _ = h4.Undo()
    Assert.Equal(2, List.length afterUndo.UndoStack)
    Assert.Equal(1, List.length afterUndo.Current.Annotations)

/// Kiểm tra SetLimit không cắt ngay stack hiện tại.
[<Fact>]
let ``SetLimit chỉ cắt ở lần Push tiếp theo`` () =
    let s1 = buildSnapshot [ lineAnnotation 0.0 0.0 1.0 1.0 ]
    let s2 = buildSnapshot [ lineAnnotation 0.0 0.0 2.0 2.0 ]
    let s3 = buildSnapshot [ lineAnnotation 0.0 0.0 3.0 3.0 ]

    let history =
        (HistoryStack.Empty 5)
            .Push(s1)
            .Push(s2)
            .Push(s3)

    let smaller = history.SetLimit 2
    // SetLimit không cắt ngay stack hiện tại.
    Assert.Equal(4, List.length smaller.UndoStack)
    Assert.Equal(2, smaller.Limit)

    let s4 = buildSnapshot [ lineAnnotation 0.0 0.0 4.0 4.0 ]
    let afterPush = smaller.Push s4
    // UndoStack: [Empty, s1, s2, s3, s4] -> limit 2 -> [s3, s4]
    Assert.Equal(2, List.length afterPush.UndoStack)
    Assert.Equal(1, List.length afterPush.Current.Annotations)

/// Kiểm tra Undo không thực hiện khi chỉ có snapshot ban đầu.
[<Fact>]
let ``Undo không hoạt động khi chỉ có snapshot ban đầu`` () =
    let history = HistoryStack.Empty HistoryStack.DefaultLimit
    let afterUndo, current = history.Undo()
    Assert.True(List.forall2 (fun a b -> a.Annotations.Length = b.Annotations.Length) history.UndoStack afterUndo.UndoStack)
    Assert.Empty(current.Annotations)
    Assert.False(afterUndo.CanUndo)
    Assert.False(afterUndo.CanRedo)

/// Kiểm tra Redo không thực hiện khi redo stack rỗng.
[<Fact>]
let ``Redo không hoạt động khi redo stack rỗng`` () =
    let history =
        (HistoryStack.Empty HistoryStack.DefaultLimit)
            .Push(snapshotWithOneLine)

    let afterRedo, current = history.Redo()
    Assert.True(List.forall2 (fun a b -> a.Annotations.Length = b.Annotations.Length) history.UndoStack afterRedo.UndoStack)
    Assert.Equal(1, List.length current.Annotations)
    Assert.False(afterRedo.CanRedo)
