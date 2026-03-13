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
            splash.SetStatus("Starte Anwendung...")
            splash.SetStatus("Initialisiere Komponenten...")
            splash.SetStatus("Suche nach der neuesten Update-Version...")
            splash.SetProgress(20.0)
            splash.Show()

            // Simulate request/response status updates during update check
            let updateCheck = async {
                let delay = 2000 // 2 seconds per step
                splash.SetStatus("Request: Checking for updates...")
                do! Async.Sleep(delay)
                splash.SetStatus("Response: No updates found.")
                splash.SetProgress(40.0)
                do! Async.Sleep(delay)
                splash.SetStatus("Request: Checking server status...")
                do! Async.Sleep(delay)
                splash.SetStatus("Response: Server online.")
                splash.SetProgress(60.0)
                do! Async.Sleep(delay)
                splash.SetStatus("Request: Validating license...")
                do! Async.Sleep(delay)
                splash.SetStatus("Response: License valid.")
                splash.SetProgress(80.0)
                do! Async.Sleep(delay)
                splash.SetStatus("Update check complete.")
                splash.SetProgress(100.0)
            }

            Async.StartImmediate(async {
                do! updateCheck
                Dispatcher.UIThread.Post(fun () ->
                    let main = MainWindow()
                    desktop.MainWindow <- main
                    main.Show()
                    splash.Close()
                )
            })

            if crashTestMode = "1" || crashTestMode = "dialog" then
                Dispatcher.UIThread.Post((fun () ->
                    showCrashDialog (Exception("Intentional crash dialog test (HOLIDAY_CRASH_TEST=dialog)"))), DispatcherPriority.Background)
            elif crashTestMode = "hard" then
                Dispatcher.UIThread.Post((fun () ->
                    raise (Exception("Intentional hard crash test (HOLIDAY_CRASH_TEST=hard)"))), DispatcherPriority.Background)
        | _ -> ()

        base.OnFrameworkInitializationCompleted()