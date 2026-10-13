namespace FShot.UI.Windows

open System
open System.IO
open System.Runtime.InteropServices
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.Markup.Xaml
open Avalonia.Platform.Storage
open SkiaSharp
open FShot.Core.Domain
open FShot.Platform.Win32.Clipboard
open FShot.Platform.Win32.Config
open FShot.UI.Logging
open FShot.UI.SkiaCanvas

/// Các công thức hình học thuần của PinWindow, tách riêng để kiểm thử đơn vị.
/// Xem tài liệu 11_07_PinWindow.md, mục 3.
module PinMath =

    /// Giới hạn tỉ lệ thu phóng an toàn: 10% – 500% (FR-PIN-03).
    let minScale = 0.1
    let maxScale = 5.0

    /// Giới hạn độ mờ đục: 10% – 100% (FR-PIN-04).
    let minOpacity = 0.1
    let maxOpacity = 1.0

    /// Hệ số thu phóng mỗi nấc lăn chuột (FR-PIN-03).
    let zoomStep = 1.1

    /// Bước điều chỉnh độ mờ đục mỗi nấc (5%) (FR-PIN-04).
    let opacityStep = 0.05

    /// Kẹp tỉ lệ thu phóng trong [0.1, 5.0].
    let clampScale (s: float) : float = max minScale (min maxScale s)

    /// Kẹp độ mờ đục trong [0.1, 1.0].
    let clampOpacity (o: float) : float = max minOpacity (min maxOpacity o)

    /// Tỉ lệ mới sau một nấc lăn chuột (delta > 0 = phóng to).
    let zoom (current: float) (delta: float) : float =
        if delta > 0.0 then clampScale (current * zoomStep) else clampScale (current / zoomStep)

    /// Độ mờ đục mới sau một nấc (delta > 0 = đậm hơn).
    let adjustOpacity (current: float) (delta: float) : float =
        let next = if delta > 0.0 then current + opacityStep else current - opacityStep
        clampOpacity next

    /// Góc xoay mới (bội số 90°) theo chiều kim đồng hồ hoặc ngược lại.
    let rotate (angle: int) (clockwise: bool) : int =
        let step = if clockwise then 90 else -90
        (angle + step + 360) % 360

    /// Góc 90°/270° làm hoán đổi chiều rộng/chiều cao (FR-PIN-05).
    let isVertical (angle: int) : bool = (angle % 180) = 90

    /// Kích thước hiển thị (w, h) từ kích thước gốc, tỉ lệ và góc xoay.
    let displaySize (baseW: float) (baseH: float) (scale: float) (angle: int) : float * float =
        let w = baseW * scale
        let h = baseH * scale
        if isVertical angle then (h, w) else (w, h)

    /// Tính vị trí cửa sổ mới để giữ điểm dưới con trỏ đứng yên khi thu phóng (Zoom toward cursor).
    /// `padding` là khoảng cách đổ bóng giữa mép cửa sổ và ảnh.
    let zoomTowardCursor
        (windowX: float)
        (windowY: float)
        (pointerX: float)
        (pointerY: float)
        (oldW: float)
        (oldH: float)
        (newW: float)
        (newH: float)
        (padding: float) : float * float =
        let fracX = (pointerX - padding) / oldW
        let fracY = (pointerY - padding) / oldH
        let screenX = windowX + pointerX
        let screenY = windowY + pointerY
        let newX = screenX - (fracX * newW + padding)
        let newY = screenY - (fracY * newH + padding)
        (newX, newY)

/// Thao tác bitmap của PinWindow (xoay 90°, chuyển sang Avalonia Bitmap).
module PinBitmap =

    /// Xoay SKBitmap theo bội số 90°. Trả về bitmap mới (người gọi chịu trách nhiệm Dispose).
    let rotate (src: SKBitmap) (angle: int) : SKBitmap =
        let a = ((angle % 360) + 360) % 360
        if a = 0 then
            src.Copy()
        else
            let vertical = (a % 180) = 90
            let nw = if vertical then src.Height else src.Width
            let nh = if vertical then src.Width else src.Height
            let dst = new SKBitmap(nw, nh, SKColorType.Bgra8888, SKAlphaType.Premul)
            use canvas = new SKCanvas(dst)
            canvas.Clear(SKColors.Transparent)
            canvas.Translate(float32 nw / 2.0f, float32 nh / 2.0f)
            canvas.RotateDegrees(float32 a)
            canvas.Translate(-float32 src.Width / 2.0f, -float32 src.Height / 2.0f)
            let sampling = new SKSamplingOptions(SKCubicResampler.Mitchell)
            canvas.DrawBitmap(src, 0.0f, 0.0f, sampling)
            dst

    /// Chuyển SKBitmap sang Avalonia Bitmap (encode PNG để giữ alpha).
    let toAvalonia (src: SKBitmap) : Bitmap =
        use image = SKImage.FromBitmap(src)
        use data = image.Encode(SKEncodedImageFormat.Png, 100)
        use ms = new MemoryStream(data.ToArray())
        new Bitmap(ms)

/// Cửa sổ ghim ảnh nổi (Pin Widget), Topmost không viền.
/// Hỗ trợ kéo, thu phóng, xoay, chỉnh độ mờ và menu ngữ cảnh (FR-PIN-01–FR-PIN-10).
/// Xem tài liệu 11_07_PinWindow.md.
type PinWindow() as this =
    inherit Window()

    /// Danh sách các cửa sổ ghim đang mở (multi-instance), giữ tham chiếu tránh bị GC.
    static let openWindows = ResizeArray<PinWindow>()

    /// Khoảng cách giữa mép cửa sổ và ảnh, tạo chỗ cho đổ bóng. Khớp với Margin trong PinWindow.axaml.
    let shadowPadding = 20.0

    let mutable baseBitmap : SKBitmap = null
    let mutable angle = 0
    let mutable scale = 1.0
    let mutable baseW = 0.0
    let mutable baseH = 0.0
    let mutable displayBitmap : Bitmap = null
    let mutable image : Image = null
    let mutable disposed = false

    do
        this.InitializeComponent()
        this.ConfigureWindow()

    /// Truy cập danh sách cửa sổ ghim đang mở.
    static member OpenWindows = openWindows

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        image <- this.FindControl<Image>("PinImage")

    /// Khởi tạo nội dung ảnh ghim, kích thước và vị trí xuất hiện.
    /// Được gọi sau khi tạo instance; tránh dùng constructor có tham số để tương thích XAML loader.
    member this.Initialize(bitmap: SKBitmap, position: PixelPoint, size: Size) =
        baseBitmap <- bitmap
        baseW <- size.Width
        baseH <- size.Height
        displayBitmap <- PinBitmap.toAvalonia bitmap
        if not (isNull image) then image.Source <- displayBitmap
        this.ApplySize()
        this.Position <- position
        this.AttachHandlers()
        FShotLog.write (sprintf "[PinWindow] initialized at %A base=%.0fx%.0f" position baseW baseH)

    /// Cấu hình cửa sổ borderless topmost trong suốt (FR-PIN-01).
    member private this.ConfigureWindow() =
        this.WindowState <- WindowState.Normal
        this.WindowDecorations <- WindowDecorations.None
        this.Topmost <- true
        this.CanResize <- false
        this.ShowInTaskbar <- false
        this.Focusable <- true
        this.Background <- Media.Brushes.Transparent
        this.TransparencyLevelHint <- [ WindowTransparencyLevel.Transparent ]
        this.Opacity <- 1.0
        this.Cursor <- new Cursor(StandardCursorType.SizeAll)

    /// Cập nhật kích thước cửa sổ theo scale/angle (kèm padding cho đổ bóng).
    member private this.ApplySize() =
        let (w, h) = PinMath.displaySize baseW baseH scale angle
        this.Width <- w + 2.0 * shadowPadding
        this.Height <- h + 2.0 * shadowPadding

    /// Tái tạo Avalonia Bitmap hiển thị theo góc xoay hiện tại.
    member private this.ApplyDisplayBitmap() =
        if not (isNull baseBitmap) then
            use rotated = PinBitmap.rotate baseBitmap angle
            let next = PinBitmap.toAvalonia rotated
            let old = displayBitmap
            displayBitmap <- next
            if not (isNull image) then image.Source <- displayBitmap
            if not (isNull old) then old.Dispose()

    member private this.AttachHandlers() =
        PinWindow.OpenWindows.Add(this)
        this.Closed.Add(fun _ -> this.DisposeResources())

        // Kéo di chuyển cửa sổ; double-click đóng (FR-PIN-02, FR-PIN-08).
        this.PointerPressed.Add(fun e ->
            let pt = e.GetCurrentPoint(this)
            if pt.Properties.IsLeftButtonPressed then
                if e.ClickCount >= 2 then
                    this.Close()
                else
                    this.BeginMoveDrag(e)
        )

        // Click phải mở menu ngữ cảnh (FR-PIN-06..FR-PIN-08).
        this.PointerReleased.Add(fun e ->
            let pt = e.GetCurrentPoint(this)
            if pt.Properties.PointerUpdateKind = PointerUpdateKind.RightButtonReleased then
                this.OpenContextMenu()
        )

        // Lăn chuột thu phóng; giữ Ctrl lăn chuột chỉnh độ mờ đục (FR-PIN-03, FR-PIN-04).
        this.PointerWheelChanged.Add(fun e ->
            let delta = e.Delta.Y
            if e.KeyModifiers.HasFlag(KeyModifiers.Control) then
                this.AdjustOpacity(delta)
            else
                this.Zoom(delta, e.GetPosition(this))
        )

    /// Thu phóng hướng tâm con trỏ chuột.
    member private this.Zoom(delta: float, pointerLocal: Point) =
        let (ow, oh) = PinMath.displaySize baseW baseH scale angle
        let newScale = PinMath.zoom scale delta
        let (nw, nh) = PinMath.displaySize baseW baseH newScale angle
        let wx = float this.Position.X
        let wy = float this.Position.Y
        let (nx, ny) =
            PinMath.zoomTowardCursor wx wy pointerLocal.X pointerLocal.Y ow oh nw nh shadowPadding
        scale <- newScale
        this.ApplySize()
        this.Position <- PixelPoint(int (round nx), int (round ny))

    member private this.AdjustOpacity(delta: float) =
        this.Opacity <- PinMath.adjustOpacity this.Opacity delta

    member private this.ApplyRotation(newAngle: int) =
        angle <- newAngle
        this.ApplyDisplayBitmap()
        this.ApplySize()

    member private this.ResetZoom() =
        scale <- 1.0
        this.ApplySize()

    /// Mở menu ngữ cảnh Kawaii (FR-PIN-06..FR-PIN-08).
    member private this.OpenContextMenu() =
        let menu = ContextMenu()

        let addItem (header: string) (action: unit -> unit) =
            let item = MenuItem()
            item.Header <- header
            item.Click.Add(fun _ -> action())
            menu.Items.Add(item) |> ignore

        addItem "Sao chép ảnh" (fun () -> this.CopyToClipboard())
        addItem "Lưu ảnh ra tệp..." (fun () -> this.SaveToFile())
        menu.Items.Add(Separator()) |> ignore
        addItem "Xoay 90° phải" (fun () -> this.ApplyRotation(PinMath.rotate angle true))
        addItem "Khôi phục 100% zoom" (fun () -> this.ResetZoom())

        let opacityMenu = MenuItem()
        opacityMenu.Header <- "Độ mờ đục"
        for level in [ 1.0; 0.75; 0.5; 0.25 ] do
            let item = MenuItem()
            item.Header <- sprintf "%d%%" (int (level * 100.0))
            item.Click.Add(fun _ -> this.Opacity <- level)
            opacityMenu.Items.Add(item) |> ignore
        menu.Items.Add(opacityMenu) |> ignore

        menu.Items.Add(Separator()) |> ignore
        addItem "Đóng ghim" (fun () -> this.Close())

        menu.Open(this)

    /// Sao chép ảnh ghim (đã xoay) vào clipboard dạng CF_DIB + PNG (FR-PIN-06).
    member this.CopyToClipboard() =
        if not (isNull baseBitmap) then
            use rotated = PinBitmap.rotate baseBitmap angle
            use data = rotated.Encode(SKEncodedImageFormat.Png, 100)
            let pngBytes = data.ToArray()
            let pixelBytes = Array.zeroCreate<byte> (rotated.Width * rotated.Height * 4)
            let ptr = rotated.GetPixels()
            if ptr <> IntPtr.Zero then
                Marshal.Copy(ptr, pixelBytes, 0, pixelBytes.Length)
            let ok = ClipboardService.copyImageToClipboard rotated.Width rotated.Height pngBytes pixelBytes
            ClipboardService.playNotificationSound ()
            if ok then
                CaptureCanvasEvents.TriggerExportCompleted(Copied)
            else
                CaptureCanvasEvents.TriggerExportCompleted(Failed)
            FShotLog.write (sprintf "[PinWindow] Copy to clipboard: %b" ok)

    /// Lưu ảnh ghim ra tệp (FR-PIN-07). Lưu nhanh nếu có SavePathFixed, ngược lại mở hộp thoại.
    member this.SaveToFile() =
        let config = ConfigStore.loadSnapshot()
        match config.SaveOptions.Path with
        | Some p when not (String.IsNullOrWhiteSpace p) ->
            try
                if not (Directory.Exists p) then Directory.CreateDirectory(p) |> ignore
                let fileName = config.SaveOptions.ResolveFileName(DateTime.Now)
                let fullPath = Path.Combine(p, fileName)
                this.WriteToFile fullPath
            with ex ->
                FShotLog.writeEx "[PinWindow] Quick save failed" ex
        | _ ->
            let run =
                async {
                    let topLevel = TopLevel.GetTopLevel(this)
                    if not (isNull topLevel) then
                        let options = FilePickerSaveOptions()
                        options.SuggestedFileName <- "fshot_pin.png"
                        let pngType = FilePickerFileType("PNG Image (*.png)")
                        pngType.Patterns <- ResizeArray [ "*.png" ]
                        let jpgType = FilePickerFileType("JPEG Image (*.jpg; *.jpeg)")
                        jpgType.Patterns <- ResizeArray [ "*.jpg"; "*.jpeg" ]
                        options.FileTypeChoices <- ResizeArray [ pngType; jpgType ]
                        let! file = topLevel.StorageProvider.SaveFilePickerAsync(options) |> Async.AwaitTask
                        if not (isNull file) then
                            let localPath =
                                match file.TryGetLocalPath() with
                                | null
                                | "" -> if file.Path.IsFile then file.Path.LocalPath else file.Path.ToString()
                                | p -> p
                            this.WriteToFile localPath
                }
            Async.StartImmediate run

    member private this.WriteToFile(path: string) =
        if not (isNull baseBitmap) then
            use rotated = PinBitmap.rotate baseBitmap angle
            let format =
                if path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                   || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) then
                    SKEncodedImageFormat.Jpeg
                else
                    SKEncodedImageFormat.Png
            use data = rotated.Encode(format, 90)
            use stream = File.Create(path)
            data.SaveTo(stream)
            stream.Flush()
            CaptureCanvasEvents.TriggerExportCompleted(Saved path)
            FShotLog.write (sprintf "[PinWindow] Saved to %s" path)

    /// Giải phóng tài nguyên unmanaged và hủy tham chiếu instance (FR-PIN-08).
    member private this.DisposeResources() =
        if not disposed then
            disposed <- true
            if not (isNull displayBitmap) then
                try displayBitmap.Dispose() with _ -> ()
            if not (isNull baseBitmap) then
                try baseBitmap.Dispose() with _ -> ()
            PinWindow.OpenWindows.Remove(this) |> ignore
            FShotLog.write "[PinWindow] resources disposed"

    override this.OnKeyDown(e: KeyEventArgs) =
        let ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control)
        let shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
        match e.Key with
        | Key.R ->
            this.ApplyRotation(PinMath.rotate angle (not shift))
            e.Handled <- true
        | Key.Escape ->
            this.Close()
            e.Handled <- true
        | Key.C when ctrl ->
            this.CopyToClipboard()
            e.Handled <- true
        | Key.S when ctrl ->
            this.SaveToFile()
            e.Handled <- true
        | Key.D0 when ctrl ->
            this.ResetZoom()
            e.Handled <- true
        | Key.OemOpenBrackets ->
            this.AdjustOpacity(-1.0)
            e.Handled <- true
        | Key.OemCloseBrackets ->
            this.AdjustOpacity(1.0)
            e.Handled <- true
        | _ ->
            base.OnKeyDown(e)
