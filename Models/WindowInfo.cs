using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace FloatingTaskbarMenu.Models;

public class WindowInfo
{
    public nint Handle { get; set; }
    public string Title { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public int ProcessId { get; set; }
    public bool IsPinned { get; set; }
    public BitmapSource? Icon { get; set; }
}

public class WindowGroup
{
    public string GroupName { get; set; } = "";
    public BitmapSource? GroupIcon { get; set; }
    public bool IsPinned { get; set; }
    public int WindowCount => Windows.Count;
    public List<WindowInfo> Windows { get; set; } = new();
}

public class WindowListEntry
{
    public WindowInfo? Window { get; set; }
    public WindowGroup? Group { get; set; }
    public bool IsGroup => Group != null;
    public bool IsPinned => Window?.IsPinned == true || Group?.IsPinned == true;
    public string SortName => Window?.Title ?? Group?.GroupName ?? "";
}

public class WindowHistory
{
    public string Title { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public DateTime ClosedTime { get; set; } = DateTime.Now;
}

public enum HistoryFilter
{
    Session,
    Day,
    Week,
    Forever
}
