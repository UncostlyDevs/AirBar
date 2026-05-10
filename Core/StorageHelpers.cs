using System.IO;
using System.Text.Json;

namespace FloatingTaskbarMenu.Core;

public static class StorageHelpers
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    public static void WriteJsonAtomic<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(value, IndentedJsonOptions);
        using (JsonDocument.Parse(json)) { }

        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, json);

        if (File.Exists(path))
            File.Replace(tempPath, path, null);
        else
            File.Move(tempPath, path);
    }

    public static string ToSafeFileName(string name, string fallback = "Untitled")
    {
        var trimmed = string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        var chars = trimmed.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray();
        var safe = new string(chars).Trim('.', ' ');
        return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
    }
}
