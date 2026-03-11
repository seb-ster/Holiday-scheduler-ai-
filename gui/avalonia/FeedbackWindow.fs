namespace HolidayScheduler.Gui

open Avalonia.Controls
open Avalonia.Markup.Xaml

type FeedbackWindow(currentVersion: string) as this =
    inherit Window()

    let submitted = Event<Support.FeedbackPayload>()
    let mutable categoryBox: ComboBox option = None
    let mutable contactBox: TextBox option = None
    let mutable messageBox: TextBox option = None
    let mutable versionText: TextBlock option = None
    let mutable sendButton: Button option = None
    let mutable cancelButton: Button option = None

    let selectedCategory () =
        match categoryBox with
        | Some box ->
            match box.SelectedItem with
            | :? ComboBoxItem as item ->
                match item.Content with
                | null -> "Allgemein"
                | value -> value.ToString()
            | _ -> "Allgemein"
        | None -> "Allgemein"

    do
        AvaloniaXamlLoader.Load(this)
        categoryBox <- Some(this.FindControl<ComboBox>("FeedbackCategoryBox"))
        contactBox <- Some(this.FindControl<TextBox>("FeedbackContactBox"))
        messageBox <- Some(this.FindControl<TextBox>("FeedbackMessageBox"))
        versionText <- Some(this.FindControl<TextBlock>("FeedbackVersionText"))
        sendButton <- Some(this.FindControl<Button>("FeedbackSendButton"))
        cancelButton <- Some(this.FindControl<Button>("FeedbackCancelButton"))

        match versionText with
        | Some control -> control.Text <- $"Version {currentVersion}"
        | None -> ()

        match cancelButton with
        | Some button -> button.Click.Add(fun _ -> this.Close())
        | None -> ()

        match sendButton with
        | Some button ->
            button.Click.Add(fun _ ->
                let message =
                    match messageBox with
                    | Some box -> box.Text
                    | None -> ""

                if not (System.String.IsNullOrWhiteSpace(message)) then
                    let payload =
                        { Support.FeedbackPayload.Category = selectedCategory ()
                          Message = message.Trim()
                          Contact =
                            match contactBox with
                            | Some box when not (System.String.IsNullOrWhiteSpace(box.Text)) -> box.Text.Trim()
                            | _ -> ""
                          CurrentVersion = currentVersion }

                    submitted.Trigger(payload)
                    this.Close())
        | None -> ()

    member _.Submitted = submitted.Publish