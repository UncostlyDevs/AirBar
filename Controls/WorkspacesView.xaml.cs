using System.Windows;
using System.Windows.Controls;
using FloatingTaskbarMenu.Core;
using FloatingTaskbarMenu.Models;
using FloatingTaskbarMenu.Windows;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace FloatingTaskbarMenu.Controls;

public partial class WorkspacesView : WpfUserControl
{
    private WorkspaceService? _workspaceService;
    private WindowManager? _windowManager;
    private SettingsService? _settingsService;
    private Window? _ownerWindow;

    public WorkspacesView()
    {
        InitializeComponent();
    }

    public void SetContext(WorkspaceService workspaceService, WindowManager windowManager, SettingsService settingsService, Window? ownerWindow = null)
    {
        _workspaceService = workspaceService;
        _windowManager = windowManager;
        _settingsService = settingsService;
        _ownerWindow = ownerWindow;
        RefreshWorkspaces();
    }

    public void RefreshWorkspaces()
    {
        if (_workspaceService == null)
            return;

        var current = SelectedWorkspaceName();
        var names = _workspaceService.GetWorkspaceNames();
        var currentMonitors = _windowManager?.GetMonitors() ?? new List<WorkspaceMonitor>();
        var options = names
            .Select(name => CreateWorkspaceOption(name, currentMonitors))
            .ToList();

        WorkspaceCombo.ItemsSource = options;
        WorkspaceCombo.SelectedItem = current != null
            ? options.FirstOrDefault(option => string.Equals(option.Name, current, StringComparison.OrdinalIgnoreCase)) ?? options.FirstOrDefault()
            : options.FirstOrDefault();

        var hasWorkspaces = names.Count > 0;
        WorkspaceCombo.Visibility = hasWorkspaces ? Visibility.Visible : Visibility.Collapsed;
        RestoreButton.IsEnabled = hasWorkspaces;
        UpdateButton.IsEnabled = hasWorkspaces;
        DeleteButton.IsEnabled = hasWorkspaces;
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        WorkspaceStatusChips.Children.Clear();
        if (_workspaceService == null || WorkspaceCombo.SelectedItem is not WorkspaceOption option)
        {
            HideLastRestoreChip();
            return;
        }

        foreach (var warning in option.WarningLabels)
            AddStatusChip(warning, warning.Contains("remap", StringComparison.OrdinalIgnoreCase) ? "AccentBrush" : "TextSecondaryBrush");

        HideLastRestoreChip();
    }

    private void AddStatusChip(string text, string borderResource)
    {
        var chip = new Border
        {
            Margin = new Thickness(0, 0, 5, 4),
            Padding = new Thickness(6, 2, 6, 2),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 10,
                TextWrapping = TextWrapping.NoWrap
            }
        };

        chip.SetResourceReference(Border.BackgroundProperty, "HoverBrush");
        chip.SetResourceReference(Border.BorderBrushProperty, borderResource);
        chip.SetResourceReference(Border.CornerRadiusProperty, "AirBarInnerCornerRadius");
        ((TextBlock)chip.Child).SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
        WorkspaceStatusChips.Children.Add(chip);
    }

    private void OnWorkspaceSelectionChanged(object sender, SelectionChangedEventArgs e)
        => RefreshSummary();

    private void OnCaptureClick(object sender, RoutedEventArgs e)
    {
        var defaultName = $"Workspace {DateTime.Now:MMM d HH-mm}";
        var owner = GetOwnerWindow();
        using var autoCloseSuspension = owner is TaskbarMenuWindow menu ? menu.SuspendAutoClose() : null;

        var dialog = new InputDialog("Capture Workspace", "Workspace name:", defaultName)
        {
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.InputText))
            return;

        Capture(dialog.InputText);
    }

    private void OnUpdateClick(object sender, RoutedEventArgs e)
    {
        if (SelectedWorkspaceName() is { } name)
            Capture(name);
    }

    private void Capture(string name)
    {
        if (_workspaceService == null || _windowManager == null || _settingsService == null)
            return;

        var workspace = _workspaceService.CaptureWorkspace(name, _windowManager.GetWindows(), _settingsService.Settings, _windowManager);
        RefreshWorkspaces();
        SelectWorkspace(workspace.Name);

        var owner = Window.GetWindow(this);
        owner ??= GetOwnerWindow();
        using var autoCloseSuspension = owner is TaskbarMenuWindow menu ? menu.SuspendAutoClose() : null;
        ThemedMessageBox.Show(
            owner,
            $"Captured {workspace.Items.Count} window(s) into \"{workspace.Name}\".",
            "Workspace Captured",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void OnRestoreClick(object sender, RoutedEventArgs e)
    {
        if (_workspaceService == null || _windowManager == null || SelectedWorkspaceName() is not { } name)
            return;

        var owner = GetOwnerWindow();
        using var autoCloseSuspension = owner is TaskbarMenuWindow menu ? menu.SuspendAutoClose() : null;
        var workspace = _workspaceService.LoadWorkspace(name);
        var result = await Task.Run(() => _workspaceService.RestoreWorkspace(workspace, _windowManager));
        ShowLastRestoreChip(result);
        var notableItems = result.Items
            .Where(item => item.Status is WorkspaceRestoreStatus.Failed or WorkspaceRestoreStatus.Skipped || item.IgnoredLowConfidenceMatch)
            .Take(8)
            .ToList();
        var details = string.Join(Environment.NewLine, notableItems.Select(item =>
            $"{item.DisplayName}: {item.Status} - {item.Message}"));
        var remainingNotable = result.Items.Count(item =>
            item.Status is WorkspaceRestoreStatus.Failed or WorkspaceRestoreStatus.Skipped || item.IgnoredLowConfidenceMatch) - notableItems.Count;
        if (remainingNotable > 0)
            details += Environment.NewLine + $"...and {remainingNotable} more.";

        ThemedMessageBox.Show(
            owner,
            $"Done: {result.RestoredCount} restored, {result.LaunchedCount} launched"
            + (result.SkippedCount > 0 ? $", {result.SkippedCount} skipped" : "")
            + (result.FailedCount > 0 ? $", {result.FailedCount} failed" : "")
            + "."
            + (result.LowConfidenceCount > 0 ? $" Low-confidence ignored: {result.LowConfidenceCount}." : "")
            + (string.IsNullOrWhiteSpace(details) ? "" : Environment.NewLine + Environment.NewLine + details),
            "Workspace Restore",
            MessageBoxButton.OK,
            result.FailedCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (_workspaceService == null || SelectedWorkspaceName() is not { } name)
            return;

        var owner = GetOwnerWindow();
        using var autoCloseSuspension = owner is TaskbarMenuWindow menu ? menu.SuspendAutoClose() : null;
        var result = ThemedMessageBox.Show(owner, $"Delete workspace \"{name}\"?", "Delete Workspace", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
            return;

        _workspaceService.DeleteWorkspace(name);
        RefreshWorkspaces();
    }

    private Window? GetOwnerWindow()
        => _ownerWindow ?? Window.GetWindow(this);

    private WorkspaceOption CreateWorkspaceOption(string name, IReadOnlyList<WorkspaceMonitor> currentMonitors)
    {
        var workspace = _workspaceService!.LoadWorkspace(name);
        var preview = _workspaceService.BuildPreview(workspace, currentMonitors);
        return new WorkspaceOption(name, WorkspacePreviewFormatter.Format(preview));
    }

    private string? SelectedWorkspaceName()
        => WorkspaceCombo.SelectedItem switch
        {
            WorkspaceOption option => option.Name,
            string name => name,
            _ => null
        };

    private void SelectWorkspace(string name)
    {
        var option = WorkspaceCombo.Items
            .OfType<WorkspaceOption>()
            .FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));

        if (option != null)
            WorkspaceCombo.SelectedItem = option;
    }

    private void ShowLastRestoreChip(WorkspaceRestoreResult result)
    {
        var parts = new List<string>();
        if (result.RestoredCount > 0)
            parts.Add($"{result.RestoredCount} restored");
        if (result.LaunchedCount > 0)
            parts.Add($"{result.LaunchedCount} launched");
        if (result.SkippedCount > 0)
            parts.Add($"{result.SkippedCount} skipped");
        if (result.FailedCount > 0)
            parts.Add($"{result.FailedCount} failed");

        LastRestoreText.Text = parts.Count > 0 ? string.Join(", ", parts) : "No windows changed";
        LastRestoreChip.Visibility = Visibility.Visible;
    }

    private void HideLastRestoreChip()
    {
        LastRestoreChip.Visibility = Visibility.Collapsed;
        LastRestoreText.Text = "";
    }

    private sealed record WorkspaceOption(string Name, WorkspacePreviewDisplay Display)
    {
        public string Info => Display.Info;
        public string Health => Display.Health;
        public IReadOnlyList<string> WarningLabels => Display.WarningLabels;
        public bool HasWarnings => WarningLabels.Count > 0;
        public override string ToString() => Name;
    }
}
