namespace HolidayScheduler.Gui

open Avalonia.Controls
open Avalonia.Markup.Xaml
open System

type SplashWindow() as this =
    inherit Window()

    let mutable versionText: TextBlock option = None
    let mutable statusText: TextBlock option = None
    let mutable progressBar: ProgressBar option = None
    let statusLines = System.Collections.Generic.List<string>()
    let maxLines = 9

    do
        AvaloniaXamlLoader.Load(this)
        versionText <- Some(this.FindControl<TextBlock>("SplashVersionText"))
        statusText <- Some(this.FindControl<TextBlock>("SplashStatusText"))
        progressBar <- Some(this.FindControl<ProgressBar>("SplashProgressBar"))

    member _.SetVersion(text: string) =
        match versionText with
        | Some control -> control.Text <- text
        | None -> ()

    member _.SetStatus(text: string) =
        let stamp = DateTime.Now.ToString("HH:mm:ss")
        let line = $"[{stamp}] {text}"
        statusLines.Add(line)

        while statusLines.Count > maxLines do
            statusLines.RemoveAt(0)

        let textValue = String.Join(Environment.NewLine, statusLines)

        match statusText with
        | Some control -> control.Text <- textValue
        | None -> ()

    member _.SetProgress(percent: float) =
        match progressBar with
        | Some control ->
            control.IsIndeterminate <- false
            control.Value <- max 0.0 (min 100.0 percent)
        | None -> ()