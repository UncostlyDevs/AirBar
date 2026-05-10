using System.Windows;
using System.Windows.Input;
using WpfButton = System.Windows.Controls.Button;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace FloatingTaskbarMenu.Windows;

public partial class ThemedMessageBox : Window
{
    private readonly MessageBoxButton _buttons;
    private MessageBoxResult _result;

    private ThemedMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
    {
        _buttons = buttons;
        _result = buttons == MessageBoxButton.YesNo ? MessageBoxResult.No : MessageBoxResult.OK;

        InitializeComponent();

        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        ConfigureIcon(image);
        ConfigureButtons(buttons);
    }

    public static MessageBoxResult Show(Window? owner, string message, string title, MessageBoxButton buttons, MessageBoxImage image)
    {
        var dialog = new ThemedMessageBox(message, title, buttons, image)
        {
            Owner = owner,
            WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner
        };

        dialog.ShowDialog();
        return dialog._result;
    }

    public static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
        => Show(null, message, title, buttons, image);

    private void ConfigureIcon(MessageBoxImage image)
    {
        IconText.Text = image switch
        {
            MessageBoxImage.Warning => "\uE7BA",
            MessageBoxImage.Error => "\uEA39",
            MessageBoxImage.Question => "\uE9CE",
            _ => "\uE946"
        };

        IconBadge.Visibility = image == MessageBoxImage.None
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void ConfigureButtons(MessageBoxButton buttons)
    {
        ButtonPanel.Children.Clear();

        if (buttons == MessageBoxButton.YesNo)
        {
            ButtonPanel.Children.Add(CreateButton("Yes", MessageBoxResult.Yes, true));
            ButtonPanel.Children.Add(CreateButton("No", MessageBoxResult.No, false));
            return;
        }

        ButtonPanel.Children.Add(CreateButton("OK", MessageBoxResult.OK, true));
    }

    private WpfButton CreateButton(string text, MessageBoxResult result, bool primary)
    {
        var button = new WpfButton
        {
            Content = text,
            MinWidth = 104,
            Height = 40,
            Padding = new Thickness(16, 0, 16, 0),
            Margin = new Thickness(ButtonPanel.Children.Count == 0 ? 0 : 10, 0, 0, 0),
            IsDefault = primary,
            IsCancel = !primary || _buttons == MessageBoxButton.OK
        };

        button.Style = (Style)FindResource(primary ? "PrimaryMenuItemButtonStyle" : "MenuItemButtonStyle");
        button.Click += (_, _) =>
        {
            _result = result;
            DialogResult = true;
            Close();
        };

        return button;
    }

    private void OnKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        _result = _buttons == MessageBoxButton.YesNo ? MessageBoxResult.No : MessageBoxResult.OK;
        DialogResult = _buttons == MessageBoxButton.YesNo ? false : true;
        Close();
    }
}
