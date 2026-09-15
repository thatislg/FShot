module FShot.UI.Tests.ToolbarIconsTests

open Avalonia.Media
open FShot.Core.Domain
open FShot.UI.SkiaCanvas
open Xunit

/// Kiểm tra tất cả tool trong toolbar đều có icon path data không rỗng.
[<Fact>]
let ``Tất cả toolbar tool icons có path data`` () =
    let tools = [
        SelectionTool
        PencilTool
        LineTool
        ArrowTool
        RectangleTool
        CircleTool
        MarkerTool
        TextTool
        PixelateTool
        IconTool
    ]

    tools
    |> List.iter (fun tool ->
        match ToolbarIcons.pathFor tool with
        | Some pathData ->
            Assert.False(System.String.IsNullOrWhiteSpace(pathData),
                sprintf "Icon %A có path data rỗng" tool)
        | None ->
            Assert.True(false, sprintf "Thiếu icon cho tool %A" tool)
    )

/// Kiểm tra mỗi tool có định nghĩa màu fill và stroke.
[<Fact>]
let ``Tất cả toolbar tool có màu fill và stroke`` () =
    let tools = [
        SelectionTool
        PencilTool
        LineTool
        ArrowTool
        RectangleTool
        CircleTool
        MarkerTool
        TextTool
        PixelateTool
        IconTool
    ]

    tools
    |> List.iter (fun tool ->
        let fill = ToolbarIcons.fillColor tool
        let stroke = ToolbarIcons.strokeColor tool
        Assert.NotEqual(fill, stroke)
    )
