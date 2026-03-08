using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HolidayScheduler.Gui
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
