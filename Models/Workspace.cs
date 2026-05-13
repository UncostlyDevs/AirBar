namespace FloatingTaskbarMenu.Models;

public class Workspace
{
    public int SchemaVersion { get; set; } = 2;
    public string Name { get; set; } = "Workspace";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string CapturedProfile { get; set; } = "Default";
    public string CapturedTheme { get; set; } = "Dark";
    public bool ShowWindowList { get; set; } = true;
    public bool ShowSystemTray { get; set; }
    public bool ShowAuxiliaryControls { get; set; } = true;
    public WorkspaceMetadata Metadata { get; set; } = new();
    public List<WorkspaceMonitor> Monitors { get; set; } = new();
    public List<WorkspaceItem> Items { get; set; } = new();
}

public class WorkspaceItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string Arguments { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public string DocumentPath { get; set; } = "";
    public string MonitorId { get; set; } = "";
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public WindowPlacementState PlacementState { get; set; } = WindowPlacementState.Normal;
    public WorkspaceAppFingerprint Fingerprint { get; set; } = new();
    public WorkspaceRect RawBounds { get; set; } = new();
    public WorkspaceRect NormalizedBounds { get; set; } = new();
    public WorkspaceWindowPlacement Placement { get; set; } = new();
    public string RestorePolicy { get; set; } = "Auto";
}

public class WorkspaceMonitor
{
    public string Id { get; set; } = "";
    public bool IsPrimary { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double WorkLeft { get; set; }
    public double WorkTop { get; set; }
    public double WorkWidth { get; set; }
    public double WorkHeight { get; set; }
}

public class WorkspaceAppFingerprint
{
    public string ExecutablePath { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public string NormalizedTitle { get; set; } = "";
    public List<string> TitleTokens { get; set; } = new();
}

public class WorkspaceRect
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public bool IsEmpty => Width <= 0 || Height <= 0;
}

public class WorkspaceWindowPlacement
{
    public bool HasPlacement { get; set; }
    public WindowPlacementState State { get; set; } = WindowPlacementState.Normal;
    public int ShowCommand { get; set; }
    public WorkspaceRect NormalPosition { get; set; } = new();
}

public class WorkspaceRestoreResult
{
    public string WorkspaceName { get; set; } = "";
    public int RestoredCount => Items.Count(i => i.Status == WorkspaceRestoreStatus.Restored);
    public int LaunchedCount => Items.Count(i => i.Status == WorkspaceRestoreStatus.Launched);
    public int FailedCount => Items.Count(i => i.Status == WorkspaceRestoreStatus.Failed);
    public int SkippedCount => Items.Count(i => i.Status == WorkspaceRestoreStatus.Skipped);
    public int LowConfidenceCount => Items.Count(i => i.IgnoredLowConfidenceMatch || i.MatchConfidence == WorkspaceMatchConfidence.Low);
    public List<WorkspaceRestoreItemResult> Items { get; set; } = new();
}

public class WorkspaceRestoreItemResult
{
    public string DisplayName { get; set; } = "";
    public WorkspaceRestoreStatus Status { get; set; }
    public string Message { get; set; } = "";
    public int MatchScore { get; set; }
    public WorkspaceMatchConfidence MatchConfidence { get; set; } = WorkspaceMatchConfidence.None;
    public bool IgnoredLowConfidenceMatch { get; set; }
    public string TargetMonitorId { get; set; } = "";
}

public enum WorkspaceRestoreStatus
{
    Restored,
    Launched,
    Failed,
    Skipped
}

public enum WorkspaceMatchConfidence
{
    None,
    Low,
    Medium,
    High
}

public class WorkspacePreview
{
    public int AppCount { get; set; }
    public int CapturedMonitorCount { get; set; }
    public int CurrentMonitorCount { get; set; }
    public int MissingPathCount { get; set; }
    public int MissingDocumentCount { get; set; }
    public int RemappedMonitorCount { get; set; }
    public bool IsBlueprint { get; set; }
}
