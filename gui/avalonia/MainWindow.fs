namespace HolidayScheduler.Gui

open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia
open Avalonia.Layout

type MainWindow() as this =
    inherit Window()
    do
        AvaloniaXamlLoader.Load(this)

        let versionMenu = this.FindControl<MenuItem>("VersionMenuItem")
        let licensesMenu = this.FindControl<MenuItem>("LicensesMenuItem")

        versionMenu.Click.Add(fun _ ->
            let splash = SplashWindow()
            splash.ShowDialog(this) |> ignore
        )

        licensesMenu.Click.Add(fun _ ->
            let panel = Window()
            panel.Title <- "Licenses"
            panel.Width <- 500.0
            panel.Height <- 400.0
            let grid = Grid()
            grid.RowDefinitions.Add(RowDefinition())
            grid.RowDefinitions.Add(RowDefinition(Height = GridLength.Auto))
            let text = TextBlock()
            text.Text <- "Holiday Scheduler Demonstrator is open source.\n\nAll included source licenses are open source.\nIf any more restrictive licenses are present, they are listed below.\n\n- MIT License\n- Apache 2.0\n- Avalonia UI: MIT\n- Other dependencies: see source."
            text.Margin <- Thickness(20.0)
            text.TextWrapping <- Avalonia.Controls.TextWrapping.Wrap
            Grid.SetRow(text, 0)
            grid.Children.Add(text)
            let creditsBtn = Button(Content = "Credits")
            creditsBtn.Margin <- Thickness(20.0, 0.0, 20.0, 20.0)
            creditsBtn.HorizontalAlignment <- HorizontalAlignment.Right
            Grid.SetRow(creditsBtn, 1)
            grid.Children.Add(creditsBtn)
            creditsBtn.Click.Add(fun _ ->
                let credits = Window()
                credits.Title <- "Credits"
                credits.Width <- 400.0
                credits.Height <- 300.0
                let creditsText = TextBlock()
                creditsText.Text <- "Credits:\n\n- Avalonia UI Team (MIT License)\n- .NET Foundation (.NET SDK)\n- Open source contributors\n- App developer: Sebastiaan van Heerde\n- See LICENSES folder for full list."
                creditsText.Margin <- Thickness(20.0)
                creditsText.TextWrapping <- Avalonia.Controls.TextWrapping.Wrap
                credits.Content <- creditsText
                credits.ShowDialog(panel) |> ignore
            )
            panel.Content <- grid
            panel.ShowDialog(this) |> ignore
        )
