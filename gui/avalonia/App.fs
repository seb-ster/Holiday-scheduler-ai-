namespace HolidayScheduler.Gui

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Avalonia.Threading
open System.Threading.Tasks

type App() =
    inherit Application()

    override this.Initialize() =
        AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            let splash = SplashWindow()
            splash.Show()

            Task.Run(fun () ->
                task {
                    do! Task.Delay(1200)
                    Dispatcher.UIThread.Post(fun () ->
                        try
                            let main = MainWindow()
                            desktop.MainWindow <- main
                            main.Show()
                        finally
                            splash.Close())
                }
                :> Task)
            |> ignore
        | _ -> ()

        base.OnFrameworkInitializationCompleted()