using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using FloatingTaskbarMenu.Models;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;
using Forms = System.Windows.Forms;

namespace FloatingTaskbarMenu.Core;

public class WindowManager
{
    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);
    private delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, string> _processNameCache = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, string> _processPathCache = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, System.Windows.Media.Imaging.BitmapSource?> _iconCache = new();

    private const int WM_GETICON = 0x007F;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;
    private const int ICON_SMALL2 = 2;
    private const int GCL_HICONSM = -34;
    private const int OBJID_WINDOW = 0;
    private const uint EVENT_OBJECT_CREATE = 0x8000;
    private const uint EVENT_OBJECT_SHOW = 0x8002;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
    private const int STARTF_USESHOWWINDOW = 0x00000001;
    private const int STARTF_USESIZE = 0x00000002;
    private const int STARTF_USEPOSITION = 0x00000004;

    /// <summary>
    /// Pre-warm process name cache on a background thread to reduce first-open lag.
    /// </summary>
    public void PreWarmCache()
    {
        try
        {
            var procs = Process.GetProcesses();
            foreach (var p in procs)
            {
                try
                {
                    _processNameCache[p.Id] = p.ProcessName;
                    p.Dispose();
                }
                catch { }
            }
        }
        catch { }
    }

    public List<WindowInfo> GetWindows()
    {
        try
        {
            var windows = new List<WindowInfo>();
            var currentProcessId = Environment.ProcessId;
            
            EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    var info = CaptureWindowInfo(hWnd, requireTitle: true, currentProcessId);
                    if (info != null)
                        windows.Add(info);
                }
                catch { }
                return true;
            }, nint.Zero);

            return windows;
        }
        catch
        {
            return new List<WindowInfo>();
        }
    }

    public List<WorkspaceMonitor> GetMonitors()
    {
        try
        {
            return Forms.Screen.AllScreens
                .Select(screen =>
                {
                    var bounds = screen.Bounds;
                    var workArea = screen.WorkingArea;
                    return new WorkspaceMonitor
                    {
                        Id = GetMonitorId(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom),
                        IsPrimary = screen.Primary,
                        Left = bounds.Left,
                        Top = bounds.Top,
                        Width = bounds.Width,
                        Height = bounds.Height,
                        WorkLeft = workArea.Left,
                        WorkTop = workArea.Top,
                        WorkWidth = workArea.Width,
                        WorkHeight = workArea.Height
                    };
                })
                .OrderByDescending(monitor => monitor.IsPrimary)
                .ThenBy(monitor => monitor.Left)
                .ThenBy(monitor => monitor.Top)
                .ToList();
        }
        catch
        {
            return new List<WorkspaceMonitor>();
        }
    }

    public IDisposable BeginWindowRestoreWatcher(Action<WindowInfo> onWindow)
        => new WindowRestoreWatcher(this, onWindow);

    public bool TryLaunchWorkspaceItem(WorkspaceItem item, WorkspaceRect targetBounds, out string message)
    {
        message = "";
        if (WorkspaceDocumentResolver.NeedsDocumentTarget(item))
        {
            if (string.IsNullOrWhiteSpace(item.DocumentPath))
            {
                message = "Document target was not resolved.";
                return false;
            }

            if (!File.Exists(item.DocumentPath))
            {
                message = "Captured document target is missing.";
                return false;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = item.DocumentPath,
                UseShellExecute = true
            });
            message = "Launched document";
            return true;
        }

        if (string.IsNullOrWhiteSpace(item.ExecutablePath))
        {
            message = "No executable path captured.";
            return false;
        }

        if (ShouldLaunchAsSettingsUri(item))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:",
                UseShellExecute = true
            });
            message = "Launched Settings";
            return true;
        }

        var isUri = Uri.TryCreate(item.ExecutablePath, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https" or "ms-settings" or "shell";
        if (!isUri && !File.Exists(item.ExecutablePath))
        {
            message = "Captured app path is missing.";
            return false;
        }

        if (!isUri && TryCreateProcessWithPlacementHint(item, targetBounds, out message))
            return true;

        var startInfo = new ProcessStartInfo
        {
            FileName = item.ExecutablePath,
            UseShellExecute = true
        };

        if (!string.IsNullOrWhiteSpace(item.Arguments))
            startInfo.Arguments = item.Arguments;

        if (!string.IsNullOrWhiteSpace(item.WorkingDirectory))
            startInfo.WorkingDirectory = item.WorkingDirectory;

        Process.Start(startInfo);
        message = isUri ? "Launched with shell" : "Launched with shell fallback";
        return true;
    }

    private static bool ShouldLaunchAsSettingsUri(WorkspaceItem item)
    {
        return item.ProcessName.Equals("SystemSettings", StringComparison.OrdinalIgnoreCase)
            || item.ExecutablePath.EndsWith("SystemSettings.exe", StringComparison.OrdinalIgnoreCase)
            || item.ExecutablePath.Contains("ImmersiveControlPanel", StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldSkipWindow(string processName, string title)
    {
        if (processName.Equals("WinAirBar", StringComparison.OrdinalIgnoreCase) ||
            processName.Equals("AirBar", StringComparison.OrdinalIgnoreCase) ||
            processName.Equals("FloatingTaskbarMenu", StringComparison.OrdinalIgnoreCase))
            return true;

        // Windows Settings often appears as an ApplicationFrameHost shell wrapper.
        if (processName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase) &&
            title.Contains("Settings", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public void ActivateWindow(nint hWnd)
    {
        if (IsIconic(hWnd))
        {
            ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
        }
        SetForegroundWindow(hWnd);
    }

    public void MinimizeWindow(nint hWnd)
    {
        ShowWindow(hWnd, ShowWindowCommand.SW_SHOWMINIMIZED);
    }

    public void MaximizeWindow(nint hWnd)
    {
        ShowWindow(hWnd, ShowWindowCommand.SW_SHOWMAXIMIZED);
    }

    public void RestoreWindowLayout(WindowInfo window, WorkspaceItem item)
    {
        RestoreWindowLayout(window.Handle, item);
    }

    public void RestoreWindowLayout(nint hWnd, WorkspaceItem item)
        => RestoreWindowLayout(hWnd, item, Array.Empty<WorkspaceMonitor>());

    public void RestoreWindowLayout(WindowInfo window, WorkspaceItem item, IReadOnlyList<WorkspaceMonitor> capturedMonitors)
    {
        RestoreWindowLayout(window.Handle, item, capturedMonitors);
    }

    public void RestoreWindowLayout(nint hWnd, WorkspaceItem item, IReadOnlyList<WorkspaceMonitor> capturedMonitors)
    {
        try
        {
            var targetBounds = ResolveTargetBounds(item, capturedMonitors, out _, out _);
            if (item.PlacementState != WindowPlacementState.Minimized)
                ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);

            if (!targetBounds.IsEmpty)
            {
                ApplyWindowPlacement(hWnd, item, targetBounds);
                MoveWindow(hWnd, (int)Math.Round(targetBounds.Left), (int)Math.Round(targetBounds.Top),
                    (int)Math.Round(targetBounds.Width), (int)Math.Round(targetBounds.Height), true);
            }

            if (item.PlacementState == WindowPlacementState.Maximized)
                ShowWindow(hWnd, ShowWindowCommand.SW_SHOWMAXIMIZED);
            else if (item.PlacementState == WindowPlacementState.Minimized)
                ShowWindow(hWnd, ShowWindowCommand.SW_SHOWMINIMIZED);
        }
        catch { }
    }

    public WorkspaceRect ResolveTargetBounds(
        WorkspaceItem item,
        IReadOnlyList<WorkspaceMonitor> capturedMonitors,
        out WorkspaceMonitor? targetMonitor,
        out bool remapped)
    {
        var currentMonitors = GetMonitors();
        return WorkspacePlacementPlanner.ResolveTargetBounds(item, capturedMonitors, currentMonitors, out targetMonitor, out remapped);
    }

    public void CloseWindow(nint hWnd)
    {
        SendMessage(hWnd, 0x0010, 0, 0); // WM_CLOSE
    }

    private string GetWindowText(nint hWnd)
    {
        int length = GetWindowTextLength(hWnd);
        if (length == 0) return "";

        var text = new System.Text.StringBuilder(length + 1);
        GetWindowText(hWnd, text, length + 1);
        return text.ToString();
    }

    private string GetWindowClassName(nint hWnd)
    {
        try
        {
            var className = new System.Text.StringBuilder(256);
            return GetClassName(hWnd, className, className.Capacity) > 0 ? className.ToString() : "";
        }
        catch
        {
            return "";
        }
    }

    private WindowInfo? CaptureWindowInfo(nint hWnd, bool requireTitle, int currentProcessId)
    {
        if (!IsWindowVisible(hWnd))
            return null;

        var title = GetWindowText(hWnd);
        if ((requireTitle && string.IsNullOrEmpty(title)) || title.Length > 100)
            return null;

        int exStyle = GetWindowLong(hWnd, -20); // GWL_EXSTYLE
        if ((exStyle & 0x00000080) != 0) // WS_EX_TOOLWINDOW
            return null;

        GetWindowThreadProcessId(hWnd, out int pid);
        if (pid == currentProcessId)
            return null;

        var processName = GetProcessName(pid);
        if (string.IsNullOrEmpty(processName) || ShouldSkipWindow(processName, title))
            return null;

        var rect = GetWindowBounds(hWnd);
        return new WindowInfo
        {
            Handle = hWnd,
            Title = title,
            ProcessName = processName,
            ExecutablePath = GetProcessExecutablePath(pid),
            ClassName = GetWindowClassName(hWnd),
            MonitorId = GetMonitorId(hWnd),
            Left = rect.Left,
            Top = rect.Top,
            Width = rect.Width,
            Height = rect.Height,
            PlacementState = IsIconic(hWnd)
                ? WindowPlacementState.Minimized
                : IsZoomed(hWnd) ? WindowPlacementState.Maximized : WindowPlacementState.Normal,
            Placement = GetWindowPlacementSnapshot(hWnd),
            ProcessId = pid,
            Icon = GetIconForProcess(hWnd, pid)
        };
    }

    private Rect GetWindowBounds(nint hWnd)
    {
        try
        {
            if (GetWindowRect(hWnd, out var rect))
                return new Rect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }
        catch { }

        return Rect.Empty;
    }

    private string GetMonitorId(nint hWnd)
    {
        try
        {
            var monitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO();
            info.cbSize = Marshal.SizeOf<MONITORINFO>();
            if (GetMonitorInfo(monitor, ref info))
                return GetMonitorId(info.rcMonitor.left, info.rcMonitor.top, info.rcMonitor.right, info.rcMonitor.bottom);
        }
        catch { }

        return "default";
    }

    public WorkspaceMonitor? GetMonitorForWindow(nint hWnd)
    {
        try
        {
            var monitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO();
            info.cbSize = Marshal.SizeOf<MONITORINFO>();
            if (!GetMonitorInfo(monitor, ref info))
                return null;

            return new WorkspaceMonitor
            {
                Id = GetMonitorId(info.rcMonitor.left, info.rcMonitor.top, info.rcMonitor.right, info.rcMonitor.bottom),
                IsPrimary = (info.dwFlags & 1) == 1,
                Left = info.rcMonitor.left,
                Top = info.rcMonitor.top,
                Width = info.rcMonitor.right - info.rcMonitor.left,
                Height = info.rcMonitor.bottom - info.rcMonitor.top,
                WorkLeft = info.rcWork.left,
                WorkTop = info.rcWork.top,
                WorkWidth = info.rcWork.right - info.rcWork.left,
                WorkHeight = info.rcWork.bottom - info.rcWork.top
            };
        }
        catch
        {
            return null;
        }
    }

    public static WorkspaceRect NormalizeBoundsToMonitor(WorkspaceRect bounds, WorkspaceMonitor? monitor)
        => WorkspacePlacementPlanner.NormalizeBoundsToMonitor(bounds, monitor);

    private WorkspaceWindowPlacement GetWindowPlacementSnapshot(nint hWnd)
    {
        try
        {
            var placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf<WINDOWPLACEMENT>();
            if (!GetWindowPlacement(hWnd, ref placement))
                return new WorkspaceWindowPlacement();

            return new WorkspaceWindowPlacement
            {
                HasPlacement = true,
                State = IsIconic(hWnd)
                    ? WindowPlacementState.Minimized
                    : IsZoomed(hWnd) ? WindowPlacementState.Maximized : WindowPlacementState.Normal,
                ShowCommand = placement.showCmd,
                NormalPosition = new WorkspaceRect
                {
                    Left = placement.rcNormalPosition.left,
                    Top = placement.rcNormalPosition.top,
                    Width = placement.rcNormalPosition.right - placement.rcNormalPosition.left,
                    Height = placement.rcNormalPosition.bottom - placement.rcNormalPosition.top
                }
            };
        }
        catch
        {
            return new WorkspaceWindowPlacement();
        }
    }

    private void ApplyWindowPlacement(nint hWnd, WorkspaceItem item, WorkspaceRect targetBounds)
    {
        if (item.Placement?.HasPlacement != true)
            return;

        try
        {
            var placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf<WINDOWPLACEMENT>();
            if (!GetWindowPlacement(hWnd, ref placement))
                return;

            placement.showCmd = (int)ShowWindowCommand.SW_SHOWNORMAL;
            placement.rcNormalPosition = new RECT
            {
                left = (int)Math.Round(targetBounds.Left),
                top = (int)Math.Round(targetBounds.Top),
                right = (int)Math.Round(targetBounds.Left + targetBounds.Width),
                bottom = (int)Math.Round(targetBounds.Top + targetBounds.Height)
            };
            SetWindowPlacement(hWnd, ref placement);
        }
        catch { }
    }

    private bool TryCreateProcessWithPlacementHint(WorkspaceItem item, WorkspaceRect targetBounds, out string message)
    {
        message = "";
        if (targetBounds.IsEmpty)
            return false;

        try
        {
            var arguments = MergeAppSpecificGeometryArguments(item, targetBounds);
            var commandLine = new StringBuilder($"\"{item.ExecutablePath}\"");
            if (!string.IsNullOrWhiteSpace(arguments))
                commandLine.Append(' ').Append(arguments);

            var startupInfo = new STARTUPINFO
            {
                cb = Marshal.SizeOf<STARTUPINFO>(),
                dwFlags = STARTF_USESHOWWINDOW | STARTF_USEPOSITION | STARTF_USESIZE,
                dwX = (int)Math.Round(targetBounds.Left),
                dwY = (int)Math.Round(targetBounds.Top),
                dwXSize = (int)Math.Max(1, Math.Round(targetBounds.Width)),
                dwYSize = (int)Math.Max(1, Math.Round(targetBounds.Height)),
                wShowWindow = (ushort)ShowWindowCommand.SW_SHOWNORMAL
            };

            var workingDirectory = string.IsNullOrWhiteSpace(item.WorkingDirectory) ? null : item.WorkingDirectory;
            if (!CreateProcess(null, commandLine, nint.Zero, nint.Zero, false, 0, nint.Zero, workingDirectory, ref startupInfo, out var processInfo))
                return false;

            CloseHandle(processInfo.hThread);
            CloseHandle(processInfo.hProcess);
            message = "Launched with placement hint";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string MergeAppSpecificGeometryArguments(WorkspaceItem item, WorkspaceRect targetBounds)
    {
        var arguments = item.Arguments?.Trim() ?? "";
        var processName = !string.IsNullOrWhiteSpace(item.ProcessName)
            ? item.ProcessName
            : Path.GetFileNameWithoutExtension(item.ExecutablePath);

        var isChromium = processName.Equals("chrome", StringComparison.OrdinalIgnoreCase)
            || processName.Equals("msedge", StringComparison.OrdinalIgnoreCase)
            || processName.Equals("brave", StringComparison.OrdinalIgnoreCase)
            || processName.Equals("vivaldi", StringComparison.OrdinalIgnoreCase);

        if (!isChromium || arguments.Contains("--window-position", StringComparison.OrdinalIgnoreCase))
            return arguments;

        var x = (int)Math.Round(targetBounds.Left);
        var y = (int)Math.Round(targetBounds.Top);
        var width = (int)Math.Round(targetBounds.Width);
        var height = (int)Math.Round(targetBounds.Height);
        return $"{arguments} --window-position={x},{y} --window-size={width},{height}".Trim();
    }

    private static string GetMonitorId(int left, int top, int right, int bottom)
        => $"{left}_{top}_{right}_{bottom}";

    private string GetProcessName(int processId)
    {
        if (_processNameCache.TryGetValue(processId, out var cachedName))
            return cachedName;

        try
        {
            using var process = Process.GetProcessById(processId);
            var name = process.ProcessName;
            _processNameCache[processId] = name;
            return name;
        }
        catch
        {
            return "";
        }
    }

    private string GetProcessExecutablePath(int processId)
    {
        if (_processPathCache.TryGetValue(processId, out var cachedPath))
            return cachedPath;

        try
        {
            using var process = Process.GetProcessById(processId);
            var path = process.MainModule?.FileName ?? "";
            _processPathCache[processId] = path;
            return path;
        }
        catch
        {
            _processPathCache[processId] = "";
            return "";
        }
    }

    /// <summary>
    /// Fast icon lookup using WM_GETICON. Caches result per process ID.
    /// Never calls Process.GetProcessById or File.Exists.
    /// </summary>
    private BitmapSource? GetIconForProcess(nint hWnd, int pid)
    {
        if (_iconCache.TryGetValue(pid, out var cached))
            return cached;

        var result = FetchIconFromWindow(hWnd);
        _iconCache[pid] = result;
        return result;
    }

    private BitmapSource? FetchIconFromWindow(nint hWnd)
    {
        try
        {
            // 1. WM_GETICON ICON_SMALL2 (best quality small icon)
            var hIcon = SendMessage(hWnd, WM_GETICON, ICON_SMALL2, 0);
            if (hIcon == nint.Zero)
                hIcon = SendMessage(hWnd, WM_GETICON, ICON_SMALL, 0);
            if (hIcon == nint.Zero)
                hIcon = SendMessage(hWnd, WM_GETICON, ICON_BIG, 0);

            // 2. Fallback to class icon
            if (hIcon == nint.Zero)
                hIcon = GetClassLongPtr(hWnd, GCL_HICONSM);
            if (hIcon == nint.Zero)
                hIcon = GetClassLongPtr(hWnd, -14); // GCL_HICON

            if (hIcon != nint.Zero)
            {
                var bmp = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                bmp.Freeze();
                return bmp;
            }
        }
        catch { }

        return null;
    }


    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(nint hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, ShowWindowCommand nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(nint hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(nint hWnd, out int lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern nint GetProp(nint hWnd, string lpString);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern nint GetClassLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(nint hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    private static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(nint hWinEventHook);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcess(
        string? lpApplicationName,
        StringBuilder lpCommandLine,
        nint lpProcessAttributes,
        nint lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        nint lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(nint hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public ushort wShowWindow;
        public ushort cbReserved2;
        public nint lpReserved2;
        public nint hStdInput;
        public nint hStdOutput;
        public nint hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public nint hProcess;
        public nint hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    private enum ShowWindowCommand : int
    {
        SW_HIDE = 0,
        SW_SHOWNORMAL = 1,
        SW_SHOWMINIMIZED = 2,
        SW_SHOWMAXIMIZED = 3,
        SW_SHOWNOACTIVATE = 4,
        SW_RESTORE = 9,
        SW_SHOWDEFAULT = 10,
    }

    enum WINDOW_STYLE : uint
    {
        WS_POPUP = 0x80000000,
        WS_CAPTION = 0x00C00000,
    }

    enum WINDOW_EX_STYLE : uint
    {
        WS_EX_TOOLWINDOW = 0x00000080,
    }

    enum GetWindowLongFlags : int
    {
        GWL_STYLE = -16,
        GWL_EXSTYLE = -20,
    }

    private sealed class WindowRestoreWatcher : IDisposable
    {
        private readonly WindowManager _owner;
        private readonly Action<WindowInfo> _onWindow;
        private readonly WinEventDelegate _callback;
        private readonly nint _hook;
        private bool _disposed;

        public WindowRestoreWatcher(WindowManager owner, Action<WindowInfo> onWindow)
        {
            _owner = owner;
            _onWindow = onWindow;
            _callback = OnWinEvent;
            _hook = SetWinEventHook(
                EVENT_OBJECT_CREATE,
                EVENT_OBJECT_SHOW,
                nint.Zero,
                _callback,
                0,
                0,
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
        }

        private void OnWinEvent(nint hWinEventHook, uint eventType, nint hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (_disposed || hWnd == nint.Zero || idObject != OBJID_WINDOW || idChild != 0)
                return;

            try
            {
                var info = _owner.CaptureWindowInfo(hWnd, requireTitle: false, Environment.ProcessId);
                if (info != null)
                    _onWindow(info);
            }
            catch { }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_hook != nint.Zero)
                UnhookWinEvent(_hook);
        }
    }
}
