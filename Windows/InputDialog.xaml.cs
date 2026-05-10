using System.Windows;

namespace FloatingTaskbarMenu.Windows;

public partial class InputDialog : Window
{
    public string InputText { get; private set; } = "";

    public InputDialog(string title, string prompt, string initialValue = "")
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        InputTextBox.Text = initialValue;
        InputTextBox.SelectAll();
        InputTextBox.Focus();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        InputText = InputTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
