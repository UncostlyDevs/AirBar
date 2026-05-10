using System.IO;
using FloatingTaskbarMenu.Core;
using FloatingTaskbarMenu.Models;

var tests = new (string Name, Action Run)[]
{
    ("legacy workspace migration fills blueprint fields", LegacyWorkspaceMigration),
    ("normalized bounds remap across monitors", NormalizedBoundsRemap),
    ("exact monitor keeps raw bounds", ExactMonitorKeepsRawBounds),
    ("monitor fallback prefers similar monitor", MonitorFallbackPrefersSimilarity),
    ("clamp keeps remapped windows visible", ClampKeepsWindowsVisible),
    ("match scoring separates confidence levels", MatchScoringConfidence),
    ("duplicate handles are excluded from matching", DuplicateHandleExclusion),
    ("document resolver finds image targets", DocumentResolverFindsImage),
    ("missing executable and document targets are reported", MissingTargetsReported),
    ("preview counts missing targets and remaps", PreviewCountsMissingTargetsAndRemaps),
    ("workspace picker text stays readable", WorkspacePickerTextFormatting),
    ("restore result counts are correct", RestoreResultCounts)
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{test.Name}: {ex.Message}");
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Workspace audit failed:");
    foreach (var failure in failures)
        Console.WriteLine($"- {failure}");
    Environment.Exit(1);
}

Console.WriteLine();
Console.WriteLine($"Workspace audit passed: {tests.Length}/{tests.Length}");

static void LegacyWorkspaceMigration()
{
    var workspace = new Workspace
    {
        SchemaVersion = 0,
        Name = "Bad:Name",
        Items = new List<WorkspaceItem>
        {
            new()
            {
                Id = "",
                ProcessName = "notepad",
                WindowTitle = "notes.txt - Notepad",
                ExecutablePath = @"C:\Windows\System32\notepad.exe",
                ClassName = "Notepad",
                Left = 10,
                Top = 20,
                Width = 640,
                Height = 480
            }
        }
    };

    var migrated = WorkspaceRestoreRules.NormalizeWorkspace(workspace, "Fallback");
    var item = migrated.Items[0];

    Require(migrated.SchemaVersion == WorkspaceRestoreRules.CurrentSchemaVersion, "schema version was not upgraded");
    Require(migrated.Name == "Bad-Name", "workspace name was not sanitized");
    Require(!string.IsNullOrWhiteSpace(item.Id), "item id was not backfilled");
    Require(!item.RawBounds.IsEmpty, "raw bounds were not backfilled");
    Require(item.Fingerprint.ProcessName == "notepad", "fingerprint process was not migrated");
    Require(item.Fingerprint.TitleTokens.Count > 0, "title tokens were not backfilled");
    Require(item.Placement != null, "placement was not backfilled");
}

static void NormalizedBoundsRemap()
{
    var captured = Monitor("captured", true, 0, 0, 1000, 800);
    var current = Monitor("current", true, 50, 100, 2000, 1600);
    var raw = Rect(100, 160, 500, 320);
    var item = Item(raw, captured.Id);
    item.NormalizedBounds = WorkspacePlacementPlanner.NormalizeBoundsToMonitor(raw, captured);

    var target = WorkspacePlacementPlanner.ResolveTargetBounds(item, new[] { captured }, new[] { current }, out var monitor, out var remapped);

    Require(remapped, "monitor should have been marked remapped");
    Require(monitor?.Id == current.Id, "current monitor was not selected");
    Near(target.Left, 250, "remapped left");
    Near(target.Top, 420, "remapped top");
    Near(target.Width, 1000, "remapped width");
    Near(target.Height, 640, "remapped height");
}

static void ExactMonitorKeepsRawBounds()
{
    var monitor = Monitor("same", true, 0, 0, 1920, 1080);
    var raw = Rect(123, 234, 700, 500);
    var item = Item(raw, monitor.Id);
    item.NormalizedBounds = Rect(0.1, 0.1, 0.1, 0.1);

    var target = WorkspacePlacementPlanner.ResolveTargetBounds(item, new[] { monitor }, new[] { monitor }, out _, out var remapped);

    Require(!remapped, "exact monitor should not remap");
    Near(target.Left, raw.Left, "raw left");
    Near(target.Top, raw.Top, "raw top");
    Near(target.Width, raw.Width, "raw width");
    Near(target.Height, raw.Height, "raw height");
}

static void MonitorFallbackPrefersSimilarity()
{
    var captured = Monitor("captured", false, 1920, 0, 1600, 900);
    var currentPrimary = Monitor("primary", true, 0, 0, 1920, 1080);
    var currentSimilar = Monitor("similar", false, 1920, 0, 1600, 900);

    var resolved = WorkspacePlacementPlanner.ResolveCurrentMonitor(
        "missing",
        captured,
        new[] { currentPrimary, currentSimilar },
        out var remapped);

    Require(remapped, "missing monitor should remap");
    Require(resolved?.Id == currentSimilar.Id, "fallback did not prefer similar non-primary monitor");
}

static void ClampKeepsWindowsVisible()
{
    var monitor = Monitor("tiny", true, 0, 0, 500, 300);
    var clamped = WorkspacePlacementPlanner.ClampToWorkArea(Rect(-400, 500, 900, 900), monitor);

    Near(clamped.Left, 0, "clamped left");
    Near(clamped.Top, 0, "clamped top");
    Near(clamped.Width, 500, "clamped width");
    Near(clamped.Height, 300, "clamped height");
}

static void MatchScoringConfidence()
{
    var item = Item(Rect(100, 100, 600, 400), "mon");
    item.ExecutablePath = @"C:\Apps\app.exe";
    item.ProcessName = "app";
    item.ClassName = "AppWindow";
    item.WindowTitle = "Quarterly Report";
    item.Fingerprint = WorkspaceRestoreRules.BuildFingerprint(new WindowInfo
    {
        ExecutablePath = item.ExecutablePath,
        ProcessName = item.ProcessName,
        ClassName = item.ClassName,
        Title = item.WindowTitle
    });

    var exact = Window(1, "Quarterly Report", "app", @"C:\Apps\app.exe", "AppWindow", "mon", 100, 100);
    var medium = Window(2, "Something Else", "app", "", "AppWindow", "other", 900, 900);
    var low = Window(3, "Quarterly Notes", "other", "", "", "other", 900, 900);

    Require(WorkspaceRestoreRules.ToConfidence(WorkspaceRestoreRules.MatchScore(exact, item)) == WorkspaceMatchConfidence.High, "exact match was not high confidence");
    Require(WorkspaceRestoreRules.ToConfidence(WorkspaceRestoreRules.MatchScore(medium, item)) == WorkspaceMatchConfidence.Medium, "process/class match was not medium confidence");
    Require(WorkspaceRestoreRules.ToConfidence(WorkspaceRestoreRules.MatchScore(low, item)) == WorkspaceMatchConfidence.Low, "weak title match was not low confidence");
    Require(!WorkspaceRestoreRules.IsAutoPlaceable(WorkspaceMatchConfidence.Low), "low confidence should not auto-place");
}

static void DuplicateHandleExclusion()
{
    var item = Item(Rect(0, 0, 600, 400), "mon");
    item.ProcessName = "app";
    var first = Window(10, "App", "app", "", "", "mon", 0, 0);
    var second = Window(20, "App", "app", "", "", "mon", 50, 50);

    var match = WorkspaceRestoreRules.FindBestMatch(new[] { item }, new[] { first, second }, handle => handle == first.Handle);

    Require(match?.Window.Handle == second.Handle, "used handle was matched again");
}

static void DocumentResolverFindsImage()
{
    var directory = Path.Combine(Path.GetTempPath(), $"winairbar-doc-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    var image = Path.Combine(directory, "sample photo.jpg");
    File.WriteAllText(image, "not really an image");

    try
    {
        var resolved = WorkspaceDocumentResolver.ResolveDocumentPathFromTitle("sample photo.jpg", new[] { directory }, recentDirectory: "");
        Require(string.Equals(resolved, image, StringComparison.OrdinalIgnoreCase), "image path was not resolved");
    }
    finally
    {
        try { Directory.Delete(directory, recursive: true); } catch { }
    }
}

static void MissingTargetsReported()
{
    var missingExe = new WorkspaceItem { ExecutablePath = @"Z:\not-real\missing.exe" };
    var uri = new WorkspaceItem { ExecutablePath = "ms-settings:" };
    var missingDoc = new WorkspaceItem
    {
        ProcessName = "Photos",
        WindowTitle = "gone.jpg",
        DocumentPath = @"Z:\not-real\gone.jpg"
    };

    Require(WorkspaceRestoreRules.HasMissingExecutablePath(missingExe), "missing exe was not reported");
    Require(!WorkspaceRestoreRules.HasMissingExecutablePath(uri), "URI was reported as missing exe");
    Require(WorkspaceDocumentResolver.IsMissingDocumentTarget(missingDoc), "missing document was not reported");
}

static void RestoreResultCounts()
{
    var result = new WorkspaceRestoreResult
    {
        Items = new List<WorkspaceRestoreItemResult>
        {
            new() { Status = WorkspaceRestoreStatus.Restored },
            new() { Status = WorkspaceRestoreStatus.Launched },
            new() { Status = WorkspaceRestoreStatus.Skipped, IgnoredLowConfidenceMatch = true },
            new() { Status = WorkspaceRestoreStatus.Failed }
        }
    };

    Require(result.RestoredCount == 1, "restored count");
    Require(result.LaunchedCount == 1, "launched count");
    Require(result.SkippedCount == 1, "skipped count");
    Require(result.FailedCount == 1, "failed count");
    Require(result.LowConfidenceCount == 1, "low confidence count");
}

static void PreviewCountsMissingTargetsAndRemaps()
{
    var service = new WorkspaceService();
    var workspace = new Workspace
    {
        SchemaVersion = 1,
        Name = "Preview",
        Monitors = new List<WorkspaceMonitor> { Monitor("captured", true, 0, 0, 1000, 800) },
        Items = new List<WorkspaceItem>
        {
            new()
            {
                ExecutablePath = @"Z:\not-real\missing.exe",
                RawBounds = Rect(0, 0, 500, 400),
                Width = 500,
                Height = 400,
                MonitorId = "captured"
            },
            new()
            {
                ProcessName = "Photos",
                WindowTitle = "gone.jpg",
                DocumentPath = @"Z:\not-real\gone.jpg",
                RawBounds = Rect(0, 0, 500, 400),
                Width = 500,
                Height = 400,
                MonitorId = "captured"
            }
        }
    };

    var preview = service.BuildPreview(workspace, new[] { Monitor("current", true, 0, 0, 1000, 800) });

    Require(preview.IsBlueprint, "legacy workspace was not normalized to blueprint preview");
    Require(preview.AppCount == 2, "app count");
    Require(preview.MissingPathCount == 1, "missing app path count");
    Require(preview.MissingDocumentCount == 1, "missing document target count");
    Require(preview.RemappedMonitorCount == 1, "monitor remap count");
}

static void WorkspacePickerTextFormatting()
{
    var same = WorkspacePreviewFormatter.Format(new WorkspacePreview
    {
        AppCount = 6,
        CapturedMonitorCount = 2,
        CurrentMonitorCount = 2
    });

    Require(same.Info == "6 windows - 2 monitors", $"same-monitor text was '{same.Info}'");
    Require(same.Health == "Ready", "same-monitor health");
    Require(!same.Info.Contains("Blueprint", StringComparison.OrdinalIgnoreCase), "blueprint leaked into same-monitor text");
    Require(!same.Info.Contains("Legacy", StringComparison.OrdinalIgnoreCase), "legacy leaked into same-monitor text");
    Require(!same.Info.Contains("->", StringComparison.Ordinal), "arrow leaked into same-monitor text");

    var changed = WorkspacePreviewFormatter.Format(new WorkspacePreview
    {
        AppCount = 1,
        CapturedMonitorCount = 2,
        CurrentMonitorCount = 1,
        RemappedMonitorCount = 1
    });

    Require(changed.Info == "1 window - saved on 2 monitors, 1 now", $"changed-monitor text was '{changed.Info}'");
    Require(changed.Health == "Monitor changed", "changed-monitor health");
    Require(changed.WarningLabels.Contains("monitor layout changed"), "changed-monitor warning");

    var missing = WorkspacePreviewFormatter.Format(new WorkspacePreview
    {
        AppCount = 3,
        CapturedMonitorCount = 1,
        CurrentMonitorCount = 1,
        MissingPathCount = 1,
        MissingDocumentCount = 2
    });

    Require(missing.Health == "Needs attention", "missing-target health");
    Require(missing.WarningLabels.Contains("1 missing app"), "missing app warning");
    Require(missing.WarningLabels.Contains("2 missing docs"), "missing doc warning");
}

static WorkspaceMonitor Monitor(string id, bool primary, double left, double top, double width, double height)
    => new()
    {
        Id = id,
        IsPrimary = primary,
        Left = left,
        Top = top,
        Width = width,
        Height = height,
        WorkLeft = left,
        WorkTop = top,
        WorkWidth = width,
        WorkHeight = height
    };

static WorkspaceItem Item(WorkspaceRect raw, string monitorId)
    => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        RawBounds = raw,
        Left = raw.Left,
        Top = raw.Top,
        Width = raw.Width,
        Height = raw.Height,
        MonitorId = monitorId
    };

static WorkspaceRect Rect(double left, double top, double width, double height)
    => new()
    {
        Left = left,
        Top = top,
        Width = width,
        Height = height
    };

static WindowInfo Window(nint handle, string title, string process, string path, string className, string monitor, double left, double top)
    => new()
    {
        Handle = handle,
        Title = title,
        ProcessName = process,
        ExecutablePath = path,
        ClassName = className,
        MonitorId = monitor,
        Left = left,
        Top = top,
        Width = 600,
        Height = 400
    };

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void Near(double actual, double expected, string label, double tolerance = 0.001)
{
    if (Math.Abs(actual - expected) > tolerance)
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
}
