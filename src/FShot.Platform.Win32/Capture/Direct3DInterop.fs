namespace FShot.Platform.Win32.Capture

open System
open System.Runtime.InteropServices
open WinRT
open Windows.Graphics.Capture
open Windows.Graphics.DirectX
open Windows.Graphics.DirectX.Direct3D11
open Vortice.Direct3D
open Vortice.Direct3D11
open Vortice.DXGI

/// COM interop cho `IGraphicsCaptureItemInterop` (GUID cố định từ Windows SDK).
/// Cung cấp `CreateForWindow` / `CreateForMonitor` để tạo GraphicsCaptureItem từ HWND/HMONITOR.
/// Xem tài liệu 02_05_WindowsGraphicsCapture.md, mục 2.
[<ComImport; Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356"); InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>]
type private IGraphicsCaptureItemInterop =
    abstract CreateForWindow : nativeint * Guid byref * nativeint byref -> int
    abstract CreateForMonitor : nativeint * Guid byref * nativeint byref -> int

/// Các helper Direct3D 11 + WinRT interop phục vụ backend Windows.Graphics.Capture.
/// Quản lý vòng đời tài nguyên unmanaged theo thiết kế 10_02_CaptureAdapter.md, mục 4.1.
[<RequireQualifiedAccess>]
module Direct3DInterop =

    /// IID của IGraphicsCaptureItem (Windows.Graphics.Capture.IGraphicsCaptureItem).
    let private iidGraphicsCaptureItem = Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760")

    /// Khởi tạo Direct3D 11 hardware device (DriverType.Hardware) hỗ trợ BGRA.
    let createDevice () : ID3D11Device =
        D3D11.D3D11CreateDevice(
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            [| FeatureLevel.Level_11_0; FeatureLevel.Level_10_1; FeatureLevel.Level_10_0 |])

    /// Bọc ID3D11Device sang WinRT IDirect3DDevice qua DXGI Interop.
    let createDirect3DDevice (device: ID3D11Device) : IDirect3DDevice =
        let dxgiDevice = device.QueryInterface<IDXGIDevice>()
        D3D11.CreateDirect3D11DeviceFromDXGIDevice<IDirect3DDevice>(dxgiDevice)

    /// Tạo GraphicsCaptureItem cho một màn hình (HMONITOR) thông qua IGraphicsCaptureItemInterop.
    let createItemForMonitor (hmonitor: nativeint) : GraphicsCaptureItem =
        use factoryRef = ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem")
        let interop = Marshal.GetObjectForIUnknown(factoryRef.ThisPtr) :?> IGraphicsCaptureItemInterop
        let mutable iid = iidGraphicsCaptureItem
        let mutable resultPtr = IntPtr.Zero
        let hr = interop.CreateForMonitor(hmonitor, &iid, &resultPtr)
        if hr < 0 then
            Marshal.ThrowExceptionForHR(hr)
        GraphicsCaptureItem.FromAbi(resultPtr)

    /// Kiểm tra hệ điều hành có hỗ trợ Windows.Graphics.Capture hay không.
    /// Windows 10 build >= 18362 (1903) và GraphicsCaptureSession.IsSupported().
    let isSupported () : bool =
        try
            let osVersion = Environment.OSVersion.Version
            let build = osVersion.Build
            build >= 18362 && GraphicsCaptureSession.IsSupported()
        with _ ->
            false

    /// Đọc raw pixel BGRA32 từ một Direct3DSurface (frame WGC) bằng Staging Texture.
    /// Quy trình: GetDXGISurface -> CopyResource -> Map -> read -> Unmap (02_05, mục 4).
    let readPixels (device: ID3D11Device) (surface: IDirect3DSurface) (width: int) (height: int) : byte[] =
        let dxgiSurface = D3D11.GetDXGISurface(surface)
        let sourceTexture = dxgiSurface.QueryInterface<ID3D11Texture2D>()
        try
            let staging =
                device.CreateTexture2D(
                    Format.B8G8R8A8_UNorm,
                    uint32 width,
                    uint32 height,
                    1u,
                    1u,
                    null,
                    BindFlags.None,
                    ResourceOptionFlags.None,
                    ResourceUsage.Staging,
                    CpuAccessFlags.Read)

            use stagingTexture = staging
            let context = device.ImmediateContext
            context.CopyResource(stagingTexture, sourceTexture)

            let mapped = context.Map(stagingTexture, 0u, MapMode.Read, MapFlags.None)
            try
                let stride = width * 4
                let total = stride * height
                let bytes = Array.zeroCreate<byte> total

                // Copy theo từng dòng để tôn trọng RowPitch (có thể lớn hơn stride do alignment).
                if int mapped.RowPitch = stride then
                    Marshal.Copy(mapped.DataPointer, bytes, 0, total)
                else
                    for row in 0 .. height - 1 do
                        Marshal.Copy(
                            IntPtr.Add(mapped.DataPointer, row * int mapped.RowPitch),
                            bytes,
                            row * stride,
                            stride)

                bytes
            finally
                context.Unmap(stagingTexture, 0u)
        finally
            sourceTexture.Dispose()
