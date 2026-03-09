module HolidayScheduler.Program

open System
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open HolidayScheduler.Logger

[<EntryPoint>]
let main argv =
    // Initialise logger
    let cfg =
        { defaultConfig with
            MinLevel   = Info
            GitHubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN") |> Option.ofObj
            GitHubRepo  = Environment.GetEnvironmentVariable("GITHUB_REPO")  |> Option.ofObj }
    initialize cfg
    let log = instance ()

    log.Info "Holiday Scheduler AI starting"
    log.Statistics()

    try
        let result =
            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(argv)
        log.Info (sprintf "Application exited with code %d" result)
        result
    with ex ->
        log.Critical "Unhandled exception – application crashed" (Some ex)
        1
