namespace HolidayScheduler.Gui

open Avalonia.Controls
open Avalonia.Markup.Xaml

// User-facing crash dialog shown on unhandled exceptions.
type CrashReportWindow(ex: exn, reportPath: string) as this =
    inherit Window()

    let mutable titleText: TextBlock option = None
    let mutable summaryText: TextBlock option = None
    let mutable detailText: TextBox option = None
    let mutable revealButton: Button option = None
    let mutable closeButton: Button option = None

    do
        AvaloniaXamlLoader.Load(this)
        titleText <- Some(this.FindControl<TextBlock>("CrashTitleText"))
        summaryText <- Some(this.FindControl<TextBlock>("CrashSummaryText"))
        detailText <- Some(this.FindControl<TextBox>("CrashDetailText"))
        revealButton <- Some(this.FindControl<Button>("CrashRevealButton"))
        closeButton <- Some(this.FindControl<Button>("CrashCloseButton"))

        match summaryText with
        | Some control ->
            control.Text <-
                if System.String.IsNullOrWhiteSpace(ex.Message) then
                    "Ein unerwarteter Fehler ist aufgetreten."
                else
                    ex.Message
        | None -> ()

        match detailText with
        | Some control ->
            control.Text <-
                $"Typ: {ex.GetType().FullName}\n\n{ex.StackTrace}"
        | None -> ()

        match revealButton with
        | Some button ->
            button.Click.Add(fun _ ->
                if not (System.String.IsNullOrWhiteSpace(reportPath)) then
                    Support.revealPath reportPath)
        | None -> ()

        match closeButton with
        | Some button -> button.Click.Add(fun _ -> this.Close())
        | None -> ()
