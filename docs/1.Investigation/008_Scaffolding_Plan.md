# Kế hoạch khởi tạo dự án (Scaffolding Plan) - F-Shot

> Tài liệu này mô tả chi tiết cách tổ chức solution, quy tắc phụ thuộc giữa các project, cấu trúc thư mục hoàn chỉnh và bộ lệnh CLI để khởi tạo khung dự án F-Shot trên Windows.  
>
> Đã cập nhật: Tách riêng project `FShot.Rendering.Skia`, chọn target .NET 8 LTS, dùng Argu cho CLI và JSON cho cấu hình.  

## 1. Chuẩn bị môi trường

- **.NET 8 SDK** (phiên bản `8.0.x`).  

- **Avalonia Templates:** Cài đặt template Avalonia mới nhất qua lệnh:

  Bash

  ```powershell
  dotnet new install Avalonia.Templates
  ```

- **Thư mục làm việc:** `D:\Kojin\FShot`.  

## 2. Cấu trúc Solution hoàn chỉnh

Kiến trúc gồm 4 project nguồn và 1 project kiểm thử:  

Plaintext

```
FShot/
├── src/
│   ├── FShot.Core/              (F# Class Library - Pure Domain, 0 external UI/Render libs)
│   │   ├── Domain/              (Capture, Selection, Annotation, History, Export, Config)
│   │   ├── Geometry/            (Point, Rect, ColorRgba, Math helpers)
│   │   └── State/               (OverlayState Machine, Input transition logic)
│   │
│   ├── FShot.Rendering.Skia/    (F# Class Library - SkiaSharp Backend)
│   │   ├── Converters/          (Mapping F# Core Types -> SKTypes)
│   │   ├── Pipeline/            (Skia backing bitmap, Composition pipeline, Smoothing)
│   │   └── Renderers/           (Vẽ Rectangle, Arrow, Pencil, Pixelate filter)
│   │
│   ├── FShot.Platform.Win32/    (C# Class Library - Windows Interop & Net8.0-windows)
│   │   ├── Capture/             (Windows.Graphics.Capture, BitBlt fallback)
│   │   ├── Hotkeys/             (RegisterHotKey P/Invoke)
│   │   ├── Clipboard/           (Ghi Bitmap vào Windows Clipboard)
│   │   └── Shell/               (TrayIcon Interop, Explorer opener)
│   │
│   └── FShot.UI/                (F# Avalonia Desktop App)
│       ├── Controls/            (CaptureCanvas bọc SkiaSharp Canvas vào Avalonia Control)
│       ├── Windows/             (CaptureOverlayWindow borderless topmost, PinWindow)
│       ├── ViewModels/          (OverlayViewModel, ToolbarViewModel)
│       └── Program.fs           (Argu CLI Entry, Avalonia App Startup)
│
├── tests/
│   └── FShot.Core.Tests/        (F# xUnit - Unit test Domain, Geometry & Undo/Redo)
└── FShot.sln
```

## 3. Quy tắc phụ thuộc (Project References)

- **`FShot.Core`:** Không phụ thuộc bất kỳ project hay thư viện ngoài nào (Pure Domain).  
- **`FShot.Rendering.Skia`:** Chỉ tham chiếu `FShot.Core` (và package `SkiaSharp`).  
- **`FShot.Platform.Win32`:** Chỉ tham chiếu `FShot.Core`.  
- **`FShot.UI`:** Tham chiếu cả 3 project: `FShot.Core`, `FShot.Rendering.Skia`, và `FShot.Platform.Win32`.  
- **`FShot.Core.Tests`:** Chỉ tham chiếu `FShot.Core` (chạy test không phụ thuộc native C++ DLLs).  

## 4. Kịch bản lệnh khởi tạo (CLI Scaffolding Script)

Chạy tuần tự các lệnh PowerShell/Bash sau tại thư mục gốc `D:\Kojin\FShot`:

### Bước 4.1: Tạo Solution và Projects

Bash

```bash
# 1. Tạo solution
dotnet new sln -n FShot

# 2. Tạo các projects trong thư mục src/
dotnet new classlib -lang "F#" -o src/FShot.Core
dotnet new classlib -lang "F#" -o src/FShot.Rendering.Skia
dotnet new classlib -lang "C#" -o src/FShot.Platform.Win32
dotnet new avalonia.app -lang "F#" -o src/FShot.UI

# 3. Tạo project test trong tests/
dotnet new xunit -lang "F#" -o tests/FShot.Core.Tests

# 4. Thêm tất cả vào solution
dotnet sln add src/FShot.Core/FShot.Core.fsproj
dotnet sln add src/FShot.Rendering.Skia/FShot.Rendering.Skia.fsproj
dotnet sln add src/FShot.Platform.Win32/FShot.Platform.Win32.csproj
dotnet sln add src/FShot.UI/FShot.UI.fsproj
dotnet sln add tests/FShot.Core.Tests/FShot.Core.Tests.fsproj
```

### Bước 4.2: Thiết lập Project References

Bash

```bash
# Rendering.Skia phụ thuộc Core
dotnet add src/FShot.Rendering.Skia reference src/FShot.Core

# Platform.Win32 phụ thuộc Core
dotnet add src/FShot.Platform.Win32 reference src/FShot.Core

# UI phụ thuộc Core, Rendering và Platform
dotnet add src/FShot.UI reference src/FShot.Core
dotnet add src/FShot.UI reference src/FShot.Rendering.Skia
dotnet add src/FShot.UI reference src/FShot.Platform.Win32

# Tests chỉ phụ thuộc Core
dotnet add tests/FShot.Core.Tests reference src/FShot.Core
```

### Bước 4.3: Cài đặt NuGet Packages

Bash

```bash
# Gói đồ họa cho Rendering
dotnet add src/FShot.Rendering.Skia package SkiaSharp --version 2.88.8

# Gói UI, Skia Control và CLI parser cho UI Shell
dotnet add src/FShot.UI package Avalonia.Controls.Skia --version 11.1.3
dotnet add src/FShot.UI package Argu --version 6.2.4

# Gói kiểm thử F#
dotnet add tests/FShot.Core.Tests package FsUnit.xUnit
```

### Bước 4.4: Tạo cấu trúc thư mục con

Bash

```bash
# Thư mục cho FShot.Core
mkdir -p src/FShot.Core/Geometry
mkdir -p src/FShot.Core/Domain
mkdir -p src/FShot.Core/State

# Thư mục cho FShot.Rendering.Skia
mkdir -p src/FShot.Rendering.Skia/Converters
mkdir -p src/FShot.Rendering.Skia/Pipeline
mkdir -p src/FShot.Rendering.Skia/Renderers

# Thư mục cho FShot.Platform.Win32
mkdir -p src/FShot.Platform.Win32/Capture
mkdir -p src/FShot.Platform.Win32/Hotkeys
mkdir -p src/FShot.Platform.Win32/Clipboard
mkdir -p src/FShot.Platform.Win32/Shell

# Thư mục cho FShot.UI
mkdir -p src/FShot.UI/Controls
mkdir -p src/FShot.UI/Windows
mkdir -p src/FShot.UI/ViewModels
```

## 5. Điều chỉnh Target Framework và Windows TFM

Do `FShot.Platform.Win32` sử dụng WinRT API (`Windows.Graphics.Capture`), hãy mở file `src/FShot.Platform.Win32/FShot.Platform.Win32.csproj` và đổi target sang Windows SDK:  

XML

```xaml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

Đồng thời mở `src/FShot.UI/FShot.UI.fsproj` đổi thành:

XML

```xaml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
  </PropertyGroup>
</Project>
```

## 6. Thứ tự compile files trong các dự án F#

Do trình biên dịch F# yêu cầu file được định nghĩa theo thứ tự phụ thuộc, thứ tự khai báo trong các file `.fsproj` cần được sắp xếp:

**Trong `FShot.Core.fsproj`:**

1. `Geometry/Types.fs` (Point, Rect, ColorRgba)  
2. `Geometry/Math.fs` (Bounding box calculations, Nudge, Clamp)
3. `Domain/Annotation.fs` (Tool DU, StrokeStyle)  
4. `Domain/Selection.fs` (Selection handles, SelectionState)  
5. `Domain/History.fs` (HistoryStack, Undo/Redo logic)  
6. `Domain/Export.fs` (ExportTarget, FilenameToken)  
7. `Domain/Config.fs` (Config Schema, JSON serializer)  
8. `State/OverlayState.fs` (State transitions: Idle -> Selecting -> Annotating)  

**Trong `FShot.Rendering.Skia.fsproj`:**

1. `Converters/SkiaMapping.fs` (Mapping Point/Rect sang SKPoint/SKRect)
2. `Pipeline/Smoothing.fs` (Thuật toán Midpoint Quad Bézier)  
3. `Renderers/ShapeRenderer.fs` (Vẽ đường thẳng, mũi tên, hình chữ nhật)
4. `Renderers/PixelateFilter.fs` (Thao tác byte array trên bitmap)  
5. `Pipeline/CanvasComposer.fs` (Điều phối render các layer)

## 7. PoC Acceptance Checklist 

- [ ] Chạy `dotnet build` toàn bộ solution không có lỗi.  
- [ ] `FShot.Core.Tests` chạy thành công test logic geometry (nudge, resize bounds).  
- [ ] `FShot.Platform.Win32` gọi được capture và trả về mảng byte/con trỏ bitmap của màn hình.  
- [ ] `FShot.UI` mở được cửa sổ không viền phủ toàn màn hình, render thành công ảnh chụp qua SkiaSharp canvas.  
- [ ] Thao tác kéo chuột tạo vùng chọn (bounding box) đạt $\ge$ 60 FPS.  
