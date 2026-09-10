namespace FShot.UI.Windows

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

type CaptureOverlayWindow () as this = 
    inherit Window ()

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
