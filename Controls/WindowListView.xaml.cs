using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfButton = System.Windows.Controls.Button;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfApplication = System.Windows.Application;
using FloatingTaskbarMenu.Core;
using FloatingTaskbarMenu.Models;
using FloatingTaskbarMenu.Windows;

namespace FloatingTaskbarMenu.Controls;

public partial class WindowListView : WpfUserControl
{
    private WindowManager? _windowManager;
    private PinnedProfileService? _pinnedProfileService;
    private AppLauncherService? _appLauncherService;
    private WindowHistoryService? _historyService;
    private SettingsService? _settingsService;
    private PinnedProfile? _currentPinnedProfile;
    private List<WindowInfo> _currentWindows = new();

    public WindowListView()
    {
        InitializeComponent();
    }

    public void RefreshWindows(List<WindowInfo> windows)
    {
        try
        {
            _currentWindows = windows;
            var entries = BuildWindowEntries(_currentWindows);
            WindowList.ItemsSource = null;
            WindowList.ItemsSource = entries;
        }
        catch { }
    }

    private List<WindowListEntry> BuildWindowEntries(List<WindowInfo> windows)
    {
        foreach (var window in windows)
            window.IsPinned = IsWindowPinned(window);

        var entries = new List<WindowListEntry>();
        var grouped = windows.GroupBy(w => w.ProcessName);

        foreach (var processGroup in grouped)
        {
            var orderedWindows = processGroup
                .OrderByDescending(w => w.IsPinned)
                .ThenBy(w => w.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (orderedWindows.Count == 1)
            {
                entries.Add(new WindowListEntry { Window = orderedWindows[0] });
                continue;
            }

            entries.Add(new WindowListEntry
            {
                Group = new WindowGroup
                {
                    GroupName = processGroup.Key,
                    GroupIcon = orderedWindows.FirstOrDefault()?.Icon,
                    IsPinned = orderedWindows.Any(w => w.IsPinned),
                    Windows = orderedWindows
                }
            });
        }

        return entries
            .OrderByDescending(e => e.IsPinned)
            .ThenBy(e => e.SortName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void OnWindowClick(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton button && button.DataContext is WindowInfo windowInfo)
        {
            _windowManager?.ActivateWindow(windowInfo.Handle);
            
            // Record launch for app launcher
            if (_appLauncherService != null && !string.IsNullOrEmpty(windowInfo.ProcessName))
            {
                _appLauncherService.AddOrUpdateApp(windowInfo.ProcessName, GetLaunchTarget(windowInfo), windowInfo.Icon);
            }
            
            Window.GetWindow(this)?.Close();
        }
    }

    private void OnWindowRightClick(object sender, WpfMouseEventArgs e)
    {
        if (sender is WpfButton button && button.DataContext is WindowInfo windowInfo)
        {
            if (Window.GetWindow(this) is TaskbarMenuWindow menuWindow)
                menuWindow.SuppressNextDeactivateClose();

            var contextMenu = new ContextMenu();
            if (WpfApplication.Current.Resources["Win11ContextMenuStyle"] is Style ctxStyle)
                contextMenu.Style = ctxStyle;

            AddMenuItem(contextMenu, "Restore", () => { _windowManager?.RestoreWindow(windowInfo.Handle); Window.GetWindow(this)?.Close(); });
            AddMenuItem(contextMenu, "Minimize", () => { _windowManager?.MinimizeWindow(windowInfo.Handle); Window.GetWindow(this)?.Close(); });
            AddMenuItem(contextMenu, "Maximize", () => { _windowManager?.MaximizeWindow(windowInfo.Handle); Window.GetWindow(this)?.Close(); });
            AddMenuItem(contextMenu, "Close",    () =>
            {
                RecordClosedWindowHistory(windowInfo);
                _windowManager?.CloseWindow(windowInfo.Handle);
                RefreshWindows(_windowManager?.GetWindows() ?? new());
            });
            AddMenuItem(contextMenu, "Force Close", () => ForceCloseWindow(windowInfo));

            contextMenu.Items.Add(new Separator());
            AddMenuItem(contextMenu, "Center", () => { _windowManager?.CenterWindow(windowInfo); Window.GetWindow(this)?.Close(); });
            AddMenuItem(contextMenu, "Snap Left", () => { _windowManager?.SnapWindowLeft(windowInfo); Window.GetWindow(this)?.Close(); });
            AddMenuItem(contextMenu, "Snap Right", () => { _windowManager?.SnapWindowRight(windowInfo); Window.GetWindow(this)?.Close(); });
            AddMoveToMonitorMenu(contextMenu, windowInfo);

            contextMenu.Items.Add(new Separator());
            var hasExecutablePath = HasExecutablePath(windowInfo);
            AddMenuItem(contextMenu, "Open File Location", () => OpenFileLocation(windowInfo), hasExecutablePath);
            AddMenuItem(contextMenu, "Reopen as Administrator", () => ReopenAsAdministrator(windowInfo), hasExecutablePath);

            contextMenu.Items.Add(new Separator());
            var isPinned = IsWindowPinned(windowInfo);
            AddMenuItem(contextMenu, isPinned ? "Unpin Window" : "Pin Window", () => TogglePin(windowInfo));

            var launcherTarget = GetLaunchTarget(windowInfo);
            var launcherApp = _appLauncherService?.GetApp(launcherTarget);
            AddMenuItem(contextMenu, launcherApp?.IsPinned == true ? "Unpin from Launcher" : "Pin to Launcher", () => TogglePinLauncher(windowInfo));

            contextMenu.PlacementTarget = button;
            contextMenu.IsOpen = true;
            e.Handled = true;
        }
    }

    private void AddMoveToMonitorMenu(ContextMenu contextMenu, WindowInfo windowInfo)
    {
        var monitors = _windowManager?.GetMonitors() ?? new List<WorkspaceMonitor>();
        var item = new MenuItem
        {
            Header = "Move to Monitor",
            IsEnabled = WindowControlPlanner.CanMoveToMonitor(monitors)
        };

        if (WpfApplication.Current.Resources["Win11MenuItemStyle"] is Style itemStyle)
            item.Style = itemStyle;

        if (item.IsEnabled)
        {
            for (var i = 0; i < monitors.Count; i++)
            {
                var monitor = monitors[i];
                var child = new MenuItem
                {
                    Header = MonitorLabel(monitor, i)
                };
                if (WpfApplication.Current.Resources["Win11MenuItemStyle"] is Style childStyle)
                    child.Style = childStyle;
                child.Click += (s, e) =>
                {
                    _windowManager?.MoveWindowToMonitor(windowInfo, monitor);
                    Window.GetWindow(this)?.Close();
                };
                item.Items.Add(child);
            }
        }

        contextMenu.Items.Add(item);
    }

    private static string MonitorLabel(WorkspaceMonitor monitor, int index)
        => $"{(monitor.IsPrimary ? "Primary" : $"Monitor {index + 1}")} ({monitor.WorkWidth:0}x{monitor.WorkHeight:0})";

    private void AddMenuItem(ContextMenu menu, string header, Action action, bool isEnabled = true)
    {
        var item = new MenuItem { Header = header, IsEnabled = isEnabled };
        if (WpfApplication.Current.Resources["Win11MenuItemStyle"] is Style itemStyle)
            item.Style = itemStyle;
        item.Click += (s, e) => action();
        menu.Items.Add(item);
    }

    public void SetWindowManager(WindowManager windowManager)
    {
        _windowManager = windowManager;
    }

    public void SetPinnedProfileService(PinnedProfileService service, string profileName)
    {
        _pinnedProfileService = service;
        _currentPinnedProfile = service.LoadProfile(profileName);
    }

    public void SetAppLauncherService(AppLauncherService service)
    {
        _appLauncherService = service;
    }

    public void SetHistoryService(WindowHistoryService historyService, SettingsService settingsService)
    {
        _historyService = historyService;
        _settingsService = settingsService;
    }

    private bool IsWindowPinned(WindowInfo windowInfo)
    {
        if (_currentPinnedProfile == null) return false;
        return _currentPinnedProfile.PinnedWindows.Any(p => IsPinnedWindowMatch(p, windowInfo));
    }

    private void TogglePin(WindowInfo windowInfo)
    {
        if (_pinnedProfileService == null || _currentPinnedProfile == null) return;

        var existing = _currentPinnedProfile.PinnedWindows.FirstOrDefault(p => IsPinnedWindowMatch(p, windowInfo));
        if (existing != null)
        {
            _currentPinnedProfile.PinnedWindows.Remove(existing);
        }
        else
        {
            _currentPinnedProfile.PinnedWindows.Add(new PinnedWindow
            {
                ExecutablePath = GetPinKey(windowInfo),
                WindowTitle = windowInfo.Title,
                ProcessName = windowInfo.ProcessName
            });
        }

        _pinnedProfileService.SaveProfile(_currentPinnedProfile);
        RefreshWindows(_currentWindows);
    }

    private void TogglePinLauncher(WindowInfo windowInfo)
    {
        if (_appLauncherService == null) return;

        var launchTarget = GetLaunchTarget(windowInfo);
        var launcherApp = _appLauncherService.GetApp(launchTarget);
        if (launcherApp != null)
        {
            if (launcherApp.IsPinned)
                _appLauncherService.UnpinApp(launchTarget);
            else
                _appLauncherService.PinApp(launchTarget);
        }
        else
        {
            _appLauncherService.AddOrUpdateApp(windowInfo.ProcessName, launchTarget, windowInfo.Icon);
            _appLauncherService.PinApp(launchTarget);
        }
    }

    private string GetPinKey(WindowInfo windowInfo)
        => !string.IsNullOrWhiteSpace(windowInfo.ExecutablePath) ? windowInfo.ExecutablePath : windowInfo.ProcessName;

    private string GetLaunchTarget(WindowInfo windowInfo)
        => !string.IsNullOrWhiteSpace(windowInfo.ExecutablePath) ? windowInfo.ExecutablePath : windowInfo.ProcessName;

    private bool IsPinnedWindowMatch(PinnedWindow pinnedWindow, WindowInfo windowInfo)
    {
        var pinKey = GetPinKey(windowInfo);
        return string.Equals(pinnedWindow.ExecutablePath, pinKey, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(pinnedWindow.ProcessName, windowInfo.ProcessName, StringComparison.OrdinalIgnoreCase);
    }

    private bool HasExecutablePath(WindowInfo windowInfo)
        => !string.IsNullOrWhiteSpace(windowInfo.ExecutablePath) && File.Exists(windowInfo.ExecutablePath);

    private void OpenFileLocation(WindowInfo windowInfo)
    {
        try
        {
            if (!HasExecutablePath(windowInfo)) return;
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{windowInfo.ExecutablePath}\"",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void ReopenAsAdministrator(WindowInfo windowInfo)
    {
        try
        {
            if (!HasExecutablePath(windowInfo)) return;
            Process.Start(new ProcessStartInfo
            {
                FileName = windowInfo.ExecutablePath,
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch { }
    }

    private void RecordClosedWindowHistory(WindowInfo windowInfo)
    {
        try
        {
            if (_historyService == null || _settingsService?.Settings.TrackWindowHistory != true)
                return;

            _historyService.AddWindowHistory(new WindowHistory
            {
                Title = windowInfo.Title,
                ProcessName = windowInfo.ProcessName,
                ExecutablePath = GetLaunchTarget(windowInfo),
                ClosedTime = DateTime.Now
            });
        }
        catch { }
    }

    private void ForceCloseWindow(WindowInfo windowInfo)
    {
        var owner = Window.GetWindow(this);
        using var autoCloseSuspension = owner is TaskbarMenuWindow menu ? menu.SuspendAutoClose() : null;

        var title = string.IsNullOrWhiteSpace(windowInfo.Title) ? windowInfo.ProcessName : windowInfo.Title;
        var result = ThemedMessageBox.Show(
            owner,
            $"Force Close will immediately terminate this app and may discard unsaved work:\n\n{title}\n\nUse normal Close first unless the window is frozen.",
            "Force Close Window",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        RecordClosedWindowHistory(windowInfo);
        var closed = _windowManager?.ForceCloseWindow(windowInfo) == true;
        RefreshWindows(_windowManager?.GetWindows() ?? new());

        if (!closed)
        {
            ThemedMessageBox.Show(
                owner,
                "WinAirBar could not force close that window. It may already be closed, elevated, protected, or owned by a system process.",
                "Force Close Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
