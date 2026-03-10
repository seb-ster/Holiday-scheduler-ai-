namespace HolidayScheduler.Gui

open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Controls.Shapes
open Avalonia.Markup.Xaml
open Avalonia.Media
open Avalonia.Threading
open Avalonia.VisualTree
open System
open System.Collections.ObjectModel
open System.Globalization

type RosterDayRow() =
    member val DayLabel = "" with get, set
    member val Month1Status = "" with get, set
    member val Month2Status = "" with get, set
    member val Month3Status = "" with get, set
    member val Month4Status = "" with get, set
    member val Month5Status = "" with get, set
    member val Month6Status = "" with get, set
    member val Month7Status = "" with get, set
    member val Month8Status = "" with get, set
    member val Month9Status = "" with get, set
    member val Month10Status = "" with get, set
    member val Month11Status = "" with get, set
    member val Month12Status = "" with get, set

type YearRosterView() =
    member val Year = 0 with get, set
    member val Month1Name = "" with get, set
    member val Month2Name = "" with get, set
    member val Month3Name = "" with get, set
    member val Month4Name = "" with get, set
    member val Month5Name = "" with get, set
    member val Month6Name = "" with get, set
    member val Month7Name = "" with get, set
    member val Month8Name = "" with get, set
    member val Month9Name = "" with get, set
    member val Month10Name = "" with get, set
    member val Month11Name = "" with get, set
    member val Month12Name = "" with get, set
    member val Rows = ResizeArray<RosterDayRow>() with get

type StatusTone =
    | Positive
    | Negative
    | Neutral

type MainWindow() as this =
    inherit Window()

    let currentYear = DateTime.Now.Year
    let years = ObservableCollection<YearRosterView>()
    let mutable selectedIndex = 2
    let mutable isSelecting = false
    let mutable isInitialized = false
    let mutable yearLabel: TextBlock option = None
    let mutable yearTabStrip: TabStrip option = None
    let mutable yearContent: ContentControl option = None
    let mutable statusBar: Border option = None
    let mutable statusMessage: TextBlock option = None
    let mutable statusCountdown: TextBlock option = None
    let mutable statusCountdownValue = 2
    let mutable statusTimer: Avalonia.Threading.DispatcherTimer option = None
    let mutable statusProgressFill: Rectangle option = None
    let mutable statusCloseButton: Border option = None
    let mutable statusCloseLabel: TextBlock option = None
    let mutable hasDiscoveredHighlight = false
    let mutable currentRosterListBox: ListBox option = None

    let monthName month =
        CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames[month - 1]

    let statusForDay year month day =
        let maxDay = DateTime.DaysInMonth(year, month)
        if day > maxDay then "" else ""

    let fillMonths (view: YearRosterView) =
        view.Month1Name <- monthName 1
        view.Month2Name <- monthName 2
        view.Month3Name <- monthName 3
        view.Month4Name <- monthName 4
        view.Month5Name <- monthName 5
        view.Month6Name <- monthName 6
        view.Month7Name <- monthName 7
        view.Month8Name <- monthName 8
        view.Month9Name <- monthName 9
        view.Month10Name <- monthName 10
        view.Month11Name <- monthName 11
        view.Month12Name <- monthName 12

    let fillRows (view: YearRosterView) year =
        for day in 1 .. 31 do
            let row = RosterDayRow()
            row.DayLabel <- day.ToString(CultureInfo.InvariantCulture)
            row.Month1Status <- statusForDay year 1 day
            row.Month2Status <- statusForDay year 2 day
            row.Month3Status <- statusForDay year 3 day
            row.Month4Status <- statusForDay year 4 day
            row.Month5Status <- statusForDay year 5 day
            row.Month6Status <- statusForDay year 6 day
            row.Month7Status <- statusForDay year 7 day
            row.Month8Status <- statusForDay year 8 day
            row.Month9Status <- statusForDay year 9 day
            row.Month10Status <- statusForDay year 10 day
            row.Month11Status <- statusForDay year 11 day
            row.Month12Status <- statusForDay year 12 day
            view.Rows.Add(row)

    do AvaloniaXamlLoader.Load(this)

    do
        yearLabel <- Some(this.FindControl<TextBlock>("YearLabel"))
        yearTabStrip <- Some(this.FindControl<TabStrip>("YearTabStrip"))
        yearContent <- Some(this.FindControl<ContentControl>("YearContent"))
        statusBar <- Some(this.FindControl<Border>("StatusBar"))
        statusMessage <- Some(this.FindControl<TextBlock>("StatusMessage"))
        statusCountdown <- Some(this.FindControl<TextBlock>("StatusCountdown"))
        statusProgressFill <- Some(this.FindControl<Rectangle>("StatusProgressFill"))
        statusCloseButton <- Some(this.FindControl<Border>("StatusCloseButton"))
        statusCloseLabel <- Some(this.FindControl<TextBlock>("StatusCloseLabel"))

    let yearTab0 = this.FindControl<TabStripItem>("YearTab0")
    let yearTab1 = this.FindControl<TabStripItem>("YearTab1")
    let yearTab2 = this.FindControl<TabStripItem>("YearTab2")
    let yearTab3 = this.FindControl<TabStripItem>("YearTab3")
    let yearTab4 = this.FindControl<TabStripItem>("YearTab4")
    let yearTabs = [| yearTab0; yearTab1; yearTab2; yearTab3; yearTab4 |]

    let updateYearLabel selectedYear =
        match yearLabel with
        | Some label -> label.Text <- $"Roster year {selectedYear}"
        | None -> ()

    let setStatusTone tone =
        let backgroundHex, foregroundHex, borderHex, progressHex =
            match tone with
            | Positive -> "#C8E6C9", "#1B5E20", "#81C784", "#A5D6A7"
            | Negative -> "#FFCDD2", "#B71C1C", "#EF9A9A", "#EF9A9A"
            | Neutral -> "#FFF9C4", "#7A5A00", "#FFE082", "#FFE082"

        match statusBar with
        | Some bar -> 
            bar.Background <- SolidColorBrush(Color.Parse(backgroundHex))
            bar.BorderBrush <- SolidColorBrush(Color.Parse(borderHex))
        | None -> ()

        let foregroundBrush = SolidColorBrush(Color.Parse(foregroundHex))

        match statusMessage with
        | Some msg -> msg.Foreground <- foregroundBrush
        | None -> ()

        match statusCountdown with
        | Some countdown -> countdown.Foreground <- foregroundBrush
        | None -> ()

        match statusCloseButton with
        | Some btn ->
            btn.BorderBrush <- SolidColorBrush(Color.Parse(borderHex))
            btn.Background <- SolidColorBrush(Color.Parse(backgroundHex))
        | None -> ()

        match statusCloseLabel with
        | Some label -> label.Foreground <- foregroundBrush
        | None -> ()

        match statusProgressFill with
        | Some fill -> fill.Fill <- SolidColorBrush(Color.Parse(progressHex))
        | None -> ()

    let selectYear index =
        if not isSelecting then
            isSelecting <- true

            try
                selectedIndex <- index
                match yearTabStrip with
                | Some tabStrip -> tabStrip.SelectedIndex <- index
                | None -> ()

                match yearContent with
                | Some content -> content.Content <- years[index]
                | None -> ()

                updateYearLabel years[index].Year
            finally
                isSelecting <- false

    let hideStatus () =
        match statusTimer with
        | Some timer ->
            timer.Stop()
        | None -> ()

        statusTimer <- None

        match statusBar with
        | Some bar -> bar.IsVisible <- false
        | None -> ()

    let showStatus message tone =
        setStatusTone tone

        match statusMessage with
        | Some msg -> msg.Text <- message
        | None -> ()

        statusCountdownValue <- 2

        match statusCountdown with
        | Some countdown -> countdown.Text <- statusCountdownValue.ToString()
        | None -> ()

        // Reset progress fill to 0 width
        match statusProgressFill with
        | Some fill -> fill.Width <- 0.0
        | None -> ()

        match statusBar with
        | Some bar -> bar.IsVisible <- true
        | None -> ()

        // Stop existing timer if any
        match statusTimer with
        | Some timer -> timer.Stop()
        | None -> ()

        // Create new countdown timer with smooth progress updates
        let totalDuration = 2.0 // seconds
        let updateInterval = 0.05 // 50ms for smooth animation
        let totalSteps = int (totalDuration / updateInterval)
        let mutable currentStep = 0
        let buttonWidth = 82.0 // matches XAML Width

        let timer = Avalonia.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(updateInterval)
        timer.Tick.Add(fun _ ->
            currentStep <- currentStep + 1
            let progress = float currentStep / float totalSteps
            
            // Update progress fill width
            match statusProgressFill with
            | Some fill -> fill.Width <- buttonWidth * progress
            | None -> ()
            
            // Update countdown display every second
            let secondsRemaining = int (totalDuration * (1.0 - progress))
            if secondsRemaining <> statusCountdownValue then
                statusCountdownValue <- secondsRemaining
                match statusCountdown with
                | Some countdown -> countdown.Text <- statusCountdownValue.ToString()
                | None -> ()
            
            if currentStep >= totalSteps then
                hideStatus())

        statusTimer <- Some timer
        timer.Start()

    let buildYearTabs () =
        years.Clear()

        for year in (currentYear - 2) .. (currentYear + 2) do
            let view = YearRosterView()
            view.Year <- year
            fillMonths view
            fillRows view year
            years.Add(view)

    do
        buildYearTabs ()

        // RosterListBox is created inside a ContentTemplate; locate it after layout pass.
        let attachRosterSelectionHandler () =
            Dispatcher.UIThread.Post((fun () ->
                let descendants =
                    match yearContent with
                    | Some content -> content.GetVisualDescendants()
                    | None -> this.GetVisualDescendants()

                let listBoxCandidate =
                    descendants
                    |> Seq.tryPick (fun visual ->
                        match visual with
                        | :? ListBox as lb -> Some lb
                        | _ -> None)

                match listBoxCandidate, currentRosterListBox with
                | Some lb, Some current when Object.ReferenceEquals(lb, current) -> ()
                | Some lb, _ ->
                    currentRosterListBox <- Some lb
                    lb.SelectionChanged.Add(fun _ ->
                        match lb.SelectedItem with
                        | :? RosterDayRow as row ->
                            if not hasDiscoveredHighlight then
                                hasDiscoveredHighlight <- true
                                showStatus "Success: highlight discovered" Positive
                            else
                                showStatus $"Day {row.DayLabel} selected" Positive
                        | _ ->
                            showStatus "No day selected" Negative)
                | None, _ -> ()), DispatcherPriority.Background)

        for i in 0 .. yearTabs.Length - 1 do
            yearTabs[i].Content <- years[i].Year.ToString(CultureInfo.InvariantCulture)

        match yearTabStrip with
        | Some tabStrip ->
            tabStrip.SelectionChanged.Add(fun _ ->
                if isInitialized && not isSelecting then
                    let index = tabStrip.SelectedIndex

                    if index >= 0 && index < years.Count && index <> selectedIndex then
                        selectYear index
                        attachRosterSelectionHandler ())
        | None -> ()

        selectYear selectedIndex
        isInitialized <- true

        // Wire up status bar close button
        let closeButton = this.FindControl<Border>("StatusCloseButton")
        if not (isNull closeButton) then
            closeButton.PointerPressed.Add(fun _ -> hideStatus())

        // Manual status test controls
        let positiveButton = this.FindControl<Button>("StatusPositiveButton")
        if not (isNull positiveButton) then
            positiveButton.Click.Add(fun _ -> showStatus "Manual positive status" Positive)

        let negativeButton = this.FindControl<Button>("StatusNegativeButton")
        if not (isNull negativeButton) then
            negativeButton.Click.Add(fun _ -> showStatus "Manual negative status" Negative)

        let neutralButton = this.FindControl<Button>("StatusNeutralButton")
        if not (isNull neutralButton) then
            neutralButton.Click.Add(fun _ -> showStatus "Manual neutral status" Neutral)

        attachRosterSelectionHandler ()