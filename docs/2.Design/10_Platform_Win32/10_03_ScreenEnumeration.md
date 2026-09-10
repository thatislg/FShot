# ScreenEnumeration — Thiết kế chi tiết

> Tài liệu này mô tả cách F-Shot liệt kê màn hình và lấy thông tin DPI trên Windows.
> Nằm trong `FShot.Platform.Win32`.

---

## 1. Mục đích

Cung cấp danh sách màn hình đang kết nối với thông tin:

- Vị trí trong Virtual Screen space.
- Kích thước logical và physical.
- Scale factor DPI.
- Màn hình chính hay phụ.

---

## 2. Cấu trúc dữ liệu

```
ScreenInfo
├── Index: int
├── Name: string
├── IsPrimary: bool
├── VirtualBounds: Rect (logical)
├── ScaleFactor: ScaleFactor
└── PhysicalSize: (width, height)
```

`PhysicalSize` được tính từ `VirtualBounds` và `ScaleFactor`:

`physicalWidth = round(virtualWidth * scaleFactor)`
`physicalHeight = round(virtualHeight * scaleFactor)`

Ví dụ: màn hình logical 1920×1080, scale 1.5 → physical 2880×1620.

---

## 3. Cách lấy thông tin

Dùng Win32 API:

- `EnumDisplayMonitors`: liệt kê các màn hình, lấy `left/top/right/bottom` logical.
- `GetDpiForMonitor` (Shcore.dll): lấy DPI theo chiều ngang/dọc.
- `scaleFactor = dpi / 96`.

---

## 4. Công thức Virtual Screen

`virtualLeft = min(left của tất cả màn hình)`
`virtualTop = min(top của tất cả màn hình)`
`virtualRight = max(right của tất cả màn hình)`
`virtualBottom = max(bottom của tất cả màn hình)`

---

## 5. Tìm màn hình chứa điểm

Điểm `(x, y)` thuộc màn hình có bounds `(L, T, R, B)` khi:

`x >= L && x < R`
`y >= T && y < B`

---

## 6. Kết nối với code

- `src/FShot.Platform.Win32/Screen/ScreenInfo.fs`
- `src/FShot.Platform.Win32/Screen/ScreenEnumeration.fs`
- Dùng trong `CaptureService` để chọn màn hình cần chụp.
