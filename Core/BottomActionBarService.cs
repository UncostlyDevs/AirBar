using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using FloatingTaskbarMenu.Models;

namespace FloatingTaskbarMenu.Core;

public class BottomActionBarService
{
    public const int SlotCount = 5;

    private readonly ThemeIconService _themeIconService = new();

    private static readonly List<BottomBuiltInActionDefinition> BuiltIns =
    [
        new() { Id = BottomBuiltInAction.History, Label = "History", IconText = "\uE81C", IconGlyph = "\uE81C" },
        new() { Id = BottomBuiltInAction.Launcher, Label = "Launcher", IconText = "\uE71D", IconGlyph = "\uE71D" },
        new() { Id = BottomBuiltInAction.Workspaces, Label = "Workspace Center", IconText = "\uE8B7", IconGlyph = "\uE8B7" },
        new() { Id = BottomBuiltInAction.CaptureWorkspace, Label = "Workspace Center", IconText = "\uE8B7", IconGlyph = "\uE8B7" },
        new() { Id = BottomBuiltInAction.WorkspaceSwitcher, Label = "Workspace Center", IconText = "\uE8B7", IconGlyph = "\uE8B7" },
        new() { Id = BottomBuiltInAction.PowerMenu, Label = "Power", IconText = "\uE7E8", IconGlyph = "\uE7E8" },
        new() { Id = BottomBuiltInAction.Settings, Label = "Windows Settings", IconText = "\uE713", IconGlyph = "\uE713" },
        new() { Id = BottomBuiltInAction.AirBarSettings, Label = "WinAirBar Settings", IconText = "\uE713", IconGlyph = "\uE713" },
        new() { Id = BottomBuiltInAction.Lock, Label = "Lock", IconText = "\uE72E", IconGlyph = "\uE72E" },
        new() { Id = BottomBuiltInAction.Sleep, Label = "Sleep", IconText = "\uE708", IconGlyph = "\uE708" },
        new() { Id = BottomBuiltInAction.Shutdown, Label = "Shutdown", IconText = "\uE7E8", IconGlyph = "\uE7E8" },
        new() { Id = BottomBuiltInAction.Restart, Label = "Restart", IconText = "\uE72C", IconGlyph = "\uE72C" },
        new() { Id = BottomBuiltInAction.SignOut, Label = "Sign Out", IconText = "\uF3B1", IconGlyph = "\uF3B1" },
        new() { Id = BottomBuiltInAction.WifiSettings, Label = "Wi-Fi", IconText = "\uE701", IconGlyph = "\uE701" },
        new() { Id = BottomBuiltInAction.VolumeMixer, Label = "Volume", IconText = "\uE767", IconGlyph = "\uE767" },
        new() { Id = BottomBuiltInAction.FrequentApps, Label = "Frequent Apps", IconText = "\uE71D", IconGlyph = "\uE71D" },
        new() { Id = BottomBuiltInAction.OpenAppDataFolder, Label = "App Data", IconText = "\uE8B7", IconGlyph = "\uE8B7" },
        new() { Id = BottomBuiltInAction.TaskManager, Label = "Task Manager", IconText = "\uE9D9", IconGlyph = "\uE9D9" },
        new() { Id = BottomBuiltInAction.FileExplorer, Label = "File Explorer", IconText = "\uE8B7", IconGlyph = "\uE8B7" },
        new() { Id = BottomBuiltInAction.ScreenSnip, Label = "Screen Snip", IconText = "\uE722", IconGlyph = "\uE722" },
        new() { Id = BottomBuiltInAction.ControlPanel, Label = "Control Panel", IconText = "\uE713", IconGlyph = "\uE713" },
        new() { Id = BottomBuiltInAction.DisplaySettings, Label = "Display", IconText = "\uE7F4", IconGlyph = "\uE7F4" },
        new() { Id = BottomBuiltInAction.SoundSettings, Label = "Sound", IconText = "\uE767", IconGlyph = "\uE767" },
        new() { Id = BottomBuiltInAction.BluetoothSettings, Label = "Bluetooth", IconText = "\uE702", IconGlyph = "\uE702" },
        new() { Id = BottomBuiltInAction.ClipboardSettings, Label = "Clipboard", IconText = "\uE8C8", IconGlyph = "\uE8C8" },
        new() { Id = BottomBuiltInAction.NotificationsSettings, Label = "Notifications", IconText = "\uE7ED", IconGlyph = "\uE7ED" },
        new() { Id = BottomBuiltInAction.DefaultAppsSettings, Label = "Default Apps", IconText = "\uECAA", IconGlyph = "\uECAA" },
        new() { Id = BottomBuiltInAction.StorageSettings, Label = "Storage", IconText = "\uEDA2", IconGlyph = "\uEDA2" },
        new() { Id = BottomBuiltInAction.WindowsUpdate, Label = "Windows Update", IconText = "\uE895", IconGlyph = "\uE895" },
        new() { Id = BottomBuiltInAction.DocumentsFolder, Label = "Documents", IconText = "\uE8A5", IconGlyph = "\uE8A5" },
        new() { Id = BottomBuiltInAction.DownloadsFolder, Label = "Downloads", IconText = "\uE896", IconGlyph = "\uE896" },
        new() { Id = BottomBuiltInAction.DesktopFolder, Label = "Desktop", IconText = "\uE80F", IconGlyph = "\uE80F" },
        new() { Id = BottomBuiltInAction.UserProfileFolder, Label = "User Profile", IconText = "\uE77B", IconGlyph = "\uE77B" }
    ];

    public IReadOnlyList<BottomBuiltInActionDefinition> GetBuiltIns() => BuiltIns;

    public BottomBuiltInActionDefinition GetDefinition(BottomBuiltInAction id)
        => BuiltIns.First(d => d.Id == id);

    public List<BottomActionSlot> CreateDefaultSlots()
    {
        return
        [
            CreateBuiltInSlot(0, BottomBuiltInAction.History),
            CreateBuiltInSlot(1, BottomBuiltInAction.Launcher),
            CreateBuiltInSlot(2, BottomBuiltInAction.PowerMenu),
            CreateBuiltInSlot(3, BottomBuiltInAction.Settings),
            CreateBuiltInSlot(4, BottomBuiltInAction.TaskManager)
        ];
    }

    public void EnsureSlots(Settings settings)
    {
        settings.BottomActionSlots ??= [];

        var defaults = CreateDefaultSlots();
        for (var i = 0; i < SlotCount; i++)
        {
            var existing = settings.BottomActionSlots.FirstOrDefault(s => s.SlotIndex == i);
            if (existing == null)
            {
                settings.BottomActionSlots.Add(defaults[i]);
                continue;
            }

            existing.SlotIndex = i;
            RepairBuiltInLabelMismatch(existing);
            if (existing.ActionKind == BottomActionKind.BuiltIn)
                existing.DisplayLabel = GetDefinition(existing.BuiltInAction).Label;
        }

        settings.BottomActionSlots = settings.BottomActionSlots
            .Where(s => s.SlotIndex >= 0 && s.SlotIndex < SlotCount)
            .OrderBy(s => s.SlotIndex)
            .Take(SlotCount)
            .ToList();

        SyncWorkspaceHeaderButtonVisibility(settings);
    }

    public bool HasWorkspaceButton(Settings settings)
        => settings.BottomActionSlots.Any(slot =>
            slot.ActionKind == BottomActionKind.BuiltIn &&
            slot.BuiltInAction == BottomBuiltInAction.Workspaces);

    public void SyncWorkspaceHeaderButtonVisibility(Settings settings)
        => settings.ShowWorkspaces = true;

    private void RepairBuiltInLabelMismatch(BottomActionSlot slot)
    {
        if (slot.ActionKind != BottomActionKind.BuiltIn || string.IsNullOrWhiteSpace(slot.DisplayLabel))
            return;

        var labelMatch = BuiltIns.FirstOrDefault(b => string.Equals(b.Label, slot.DisplayLabel, StringComparison.OrdinalIgnoreCase));
        if (labelMatch != null && labelMatch.Id != slot.BuiltInAction)
            slot.BuiltInAction = labelMatch.Id;
    }

    public BottomActionSlot CreateBuiltInSlot(int slotIndex, BottomBuiltInAction action)
    {
        var definition = GetDefinition(action);
        return new BottomActionSlot
        {
            SlotIndex = slotIndex,
            ActionKind = BottomActionKind.BuiltIn,
            BuiltInAction = action,
            DisplayLabel = definition.Label,
            UseAutoIcon = true
        };
    }

    public BottomActionSlot CreateCustomSlot(int slotIndex, string targetPath, string label)
    {
        return new BottomActionSlot
        {
            SlotIndex = slotIndex,
            ActionKind = BottomActionKind.Custom,
            DisplayLabel = label,
            TargetPath = targetPath,
            UseAutoIcon = true
        };
    }

    public string GetDisplayLabel(BottomActionSlot slot)
    {
        if (slot.ActionKind == BottomActionKind.BuiltIn)
            return GetDefinition(slot.BuiltInAction).Label;

        if (!string.IsNullOrWhiteSpace(slot.DisplayLabel))
            return slot.DisplayLabel;

        return Path.GetFileNameWithoutExtension(slot.TargetPath);
    }

    public string GetIconText(BottomActionSlot slot)
    {
        if (slot.ActionKind == BottomActionKind.BuiltIn)
            return GetDefinition(slot.BuiltInAction).IconGlyph;

        var label = GetDisplayLabel(slot);
        return string.IsNullOrWhiteSpace(label) ? "\uE8A5" : label[..1].ToUpperInvariant();
    }

    public string GetIconFontFamily(BottomActionSlot slot)
        => slot.ActionKind == BottomActionKind.BuiltIn
            ? GetDefinition(slot.BuiltInAction).IconFontFamily
            : "Segoe UI Variable, Segoe UI";

    public BitmapSource? GetIcon(BottomActionSlot slot)
    {
        if (!slot.UseAutoIcon && !string.IsNullOrWhiteSpace(slot.CustomIconPath))
            return LoadImageOrAssociatedIcon(slot.CustomIconPath);

        if (slot.ActionKind == BottomActionKind.BuiltIn && slot.UseAutoIcon)
        {
            var themeIcon = _themeIconService.GetIcon(slot.BuiltInAction);
            if (themeIcon != null)
                return themeIcon;
        }

        if (slot.ActionKind == BottomActionKind.Custom && !string.IsNullOrWhiteSpace(slot.TargetPath))
            return LoadImageOrAssociatedIcon(slot.TargetPath);

        return null;
    }

    public BitmapSource? GetAssociatedIcon(string path)
        => LoadImageOrAssociatedIcon(path);

    public void LaunchCustomTarget(BottomActionSlot slot)
    {
        if (string.IsNullOrWhiteSpace(slot.TargetPath))
            return;

        var startInfo = new ProcessStartInfo
        {
            FileName = slot.TargetPath,
            UseShellExecute = true
        };

        if (!string.IsNullOrWhiteSpace(slot.Arguments))
            startInfo.Arguments = slot.Arguments;

        if (!string.IsNullOrWhiteSpace(slot.WorkingDirectory))
            startInfo.WorkingDirectory = slot.WorkingDirectory;

        Process.Start(startInfo);
    }

    public string GetAppDataDirectory()
        => AppIdentity.AppDataDirectory;

    private BitmapSource? LoadImageOrAssociatedIcon(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".ico")
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(path, UriKind.Absolute);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }

                using var icon = Icon.ExtractAssociatedIcon(path);
                if (icon != null)
                {
                    var source = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(32, 32));
                    source.Freeze();
                    return source;
                }
            }
        }
        catch { }

        return null;
    }
}
