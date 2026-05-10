using FloatingTaskbarMenu.Models;

namespace FloatingTaskbarMenu.Core;

internal static class WorkspacePlacementPlanner
{
    public static WorkspaceRect ResolveTargetBounds(
        WorkspaceItem item,
        IReadOnlyList<WorkspaceMonitor> capturedMonitors,
        IReadOnlyList<WorkspaceMonitor> currentMonitors,
        out WorkspaceMonitor? targetMonitor,
        out bool remapped)
    {
        remapped = false;
        var raw = !item.RawBounds.IsEmpty
            ? item.RawBounds
            : new WorkspaceRect { Left = item.Left, Top = item.Top, Width = item.Width, Height = item.Height };

        if (currentMonitors.Count == 0)
        {
            targetMonitor = null;
            return raw;
        }

        var capturedMonitor = capturedMonitors.FirstOrDefault(m => string.Equals(m.Id, item.MonitorId, StringComparison.OrdinalIgnoreCase));
        targetMonitor = ResolveCurrentMonitor(item.MonitorId, capturedMonitor, currentMonitors, out remapped);

        if (targetMonitor == null)
            return raw;

        if (!remapped && !raw.IsEmpty)
            return raw;

        if (!item.NormalizedBounds.IsEmpty)
        {
            var normalized = item.NormalizedBounds;
            var mapped = new WorkspaceRect
            {
                Left = targetMonitor.WorkLeft + normalized.Left * targetMonitor.WorkWidth,
                Top = targetMonitor.WorkTop + normalized.Top * targetMonitor.WorkHeight,
                Width = normalized.Width * targetMonitor.WorkWidth,
                Height = normalized.Height * targetMonitor.WorkHeight
            };
            return ClampToWorkArea(mapped, targetMonitor);
        }

        if (!raw.IsEmpty)
            return ClampToWorkArea(raw, targetMonitor);

        return raw;
    }

    public static WorkspaceRect NormalizeBoundsToMonitor(WorkspaceRect bounds, WorkspaceMonitor? monitor)
    {
        if (monitor == null || bounds.IsEmpty || monitor.WorkWidth <= 0 || monitor.WorkHeight <= 0)
            return new WorkspaceRect();

        return new WorkspaceRect
        {
            Left = (bounds.Left - monitor.WorkLeft) / monitor.WorkWidth,
            Top = (bounds.Top - monitor.WorkTop) / monitor.WorkHeight,
            Width = bounds.Width / monitor.WorkWidth,
            Height = bounds.Height / monitor.WorkHeight
        };
    }

    public static WorkspaceMonitor? ResolveCurrentMonitor(
        string monitorId,
        WorkspaceMonitor? capturedMonitor,
        IReadOnlyList<WorkspaceMonitor> currentMonitors,
        out bool remapped)
    {
        remapped = false;
        var exact = currentMonitors.FirstOrDefault(m => string.Equals(m.Id, monitorId, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return exact;

        remapped = true;
        if (capturedMonitor == null)
            return currentMonitors.FirstOrDefault(m => m.IsPrimary) ?? currentMonitors.FirstOrDefault();

        var primary = capturedMonitor.IsPrimary
            ? currentMonitors.FirstOrDefault(m => m.IsPrimary)
            : null;
        if (primary != null)
            return primary;

        return currentMonitors
            .OrderBy(m => MonitorSimilarityDistance(capturedMonitor, m))
            .FirstOrDefault();
    }

    public static WorkspaceRect ClampToWorkArea(WorkspaceRect bounds, WorkspaceMonitor monitor)
    {
        if (bounds.IsEmpty || monitor.WorkWidth <= 0 || monitor.WorkHeight <= 0)
            return bounds;

        var width = Math.Min(bounds.Width, monitor.WorkWidth);
        var height = Math.Min(bounds.Height, monitor.WorkHeight);
        var minLeft = monitor.WorkLeft;
        var minTop = monitor.WorkTop;
        var maxLeft = monitor.WorkLeft + monitor.WorkWidth - width;
        var maxTop = monitor.WorkTop + monitor.WorkHeight - height;

        return new WorkspaceRect
        {
            Left = Math.Max(minLeft, Math.Min(bounds.Left, maxLeft)),
            Top = Math.Max(minTop, Math.Min(bounds.Top, maxTop)),
            Width = width,
            Height = height
        };
    }

    public static double MonitorSimilarityDistance(WorkspaceMonitor captured, WorkspaceMonitor current)
    {
        var capturedRatio = captured.Height <= 0 ? 1 : captured.Width / captured.Height;
        var currentRatio = current.Height <= 0 ? 1 : current.Width / current.Height;
        var ratioDelta = Math.Abs(capturedRatio - currentRatio);
        var widthDelta = Math.Abs(captured.Width - current.Width) / Math.Max(1, captured.Width);
        var heightDelta = Math.Abs(captured.Height - current.Height) / Math.Max(1, captured.Height);
        var primaryPenalty = captured.IsPrimary == current.IsPrimary ? 0 : 0.5;
        return ratioDelta + widthDelta + heightDelta + primaryPenalty;
    }
}
