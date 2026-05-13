using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using FloatingTaskbarMenu.Models;

namespace FloatingTaskbarMenu.Core;

public class WorkspaceService
{
    private readonly string _workspaceDirectory;

    public WorkspaceService(string? appDataDirectory = null)
    {
        _workspaceDirectory = Path.Combine(appDataDirectory ?? AppIdentity.AppDataDirectory, "Workspaces");
        Directory.CreateDirectory(_workspaceDirectory);
    }

    public List<string> GetWorkspaceNames()
    {
        try
        {
            return Directory.GetFiles(_workspaceDirectory, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public Workspace LoadWorkspace(string name)
    {
        var safeName = NormalizeWorkspaceName(name);
        try
        {
            var path = GetWorkspacePath(safeName);
            if (!File.Exists(path))
                return WorkspaceRestoreRules.NormalizeWorkspace(new Workspace { Name = safeName }, safeName);

            var json = File.ReadAllText(path);
            var workspace = JsonSerializer.Deserialize<Workspace>(json) ?? new Workspace { Name = safeName };
            return WorkspaceRestoreRules.NormalizeWorkspace(workspace, safeName);
        }
        catch
        {
            return WorkspaceRestoreRules.NormalizeWorkspace(new Workspace { Name = safeName }, safeName);
        }
    }

    public Workspace CaptureWorkspace(string name, IEnumerable<WindowInfo> windows, Settings settings, WindowManager windowManager, IEnumerable<WorkspaceRule>? rules = null)
    {
        var safeName = NormalizeWorkspaceName(name);
        var now = DateTime.Now;
        var existing = LoadWorkspace(safeName);
        var monitors = windowManager.GetMonitors();
        var ruleList = rules?.ToList() ?? new List<WorkspaceRule>();

        var workspace = new Workspace
        {
            SchemaVersion = WorkspaceRestoreRules.CurrentSchemaVersion,
            Name = safeName,
            CreatedAt = existing.CreatedAt == default ? now : existing.CreatedAt,
            UpdatedAt = now,
            Metadata = existing.Metadata ?? new WorkspaceMetadata(),
            CapturedProfile = settings.CurrentPinnedProfile,
            CapturedTheme = settings.CurrentTheme,
            ShowWindowList = settings.ShowWindowList,
            ShowSystemTray = settings.ShowSystemTray,
            ShowAuxiliaryControls = settings.ShowAuxiliaryControls,
            Monitors = monitors,
            Items = windows
                .Where(w => !string.IsNullOrWhiteSpace(w.ProcessName) && !IsExcludedByRule(w, ruleList))
                .Select(window => WorkspaceRestoreRules.ToWorkspaceItem(window, monitors))
                .ToList()
        };

        SaveWorkspace(workspace);
        return workspace;
    }

    public WorkspacePreview BuildPreview(Workspace workspace, IReadOnlyList<WorkspaceMonitor> currentMonitors)
    {
        workspace = WorkspaceRestoreRules.NormalizeWorkspace(workspace, workspace.Name);
        var capturedMonitorIds = workspace.Monitors.Count > 0
            ? workspace.Monitors.Select(m => m.Id)
            : workspace.Items.Select(i => i.MonitorId);

        var currentIds = currentMonitors.Select(m => m.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var capturedIds = capturedMonitorIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new WorkspacePreview
        {
            AppCount = workspace.Items.Count,
            CapturedMonitorCount = Math.Max(1, capturedIds.Count),
            CurrentMonitorCount = Math.Max(1, currentMonitors.Count),
            MissingPathCount = workspace.Items.Count(WorkspaceRestoreRules.HasMissingExecutablePath),
            MissingDocumentCount = workspace.Items.Count(WorkspaceDocumentResolver.IsMissingDocumentTarget),
            RemappedMonitorCount = capturedIds.Count(id => !currentIds.Contains(id)),
            IsBlueprint = workspace.SchemaVersion >= WorkspaceRestoreRules.CurrentSchemaVersion
        };
    }

    public void SaveWorkspace(Workspace workspace)
    {
        workspace = WorkspaceRestoreRules.NormalizeWorkspace(workspace, workspace.Name);
        workspace.SchemaVersion = WorkspaceRestoreRules.CurrentSchemaVersion;
        workspace.UpdatedAt = DateTime.Now;
        StorageHelpers.WriteJsonAtomic(GetWorkspacePath(workspace.Name), workspace);
    }

    public void DeleteWorkspace(string name)
    {
        try
        {
            var path = GetWorkspacePath(NormalizeWorkspaceName(name));
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }

    public WorkspaceRestoreResult RestoreWorkspace(Workspace workspace, WindowManager windowManager)
    {
        workspace = WorkspaceRestoreRules.NormalizeWorkspace(workspace, workspace.Name);
        var result = new WorkspaceRestoreResult { WorkspaceName = workspace.Name };
        var placedItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedHandles = new ConcurrentDictionary<nint, byte>();
        var sync = new object();

        using var watcher = windowManager.BeginWindowRestoreWatcher(window =>
        {
            WorkspaceMatch? match;
            lock (sync)
            {
                match = WorkspaceRestoreRules.FindBestMatch(
                    workspace.Items.Where(item => !placedItemIds.Contains(item.Id)),
                    new[] { window },
                    handle => usedHandles.ContainsKey(handle));
                if (match == null || !WorkspaceRestoreRules.IsAutoPlaceable(match.Confidence))
                    return;

                placedItemIds.Add(match.Item.Id);
                usedHandles.TryAdd(window.Handle, 0);
            }

            windowManager.RestoreWindowLayout(window, match.Item, workspace.Monitors);
        });

        foreach (var item in workspace.Items)
        {
            var itemResult = new WorkspaceRestoreItemResult
            {
                DisplayName = item.DisplayName,
                TargetMonitorId = item.MonitorId
            };

            try
            {
                var currentWindows = windowManager.GetWindows();
                var match = WorkspaceRestoreRules.FindBestMatch(new[] { item }, currentWindows, handle => usedHandles.ContainsKey(handle));
                WorkspaceMatch? ignoredLowConfidenceMatch = null;
                var launched = false;

                if (match != null && WorkspaceRestoreRules.IsAutoPlaceable(match.Confidence))
                {
                    windowManager.RestoreWindowLayout(match.Window, item, workspace.Monitors);
                    lock (sync)
                    {
                        placedItemIds.Add(item.Id);
                        usedHandles.TryAdd(match.Window.Handle, 0);
                    }

                    itemResult.Status = WorkspaceRestoreStatus.Restored;
                    itemResult.MatchScore = match.Score;
                    itemResult.MatchConfidence = match.Confidence;
                    itemResult.Message = MatchMessage(match, "Restored existing window");
                    result.Items.Add(itemResult);
                    continue;
                }

                if (match != null && match.Confidence == WorkspaceMatchConfidence.Low)
                {
                    ignoredLowConfidenceMatch = match;
                    itemResult.IgnoredLowConfidenceMatch = true;
                    itemResult.MatchScore = match.Score;
                    itemResult.MatchConfidence = match.Confidence;
                    match = null;
                }

                var targetBounds = windowManager.ResolveTargetBounds(item, workspace.Monitors, out var targetMonitor, out var remapped);
                itemResult.TargetMonitorId = targetMonitor?.Id ?? item.MonitorId;
                if (!windowManager.TryLaunchWorkspaceItem(item, targetBounds, out var launchMessage))
                {
                    itemResult.Status = WorkspaceRestoreStatus.Skipped;
                    itemResult.Message = LowConfidencePrefix(ignoredLowConfidenceMatch) + launchMessage;
                    result.Items.Add(itemResult);
                    continue;
                }

                launched = true;
                match = WaitForWindow(windowManager, item, usedHandles, WorkspaceRestoreRules.LaunchWaitTimeout, () =>
                {
                    lock (sync)
                        return placedItemIds.Contains(item.Id);
                });
                if (match == null)
                {
                    bool placedByWatcher;
                    lock (sync)
                        placedByWatcher = placedItemIds.Contains(item.Id);

                    itemResult.Status = WorkspaceRestoreStatus.Launched;
                    itemResult.Message = LowConfidencePrefix(ignoredLowConfidenceMatch) + (placedByWatcher
                        ? $"{launchMessage}; placed as soon as the window appeared."
                        : $"{launchMessage}; matching window was not found yet.");
                    result.Items.Add(itemResult);
                    continue;
                }

                windowManager.RestoreWindowLayout(match.Window, item, workspace.Monitors);
                lock (sync)
                {
                    placedItemIds.Add(item.Id);
                    usedHandles.TryAdd(match.Window.Handle, 0);
                }

                itemResult.Status = WorkspaceRestoreStatus.Launched;
                itemResult.MatchScore = match.Score;
                itemResult.MatchConfidence = match.Confidence;
                itemResult.Message = LowConfidencePrefix(ignoredLowConfidenceMatch) + MatchMessage(match, launched ? launchMessage : "Restored", remapped);
            }
            catch (Exception ex)
            {
                itemResult.Status = WorkspaceRestoreStatus.Failed;
                itemResult.Message = ex.Message;
            }

            result.Items.Add(itemResult);
        }

        Thread.Sleep(WorkspaceRestoreRules.SettleDelay);
        RunSettlePass(workspace, windowManager, placedItemIds);
        return result;
    }

    private static WorkspaceMatch? WaitForWindow(
        WindowManager windowManager,
        WorkspaceItem item,
        ConcurrentDictionary<nint, byte> usedHandles,
        TimeSpan timeout,
        Func<bool> alreadyPlaced)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            Thread.Sleep(WorkspaceRestoreRules.LaunchPollInterval);
            if (alreadyPlaced())
                return null;

            var match = WorkspaceRestoreRules.FindBestMatch(new[] { item }, windowManager.GetWindows(), handle => usedHandles.ContainsKey(handle));
            if (match != null && WorkspaceRestoreRules.IsAutoPlaceable(match.Confidence))
                return match;
        }

        return null;
    }

    private static void RunSettlePass(Workspace workspace, WindowManager windowManager, HashSet<string> placedItemIds)
    {
        if (placedItemIds.Count == 0)
            return;

        var windows = windowManager.GetWindows();
        var usedHandles = new ConcurrentDictionary<nint, byte>();
        foreach (var item in workspace.Items.Where(item => placedItemIds.Contains(item.Id)))
        {
            var match = WorkspaceRestoreRules.FindBestMatch(new[] { item }, windows, handle => usedHandles.ContainsKey(handle));
            if (match == null || !WorkspaceRestoreRules.IsAutoPlaceable(match.Confidence))
                continue;

            windowManager.RestoreWindowLayout(match.Window, item, workspace.Monitors);
            usedHandles.TryAdd(match.Window.Handle, 0);
        }
    }

    private static string MatchMessage(WorkspaceMatch match, string prefix, bool remapped = false)
    {
        var confidence = match.Confidence.ToString().ToLowerInvariant();
        return $"{prefix}; {confidence} confidence ({match.Score})" + (remapped ? "; remapped monitor" : "");
    }

    private static string LowConfidencePrefix(WorkspaceMatch? match)
        => match == null ? "" : $"Ignored low-confidence existing match ({match.Score}); ";

    private string GetWorkspacePath(string name)
        => Path.Combine(_workspaceDirectory, $"{NormalizeWorkspaceName(name)}.json");

    public static string NormalizeWorkspaceName(string name)
        => WorkspaceRestoreRules.NormalizeWorkspaceName(name);

    private static bool IsExcludedByRule(WindowInfo window, IReadOnlyList<WorkspaceRule> rules)
        => rules.Any(rule => rule.ExcludeFromCapture &&
            ((!string.IsNullOrWhiteSpace(rule.ProcessName) && string.Equals(rule.ProcessName, window.ProcessName, StringComparison.OrdinalIgnoreCase)) ||
             (!string.IsNullOrWhiteSpace(rule.ExecutablePath) && string.Equals(rule.ExecutablePath, window.ExecutablePath, StringComparison.OrdinalIgnoreCase))));
}
