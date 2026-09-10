module FShot.Core.Tests.Geometry.TypesTests

open FShot.Core.Geometry
open FShot.Core.Geometry.Operations
open Xunit

/// Tạo một điểm từ hai tọa độ.
let private point x y =
    {
      X = x
      Y = y
    }

/// Tạo một vector từ hai thành phần.
let private vector dx dy =
    {
      Dx = dx
      Dy = dy
    }

/// Tạo một hình chữ nhật từ gốc và kích thước.
let private rect x y width height =
    {
      X = x
      Y = y
      Width = width
      Height = height
    }

/// Kiểm tra các phép toán trên Point.
/// Công thức: cộng theo từng thành phần. Xem 01_02_Point.md, mục 4.1.
[<Fact>]
let ``Cộng hai điểm cho ra điểm đúng`` () =
    let a = point 100.0 200.0
    let b = point 150.0 220.0
    let result = pointAdd a b
    Assert.Equal(point 250.0 420.0, result)

/// Kiểm tra hiệu hai điểm cho ra vector dịch chuyển.
/// Công thức: (X2 - X1, Y2 - Y1). Xem 01_02_Point.md, mục 4.1.
[<Fact>]
let ``Trừ hai điểm cho ra vector dịch chuyển đúng`` () =
    let a = point 100.0 200.0
    let b = point 150.0 220.0
    let result = pointSubtract b a
    Assert.Equal(vector 50.0 20.0, result)

/// Kiểm tra nhân điểm với hệ số scale.
/// Công thức: (X * k, Y * k). Xem 01_02_Point.md, mục 4.2.
[<Fact>]
let ``Nhân điểm với hệ số scale giữ tỉ lệ`` () =
    let p = point 100.0 200.0
    let result = pointScale 1.5 p
    Assert.Equal(point 150.0 300.0, result)

/// Kiểm tra khoảng cách giữa hai điểm.
/// Công thức: sqrt((X2 - X1)^2 + (Y2 - Y1)^2). Xem 01_02_Point.md, mục 4.3.
[<Fact>]
let ``Khoảng cách giữa hai điểm ngang nhau bằng 300`` () =
    let a = point 100.0 100.0
    let b = point 400.0 100.0
    let result = pointDistance a b
    Assert.Equal(300.0, result)

/// Kiểm tra khoảng cách chéo giữa hai điểm.
/// Kết quả đúng là sqrt(300^2 + 300^2) ≈ 424.26.
[<Fact>]
let ``Khoảng cách chéo giữa hai điểm đúng`` () =
    let a = point 100.0 100.0
    let b = point 400.0 400.0
    let result = pointDistance a b
    Assert.Equal(424.2640687119285, result, 10)

/// Kiểm tra nội suy giữa hai điểm với t = 0.5.
/// Công thức: A + (B - A) * 0.5. Xem 01_02_Point.md, mục 4.4.
[<Fact>]
let ``Nội suy giữa hai điểm với tỉ lệ 0.5 cho điểm chính giữa`` () =
    let a = point 100.0 100.0
    let b = point 300.0 200.0
    let result = pointLerp a b 0.5
    Assert.Equal(point 200.0 150.0, result)

/// Kiểm tra so sánh hai điểm gần nhau với ngưỡng sai số.
/// Xem 01_02_Point.md, mục 4.5.
[<Fact>]
let ``So sánh hai điểm gần nhau với ngưỡng sai số`` () =
    let a = point 100.0 200.0
    let b = point 100.1 200.1
    Assert.True(pointApproxEqual 1.0 a b)
    Assert.False(pointApproxEqual 0.05 a b)

/// Kiểm tra tạo Rect từ hai điểm đối diện.
/// Công thức: X = min(X1, X2), Y = min(Y1, Y2), Width = |X2 - X1|, Height = |Y2 - Y1|.
[<Fact>]
let ``Tạo Rect từ hai điểm đối diện đúng`` () =
    let a = point 100.0 80.0
    let b = point 300.0 230.0
    let result = rectFromPoints a b
    Assert.Equal(100.0, result.X)
    Assert.Equal(80.0, result.Y)
    Assert.Equal(200.0, result.Width)
    Assert.Equal(150.0, result.Height)

/// Kiểm tra điểm nằm trong Rect.
/// Công thức: x >= L && x <= R && y >= T && y <= B. Xem 01_03_Rect.md, mục 3.1.
[<Fact>]
let ``Kiểm tra điểm nằm trong Rect`` () =
    let r = rect 100.0 80.0 200.0 150.0
    Assert.True(rectContainsPoint r (point 150.0 150.0))
    Assert.False(rectContainsPoint r (point 50.0 150.0))
    Assert.True(rectContainsPoint r (point 300.0 230.0))

/// Kiểm tra giao nhau giữa hai Rect.
/// Công thức: L1 < R2 && R1 > L2 && T1 < B2 && B1 > T2.
/// Xem 01_03_Rect.md, mục 3.2.
[<Fact>]
let ``Hai Rect giao nhau`` () =
    let a = rect 100.0 100.0 200.0 150.0
    let b = rect 250.0 200.0 200.0 150.0
    Assert.True(rectIntersects a b)

[<Fact>]
let ``Hai Rect không giao nhau`` () =
    let a = rect 100.0 100.0 200.0 150.0
    let b = rect 350.0 300.0 100.0 100.0
    Assert.False(rectIntersects a b)

/// Kiểm tra hợp nhất hai Rect.
/// Công thức: L = min(L1, L2), T = min(T1, T2), R = max(R1, R2), B = max(B1, B2).
/// Xem 01_03_Rect.md, mục 3.3.
[<Fact>]
let ``Hợp nhất hai Rect cho ra bounding box đúng`` () =
    let a = rect 100.0 100.0 200.0 150.0
    let b = rect 250.0 50.0 200.0 300.0
    let result = rectUnion a b
    Assert.Equal(100.0, result.X)
    Assert.Equal(50.0, result.Y)
    Assert.Equal(350.0, result.Width)
    Assert.Equal(300.0, result.Height)

/// Kiểm tra phóng to Rect.
/// Công thức: X' = X - d, Y' = Y - d, Width' = Width + 2d, Height' = Height + 2d.
/// Xem 01_03_Rect.md, mục 3.4.
[<Fact>]
let ``Phóng to Rect đều 4 cạnh`` () =
    let r = rect 100.0 80.0 200.0 150.0
    let result = rectInflate 10.0 r
    Assert.Equal(90.0, result.X)
    Assert.Equal(70.0, result.Y)
    Assert.Equal(220.0, result.Width)
    Assert.Equal(170.0, result.Height)

/// Kiểm tra giới hạn Rect trong vùng lớn hơn.
/// Công thức: X' = max(L, min(X, R - Width)), Y' = max(T, min(Y, B - Height)).
/// Xem 01_03_Rect.md, mục 3.5.
[<Fact>]
let ``Giới hạn Rect trong vùng lớn hơn`` () =
    let r = rect 1800.0 900.0 300.0 200.0
    let bounds = rect 0.0 0.0 1920.0 1080.0
    let result = rectClamp bounds r
    Assert.Equal(1620.0, result.X)
    Assert.Equal(880.0, result.Y)
    Assert.Equal(300.0, result.Width)
    Assert.Equal(200.0, result.Height)

/// Kiểm tra di chuyển Rect theo vector.
/// Công thức: X' = X + dx, Y' = Y + dy, kích thước giữ nguyên.
/// Xem 01_03_Rect.md, mục 3.7.
[<Fact>]
let ``Di chuyển Rect theo vector`` () =
    let r = rect 100.0 80.0 200.0 150.0
    let v = vector 30.0 -20.0
    let result = rectTranslate v r
    Assert.Equal(130.0, result.X)
    Assert.Equal(60.0, result.Y)
    Assert.Equal(200.0, result.Width)
    Assert.Equal(150.0, result.Height)

/// Kiểm tra chuyển đổi màu sang chuỗi hex.
/// Công thức: ghép R, G, B, A thành 2 chữ số thập lục phân.
/// Xem 01_04_Color.md, mục 3.1.
[<Fact>]
let ``Chuyển Color sang chuỗi hex`` () =
    let color =
        {
          R = 255uy
          G = 87uy
          B = 51uy
          A = 255uy
        }
    Assert.Equal("#FF5733FF", color.ToHex())

/// Kiểm tra pha trộn màu theo alpha.
/// Công thức: Ao = As + Ad * (1 - As); Ro = (S * As + D * Ad * (1 - As)) / Ao.
/// Xem 01_04_Color.md, mục 3.2.
[<Fact>]
let ``Pha trộn màu đỏ nửa trong suốt lên nền xám`` () =
    let source =
        {
          R = 255uy
          G = 0uy
          B = 0uy
          A = 128uy
        }
    let destination =
        {
          R = 128uy
          G = 128uy
          B = 128uy
          A = 255uy
        }
    let result = colorBlend source destination
    // Red ≈ 192, green ≈ 64, blue ≈ 64, alpha = 255
    Assert.Equal(255uy, result.A)
    Assert.True(result.R >= 190uy && result.R <= 195uy)
    Assert.True(result.G >= 62uy && result.G <= 66uy)
    Assert.True(result.B >= 62uy && result.B <= 66uy)

/// Kiểm tra độ sáng của màu đen và trắng.
/// Công thức: L = (0.299 * R + 0.587 * G + 0.114 * B) / 255.
/// Xem 01_04_Color.md, mục 3.4.
[<Fact>]
let ``Độ sáng của màu đen và trắng`` () =
    Assert.Equal(0.0, Color.Black.Luminance(), 10)
    Assert.Equal(1.0, Color.White.Luminance(), 10)

/// Kiểm tra chuyển đổi từ chuỗi hex sang Color.
/// Xem 01_04_Color.md, mục 3.1.
[<Fact>]
let ``Chuyển chuỗi hex sang Color`` () =
    let color = colorFromHex "#80A0C0FF"
    Assert.True(color.IsSome)
    Assert.Equal(128uy, color.Value.R)
    Assert.Equal(160uy, color.Value.G)
    Assert.Equal(192uy, color.Value.B)
    Assert.Equal(255uy, color.Value.A)

/// Kiểm tra StrokeWidth bị giới hạn trong khoảng cho phép.
/// Công thức: value' = max(MinValue, min(value, MaxValue)).
/// Xem 01_05_StrokeWidth.md, mục 3.1 và 3.2.
[<Fact>]
let ``StrokeWidth bị giới hạn trong khoảng`` () =
    Assert.Equal(1.0, (StrokeWidth.Create 0.0).Value)
    Assert.Equal(50.0, (StrokeWidth.Create 80.0).Value)
    Assert.Equal(5.0, (StrokeWidth.Create 5.0).Value)

/// Kiểm tra tăng/giảm độ dày nét theo bước.
/// Xem 01_05_StrokeWidth.md, mục 3.3.
[<Fact>]
let ``Tăng giảm StrokeWidth theo bước`` () =
    let current = StrokeWidth.Create 4.0
    let increased = strokeWidthIncrease current
    Assert.Equal(5.0, increased.Value)

    let decreased = strokeWidthDecrease increased
    Assert.Equal(3.0, decreased.Value)

/// Kiểm tra chuyển đổi Point từ logical sang physical.
/// Công thức: physical = logical * scaleFactor.
/// Xem 01_07_DpiAndScaling.md, mục 2.3.
[<Fact>]
let ``Chuyển Point logical sang physical`` () =
    let scale = ScaleFactor.Create 1.5
    let p = point 100.0 200.0
    let result = logicalToPhysicalPoint scale p
    Assert.Equal(point 150.0 300.0, result)

/// Kiểm tra chuyển đổi Rect từ logical sang physical.
/// Công thức: round(L * s), round(T * s), round(R * s), round(B * s).
/// Xem 01_07_DpiAndScaling.md, mục 4.4.
[<Fact>]
let ``Chuyển Rect logical sang physical`` () =
    let scale = ScaleFactor.Create 1.5
    let r = rect 100.0 80.0 200.0 150.0
    let result = logicalToPhysicalRect scale r
    Assert.Equal(150.0, result.X)
    Assert.Equal(120.0, result.Y)
    Assert.Equal(300.0, result.Width)
    Assert.Equal(225.0, result.Height)

/// Kiểm tra khoảng cách từ điểm đến đoạn thẳng.
/// Xem 01_06_HitTesting.md, mục 3.2.
[<Fact>]
let ``Khoảng cách từ điểm đến đoạn thẳng`` () =
    let a = point 100.0 100.0
    let b = point 400.0 100.0
    let p = point 250.0 110.0
    let result = distanceToSegment p a b
    Assert.Equal(10.0, result, 10)

/// Kiểm tra hit-test điểm neo.
/// Công thức: mở rộng handle đều ra tolerance, sau đó kiểm tra point-in-rect.
/// Xem 01_06_HitTesting.md, mục 3.4.
[<Fact>]
let ``Hit-test điểm neo`` () =
    let handle = rect 200.0 200.0 10.0 10.0
    let p = point 215.0 215.0
    Assert.True(hitHandle 6.0 p handle)
    Assert.False(hitHandle 2.0 p handle)
