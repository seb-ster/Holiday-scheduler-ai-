namespace HolidayScheduler.Gui

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Avalonia.Threading
open System
open System.Threading.Tasks

type App() =
    inherit Application()

    let installCrashReporting () =
        Dispatcher.UIThread.UnhandledException.Add(fun args ->
            Support.saveCrashReport args.Exception |> ignore)

        AppDomain.CurrentDomain.UnhandledException.Add(fun args ->
            match args.ExceptionObject with
            | :? exn as exception -> Support.saveCrashReport exception |> ignore
            | _ -> ())

        TaskScheduler.UnobservedTaskException.Add(fun args ->
            Support.saveCrashReport args.Exception |> ignore
            args.SetObserved())

    override this.Initialize() =
        AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        installCrashReporting ()

        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            let splash = SplashWindow()
            splash.SetVersion($"Version {Support.currentVersionText()}")
            splash.SetStatus("Starte Anwendung...")
            splash.SetProgress(20.0)
            splash.Show()

            Dispatcher.UIThread.Post((fun () ->
                try
                    splash.SetStatus("Lade Hauptfenster...")
                    splash.SetProgress(70.0)
                    let main = MainWindow()
                    desktop.MainWindow <- main
                    main.Show()
                finally
                    splash.SetProgress(100.0)
                    splash.Close()), DispatcherPriority.Background)
        | _ -> ()

        base.OnFrameworkInitializationCompleted()