# FileStore — Cơ chế Đọc/Ghi Cấu hình trên Windows

> Tài liệu này mô tả chi tiết vị trí lưu trữ, quy trình nạp (Load), ghi (Save), xử lý fallback an toàn và khởi tạo tự động file cấu hình tại `%APPDATA%\FShot\config.json`.

---

## 1. Vị trí lưu trữ cấu hình trên Windows

- **Thư mục cấu hình**:
  - `%APPDATA%\FShot`
  - Được phân giải thông qua .NET API:
    ```csharp
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FShot")
    ```
- **File cấu hình chính**:
  - `%APPDATA%\FShot\config.json`

---

## 2. Quy trình nạp cấu hình (`LoadConfig`)

```
                  ┌──────────────────────┐
                  │ Bắt đầu LoadConfig() │
                  └──────────┬───────────┘
                             │
                  Thư mục & File tồn tại?
                             │
                ┌────────────┴────────────┐
             Chưa                         Đã có
                │                           │
  Tạo thư mục %APPDATA%\FShot               Đọc chuỗi JSON từ file
  Ghi file config.json với                  Parse qua System.Text.Json
  cấu hình mặc định AppConfig.Default       (WriteIndented, CamelCase)
                │                           │
  Trả về AppConfig.Default        ┌─────────┴─────────┐
                                Thành công          Lỗi cú pháp / IO
                                  │                   │
                                Trả về AppConfig    Ghi log cảnh báo
                                đã chuẩn hóa        Trả về AppConfig.Default
```

### Nguyên tắc xử lý lỗi (Graceful Fallback):
1. **File chưa tồn tại**: Ứng dụng không báo lỗi mà tự động sinh file cấu hình chuẩn với đầy đủ các trường và chú thích định dạng, giúp người dùng dễ dàng chỉnh sửa sau này.
2. **File bị hỏng / cú pháp JSON không hợp lệ**: Ứng dụng không crash hay dừng hoạt động. Hệ thống ghi log cảnh báo và nạp cấu hình mặc định an toàn (`AppConfig.Default`).
3. **Thiếu một số trường**: Các trường bị thiếu được tự động điền bằng giá trị mặc định của hệ thống.

---

## 3. Quy trình ghi cấu hình (`SaveConfig`)

1. Đảm bảo thư mục cha `%APPDATA%\FShot` tồn tại thông qua `Directory.CreateDirectory`.
2. Chuẩn hóa dữ liệu cấu hình trước khi ghi (`config.Normalized()`).
3. Tuần tự hóa `AppConfig` sang chuỗi JSON có thụt đầu dòng (indented formatting) theo chuẩn `camelCase`.
4. Ghi nguyên tử hoặc ghi đè an toàn vào file `config.json` qua `File.WriteAllText`.

---

## 4. Tích hợp với vòng đời ứng dụng

- **Khi ứng dụng khởi động (`Program.fs` / `App.axaml.fs`)**:
  - Hệ thống gọi `ConfigStore.loadSnapshot()` để lấy bản sao `ConfigSnapshot`.
  - Giá trị này được lưu vào `App.ConfigSnapshot` và truyền vào `OverlayStateLogic.init` khi mở cửa sổ overlay.
  - Chế độ chụp không giao diện (headless) trong `Program.fs` cũng sử dụng snapshot này để xác định chất lượng ảnh nén hoặc đường dẫn lưu mặc định.
- **Khi xuất dữ liệu (`Save` / `Copy`)**:
  - `CaptureCanvas` sử dụng `state.Config.SaveOptions` để tự động kích hoạt lưu tức thì nếu `savePath` được chỉ định, hoặc mở hộp thoại Save As nếu chưa cấu hình.
