using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using FloatingTaskbarMenu.Models;
using WpfApplication = System.Windows.Application;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfFontFamily = System.Windows.Media.FontFamily;

namespace FloatingTaskbarMenu.Core;

public class ThemeService
{
    public static event Action? ThemeResourcesApplied;

    private static readonly List<string> ThemeNames =
    [
        "Dark",
        "Light",
        "Windows 1.x",
        "Windows 3.1",
        "Windows 95",
        "Windows 98",
        "Windows ME",
        "Windows XP Luna",
        "Windows 7 Aero",
        "Windows 10"
    ];

    public List<string> GetThemeNames()
        => new(ThemeNames);

    public static string NormalizeThemeName(string? themeName)
    {
        var normalized = (themeName ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return "Dark";

        if (normalized is "win7" or "win 7" or "windows 7" or "win7 aero" or "windows 7 aero")
            return "Windows 7 Aero";

        if (normalized is "win10" or "win 10" or "windows 10" or "win10 light" or "windows 10 light")
            return "Windows 10";

        if (normalized is "light" or "win11 light" or "windows 11 light")
            return "Light";

        if (normalized is "windows 1.x" or "windows 1" or "win 1" or "win1")
            return "Windows 1.x";

        if (normalized.Contains("3.1") || normalized.Contains("windows 3") || normalized.Contains("win 3"))
            return "Windows 3.1";

        if (normalized.Contains("95"))
            return "Windows 95";

        if (normalized.Contains("98"))
            return "Windows 98";

        if (normalized is "me" or "win me" or "windows me" || normalized.Contains("millennium"))
            return "Windows ME";

        if (normalized.Contains("xp") || normalized.Contains("luna"))
            return "Windows XP Luna";

        return "Dark";
    }

    public Theme LoadTheme(string themeName)
    {
        return NormalizeThemeName(themeName) switch
        {
            "Light" => new Theme
            {
                Name = "Light",
                BackgroundColor = "#FAFAFA",
                HeaderColor = "#FFFFFF",
                FooterColor = "#FFFFFF",
                SurfaceColor = "#FFFFFF",
                ForegroundColor = "#000000",
                SecondaryForegroundColor = "#000000",
                HeaderForegroundColor = "#000000",
                AccentColor = "#0067C0",
                AccentForegroundColor = "#000000",
                BorderColor = "#D6D6D6",
                HoverColor = "#F0F0F0",
                PressedColor = "#E6E6E6",
                CornerRadius = 12,
                BorderThickness = 1,
                FontSize = 12,
                MinimalMode = false,
                DarkMode = false
            },
            "Windows 1.x" => ClassicTheme("Windows 1.x", "#FFFFFF", "#000000", "#000000", "#FFFFFF", "#E8E8E8", "#C0C0C0", "System, Fixedsys, MS Sans Serif, Segoe UI"),
            "Windows 3.1" => ClassicTheme("Windows 3.1", "#C0C0C0", "#000080", "#000000", "#FFFFFF", "#DCDCDC", "#A0A0A0", "MS Sans Serif, Microsoft Sans Serif, Tahoma, Segoe UI"),
            "Windows 95" => ClassicTheme("Windows 95", "#C0C0C0", "#000080", "#000000", "#FFFFFF", "#E0E0E0", "#A0A0A0", "MS Sans Serif, Microsoft Sans Serif, Tahoma, Segoe UI"),
            "Windows 98" => ClassicTheme("Windows 98", "#D4D0C8", "#000080", "#000000", "#FFFFFF", "#E7E3DA", "#B8B4AA", "MS Sans Serif, Microsoft Sans Serif, Tahoma, Segoe UI"),
            "Windows ME" => ClassicTheme("Windows ME", "#D4D0C8", "#0066CC", "#000000", "#FFFFFF", "#EAF2FF", "#B8CCE8", "Microsoft Sans Serif, Tahoma, Segoe UI"),
            "Windows XP Luna" => new Theme
            {
                Name = "Windows XP Luna",
                BackgroundColor = "#ECE9D8",
                HeaderColor = "#245EDC",
                FooterColor = "#ECE9D8",
                SurfaceColor = "#FFFDF2",
                ForegroundColor = "#000000",
                SecondaryForegroundColor = "#000000",
                HeaderForegroundColor = "#FFFFFF",
                AccentColor = "#316AC5",
                AccentForegroundColor = "#FFFFFF",
                BorderColor = "#003C74",
                HoverColor = "#DDEEFF",
                PressedColor = "#C7D9F5",
                FontFamily = "Tahoma, Segoe UI",
                DisplayFontFamily = "Tahoma, Segoe UI",
                CornerRadius = 6,
                BorderThickness = 1,
                FontSize = 12,
                MinimalMode = false,
                DarkMode = false
            },
            "Windows 7 Aero" => new Theme
            {
                Name = "Windows 7 Aero",
                BackgroundColor = "#EEF5FC",
                HeaderColor = "#D7EBFA",
                FooterColor = "#F7FBFF",
                SurfaceColor = "#F6FBFF",
                ForegroundColor = "#111111",
                SecondaryForegroundColor = "#2E2E2E",
                HeaderForegroundColor = "#111111",
                AccentColor = "#0078D7",
                AccentForegroundColor = "#FFFFFF",
                BorderColor = "#6EA6D8",
                HoverColor = "#E8F3FC",
                PressedColor = "#C8E3F7",
                FontFamily = "Segoe UI, Tahoma",
                DisplayFontFamily = "Segoe UI, Tahoma",
                CornerRadius = 7,
                BorderThickness = 1,
                FontSize = 12,
                MinimalMode = false,
                DarkMode = false,
                UseGlassEffect = true
            },
            "Windows 10" => new Theme
            {
                Name = "Windows 10",
                BackgroundColor = "#F2F2F2",
                HeaderColor = "#0078D7",
                FooterColor = "#F7F7F7",
                SurfaceColor = "#FFFFFF",
                ForegroundColor = "#1F1F1F",
                SecondaryForegroundColor = "#333333",
                HeaderForegroundColor = "#FFFFFF",
                AccentColor = "#0078D7",
                AccentForegroundColor = "#FFFFFF",
                BorderColor = "#D0D0D0",
                HoverColor = "#E5F1FB",
                PressedColor = "#CCE4F7",
                FontFamily = "Segoe UI, Segoe UI Variable",
                DisplayFontFamily = "Segoe UI, Segoe UI Variable Display",
                CornerRadius = 2,
                BorderThickness = 1,
                FontSize = 12,
                MinimalMode = false,
                DarkMode = false
            },
            _ => new Theme
            {
                Name = "Dark",
                BackgroundColor = "#202020",
                HeaderColor = "#2C2C2C",
                FooterColor = "#2C2C2C",
                SurfaceColor = "#2C2C2C",
                ForegroundColor = "#FFFFFF",
                SecondaryForegroundColor = "#FFFFFF",
                HeaderForegroundColor = "#FFFFFF",
                AccentColor = "#60CDFF",
                AccentForegroundColor = "#FFFFFF",
                BorderColor = "#383838",
                HoverColor = "#383838",
                PressedColor = "#484848",
                CornerRadius = 12,
                BorderThickness = 1,
                FontSize = 12,
                MinimalMode = false,
                DarkMode = true
            }
        };
    }

    public void ApplyThemeResources(string themeName)
        => ApplyThemeResources(LoadTheme(themeName));

    public void ApplyThemeResources(Settings settings)
    {
        settings.CurrentTheme = NormalizeThemeName(settings.CurrentTheme);
        MigrateLegacyTextColors(settings);

        var theme = LoadTheme(settings.CurrentTheme);
        theme.CornerRadius = settings.CornerRadius;
        theme.FontSize = settings.FontSize;
        theme.MinimalMode = settings.MinimalMode;

        var textColors = GetCurrentTextColors(settings);
        if (textColors.Enabled)
        {
            theme.ForegroundColor = ResolveColor(textColors.PrimaryColor, theme.ForegroundColor);
            theme.SecondaryForegroundColor = ResolveColor(textColors.SecondaryColor, theme.SecondaryForegroundColor);
        }

        settings.UseCustomTextColors = textColors.Enabled;
        ApplyThemeResources(theme);
    }

    public ThemeTextColorSettings GetCurrentTextColors(Settings settings)
    {
        settings.CurrentTheme = NormalizeThemeName(settings.CurrentTheme);
        MigrateLegacyTextColors(settings);

        var theme = LoadTheme(settings.CurrentTheme);
        if (settings.CustomTextColorsByTheme.TryGetValue(settings.CurrentTheme, out var saved))
        {
            return new ThemeTextColorSettings
            {
                Enabled = saved.Enabled,
                PrimaryColor = ResolveColor(saved.PrimaryColor, theme.ForegroundColor),
                SecondaryColor = ResolveColor(saved.SecondaryColor, theme.SecondaryForegroundColor)
            };
        }

        return new ThemeTextColorSettings
        {
            Enabled = false,
            PrimaryColor = theme.ForegroundColor,
            SecondaryColor = theme.SecondaryForegroundColor
        };
    }

    public void SetCurrentTextColors(Settings settings, bool enabled, string primaryColor, string secondaryColor)
    {
        settings.CurrentTheme = NormalizeThemeName(settings.CurrentTheme);
        var theme = LoadTheme(settings.CurrentTheme);
        var primary = ResolveColor(primaryColor, theme.ForegroundColor);
        var secondary = ResolveColor(secondaryColor, theme.SecondaryForegroundColor);

        settings.CustomTextColorsByTheme[settings.CurrentTheme] = new ThemeTextColorSettings
        {
            Enabled = enabled,
            PrimaryColor = primary,
            SecondaryColor = secondary
        };
        settings.UseCustomTextColors = enabled;
        SyncLegacyTextColorFields(settings, primary, secondary);
    }

    public void ResetCurrentTextColors(Settings settings)
    {
        settings.CurrentTheme = NormalizeThemeName(settings.CurrentTheme);
        var theme = LoadTheme(settings.CurrentTheme);
        settings.CustomTextColorsByTheme[settings.CurrentTheme] = new ThemeTextColorSettings
        {
            Enabled = false,
            PrimaryColor = theme.ForegroundColor,
            SecondaryColor = theme.SecondaryForegroundColor
        };
        settings.UseCustomTextColors = false;
        SyncLegacyTextColorFields(settings, theme.ForegroundColor, theme.SecondaryForegroundColor);
    }

    public void ApplyThemeResources(Theme theme)
    {
        try
        {
            var resources = WpfApplication.Current?.Resources;
            if (resources == null) return;

            resources["AirBarCurrentTheme"] = theme.Name;
            SetBrush(resources, "TextPrimaryBrush", theme.ForegroundColor);
            SetBrush(resources, "TextSecondaryBrush", theme.SecondaryForegroundColor);
            SetBrush(resources, "TextPrimaryBrushDark", theme.ForegroundColor);
            SetBrush(resources, "TextSecondaryBrushDark", theme.SecondaryForegroundColor);
            SetBrush(resources, "HeaderTextBrush", theme.HeaderForegroundColor);
            SetBrush(resources, "BorderBrush", theme.BorderColor);
            SetBrush(resources, "BorderBrushDark", theme.BorderColor);
            SetBrush(resources, "HeaderBrush", theme.HeaderColor);
            SetBrush(resources, "FooterBrush", theme.FooterColor);
            SetBrush(resources, "SurfaceBrush", theme.SurfaceColor);
            SetBrush(resources, "HoverBrush", theme.HoverColor);
            SetBrush(resources, "HoverBrushDark", theme.HoverColor);
            SetBrush(resources, "PressedBrush", theme.PressedColor);
            SetBrush(resources, "PressedBrushDark", theme.PressedColor);
            SetBrush(resources, "AccentBrush", theme.AccentColor);
            SetBrush(resources, "AccentDarkBrush", GetAccentHoverColor(theme.AccentColor, theme.DarkMode));
            SetBrush(resources, "AccentHoverBrush", GetAccentHoverColor(theme.AccentColor, theme.DarkMode));
            SetBrush(resources, "AccentTextBrush", theme.AccentForegroundColor);
            SetBrush(resources, "MenuBackgroundBrush", theme.BackgroundColor);
            SetBrush(resources, "MenuBorderBrush", theme.BorderColor);
            SetBrush(resources, "ItemHoverBrush", theme.HoverColor);
            SetBrush(resources, "ItemPressedBrush", theme.PressedColor);
            SetBrush(resources, "FlyoutBackgroundBrush", theme.SurfaceColor);
            SetBrush(resources, "ScrollThumbBrush", theme.SecondaryForegroundColor);
            SetBrush(resources, "ScrollThumbHoverBrush", theme.AccentColor);
            SetBrush(resources, "AcrylicFallbackBrush", theme.BackgroundColor);
            SetBrush(resources, "AcrylicFallbackBrushDark", theme.BackgroundColor);
            SetBrush(resources, "SeparatorBrush", theme.BorderColor);
            SetBrush(resources, "SeparatorBrushDark", theme.BorderColor);
            SetColor(resources, "Win11BgColor", theme.BackgroundColor);
            SetColor(resources, "Win11CardColor", theme.SurfaceColor);
            SetColor(resources, "Win11BorderColor", theme.BorderColor);
            SetColor(resources, "Win11HoverColor", theme.HoverColor);
            SetColor(resources, "Win11PressedColor", theme.PressedColor);
            SetColor(resources, "Win11AccentColor", theme.AccentColor);
            SetColor(resources, "Win11AccentDarkColor", GetAccentHoverColor(theme.AccentColor, theme.DarkMode));
            SetColor(resources, "Win11AcrylicBorderColor", theme.BorderColor);
            SetColor(resources, "AcrylicFallbackColor", theme.BackgroundColor);
            SetColor(resources, "AcrylicFallbackColorDark", theme.BackgroundColor);

            resources["AirBarCornerRadius"] = new CornerRadius(theme.CornerRadius);
            resources["AirBarInnerCornerRadius"] = new CornerRadius(Math.Max(0, theme.CornerRadius - 4));
            resources["AirBarHeaderCornerRadius"] = new CornerRadius(theme.CornerRadius, theme.CornerRadius, 0, 0);
            resources["AirBarFooterCornerRadius"] = new CornerRadius(0, 0, theme.CornerRadius, theme.CornerRadius);
            resources["AirBarBorderThickness"] = new Thickness(theme.BorderThickness);
            resources["AirBarBaseFontSize"] = (double)theme.FontSize;
            resources["AirBarFontFamily"] = new WpfFontFamily(theme.FontFamily);
            resources["AirBarDisplayFontFamily"] = new WpfFontFamily(theme.DisplayFontFamily);
            ThemeResourcesApplied?.Invoke();
        }
        catch { }
    }

    private static Theme ClassicTheme(string name, string background, string header, string foreground, string headerForeground, string hover, string pressed, string fontFamily)
    {
        return new Theme
        {
            Name = name,
            BackgroundColor = background,
            HeaderColor = header,
            FooterColor = background,
            SurfaceColor = background,
            ForegroundColor = foreground,
            SecondaryForegroundColor = foreground,
            HeaderForegroundColor = headerForeground,
            AccentColor = header,
            AccentForegroundColor = headerForeground,
            BorderColor = "#000000",
            HoverColor = hover,
            PressedColor = pressed,
            FontFamily = fontFamily,
            DisplayFontFamily = fontFamily,
            CornerRadius = 0,
            BorderThickness = 2,
            FontSize = 12,
            MinimalMode = false,
            DarkMode = false
        };
    }

    private void MigrateLegacyTextColors(Settings settings)
    {
        settings.CustomTextColorsByTheme ??= new Dictionary<string, ThemeTextColorSettings>();

        if (!settings.UseCustomTextColors)
            return;

        if (!settings.CustomTextColorsByTheme.ContainsKey("Light"))
        {
            settings.CustomTextColorsByTheme["Light"] = new ThemeTextColorSettings
            {
                Enabled = true,
                PrimaryColor = ResolveColor(settings.LightTextPrimaryColor, "#000000"),
                SecondaryColor = ResolveColor(settings.LightTextSecondaryColor, "#000000")
            };
        }

        if (!settings.CustomTextColorsByTheme.ContainsKey("Dark"))
        {
            settings.CustomTextColorsByTheme["Dark"] = new ThemeTextColorSettings
            {
                Enabled = true,
                PrimaryColor = ResolveColor(settings.DarkTextPrimaryColor, "#FFFFFF"),
                SecondaryColor = ResolveColor(settings.DarkTextSecondaryColor, "#FFFFFF")
            };
        }
    }

    private static void SyncLegacyTextColorFields(Settings settings, string primary, string secondary)
    {
        if (settings.CurrentTheme == "Dark")
        {
            settings.DarkTextPrimaryColor = primary;
            settings.DarkTextSecondaryColor = secondary;
        }
        else if (settings.CurrentTheme == "Light")
        {
            settings.LightTextPrimaryColor = primary;
            settings.LightTextSecondaryColor = secondary;
        }
    }

    private static void SetBrush(ResourceDictionary resources, string key, string colorText)
    {
        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(colorText);
            if (!SetBrushInDictionary(resources, key, color))
                resources[key] = new SolidColorBrush(color);
        }
        catch { }
    }

    private static bool SetBrushInDictionary(ResourceDictionary resources, string key, WpfColor color)
    {
        var updated = false;

        if (resources.Contains(key))
        {
            if (resources[key] is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.Color = color;
            }
            else
            {
                resources[key] = new SolidColorBrush(color);
            }

            updated = true;
        }

        foreach (var dictionary in resources.MergedDictionaries)
        {
            if (SetBrushInDictionary(dictionary, key, color))
                updated = true;
        }

        return updated;
    }

    private static void SetColor(ResourceDictionary resources, string key, string colorText)
    {
        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(colorText);
            if (!SetColorInDictionary(resources, key, color))
                resources[key] = color;
        }
        catch { }
    }

    private static bool SetColorInDictionary(ResourceDictionary resources, string key, WpfColor color)
    {
        var updated = false;

        if (resources.Contains(key))
        {
            resources[key] = color;
            updated = true;
        }

        foreach (var dictionary in resources.MergedDictionaries)
        {
            if (SetColorInDictionary(dictionary, key, color))
                updated = true;
        }

        return updated;
    }

    private static string ResolveColor(string colorText, string fallback)
    {
        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(colorText);
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        catch
        {
            return fallback;
        }
    }

    private static string GetAccentHoverColor(string accentColor, bool darkMode)
    {
        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(accentColor);
            var factor = darkMode ? 0.88 : 1.12;
            return $"#{Scale(color.R, factor):X2}{Scale(color.G, factor):X2}{Scale(color.B, factor):X2}";
        }
        catch
        {
            return accentColor;
        }
    }

    private static byte Scale(byte channel, double factor)
        => (byte)Math.Clamp((int)Math.Round(channel * factor), 0, 255);
}
