namespace FloatingTaskbarMenu.Models;

public class Theme
{
    public string Name { get; set; } = "";
    public string BackgroundColor { get; set; } = "#202020";
    public string HeaderColor { get; set; } = "#2C2C2C";
    public string FooterColor { get; set; } = "#2C2C2C";
    public string SurfaceColor { get; set; } = "#2C2C2C";
    public string ForegroundColor { get; set; } = "#FFFFFF";
    public string SecondaryForegroundColor { get; set; } = "#FFFFFF";
    public string HeaderForegroundColor { get; set; } = "#FFFFFF";
    public string AccentColor { get; set; } = "#60CDFF";
    public string AccentForegroundColor { get; set; } = "#FFFFFF";
    public string BorderColor { get; set; } = "#383838";
    public string HoverColor { get; set; } = "#383838";
    public string PressedColor { get; set; } = "#484848";
    public string FontFamily { get; set; } = "Segoe UI Variable, Segoe UI";
    public string DisplayFontFamily { get; set; } = "Segoe UI Variable Display, Segoe UI";
    public double CornerRadius { get; set; } = 12;
    public double BorderThickness { get; set; } = 1;
    public double FontSize { get; set; } = 12;
    public bool MinimalMode { get; set; } = false;
    public bool DarkMode { get; set; } = true;
    public bool UseGlassEffect { get; set; } = false;
}
