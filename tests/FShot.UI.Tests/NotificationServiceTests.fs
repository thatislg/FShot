module FShot.UI.Tests.NotificationServiceTests

open System
open System.IO
open FShot.Platform.Win32.Notifications
open Xunit

/// Kiểm tra nội dung thông báo được xây dựng đúng theo từng loại.
/// Test này không cần gọi Win32 API thực; nó kiểm tra logic F#.
[<Fact>]
let ``buildContent tao noi dung thong bao dung`` () =
    let service = NotificationService()

    // Dùng reflection để gọi hàm private buildContent vì nó là let binding trong class.
    let buildContent =
        typeof<NotificationService>.GetMethod("buildContent", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Instance)

    let (title1, text1, _) =
        buildContent.Invoke(service, [| CaptureSuccess (Some "C:\\Screenshots\\fshot_2025-01-01.png") |])
        :?> (string * string * uint32)
    Assert.Equal("Đã lưu ảnh chụp", title1)
    Assert.Equal("fshot_2025-01-01.png", text1)

    let (title2, text2, _) =
        buildContent.Invoke(service, [| CaptureSuccess None |])
        :?> (string * string * uint32)
    Assert.Equal("Đã lưu ảnh chụp", title2)
    Assert.Equal("", text2)

    let (title3, text3, _) =
        buildContent.Invoke(service, [| CopySuccess |])
        :?> (string * string * uint32)
    Assert.Equal("Đã sao chép", title3)
    Assert.Equal("Ảnh chụp đã được đưa vào clipboard", text3)

    let (title4, text4, _) =
        buildContent.Invoke(service, [| CaptureAborted |])
        :?> (string * string * uint32)
    Assert.Equal("Đã hủy", title4)
    Assert.Equal("Thao tác chụp màn hình bị hủy", text4)

/// Kiểm tra ShowNotification tôn trọng cờ `enabled` và `force`.
[<Fact>]
let ``ShowNotification ton trong co enabled va force`` () =
    use service = new NotificationService()
    // Khi disabled và không force, không gọi Win32/Toast, trả về false.
    Assert.False(service.ShowNotification(CaptureSuccess None, false))
    // Khi disabled nhưng force=true, vẫn cố gắng hiển thị (trả về true nếu toast/balloon thành công).
    // Điều này chủ yếu đảm bảo code path đi qua mà không throw.
    Assert.True(service.ShowNotification(CaptureSuccess None, false, force = true))

/// Kiểm tra HighlightFileInExplorer trả về false khi file không tồn tại.
[<Fact>]
let ``HighlightFileInExplorer tra ve false khi file khong ton tai`` () =
    let nonExistent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png")
    Assert.False(NotificationService.HighlightFileInExplorer(nonExistent))

/// Kiểm tra Dispose không throw khi chưa thêm icon nào.
[<Fact>]
let ``Dispose khong throw khi chua co icon`` () =
    use service = new NotificationService()
    service.Dispose()
    (service :> IDisposable).Dispose()
    Assert.True(true)
