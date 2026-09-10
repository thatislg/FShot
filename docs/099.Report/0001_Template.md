# Báo cáo thực thi tác vụ: [TÊN TÁC VỤ]

- **File Name:** `YYYYMMDD_HHmm_[MODULE]_[TASK_SLUG].md`
- **Thời gian hoàn thành:** YYYY-MM-DD HH:mm (JST)
- **Người thực hiện:** Kimi-2.7-Code
- **Trạng thái:** [Completed / In-Progress / Blocked]
- **Trace SRS / Architecture:**
  - SRS Items: `FR-CAP-xx`, `FR-SEL-xx`, `FR-NF-xx`
  - Layer thực thi: `FShot.Core` | `FShot.Rendering.Skia` | `FShot.Platform.Win32` | `FShot.UI` | `FShot.Core.Tests`

---

### 1. Tóm tắt công việc (Summary)
- <Mục tiêu chính của tác vụ>
- <Những thay đổi trọng tâm vừa hoàn thành>

### 2. Chi tiết thay đổi mã nguồn (File Changes)
- `src/...`: Thêm mới logic...
- `tests/...`: Bổ sung test case cho...

### 3. Đánh giá tác động kiến trúc (Architectural Check)
- [ ] **Domain Purity:** Module `Core` giữ nguyên trạng thái bất biến (Immutable), không bị dính UI/Rendering library?
- [ ] **Resource Management:** Các đối tượng unmanaged (`IDisposable`, `SKBitmap`, GDI handles) đã được giải phóng đúng cách?
- [ ] **Contract Integrity:** Hợp đồng Interface/DU dùng chung giữa các layer không bị phá vỡ ngoài dự kiến?

### 4. Kết quả kiểm thử & Nghiệm thu (Acceptance & Verification)
- **Build Status:** [Pass / Fail] (`dotnet build`)
- **Unit Tests:** X passed, 0 failed (`dotnet test`)
- **Manual Verification:**
  - [ ] Kiểm tra hiển thị trực quan (Visual render / Window layout)
  - [ ] Kiểm tra tương tác chuột/bàn phím (Input Handling)
  - [ ] Hiệu năng đạt chuẩn (FPS ≥ 60 hoặc Latency < 100ms)

### 5. Nợ kỹ thuật & Bước kế tiếp (Technical Debt & Next Action)
- **Technical Debt / Cần thay thế sau:** <Ví dụ: dữ liệu giả lập (Stub), cần thay bằng Win32 API thật ở Phase 2>
- **Tác vụ kế tiếp:** <Nêu rõ bước tiếp theo cần làm>
