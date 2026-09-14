namespace FShot.Core.Geometry

/// Thuật toán làm mịn đường nét tự do.
/// Dùng chung cho Pencil và Marker trong phạm vi MVP.
/// Xem tài liệu 04_03_Pencil.md, mục 4 và 5.
module PathSmoothing =

    /// Loại bỏ các điểm quá gần điểm liền trước đó.
    /// Quy tắc: giữ điểm đầu tiên; với mỗi điểm tiếp theo, nếu khoảng cách
    /// từ điểm đó đến điểm cuối cùng đã được giữ nhỏ hơn minDistance thì bỏ qua,
    /// ngược lại thêm vào danh sách đã giữ.
    /// Công thức khoảng cách: sqrt((x2 - x1)^2 + (y2 - y1)^2).
    let simplifyPoints (minDistance: float) (points: Point list) : Point list =
        if minDistance <= 0.0 then
            points
        else
            let rec loop (kept: Point list) (rest: Point list) : Point list =
                match rest with
                | [] -> List.rev kept
                | p :: tail ->
                    let last = List.head kept
                    if Operations.pointDistance last p < minDistance then
                        loop kept tail
                    else
                        loop (p :: kept) tail

            match points with
            | [] -> []
            | first :: rest -> loop [ first ] rest

    /// Chuyển danh sách điểm đã rà soát thành các đoạn cong bậc hai.
    /// Mỗi đoạn cong gồm ba điểm: điểm bắt đầu, điểm điều khiển, điểm kết thúc.
    /// Với ba điểm gốc P0, P1, P2 liên tiếp:
    ///   - điểm bắt đầu là trung điểm của P0 và P1,
    ///   - điểm điều khiển là P1,
    ///   - điểm kết thúc là trung điểm của P1 và P2.
    /// Với chỉ hai điểm, đoạn cong duy nhất đi từ điểm đầu, điều khiển là trung điểm,
    /// đến điểm cuối.
    /// Với ít hơn hai điểm, không tạo đoạn cong nào.
    let toQuadraticSegments (points: Point list) : (Point * Point * Point) list =
        let midpoint (a: Point) (b: Point) : Point =
            Operations.pointLerp a b 0.5

        match points with
        | []
        | [ _ ] -> []
        | [ a; b ] -> [ (a, midpoint a b, b) ]
        | _ ->
            let rec build (pts: Point list) (acc: (Point * Point * Point) list) : (Point * Point * Point) list =
                match pts with
                | a :: b :: c :: rest ->
                    let m1 = midpoint a b
                    let m2 = midpoint b c
                    build (b :: c :: rest) ((m1, b, m2) :: acc)
                | _ -> List.rev acc

            build points []

    /// Kết hợp giảm dư thừa và tạo đoạn cong bậc hai trong một bước.
    /// Ngưỡng giảm dư thừa được tính từ độ dày nét: max(strokeWidth * 0.25, 0.5)
    /// theo gợi ý trong 04_03_Pencil.md, mục 5.1.
    let toSmoothedSegments (strokeWidth: StrokeWidth) (points: Point list) : (Point * Point * Point) list =
        let minDistance = max (strokeWidth.Value * 0.25) 0.5
        let simplified = simplifyPoints minDistance points
        toQuadraticSegments simplified
