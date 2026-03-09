namespace HolidayScheduler.Gui

open Avalonia
open Avalonia.ReactiveUI

module Program =
    [<EntryPoint>]
    let main args =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI()
            .StartWithClassicDesktopLifetime(args)