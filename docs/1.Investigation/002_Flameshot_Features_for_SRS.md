# Danh sách tính năng Flameshot phục vụ SRS

> Tài liệu này được rút ra từ việc audit mã nguồn `flameshot/src`.  
> Mục đích: cung cấp bộ yêu cầu chức năng đầy đủ để migrate FlameX sang F# / C# / Avalonia trên Windows.

---

## 1. Phạm vi và mục đích

Tài liệu liệt kê toàn bộ tính năng người dùng có thể quan sát, các tùy chọn cấu hình và các điểm tích hợp hệ thống tìm thấy trong Flameshot gốc. Mỗi mục được gán một mã yêu cầu (`CAP-xxx`, `SEL-xxx`, `ANN-xxx`, `OUT-xxx`, `PIN-xxx`, `SYS-xxx`, `CFG-xxx`, `UP-xxx`, `SH-xxx`, `CLI-xxx`, `NF-xxx`) để dễ dàng trace vào SRS, kế hoạch test hoặc backlog.

**Ngoài phạm vi tài liệu này:**
- Kiến trúc triển khai Qt (xem `001_Flameshot_Architecture.md`).
- Quyết định thiết kế UI/UX (màu sắc, font, khoảng cách).
- Packaging, installer và phân phối qua store.

---

## 2. Chú thích ký hiệu

| Ký hiệu | Ý nghĩa |
|---------|---------|
| **M** | Bắt buộc có trong MVP |
| **S** | Nên có trong bản v1.0 |
| **C** | Có thể có / nice-to-have |
| **P** | Phụ thuộc nền tảng (ghi rõ hành vi Windows) |
| **I** | Phụ thuộc Imgur / tính năng upload đám mây |

---

## 3. Các chế độ chụp màn hình (CAP)

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú / Tham chiếu mã nguồn |
|----|-----------|----------------|-------------------------------|
| CAP-001 | **GUI capture** — mở overlay toàn màn hình để chọn vùng và chú thích tương tác. | M | `Flameshot::gui`, `CaptureWidget`, `main.cpp::guiArgument` |
| CAP-002 | **Full-screen capture** — chụp toàn bộ màn hình một lúc, không hiện GUI. | M | `Flameshot::full`, `CaptureRequest::FULLSCREEN_MODE` |
| CAP-003 | **Single-screen capture** — chụp một màn hình cụ thể theo chỉ số, hoặc màn hình đang chứa con trỏ. | M | `Flameshot::screen`, `CaptureRequest::SCREEN_MODE` |
| CAP-004 | **Launcher dialog** — hiển thị hộp thoại chụp với đếm ngược trước khi chụp. | S | `Flameshot::launcher`, `CaptureLauncher` |
| CAP-005 | **Delayed capture** — trì hoãn N mili-giây trước khi thực hiện chụp (áp dụng mọi chế độ). | M | `--delay / -d`, `requestCapture` dùng `QTimer::singleShot` |
| CAP-006 | **Pre-selected monitor** — bỏ qua UI chọn màn hình khi đã truyền `--number` hoặc `selectedMonitor`. | S | `ScreenGrabber::grabEntireDesktop(preSelectedMonitor)` |
| CAP-007 | **Monitor selection UI** — khi không có màn hình được chọn trước trên Windows, hiển thị thumbnail các màn hình để người dùng chọn. | S/P | `ScreenGrabber::createMonitorPreviews`, `selectMonitorAndCrop` |
| CAP-008 | **Active-monitor auto-capture** — tự động chọn màn hình đang chứa con trỏ. | S | `captureActiveMonitor` config (không dùng trên macOS) |
| CAP-009 | **Initial selection / predefined region** — chấp nhận chuỗi `--region WxH+X+Y` hoặc `screen<N>` để đặt sẵn vùng chọn. | S | `--region`, `CaptureRequest::setInitialSelection` |
| CAP-010 | **Last-region recall** — nhớ lại và dùng lại tọa độ vùng chọn trước đó khi bật `saveLastRegion` hoặc truyền `--last-region`. | S | `--last-region`, `saveLastRegion`, `getLastRegion` |
| CAP-011 | **Accept-on-select** — kết thúc chụp ngay khi người dùng vừa kéo xong vùng chọn (`--accept-on-select`). | S | `CaptureRequest::ACCEPT_ON_SELECT` |
| CAP-012 | **Edit mode for screen capture** — mở kết quả của `flameshot screen -e` trong GUI editor thay vì xuất trực tiếp. | S | `--edit` trên lệnh screen |

---

## 4. Vùng chọn và lớp phủ (SEL)

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú |
|----|-----------|----------------|---------|
| SEL-001 | **Darkened overlay** — làm tối phần ngoài vùng chọn, vẫn giữ ảnh desktop đã chụp ở dưới. | M | `drawInactiveRegion`, `contrastOpacity` |
| SEL-002 | **Freehand bounding-box selection** — kéo chuột để tạo hình chữ nhật vùng chọn. | M | `SelectionTool`, `mousePress/Move/Release` |
| SEL-003 | **8 resize handles** — kéo 8 điểm neo (4 cạnh + 4 góc) để thay đổi kích thước vùng chọn. | M | `SelectionWidget::SideType`, 8 hình chữ nhật `m_*Handle` |
| SEL-004 | **Move selection** — kéo bên trong vùng chọn để di chuyển toàn bộ vùng. | M | `MoveTool`, `m_movingSelection` |
| SEL-005 | **Keyboard nudge** — dịch chuyển vùng chọn 1 pixel bằng phím mũi tên. | M | Phím tắt `TYPE_MOVE_*`, `SelectionWidget::moveLeft/Right/Up/Down` |
| SEL-006 | **Keyboard resize** — thay đổi kích thước vùng chọn 1 pixel bằng `Shift + Arrow`. | M | Phím tắt `TYPE_RESIZE_*` |
| SEL-007 | **Symmetric resize** — co giãn đối xứng 2 pixel bằng `Ctrl+Shift+Arrow`. | S | Phím tắt `TYPE_SYM_RESIZE_*` |
| SEL-008 | **Minimum selection size enforcement** — ngăn vùng chọn thu nhỏ quá mức không sử dụng được. | S | `ButtonHandler::ensureSelectionMinimumSize` |
| SEL-009 | **Constrain initial selection to capture area** — giới hạn `--region` nằm trong phạm vi màn hình. | S | `initSelection` với `constrainedToCaptureArea` |
| SEL-010 | **Select all** — mở rộng vùng chọn ra toàn bộ khu vực chụp (`Ctrl+A`). | S | `selectAll()` |
| SEL-011 | **Cancel selection / capture** — xóa vùng chọn hoặc đóng GUI (`Esc`, `Ctrl+Backspace`). | M | `TYPE_CANCEL`, `deleteToolWidgetOrClose` |
| SEL-012 | **Selection geometry display (XYWH)** — hiển thị chiều rộng, chiều cao, tọa độ gần vùng chọn. | S | `showSelectionGeometry`, `showxywh`, `xywh_position` |
| SEL-013 | **XYWH timeout** — tự động ẩn thông tin XYWH sau số mili-giây cấu hình. | C | `showSelectionGeometryHideTime` |
| SEL-014 | **Snap-to-grid** — bắt dính vùng chọn và điểm công cụ vào lưới có thể cấu hình. | C | `m_displayGrid`, `m_gridSize`, `snapToGrid` |
| SEL-015 | **Grid display** — vẽ lưới chấm / lưới ô lên khu vực chụp. | C | `onDisplayGridChanged` |
| SEL-016 | **Mirror resize with Shift** — giữ Shift trong khi kéo một điểm neo sẽ đối xứng co giãn ở điểm đối diện. | S | Ghi chú trong README shortcuts |

---

## 5. Kính lúp và chọn màu (MAG)

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú |
|----|-----------|----------------|---------|
| MAG-001 | **Magnifier widget** — hiển thị vùng pixel được phóng to xung quanh con trỏ, kèm thông tin màu. | S | `MagnifierWidget`, `showMagnifier` |
| MAG-002 | **Square vs circular magnifier** — chuyển đổi hình dạng kính lúp (`squareMagnifier`). | C | `m_square` |
| MAG-003 | **Color under cursor (HEX/RGB)** — hiển thị màu pixel hiện tại trong kính lúp. | S | Lấy mẫu từ `m_screenshot` |
| MAG-004 | **Color picker / eyedropper** — lấy màu từ màn hình và đặt làm màu vẽ đang dùng (`G`). | S | `ColorPicker`, `startColorGrab`, `TYPE_GRAB_COLOR` |
| MAG-005 | **Right-click color wheel** — mở color wheel popup khi click chuột phải. | S | Triển khai qua `ColorPickerWidget` |

---

## 6. Công cụ chú thích (ANN)

Mọi công cụ đều hỗ trợ: màu đang chọn, kích thước/nét có thể cấu hình, preview khi di chuột, undo/redo, và sắp xếp lớp khi áp dụng.

| Mã | Tính năng | Mức độ ưu tiên | Kiểu công cụ | Ghi chú |
|----|-----------|----------------|--------------|---------|
| ANN-001 | **Pencil / freehand pen** — vẽ nét tự do mượt mà. | M | Path tool | `PencilTool` (phím mặc định `P`) |
| ANN-002 | **Line / drawer** — vẽ đường thẳng. | M | Two-point tool | `LineTool` (phím mặc định `D`) |
| ANN-003 | **Arrow** — vẽ mũi tên với đầu mũi tên có thể cấu hình. | M | Two-point tool | `ArrowTool` (phím mặc định `A`) |
| ANN-004 | **Rectangle** — vẽ hình chữ nhật, có thể bo góc. | M | Two-point tool | `RectangleTool` (phím `R`), `drawRectangleSize` điều chỉnh bán kính bo góc |
| ANN-005 | **Circle / ellipse** — vẽ hình tròn / elip; giữ Ctrl để giữ tỷ lệ 1:1. | M | Two-point tool | `CircleTool` (phím `C`) |
| ANN-006 | **Marker / highlighter** — nét bán trong suốt để làm nổi bật. | M | Two-point tool | `MarkerTool` (phím `M`), `drawMarkerSize`, alpha blend |
| ANN-007 | **Text** — thêm hộp văn bản có thể chỉnh font, cỡ, gạch chân, gạch ngang, đậm, nghiêng, căn lề. | M | Widget tool | `TextTool` (phím `T`), `TextConfig` |
| ANN-008 | **Pixelate / mosaic** — làm mờ hoặc pixelate một vùng. | M | Two-point tool | `PixelateTool` (phím `B`), `insecurePixelate`, `drawPixelateSize` |
| ANN-009 | **Invert colors** — đảo màu bên trong một vùng. | S | Two-point tool | `InvertTool` (phím `I`) |
| ANN-010 | **Circle counter / numbered bubble** — đặt các huy hiệu số tự động tăng (1, 2, 3…). | S | Two-point tool | `CircleCountTool` (không có phím tắt mặc định), `circleCount` |
| ANN-011 | **Orthogonal constraint** — giữ Ctrl khi vẽ line/arrow/marker để ràng buộc ngang/dọc/chéo. | S | Phím bổ trợ | `m_supportsOrthogonalAdj`, `m_supportsDiagonalAdj` |
| ANN-012 | **Aspect-ratio constraint** — giữ Ctrl khi vẽ rectangle/circle để giữ tỷ lệ 1:1. | S | Phím bổ trợ | `drawMoveWithAdjustment` |
| ANN-013 | **Tool size by keyboard** — gõ số để đặt chính xác kích thước công cụ. | S | Xử lý phím số trong `keyPressEvent`, `setToolSize` |
| ANN-014 | **Tool size via mouse wheel** — lăn chuột để tăng/giảm độ dày nét. | S | `wheelEvent`, `SizeIncreaseTool`, `SizeDecreaseTool` |
| ANN-015 | **Tool size buttons** — nút toolbar chuyên dụng để tăng/giảm kích thước. | C | `TYPE_SIZEINCREASE`, `TYPE_SIZEDECREASE` |
| ANN-016 | **Arrow style** — chuyển giữa mũi tên mặc định và mũi tên cong. | C | `arrowStyle`, `reverseArrow` |
| ANN-017 | **Reverse arrow** — đảo hướng mũi tên. | C | `reverseArrow` |
| ANN-018 | **Object selection & edit mode** — click vào chú thích cũ để di chuyển/sửa; double-click text để sửa trực tiếp. | S | `selectToolItemAtPos`, `mouseDoubleClickEvent`, `setEditMode` |
| ANN-019 | **Object hit-testing with tolerance** — tìm chú thích gần con trỏ ngay cả khi không click chính xác pixel. | S | `CaptureToolObjects::find`, bán kính tìm kiếm |
| ANN-020 | **Delete current annotation** — xóa đối tượng đang chọn (`Delete` / `Backspace`). | S | `TYPE_DELETE_CURRENT_TOOL` |
| ANN-021 | **Commit active tool** — kết thúc chỉnh sửa text tại chỗ (`Ctrl+Return`). | S | `TYPE_COMMIT_CURRENT_TOOL` |

---

## 7. Thanh công cụ và nút chức năng (TB)

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú |
|----|-----------|----------------|---------|
| TB-001 | **Context-aware tool button bar** — hiển thị thanh nút bo tròn xung quanh vùng chọn. | M | `ButtonHandler`, `CaptureToolButton` |
| TB-002 | **Configurable visible buttons** — người dùng có thể bật/tắt nút nào hiện. | S | Config `buttons`, `ButtonListView` |
| TB-003 | **Hidden-but-shortcut-enabled buttons** — nút có thể bị ẩn nhưng phím tắt vẫn hoạt động. | S | Logic trong `initButtons` |
| TB-004 | **Button positioning** — tự động đặt nút bên trong vùng chọn hoặc dọc theo cạnh tùy không gian. | S | `ButtonHandler::positionButtonsInside`, logic blocked-side |
| TB-005 | **Button right-click for tool size** — click phải vào nút công cụ chọn được để chỉnh kích thước. | C | `handleButtonRightClick` |
| TB-006 | **Tool icon adapts to UI background color** — tự động chọn biểu tượng sáng/tối cho đủ tương phản. | S | `iconPath`, `ColorUtils::colorIsDark` |
| TB-007 | **Size indicator notifier** — hiển thị tạm thời kích thước công cụ khi thay đổi bằng bàn phím/lăn chuột. | C | `NotifierBox`, `updateSizeIndicator` |

---

## 8. Xuất và chia sẻ (OUT)

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú |
|----|-----------|----------------|---------|
| OUT-001 | **Save to file** — ghi ảnh cuối cùng ra đĩa. | M | `SaveTool`, `saveToFilesystem`, `saveToFilesystemGUI` |
| OUT-002 | **Save to configured path** — sử dụng `savePath` và `filenamePattern`. | M | `savePath`, `savePathFixed` |
| OUT-003 | **Save with auto-generated filename** — mẫu tên file dùng token strftime (ví dụ `%F_%H-%M`). | M | `filenamePattern`, `FileNameHandler` |
| OUT-004 | **Save dialog fallback** — hiện hộp thoại lưu khi không có đường dẫn cố định. | M | `ShowSaveFileDialog` |
| OUT-005 | **Clipboard copy** — copy ảnh cuối vào clipboard. | M | `CopyTool`, `FlameshotDaemon::copyToClipboard`, `saveToClipboardMime` |
| OUT-006 | **Copy-on-double-click** — double-click vào vùng chọn để copy mà không cần nhấn nút. | S | `copyOnDoubleClick` |
| OUT-007 | **Save-after-copy** — tự động lưu file sau khi đã copy. | S | `saveAfterCopy` |
| OUT-008 | **Copy-path-after-save** — copy đường dẫn file đã lưu vào clipboard. | S | `copyPathAfterSave` |
| OUT-009 | **Raw PNG to stdout** — xuất byte PNG ra stdout để pipe. | S | `--raw / -r`, `CR::PRINT_RAW` |
| OUT-010 | **Print geometry to stdout** — xuất `WxH+X+Y` của vùng chọn ra stdout. | S | `--print-geometry / -g`, `CR::PRINT_GEOMETRY` |
| OUT-011 | **Open with external application** — mở ảnh bằng trình xem ảnh mặc định hoặc ứng dụng được chọn. | S | `AppLauncher`, `TYPE_OPEN_APP`, `keepOpenAppLauncher` |
| OUT-012 | **Pin to desktop** — tạo cửa sổ nổi luôn nằm trên cùng từ ảnh/vùng đã chụp. | S | `PinTool`, `FlameshotDaemon::createPin` |
| OUT-013 | **File extension preference** — chọn định dạng lưu mặc định (PNG/JPG). | S | `saveAsFileExtension`, `jpegQuality` |
| OUT-014 | **JPG clipboard** — tùy chọn copy dưới dạng JPG thay vì PNG. | C/P | `useJpgForClipboard` (trong gốc không khả dụng trên Windows) |
| OUT-015 | **Notifications on save/copy** — hiển thị toast desktop khi thao tác hoàn tất. | S | `showDesktopNotification` |
| OUT-016 | **Abort notification** — hiển thị toast khi hủy chụp. | C | `showAbortNotification` |

---

## 9. Cửa sổ ghim (Pin Widget) (PIN)

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú |
|----|-----------|----------------|---------|
| PIN-001 | **Floating topmost pin window** — hiển thị vùng đã chụp trong cửa sổ luôn nằm trên cùng, không viền. | S | `PinWidget` |
| PIN-002 | **Drag to reposition** — kéo chuột để di chuyển cửa sổ ghim. | S | `mouseMoveEvent` |
| PIN-003 | **Zoom / scale** — pinch hoặc lăn chuột để phóng to/thu nhỏ ảnh ghim. | S | `pinchTriggered`, `scrollEvent`, `m_scaleFactor` |
| PIN-004 | **Opacity adjustment** — tăng/giảm độ trong suốt cửa sổ ghim. | S | `increaseOpacity`, `decreaseOpacity` |
| PIN-005 | **Rotate left/right** — xoay ảnh ghim theo bước 90°. | S | `rotateLeft`, `rotateRight` |
| PIN-006 | **Copy from pin** — copy ảnh ghim vào clipboard qua menu ngữ cảnh. | S | `copyToClipboard` |
| PIN-007 | **Save from pin** — lưu ảnh ghim qua menu ngữ cảnh. | S | `saveToFile` |
| PIN-008 | **Close pin** — đóng cửa sổ ghim. | S | `closePin`, `mouseDoubleClickEvent` |
| PIN-009 | **Anti-aliasing on zoom** — làm mịn khi phóng to. | C | `antialiasingPinZoom` |
| PIN-010 | **Drop shadow** — hiệu ứng đổ bóng xung quanh cửa sổ ghim. | C | `QGraphicsDropShadowEffect` |

---

## 10. Upload đám mây (UP)

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú |
|----|-----------|----------------|---------|
| UP-001 | **Upload to Imgur** — upload ảnh lên Imgur và nhận URL công khai. | I | `ImgUploaderTool`, `ImgUploaderManager`, `ENABLE_IMGUR` |
| UP-002 | **Upload confirmation dialog** — hỏi người dùng trước khi upload, trừ khi đã tắt. | I | `uploadWithoutConfirmation` |
| UP-003 | **Copy URL after upload** — tự động copy URL trả về vào clipboard. | I | `copyURLAfterUpload` |
| UP-004 | **Upload post-dialog** — sau upload hiển thị URL, mở URL, copy URL, copy ảnh, lưu, xóa. | I | `ImgUploaderBase::showPostUploadDialog` |
| UP-005 | **Upload history** — duyệt và quản lý các ảnh đã upload. | I | `UploadHistory`, `uploadHistoryMax` |
| UP-006 | **Upload client secret** — cấu hình API key Imgur. | I | `uploadClientSecret` |
| UP-007 | **History deletion confirmation** — xác nhận trước khi xóa mục lịch sử. | I | `historyConfirmationToDelete` |

---

## 11. Hoàn tác / làm lại và lịch sử thao tác (UNDO)

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú |
|----|-----------|----------------|---------|
| UNDO-001 | **Undo last annotation/action** — hoàn tác một bước (`Ctrl+Z`). | M | `UndoTool`, `QUndoStack`, `undo()` |
| UNDO-002 | **Redo next annotation/action** — làm lại một bước đã hoàn tác (`Ctrl+Shift+Z`). | M | `RedoTool`, `redo()` |
| UNDO-003 | **Configurable undo limit** — giới hạn kích thước stack hoàn tác (0–999, mặc định 100). | S | `undoLimit` |
| UNDO-004 | **Per-object state snapshots** — lưu toàn bộ danh sách đối tượng chú thích tại mỗi bước. | S | `pushObjectsStateToUndoStack`, `ModificationCommand` |
| UNDO-005 | **Circle-count restoration** — khôi phục chỉ số counter tiếp theo sau undo/redo. | S | `restoreCircleCountState` |
| UNDO-006 | **Layer reordering undo** — hoàn tác việc di chuyển lớp lên/xuống. | S | `onMoveCaptureToolUp/Down` |

---

## 12. Cấu hình (CFG)

Cấu hình được lưu trong file INI (`flameshot.ini`).

### 12.1 Cài đặt chung

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú |
|----|-----------|----------------|---------|
| CFG-001 | **Save path** — thư mục mặc định để lưu ảnh chụp. | M | `savePath` |
| CFG-002 | **Fixed save path** — luôn lưu vào `savePath` mà không hiện hộp thoại. | S | `savePathFixed` |
| CFG-003 | **Filename pattern** — mẫu tên file dùng token strftime, có preview và editor. | M | `filenamePattern`, `FileNameEditor` |
| CFG-004 | **Default file extension** — `.png` hoặc `.jpg`. | S | `saveAsFileExtension` |
| CFG-005 | **JPEG quality** — thanh trượt chất lượng JPG 0–100. | S | `jpegQuality` |
| CFG-006 | **Startup launch** — chạy khi đăng nhập OS. | S/P | `startupLaunch` |
| CFG-007 | **Tray icon toggle** — hiện/ẩn biểu tượng system tray. | S | `disabledTrayIcon` |
| CFG-008 | **Desktop notifications** — bật/tắt thông báo thành công / hủy. | S | `showDesktopNotification`, `showAbortNotification` |
| CFG-009 | **Show help on startup** — hiển thị overlay phím tắt khi GUI mở. | S | `showHelp` |
| CFG-010 | **Show side panel button** — hiển thị nút bật/tắt side panel. | S | `showSidePanelButton` |
| CFG-011 | **Show magnifier** — bật kính lúp theo mặc định. | S | `showMagnifier` |
| CFG-012 | **Square magnifier** — dùng hình vuông cho kính lúp. | C | `squareMagnifier` |
| CFG-013 | **Show quit prompt** — xác nhận trước khi thoát GUI. | C | `showQuitPrompt` |
| CFG-014 | **Copy on double click** — bật double-click để copy. | S | `copyOnDoubleClick` |
| CFG-015 | **Save last region** — nhớ lại vùng chọn cuối. | S | `saveLastRegion` |
| CFG-016 | **Allow multiple GUI instances** — cho phép nhiều `flameshot gui` chạy đồng thời. | S | `allowMultipleGuiInstances` |
| CFG-017 | **Auto-close idle daemon** — tự động đóng daemon nền khi không dùng (trong gốc chỉ Linux). | C/P | `autoCloseIdleDaemon` |
| CFG-018 | **Show startup launch message** — thông báo chào mừng khi chạy lần đầu. | C | `showStartupLaunchMessage` |
| CFG-019 | **Check for updates** — tự động kiểm tra bản phát hành GitHub. | C | `checkForUpdates` ( cờ biên dịch `DISABLE_UPDATE_CHECKER`) |
| CFG-020 | **Ignore update to version** — bỏ qua thông báo cập nhật cho một phiên bản cụ thể. | C | `ignoreUpdateToVersion` |
| CFG-021 | **Use JPG for clipboard** — copy dưới dạng JPG thay vì PNG. | C/P | `useJpgForClipboard` |
| CFG-022 | **Anti-aliasing pin zoom** — làm mịn khi phóng to trong pin widget. | C | `antialiasingPinZoom` |
| CFG-023 | **History confirmation to delete** — xác nhận trước khi xóa lịch sử upload. | I | `historyConfirmationToDelete` |
| CFG-024 | **Upload history max** — giới hạn số lượng bản ghi lịch sử upload. | I | `uploadHistoryMax` |
| CFG-025 | **Upload without confirmation** — bỏ qua hộp thoại xác nhận upload. | I | `uploadWithoutConfirmation` |
| CFG-026 | **Copy URL after upload** — copy URL trả về vào clipboard. | I | `copyURLAfterUpload` |
| CFG-027 | **Upload client secret** — trường nhập API key Imgur. | I | `uploadClientSecret` |
| CFG-028 | **Keep app launcher open** — không đóng launcher sau khi mở ứng dụng ngoài. | C | `keepOpenAppLauncher` |

### 12.2 Cài đặt giao diện

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú |
|----|-----------|----------------|---------|
| CFG-100 | **Main UI color** — màu accent chính. | S | `uiColor` |
| CFG-101 | **Contrast UI color** — màu accent phụ / tối hơn. | S | `contrastUiColor` |
| CFG-102 | **Overlay opacity** — độ mờ phần ngoài vùng chọn (0–255). | S | `contrastOpacity` |
| CFG-103 | **Color palette presets** — bánh xe màu có sẵn nhỏ hoặc lớn. | S | `predefinedColorPaletteLarge` |
| CFG-104 | **User-defined colors** — thêm/xóa/cập nhật màu tùy chỉnh. | S | `userColors`, `ColorPickerEditor` |
| CFG-105 | **Visible toolbar buttons** — sắp xếp và ẩn/hiện các nút chú thích. | S | `buttons`, `ButtonListView` |
| CFG-106 | **Language / i18n** — chọn ngôn ngữ giao diện (`auto` hoặc cụ thể). | S | `uiLanguage` |
| CFG-107 | **Font family** — font mặc định cho công cụ text. | S | `fontFamily` |

### 12.3 Giá trị mặc định của công cụ

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú |
|----|-----------|----------------|---------|
| CFG-200 | **Draw color** — màu chú thích được dùng gần nhất. | M | `drawColor` |
| CFG-201 | **Draw thickness** — độ dày chung cho pencil, line, arrow, selection, circle. | M | `drawThickness` |
| CFG-202 | **Font size** — cỡ chữ mặc định của text tool. | S | `drawFontSize` |
| CFG-203 | **Circle counter size** — cỡ huy hiệu số mặc định. | S | `drawCircleCounterSize` |
| CFG-204 | **Pixelate size** — cỡ ô mosaic mặc định. | S | `drawPixelateSize` |
| CFG-205 | **Rectangle corner size** — bán kính bo góc mặc định. | S | `drawRectangleSize` |
| CFG-206 | **Marker size** — độ dày marker mặc định. | S | `drawMarkerSize` |
| CFG-207 | **Arrow style** — mặc định hoặc cong. | C | `arrowStyle` |
| CFG-208 | **Reverse arrow** — cờ đảo hướng mũi tên mặc định. | C | `reverseArrow` |
| CFG-209 | **Insecure pixelate** — chế độ pixelate nhanh hơn nhưng kém bảo mật hơn. | C | `insecurePixelate` |

---

## 13. Phím tắt (SH)

Tất cả phím tắt đều có thể cấu hình trong tab Shortcuts, trừ các phím nóng toàn hệ thống được ghi chú riêng.

| Mã | Hành động | Phím tắt mặc định | Ghi chú |
|----|-----------|--------------------|---------|
| SH-001 | Kích hoạt Pencil | `P` | |
| SH-002 | Kích hoạt Line/Drawer | `D` | |
| SH-003 | Kích hoạt Arrow | `A` | |
| SH-004 | Kích hoạt Selection | `S` | |
| SH-005 | Kích hoạt Rectangle | `R` | |
| SH-006 | Kích hoạt Circle | `C` | |
| SH-007 | Kích hoạt Marker | `M` | |
| SH-008 | Kích hoạt Text | `T` | |
| SH-009 | Kích hoạt Pixelate | `B` | |
| SH-010 | Kích hoạt Invert | `I` | |
| SH-011 | Di chuyển vùng chọn | `Ctrl+M` | |
| SH-012 | Hoàn tác | `Ctrl+Z` | |
| SH-013 | Làm lại | `Ctrl+Shift+Z` | |
| SH-014 | Copy | `Ctrl+C` | |
| SH-015 | Save | `Ctrl+S` | |
| SH-016 | Thoát / hủy chụp | `Ctrl+Q` / `Ctrl+Backspace` | |
| SH-017 | Mở bằng ứng dụng khác | `Ctrl+O` | không dùng trên macOS |
| SH-018 | Upload lên Imgur | `Return` | nếu bật Imgur |
| SH-019 | Chấp nhận / commit | `Return` | `TYPE_ACCEPT` |
| SH-020 | Bật/tắt side panel | `Space` | |
| SH-021 | Lấy màu | `G` | |
| SH-022 | Dịch vùng chọn 1 px | Phím mũi tên | |
| SH-023 | Thay đổi kích thước vùng chọn 1 px | `Shift + Arrow` | |
| SH-024 | Co giãn đối xứng 2 px | `Ctrl+Shift + Arrow` | |
| SH-025 | Chọn toàn bộ | `Ctrl+A` | |
| SH-026 | Xóa chú thích hiện tại | `Delete` / `Backspace` | Windows: Delete; macOS: Backspace |
| SH-027 | Commit công cụ hiện tại | `Ctrl+Return` | kết thúc chỉnh sửa text |
| SH-028 | Phím nóng toàn cục chụp màn hình | `Win+Shift+X` (Win), `Ctrl+Shift+X` (macOS) | Global, có thể cấu hình trừ Print Screen |
| SH-029 | Phím nóng toàn cục lịch sử | `Alt+Shift+X` (macOS) | nếu bật Imgur |
| SH-030 | Tích hợp Print Screen | `PrtSc` | Windows cố định; Linux cần cấu hình DE |

---

## 14. Tích hợp hệ thống (SYS)

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú |
|----|-----------|----------------|---------|
| SYS-001 | **System tray icon** — biểu tượng tray liên tục với menu ngữ cảnh. | S | `TrayIcon`, `FlameshotDaemon::initTrayIcon` |
| SYS-002 | **Tray menu: Capture** — bắt đầu chụp GUI. | S | `startGuiCapture` |
| SYS-003 | **Tray menu: Capture on screen N** — chụp trên màn hình cụ thể. | S | `initScreenMenu`, action theo từng màn hình |
| SYS-004 | **Tray menu: Launcher** — mở hộp thoại launcher. | S | `m_launcherAction` |
| SYS-005 | **Tray menu: Information** — hiển thị cửa sổ about / phím tắt. | S | `m_infoAction`, `InfoWindow` |
| SYS-006 | **Tray menu: Configuration** — mở cửa sổ cài đặt. | S | `Flameshot::config` |
| SYS-007 | **Tray menu: Open save path** — mở thư mục lưu trong file manager. | S | `openSavePath` |
| SYS-008 | **Tray menu: Exit** — thoát daemon. | S | action exit trong tray |
| SYS-009 | **Global hotkey registration** — đăng ký phím nóng toàn hệ thống cho chụp/lịch sử. | S/P | `QHotkey`, `GlobalShortcutFilter` |
| SYS-010 | **Single-instance enforcement** — ngăn chạy đồng thời nhiều `flameshot gui` trừ khi cho phép. | S | `guiMutexLock`, `QSharedMemory` |
| SYS-011 | **Config file watcher** — tự động tải lại khi file INI thay đổi trên đĩa. | S | `QFileSystemWatcher`, `ConfigHandler` |
| SYS-012 | **Config error resolver** — phát hiện cài đặt không hợp lệ và hiển thị hộp thoại sửa chữa. | S | `ConfigResolver`, `checkForErrors` |
| SYS-013 | **Import/export configuration** — lưu / khôi phục file cấu hình. | C | `importConfiguration`, `exportFileConfiguration` |
| SYS-014 | **Reset configuration** — khôi phục giá trị mặc định. | C | `resetConfiguration` |
| SYS-015 | **DBus interface** — expose capture API trên Linux. | P | `FlameshotDBusAdapter`, chỉ Linux |
| SYS-016 | **Signal daemon handling** — thoát graceful khi nhận SIGINT/SIGTERM. | P | `signaldaemon.cpp`, Unix |
| SYS-017 | **Multi-monitor DPI handling** — xử lý đúng scale trên nhiều màn hình DPI khác nhau. | M | `windowsScreenshot`, logic `devicePixelRatio` |
| SYS-018 | **High-DPI awareness** — tôn trọng hệ số scale của OS cho cả chụp và UI. | M | |
| SYS-019 | **Permission handling** — yêu cầu quyền ghi màn hình trên macOS. | P | `CGPreflightScreenCaptureAccess` |
| SYS-020 | **Windows Print Screen hijack mitigation** — tùy chọn ngăn Windows Snipping Tool chiếm `PrtSc`. | P | `ignorePrntScrForcesSnipping`, `registerMsScreenclip` |
| SYS-021 | **Windows CLI wrapper** — `flameshot-cli.exe` để hiển thị output console. | P | README Windows usage |
| SYS-022 | **Wayland portal capture** — dùng xdg-desktop-portal trên Linux Wayland. | P | `freeDesktopPortal`, `unixScreenshot` |
| SYS-023 | **X11 legacy capture** — bỏ qua portal và dùng Qt X11 grab. | P | `useX11LegacyScreenshot`, `x11LegacyScreenshot` |

---

## 15. Lệnh CLI (CLI)

| Mã | Lệnh | Tùy chọn | Mức độ ưu tiên | Ghi chú |
|----|------|----------|----------------|---------|
| CLI-001 | `flameshot gui` | `-p`, `-c`, `-d`, `--region`, `--last-region`, `--raw`, `--print-geometry`, `--pin`, `--accept-on-select` | M | chụp tương tác |
| CLI-002 | `flameshot full` | `-p`, `-c`, `-d`, `--raw` | M | chụp toàn màn hình |
| CLI-003 | `flameshot screen` | `-n`, `-p`, `-c`, `-d`, `--region`, `--raw`, `--pin`, `--edit` | M | chụp từng màn hình |
| CLI-004 | `flameshot launcher` | — | S | mở hộp thoại launcher |
| CLI-005 | `flameshot config` | `--autostart`, `--notifications`, `--filename`, `--trayicon`, `--showhelp`, `--maincolor`, `--contrastcolor`, `--check` | S | mở hoặc sửa cài đặt |
| CLI-006 | `flameshot` (không tham số) | — | S | chạy daemon nền với tray icon |
| CLI-007 | `--help` / `--version` | — | S | hiển thị trợ giúp / phiên bản |

---

## 16. Yêu cầu phiên chức năng (NF)

| Mã | Yêu cầu | Mức độ ưu tiên | Ghi chú |
|----|---------|----------------|---------|
| NF-001 | **Hiệu năng:** overlay chụp phải render ổn định 60 FPS khi kéo chuột / thay đổi kích thước. | M | Gốc dùng `QPainter` trên backing pixmap. |
| NF-002 | **Độ phản hồi:** phím tắt và thanh công cụ phản hồi trong vòng một frame. | M | |
| NF-003 | **Hỗ trợ nhiều màn hình** — xử lý layout màn hình bất kỳ và mixed DPI. | M | Windows path xử lý DPR từng màn hình. |
| NF-004 | **High-DPI awareness** — tôn trọng hệ số scale của OS cho cả chụp và UI. | M | |
| NF-005 | **Đa ngôn ngữ** — chuỗi giao diện có thể dịch; hỗ trợ định dạng strftime theo locale. | S | Hệ thống dịch Qt trong gốc. |
| NF-006 | **Khả năng tiếp cận:** thao tác chỉ bằng bàn phím cho chọn vùng và hành động phổ biến. | S | |
| NF-007 | **Riêng tư:** công cụ pixelate phải che phủ nội dung an toàn (có chế độ nhanh nhưng kém an toàn). | S | Cờ `insecurePixelate`. |
| NF-008 | **Độ tin cậy:** xử lý gracefully khi chụp màn hình thất bại hoặc bị từ chối quyền. | M | Cờ `ok`, ghi log lỗi. |
| NF-009 | **Riêng tư / Bảo mật:** không ghi log hoặc lưu credential upload đám mây ngoài file config. | I | `uploadClientSecret` lưu trong INI. |
| NF-010 | **Tương thích ngược:** giữ nguyên các key cấu hình và phím tắt hiện có của người dùng. | C | Vấn đề khi migrate. |

---

## 17. Các điểm cần lưu ý trên Windows (WIN)

| Mã | Tính năng | Mức độ ưu tiên | Ghi chú |
|----|-----------|----------------|---------|
| WIN-001 | Sử dụng `Windows.Graphics.Capture` hoặc tương đương để chụp đa màn hình đúng mixed-DPI. | M | Khuyến nghị trong draft kiến trúc. |
| WIN-002 | Đăng ký phím nóng toàn cục `Win+Shift+X` để chụp. | S | P/Invoke `RegisterHotKey`. |
| WIN-003 | Ngăn Windows Snipping Tool chiếm phím `PrtSc`. | C | Tương đương `ignorePrntScrForcesSnipping`. |
| WIN-004 | Xuất console cho các lệnh CLI. | S | Cung cấp CLI wrapper hoặc allocate console. |
| WIN-005 | Đăng ký khởi động cùng Windows qua registry / task scheduler. | S | `startupLaunch`. |
| WIN-006 | Lưu config tại `%APPDATA%\Roaming\flameshot\flameshot.ini`. | S | Đường dẫn gốc trên Windows. |

---

## 18. Tổng hợp số lượng tính năng

| Nhóm | Số lượng |
|------|----------|
| Capture Modes (CAP) | 12 |
| Selection & Overlay (SEL) | 16 |
| Magnifier & Color (MAG) | 5 |
| Annotation Tools (ANN) | 21 |
| Toolbar (TB) | 7 |
| Output & Export (OUT) | 16 |
| Pin Widget (PIN) | 10 |
| Upload (UP) | 7 |
| Undo/Redo (UNDO) | 6 |
| Configuration (CFG) | 35 |
| Keyboard Shortcuts (SH) | 30 |
| System Integration (SYS) | 23 |
| CLI Commands (CLI) | 7 |
| Non-Functional (NF) | 10 |
| Windows-Specific (WIN) | 6 |
| **Tổng cộng** | **~205** |

---

## 19. Ghi chú cho việc migrate sang F# / C# / Avalonia

1. **Mô hình domain bằng F#:** `Tool`, `CaptureMode`, `ExportTask`, `ConfigKey` và danh sách chú thích rất phù hợp với Discriminated Unions và immutable records của F#. Undo/redo có thể mô hình hóa thành `History<T>` của `CaptureToolObjects`.
2. **UI shell bằng C#:** XAML Avalonia, tray icon, P/Invoke phím nóng toàn cục, hộp thoại file và các cờ cửa sổ đặc thù nền tảng dễ viết hơn bằng C# với MVVM hoặc code-behind.
3. **Rendering:** Dùng Skia (`SKBitmap` / `WriteableBitmap`) làm backing store. Vẽ các chú thích lên bitmap mỗi frame; không tạo nhiều `Shape` element Avalonia cho từng nét vẽ.
4. **Chụp màn hình trên Windows:** Ưu tiên `Windows.Graphics.Capture` thay vì `BitBlt` để đảm bảo đúng đa màn hình và mixed DPI.
5. **Clipboard:** Dùng `IClipboard` của Avalonia nhưng đảm bảo data object sống đủ lâu (Windows có thể cần một daemon nền để giữ clipboard, tương tự `FlameshotDaemon`).
6. **Cấu hình:** Ban đầu ánh xạ 1:1 các key INI để giữ cài đặt người dùng; sau đó migrate sang mô hình config kiểu F#.
7. **Phím tắt:** Lưu dưới dạng chuỗi key sequence và bind qua `KeyBinding` của Avalonia hoặc một global hotkey service.
8. **Upload:** Giữ Imgur như một module/plugin tùy chọn; bật/tắt bằng cờ biên dịch hoặc runtime feature flag.

---

*Kết thúc danh sách. Sử dụng các mã yêu cầu để đưa vào các mục 3.x (Yêu cầu chức năng) và 4.x (Yêu cầu phiên chức năng) của SRS.*
