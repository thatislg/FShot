# FileAndClipboard — Thiết kế chi tiết

> Tài liệu này mô tả các thao tác I/O lưu file đĩa và tích hợp Clipboard trên Windows (`FR-OUT-01`, `FR-OUT-02`, `FR-OUT-04`, `FR-OUT-05`).

---

## 1. Cơ chế I/O lưu file đĩa trên Windows

Trong Avalonia UI và .NET 8 trên Windows, việc ghi file cần xử lý chặt chẽ các trường hợp sau:

### 1.1. Luồng lưu tức thì (Instant Save)
- Kích hoạt khi có cấu hình `savePath` (từ cấu hình hoặc CLI).
- Tự động gọi `Directory.CreateDirectory(dir)` nếu thư mục đích chưa tồn tại.
- Sử dụng `File.Create(resolvedPath)` để ghi trực tiếp byte data của bitmap đã mã hóa (`SKImage.Encode`), sau đó `stream.Flush()`.
- Dispatch event `ExportCompleted true` và tự động đóng overlay nếu `CloseAfterExport = true`.

### 1.2. Luồng Save As File Picker Fallback
- Sử dụng `w.StorageProvider.SaveFilePickerAsync(options)` với các bộ lọc `*.png` và `*.jpg; *.jpeg`.
- **Quan trọng về đường dẫn Avalonia trên Windows**: Đối tượng `IStorageFile` trả về từ Avalonia có thuộc tính `f.Path` là `System.Uri` (ví dụ `file:///D:/path/file.png`), nếu lấy `f.Path.AbsolutePath` sẽ sinh chuỗi có dấu gạch chéo đầu `"/D:/path/file.png"` gây lỗi `NotSupportedException` trên Windows `File.OpenWrite`.
- **Giải pháp an toàn**: 
  1. Sử dụng trực tiếp `let! stream = f.OpenWriteAsync() |> Async.AwaitTask` do Avalonia cung cấp để ghi dữ liệu, đảm bảo tương thích mọi nền tảng và mọi bộ lưu trữ.
  2. Lấy đường dẫn cục bộ qua `f.TryGetLocalPath()` (hoặc `f.Path.LocalPath`) để xác định định dạng mở rộng và ghi log.
- Nếu người dùng nhấn Cancel, dispatch `ExportCompleted false`, không thay đổi trạng thái canvas và không làm gián đoạn phiên làm việc.

---

## 2. Cơ chế tích hợp Clipboard trên Windows

### 2.1. Mã hóa hình ảnh
- Vùng chọn ảnh screenshot kết hợp tất cả các annotations đã commit được vẽ và crop qua `SceneComposer.renderExport`.
- Bitmap được mã hóa sang mảng byte PNG nguyên bản (`SKEncodedImageFormat.Png`, chất lượng 100).

### 2.2. Đưa vào Clipboard Avalonia
- Khởi tạo `DataFormat.CreateBytesPlatformFormat("PNG")`.
- Đóng gói qua `DataTransferItem` và `DataTransfer`.
- Gọi `clipboard.SetDataAsync(transfer)` trên `TopLevel.Clipboard`.
- Điều này cho phép dán trực tiếp vào các ứng dụng phổ biến trên Windows như Discord, Telegram, trình duyệt web, Slack, Photoshop, Paint.

---

## 3. Xử lý lỗi & Ghi log (Logging)

- Mọi thao tác xuất ảnh (Save / Copy) đều được bọc trong khối `try...with` chạy bất đồng bộ (`Async.Start`).
- Các lỗi IO hoặc ngoại lệ clipboard đều được ghi chi tiết qua `FShotLog.writeEx` kèm stack trace vào `%TEMP%\FShot\logs\FShot_Current.log`.
- Khi có lỗi, hệ thống dispatch `ExportCompleted false` để giữ an toàn cho ứng dụng, không làm crash giao diện.
