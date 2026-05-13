using System.IO;
using System.IO.Compression;
using System.Text.Json;
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
    ("restore result counts are correct", RestoreResultCounts),
    ("bottom action defaults expose five slots", BottomActionDefaultsExposeFiveSlots),
    ("bottom action migration preserves existing four slots", BottomActionMigrationPreservesExistingFourSlots),
    ("bottom action built-ins include workflow dock options", BottomActionBuiltInsIncludeWorkflowDockOptions),
    ("workspace header button stays visible with bottom workspace slot", WorkspaceHeaderButtonStaysVisibleWithBottomWorkspaceSlot),
    ("bottom action labels are automatic for built-ins and custom for user actions", BottomActionLabelsAreAutomaticForBuiltInsAndCustomForUserActions),
    ("workspace rollback snapshots are capped and undo uses latest", WorkspaceRollbackSnapshotsAreCappedAndLatestWins),
    ("manual workspace snapshots are readable JSON", ManualWorkspaceSnapshotsAreReadableJson),
    ("backup export includes only selected sections and readable manifest", BackupExportIncludesOnlySelectedSections),
    ("backup import preview and selected import create pre-import backup", BackupImportPreviewAndSelectedImport),
    ("window control planner handles monitor actions safely", WindowControlPlannerHandlesMonitorActionsSafely),
    ("workspace diff classifies matches launches missing and extras", WorkspaceDiffClassifiesPlanItems),
    ("workspace restore modes build distinct preview plans", WorkspaceRestoreModesBuildPlans),
    ("extra cleanup defaults protect risky windows", ExtraCleanupDefaultsProtectRiskyWindows),
    ("workspace versions cap at ten", WorkspaceVersionsCapAtTen),
    ("workspace timeline records detailed restore items", WorkspaceTimelineRecordsDetailedItems),
    ("workspace suggestions threshold dismiss and never suggest", WorkspaceSuggestionsThresholdDismissNeverSuggest),
    ("workspace rules apply to capture cleanup and position", WorkspaceRulesApplyToCaptureCleanupAndPosition),
    ("launcher tags persist and filter", LauncherTagsPersistAndFilter),
    ("backup conflicts preview and import choices are safe", BackupConflictsPreviewAndChoices)
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

static void BottomActionDefaultsExposeFiveSlots()
{
    var service = new BottomActionBarService();
    var defaults = service.CreateDefaultSlots();

    Require(defaults.Count == BottomActionBarService.SlotCount, "default slot count");
    Require(defaults.Select(slot => slot.SlotIndex).SequenceEqual(new[] { 0, 1, 2, 3, 4 }), "default slot indexes");
    Require(defaults[0].BuiltInAction == BottomBuiltInAction.History, "slot 1 default");
    Require(defaults[1].BuiltInAction == BottomBuiltInAction.Launcher, "slot 2 default");
    Require(defaults[2].BuiltInAction == BottomBuiltInAction.PowerMenu, "slot 3 default");
    Require(defaults[3].BuiltInAction == BottomBuiltInAction.Settings, "slot 4 default");
    Require(defaults[4].BuiltInAction == BottomBuiltInAction.TaskManager, "slot 5 default");
}

static void BottomActionMigrationPreservesExistingFourSlots()
{
    var service = new BottomActionBarService();
    var settings = new Settings
    {
        BottomActionSlots =
        [
            service.CreateBuiltInSlot(0, BottomBuiltInAction.Lock),
            service.CreateBuiltInSlot(1, BottomBuiltInAction.Sleep),
            service.CreateBuiltInSlot(2, BottomBuiltInAction.Restart),
            new()
            {
                SlotIndex = 3,
                ActionKind = BottomActionKind.Custom,
                DisplayLabel = "Docs",
                TargetPath = @"C:\Docs"
            }
        ]
    };

    service.EnsureSlots(settings);

    Require(settings.BottomActionSlots.Count == BottomActionBarService.SlotCount, "migrated slot count");
    Require(settings.BottomActionSlots[0].BuiltInAction == BottomBuiltInAction.Lock, "slot 1 was not preserved");
    Require(settings.BottomActionSlots[1].BuiltInAction == BottomBuiltInAction.Sleep, "slot 2 was not preserved");
    Require(settings.BottomActionSlots[2].BuiltInAction == BottomBuiltInAction.Restart, "slot 3 was not preserved");
    Require(settings.BottomActionSlots[3].ActionKind == BottomActionKind.Custom, "custom slot was not preserved");
    Require(settings.BottomActionSlots[3].TargetPath == @"C:\Docs", "custom target was not preserved");
    Require(settings.BottomActionSlots[4].BuiltInAction == BottomBuiltInAction.TaskManager, "slot 5 did not append Task Manager");
    Require(settings.ShowWorkspaces, "top workspace button should remain visible when bottom slots do not include Workspaces");
}

static void BottomActionBuiltInsIncludeWorkflowDockOptions()
{
    var service = new BottomActionBarService();
    var builtIns = service.GetBuiltIns().Select(action => action.Id).ToHashSet();

    var expected = new[]
    {
        BottomBuiltInAction.Workspaces,
        BottomBuiltInAction.CaptureWorkspace,
        BottomBuiltInAction.WorkspaceSwitcher,
        BottomBuiltInAction.TaskManager,
        BottomBuiltInAction.FileExplorer,
        BottomBuiltInAction.ScreenSnip,
        BottomBuiltInAction.ControlPanel,
        BottomBuiltInAction.DisplaySettings,
        BottomBuiltInAction.SoundSettings,
        BottomBuiltInAction.BluetoothSettings,
        BottomBuiltInAction.ClipboardSettings,
        BottomBuiltInAction.NotificationsSettings,
        BottomBuiltInAction.DefaultAppsSettings,
        BottomBuiltInAction.StorageSettings,
        BottomBuiltInAction.WindowsUpdate,
        BottomBuiltInAction.DocumentsFolder,
        BottomBuiltInAction.DownloadsFolder,
        BottomBuiltInAction.DesktopFolder,
        BottomBuiltInAction.UserProfileFolder
    };

    foreach (var action in expected)
        Require(builtIns.Contains(action), $"{action} was not available as a built-in action");
}

static void WorkspaceHeaderButtonStaysVisibleWithBottomWorkspaceSlot()
{
    var service = new BottomActionBarService();
    var settings = new Settings { ShowWorkspaces = true };

    service.EnsureSlots(settings);
    Require(settings.ShowWorkspaces, "top workspace button should be visible with default slots");

    settings.BottomActionSlots[4] = service.CreateBuiltInSlot(4, BottomBuiltInAction.Workspaces);
    service.EnsureSlots(settings);
    Require(settings.ShowWorkspaces, "top workspace button should stay visible when Workspace Center is in the bottom bar");

    settings.BottomActionSlots[4] = service.CreateBuiltInSlot(4, BottomBuiltInAction.TaskManager);
    service.EnsureSlots(settings);
    Require(settings.ShowWorkspaces, "top workspace button should remain visible when Workspace Center leaves the bottom bar");
}

static void BottomActionLabelsAreAutomaticForBuiltInsAndCustomForUserActions()
{
    var service = new BottomActionBarService();
    var builtIn = new BottomActionSlot
    {
        SlotIndex = 0,
        ActionKind = BottomActionKind.BuiltIn,
        BuiltInAction = BottomBuiltInAction.TaskManager,
        DisplayLabel = "My Renamed Thing"
    };
    var custom = new BottomActionSlot
    {
        SlotIndex = 1,
        ActionKind = BottomActionKind.Custom,
        DisplayLabel = "My Tool",
        TargetPath = @"C:\Tools\tool.exe"
    };
    var settings = new Settings { BottomActionSlots = [builtIn, custom] };

    service.EnsureSlots(settings);

    Require(service.GetDisplayLabel(settings.BottomActionSlots[0]) == "Task Manager", "built-in label was not canonical");
    Require(settings.BottomActionSlots[0].DisplayLabel == "Task Manager", "built-in persisted label was not repaired");
    Require(service.GetDisplayLabel(settings.BottomActionSlots[1]) == "My Tool", "custom label was not preserved");
}

static void WorkspaceRollbackSnapshotsAreCappedAndLatestWins()
{
    var directory = TempDir("winairbar-snap-auto");
    try
    {
        var service = new WorkspaceSnapshotService(directory);
        var manager = new WindowManager();
        var settings = new Settings();

        for (var i = 0; i < WorkspaceSnapshotService.AutomaticSnapshotLimit + 2; i++)
        {
            service.CaptureAutomaticRollback([Window(100 + i, $"Window {i}", $"app{i}", "", "", "mon", i, i)], settings, manager);
            Thread.Sleep(4);
        }

        var automaticFiles = Directory.GetFiles(Path.Combine(directory, "Automatic"), "*.json");
        Require(automaticFiles.Length == WorkspaceSnapshotService.AutomaticSnapshotLimit, "automatic rollback cap was not enforced");

        var latest = service.LoadLatestAutomaticRollback();
        Require(latest != null, "latest rollback was not loadable");
        Require(latest!.Items.Any(item => item.ProcessName == $"app{WorkspaceSnapshotService.AutomaticSnapshotLimit + 1}"), "undo rollback did not point at latest snapshot");
    }
    finally
    {
        TryDelete(directory);
    }
}

static void ManualWorkspaceSnapshotsAreReadableJson()
{
    var directory = TempDir("winairbar-snap-manual");
    try
    {
        var service = new WorkspaceSnapshotService(directory);
        var manager = new WindowManager();
        var snapshot = service.SaveManualSnapshot("Daily Point", [Window(1, "Notes", "notepad", "", "", "mon", 10, 20)], new Settings(), manager);

        var path = Path.Combine(directory, "Manual", $"{snapshot.Name}.json");
        Require(File.Exists(path), "manual snapshot file missing");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Require(document.RootElement.GetProperty("Name").GetString() == "Daily Point", "manual snapshot JSON name not readable");
        Require(document.RootElement.GetProperty("Items").GetArrayLength() == 1, "manual snapshot JSON items not readable");
        Require(service.GetManualSnapshotNames().Contains("Daily Point"), "manual snapshot was not listed");
    }
    finally
    {
        TryDelete(directory);
    }
}

static void BackupExportIncludesOnlySelectedSections()
{
    var directory = TempDir("winairbar-backup-export");
    try
    {
        Directory.CreateDirectory(Path.Combine(directory, "Workspaces"));
        StorageHelpers.WriteJsonAtomic(Path.Combine(directory, "settings.json"), new Settings());
        StorageHelpers.WriteJsonAtomic(Path.Combine(directory, "Workspaces", "Daily.json"), new Workspace { Name = "Daily" });

        var service = new BackupService(directory);
        var zipPath = Path.Combine(directory, "backup.zip");
        var result = service.Export(zipPath, BackupSection.Workspaces);

        Require(result.Manifest.SelectedSections.SequenceEqual(["Workspaces"]), "manifest selected sections");
        using var archive = ZipFile.OpenRead(zipPath);
        var entries = archive.Entries.Select(entry => entry.FullName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Require(entries.Contains("manifest.json"), "manifest missing");
        Require(entries.Contains("workspaces/Daily.json"), "workspace missing");
        Require(!entries.Contains("settings/settings.json"), "unselected settings were exported");

        var manifestEntry = archive.GetEntry("manifest.json")!;
        using var manifestStream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<BackupManifest>(manifestStream);
        Require(manifest?.Files.Contains("workspaces/Daily.json") == true, "manifest did not list workspace file");

        var preview = service.PreviewImport(zipPath);
        Require(preview.IncludedSections == BackupSection.Workspaces, "preview did not detect workspace section");
    }
    finally
    {
        TryDelete(directory);
    }
}

static void BackupImportPreviewAndSelectedImport()
{
    var source = TempDir("winairbar-backup-source");
    var target = TempDir("winairbar-backup-target");
    try
    {
        Directory.CreateDirectory(Path.Combine(source, "Workspaces"));
        StorageHelpers.WriteJsonAtomic(Path.Combine(source, "settings.json"), new Settings { MenuWidth = 333 });
        StorageHelpers.WriteJsonAtomic(Path.Combine(source, "Workspaces", "Imported.json"), new Workspace { Name = "Imported" });

        Directory.CreateDirectory(Path.Combine(target, "Workspaces"));
        StorageHelpers.WriteJsonAtomic(Path.Combine(target, "settings.json"), new Settings { MenuWidth = 444 });
        StorageHelpers.WriteJsonAtomic(Path.Combine(target, "Workspaces", "Old.json"), new Workspace { Name = "Old" });

        var sourceService = new BackupService(source);
        var zipPath = Path.Combine(source, "backup.zip");
        sourceService.Export(zipPath, BackupSection.Settings | BackupSection.Workspaces);

        var targetService = new BackupService(target);
        var preview = targetService.PreviewImport(zipPath);
        Require(preview.IncludedSections.HasFlag(BackupSection.Settings), "preview missed settings");
        Require(preview.IncludedSections.HasFlag(BackupSection.Workspaces), "preview missed workspaces");

        var result = targetService.Import(zipPath, BackupSection.Workspaces);
        Require(File.Exists(result.PreImportBackupPath), "pre-import backup was not created");
        Require(File.Exists(Path.Combine(target, "Workspaces", "Imported.json")), "selected workspace was not imported");

        var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path.Combine(target, "settings.json")));
        Require(settings?.MenuWidth == 444, "unselected settings were overwritten");
    }
    finally
    {
        TryDelete(source);
        TryDelete(target);
    }
}

static void WindowControlPlannerHandlesMonitorActionsSafely()
{
    var primary = Monitor("primary", true, 0, 0, 1920, 1080);
    var secondary = Monitor("secondary", false, 1920, 0, 1280, 1024);
    var current = Rect(100, 100, 800, 600);

    Require(!WindowControlPlanner.CanMoveToMonitor([primary]), "single monitor should disable move");
    Require(WindowControlPlanner.CanMoveToMonitor([primary, secondary]), "multi-monitor should enable move");

    var centered = WindowControlPlanner.Center(current, primary);
    Near(centered.Left, 560, "centered left");
    Near(centered.Top, 240, "centered top");

    var left = WindowControlPlanner.SnapLeft(primary);
    Near(left.Left, 0, "snap-left left");
    Near(left.Width, 960, "snap-left width");

    var right = WindowControlPlanner.SnapRight(primary);
    Near(right.Left, 960, "snap-right left");
    Near(right.Width, 960, "snap-right width");

    var moved = WindowControlPlanner.MoveToMonitor(current, secondary);
    Near(moved.Left, 2160, "move monitor left");
    Near(moved.Top, 212, "move monitor top");
}

static void WorkspaceDiffClassifiesPlanItems()
{
    var workspace = new Workspace
    {
        Name = "Diff",
        Items =
        [
            WorkspaceItem("Notes", "notepad", "Notes", "notepad.exe"),
            WorkspaceItem("Browser", "browser", "Browser", "ms-settings:"),
            WorkspaceItem("Missing", "missing", "Missing", @"Z:\missing.exe")
        ]
    };
    var current = new[]
    {
        Window(1, "Notes", "notepad", "notepad.exe", "", "mon", 10, 10),
        Window(2, "Extra", "calc", "calc.exe", "", "other", 9000, 9000)
    };

    var plan = new WorkspaceAnalysisService().BuildPlan(workspace, current, new WindowManager(), WorkspaceRestoreMode.Full);

    Require(plan.Items.Any(i => i.Kind is WorkspacePlanItemKind.Matched or WorkspacePlanItemKind.ChangedPosition or WorkspacePlanItemKind.MonitorRemap), "matched item missing");
    Require(plan.Items.Any(i => i.Kind == WorkspacePlanItemKind.WillLaunch), "launch item missing");
    Require(plan.Items.Any(i => i.Kind == WorkspacePlanItemKind.MissingTarget), "missing target item missing");
    Require(plan.ExtraWindows.Any(e => e.Window.ProcessName == "calc"), "extra current window missing");
}

static void WorkspaceRestoreModesBuildPlans()
{
    var workspace = new Workspace
    {
        Name = "Modes",
        Metadata = new WorkspaceMetadata { DefaultCleanupAction = WorkspaceCleanupAction.Minimize }
    };
    var current = new[] { Window(1, "Extra", "calc", "calc.exe", "", "mon", 0, 0) };
    var analysis = new WorkspaceAnalysisService();

    foreach (var mode in Enum.GetValues<WorkspaceRestoreMode>())
    {
        var plan = analysis.BuildPlan(workspace, current, new WindowManager(), mode);
        Require(plan.Mode == mode, $"{mode} plan mode was not preserved");
    }

    var clean = analysis.BuildPlan(workspace, current, new WindowManager(), WorkspaceRestoreMode.Clean);
    Require(clean.ExtraWindows.Count == 1, "clean plan did not include extras");
    Require(clean.ExtraWindows[0].Action == WorkspaceCleanupAction.Minimize, "normal extra did not default to minimize");
}

static void ExtraCleanupDefaultsProtectRiskyWindows()
{
    var protectedWindow = Window(1, "Windows Installer", "setup", "setup.exe", "", "mon", 0, 0);
    var riskyWindow = Window(2, "Untitled Document *", "word", "word.exe", "", "mon", 0, 0);
    var normalWindow = Window(3, "Chat", "chat", "chat.exe", "", "mon", 0, 0);
    var workspace = new Workspace { Name = "Clean" };
    var plan = new WorkspaceAnalysisService().BuildPlan(workspace, [protectedWindow, riskyWindow, normalWindow], new WindowManager(), WorkspaceRestoreMode.Clean);

    var protectedExtra = plan.ExtraWindows.First(e => e.Window.Handle == protectedWindow.Handle);
    var riskyExtra = plan.ExtraWindows.First(e => e.Window.Handle == riskyWindow.Handle);
    var normalExtra = plan.ExtraWindows.First(e => e.Window.Handle == normalWindow.Handle);

    Require(protectedExtra.IsProtected && protectedExtra.Action == WorkspaceCleanupAction.Keep, "protected setup window was not kept");
    Require(riskyExtra.IsRisky && riskyExtra.Action == WorkspaceCleanupAction.Keep, "risky unsaved-looking window was not kept");
    Require(normalExtra.Action == WorkspaceCleanupAction.Minimize, "normal extra did not minimize");
}

static void WorkspaceVersionsCapAtTen()
{
    var directory = TempDir("winairbar-versions");
    try
    {
        Directory.CreateDirectory(Path.Combine(directory, "Workspaces"));
        var service = new WorkspaceVersionService(directory);
        for (var i = 0; i < WorkspaceVersionService.VersionLimit + 3; i++)
        {
            StorageHelpers.WriteJsonAtomic(Path.Combine(directory, "Workspaces", "Daily.json"), new Workspace { Name = "Daily", Metadata = new WorkspaceMetadata { Notes = $"v{i}" } });
            service.SaveVersionBeforeOverwrite("Daily");
            Thread.Sleep(3);
        }

        Require(service.GetVersions("Daily").Count == WorkspaceVersionService.VersionLimit, "version cap was not enforced");
    }
    finally
    {
        TryDelete(directory);
    }
}

static void WorkspaceTimelineRecordsDetailedItems()
{
    var directory = TempDir("winairbar-timeline");
    try
    {
        var service = new WorkspaceTimelineService(directory);
        service.Record("Daily", "restore", "Restored Daily", [new WorkspaceTimelineItem { DisplayName = "Notes", Status = "Restored", Message = "ok" }]);

        var events = service.GetEvents("Daily");
        Require(events.Count == 1, "timeline event missing");
        Require(events[0].Items.Count == 1, "timeline item missing");
        Require(events[0].Items[0].DisplayName == "Notes", "timeline item detail was not retained");
    }
    finally
    {
        TryDelete(directory);
    }
}

static void WorkspaceSuggestionsThresholdDismissNeverSuggest()
{
    var directory = TempDir("winairbar-suggestions");
    try
    {
        var service = new WorkspaceSuggestionService(directory);
        var windows = new[]
        {
            Window(1, "Code", "Code", "Code.exe", "", "mon", 0, 0),
            Window(2, "Browser", "Browser", "Browser.exe", "", "mon", 0, 0)
        };

        service.RecordObservation(windows);
        service.RecordObservation(windows);
        Require(service.GetActiveSuggestions().Count == 0, "suggested before threshold");
        service.RecordObservation(windows);
        var active = service.GetActiveSuggestions();
        Require(active.Count == 1, "suggestion threshold did not activate");
        service.Dismiss(active[0].Id, neverSuggest: false);
        Require(service.GetActiveSuggestions().Count == 0, "dismissed suggestion still active");

        var service2 = new WorkspaceSuggestionService(directory);
        service2.RecordObservation(windows);
        service2.RecordObservation(windows);
        service2.RecordObservation(windows);
        var second = service2.GetActiveSuggestions().FirstOrDefault();
        if (second != null)
            service2.Dismiss(second.Id, neverSuggest: true);
        Require(service2.GetActiveSuggestions().Count == 0, "never-suggest suggestion still active");
    }
    finally
    {
        TryDelete(directory);
    }
}

static void WorkspaceRulesApplyToCaptureCleanupAndPosition()
{
    var directory = TempDir("winairbar-rules");
    try
    {
        var ruleService = new WorkspaceRuleService(directory);
        ruleService.SaveRule(new WorkspaceRule
        {
            ProcessName = "skipme",
            ExcludeFromCapture = true,
            NeverCleanup = true,
            DefaultCleanupAction = WorkspaceCleanupAction.Close,
            DefaultRestoreMode = WorkspaceRestoreMode.LayoutOnly,
            DefaultPosition = WorkspaceDefaultPosition.SnapLeft
        });

        var workspaceService = new WorkspaceService(directory);
        var captured = workspaceService.CaptureWorkspace(
            "Rules",
            [Window(1, "Skip", "skipme", "skipme.exe", "", "mon", 0, 0), Window(2, "Keep", "keepme", "keepme.exe", "", "mon", 0, 0)],
            new Settings(),
            new WindowManager(),
            ruleService.GetRules());

        Require(captured.Items.All(item => item.ProcessName != "skipme"), "exclude-from-capture rule failed");
        Require(ruleService.CleanupActionFor(Window(3, "Skip", "skipme", "skipme.exe", "", "mon", 0, 0), WorkspaceCleanupAction.Minimize) == WorkspaceCleanupAction.Keep, "never cleanup rule failed");
        Require(ruleService.PositionFor(Window(4, "Skip", "skipme", "skipme.exe", "", "mon", 0, 0)) == WorkspaceDefaultPosition.SnapLeft, "position rule failed");
        Require(ruleService.RestoreModeFor(Window(5, "Skip", "skipme", "skipme.exe", "", "mon", 0, 0), WorkspaceRestoreMode.Full) == WorkspaceRestoreMode.LayoutOnly, "restore-mode rule failed");
    }
    finally
    {
        TryDelete(directory);
    }
}

static void LauncherTagsPersistAndFilter()
{
    var directory = TempDir("winairbar-tags");
    try
    {
        var service = new AppLauncherService(directory);
        service.AddOrUpdateApp("Code", @"C:\Apps\code.exe", null);
        service.AddOrUpdateApp("Paint", @"C:\Apps\paint.exe", null);
        service.SetTags(@"C:\Apps\code.exe", ["dev", "daily", "dev"]);
        service.SetTags(@"C:\Apps\paint.exe", ["art"]);

        var tags = service.GetAllTags();
        Require(tags.SequenceEqual(["art", "daily", "dev"]), "launcher tags were not persisted distinctly");
        var devApps = service.GetAppsByTag("dev");
        Require(devApps.Count == 1 && devApps[0].Name == "Code", "launcher tag filter failed");
    }
    finally
    {
        TryDelete(directory);
    }
}

static void BackupConflictsPreviewAndChoices()
{
    var source = TempDir("winairbar-conflict-source");
    var target = TempDir("winairbar-conflict-target");
    try
    {
        Directory.CreateDirectory(Path.Combine(source, "Workspaces"));
        StorageHelpers.WriteJsonAtomic(Path.Combine(source, "Workspaces", "Daily.json"), new Workspace { Name = "Imported Daily" });
        var sourceService = new BackupService(source);
        var zipPath = Path.Combine(source, "backup.zip");
        sourceService.Export(zipPath, BackupSection.Workspaces);

        Directory.CreateDirectory(Path.Combine(target, "Workspaces"));
        StorageHelpers.WriteJsonAtomic(Path.Combine(target, "Workspaces", "Daily.json"), new Workspace { Name = "Local Daily" });
        var targetService = new BackupService(target);
        var conflicts = targetService.PreviewConflicts(zipPath, BackupSection.Workspaces);

        Require(conflicts.Count == 1, "workspace conflict not detected");
        Require(conflicts[0].Choice == BackupConflictChoice.ImportAsCopy, "workspace conflict did not default to import-as-copy");

        targetService.Import(zipPath, BackupSection.Workspaces, conflicts);
        Require(File.Exists(Path.Combine(target, "Workspaces", "Daily (Imported).json")), "import-as-copy did not create copy");

        conflicts = targetService.PreviewConflicts(zipPath, BackupSection.Workspaces);
        conflicts[0].Choice = BackupConflictChoice.KeepLocal;
        targetService.Import(zipPath, BackupSection.Workspaces, conflicts);
        var local = JsonSerializer.Deserialize<Workspace>(File.ReadAllText(Path.Combine(target, "Workspaces", "Daily.json")));
        Require(local?.Name == "Local Daily", "keep-local conflict overwrote local workspace");
    }
    finally
    {
        TryDelete(source);
        TryDelete(target);
    }
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

static WorkspaceItem WorkspaceItem(string displayName, string process, string title, string path)
    => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        DisplayName = displayName,
        ProcessName = process,
        WindowTitle = title,
        ExecutablePath = path,
        RawBounds = Rect(0, 0, 600, 400),
        Left = 0,
        Top = 0,
        Width = 600,
        Height = 400,
        MonitorId = "mon",
        Fingerprint = WorkspaceRestoreRules.BuildFingerprint(new WindowInfo
        {
            ProcessName = process,
            Title = title,
            ExecutablePath = path
        })
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

static string TempDir(string prefix)
{
    var path = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
    Directory.CreateDirectory(path);
    return path;
}

static void TryDelete(string path)
{
    try { Directory.Delete(path, recursive: true); } catch { }
}
