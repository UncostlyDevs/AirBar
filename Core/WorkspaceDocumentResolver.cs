using System.IO;
using FloatingTaskbarMenu.Models;

namespace FloatingTaskbarMenu.Core;

internal static class WorkspaceDocumentResolver
{
    public static string ResolveDocumentPath(WindowInfo window)
    {
        if (!NeedsDocumentTarget(window.ProcessName, window.Title))
            return "";

        return ResolveDocumentPathFromTitle(window.Title);
    }

    public static string ResolveDocumentPath(WorkspaceItem item)
    {
        if (!NeedsDocumentTarget(item.ProcessName, item.WindowTitle))
            return "";

        return ResolveDocumentPathFromTitle(item.WindowTitle);
    }

    public static bool NeedsDocumentTarget(WorkspaceItem item)
        => NeedsDocumentTarget(item.ProcessName, item.WindowTitle);

    public static bool NeedsDocumentTarget(string processName, string title)
    {
        if (string.IsNullOrWhiteSpace(title) || !LooksLikeFileName(title))
            return false;

        return processName.Equals("Photos", StringComparison.OrdinalIgnoreCase)
            || processName.Equals("Microsoft.Photos", StringComparison.OrdinalIgnoreCase)
            || processName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase)
            || IsKnownDocumentExtension(Path.GetExtension(title));
    }

    public static bool IsMissingDocumentTarget(WorkspaceItem item)
    {
        if (!NeedsDocumentTarget(item))
            return false;

        return string.IsNullOrWhiteSpace(item.DocumentPath) || !File.Exists(item.DocumentPath);
    }

    public static string ResolveDocumentPathFromTitle(string title, IEnumerable<string>? searchDirectories = null, string? recentDirectory = null)
    {
        var fileName = Path.GetFileName(title.Trim());
        if (string.IsNullOrWhiteSpace(fileName) || !LooksLikeFileName(fileName))
            return "";

        foreach (var candidate in EnumerateDocumentCandidates(fileName, searchDirectories, recentDirectory))
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return "";
    }

    public static bool IsKnownDocumentExtension(string extension)
    {
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".heic", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeFileName(string title)
        => IsKnownDocumentExtension(Path.GetExtension(title));

    private static IEnumerable<string> EnumerateDocumentCandidates(
        string fileName,
        IEnumerable<string>? searchDirectories,
        string? recentDirectory)
    {
        var directories = searchDirectories?.Where(path => !string.IsNullOrWhiteSpace(path))
            ?? EnumerateDefaultDocumentDirectories();

        foreach (var directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate))
                yield return candidate;
        }

        var recent = recentDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.Recent);
        foreach (var recentTarget in ResolveRecentShortcutTargets(fileName, recent))
            yield return recentTarget;
    }

    private static IEnumerable<string> EnumerateDefaultDocumentDirectories()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var oneDrive = Path.Combine(profile, "OneDrive");

        return new[]
        {
            pictures,
            Path.Combine(pictures, "Screenshots"),
            desktop,
            Path.Combine(profile, "Downloads"),
            Path.Combine(oneDrive, "Pictures"),
            Path.Combine(oneDrive, "Pictures", "Screenshots"),
            Path.Combine(oneDrive, "Desktop"),
            Path.Combine(oneDrive, "Downloads")
        };
    }

    private static IEnumerable<string> ResolveRecentShortcutTargets(string fileName, string recentDirectory)
    {
        if (string.IsNullOrWhiteSpace(recentDirectory) || !Directory.Exists(recentDirectory))
            yield break;

        IEnumerable<string> shortcuts;
        try
        {
            shortcuts = Directory.EnumerateFiles(recentDirectory, "*.lnk", SearchOption.TopDirectoryOnly)
                .Where(path => Path.GetFileNameWithoutExtension(path).Contains(Path.GetFileNameWithoutExtension(fileName), StringComparison.OrdinalIgnoreCase))
                .Take(20)
                .ToList();
        }
        catch
        {
            yield break;
        }

        foreach (var shortcut in shortcuts)
        {
            var target = ResolveShortcutTarget(shortcut);
            if (!string.IsNullOrWhiteSpace(target)
                && string.Equals(Path.GetFileName(target), fileName, StringComparison.OrdinalIgnoreCase))
            {
                yield return target;
            }
        }
    }

    private static string ResolveShortcutTarget(string shortcutPath)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
                return "";

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            return shortcut.TargetPath as string ?? "";
        }
        catch
        {
            return "";
        }
    }
}
