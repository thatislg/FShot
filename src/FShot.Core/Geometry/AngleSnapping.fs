namespace FShot.Core.Geometry

open System

/// Các giải thuật ràng buộc hình học khi giữ phím Ctrl.
/// Xem tài liệu 04_10_ConstraintsAndSizing.md.
module AngleSnapping =

    /// Ngưỡng khoảng cách tối thiểu để áp dụng ràng buộc góc (tránh chia cho 0 / rung lắc).
    let private minDistance = 2.0

    /// Ràng buộc góc 45° cho điểm kết thúc khi vẽ Line/Arrow (FR-ANN-11).
    /// Làm tròn góc theta = atan2(dy, dx) tới bội số gần nhất của pi/4 (45°),
    /// giữ nguyên độ dài vector L rồi tính lại tọa độ đích.
    /// Xem 04_10_ConstraintsAndSizing.md, mục 2.2.
    let snapAngle45 (start: Point) (current: Point) : Point =
        let dx = current.X - start.X
        let dy = current.Y - start.Y
        let length = Math.Sqrt(dx * dx + dy * dy)

        if length < minDistance then
            current
        else
            let theta = Math.Atan2(dy, dx)
            let snapStep = Math.PI / 4.0
            let snapped = Math.Round(theta / snapStep) * snapStep
            {
              X = start.X + length * Math.Cos(snapped)
              Y = start.Y + length * Math.Sin(snapped)
            }

    /// Ràng buộc hình vuông / hình tròn 1:1 (FR-ANN-12).
    /// side = max(|dx|, |dy|), bảo toàn hướng kéo theo 4 góc phần tư.
    /// Xem 04_10_ConstraintsAndSizing.md, mục 3.2.
    let snapSquare (start: Point) (current: Point) : Point =
        let dx = current.X - start.X
        let dy = current.Y - start.Y
        let side = max (abs dx) (abs dy)
        let sign v = if v < 0.0 then -1.0 else 1.0
        {
          X = start.X + sign dx * side
          Y = start.Y + sign dy * side
        }
