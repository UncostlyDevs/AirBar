using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using FloatingTaskbarMenu.Core;
using FloatingTaskbarMenu.Models;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;
using WpfTextChangedEventArgs = System.Windows.Controls.TextChangedEventArgs;

namespace FloatingTaskbarMenu.Windows;

public partial class TaskbarMenuWindow : Window
{
    private readonly WindowManager _windowManager;
    private readonly SettingsService _settingsService;
    private readonly PinnedProfileService _pinnedProfileService;
    private readonly WindowHistoryService _historyService;
    private readonly AppLauncherService _appLauncherService;
    private Settings _settings;
    private HistoryWindow? _historyWindow;
    private bool _suppressNextDeactivateClose;

    public TaskbarMenuWindow(WindowManager windowManager, SettingsService settingsService)
    {
        _windowManager = windowManager;
        _settingsService = settingsService;
        _pinnedProfileService = new PinnedProfileService();
        _historyService = new WindowHistoryService();
        _appLauncherService = new AppLauncherService();
        _settings = settingsService.Settings;

        InitializeComponent();
        DataContext = _settings;

        Loaded += OnLoaded;
        Deactivated += OnDeactivated;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyDwmBackground();
        System.Threading.Tasks.Task.Run(() => _windowManager.PreWarmCache());
        RefreshWindows();
        SearchBox.Focus();
    }

    private void OnSearchBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (SearchBox.Text == "Search windows...")
            SearchBox.Text = "";
    }

    private void OnSearchBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SearchBox.Text))
            SearchBox.Text = "Search windows...";
    }

    private void OnSearchBoxTextChanged(object sender, WpfTextChangedEventArgs e)
    {
        FilterWindows(SearchBox.Text);
    }

    private void OnDragHandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed || e.ClickCount != 1)
            return;

        if (HasVisualParent<System.Windows.Controls.Primitives.ButtonBase>(e.OriginalSource as DependencyObject) ||
            HasVisualParent<System.Windows.Controls.TextBox>(e.OriginalSource as DependencyObject))
        {
            return;
        }

        try
        {
            DragMove();
            e.Handled = true;
        }
        catch
        {
            // DragMove can throw if the mouse is released between down and drag.
        }
    }

    private static bool HasVisualParent<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source != null)
        {
            if (source is T)
                return true;

            try
            {
                source = VisualTreeHelper.GetParent(source);
            }
            catch
            {
                source = null;
            }
        }

        return false;
    }

    private void FilterWindows(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText) || searchText == "Search windows...")
        {
            RefreshWindows();
            return;
        }

        var allWindows = _windowManager?.GetWindows() ?? new List<WindowInfo>();
        var filtered = allWindows.Where(w =>
            w.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            w.ProcessName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        WindowListView.RefreshWindows(filtered);
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (_suppressNextDeactivateClose)
        {
            _suppressNextDeactivateClose = false;
            return;
        }

        SaveCurrentLayout();
        try { Close(); } catch { }
    }

    public void SuppressNextDeactivateClose()
    {
        _suppressNextDeactivateClose = true;
    }

    private void ApplyDwmBackground()
    {
        // Disabled DWM effects for performance - using simple transparency instead
    }

    public void RefreshWindows()
    {
        if (WindowListView == null) return;

        var windows = _windowManager.GetWindows();
        WindowListView.SetWindowManager(_windowManager);
        WindowListView.SetPinnedProfileService(_pinnedProfileService, _settings.CurrentPinnedProfile);
        WindowListView.SetAppLauncherService(_appLauncherService);
        WindowListView.SetHistoryService(_historyService, _settingsService);
        WindowListView.RefreshWindows(windows);
        AuxiliaryControls.SetContext(_settingsService, _historyService, () =>
        {
            _settings = _settingsService.Settings;
            DataContext = _settings;
            RefreshWindows();
        });
    }

    public void PositionAtCursor()
    {
        try
        {
            GetCursorPos(out System.Drawing.Point cursorPos);
            var workArea = GetWorkArea(cursorPos);

            double left = cursorPos.X;
            double top = cursorPos.Y;

            if (left + Width > workArea.Right)
                left = workArea.Right - Width;
            if (top + Height > workArea.Bottom)
                top = workArea.Bottom - Height;
            if (left < workArea.Left)
                left = workArea.Left;
            if (top < workArea.Top)
                top = workArea.Top;

            Left = left;
            Top = top;
        }
        catch { }
    }

    private string GetMonitorId(System.Drawing.Point point)
    {
        try
        {
            var monitor = MonitorFromPoint(new POINT(point.X, point.Y), MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO();
            info.cbSize = Marshal.SizeOf<MONITORINFO>();
            if (GetMonitorInfo(monitor, ref info))
                return $"{info.rcMonitor.left}_{info.rcMonitor.top}_{info.rcMonitor.right}_{info.rcMonitor.bottom}";
        }
        catch { }
        return "default";
    }

    private void SaveCurrentLayout()
    {
        try
        {
            GetCursorPos(out System.Drawing.Point cursorPos);
            var monitorId = GetMonitorId(cursorPos);

            _settings.MonitorLayouts[monitorId] = new MonitorLayoutSettings
            {
                Left = Left,
                Top = Top,
                Width = Width,
                Opacity = Opacity
            };

            _settingsService.Save();
        }
        catch { }
    }

    private Rect GetWorkArea(System.Drawing.Point cursorPos)
    {
        try
        {
            var monitor = MonitorFromPoint(new POINT(cursorPos.X, cursorPos.Y), MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO();
            info.cbSize = Marshal.SizeOf<MONITORINFO>();
            GetMonitorInfo(monitor, ref info);
            return new Rect(info.rcWork.left, info.rcWork.top,
                info.rcWork.right - info.rcWork.left,
                info.rcWork.bottom - info.rcWork.top);
        }
        catch
        {
            return new Rect(0, 0, 1920, 1080);
        }
    }

    private void OpenAirBarSettings()
    {
        var settingsWindow = new SettingsWindow(_settingsService)
        {
            Owner = this
        };
        settingsWindow.SettingsApplied += (s, e) =>
        {
            _settings = _settingsService.Settings;
            DataContext = _settings;
            RefreshWindows();
        };
        settingsWindow.ShowDialog();

        _settings = _settingsService.Settings;
        DataContext = _settings;
        RefreshWindows();
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        OpenAirBarSettings();
    }

    private void OpenHistoryWindow()
    {
        try
        {
            if (_historyWindow != null)
            {
                if (_historyWindow.IsVisible)
                {
                    _historyWindow.Activate();
                    return;
                }

                _historyWindow = null;
            }

            _suppressNextDeactivateClose = true;
            _historyWindow = new HistoryWindow(_historyService, _settings.HistoryFilter);
            _historyWindow.Closed += (s, e) => _historyWindow = null;
            _historyWindow.Show();
            _historyWindow.Activate();
        }
        catch
        {
            _suppressNextDeactivateClose = false;
        }
    }

    private void OnHistoryClick(object sender, RoutedEventArgs e)
    {
        OpenHistoryWindow();
    }

    private void OnQuickSettingsClick(object sender, RoutedEventArgs e)
    {
        AuxiliaryControls.ShowSettingsFlyout(QuickSettingsButton);
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
        public POINT(int x, int y) { X = x; Y = y; }
    }

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
}
