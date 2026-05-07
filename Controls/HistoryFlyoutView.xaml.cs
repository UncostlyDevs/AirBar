using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FloatingTaskbarMenu.Core;
using FloatingTaskbarMenu.Models;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfApplication = System.Windows.Application;

namespace FloatingTaskbarMenu.Controls;

public partial class HistoryFlyoutView : WpfUserControl
{
    private readonly WindowHistoryService _historyService;
    private readonly AppLauncherService _appLauncherService = new();
    private readonly HistoryFilter _filter;
    private int _currentPage;
    private const int PageSize = 24;

    public HistoryFlyoutView(WindowHistoryService historyService, HistoryFilter filter)
    {
        _historyService = historyService;
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
        if (_currentPage <= 0) return;
        _currentPage--;
        LoadHistoryPage();
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        var totalCount = _historyService.GetTotalCount(_filter);
        var totalPages = (int)Math.Ceiling((double)totalCount / PageSize);

        if (_currentPage >= totalPages - 1) return;
        _currentPage++;
        LoadHistoryPage();
    }

    private void OnHistoryItemRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not WindowHistory history)
            return;

        var target = string.IsNullOrWhiteSpace(history.ExecutablePath) ? history.ProcessName : history.ExecutablePath;
        var launcherApp = _appLauncherService.GetApp(target);
        var contextMenu = new ContextMenu();
        if (WpfApplication.Current.Resources["Win11ContextMenuStyle"] is Style ctxStyle)
            contextMenu.Style = ctxStyle;

        var pinItem = new MenuItem
        {
            Header = launcherApp?.IsPinned == true ? "Unpin from Launcher" : "Pin to Launcher"
        };
        if (WpfApplication.Current.Resources["Win11MenuItemStyle"] is Style itemStyle)
            pinItem.Style = itemStyle;

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

        contextMenu.PlacementTarget = border;
        contextMenu.IsOpen = true;
        e.Handled = true;
    }
}
