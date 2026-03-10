namespace HolidayScheduler

open System
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

// ── Main Window ──────────────────────────────────────────────────────────────

type MainWindow() as this =
    inherit Window()

    // Mutable scheduler state
    let mutable population  : Population = createPopulation 6
    let mutable generation  : int = 1
    let mutable state : SchedulerState =
        { Queue    = []
          Schedule = []
          Blocked  = Set.empty
          Capacity = 3 }

    let log = Logger.instance ()

    do
        AvaloniaXamlLoader.Load(this)
        this.InitializeComponent()

    member private this.InitializeComponent() =
        // Wire up sidebar action buttons
        let btnEvolve = this.FindControl<Button>("BtnRunEvolution")
        let btnSample = this.FindControl<Button>("BtnAddSample")

        if btnEvolve <> null then btnEvolve.Click.Add(fun _ -> this.OnRunEvolution())
        if btnSample <> null then btnSample.Click.Add(fun _ -> this.OnAddSampleRequest())

        let btnDemo  = this.FindControl<Button>("BtnRunDemo")
        if btnDemo  <> null then btnDemo.Click.Add(fun _ -> this.OnRunDemo())

        // Wire up Menu items defined in XAML
        let wireMenuItem (name : string) (handler : unit -> unit) =
            let item = this.FindControl<MenuItem>(name)
            if item <> null then
                item.Click.Add(fun _ -> handler())

        wireMenuItem "MenuFileNew"    (fun () -> this.OnNewSchedule())
        wireMenuItem "MenuFileExit"   (fun () -> this.Close())
        wireMenuItem "MenuEditAdd"    (fun () -> this.OnAddSampleRequest())
        wireMenuItem "MenuEditEvolve" (fun () -> this.OnRunEvolution())
        wireMenuItem "MenuViewRoster" (fun () -> ())
        wireMenuItem "MenuHelpAbout"  (fun () -> this.ShowAbout())
        wireMenuItem "MenuHelpLogs"   (fun () -> log.Statistics())

        // Set initial log path in status bar
        match log.LogPath with
        | Some p -> this.UpdateStatus (sprintf "Log: %s" p) ""
        | None   -> this.UpdateStatus "Log: console only" ""

        this.RefreshUI()
        log.Info "MainWindow initialized"

    member private this.OnNewSchedule() =
        state <- { Queue = []; Schedule = []; Blocked = Set.empty; Capacity = 3 }
        population  <- createPopulation 6
        generation  <- 1
        log.Info "New schedule started"
        this.RefreshUI()

    member private this.OnAddSampleRequest() =
        let rng = Random()
        let employees = [| "Alice"; "Bob"; "Carol"; "Dave"; "Eve" |]
        let today     = DateOnly.FromDateTime(DateTime.Today)
        let startOff  = rng.Next(1, 60)
        let duration  = rng.Next(1, 14)
        let req =
            { RequestId   = Guid.NewGuid()
              EmployeeId  = employees.[rng.Next(employees.Length)]
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

    member private this.OnRunEvolution() =
        log.Info (sprintf "Starting evolution – generation %d, population %d, queue %d"
                      generation (List.length population) (List.length state.Queue))

        let (newPop, newState) =
            evolveGeneration population state 3 (List.length population) generation

        population <- newPop
        state      <- newState
        generation <- generation + 1

        log.Info (sprintf "Evolution complete – generation %d, scheduled %d"
                      generation (List.length state.Schedule))
        this.RefreshUI()

    member private this.OnRunDemo() =
        // Reset to a clean state
        state      <- { Queue = []; Schedule = []; Blocked = Set.empty; Capacity = 3 }
        population <- createPopulation 6
        generation <- 1
        log.Info "Demo started – resetting and loading sample requests"

        // Seed 15 deterministic-ish holiday requests spread across the next 90 days
        let rng       = Random(42)
        let employees = [| "Alice"; "Bob"; "Carol"; "Dave"; "Eve" |]
        let today     = DateOnly.FromDateTime(DateTime.Today)
        let requests =
            [ for i in 0 .. 14 do
                let startDayOffset = (i * 6) + rng.Next(1, 5)
                let duration = rng.Next(3, 12)
                yield
                    { RequestId   = Guid.NewGuid()
                      EmployeeId  = employees.[i % employees.Length]
                      StartDate   = today.AddDays(startDayOffset)
                      EndDate     = today.AddDays(startDayOffset + duration)
                      Priority    = (i % 5) + 1
                      SubmittedAt = DateTime.UtcNow } ]
        state <- { state with Queue = requests }

        log.Info (sprintf "Demo – loaded %d requests; running 5 evolution cycles" (List.length state.Queue))

        // Run 5 evolution cycles automatically
        for _ in 1 .. 5 do
            let (newPop, newState) =
                evolveGeneration population state 3 (List.length population) generation
            population <- newPop
            state      <- newState
            generation <- generation + 1

        log.Info (sprintf "Demo complete – generation %d, scheduled %d, best reward %.2f"
                      generation
                      (List.length state.Schedule)
                      (population
                       |> List.map (fun a -> a.Metrics.AverageReward)
                       |> (fun rs -> if List.isEmpty rs then 0.0 else List.max rs)))
        this.RefreshUI()

    member private this.RefreshUI() =
        Dispatcher.UIThread.Post(fun () ->
            // Update sidebar
            this.SetText "SidebarGeneration"  (string generation)
            this.SetText "SidebarPopulation"  (sprintf "%d agents" (List.length population))

            let bestAgent =
                population
                |> List.sortByDescending (fun a -> a.Metrics.AverageReward)
                |> List.tryHead

            let bestReward =
                bestAgent
                |> Option.map (fun a -> a.Metrics.AverageReward)
                |> Option.defaultValue 0.0
            this.SetText "SidebarBestReward"  (sprintf "%.2f" bestReward)
            this.SetText "SidebarQueueCount"  (string (List.length state.Queue))
            this.SetText "SidebarScheduled"   (string (List.length state.Schedule))

            // Update gene weights for the top agent
            match bestAgent with
            | None -> ()
            | Some a ->
                let g = a.Genes
                this.SetText "SidebarGeneGreedy"   (sprintf "%.3f" g.GreedyWeight)
                this.SetText "SidebarGeneLoad"     (sprintf "%.3f" g.LoadBalanceWeight)
                this.SetText "SidebarGenePriority" (sprintf "%.3f" g.PriorityWeight)
                this.SetText "SidebarGeneCluster"  (sprintf "%.3f" g.ClusterWeight)

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
        dlg.Width  <- 400.0
        dlg.Height <- 220.0
        let aboutText =
            "Holiday Scheduler AI\n" +
            "F# + Avalonia multi-agent scheduler\n\n" +
            "Agents evolve across generations to optimally\n" +
            "place holiday requests for maximum reward."
        let tb = TextBlock(
            Text = aboutText,
            Margin = Avalonia.Thickness(20.0),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap)
        dlg.Content <- tb
        dlg.ShowDialog(this) |> ignore
