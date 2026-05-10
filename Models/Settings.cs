using System.Collections.Generic;

namespace FloatingTaskbarMenu.Models;

public class Settings
{
    public int LongPressDurationMs { get; set; } = 600;
    public MouseButton TriggerButton { get; set; } = MouseButton.Left;
    public bool AutoStartWithWindows { get; set; } = false;
    public bool ShowWindowList { get; set; } = true;
    public bool ShowWorkspaces { get; set; } = true;
    public bool ShowSystemTray { get; set; } = false;
    public bool ShowAuxiliaryControls { get; set; } = true;
    public bool ShowWindowPreviews { get; set; } = true;
    public bool DarkMode { get; set; } = true;
    public double MenuOpacity { get; set; } = 0.95;
    public int MenuWidth { get; set; } = 300;
    public string AccentColor { get; set; } = "#60CDFF";
    public bool AllMonitors { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public string CurrentPinnedProfile { get; set; } = "Default";
    public bool AutoSavePinnedProfile { get; set; } = true;
    public Dictionary<string, MonitorLayoutSettings> MonitorLayouts { get; set; } = new();
    public HistoryFilter HistoryFilter { get; set; } = HistoryFilter.Day;
    public bool TrackWindowHistory { get; set; } = true;
    public string CurrentTheme { get; set; } = "Dark";
    public bool MinimalMode { get; set; } = false;
    public double CornerRadius { get; set; } = 8;
    public double FontSize { get; set; } = 12;
    public bool UseCustomTextColors { get; set; } = false;
    public string LightTextPrimaryColor { get; set; } = "#111111";
    public string LightTextSecondaryColor { get; set; } = "#5F5F5F";
    public string DarkTextPrimaryColor { get; set; } = "#F3F3F3";
    public string DarkTextSecondaryColor { get; set; } = "#C8C8C8";
    public bool DefaultWin11StyleMigrated { get; set; } = false;
    public Dictionary<string, ThemeTextColorSettings> CustomTextColorsByTheme { get; set; } = new();
    public List<BottomActionSlot> BottomActionSlots { get; set; } = new();
}

public class ThemeTextColorSettings
{
    public bool Enabled { get; set; } = false;
    public string PrimaryColor { get; set; } = "";
    public string SecondaryColor { get; set; } = "";
}

public class MonitorLayoutSettings
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Opacity { get; set; }
}

public enum MouseButton
{
    Left = 0x0201,
    Right = 0x0204,
    Middle = 0x0207
}
