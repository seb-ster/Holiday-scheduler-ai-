namespace HolidayScheduler.Gui

open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Markup.Xaml
open Avalonia.Media
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
    let mutable statusCountdownValue = 5
    let mutable statusTimer: Avalonia.Threading.DispatcherTimer option = None

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
        let backgroundHex, foregroundHex =
            match tone with
            | Positive -> "#DCFCE7", "#166534"
            | Negative -> "#FEE2E2", "#991B1B"

        match statusBar with
        | Some bar -> bar.Background <- SolidColorBrush(Color.Parse(backgroundHex))
        | None -> ()

        let foregroundBrush = SolidColorBrush(Color.Parse(foregroundHex))

        match statusMessage with
        | Some msg -> msg.Foreground <- foregroundBrush
        | None -> ()

        match statusCountdown with
        | Some countdown -> countdown.Foreground <- foregroundBrush
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

        statusCountdownValue <- 5

        match statusCountdown with
        | Some countdown -> countdown.Text <- statusCountdownValue.ToString()
        | None -> ()

        match statusBar with
        | Some bar -> bar.IsVisible <- true
        | None -> ()

        // Stop existing timer if any
        match statusTimer with
        | Some timer -> timer.Stop()
        | None -> ()

        // Create new countdown timer
        let timer = Avalonia.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(1.0)
        timer.Tick.Add(fun _ ->
            statusCountdownValue <- statusCountdownValue - 1

            match statusCountdown with
            | Some countdown -> countdown.Text <- statusCountdownValue.ToString()
            | None -> ()

            if statusCountdownValue <= 0 then
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

        for i in 0 .. yearTabs.Length - 1 do
            yearTabs[i].Content <- years[i].Year.ToString(CultureInfo.InvariantCulture)

        match yearTabStrip with
        | Some tabStrip ->
            tabStrip.SelectionChanged.Add(fun _ ->
                if isInitialized && not isSelecting then
                    let index = tabStrip.SelectedIndex

                    if index >= 0 && index < years.Count && index <> selectedIndex then
                        selectYear index)
        | None -> ()

        selectYear selectedIndex
        isInitialized <- true

        // Wire up status bar close button
        let closeButton = this.FindControl<Border>("StatusCloseButton")
        if not (isNull closeButton) then
            closeButton.PointerPressed.Add(fun _ -> hideStatus())

        // Wire up roster list selection to show status
        let rosterListBox = this.FindControl<ListBox>("RosterListBox")
        if not (isNull rosterListBox) then
            rosterListBox.SelectionChanged.Add(fun _ ->
                match rosterListBox.SelectedItem with
                | :? RosterDayRow as row ->
                    showStatus $"Day {row.DayLabel} selected" Positive
                | _ ->
                    showStatus "No day selected" Negative)