using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using FloatingTaskbarMenu.Models;
using WpfApplication = System.Windows.Application;

namespace FloatingTaskbarMenu.Core;

public sealed class ThemeIconService
{
    private const string CurrentThemeResourceKey = "AirBarCurrentTheme";

    private static readonly IReadOnlyDictionary<ThemeIconKind, string> Win31Icons = new Dictionary<ThemeIconKind, string>
    {
        [ThemeIconKind.Back] = "pixel/arrow-left.png",
        [ThemeIconKind.Next] = "pixel/arrow-right.png",
        [ThemeIconKind.Pin] = "pixel/pin.png",
        [ThemeIconKind.Settings] = "win31/settings.png",
        [ThemeIconKind.History] = "win31/history.png",
        [ThemeIconKind.Lock] = "pixel/lock.png",
        [ThemeIconKind.Sleep] = "win31/sleep.png",
        [ThemeIconKind.Shutdown] = "win31/power.png",
        [ThemeIconKind.Restart] = "win31/restart.png",
        [ThemeIconKind.SignOut] = "pixel/logout.png",
        [ThemeIconKind.VolumeDown] = "pixel/volume-down.png",
        [ThemeIconKind.VolumeMute] = "pixel/volume-mute.png",
        [ThemeIconKind.VolumeUp] = "pixel/volume-up.png",
        [ThemeIconKind.SoundSettings] = "win31/settings.png",
        [ThemeIconKind.Network] = "pixel/network.png",
        [ThemeIconKind.Folder] = "win31/open-folder.png",
        [ThemeIconKind.Launcher] = "win31/launcher.png",
        [ThemeIconKind.Power] = "win31/power.png"
    };

    private static readonly IReadOnlyDictionary<ThemeIconKind, string> Win9xIcons = new Dictionary<ThemeIconKind, string>
    {
        [ThemeIconKind.Back] = "pixel/arrow-left.png",
        [ThemeIconKind.Next] = "pixel/arrow-right.png",
        [ThemeIconKind.Pin] = "pixel/pin.png",
        [ThemeIconKind.Settings] = "win9x/settings.ico",
        [ThemeIconKind.History] = "win9x/history.ico",
        [ThemeIconKind.Lock] = "pixel/lock.png",
        [ThemeIconKind.Sleep] = "win9x/sleep.ico",
        [ThemeIconKind.Shutdown] = "win9x/power.ico",
        [ThemeIconKind.Restart] = "win9x/restart.ico",
        [ThemeIconKind.SignOut] = "pixel/logout.png",
        [ThemeIconKind.VolumeDown] = "pixel/volume-down.png",
        [ThemeIconKind.VolumeMute] = "pixel/volume-mute.png",
        [ThemeIconKind.VolumeUp] = "pixel/volume-up.png",
        [ThemeIconKind.SoundSettings] = "win9x/settings.ico",
        [ThemeIconKind.Network] = "win9x/network.ico",
        [ThemeIconKind.Folder] = "win9x/open-folder.ico",
        [ThemeIconKind.Launcher] = "win9x/launcher.ico",
        [ThemeIconKind.Power] = "win9x/power.ico"
    };

    private static readonly IReadOnlyDictionary<ThemeIconKind, string> XpIcons = new Dictionary<ThemeIconKind, string>
    {
        [ThemeIconKind.Back] = "pixel/arrow-left.png",
        [ThemeIconKind.Next] = "pixel/arrow-right.png",
        [ThemeIconKind.Pin] = "pixel/pin.png",
        [ThemeIconKind.Settings] = "xp/settings.ico",
        [ThemeIconKind.History] = "xp/history.ico",
        [ThemeIconKind.Lock] = "pixel/lock.png",
        [ThemeIconKind.Sleep] = "xp/sleep.ico",
        [ThemeIconKind.Shutdown] = "xp/power.ico",
        [ThemeIconKind.Restart] = "xp/restart.ico",
        [ThemeIconKind.SignOut] = "pixel/logout.png",
        [ThemeIconKind.VolumeDown] = "pixel/volume-down.png",
        [ThemeIconKind.VolumeMute] = "pixel/volume-mute.png",
        [ThemeIconKind.VolumeUp] = "pixel/volume-up.png",
        [ThemeIconKind.SoundSettings] = "xp/settings.ico",
        [ThemeIconKind.Network] = "xp/network.png",
        [ThemeIconKind.Folder] = "xp/open-folder.ico",
        [ThemeIconKind.Launcher] = "xp/launcher.ico",
        [ThemeIconKind.Power] = "xp/power.ico"
    };

    private static readonly IReadOnlyDictionary<ThemeIconKind, string> Win7Icons = new Dictionary<ThemeIconKind, string>
    {
        [ThemeIconKind.Back] = "win7/back.png",
        [ThemeIconKind.Next] = "win7/next.png",
        [ThemeIconKind.Pin] = "pixel/pin.png",
        [ThemeIconKind.Settings] = "win7/settings.png",
        [ThemeIconKind.History] = "win7/history.png",
        [ThemeIconKind.Lock] = "win7/lock.png",
        [ThemeIconKind.Sleep] = "win7/sleep.png",
        [ThemeIconKind.Shutdown] = "win7/shutdown.png",
        [ThemeIconKind.Restart] = "win7/restart.png",
        [ThemeIconKind.SignOut] = "win7/sign-out.png",
        [ThemeIconKind.VolumeDown] = "win7/volume-down.png",
        [ThemeIconKind.VolumeMute] = "win7/volume-mute.png",
        [ThemeIconKind.VolumeUp] = "win7/volume-up.png",
        [ThemeIconKind.SoundSettings] = "win7/sound-settings.png",
        [ThemeIconKind.Network] = "win7/network.png",
        [ThemeIconKind.Folder] = "win7/folder.png",
        [ThemeIconKind.Launcher] = "win7/launcher.png",
        [ThemeIconKind.Power] = "win7/shutdown.png"
    };

    private static readonly IReadOnlyDictionary<ThemeIconKind, string> Win10Icons = new Dictionary<ThemeIconKind, string>
    {
        [ThemeIconKind.Back] = "win10/back.png",
        [ThemeIconKind.Next] = "win10/next.png",
        [ThemeIconKind.Pin] = "pixel/pin.png",
        [ThemeIconKind.Settings] = "win10/settings.png",
        [ThemeIconKind.History] = "win10/history.png",
        [ThemeIconKind.Lock] = "win10/lock.png",
        [ThemeIconKind.Sleep] = "win10/sleep.png",
        [ThemeIconKind.Shutdown] = "win10/shutdown.png",
        [ThemeIconKind.Restart] = "win10/restart.png",
        [ThemeIconKind.SignOut] = "win10/sign-out.png",
        [ThemeIconKind.VolumeDown] = "win10/volume-down.png",
        [ThemeIconKind.VolumeMute] = "win10/volume-mute.png",
        [ThemeIconKind.VolumeUp] = "win10/volume-up.png",
        [ThemeIconKind.SoundSettings] = "win10/sound-settings.png",
        [ThemeIconKind.Network] = "win10/network.png",
        [ThemeIconKind.Folder] = "win10/folder.png",
        [ThemeIconKind.Launcher] = "win10/launcher.png",
        [ThemeIconKind.Power] = "win10/power.png"
    };

    private static readonly IReadOnlyDictionary<BottomBuiltInAction, ThemeIconKind> BuiltInIconKinds = new Dictionary<BottomBuiltInAction, ThemeIconKind>
    {
        [BottomBuiltInAction.History] = ThemeIconKind.History,
        [BottomBuiltInAction.Launcher] = ThemeIconKind.Launcher,
        [BottomBuiltInAction.Workspaces] = ThemeIconKind.Folder,
        [BottomBuiltInAction.CaptureWorkspace] = ThemeIconKind.Folder,
        [BottomBuiltInAction.FrequentApps] = ThemeIconKind.Launcher,
        [BottomBuiltInAction.Settings] = ThemeIconKind.Settings,
        [BottomBuiltInAction.AirBarSettings] = ThemeIconKind.Settings,
        [BottomBuiltInAction.PowerMenu] = ThemeIconKind.Power,
        [BottomBuiltInAction.Shutdown] = ThemeIconKind.Shutdown,
        [BottomBuiltInAction.SignOut] = ThemeIconKind.SignOut,
        [BottomBuiltInAction.Lock] = ThemeIconKind.Lock,
        [BottomBuiltInAction.Sleep] = ThemeIconKind.Sleep,
        [BottomBuiltInAction.Restart] = ThemeIconKind.Restart,
        [BottomBuiltInAction.WifiSettings] = ThemeIconKind.Network,
        [BottomBuiltInAction.VolumeMixer] = ThemeIconKind.VolumeMute,
        [BottomBuiltInAction.OpenAppDataFolder] = ThemeIconKind.Folder,
        [BottomBuiltInAction.TaskManager] = ThemeIconKind.Launcher,
        [BottomBuiltInAction.FileExplorer] = ThemeIconKind.Folder,
        [BottomBuiltInAction.ScreenSnip] = ThemeIconKind.Launcher,
        [BottomBuiltInAction.ControlPanel] = ThemeIconKind.Settings,
        [BottomBuiltInAction.DisplaySettings] = ThemeIconKind.Settings,
        [BottomBuiltInAction.SoundSettings] = ThemeIconKind.SoundSettings,
        [BottomBuiltInAction.BluetoothSettings] = ThemeIconKind.Network,
        [BottomBuiltInAction.ClipboardSettings] = ThemeIconKind.Folder,
        [BottomBuiltInAction.NotificationsSettings] = ThemeIconKind.Settings,
        [BottomBuiltInAction.DefaultAppsSettings] = ThemeIconKind.Settings,
        [BottomBuiltInAction.StorageSettings] = ThemeIconKind.Folder,
        [BottomBuiltInAction.WindowsUpdate] = ThemeIconKind.Settings,
        [BottomBuiltInAction.DocumentsFolder] = ThemeIconKind.Folder,
        [BottomBuiltInAction.DownloadsFolder] = ThemeIconKind.Folder,
        [BottomBuiltInAction.DesktopFolder] = ThemeIconKind.Folder,
        [BottomBuiltInAction.UserProfileFolder] = ThemeIconKind.Folder
    };

    public BitmapSource? GetIcon(BottomBuiltInAction action)
    {
        return BuiltInIconKinds.TryGetValue(action, out var kind)
            ? GetIcon(kind)
            : null;
    }

    public BitmapSource? GetIcon(ThemeIconKind kind)
    {
        var themeName = GetCurrentThemeName();
        var iconPath = GetIconPath(themeName, kind);
        return iconPath == null ? null : LoadPackImage(iconPath);
    }

    private static string? GetIconPath(string themeName, ThemeIconKind kind)
    {
        var icons = themeName switch
        {
            "Windows 1.x" or "Windows 3.1" => Win31Icons,
            "Windows 95" or "Windows 98" or "Windows ME" => Win9xIcons,
            "Windows XP Luna" => XpIcons,
            "Windows 7 Aero" => Win7Icons,
            "Windows 10" => Win10Icons,
            _ => null
        };

        return icons != null && icons.TryGetValue(kind, out var iconPath)
            ? $"Assets/ThemeIcons/{iconPath}"
            : null;
    }

    private static string GetCurrentThemeName()
    {
        var resources = WpfApplication.Current?.Resources;
        if (resources != null && resources.Contains(CurrentThemeResourceKey) && resources[CurrentThemeResourceKey] is string themeName)
            return ThemeService.NormalizeThemeName(themeName);

        return "Dark";
    }

    private static BitmapSource? LoadPackImage(string relativePath)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri($"pack://application:,,,/{relativePath}", UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
