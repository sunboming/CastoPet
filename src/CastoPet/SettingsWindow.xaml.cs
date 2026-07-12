using System.Windows;
using System.Windows.Input;
using CastoPet.Core;
using WpfControls = System.Windows.Controls;
using WpfMedia = System.Windows.Media;

namespace CastoPet;

public partial class SettingsWindow : Window, ISettingsWindow
{
    private readonly MenuCommandService _commands;
    private readonly CrashReportService _crashReports;
    private readonly UpdateCoordinator _updates;
    private readonly IReadOnlyList<SettingDefinition> _definitions;
    private readonly Dictionary<string, WpfControls.CheckBox> _switches = new(StringComparer.Ordinal);

    public SettingsWindow(
        MenuCommandService commands,
        CrashReportService crashReports,
        UpdateCoordinator updates)
    {
        InitializeComponent();
        _commands = commands;
        _crashReports = crashReports;
        _updates = updates;
        _definitions = SettingCatalog.Create(commands);
        BuildSettingsRows();
        RefreshValues();
        CurrentVersionText.Text = $"当前版本 {_updates.CurrentVersion}";
        UpdateStatusText.Text = _updates.IsInstalled
            ? "每天启动时检查一次"
            : "开发版本不支持自动更新";
        _commands.SettingsChanged += RefreshValues;
        Closed += OnSettingsWindowClosed;
    }

    private void BuildSettingsRows()
    {
        foreach (var group in _definitions.GroupBy(item => item.Group))
        {
            SettingsItemsHost.Children.Add(CreateGroupHeader(group.Key));
            foreach (var definition in group)
            {
                SettingsItemsHost.Children.Add(CreateSettingRow(definition));
            }
        }
    }

    private FrameworkElement CreateGroupHeader(SettingGroup group)
    {
        return new WpfControls.Border
        {
            Margin = new Thickness(0, 6, 0, 0),
            Padding = new Thickness(11, 7, 11, 7),
            Background = (WpfMedia.Brush)FindResource("LavenderBrush"),
            BorderBrush = (WpfMedia.Brush)FindResource("DividerBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = new WpfControls.TextBlock
            {
                Text = GetGroupLabel(group),
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                Foreground = (WpfMedia.Brush)FindResource("PurpleBrush"),
            },
        };
    }

    private FrameworkElement CreateSettingRow(SettingDefinition definition)
    {
        var toggle = new WpfControls.CheckBox
        {
            Style = (Style)FindResource("ToggleSwitchStyle"),
            IsChecked = definition.GetValue(),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = definition.Label,
        };
        toggle.Click += (_, _) => definition.Toggle();
        _switches.Add(definition.Id, toggle);

        var text = new WpfControls.StackPanel { VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new WpfControls.TextBlock
        {
            Text = definition.Label,
            FontSize = 13.5,
            FontWeight = FontWeights.Medium,
            Foreground = (WpfMedia.Brush)FindResource("TextBrush"),
        });
        text.Children.Add(new WpfControls.TextBlock
        {
            Text = definition.Description,
            Margin = new Thickness(0, 3, 0, 0),
            FontSize = 11,
            Foreground = (WpfMedia.Brush)FindResource("SecondaryTextBrush"),
            TextWrapping = TextWrapping.Wrap,
        });

        var grid = new WpfControls.Grid { Margin = new Thickness(11, 0, 11, 0) };
        grid.ColumnDefinitions.Add(new WpfControls.ColumnDefinition());
        grid.ColumnDefinitions.Add(new WpfControls.ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(text);
        WpfControls.Grid.SetColumn(toggle, 1);
        grid.Children.Add(toggle);

        return new WpfControls.Border
        {
            MinHeight = 56,
            Padding = new Thickness(0, 5, 0, 5),
            BorderBrush = (WpfMedia.Brush)FindResource("DividerBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid,
        };
    }

    private void RefreshValues()
    {
        foreach (var definition in _definitions)
        {
            _switches[definition.Id].IsChecked = definition.GetValue();
        }
    }

    private static string GetGroupLabel(SettingGroup group)
    {
        return group switch
        {
            SettingGroup.Behavior => "行为",
            SettingGroup.Interaction => "交互",
            SettingGroup.System => "系统",
            _ => group.ToString(),
        };
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OpenCrashReportsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_crashReports.OpenReportsDirectory())
        {
            System.Windows.MessageBox.Show("无法打开崩溃日志目录。", "CastoPet", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        CheckForUpdatesButton.IsEnabled = false;
        UpdateStatusText.Text = "正在检查更新...";
        try
        {
            var result = await _updates.CheckAsync(manual: true);
            UpdateStatusText.Text = GetUpdateStatusText(result);
            if (result.Status == UpdateCheckStatus.Available && result.AvailableUpdate is not null)
            {
                await PromptAndInstallUpdateAsync(result.AvailableUpdate);
            }
        }
        finally
        {
            CheckForUpdatesButton.IsEnabled = true;
        }
    }

    private async Task PromptAndInstallUpdateAsync(UpdateAvailability update)
    {
        var notes = string.IsNullOrWhiteSpace(update.ReleaseNotes)
            ? "此版本没有发布说明。"
            : update.ReleaseNotes;
        if (notes.Length > 600)
        {
            notes = notes[..600] + "...";
        }

        var choice = System.Windows.MessageBox.Show(
            $"发现新版本 {update.Version}\n\n{notes}\n\n是否立即更新？",
            "CastoPet 更新",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);
        if (choice != MessageBoxResult.Yes)
        {
            UpdateStatusText.Text = $"新版本 {update.Version} 可用，已稍后处理";
            return;
        }

        var progress = new Progress<int>(value => UpdateStatusText.Text = $"正在下载更新 {value}%");
        if (!await _updates.DownloadUpdatesAsync(update, progress))
        {
            UpdateStatusText.Text = "下载失败，请稍后重试";
            return;
        }

        UpdateStatusText.Text = "下载完成，正在重新启动...";
        _updates.ApplyUpdatesAndRestart(update);
    }

    private static string GetUpdateStatusText(UpdateCheckResult result)
    {
        return result.Status switch
        {
            UpdateCheckStatus.Current => "当前已是最新版本",
            UpdateCheckStatus.Available => $"发现新版本 {result.AvailableUpdate?.Version}",
            UpdateCheckStatus.DevelopmentBuild => "开发版本不支持自动更新",
            UpdateCheckStatus.Busy => "更新检查正在进行",
            UpdateCheckStatus.Skipped => "今天已经检查过更新",
            _ => "检查失败，请稍后重试",
        };
    }

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        _commands.SettingsChanged -= RefreshValues;
        Closed -= OnSettingsWindowClosed;
    }
}
