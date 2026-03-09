namespace HolidayScheduler.Gui

open Avalonia.Controls
open Avalonia.Markup.Xaml

type SplashWindow() as this =
    inherit Window()

    do
        AvaloniaXamlLoader.Load(this)