using System.Windows.Controls;
using WpfUserControl = System.Windows.Controls.UserControl;
using FloatingTaskbarMenu.Models;

namespace FloatingTaskbarMenu.Controls;

public partial class SystemTrayView : WpfUserControl
{
    public SystemTrayView()
    {
        InitializeComponent();
        LoadTrayIcons();
    }

    private void LoadTrayIcons()
    {
        TrayIcons.ItemsSource = Array.Empty<TrayIconInfo>();
    }

    private void OnTrayIconClick(object sender, System.Windows.RoutedEventArgs e)
    {
    }
}
