using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FloatingTaskbarMenu.Core;
using FloatingTaskbarMenu.Models;

namespace FloatingTaskbarMenu.Windows;

public partial class HistoryWindow : Window
{
    private readonly WindowHistoryService _historyService;
    private readonly AppLauncherService _appLauncherService;
    private readonly HistoryFilter _filter;
    private int _currentPage = 0;
    private const int PageSize = 30;

    public HistoryWindow(WindowHistoryService historyService, HistoryFilter filter)
    {
        _historyService = historyService;
        _appLauncherService = new AppLauncherService();
        _filter = filter;

        InitializeComponent();
        LoadHistoryPage();
    }

    private void LoadHistoryPage()
    {
        var history = _historyService.GetHistory(_filter, _currentPage, PageSize);
        HistoryList.ItemsSource = history;

        var totalCount = _historyService.GetTotalCount(_filter);
        var totalPages = (int)Math.Ceiling((double)totalCount / PageSize);
        PageIndicator.Text = $"Page {_currentPage + 1} of {Math.Max(1, totalPages)}";
    }

    private void OnPreviousClick(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            LoadHistoryPage();
        }
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        var totalCount = _historyService.GetTotalCount(_filter);
        var totalPages = (int)Math.Ceiling((double)totalCount / PageSize);

        if (_currentPage < totalPages - 1)
        {
            _currentPage++;
            LoadHistoryPage();
        }
    }

    private void OnHistoryItemRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is WindowHistory history)
        {
            var contextMenu = new ContextMenu();
            var target = string.IsNullOrWhiteSpace(history.ExecutablePath) ? history.ProcessName : history.ExecutablePath;
            var launcherApp = _appLauncherService.GetApp(target);
            var pinItem = new MenuItem
            {
                Header = launcherApp?.IsPinned == true ? "Unpin from Launcher" : "Pin to Launcher"
            };
            pinItem.Click += (s, args) =>
            {
                if (launcherApp != null)
                {
                    if (launcherApp.IsPinned)
                        _appLauncherService.UnpinApp(target);
                    else
                        _appLauncherService.PinApp(target);
                }
                else
                {
                    _appLauncherService.AddOrUpdateApp(history.ProcessName, target, null);
                    _appLauncherService.PinApp(target);
                }
            };
            contextMenu.Items.Add(pinItem);

            contextMenu.IsOpen = true;
            e.Handled = true;
        }
    }
}
