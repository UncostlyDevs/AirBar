using System.Windows.Media.Imaging;

namespace FloatingTaskbarMenu.Models;

public class TrayIconInfo
{
    public string Title { get; set; } = "";
    public string Tooltip { get; set; } = "";
    public BitmapSource? Icon { get; set; }
    public nint WindowHandle { get; set; }
}
