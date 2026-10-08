# Báo cáo: Epic 8 — Real Capture Backend & Mixed DPI (P2.07–P2.10)

- **Ngày thực hiện:** 2026-10-08
- **Phạm vi:**
  - **P2.07** Backend `Windows.Graphics.Capture` (WinRT) chụp đa màn hình đúng mixed-DPI (`FR-WIN-001`).
  - **P2.08** Chụp một màn hình cụ thể + màn hình chứa con trỏ qua WinRT.
  - **P2.09** Fallback GDI `BitBlt` + bộ điều phối `CompositeCaptureService` + cấu hình `captureBackend` + cờ CLI `--backend`/`--force-gdi`.
  - **P2.10** Xử lý Mixed DPI + Per-Monitor V2 DPI Awareness (`FR-SYS-017`, `FR-SYS-018`).
- **Trạng thái:** Hoàn thành phần triển khai, biên dịch 0 lỗi, **295 / 295 unit tests Passed**. Runtime verification **treo lại** (cần máy Windows thật + GPU), xem mục 6.

---

## 1. Quyết định thiết kế chính

| Vấn đề | Quyết định |
| ------ | ---------- |
| Cách lấy `GraphicsCaptureItem` từ màn hình | COM Interop `IGraphicsCaptureItemInterop.CreateForMonitor` (GUID `3628E81B-…`), qua `WinRT.ActivationFactory.Get`. |
| Thiết bị đồ họa | Thư viện **Vortice.Direct3D11 + Vortice.DXGI** (đã cache sẵn trong NuGet) để tránh viết thủ công vtable D3D11/DXGI hàng trăm method. |
| Cầu nối ID3D11Device → WinRT `IDirect3DDevice` | `Vortice.D3D11.CreateDirect3D11DeviceFromDXGIDevice<IDirect3DDevice>`. |
| Đọc pixel | Staging Texture (`ResourceUsage.Staging` + `CpuAccessFlags.Read`) → `CopyResource` → `Map` → read → `Unmap` (02_05 mục 4). |
| FramePool | `CreateFreeThreaded`, buffer **1 frame**, `DirectXPixelFormat.B8G8R8A8UIntNormalized`. |
| Con trỏ/viền vàng | `IsCursorCaptureEnabled = false`; `IsBorderRequired` không có trong projection 19041 nên bỏ qua (nợ test). |
| Mixed DPI virtual screen | Chụp từng màn hình ở native resolution rồi downscale về logical để ghép canvas virtual (ScaleFactor 1.0). |
| Fallback | `CompositeCaptureService`: GDI nếu `captureBackend=Gdi`/`--force-gdi`, hoặc WGC thất bại, hoặc OS < 18362. |

---

## 2. Chi tiết công việc

### 2.1 P2.07 — WGC backend
- **`Direct3DInterop.fs` (mới):** COM interop `IGraphicsCaptureItemInterop`, tạo device, cầu nối DXGI→WinRT, `createItemForMonitor`, `isSupported` (build ≥ 18362 + `GraphicsCaptureSession.IsSupported`), `readPixels` (Staging Texture, tôn trọng `RowPitch`).
- **`WgcCaptureService.fs` (mới):** implements `ICaptureService`; device Lazy Singleton; poll frame (`TryGetNextFrame`, timeout ~1s); `captureMonitor` trả về `CaptureResult` native resolution.

### 2.2 P2.08 — Chụp màn hình đơn + con trỏ
- `CaptureScreenAsync(index)` tra `HMONITOR` qua `ScreenEnumeration.getMonitorHandle` (mới bổ sung), chụp native.
- `CaptureCursorScreenAsync()` đọc vị trí con trỏ (`GetCursorPos`) → `getScreenContainingPoint` → chụp; fallback màn hình chính.

### 2.3 P2.09 — Fallback & điều phối
- **`CompositeCaptureService.fs` (mới):** chọn backend theo `captureBackend` ("Auto"|"Wgc"|"Gdi"), try-catch WGC → fallback GDI; static `ForceGdi` cho cờ CLI.
- **`Config.fs`:** thêm trường `CaptureBackend`.
- **`Program.fs`:** cờ `--force-gdi` / `--backend gdi|wgc|auto` ép backend.
- **`App.axaml.fs` / `CaptureOverlayWindow.axaml.fs`:** thay `WindowsCaptureService` bằng `CompositeCaptureService` (giữ fallback `StubCaptureService`).

### 2.4 P2.10 — Per-Monitor V2 & Mixed DPI
- **`app.manifest`:** bổ sung `<dpiAwareness>PerMonitorV2, unaware</dpiAwareness>`.
- **`ScreenEnumeration.fs`:** đã có `GetDpiForMonitor(MDT_EFFECTIVE_DPI)` từ trước; bổ sung `getMonitorHandle`.

---

## 3. Các file mã nguồn thay đổi

1. `src/FShot.Platform.Win32/Capture/Direct3DInterop.fs` — **mới** (COM + D3D11 + WinRT bridge).
2. `src/FShot.Platform.Win32/Capture/WgcCaptureService.fs` — **mới** (backend WGC).
3. `src/FShot.Platform.Win32/Capture/CompositeCaptureService.fs` — **mới** (điều phối đa backend).
4. `src/FShot.Platform.Win32/Screen/ScreenEnumeration.fs` — thêm `getMonitorHandle`.
5. `src/FShot.Platform.Win32/FShot.Platform.Win32.fsproj` — thêm Vortice + 3 file capture + sắp xếp thứ tự compile (Config trước Capture).
6. `src/FShot.Core/Domain/Config.fs` — thêm `CaptureBackend`.
7. `src/FShot.UI/app.manifest` — Per-Monitor V2.
8. `src/FShot.UI/App.axaml.fs`, `Program.fs`, `Windows/CaptureOverlayWindow.axaml.fs` — dùng `CompositeCaptureService` + cờ CLI backend.
9. Tests: `CaptureBackendTests.fs` (mới), cập nhật `ConfigTests.fs`, `ConfigStoreTests.fs`.

---

## 4. Build & Test

```text
dotnet build FShot.sln   → Build succeeded. 0 Error(s)

dotnet test FShot.sln:
  FShot.Core.Tests.dll           Passed: 202
  FShot.Rendering.Skia.Tests.dll Passed:  22
  FShot.UI.Tests.dll             Passed:  71
  ─────────────────────────────────────────────
  TỔNG CỘNG: 295 / 295 PASSED
```

---

## 5. Nợ test (treo lại, chờ runtime / GPU thật)

| # | Nợ test | Cách verify | Lý do treo |
| - | ------- | ----------- | ---------- |
| 1 | WGC chụp đúng native resolution, không viền vàng | Chạy `fshot screen 0` trên Win10/11 có GPU | Cần GPU + WinRT runtime |
| 2 | Chụp đa màn hình mixed-DPI (150% + 100%) không nhòe | Kết nối 4K(150%) + FHD(100%), chụp virtual screen | Cần phần cứng |
| 3 | Fallback GDI khi WGC bị chặn | `fshot --force-gdi` hoặc chặn quyền ghi màn hình | Cần runtime |
| 4 | `IsBorderRequired = false` trên Win11 22000+ | Chụp trên Win11, kiểm tra không có viền vàng | Projection 19041 không expose property này (cần nâng SDK hoặc reflection) |
| 5 | Hiệu năng < 50ms | Đo thời gian chụp frame đầu | Cần runtime |
| 6 | Downscale nearest-neighbor khi ghép virtual screen | So chất lượng ảnh ghép trên 2 màn hình khác scale | Có thể nâng lên box-filter sau |

---

## 6. Tiếp theo

- Người dùng thực hiện tài liệu thiết kế **Epic 9: Config Persistence & Editor**.
- Khi có máy Windows thật, quay lại giải quyết các mục nợ test ở mục 5.
