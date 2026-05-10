using FloatingTaskbarMenu.Models;

namespace FloatingTaskbarMenu.Core;

internal static class WorkspacePreviewFormatter
{
    public static WorkspacePreviewDisplay Format(WorkspacePreview preview)
    {
        var warnings = new List<string>();
        if (preview.MissingPathCount > 0)
            warnings.Add(CompactCount(preview.MissingPathCount, "missing app"));
        if (preview.MissingDocumentCount > 0)
            warnings.Add(CompactCount(preview.MissingDocumentCount, "missing doc"));
        if (preview.RemappedMonitorCount > 0)
            warnings.Add("monitor layout changed");

        return new WorkspacePreviewDisplay(
            $"{CompactCount(preview.AppCount, "window")} - {MonitorInfo(preview)}",
            HealthLabel(preview),
            warnings);
    }

    private static string HealthLabel(WorkspacePreview preview)
    {
        if (preview.MissingPathCount > 0 || preview.MissingDocumentCount > 0)
            return "Needs attention";
        if (preview.RemappedMonitorCount > 0)
            return "Monitor changed";
        return "Ready";
    }

    private static string MonitorInfo(WorkspacePreview preview)
    {
        if (preview.CapturedMonitorCount == preview.CurrentMonitorCount)
            return CompactCount(preview.CurrentMonitorCount, "monitor");

        return $"saved on {CompactCount(preview.CapturedMonitorCount, "monitor")}, {preview.CurrentMonitorCount} now";
    }

    private static string CompactCount(int count, string singular)
        => $"{count} {Plural(count, singular, singular + "s")}";

    private static string Plural(int count, string singular, string plural)
        => count == 1 ? singular : plural;
}

internal sealed record WorkspacePreviewDisplay(string Info, string Health, IReadOnlyList<string> WarningLabels)
{
    public bool HasWarnings => WarningLabels.Count > 0;
}
