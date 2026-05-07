using System.Windows;
using System.Windows.Controls;
using FloatingTaskbarMenu.Core;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace FloatingTaskbarMenu.Controls;

public partial class SettingsFlyoutView : WpfUserControl
{
    private readonly SettingsService _settingsService;
    private readonly ThemeService _themeService = new();
    private readonly Action? _settingsApplied;

    public SettingsFlyoutView(SettingsService settingsService, Action? settingsApplied = null)
    {
        _settingsService = settingsService;
        _settingsApplied = settingsApplied;
        InitializeComponent();
        LoadState();
    }

    private void LoadState()
    {
        var settings = _settingsService.Settings;
        WindowListCheck.IsChecked = settings.ShowWindowList;
        TrayCheck.IsChecked = settings.ShowSystemTray;
        ActionsCheck.IsChecked = settings.ShowAuxiliaryControls;
        HistoryCheck.IsChecked = settings.TrackWindowHistory;
        settings.CurrentTheme = ThemeService.NormalizeThemeName(settings.CurrentTheme);
        ThemeCombo.ItemsSource = _themeService.GetThemeNames();
        ThemeCombo.SelectedItem = settings.CurrentTheme;
    }

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Settings;
        settings.ShowWindowList = WindowListCheck.IsChecked == true;
        settings.ShowSystemTray = TrayCheck.IsChecked == true;
        settings.ShowAuxiliaryControls = ActionsCheck.IsChecked == true;
        settings.TrackWindowHistory = HistoryCheck.IsChecked == true;

        if (ThemeCombo.SelectedItem is string themeName)
        {
            settings.CurrentTheme = ThemeService.NormalizeThemeName(themeName);
            var theme = _themeService.LoadTheme(settings.CurrentTheme);
            settings.DarkMode = theme.DarkMode;
            settings.AccentColor = theme.AccentColor;
            settings.CornerRadius = theme.CornerRadius;
            settings.FontSize = theme.FontSize;
            settings.MinimalMode = theme.MinimalMode;
            _themeService.ApplyThemeResources(settings);
        }

        _settingsService.Save();
        _settingsApplied?.Invoke();
    }
}
