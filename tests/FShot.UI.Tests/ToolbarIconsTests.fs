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

/// Kiểm tra mỗi tool có định nghĩa màu fill và stroke đồng nhất theo token ToolButton.Default.IconColor (#3D2B1F).
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

    let expectedDefault = Avalonia.Media.Color.FromRgb(0x3Duy, 0x2Buy, 0x1Fuy)
    let expectedActive = Avalonia.Media.Color.FromRgb(0xFFuy, 0xFFuy, 0xFFuy)

    tools
    |> List.iter (fun tool ->
        let fill = ToolbarIcons.fillColor tool
        let stroke = ToolbarIcons.strokeColor tool
        // Fill và stroke phải khác nhau về độ trong suốt để tạo chiều sâu.
        Assert.NotEqual(fill, stroke)
        // Màu nền RGB phải là dark walnut (#3D2B1F) theo design token.
        Assert.Equal(expectedDefault.R, fill.R)
        Assert.Equal(expectedDefault.G, fill.G)
        Assert.Equal(expectedDefault.B, fill.B)
        Assert.Equal(expectedDefault.R, stroke.R)
        Assert.Equal(expectedDefault.G, stroke.G)
        Assert.Equal(expectedDefault.B, stroke.B)
        // Alpha phải đủ để icon nhìn rõ (>= 80%).
        Assert.True(fill.A >= 0xCCuy, sprintf "Fill alpha quá thấp cho %A" tool)
        Assert.True(stroke.A >= 0xCCuy, sprintf "Stroke alpha quá thấp cho %A" tool)
    )

    // Màu icon active phải là trắng đậm.
    Assert.Equal(expectedActive, ToolbarIcons.activeIconColor)
