using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FloatingTaskbarMenu.Core;
using WpfImage = System.Windows.Controls.Image;
using WpfBinding = System.Windows.Data.Binding;
using WpfFontFamily = System.Windows.Media.FontFamily;

namespace FloatingTaskbarMenu.Controls;

public sealed class ThemeIconView : ContentControl
{
    private readonly ThemeIconService _themeIcons = new();

    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(nameof(Kind), typeof(ThemeIconKind), typeof(ThemeIconView),
            new PropertyMetadata(ThemeIconKind.Settings, OnIconPropertyChanged));

    public static readonly DependencyProperty FallbackGlyphProperty =
        DependencyProperty.Register(nameof(FallbackGlyph), typeof(string), typeof(ThemeIconView),
            new PropertyMetadata(string.Empty, OnIconPropertyChanged));

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(ThemeIconView),
            new PropertyMetadata(16.0, OnIconPropertyChanged));

    public static readonly DependencyProperty GlyphFontSizeProperty =
        DependencyProperty.Register(nameof(GlyphFontSize), typeof(double), typeof(ThemeIconView),
            new PropertyMetadata(double.NaN, OnIconPropertyChanged));

    public ThemeIconKind Kind
    {
        get => (ThemeIconKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public string FallbackGlyph
    {
        get => (string)GetValue(FallbackGlyphProperty);
        set => SetValue(FallbackGlyphProperty, value);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public double GlyphFontSize
    {
        get => (double)GetValue(GlyphFontSizeProperty);
        set => SetValue(GlyphFontSizeProperty, value);
    }

    public ThemeIconView()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private static void OnIconPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ThemeIconView iconView)
            iconView.RefreshContent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ThemeService.ThemeResourcesApplied += OnThemeResourcesApplied;
        RefreshContent();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ThemeService.ThemeResourcesApplied -= OnThemeResourcesApplied;
    }

    private void OnThemeResourcesApplied()
        => Dispatcher.Invoke(RefreshContent);

    private void RefreshContent()
    {
        var iconSource = _themeIcons.GetIcon(Kind);
        if (iconSource != null)
        {
            var image = new WpfImage
            {
                Source = iconSource,
                Width = IconSize,
                Height = IconSize,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            Content = image;
            return;
        }

        var glyph = new TextBlock
        {
            Text = FallbackGlyph,
            FontFamily = new WpfFontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = double.IsNaN(GlyphFontSize) ? IconSize : GlyphFontSize,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };
        glyph.SetBinding(TextBlock.ForegroundProperty, new WpfBinding(nameof(Foreground)) { Source = this });
        Content = glyph;
    }
}
