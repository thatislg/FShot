# F-Shot Progress — Phase 3: Checklist Chuẩn bị Đưa Bản v1.0 lên Microsoft Store

- **Thư mục:** `docs/3.Progress/Phase3/`
- **Liên kết:** [03_31_MSIX_Packaging_And_Store_Progress.md](file:///d:/Kojin/FShot/docs/3.Progress/Phase3/03_31_MSIX_Packaging_And_Store_Progress.md)
- **Mục tiêu:** Hoàn thiện các yêu cầu bắt buộc trên gói `FShot.Packaging` trước khi xuất `.msixupload` và nộp lên Microsoft Partner Center.

---

## 1. Hiện trạng Build & Đóng gói

- [x] Đã tạo project [FShot.Packaging.wapproj](file:///d:/Kojin/FShot/FShot.Packaging/FShot.Packaging.wapproj) và liên kết Entry Point tới [FShot.UI.fsproj](file:///d:/Kojin/FShot/src/FShot.UI/FShot.UI.fsproj).
- [x] Đã xử lý lỗi `wappublish\win-x64\project.assets.json not found` và tắt cảnh báo giả `NU1701` (`UAP,Version=v10.0.17763`).
- [x] Đã đồng bộ `TargetDeviceFamily` sang `Windows.Desktop` (`10.0.19041.0` – `10.0.26100.0`) trong [Package.appxmanifest](file:///d:/Kojin/FShot/FShot.Packaging/Package.appxmanifest).
- [x] Đã build thử thành công gói Sideloading (`FShot.Packaging_0.1.0.0_x64.msixbundle`).

---

## 2. Checklist 5 Hạng mục Bắt buộc Trước khi Nộp Microsoft Store

### 2.1. Liên kết ứng dụng với Microsoft Partner Center (*Associate App with the Store*)
- [ ] Chuột phải vào project **`FShot.Packaging`** trong Visual Studio → **Publish** → **Associate App with the Store...**.
- [ ] Đăng nhập tài khoản Microsoft Developer và chọn tên ứng dụng đã đăng ký/đặt chỗ trên Partner Center.
- [ ] Xác nhận Visual Studio đã tự cập nhật lại `<Identity Name="..." Publisher="..." />` trong [Package.appxmanifest](file:///d:/Kojin/FShot/FShot.Packaging/Package.appxmanifest#L9-L12) (thay cho giá trị tạm `Publisher="CN=NTQJ-"` hiện tại).

### 2.2. Cập nhật Tên hiển thị, Nhà phát hành & Mô tả (`Package.appxmanifest`)
- [ ] Mở [Package.appxmanifest](file:///d:/Kojin/FShot/FShot.Packaging/Package.appxmanifest#L14-L36) (tab **Application** & **Packaging**) và thay các giá trị mặc định:
  - [ ] `DisplayName` (Properties & VisualElements): Đổi `FShot.Packaging` → `F-Shot` (khớp tên đã đặt trên Store).
  - [ ] `PublisherDisplayName`: Đổi `NTQJ-` → tên nhà phát hành chính thức trên Partner Center.
  - [ ] `Description`: Đổi `FShot.Packaging` → mô tả rõ ràng (ví dụ: *Fast, lightweight screenshot & annotation tool for Windows.*).
  - [ ] `Version`: Kiểm tra số phiên bản hợp lệ cho Store (4 cụm số, cụm thứ 4 kết thúc bằng `.0`, ví dụ `1.0.0.0` hoặc `0.1.0.0`).

### 2.3. Thay thế Bộ Visual Assets (Icons) chuẩn Store
- [ ] Kiểm tra thư mục `FShot.Packaging/Images/`: đảm bảo không còn giữ các file icon dấu gạch chéo (`X`) mặc định của Visual Studio template (Store sẽ từ chối nếu phát hiện placeholder icon).
- [ ] Dùng tab **Visual Assets → Asset Generator** trong [Package.appxmanifest](file:///d:/Kojin/FShot/FShot.Packaging/Package.appxmanifest) với file PNG gốc `1024×1024 px` nền trong suốt để sinh đủ:
  - [ ] `Square44x44Logo` (các mức scale + `targetsize-24_altform-unplated.png`)
  - [ ] `Square150x150Logo`
  - [ ] `Wide310x150Logo`
  - [ ] `StoreLogo`
  - [ ] `SplashScreen`

### 2.4. Rà soát Quyền (`Capabilities`) & Chuẩn bị Giải trình `runFullTrust`
- [ ] Kiểm tra mục `<Capabilities>` trong [Package.appxmanifest](file:///d:/Kojin/FShot/FShot.Packaging/Package.appxmanifest#L45-L48):
  - [ ] Nếu bản v1.0 chưa sử dụng kết nối mạng: xóa `<Capability Name="internetClient" />` để giảm bớt bước kiểm duyệt không cần thiết.
  - [ ] Giữ `<rescap:Capability Name="runFullTrust" />` (bắt buộc cho ứng dụng desktop Win32/.NET 8).
- [ ] Chuẩn bị sẵn nội dung giải trình `runFullTrust` để dán vào mục *Notes for certification* trên Partner Center:
  - *Gợi ý mẫu (tiếng Anh):*
    > `F-Shot is a desktop screenshot and annotation utility built on .NET 8 and Avalonia. It requires the runFullTrust capability to: (1) register a global PrintScreen keyboard hook (WH_KEYBOARD_LL) for instant capture from any window, (2) capture the desktop screen via Windows.Graphics.Capture / GDI BitBlt, (3) run a system tray background icon, and (4) save captured screenshots to user-selected folders such as Pictures\Screenshots.`

### 2.5. Xuất gói Store (`.msixupload`), Chạy WACK & Upload Partner Center
- [ ] Chuột phải **`FShot.Packaging`** → **Publish** → **Create App Packages...** → chọn **Microsoft Store under an existing app name** (hoặc tên đã liên kết).
- [ ] Chọn cấu hình **Release** (`x64`, có thể kèm `ARM64` nếu muốn).
- [ ] Sau khi tạo xong gói, bấm **Launch Windows App Certification Kit (WACK)** ngay cuối wizard.
- [ ] Xác nhận WACK chạy hoàn tất và trả về kết quả **`PASSED`**.
- [ ] Truy cập [Microsoft Partner Center](https://partner.microsoft.com/dashboard):
  - [ ] Upload file **`.msixupload`** vào mục **Packages**.
  - [ ] Tải lên ảnh chụp màn hình giới thiệu ứng dụng (có thể dùng chính F-Shot để chụp!), điền mô tả Store Listing, phân loại độ tuổi (Age Rating), chính sách quyền riêng tư (Privacy Policy) và ghi chú `runFullTrust`.
  - [ ] Bấm **Submit to the Store**.
