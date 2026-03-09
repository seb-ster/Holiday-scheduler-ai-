namespace HolidayScheduler

open System
open System.IO
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Threading
open HolidayScheduler.Strategies
open HolidayScheduler.Logger

// ── Row model for the DataGrid ────────────────────────────────────────────────

type RosterRow =
    { EmployeeId : string
      StartDate  : string
      EndDate    : string
      Priority   : int
      Reward     : string
      PlacedAt   : string }

// ── Evolution history entry ───────────────────────────────────────────────────

type EvolutionEntry =
    { Generation  : int
      BestReward  : float
      Scheduled   : int }

// ── Main Window ──────────────────────────────────────────────────────────────

type MainWindow() as this =
    inherit Window()

    let sampleEmployees = [| "Alice"; "Bob"; "Carol"; "Dave"; "Eve"; "Frank"; "Grace"; "Henry" |]

    // Mutable scheduler state
    let mutable population       : Population = createPopulation 6
    let mutable generation       : int = 1
    let mutable state : SchedulerState =
        { Queue    = []
          Schedule = []
          Blocked  = Set.empty
          Capacity = 3 }
    let mutable isEvolutionRunning = false
    // Most-recent entry first; prepend for O(1) insertion
    let mutable evolutionHistory : EvolutionEntry list = []

    let log = Logger.instance ()

    do
        AvaloniaXamlLoader.Load(this)
        this.InitializeComponent()

    member private this.InitializeComponent() =
        // Wire up sidebar action buttons
        let btnEvolve     = this.FindControl<Button>("BtnRunEvolution")
        let btnSample     = this.FindControl<Button>("BtnAddSample")
        let btnAddBulk    = this.FindControl<Button>("BtnAddBulk")
        let btnClearQueue = this.FindControl<Button>("BtnClearQueue")

        if btnEvolve     <> null then btnEvolve.Click.Add(fun _ -> this.OnRunEvolution())
        if btnSample     <> null then btnSample.Click.Add(fun _ -> this.OnAddSampleRequest())
        if btnAddBulk    <> null then btnAddBulk.Click.Add(fun _ -> this.OnAddBulkRequests())
        if btnClearQueue <> null then btnClearQueue.Click.Add(fun _ -> this.OnClearQueue())

        // Wire up Menu items defined in XAML
        let wireMenuItem (name : string) (handler : unit -> unit) =
            let item = this.FindControl<MenuItem>(name)
            if item <> null then
                item.Click.Add(fun _ -> handler())

        wireMenuItem "MenuFileNew"      (fun () -> this.OnNewSchedule())
        wireMenuItem "MenuFileExport"   (fun () -> this.OnExportCsv())
        wireMenuItem "MenuFileExit"     (fun () -> this.Close())
        wireMenuItem "MenuEditAdd"      (fun () -> this.OnAddSampleRequest())
        wireMenuItem "MenuEditAddBulk"  (fun () -> this.OnAddBulkRequests())
        wireMenuItem "MenuEditClear"    (fun () -> this.OnClearQueue())
        wireMenuItem "MenuEditEvolve"   (fun () -> this.OnRunEvolution())
        wireMenuItem "MenuViewRoster"   (fun () -> ())
        wireMenuItem "MenuHelpAbout"    (fun () -> this.ShowAbout())
        wireMenuItem "MenuHelpLogs"     (fun () -> log.Statistics())

        // Set initial log path in status bar
        match log.LogPath with
        | Some p -> this.UpdateStatus (sprintf "Log: %s" p) ""
        | None   -> this.UpdateStatus "Log: console only" ""

        this.RefreshUI()
        log.Info "MainWindow initialized"

    member private this.OnNewSchedule() =
        state            <- { Queue = []; Schedule = []; Blocked = Set.empty; Capacity = 3 }
        population       <- createPopulation 6
        generation       <- 1
        evolutionHistory <- []
        log.Info "New schedule started"
        this.RefreshUI()

    member private this.OnAddSampleRequest() =
        let rng = Random()
        let today    = DateOnly.FromDateTime(DateTime.Today)
        let startOff = rng.Next(1, 90)
        let duration = rng.Next(1, 14)
        let req =
            { RequestId   = Guid.NewGuid()
              EmployeeId  = sampleEmployees.[rng.Next(sampleEmployees.Length)]
              StartDate   = today.AddDays(startOff)
              EndDate     = today.AddDays(startOff + duration)
              Priority    = rng.Next(1, 6)
              SubmittedAt = DateTime.UtcNow }
        state <- { state with Queue = state.Queue @ [ req ] }
        log.Info (sprintf "Added request for %s (%s → %s)"
                      req.EmployeeId
                      (req.StartDate.ToString("yyyy-MM-dd"))
                      (req.EndDate.ToString("yyyy-MM-dd")))
        this.RefreshUI()

    member private this.OnAddBulkRequests() =
        let rng   = Random()
        let today = DateOnly.FromDateTime(DateTime.Today)
        let newReqs =
            List.init 10 (fun _ ->
                let startOff = rng.Next(1, 180)
                let duration = rng.Next(1, 21)
                { RequestId   = Guid.NewGuid()
                  EmployeeId  = sampleEmployees.[rng.Next(sampleEmployees.Length)]
                  StartDate   = today.AddDays(startOff)
                  EndDate     = today.AddDays(startOff + duration)
                  Priority    = rng.Next(1, 6)
                  SubmittedAt = DateTime.UtcNow })
        state <- { state with Queue = state.Queue @ newReqs }
        log.Info (sprintf "Added %d sample requests to queue" newReqs.Length)
        this.RefreshUI()

    member private this.OnClearQueue() =
        let cleared = List.length state.Queue
        state <- { state with Queue = [] }
        log.Info (sprintf "Cleared %d requests from queue" cleared)
        this.RefreshUI()

    member private this.OnExportCsv() =
        if List.isEmpty state.Schedule then
            log.Warning "Export: schedule is empty, nothing to export"
        else
            try
                let desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                let timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss")
                let path = Path.Combine(desktop, sprintf "holiday-schedule-%s.csv" timestamp)
                let header = "EmployeeId,StartDate,EndDate,Priority,Reward,PlacedAt"
                let lines =
                    state.Schedule
                    |> List.sortByDescending (fun s -> s.PlacedAt)
                    |> List.map (fun s ->
                        sprintf "%s,%s,%s,%d,%.2f,%s"
                            s.Request.EmployeeId
                            (s.Request.StartDate.ToString("yyyy-MM-dd"))
                            (s.Request.EndDate.ToString("yyyy-MM-dd"))
                            s.Request.Priority
                            s.Reward
                            (s.PlacedAt.ToString("yyyy-MM-dd HH:mm:ss")))
                File.WriteAllLines(path, header :: lines)
                log.Info (sprintf "Schedule exported to %s (%d rows)" path lines.Length)
                this.UpdateStatus (sprintf "Exported: %s" path) ""
            with ex ->
                log.Error (sprintf "Export failed: %s" ex.Message)

    member private this.OnRunEvolution() =
        if isEvolutionRunning then () else

        let btnEvolve  = this.FindControl<Button>("BtnRunEvolution")
        let menuEvolve = this.FindControl<MenuItem>("MenuEditEvolve")

        let setControlsEnabled enabled =
            btnEvolve  |> Option.ofObj |> Option.iter (fun b -> b.IsEnabled <- enabled)
            menuEvolve |> Option.ofObj |> Option.iter (fun m -> m.IsEnabled <- enabled)

        isEvolutionRunning <- true
        setControlsEnabled false

        let capturedPop   = population
        let capturedState = state
        let capturedGen   = generation

        log.Info (sprintf "Starting evolution – generation %d, population %d, queue %d"
                      capturedGen (List.length capturedPop) (List.length capturedState.Queue))

        async {
            do! Async.SwitchToThreadPool()
            try
                let (newPop, newState) =
                    evolveGeneration capturedPop capturedState 3 (List.length capturedPop) capturedGen

                Dispatcher.UIThread.Post(fun () ->
                    let bestReward =
                        if List.isEmpty newPop then 0.0
                        else
                            newPop
                            |> List.maxBy (fun a -> a.Metrics.AverageReward)
                            |> fun a -> a.Metrics.AverageReward

                    let entry =
                        { Generation = capturedGen
                          BestReward = bestReward
                          Scheduled  = List.length newState.Schedule }
                    // Prepend for O(1) insertion; history is most-recent-first
                    evolutionHistory <- entry :: evolutionHistory

                    population <- newPop
                    state      <- newState
                    generation <- capturedGen + 1
                    isEvolutionRunning <- false
                    setControlsEnabled true
                    log.Info (sprintf "Evolution complete – generation %d, scheduled %d, best reward %.2f"
                                  (capturedGen + 1) (List.length newState.Schedule) bestReward)
                    this.RefreshUI()
                )
            with ex ->
                log.Error (sprintf "Evolution failed: %s" ex.Message)
                Dispatcher.UIThread.Post(fun () ->
                    isEvolutionRunning <- false
                    setControlsEnabled true
                )
        }
        |> Async.Start

    member private this.RefreshUI() =
        Dispatcher.UIThread.Post(fun () ->
            // Update sidebar
            this.SetText "SidebarGeneration"  (string generation)
            this.SetText "SidebarPopulation"  (sprintf "%d agents" (List.length population))

            let bestReward =
                if List.isEmpty population then 0.0
                else
                    population
                    |> List.maxBy (fun a -> a.Metrics.AverageReward)
                    |> fun a -> a.Metrics.AverageReward
            this.SetText "SidebarBestReward"  (sprintf "%.2f" bestReward)
            this.SetText "SidebarQueueCount"  (string (List.length state.Queue))
            this.SetText "SidebarScheduled"   (string (List.length state.Schedule))

            // Update evolution history display (history is most-recent-first)
            let historyText =
                if List.isEmpty evolutionHistory then "—"
                else
                    evolutionHistory
                    |> List.truncate 5
                    |> List.map (fun e -> sprintf "Gen %d: %.2f" e.Generation e.BestReward)
                    |> String.concat "\n"
            this.SetText "SidebarHistory" historyText

            // Update status bar
            this.UpdateStatus
                (match log.LogPath with Some p -> sprintf "Log: %s" p | None -> "Log: console only")
                (sprintf "Agents: %d | Gen: %d" (List.length population) generation)

            // Update DataGrid
            let rows =
                state.Schedule
                |> List.sortByDescending (fun s -> s.PlacedAt)
                |> List.map (fun s ->
                    { EmployeeId = s.Request.EmployeeId
                      StartDate  = s.Request.StartDate.ToString("yyyy-MM-dd")
                      EndDate    = s.Request.EndDate.ToString("yyyy-MM-dd")
                      Priority   = s.Request.Priority
                      Reward     = sprintf "%.2f" s.Reward
                      PlacedAt   = s.PlacedAt.ToString("yyyy-MM-dd HH:mm:ss") })

            match this.FindControl<DataGrid>("RosterGrid") with
            | null -> ()
            | grid -> grid.ItemsSource <- rows
        )

    member private this.SetText (name : string) (text : string) =
        match this.FindControl<TextBlock>(name) with
        | null -> ()
        | tb   -> tb.Text <- text

    member private this.UpdateStatus (left : string) (right : string) =
        this.SetText "StatusLogPath"   left
        this.SetText "StatusAgentInfo" right

    member private this.ShowAbout() =
        let dlg = Window()
        dlg.Title  <- "About Holiday Scheduler AI"
        dlg.Width  <- 420.0
        dlg.Height <- 260.0
        let aboutText =
            "Holiday Scheduler AI\n" +
            "F# + Avalonia multi-agent scheduler\n\n" +
            "Agents compete each generation to schedule holiday\n" +
            "requests for maximum reward. The fittest agent's\n" +
            "strategy genes are inherited (with mutation) by\n" +
            "the next generation — an evolutionary approach\n" +
            "to optimal holiday placement."
        let tb = TextBlock(
            Text = aboutText,
            Margin = Avalonia.Thickness(20.0),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap)
        dlg.Content <- tb
        dlg.ShowDialog(this) |> ignore
