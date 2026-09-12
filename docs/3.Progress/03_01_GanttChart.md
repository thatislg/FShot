# F-Shot Lộ trình Gantt Chart

> Biểu đồ Gantt tổng quan cho 4 Phase của F-Shot.
> Ngày bắt đầu dự án giả định: 03/09/2026.

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#e1f5fe', 'primaryTextColor': '#01579b', 'primaryBorderColor': '#0288d1', 'lineColor': '#0288d1', 'secondaryColor': '#fff3e0', 'tertiaryColor': '#e8f5e9'}}}%%
gantt
    title F-Shot Development Roadmap
    dateFormat YYYY-MM-DD
    axisFormat %d/%m

    section Phase 0
    PoC & Verification           :done, p0, 2026-09-03, 2026-09-16

    section Phase 1
    P1.01 Domain Model          :active, p101, 2026-09-17, 5d
    P1.02 HistoryStack          :p102, after p101, 4d
    P1.03 OverlayState Machine  :p103, after p101, 6d
    P1.04 Unit Tests            :p104, after p103, 5d
    P1.05 Selection Dimming     :p105, after p104, 3d
    P1.06-P1.08 Selection Engine :p106, after p105, 7d
    P1.09-P1.11 Keyboard Selection :p107, after p106, 4d
    P1.12-P1.20 Annotation Tools :p108, after p107, 10d
    P1.21-P1.23 Undo/Redo & Toolbar :p109, after p108, 5d
    P1.24-P1.29 Export & CLI    :p110, after p109, 6d
    MVP Integration & Test       :p111, after p110, 7d

    section Phase 2
    P2.01-P2.03 System Tray     :p201, after p111, 5d
    P2.04-P2.06 Global Hotkeys  :p202, after p201, 5d
    P2.07-P2.10 Real Capture & Mixed DPI :p203, after p202, 7d
    P2.11-P2.14 Config & Editor  :p204, after p203, 7d
    P2.15-P2.17 Pin Widget       :p205, after p204, 6d
    P2.20-P2.26 Advanced Tools   :p206, after p205, 8d
    P2.27-P2.31 Selection Advanced :p207, after p206, 5d
    P2.32-P2.34 Undo Advanced   :p208, after p207, 4d
    P2.35-P2.39 Export & Shortcuts :p209, after p208, 5d
    P2.40-P2.43 i18n & Windows Integration :p210, after p209, 6d
    v1.0 Integration & Test    :p211, after p210, 7d

    section Phase 3
    P3.01-P3.02 Imgur Upload    :p301, after p211, 7d
    P3.03-P3.04 Precision Tools :p302, after p301, 6d
    P3.05-P3.07 Customization   :p303, after p302, 7d
    P3.08 Advanced Settings     :p304, after p303, 5d
    v1.x Polish & Release      :p305, after p304, 7d

    section Milestones
    PoC Complete                :milestone, after p0, 0d
    MVP Core Complete           :milestone, after p111, 0d
    Windows v1.0 Complete       :milestone, after p211, 0d
    v1.x Advanced Complete      :milestone, after p305, 0d
```

---

## Ghi chú

- **Phase 0**: 03/09/2026 → 16/09/2026 — **đã hoàn thành**.
- **Phase 1**: 17/09/2026 → 10/10/2026 — đang bắt đầu với P1.01.
- **Phase 2**: 11/10/2026 → 07/11/2026 — phụ thuộc hoàn thành Phase 1.
- **Phase 3**: 08/11/2026 → 28/11/2026 — phụ thuộc hoàn thành Phase 2.

Mỗi task trong Gantt tương ứng với các mục trong `03_00_Progres_Overview.md`.
