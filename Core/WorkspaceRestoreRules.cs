using System.IO;
using FloatingTaskbarMenu.Models;

namespace FloatingTaskbarMenu.Core;

internal static class WorkspaceRestoreRules
{
    public const int CurrentSchemaVersion = 2;
    public const int MediumConfidenceScore = 60;
    public const int HighConfidenceScore = 120;
    public static readonly TimeSpan LaunchWaitTimeout = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan LaunchPollInterval = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(650);

    public static Workspace NormalizeWorkspace(Workspace workspace, string fallbackName)
    {
        workspace.Name = NormalizeWorkspaceName(string.IsNullOrWhiteSpace(workspace.Name) ? fallbackName : workspace.Name);
        workspace.SchemaVersion = Math.Max(workspace.SchemaVersion, CurrentSchemaVersion);
        workspace.Metadata ??= new WorkspaceMetadata();
        workspace.Metadata.AutoActions ??= new List<WorkspaceAutoAction>();
        workspace.Metadata.ScreenGallery ??= new List<WorkspaceScreenMemory>();
        workspace.Monitors ??= new List<WorkspaceMonitor>();
        workspace.Items ??= new List<WorkspaceItem>();

        foreach (var item in workspace.Items)
            NormalizeItem(item);

        return workspace;
    }

    public static WorkspaceItem ToWorkspaceItem(WindowInfo window, IReadOnlyList<WorkspaceMonitor> monitors)
    {
        var rawBounds = new WorkspaceRect
        {
            Left = window.Left,
            Top = window.Top,
            Width = window.Width,
            Height = window.Height
        };
        var monitor = monitors.FirstOrDefault(m => string.Equals(m.Id, window.MonitorId, StringComparison.OrdinalIgnoreCase));

        return new WorkspaceItem
        {
            Id = Guid.NewGuid().ToString("N"),
            DisplayName = !string.IsNullOrWhiteSpace(window.Title) ? window.Title : window.ProcessName,
            ExecutablePath = window.ExecutablePath,
            ProcessName = window.ProcessName,
            WindowTitle = window.Title,
            ClassName = window.ClassName,
            DocumentPath = WorkspaceDocumentResolver.ResolveDocumentPath(window),
            MonitorId = window.MonitorId,
            Left = window.Left,
            Top = window.Top,
            Width = window.Width,
            Height = window.Height,
            PlacementState = window.PlacementState,
            Fingerprint = BuildFingerprint(window),
            RawBounds = rawBounds,
            NormalizedBounds = WorkspacePlacementPlanner.NormalizeBoundsToMonitor(rawBounds, monitor),
            Placement = window.Placement,
            RestorePolicy = "Auto"
        };
    }

    public static WorkspaceMatch? FindBestMatch(
        IEnumerable<WorkspaceItem> items,
        IEnumerable<WindowInfo> windows,
        Func<nint, bool>? isHandleUsed = null)
    {
        WorkspaceMatch? best = null;
        foreach (var item in items)
        {
            foreach (var window in windows)
            {
                if (isHandleUsed?.Invoke(window.Handle) == true)
                    continue;

                var score = MatchScore(window, item);
                if (score <= 0 || (best != null && score <= best.Score))
                    continue;

                best = new WorkspaceMatch(item, window, score, ToConfidence(score));
            }
        }

        return best;
    }

    public static int MatchScore(WindowInfo window, WorkspaceItem item)
    {
        var score = 0;
        var fingerprint = item.Fingerprint ?? new WorkspaceAppFingerprint();

        var itemPath = FirstNonEmpty(fingerprint.ExecutablePath, item.ExecutablePath);
        if (!string.IsNullOrWhiteSpace(itemPath)
            && string.Equals(window.ExecutablePath, itemPath, StringComparison.OrdinalIgnoreCase))
            score += 80;

        var itemProcess = FirstNonEmpty(fingerprint.ProcessName, item.ProcessName);
        if (!string.IsNullOrWhiteSpace(itemProcess)
            && string.Equals(window.ProcessName, itemProcess, StringComparison.OrdinalIgnoreCase))
            score += 40;

        var itemClass = FirstNonEmpty(fingerprint.ClassName, item.ClassName);
        if (!string.IsNullOrWhiteSpace(itemClass)
            && string.Equals(window.ClassName, itemClass, StringComparison.OrdinalIgnoreCase))
            score += 20;

        var itemTitle = FirstNonEmpty(fingerprint.WindowTitle, item.WindowTitle);
        var normalizedItemTitle = FirstNonEmpty(fingerprint.NormalizedTitle, NormalizeTitle(itemTitle));
        var normalizedWindowTitle = NormalizeTitle(window.Title);
        if (!string.IsNullOrWhiteSpace(normalizedItemTitle) && normalizedItemTitle == normalizedWindowTitle)
            score += 30;
        else
            score += TitleTokenScore(fingerprint.TitleTokens, itemTitle, window.Title);

        if (!string.IsNullOrWhiteSpace(item.MonitorId)
            && string.Equals(item.MonitorId, window.MonitorId, StringComparison.OrdinalIgnoreCase))
            score += 10;

        score += LayoutProximityScore(window, item);
        return score;
    }

    public static WorkspaceMatchConfidence ToConfidence(int score)
    {
        if (score >= HighConfidenceScore)
            return WorkspaceMatchConfidence.High;
        if (score >= MediumConfidenceScore)
            return WorkspaceMatchConfidence.Medium;
        return WorkspaceMatchConfidence.Low;
    }

    public static bool IsAutoPlaceable(WorkspaceMatchConfidence confidence)
        => confidence is WorkspaceMatchConfidence.Medium or WorkspaceMatchConfidence.High;

    public static bool HasMissingExecutablePath(WorkspaceItem item)
    {
        if (string.IsNullOrWhiteSpace(item.ExecutablePath))
            return false;

        var isUri = Uri.TryCreate(item.ExecutablePath, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https" or "ms-settings" or "shell";
        return !isUri && !File.Exists(item.ExecutablePath);
    }

    public static WorkspaceAppFingerprint BuildFingerprint(WindowInfo window)
    {
        return new WorkspaceAppFingerprint
        {
            ExecutablePath = window.ExecutablePath,
            ProcessName = window.ProcessName,
            ClassName = window.ClassName,
            WindowTitle = window.Title,
            NormalizedTitle = NormalizeTitle(window.Title),
            TitleTokens = TokenizeTitle(window.Title).ToList()
        };
    }

    public static string NormalizeTitle(string title)
        => string.Join(' ', TokenizeTitle(title));

    public static IReadOnlyCollection<string> TokenizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Array.Empty<string>();

        var normalized = new string(title
            .Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ')
            .ToArray());

        return normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
    }

    public static string NormalizeWorkspaceName(string name)
        => StorageHelpers.ToSafeFileName(name, "Workspace");

    private static void NormalizeItem(WorkspaceItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Id))
            item.Id = Guid.NewGuid().ToString("N");

        item.Fingerprint ??= new WorkspaceAppFingerprint();
        item.Fingerprint.ExecutablePath = FirstNonEmpty(item.Fingerprint.ExecutablePath, item.ExecutablePath);
        item.Fingerprint.ProcessName = FirstNonEmpty(item.Fingerprint.ProcessName, item.ProcessName);
        item.Fingerprint.ClassName = FirstNonEmpty(item.Fingerprint.ClassName, item.ClassName);
        item.Fingerprint.WindowTitle = FirstNonEmpty(item.Fingerprint.WindowTitle, item.WindowTitle);
        item.Fingerprint.NormalizedTitle = FirstNonEmpty(item.Fingerprint.NormalizedTitle, NormalizeTitle(item.Fingerprint.WindowTitle));
        if (item.Fingerprint.TitleTokens == null || item.Fingerprint.TitleTokens.Count == 0)
            item.Fingerprint.TitleTokens = TokenizeTitle(item.Fingerprint.WindowTitle).ToList();

        item.RawBounds ??= new WorkspaceRect();
        if (item.RawBounds.IsEmpty && item.Width > 0 && item.Height > 0)
        {
            item.RawBounds = new WorkspaceRect
            {
                Left = item.Left,
                Top = item.Top,
                Width = item.Width,
                Height = item.Height
            };
        }

        item.NormalizedBounds ??= new WorkspaceRect();
        item.Placement ??= new WorkspaceWindowPlacement();
        item.Placement.State = item.Placement.State == default ? item.PlacementState : item.Placement.State;
        if (string.IsNullOrWhiteSpace(item.DocumentPath))
            item.DocumentPath = WorkspaceDocumentResolver.ResolveDocumentPath(item);
        if (string.IsNullOrWhiteSpace(item.RestorePolicy))
            item.RestorePolicy = "Auto";
    }

    private static int TitleTokenScore(IReadOnlyCollection<string>? capturedTokens, string capturedTitle, string currentTitle)
    {
        var itemTokens = capturedTokens is { Count: > 0 }
            ? capturedTokens
            : TokenizeTitle(capturedTitle);
        var windowTokens = TokenizeTitle(currentTitle);
        if (itemTokens.Count == 0 || windowTokens.Count == 0)
            return 0;

        var overlap = itemTokens.Intersect(windowTokens, StringComparer.OrdinalIgnoreCase).Count();
        return (int)Math.Round(25.0 * overlap / Math.Max(1, itemTokens.Count));
    }

    private static int LayoutProximityScore(WindowInfo window, WorkspaceItem item)
    {
        var raw = !item.RawBounds.IsEmpty
            ? item.RawBounds
            : new WorkspaceRect { Left = item.Left, Top = item.Top, Width = item.Width, Height = item.Height };
        if (raw.IsEmpty || window.Width <= 0 || window.Height <= 0)
            return 0;

        var savedCenterX = raw.Left + raw.Width / 2;
        var savedCenterY = raw.Top + raw.Height / 2;
        var currentCenterX = window.Left + window.Width / 2;
        var currentCenterY = window.Top + window.Height / 2;
        var distance = Math.Sqrt(Math.Pow(savedCenterX - currentCenterX, 2) + Math.Pow(savedCenterY - currentCenterY, 2));
        if (distance < 60)
            return 15;
        if (distance < 180)
            return 8;
        if (distance < 360)
            return 4;

        return 0;
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
}

internal sealed record WorkspaceMatch(
    WorkspaceItem Item,
    WindowInfo Window,
    int Score,
    WorkspaceMatchConfidence Confidence);
