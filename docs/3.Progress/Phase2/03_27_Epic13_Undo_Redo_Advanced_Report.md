# Báo cáo: Epic 13 — Undo/Redo Advanced (P2.32–P2.34)

- **Ngày thực hiện:** 2026-11-12
- **Phạm vi:**
  - **P2.32** Cấu hình giới hạn số bước lưu lịch sử (`FR-UNDO-03`).
  - **P2.33** Lưu toàn bộ danh sách chú thích vào mỗi snapshot (`FR-UNDO-04`).
  - **P2.34** Hoàn tác việc di chuyển lớp lên/xuống (`FR-UNDO-06`).
- **Trạng thái:** Hoàn thành phần triển khai, biên dịch 0 lỗi, toàn bộ unit tests Passed.

---

## 1. Quyết định thiết kế chính

| Vấn đề | Quyết định |
| ------ | ---------- |
| Giới hạn lịch sử | Thêm `UndoLimit: int` vào `AppConfig` (mặc định 100, clamp `[10..500]`), thread sang `ConfigSnapshot.HistoryLimit`. |
| Cắt stack ngay lập tức | `HistoryStack.SetLimit` cắt ngay các snapshot cũ nhất khi vượt ngưỡng mới (không còn trì hoãn sang lần `Push`), luôn giữ ít nhất snapshot hiện tại. |
| Snapshot đầy đủ | `Snapshot` thêm `SelectedId: Guid option` + `CreateWithSelection`; lưu `Annotations`, `CounterIndex`, `SelectedId` để undo/redo phục hồi đúng focus. |
| Z-order | Module `AnnotationZOrder` với `bringForward`/`sendBackward`/`bringToFront`/`sendToBack`; mỗi thao tác đẩy snapshot mới vào undo stack. |

---

## 2. Chi tiết công việc

### 2.1 Cấu hình (`src/FShot.Core/Domain/Config.fs`)
- `AppConfig` thêm `UndoLimit: int` (mặc định `HistoryStack.DefaultLimit` = 100, clamp `[10..500]`).
- `ToSnapshot` map `UndoLimit → HistoryLimit`; `FromSnapshot` map ngược.
- JSON converter đọc/ghi `undoLimit`.

### 2.2 History stack (`src/FShot.Core/Domain/History.fs`)
- `Snapshot` thêm `SelectedId: Guid option`, `CreateWithSelection`.
- `SetLimit` cắt ngay, giữ ít nhất 1 snapshot (không bao giờ rỗng).

### 2.3 Z-order (`src/FShot.Core/Domain/Annotation.fs`)
- Module `AnnotationZOrder` với 4 hàm; sửa lỗi `move` dùng `List.take`/`List.skip` đặt đúng vị trí cuối cùng.

### 2.4 State machine (`src/FShot.Core/State/OverlayState.fs`)
- `restoreFromSnapshot` phục hồi `SelectedAnnotation` + `CounterIndex` từ snapshot.
- `reorderSelected` áp dụng Z-order + đẩy snapshot.
- Handler `BringForward`/`SendBackward`/`BringToFront`/`SendToBack`.

### 2.5 UI (`src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs`)
- Phím `PageUp`/`PageDown`/`Home`/`End` dispatch Z-order.

### 2.6 Unit tests
- `tests/FShot.Core.Tests/Domain/HistoryTests.fs`: viết lại test `SetLimit` cho hành vi cắt ngay + thêm test không rỗng.
- `tests/FShot.Core.Tests/Domain/AnnotationTests.fs`: thêm test 4 hàm Z-order + id không tồn tại.
- `tests/FShot.Core.Tests/State/OverlayStateTests.fs`: thêm test Z-order đẩy snapshot + undo.

---

## 3. Các file mã nguồn thay đổi

1. `src/FShot.Core/Domain/Config.fs` — `UndoLimit`.
2. `src/FShot.Core/Domain/History.fs` — `SelectedId`, `SetLimit`.
3. `src/FShot.Core/Domain/Annotation.fs` — `AnnotationZOrder`.
4. `src/FShot.Core/State/OverlayState.fs` — restore selection, Z-order handlers.
5. `src/FShot.UI/SkiaCanvas/CaptureCanvas.axaml.fs` — phím Z-order.
6. `src/FShot.UI/Windows/SettingsWindow.axaml(.fs)` — slider `UndoLimit`.
7. Tests: `HistoryTests.fs`, `AnnotationTests.fs`, `OverlayStateTests.fs`.

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
| 1 | Đặt UndoLimit=5, vẽ 10 nét, Ctrl+Z chỉ hoàn tác được 5 nét | Thao tác trong UI | Cần GUI |
| 2 | Vẽ rect đè text, chọn text, PageUp đưa lên trên, Ctrl+Z chìm lại | Thao tác trong UI | Cần GUI |

---

## 6. Tiếp theo

- Người dùng thực hiện **Epic 14: Export & Shortcuts Full** (P2.35–P2.39).
