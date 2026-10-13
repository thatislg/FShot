# Báo cáo kỹ thuật: Điều tra & Giải quyết dứt điểm lỗi TSF IME (BambooMintKey) trên FShot

- **File Name:** `20261014_0100_TSF_BambooMintKey_Text_Input_Bug_Report.md`
- **Thời gian lập ban đầu:** 2026-10-14 01:00 (JST)
- **Thời gian giải quyết dứt điểm:** 2026-10-14 03:20 (JST)
- **Môi trường:** Windows 10/11 x64, .NET 8, Avalonia UI 12.1.2, BambooMintKey (TSF NativeAOT TIP)
- **Trạng thái:** **RESOLVED & VERIFIED 100% BY EMPIRICAL PoC AND LIVE TESTING**
- **Trace SRS / Architecture:**
  - SRS Items: `FR-ANN-12` (Text Annotation), `FR-ANN-21` (Text Input Handling & IME Support)
  - Layer liên quan: `FShot.Core`, `FShot.Platform.Win32`, `FShot.UI`

---

## 1. Tóm tắt vấn đề & Diễn biến thực nghiệm

### 1.1. Hiện tượng ban đầu
- Trên FShot, khi dùng công cụ Text Annotation để gõ tiếng Việt bằng bộ gõ **BambooMintKey** (bộ gõ tiếng Việt hiện đại dựa trên Windows Text Services Framework - TSF):
  1. Hoàn toàn không gõ được tiếng Việt có dấu, chỉ ra chữ không dấu thô (ví dụ: gõ `thuw` chỉ ra `thuw` thay vì `thư`).
  2. Khi đang ở ô nhập text, nếu chuyển sang bộ gõ khác (như US Keyboard, Japanese IME) thì chuyển được; nhưng **không thể chuyển đổi lại đúng BambooMintKey**.
  3. Hiện tượng này xảy ra trên toàn bộ các ô TextBox trong FShot (kể cả trong cửa sổ Cài đặt `SettingsWindow`).

### 1.2. Kết quả kiểm chứng qua PoC độc lập (`BambooMintKeyTest`)
- Thiết lập một project Avalonia 12.1.2 (.NET 8) thuần túy độc lập mang tên `BambooMintKeyTest`.
- Kết quả kiểm chứng thực tế: **Avalonia TextBox chuẩn hỗ trợ BambooMintKey hoàn hảo 100%** mà không cần bất kỳ can thiệp native hay thư viện bổ sung nào.
- Phép thử phân loại trên FShot: Kiểm tra ô TextBox trong `SettingsWindow` cũng không gõ được tiếng Việt $\rightarrow$ **Khẳng định nguyên nhân gốc nằm ở cấp Tiến trình (Process-level Execution Context) chứ không phải do Avalonia TextBox**.

---

## 2. Phân tích nguyên nhân gốc thực sự (True Root Cause Analysis)

### 2.1. Nguyên nhân cốt lõi số 1: Lệch Apartment State của COM / TSF do F# `Async.RunSynchronously`
- **Cơ chế của Windows TSF**: BambooMintKey là một **TSF TIP (Text Input Processor)** in-process COM Server DLL. Windows TSF bắt buộc luồng giao diện của ứng dụng chủ phải là **STA (Single-Threaded Apartment)** thuần túy với Message Loop chuẩn trực tiếp từ hệ điều hành.
- **Lỗi trong FShot**:
  Trong `Program.fs`, hàm `main` có gán cờ `[<EntryPoint; STAThread>]`, nhưng lệnh khởi động Avalonia lại bị bọc bên trong một khối F# asynchronous computation:
  ```fsharp
  // CODE CŨ GÂY LỖI:
  and runDaemonAsync (cancellationToken: CancellationToken) : Async<int> =
      async {
          let app = buildAvaloniaApp()
          App.IsDaemon <- true
          return app.StartWithClassicDesktopLifetime([||])
      }

  // Trong main:
  runDaemonAsync cts.Token |> Async.RunSynchronously
  ```
- **Hậu quả**: `Async.RunSynchronously` của F# runtime thiết lập một synchronization context riêng biệt lên luồng chính. Khi Avalonia khởi tạo message loop bên trong ngữ cảnh này, **Windows COM / TSF không thể nhận diện luồng này là STA UI Thread tiêu chuẩn**. Windows TSF tự động đánh dấu toàn bộ tiến trình FShot là *Non-TSF Application* và **loại bỏ BambooMintKey khỏi danh sách bộ gõ hợp lệ của tiến trình**.

### 2.2. Nguyên nhân số 2: Extended Window Style `WS_EX_TOOLWINDOW` do `ShowInTaskbar = false`
- Trên `CaptureOverlayWindow`, thuộc tính `ShowInTaskbar` ban đầu được đặt là `false`.
- Trên Win32 backend của Avalonia, `ShowInTaskbar = false` làm cho Window nhận extended style **`WS_EX_TOOLWINDOW`**.
- Mặc định, Windows TSF coi ToolWindow là thanh công cụ nổi (floating palette) và vô hiệu hóa TSF Text Input Processors trên các cửa sổ này.

### 2.3. Nguyên nhân số 3: Xung đột phím tắt và Focus trong `CaptureCanvas`
- Trong `CaptureCanvas.axaml.fs`, `CaptureCanvas` có `Focusable = true` và tự gọi `this.Focus()` trong `OnAttachedToVisualTree` và `OnPointerPressed`.
- Sự kiện `OnKeyDown` của Canvas bắt các phím tắt công cụ (`s`, `r`, `a`, `1..9`) nuốt mất các phím gõ Telex/VNI trước khi TextBox xử lý.

---

## 3. Giải pháp đã thực hiện & Kết quả nghiệm thu

| Thành phần | Thay đổi thực hiện | Kết quả |
|---|---|---|
| **`Program.fs`** | Chuyển `runDaemon` và `runGui` thành hàm đồng bộ, gọi trực tiếp `app.StartWithClassicDesktopLifetime([||])` trên STA Main Thread (loại bỏ `Async.RunSynchronously`). | **Khắc phục triệt để**: Windows TSF nhận diện tiến trình STA hợp lệ; BambooMintKey kích hoạt thành công trên toàn bộ FShot. |
| **`CaptureOverlayWindow.axaml`** | Đổi `ShowInTaskbar = true`, gỡ bỏ `Focusable="True"` và `IsVisible="False"`. | Loại bỏ cờ `WS_EX_TOOLWINDOW`, giúp TSF Document Manager duy trì kết nối context nhập liệu. |
| **`CaptureCanvas.axaml.fs`** | Khi `textBox.IsSome`: Bỏ qua 100% phím tắt công cụ, chỉ bắt `Escape` (hủy) và `Ctrl+Enter` (commit). Click ra ngoài thì commit và không sinh lặp TextBox. | Nhập liệu tiếng Việt mượt mà, không bị nuốt phím tắt, vòng đời đóng/mở chuẩn xác. |
| **`app.manifest`** | Khôi phục Per-Monitor V2 DPI Awareness tiêu chuẩn. | Đảm bảo rendering sạch nét trên đa màn hình mixed-DPI mà không ảnh hưởng tới bộ gõ. |

---

## 4. Kết luận & Bài học kinh nghiệm
1. **Avalonia UI 12 hoàn toàn tương thích với các bộ gõ TSF hiện đại (như BambooMintKey)** mà không cần viết thêm bất kỳ Native Win32 wrapper hay COM hook phức tạp nào.
2. **Luôn chạy GUI Event Loop trực tiếp trên Main Thread**: Trong F#, tuyệt đối không bọc lệnh blocking khởi động GUI (`app.StartWithClassicDesktopLifetime`) vào trong `async { ... } |> Async.RunSynchronously` vì sẽ làm sai lệch ApartmentState của luồng và phá vỡ cơ chế tích hợp OLE/COM/TSF của hệ điều hành Windows.
3. Không tự ý thay thế control chuẩn của framework bằng Win32 controls khi chưa có thực nghiệm cô lập (PoC).
