# Báo cáo: Epic 12 — Selection Engine Advanced (P2.27–P2.31)

- **Ngày thực hiện:** 2026-11-12
- **Phạm vi:**
  - **P2.27** Co giãn đối xứng 2 px bằng `Ctrl+Shift+Arrow` (`FR-SEL-07`).
  - **P2.28** Ngăn vùng chọn thu nhỏ quá mức và giới hạn trong phạm vi chụp (`FR-SEL-08`, `FR-SEL-09`).
  - **P2.29** Chọn toàn bộ màn hình chụp bằng `Ctrl+A` (`FR-SEL-10`).
  - **P2.30** Hiển thị tọa độ và kích thước vùng chọn `WxH+X+Y` (`FR-SEL-12`).
  - **P2.31** Co giãn đối xứng điểm đối diện khi giữ Shift kéo handle (`FR-SEL-16`).
- **Trạng thái:** Hoàn thành phần triển khai, biên dịch 0 lỗi, toàn bộ unit tests Passed.

---

## 1. Quyết định thiết kế chính

| Vấn đề | Quyết định |
| ------ | ---------- |
| Kích thước tối thiểu | Nâng `MinWidth`/`MinHeight` từ 10.0 → **20.0 px** theo `FR-SEL-08`, đảm bảo đủ diện tích hiển thị 8 handles + toolbar, tránh render 0-pixel. |
| Hoàn tất tương tác co giãn | `FinishInteraction` **clamp về kích thước tối thiểu** thay vì hủy vùng, tránh vùng bị lật ngược (inverted) khi kéo handle quá tay. |
| Co giãn đối xứng bàn phím | `KeyboardSymmetricResize(dx, dy)` giữ cố định tâm `C`, biến đổi đều 2 cạnh đối diện: `X' = X - dx/2`, `W' = W + dx`. |
| Mirror-resize chuột | `UpdateResizingSymmetric(handle, current)` tính nửa kích thước mới từ tâm `C` tới handle đang kéo, đối diện phản chiếu đối xứng. |
| Select-All | `Selection.SelectAll(virtualBounds)` tạo vùng phủ trọn `VirtualBounds`, hoạt động từ cả `Idle` lẫn `Selected`, bảo toàn annotation. |
| Geometry badge | `RenderModel.GeometryText: string option` format `"%.0f × %.0f + %.0f + %.0f"`, vẽ trong `CaptureCanvas.RenderGeometryBadge` với nền tối mờ + bo góc, tự lật xuống dưới khi sát mép trên. |

---

## 2. Chi tiết công việc

### 2.1 Domain model (`src/FShot.Core/Domain/Selection.fs`)
- `MinWidth`/`MinHeight` → `20.0`.
- `FinishInteraction()` clamp về min-size (không reset về `Idle`).
- Thêm `EnsureMinimumSize(minW, minH, rect)`.
- Thêm `KeyboardSymmetricResize(dx, dy)`.
- Thêm `UpdateResizingSymmetric(handle, current)`.
- Thêm `SelectAll(virtualBounds)`.

### 2.2 State machine (`src/FShot.Core/State/OverlayState.fs`)
- `RenderModel` thêm `GeometryText: string option`.
- `OverlayState` thêm `ShiftPressed`.
- `OverlayEvent` thêm `ShiftModifier`, `SelectAll`.
- Handler `SelectAll` cho `Idle`/`Selected`.
- Handler `ShiftModifier` cập nhật cờ mirror-resize.
- `Ctrl+Shift+Arrow` → `KeyboardSymmetricResize(dx*2, dy*2)`; `Shift+Arrow` → `KeyboardResize`; mũi tên → `Nudge`.
- `Resizing` + `PointerMoved` khi `ShiftPressed` → `UpdateResizingSymmetric`.
- `buildRenderModel` phát `GeometryText`.

### 2.3 UI (`src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`)
- `OnKeyDown`: `Ctrl+A` → `SelectAll`; `LeftShift`/`RightShift` → `ShiftModifier true`.
- `OnKeyUp`: reset `ShiftModifier false`.
- Thêm `RenderGeometryBadge` + gọi trong `Render`.

### 2.4 Unit tests
- `tests/FShot.Core.Tests/Domain/SelectionTests.fs`: cập nhật test min-size 10→20; thêm test `KeyboardSymmetricResize` 4 hướng + clamp min, `SelectAll`, `UpdateResizingSymmetric` giữ tâm.
- `tests/FShot.Core.Tests/State/OverlayStateTests.fs`: thêm test `SelectAll` từ Idle/Selected, `Ctrl+Shift+Right`, `GeometryText` WxH+X+Y.

---

## 3. Các file mã nguồn thay đổi

1. `src/FShot.Core/Domain/Selection.fs` — min-size, symmetric resize, mirror-resize, SelectAll.
2. `src/FShot.Core/State/OverlayState.fs` — `GeometryText`, `ShiftPressed`, event/handler mới.
3. `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs` — phím tắt, `RenderGeometryBadge`.
4. `tests/FShot.Core.Tests/Domain/SelectionTests.fs` — cập nhật + thêm test.
5. `tests/FShot.Core.Tests/State/OverlayStateTests.fs` — thêm test.

---

## 4. Build & Test

```text
dotnet build FShot.sln → Build succeeded. 0 Error(s)
dotnet test FShot.sln   → toàn bộ Passed (xem Epic 14 tổng hợp)
```

---

## 5. Nợ test (chờ runtime)

| # | Nợ test | Cách verify | Lý do treo |
| - | ------- | ----------- | ---------- |
| 1 | Kéo handle ép vùng siêu nhỏ, xác nhận dừng ở 20×20 không lật ngược | Kéo chuột trên overlay | Cần GUI |
| 2 | `Ctrl+Shift+Arrow` nở/co đều quanh tâm | Bấm phím trên overlay | Cần GUI |
| 3 | Badge WxH+X+Y nhảy xuống dưới khi sát mép trên | Kéo vùng sát mép trên | Cần GUI |
| 4 | `fshot gui --region` clamp về mép màn hình | Chạy CLI | Chưa wire `--region` vào CLI parser |

---

## 6. Tiếp theo

- Người dùng thực hiện **Epic 13: Undo/Redo Advanced** (P2.32–P2.34) hoặc các Epic tiếp theo.
