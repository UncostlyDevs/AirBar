using System;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace FloatingTaskbarMenu.Models;

public class AppLauncher
{
    public string Name { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    [JsonIgnore]
    public BitmapSource? Icon { get; set; }
    public int LaunchCount { get; set; }
    public DateTime LastLaunched { get; set; } = DateTime.Now;
    public bool IsPinned { get; set; }
}
