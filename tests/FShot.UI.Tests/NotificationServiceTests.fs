module FShot.UI.Tests.NotificationServiceTests

open System
open System.IO
open FShot.UI.Services
open Xunit

/// Kiểm tra nội dung thông báo được xây dựng đúng theo từng loại.
/// Test này không cần mở cửa sổ thực; nó kiểm tra logic F# qua reflection.
[<Fact>]
let ``buildContent tao noi dung thong bao dung`` () =
    let service = new NotificationService()

    // Dùng reflection để gọi hàm private buildContent vì nó là let binding trong class.
    let buildContent =
        typeof<NotificationService>.GetMethod("buildContent", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Instance)

    let (title1, text1, file1) =
        buildContent.Invoke(service, [| CaptureSuccess (Some "C:\\Screenshots\\fshot_2025-01-01.png") |])
        :?> (string * string * string option)
    Assert.Equal("Đã lưu ảnh chụp", title1)
    Assert.Equal("fshot_2025-01-01.png", text1)
    Assert.Equal(Some "C:\\Screenshots\\fshot_2025-01-01.png", file1)

    let (title2, text2, file2) =
        buildContent.Invoke(service, [| CaptureSuccess None |])
        :?> (string * string * string option)
    Assert.Equal("Đã lưu ảnh chụp", title2)
    Assert.Equal("", text2)
    Assert.Equal(None, file2)

    let (title3, text3, file3) =
        buildContent.Invoke(service, [| CopySuccess |])
        :?> (string * string * string option)
    Assert.Equal("Đã sao chép", title3)
    Assert.Equal("Ảnh chụp đã được đưa vào clipboard", text3)
    Assert.Equal(None, file3)

    let (title4, text4, file4) =
        buildContent.Invoke(service, [| CaptureAborted |])
        :?> (string * string * string option)
    Assert.Equal("Đã hủy", title4)
    Assert.Equal("Thao tác chụp màn hình bị hủy", text4)
    Assert.Equal(None, file4)

/// Kiểm tra ShowNotification tôn trọng cờ `enabled` và `force`.
/// Khi disabled và không force, không mở cửa sổ và trả về false.
/// Khi disabled nhưng force=true, code path vẫn chạy qua (có thể fail mở window trong môi trường test headless).
[<Fact>]
let ``ShowNotification ton trong co enabled va force`` () =
    use service = new NotificationService()
    Assert.False(service.ShowNotification(CaptureSuccess None, false))
    // force=true bỏ qua enabled; trong môi trường không có Avalonia app thì Show() có thể throw
    // nhưng điều đó thuộc về runtime, test này chỉ đảm bảo không throw ở phần check enabled.
    service.ShowNotification(CaptureSuccess None, false, force = true) |> ignore

/// Kiểm tra HighlightFileInExplorer trả về false khi file không tồn tại.
[<Fact>]
let ``HighlightFileInExplorer tra ve false khi file khong ton tai`` () =
    let nonExistent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png")
    Assert.False(NotificationService.HighlightFileInExplorer(nonExistent))

/// Kiểm tra Dispose không throw khi chưa có cửa sổ nào.
[<Fact>]
let ``Dispose khong throw khi chua co cua so`` () =
    use service = new NotificationService()
    service.Dispose()
    (service :> IDisposable).Dispose()
    Assert.True(true)
