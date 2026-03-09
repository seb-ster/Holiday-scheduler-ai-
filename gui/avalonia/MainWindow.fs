namespace HolidayScheduler.Gui

open Avalonia.Controls
open Avalonia.Markup.Xaml
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

type MainWindow() as this =
    inherit Window()

    let currentYear = DateTime.Now.Year
    let years = ObservableCollection<YearRosterView>()

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

    let updateYearLabel selectedYear =
        let label = this.FindControl<TextBlock>("YearLabel")
        label.Text <- $"Roster year {selectedYear}"

    let buildYearTabs () =
        years.Clear()

        for year in (currentYear - 2) .. (currentYear + 2) do
            let view = YearRosterView()
            view.Year <- year
            fillMonths view
            fillRows view year
            years.Add(view)

    do
        AvaloniaXamlLoader.Load(this)

        let yearTabs = this.FindControl<TabControl>("YearTabs")
        yearTabs.ItemsSource <- years

        buildYearTabs ()

        let selectedIndex = 2
        yearTabs.SelectedIndex <- selectedIndex
        updateYearLabel years[selectedIndex].Year

    member this.OnYearTabChanged(_sender: obj, _e: SelectionChangedEventArgs) =
        let yearTabs = this.FindControl<TabControl>("YearTabs")

        match yearTabs.SelectedItem with
        | :? YearRosterView as selected -> updateYearLabel selected.Year
        | _ -> ()