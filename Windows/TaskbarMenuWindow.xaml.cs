using System.Runtime.InteropServices;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FloatingTaskbarMenu.Controls;
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
    private readonly WorkspaceService _workspaceService;
    private readonly BottomActionBarService _bottomActionBarService;
    private readonly string _debugLogPath = Path.Combine(AppIdentity.AppDataDirectory, "app_debug.log");
    private Settings _settings;
    private HistoryWindow? _historyWindow;
    private Popup? _workspacePopup;
    private bool _suppressNextDeactivateClose;
    private int _autoCloseSuppressionCount;

    public TaskbarMenuWindow(WindowManager windowManager, SettingsService settingsService)
    {
        _windowManager = windowManager;
        _settingsService = settingsService;
        _pinnedProfileService = new PinnedProfileService();
        _historyService = new WindowHistoryService();
        _appLauncherService = new AppLauncherService();
        _workspaceService = new WorkspaceService();
        _bottomActionBarService = new BottomActionBarService();
        _settings = settingsService.Settings;
        _bottomActionBarService.EnsureSlots(_settings);

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
        Dispatcher.BeginInvoke(ActivateForInput, DispatcherPriority.ApplicationIdle);
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
        if (_autoCloseSuppressionCount > 0)
            return;

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

    public void ActivateForInput()
    {
        try
        {
            Topmost = true;
            Show();
            Activate();
            Focus();

            var handle = new WindowInteropHelper(this).Handle;
            if (handle != nint.Zero)
                SetForegroundWindow(handle);
        }
        catch { }
    }

    public IDisposable SuspendAutoClose()
    {
        _autoCloseSuppressionCount++;
        return new AutoCloseSuspension(this);
    }

    private void ResumeAutoClose()
    {
        if (_autoCloseSuppressionCount > 0)
            _autoCloseSuppressionCount--;
    }

    private void ApplyDwmBackground()
    {
        // Disabled DWM effects for performance - using simple transparency instead
    }

    private void Log(string message)
    {
        try
        {
            File.AppendAllText(_debugLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch { }
    }

    public void RefreshWindows()
    {
        if (WindowListView == null) return;

        var windows = _windowManager.GetWindows();
        _bottomActionBarService.EnsureSlots(_settings);
        DataContext = null;
        DataContext = _settings;
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

    private void CloseWorkspacePopup()
    {
        try
        {
            if (_workspacePopup != null)
            {
                _workspacePopup.IsOpen = false;
                _workspacePopup = null;
            }
        }
        catch { }
    }

    private void PrepareForSecondaryWindow()
    {
        CloseWorkspacePopup();
        SuppressNextDeactivateClose();
        Hide();
    }

    private void RestoreAfterSecondaryWindowFailure()
    {
        _suppressNextDeactivateClose = false;
        Show();
        ActivateForInput();
    }

    private void CloseAfterSecondaryWindowShown(Window secondaryWindow)
    {
        try { Hide(); } catch { }

        try
        {
            if (secondaryWindow.IsVisible)
                secondaryWindow.Activate();

            SaveCurrentLayout();
            Close();
        }
        catch { }
    }

    public void ShowWorkspacePopupFromAction(FrameworkElement? placementTarget = null)
        => ShowWorkspacePopup(placementTarget);

    public void CaptureWorkspaceFromAction()
    {
        var view = new WorkspacesView();
        view.SetContext(_workspaceService, _windowManager, _settingsService, this);
        view.CaptureWorkspaceInteractive();
    }

    public void OpenWorkspaceControlCenter()
    {
        try
        {
            Log("Opening Workspace Control Center from menu");
            PrepareForSecondaryWindow();
            var window = new WorkspaceControlCenterWindow(_windowManager, _settingsService)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true
            };
            window.Show();
            window.Activate();
            window.Dispatcher.BeginInvoke(() =>
            {
                window.Topmost = false;
                window.Activate();
            }, DispatcherPriority.ApplicationIdle);
            CloseAfterSecondaryWindowShown(window);
            Log("Workspace Control Center show requested");
        }
        catch (Exception ex)
        {
            Log($"Workspace Control Center failed: {ex}");
            RestoreAfterSecondaryWindowFailure();
            ThemedMessageBox.Show(
                null,
                $"Workspace Control Center could not open.\n\n{ex.Message}",
                "Workspace Control Center",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    public void OpenWorkspaceSwitcher()
    {
        try
        {
            PrepareForSecondaryWindow();
            var window = new WorkspaceSwitcherWindow(_windowManager, _settingsService)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            window.Show();
            window.Activate();
            CloseAfterSecondaryWindowShown(window);
        }
        catch (Exception ex)
        {
            RestoreAfterSecondaryWindowFailure();
            ThemedMessageBox.Show(
                null,
                $"Workspace Switcher could not open.\n\n{ex.Message}",
                "Workspace Switcher",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ShowWorkspacePopup(FrameworkElement? placementTarget = null)
    {
        try
        {
            CloseWorkspacePopup();
            SuppressNextDeactivateClose();

            var view = new WorkspacesView();
            view.SetContext(_workspaceService, _windowManager, _settingsService, this);

             var border = new Border
             {
                 BorderThickness = new Thickness(1),
                 Padding = new Thickness(8),
                 Child = view
             };
            border.SetResourceReference(Border.BackgroundProperty, "FlyoutBackgroundBrush");
            border.SetResourceReference(Border.BorderBrushProperty, "MenuBorderBrush");
            border.SetResourceReference(Border.CornerRadiusProperty, "AirBarCornerRadius");
            border.SetResourceReference(Border.BorderThicknessProperty, "AirBarBorderThickness");

            _workspacePopup = new Popup
            {
                PlacementTarget = placementTarget ?? WorkspaceButton,
                Placement = PlacementMode.Bottom,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                StaysOpen = false,
                Child = border
            };

            _workspacePopup.Closed += (s, e) => _workspacePopup = null;
            _workspacePopup.IsOpen = true;
        }
        catch { }
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
        try
        {
            PrepareForSecondaryWindow();
            var settingsWindow = new SettingsWindow(_settingsService)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            settingsWindow.SettingsApplied += (s, e) =>
            {
                _settings = _settingsService.Settings;
                DataContext = _settings;
                RefreshWindows();
            };
            settingsWindow.Closed += (s, e) =>
            {
                _settings = _settingsService.Settings;
                DataContext = _settings;
                RefreshWindows();
            };
            settingsWindow.Show();
            settingsWindow.Activate();
            CloseAfterSecondaryWindowShown(settingsWindow);
        }
        catch (Exception ex)
        {
            RestoreAfterSecondaryWindowFailure();
            ThemedMessageBox.Show(
                null,
                $"Settings could not open.\n\n{ex.Message}",
                "Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
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
                    PrepareForSecondaryWindow();
                    _historyWindow.Activate();
                    CloseAfterSecondaryWindowShown(_historyWindow);
                    return;
                }

                _historyWindow = null;
            }

            PrepareForSecondaryWindow();
            _historyWindow = new HistoryWindow(_historyService, _settings.HistoryFilter);
            _historyWindow.Closed += (s, e) => _historyWindow = null;
            _historyWindow.Show();
            _historyWindow.Activate();
            CloseAfterSecondaryWindowShown(_historyWindow);
        }
        catch
        {
            RestoreAfterSecondaryWindowFailure();
        }
    }

    private void OnHistoryClick(object sender, RoutedEventArgs e)
    {
        OpenHistoryWindow();
    }

    private void OnWorkspaceClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        OpenWorkspaceControlCenter();
    }

    private void OnWorkspaceCaptureClick(object sender, RoutedEventArgs e)
    {
        ActivateForInput();
        ShowWorkspacePopup(WorkspaceCaptureButton);
    }

    private void OnQuickSettingsClick(object sender, RoutedEventArgs e)
    {
        ActivateForInput();
        AuxiliaryControls.ShowSettingsFlyout(QuickSettingsButton);
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

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

    private sealed class AutoCloseSuspension : IDisposable
    {
        private TaskbarMenuWindow? _owner;

        public AutoCloseSuspension(TaskbarMenuWindow owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            _owner?.ResumeAutoClose();
            _owner = null;
        }
    }

}
