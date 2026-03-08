using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace HolidayScheduler.Gui
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Show a splash window first, then load the main window.
                var splash = new SplashWindow();
                splash.Show();

                // Load main window after a short delay so the splash is visible.
                Task.Run(async () =>
                {
                    await Task.Delay(1200);
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            var main = new MainWindow();
                            desktop.MainWindow = main;
                            main.Show();
                        }
                        finally
                        {
                            splash.Close();
                        }
                    });
                });
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
