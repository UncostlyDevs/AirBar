namespace FloatingTaskbarMenu.Models;

public class Theme
{
    public string Name { get; set; } = "";
    public string BackgroundColor { get; set; } = "#202020";
    public string HeaderColor { get; set; } = "#2C2C2C";
    public string FooterColor { get; set; } = "#2C2C2C";
    public string SurfaceColor { get; set; } = "#2C2C2C";
    public string ForegroundColor { get; set; } = "#F3F3F3";
    public string SecondaryForegroundColor { get; set; } = "#C8C8C8";
    public string HeaderForegroundColor { get; set; } = "#F3F3F3";
    public string AccentColor { get; set; } = "#60CDFF";
    public string AccentForegroundColor { get; set; } = "#000000";
    public string BorderColor { get; set; } = "#3A3A3A";
    public string HoverColor { get; set; } = "#333333";
    public string PressedColor { get; set; } = "#3D3D3D";
    public string FontFamily { get; set; } = "Segoe UI Variable, Segoe UI";
    public string DisplayFontFamily { get; set; } = "Segoe UI Variable Display, Segoe UI";
    public double CornerRadius { get; set; } = 8;
    public double BorderThickness { get; set; } = 1;
    public double FontSize { get; set; } = 12;
    public bool MinimalMode { get; set; } = false;
    public bool DarkMode { get; set; } = true;
    public bool UseGlassEffect { get; set; } = false;
}
