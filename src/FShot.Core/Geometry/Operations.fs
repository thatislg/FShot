namespace FShot.Core.Geometry

open System

/// Các phép toán hình học dùng chung cho Point, Vector, Rect, Color, StrokeWidth.
/// Các công thức trong module này đều dựa trên tài liệu thiết kế Geometry.
module Operations =

    // --------------------------------------------------------
    // Point operations
    // --------------------------------------------------------

    /// Cộng hai điểm theo từng thành phần.
    /// Công thức: (X1 + X2, Y1 + Y2).
    /// Xem tài liệu 01_02_Point.md, mục 4.1.
    let pointAdd (a: Point) (b: Point) : Point =
        {
          X = a.X + b.X
          Y = a.Y + b.Y
        }

    /// Trừ hai điểm, cho ra vector dịch chuyển.
    /// Công thức: (X2 - X1, Y2 - Y1).
    /// Xem tài liệu 01_02_Point.md, mục 4.1.
    let pointSubtract (a: Point) (b: Point) : Vector =
        {
          Dx = a.X - b.X
          Dy = a.Y - b.Y
        }

    /// Nhân điểm với một hệ số.
    /// Công thức: (X * k, Y * k).
    /// Dùng khi chuyển đổi logical → physical pixels. Xem 01_02_Point.md, mục 4.2.
    let pointScale (k: float) (p: Point) : Point =
        {
          X = p.X * k
          Y = p.Y * k
        }

    /// Chia điểm cho một hệ số.
    /// Công thức: (X / k, Y / k), với k <> 0.
    /// Dùng khi chuyển đổi physical → logical pixels.
    let pointUnscale (k: float) (p: Point) : Point =
        if k = 0.0 then
            invalidArg (nameof k) "Hệ số chia phải khác 0."
        {
          X = p.X / k
          Y = p.Y / k
        }

    /// Tính khoảng cách giữa hai điểm.
    /// Công thức: sqrt((X2 - X1)^2 + (Y2 - Y1)^2).
    /// Xem tài liệu 01_02_Point.md, mục 4.3.
    let pointDistance (a: Point) (b: Point) : float =
        let dx = b.X - a.X
        let dy = b.Y - a.Y
        Math.Sqrt(dx * dx + dy * dy)

    /// Nội suy giữa hai điểm theo tỉ lệ t.
    /// Công thức: A + (B - A) * t, hay ((1 - t) * A + t * B).
    /// Khi t = 0.5 cho điểm chính giữa. Xem 01_02_Point.md, mục 4.4.
    let pointLerp (a: Point) (b: Point) (t: float) : Point =
        {
          X = a.X + (b.X - a.X) * t
          Y = a.Y + (b.Y - a.Y) * t
        }

    /// So sánh hai điểm có gần nhau không, dùng ngưỡng sai số epsilon.
    /// Kiểm tra |X1 - X2| <= epsilon và |Y1 - Y2| <= epsilon.
    /// Xem tài liệu 01_02_Point.md, mục 4.5.
    let pointApproxEqual (epsilon: float) (a: Point) (b: Point) : bool =
        Math.Abs(a.X - b.X) <= epsilon && Math.Abs(a.Y - b.Y) <= epsilon

    // --------------------------------------------------------
    // Vector operations
    // --------------------------------------------------------

    /// Cộng vector vào điểm để di chuyển điểm.
    /// Công thức: (X + Dx, Y + Dy).
    let pointAddVector (p: Point) (v: Vector) : Point =
        {
          X = p.X + v.Dx
          Y = p.Y + v.Dy
        }

    /// Chuyển vector thành điểm bằng cách cộng vào gốc tọa độ.
    /// Công thức: (Dx, Dy).
    let vectorAsPoint (v: Vector) : Point =
        {
          X = v.Dx
          Y = v.Dy
        }

    // --------------------------------------------------------
    // Rect operations
    // --------------------------------------------------------

    /// Tạo Rect từ gốc (X, Y) và kích thước (Width, Height).
    /// Nếu Width hoặc Height âm, Rect sẽ được chuẩn hóa để kích thước dương
    /// bằng cách dịch chuyển gốc và đổi dấu kích thước.
    /// Xem tài liệu 01_03_Rect.md, mục 5.2.
    let rectCreate (x: float) (y: float) (width: float) (height: float) : Rect =
        let x' = if width < 0.0 then x + width else x
        let y' = if height < 0.0 then y + height else y
        {
          X = x'
          Y = y'
          Width = Math.Abs(width)
          Height = Math.Abs(height)
        }

    /// Tạo Rect từ hai điểm đối diện (thường là điểm bắt đầu và kết thúc khi kéo chuột).
    /// Công thức: X = min(X1, X2), Y = min(Y1, Y2),
    /// Width = |X2 - X1|, Height = |Y2 - Y1|.
    let rectFromPoints (a: Point) (b: Point) : Rect =
        let x = Math.Min(a.X, b.X)
        let y = Math.Min(a.Y, b.Y)
        let width = Math.Abs(b.X - a.X)
        let height = Math.Abs(b.Y - a.Y)
        {
          X = x
          Y = y
          Width = width
          Height = height
        }

    /// Kiểm tra điểm có nằm trong Rect hay không.
    /// Công thức: x >= L && x <= R && y >= T && y <= B.
    /// Xem tài liệu 01_03_Rect.md, mục 3.1.
    let rectContainsPoint (rect: Rect) (point: Point) : bool =
        let left = rect.Left
        let right = rect.Right
        let top = rect.Top
        let bottom = rect.Bottom
        point.X >= left && point.X <= right
        && point.Y >= top && point.Y <= bottom

    /// Kiểm tra hai Rect có giao nhau hay không.
    /// Công thức: L1 < R2 && R1 > L2 && T1 < B2 && B1 > T2.
    /// Xem tài liệu 01_03_Rect.md, mục 3.2.
    let rectIntersects (a: Rect) (b: Rect) : bool =
        a.Left < b.Right && a.Right > b.Left
        && a.Top < b.Bottom && a.Bottom > b.Top

    /// Tính hình chữ nhật hợp nhất (bounding box) của hai Rect.
    /// Công thức: L = min(L1, L2), T = min(T1, T2),
    /// R = max(R1, R2), B = max(B1, B2).
    /// Xem tài liệu 01_03_Rect.md, mục 3.3.
    let rectUnion (a: Rect) (b: Rect) : Rect =
        let left = Math.Min(a.Left, b.Left)
        let top = Math.Min(a.Top, b.Top)
        let right = Math.Max(a.Right, b.Right)
        let bottom = Math.Max(a.Bottom, b.Bottom)
        {
          X = left
          Y = top
          Width = right - left
          Height = bottom - top
        }

    /// Phóng to hoặc thu nhỏ Rect đều theo khoảng d.
    /// Công thức: X' = X - d, Y' = Y - d,
    /// Width' = Width + 2d, Height' = Height + 2d.
    /// Xem tài liệu 01_03_Rect.md, mục 3.4.
    let rectInflate (d: float) (rect: Rect) : Rect =
        {
          X = rect.X - d
          Y = rect.Y - d
          Width = rect.Width + 2.0 * d
          Height = rect.Height + 2.0 * d
        }

    /// Giới hạn Rect nằm hoàn toàn bên trong một Rect giới hạn.
    /// Công thức:
    ///   X' = max(L, min(X, R - Width))
    ///   Y' = max(T, min(Y, B - Height))
    ///   Width' = min(Width, R - L)
    ///   Height' = min(Height, B - T)
    /// Xem tài liệu 01_03_Rect.md, mục 3.5.
    let rectClamp (bounds: Rect) (rect: Rect) : Rect =
        let x = Math.Max(bounds.Left, Math.Min(rect.X, bounds.Right - rect.Width))
        let y = Math.Max(bounds.Top, Math.Min(rect.Y, bounds.Bottom - rect.Height))
        let width = Math.Min(rect.Width, bounds.Right - bounds.Left)
        let height = Math.Min(rect.Height, bounds.Bottom - bounds.Top)
        {
          X = x
          Y = y
          Width = width
          Height = height
        }

    /// Di chuyển Rect theo vector dịch chuyển.
    /// Công thức: X' = X + dx, Y' = Y + dy, kích thước giữ nguyên.
    /// Xem tài liệu 01_03_Rect.md, mục 3.7.
    let rectTranslate (v: Vector) (rect: Rect) : Rect =
        { rect with
            X = rect.X + v.Dx
            Y = rect.Y + v.Dy }

    /// Tính vùng giao của hai Rect.
    /// Công thức: L = max(L1, L2), T = max(T1, T2),
    /// R = min(R1, R2), B = min(B1, B2).
    /// Nếu không giao nhau, trả về Rect có kích thước 0.
    let rectIntersection (a: Rect) (b: Rect) : Rect =
        let left = Math.Max(a.Left, b.Left)
        let top = Math.Max(a.Top, b.Top)
        let right = Math.Min(a.Right, b.Right)
        let bottom = Math.Min(a.Bottom, b.Bottom)
        let width = Math.Max(0.0, right - left)
        let height = Math.Max(0.0, bottom - top)
        {
          X = left
          Y = top
          Width = width
          Height = height
        }

    // --------------------------------------------------------
    // Color operations
    // --------------------------------------------------------

    /// Pha trộn màu nguồn (source) lên màu đích (destination) theo alpha.
    /// Công thức alpha blending chuẩn:
    ///   Ao = As + Ad * (1 - As)
    ///   Ro = (S * As + D * Ad * (1 - As)) / Ao
    /// Xem tài liệu 01_04_Color.md, mục 3.2.
    let colorBlend (source: Color) (destination: Color) : Color =
        let sourceA = float source.A / 255.0
        let destA = float destination.A / 255.0

        let outA = sourceA + destA * (1.0 - sourceA)
        if outA = 0.0 then
            Color.Transparent
        else
            let blendChannel s d =
                let sF = float s / 255.0
                let dF = float d / 255.0
                let outF = (sF * sourceA + dF * destA * (1.0 - sourceA)) / outA
                byte (Math.Round(outF * 255.0))

            {
              R = blendChannel source.R destination.R
              G = blendChannel source.G destination.G
              B = blendChannel source.B destination.B
              A = byte (Math.Round(outA * 255.0))
            }

    /// Làm sáng hoặc tối màu bằng cách nhân các kênh R, G, B với hệ số.
    /// Công thức: R' = clamp(R * factor, 0, 255), tương tự G, B.
    /// Alpha giữ nguyên. Xem tài liệu 01_04_Color.md, mục 3.3.
    let colorAdjustBrightness (factor: float) (color: Color) : Color =
        let clamp255 (value: float) = byte (Math.Max(0.0, Math.Min(255.0, Math.Round value)))
        { color with
            R = clamp255 (float color.R * factor)
            G = clamp255 (float color.G * factor)
            B = clamp255 (float color.B * factor) }

    /// Chuyển chuỗi hex sang Color.
    /// Hỗ trợ dạng #RRGGBB và #RRGGBBAA.
    /// Công thức: tách chuỗi thành các nhóm 2 ký tự, chuyển từ hex sang byte.
    /// Xem tài liệu 01_04_Color.md, mục 3.1.
    let colorFromHex (hex: string) : Color option =
        let normalized = hex.TrimStart('#')
        let parseByte (s: string) =
            match Byte.TryParse(s, Globalization.NumberStyles.HexNumber, Globalization.CultureInfo.InvariantCulture) with
            | true, value -> Some value
            | _ -> None

        if normalized.Length = 6 then
            let r = parseByte normalized[0..1]
            let g = parseByte normalized[2..3]
            let b = parseByte normalized[4..5]
            match r, g, b with
            | Some r, Some g, Some b ->
                Some
                    {
                      R = r
                      G = g
                      B = b
                      A = 255uy
                    }
            | _ -> None
        elif normalized.Length = 8 then
            let r = parseByte normalized[0..1]
            let g = parseByte normalized[2..3]
            let b = parseByte normalized[4..5]
            let a = parseByte normalized[6..7]
            match r, g, b, a with
            | Some r, Some g, Some b, Some a ->
                Some
                    {
                      R = r
                      G = g
                      B = b
                      A = a
                    }
            | _ -> None
        else
            None

    // --------------------------------------------------------
    // StrokeWidth operations
    // --------------------------------------------------------

    /// Danh sách bước điều chỉnh độ dày nét khi lăn chuột.
    /// Xem tài liệu 01_05_StrokeWidth.md, mục 3.3.
    let strokeWidthSteps = [ 1.0; 2.0; 3.0; 5.0; 8.0; 12.0; 16.0; 20.0; 25.0; 32.0; 40.0; 50.0 ]

    /// Tăng độ dày nét lên bước kế tiếp trong danh sách.
    /// Nếu giá trị hiện tại lớn hơn hoặc bằng bước lớn nhất, giữ nguyên.
    let strokeWidthIncrease (current: StrokeWidth) : StrokeWidth =
        let value = current.Value
        let next =
            strokeWidthSteps
            |> List.tryFind (fun step -> step > value)
            |> Option.defaultValue StrokeWidth.MaxValue
        StrokeWidth.Create next

    /// Giảm độ dày nét xuống bước trước đó trong danh sách.
    /// Nếu giá trị hiện tại nhỏ hơn hoặc bằng bước nhỏ nhất, giữ nguyên.
    let strokeWidthDecrease (current: StrokeWidth) : StrokeWidth =
        let value = current.Value
        let previous =
            strokeWidthSteps
            |> List.rev
            |> List.tryFind (fun step -> step < value)
            |> Option.defaultValue StrokeWidth.MinValue
        StrokeWidth.Create previous

    // --------------------------------------------------------
    // Scale / DPI operations
    // --------------------------------------------------------

    /// Chuyển điểm từ logical pixels sang physical pixels.
    /// Công thức: physical = logical * scaleFactor.
    /// Xem tài liệu 01_07_DpiAndScaling.md, mục 2.3.
    let logicalToPhysicalPoint (scale: ScaleFactor) (p: Point) : Point =
        {
          X = p.X * scale.Value
          Y = p.Y * scale.Value
        }

    /// Chuyển điểm từ physical pixels sang logical pixels.
    /// Công thức: logical = physical / scaleFactor.
    let physicalToLogicalPoint (scale: ScaleFactor) (p: Point) : Point =
        if scale.Value = 0.0 then
            invalidArg (nameof scale) "Hệ số phóng to phải khác 0."
        {
          X = p.X / scale.Value
          Y = p.Y / scale.Value
        }

    /// Chuyển Rect từ logical sang physical, làm tròn từng cạnh.
    /// Công thức: round(L * s), round(T * s), round(R * s), round(B * s).
    /// Xem tài liệu 01_07_DpiAndScaling.md, mục 4.4.
    let logicalToPhysicalRect (scale: ScaleFactor) (rect: Rect) : Rect =
        let left = Math.Round(rect.Left * scale.Value)
        let top = Math.Round(rect.Top * scale.Value)
        let right = Math.Round(rect.Right * scale.Value)
        let bottom = Math.Round(rect.Bottom * scale.Value)
        {
          X = left
          Y = top
          Width = right - left
          Height = bottom - top
        }

    /// Chuyển Rect từ physical sang logical.
    /// Công thức: L / s, T / s, R / s, B / s.
    let physicalToLogicalRect (scale: ScaleFactor) (rect: Rect) : Rect =
        if scale.Value = 0.0 then
            invalidArg (nameof scale) "Hệ số phóng to phải khác 0."
        let left = rect.Left / scale.Value
        let top = rect.Top / scale.Value
        let right = rect.Right / scale.Value
        let bottom = rect.Bottom / scale.Value
        {
          X = left
          Y = top
          Width = right - left
          Height = bottom - top
        }

    // --------------------------------------------------------
    // Hit-testing helpers
    // --------------------------------------------------------

    /// Tính khoảng cách từ điểm P đến đoạn thẳng AB.
    /// Công thức: nếu hình chiếu của P lên đường thẳng AB nằm trong đoạn AB,
    /// dùng khoảng cách vuông góc; nếu không, dùng khoảng cách đến đầu mút gần nhất.
    /// Xem tài liệu 01_06_HitTesting.md, mục 3.2.
    let distanceToSegment (p: Point) (a: Point) (b: Point) : float =
        let dx = b.X - a.X
        let dy = b.Y - a.Y
        let lenSquared = dx * dx + dy * dy

        if lenSquared = 0.0 then
            // A và B trùng nhau, khoảng cách là khoảng cách đến A.
            pointDistance p a
        else
            // Tính tham số t = ((P - A) · (B - A)) / |B - A|^2
            // t nằm trong [0, 1] nếu hình chiếu nằm trên đoạn AB.
            let t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSquared
            let tClamped = Math.Max(0.0, Math.Min(1.0, t))

            // Điểm hình chiếu: A + t * (B - A)
            let projection =
                {
                  X = a.X + tClamped * dx
                  Y = a.Y + tClamped * dy
                }

            pointDistance p projection

    /// Kiểm tra điểm có gần đoạn thẳng AB trong bán kính dung sai hay không.
    /// Công thức: distanceToSegment(P, A, B) <= tolerance.
    let hitSegment (tolerance: float) (p: Point) (a: Point) (b: Point) : bool =
        distanceToSegment p a b <= tolerance

    /// Kiểm tra điểm có nằm trong điểm neo (handle) đã được mở rộng bán kính dung sai.
    /// Công thức: mở rộng handle đều ra tolerance theo 4 hướng,
    /// sau đó kiểm tra point-in-rect như 01_03_Rect.md mục 3.1.
    /// Xem tài liệu 01_06_HitTesting.md, mục 3.4.
    let hitHandle (tolerance: float) (p: Point) (handle: Rect) : bool =
        let inflated = rectInflate tolerance handle
        rectContainsPoint inflated p
