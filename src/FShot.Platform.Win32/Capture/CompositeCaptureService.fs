namespace FShot.Platform.Win32.Capture

open System
open FShot.Core.Domain
open FShot.Platform.Win32.Config

/// Lựa chọn backend chụp từ cấu hình người dùng.
[<RequireQualifiedAccess>]
type CaptureBackendChoice =
    | Auto
    | Wgc
    | Gdi

    /// Phân giải chuỗi cấu hình "Auto" | "Wgc" | "Gdi" (không phân biệt hoa thường).
    static member FromString(value: string) : CaptureBackendChoice =
        match (if isNull value then "" else value).Trim().ToLowerInvariant() with
        | "wgc" -> Wgc
        | "gdi" -> Gdi
        | _ -> Auto

/// Bộ điều phối đa backend: tự động chọn Windows.Graphics.Capture (chính) hoặc GDI BitBlt (dự phòng).
/// Xem tài liệu 10_02_CaptureAdapter.md và 02_06_FallbackBitBlt.md.
type CompositeCaptureService() =

    [<DefaultValue>]
    static val mutable private _forceGdi: bool

    /// Ép dùng GDI BitBlt (dùng cho cờ CLI --force-gdi).
    static member ForceGdi
        with get() = CompositeCaptureService._forceGdi
        and set(value) = CompositeCaptureService._forceGdi <- value

    let gdiService = WindowsCaptureService() :> ICaptureService
    let wgcService = WgcCaptureService() :> ICaptureService

    /// Thực thi WGC, tự động chuyển sang GDI khi không hỗ trợ hoặc thất bại.
    let tryCapture
        (wgcFn: unit -> Async<Result<CaptureResult, CaptureError>>)
        (gdiFn: unit -> Async<Result<CaptureResult, CaptureError>>)
        : Async<Result<CaptureResult, CaptureError>> =
        async {
            let backend =
                if CompositeCaptureService.ForceGdi then
                    CaptureBackendChoice.Gdi
                else
                    CaptureBackendChoice.FromString (ConfigStore.loadConfig().CaptureBackend)

            match backend with
            | CaptureBackendChoice.Gdi ->
                return! gdiFn()

            | CaptureBackendChoice.Wgc ->
                let! result =
                    async {
                        try
                            return! wgcFn()
                        with ex ->
                            return Error (CaptureError.Unknown ex.Message)
                    }
                return result

            | CaptureBackendChoice.Auto ->
                if WgcCaptureService.IsSupported then
                    let! wgcResult =
                        async {
                            try
                                return! wgcFn()
                            with ex ->
                                return Error (CaptureError.Unknown ex.Message)
                        }
                    match wgcResult with
                    | Ok r -> return Ok r
                    | Error err ->
                        System.Diagnostics.Trace.WriteLine(sprintf "[Capture] WGC failed (%A); falling back to GDI BitBlt" err)
                        return! gdiFn()
                else
                    System.Diagnostics.Trace.WriteLine("[Capture] WGC not supported; using GDI BitBlt")
                    return! gdiFn()
        }

    interface ICaptureService with

        member _.CaptureScreenAsync(screenIndex: int) =
            tryCapture
                (fun () -> wgcService.CaptureScreenAsync screenIndex)
                (fun () -> gdiService.CaptureScreenAsync screenIndex)

        member _.CaptureCursorScreenAsync() =
            tryCapture
                (fun () -> wgcService.CaptureCursorScreenAsync())
                (fun () -> gdiService.CaptureCursorScreenAsync())

        member _.CaptureVirtualScreenAsync() =
            tryCapture
                (fun () -> wgcService.CaptureVirtualScreenAsync())
                (fun () -> gdiService.CaptureVirtualScreenAsync())
