namespace FloatingTaskbarMenu.Models;

public enum BottomActionKind
{
    BuiltIn,
    Custom
}

public enum BottomBuiltInAction
{
    History,
    Launcher,
    Workspaces,
    CaptureWorkspace,
    WorkspaceSwitcher,
    PowerMenu,
    Settings,
    AirBarSettings,
    Lock,
    Sleep,
    Shutdown,
    Restart,
    SignOut,
    WifiSettings,
    VolumeMixer,
    FrequentApps,
    OpenAppDataFolder,
    TaskManager,
    FileExplorer,
    ScreenSnip,
    ControlPanel,
    DisplaySettings,
    SoundSettings,
    BluetoothSettings,
    ClipboardSettings,
    NotificationsSettings,
    DefaultAppsSettings,
    StorageSettings,
    WindowsUpdate,
    DocumentsFolder,
    DownloadsFolder,
    DesktopFolder,
    UserProfileFolder
}

public class BottomActionSlot
{
    public int SlotIndex { get; set; }
    public BottomActionKind ActionKind { get; set; } = BottomActionKind.BuiltIn;
    public BottomBuiltInAction BuiltInAction { get; set; } = BottomBuiltInAction.History;
    public string DisplayLabel { get; set; } = "";
    public string TargetPath { get; set; } = "";
    public string Arguments { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public bool UseAutoIcon { get; set; } = true;
    public string CustomIconPath { get; set; } = "";
    public bool CustomLaunchConfirmed { get; set; }
}

public class BottomBuiltInActionDefinition
{
    public BottomBuiltInAction Id { get; init; }
    public string Label { get; init; } = "";
    public string IconText { get; init; } = "";
    public string IconGlyph { get; init; } = "";
    public string IconFontFamily { get; init; } = "Segoe Fluent Icons, Segoe MDL2 Assets";
}
