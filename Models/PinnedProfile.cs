using System.Collections.Generic;

namespace FloatingTaskbarMenu.Models;

public class PinnedProfile
{
    public string Name { get; set; } = "Default";
    public List<PinnedWindow> PinnedWindows { get; set; } = new();
}

public class PinnedWindow
{
    public string ExecutablePath { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public string ProcessName { get; set; } = "";
}
