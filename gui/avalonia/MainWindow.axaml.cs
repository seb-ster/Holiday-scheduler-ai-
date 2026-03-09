using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace HolidayScheduler.Gui
{
    public partial class MainWindow : Window
    {
        private readonly int _year;
        private int _currentPage;

        public ObservableCollection<YearRosterPage> Pages { get; } = new();

        public MainWindow()
        {
            InitializeComponent();

            _year = DateTime.Now.Year;
            BuildPages();

            RosterCarousel.ItemsSource = Pages;
            _currentPage = 0;
            RosterCarousel.SelectedIndex = _currentPage;

            YearLabel.Text = $"Roster overview for {_year}";
            UpdatePageState();
        }

        private void OnPreviousPage(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_currentPage <= 0)
            {
                return;
            }

            _currentPage -= 1;
            RosterCarousel.SelectedIndex = _currentPage;
            UpdatePageState();
        }

        private void OnNextPage(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_currentPage >= Pages.Count - 1)
            {
                return;
            }

            _currentPage += 1;
            RosterCarousel.SelectedIndex = _currentPage;
            UpdatePageState();
        }

        private void BuildPages()
        {
            Pages.Clear();

            for (var monthStart = 1; monthStart <= 12; monthStart += 4)
            {
                var months = new[]
                {
                    monthStart,
                    monthStart + 1,
                    monthStart + 2,
                    monthStart + 3
                };

                var page = new YearRosterPage
                {
                    Month1Name = MonthName(months[0]),
                    Month2Name = MonthName(months[1]),
                    Month3Name = MonthName(months[2]),
                    Month4Name = MonthName(months[3])
                };

                for (var day = 1; day <= 31; day++)
                {
                    page.Rows.Add(new RosterDayRow
                    {
                        DayLabel = day.ToString(CultureInfo.InvariantCulture),
                        Month1Status = StatusForDay(months[0], day),
                        Month2Status = StatusForDay(months[1], day),
                        Month3Status = StatusForDay(months[2], day),
                        Month4Status = StatusForDay(months[3], day)
                    });
                }

                Pages.Add(page);
            }
        }

        private string MonthName(int month)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames[month - 1];
        }

        private string StatusForDay(int month, int day)
        {
            var maxDay = DateTime.DaysInMonth(_year, month);
            if (day > maxDay)
            {
                return "-";
            }

            var date = new DateTime(_year, month, day);
            return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "OFF" : "AVL";
        }

        private void UpdatePageState()
        {
            PageIndicator.Text = $"Page {_currentPage + 1} of {Pages.Count}";
            PreviousPageButton.IsEnabled = _currentPage > 0;
            NextPageButton.IsEnabled = _currentPage < Pages.Count - 1;
        }
    }

    public sealed class YearRosterPage
    {
        public string Month1Name { get; set; } = string.Empty;

        public string Month2Name { get; set; } = string.Empty;

        public string Month3Name { get; set; } = string.Empty;

        public string Month4Name { get; set; } = string.Empty;

        public List<RosterDayRow> Rows { get; } = new();
    }

    public sealed class RosterDayRow
    {
        public string DayLabel { get; set; } = string.Empty;

        public string Month1Status { get; set; } = string.Empty;

        public string Month2Status { get; set; } = string.Empty;

        public string Month3Status { get; set; } = string.Empty;

        public string Month4Status { get; set; } = string.Empty;
    }
}
