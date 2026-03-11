namespace HolidayScheduler.Gui

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Avalonia.Threading
open System
open System.Threading.Tasks

type App() =
    inherit Application()

    let showCrashDialog (ex: exn) =
        Dispatcher.UIThread.Post(fun () ->
            try
                let reportPath = Support.saveCrashReport ex
                let dialog = CrashReportWindow(ex, reportPath)

                match Application.Current.ApplicationLifetime with
                | :? IClassicDesktopStyleApplicationLifetime as desktop when not (isNull desktop.MainWindow) ->
                    dialog.ShowDialog(desktop.MainWindow) |> ignore
                | _ ->
                    dialog.Show()
            with _ ->
                ())

    let installCrashReporting () =
        AppDomain.CurrentDomain.UnhandledException.Add(fun args ->
            match args.ExceptionObject with
            | :? exn as ex -> showCrashDialog ex
            | _ -> ())

        TaskScheduler.UnobservedTaskException.Add(fun args ->
            showCrashDialog args.Exception
            args.SetObserved())

    override this.Initialize() =
        AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        installCrashReporting ()

        let crashTestMode =
            let value = Environment.GetEnvironmentVariable("HOLIDAY_CRASH_TEST")
            if String.IsNullOrWhiteSpace(value) then ""
            else value.Trim().ToLowerInvariant()

        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            let splash = SplashWindow()
            splash.SetVersion($"Version {Support.currentVersionText()}")
            splash.SetStatus("Starte Anwendung...")
            splash.SetStatus("Initialisiere Komponenten...")
            splash.SetStatus("Suche nach der neuesten Update-Version...")
            splash.SetProgress(20.0)
            splash.Show()

            Dispatcher.UIThread.Post((fun () ->
                try
                    splash.SetStatus("Lade Hauptfenster...")
                    splash.SetStatus("Übergebe Update-Prüfung an Hauptfenster...")
                    splash.SetProgress(70.0)
                    let main = MainWindow(splash)
                    desktop.MainWindow <- main
                    main.Show()

                    if crashTestMode = "1" || crashTestMode = "dialog" then
                        Dispatcher.UIThread.Post((fun () ->
                            showCrashDialog (Exception("Intentional crash dialog test (HOLIDAY_CRASH_TEST=dialog)"))), DispatcherPriority.Background)
                    elif crashTestMode = "hard" then
                        Dispatcher.UIThread.Post((fun () ->
                            raise (Exception("Intentional hard crash test (HOLIDAY_CRASH_TEST=hard)"))), DispatcherPriority.Background)
                finally
                    splash.SetProgress(100.0)
                    // Don't close splash here; MainWindow will close it after update check.
                    ()), DispatcherPriority.Background)
        | _ -> ()

        base.OnFrameworkInitializationCompleted()