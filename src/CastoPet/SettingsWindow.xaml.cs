using System.Windows;
using System.Windows.Input;
using CastoPet.Core;
using WpfControls = System.Windows.Controls;

namespace CastoPet;

public partial class SettingsWindow : Window, ISettingsWindow
{
    private readonly MenuCommandService _commands;
    private readonly CrashReportService _crashReports;
    private readonly UpdateCoordinator _updates;
    private readonly ShortcutService _shortcutService;
    private readonly ShortcutDropHandler _shortcutDropHandler;
    private readonly ShortcutLauncher _shortcutLauncher;
    private readonly IReadOnlyList<SettingDefinition> _definitions;
    private readonly Dictionary<string, WpfControls.CheckBox> _switches = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private bool _updatingThemeChoice;
    private bool _isClosed;

    public SettingsWindow(
        MenuCommandService commands,
        CrashReportService crashReports,
        UpdateCoordinator updates,
        ShortcutService shortcutService,
        ShortcutDropHandler shortcutDropHandler,
        ShortcutLauncher shortcutLauncher)
    {
        InitializeComponent();
        _commands = commands;
        _crashReports = crashReports;
        _updates = updates;
        _shortcutService = shortcutService;
        _shortcutDropHandler = shortcutDropHandler;
        _shortcutLauncher = shortcutLauncher;
        _definitions = SettingCatalog.Create(commands);
        ApplyTheme();
        RefreshThemeChoice();
        BuildSettingsRows();
        RefreshValues();
        RefreshShortcutRows();
        CurrentVersionText.Text = $"当前版本 {_updates.CurrentVersion}";
        UpdateStatusText.Text = _updates.IsInstalled
            ? "每天启动时检查一次"
            : "开发版本不支持自动更新";
        _commands.SettingsChanged += OnSettingsChanged;
        _shortcutService.Changed += OnShortcutsChanged;
        SourceInitialized += OnSourceInitialized;
        Activated += OnSettingsWindowActivated;
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
        var label = new WpfControls.TextBlock
        {
            Text = GetGroupLabel(group),
            FontSize = 12.5,
            FontWeight = FontWeights.Medium,
        };
        label.SetResourceReference(WpfControls.TextBlock.ForegroundProperty, "PurpleBrush");

        var header = new WpfControls.Border
        {
            Margin = new Thickness(0, 6, 0, 0),
            Padding = new Thickness(11, 7, 11, 7),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = label,
        };
        header.SetResourceReference(WpfControls.Border.BackgroundProperty, "LavenderBrush");
        header.SetResourceReference(WpfControls.Border.BorderBrushProperty, "DividerBrush");
        return header;
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
        var title = new WpfControls.TextBlock
        {
            Text = definition.Label,
            FontSize = 13.5,
            FontWeight = FontWeights.Medium,
        };
        title.SetResourceReference(WpfControls.TextBlock.ForegroundProperty, "TextBrush");
        text.Children.Add(title);

        var description = new WpfControls.TextBlock
        {
            Text = definition.Description,
            Margin = new Thickness(0, 3, 0, 0),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
        };
        description.SetResourceReference(WpfControls.TextBlock.ForegroundProperty, "SecondaryTextBrush");
        text.Children.Add(description);

        var grid = new WpfControls.Grid { Margin = new Thickness(11, 0, 11, 0) };
        grid.ColumnDefinitions.Add(new WpfControls.ColumnDefinition());
        grid.ColumnDefinitions.Add(new WpfControls.ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(text);
        WpfControls.Grid.SetColumn(toggle, 1);
        grid.Children.Add(toggle);

        var row = new WpfControls.Border
        {
            MinHeight = 56,
            Padding = new Thickness(0, 5, 0, 5),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid,
        };
        row.SetResourceReference(WpfControls.Border.BorderBrushProperty, "DividerBrush");
        return row;
    }

    private void RefreshValues()
    {
        foreach (var definition in _definitions)
        {
            _switches[definition.Id].IsChecked = definition.GetValue();
        }
    }

    private void OnSettingsChanged()
    {
        RefreshValues();
        ApplyTheme();
        RefreshThemeChoice();
    }

    private void ApplyTheme()
    {
        var effectiveMode = ThemeModeResolver.Resolve(
            _commands.Settings.ThemeMode,
            WindowsSystemThemeReader.UsesDarkApps());
        var hasNativeHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle != IntPtr.Zero;
        var hasNativeBackdrop = SettingsBackdropService.TryApply(this, effectiveMode == AppThemeMode.Dark);
        SettingsThemePalette.Apply(Resources, effectiveMode, translucent: !hasNativeHandle || hasNativeBackdrop);
    }

    private void RefreshThemeChoice()
    {
        _updatingThemeChoice = true;
        try
        {
            SystemThemeButton.IsChecked = _commands.Settings.ThemeMode == AppThemeMode.System;
            LightThemeButton.IsChecked = _commands.Settings.ThemeMode == AppThemeMode.Light;
            DarkThemeButton.IsChecked = _commands.Settings.ThemeMode == AppThemeMode.Dark;
        }
        finally
        {
            _updatingThemeChoice = false;
        }
    }

    private void ThemeMode_Checked(object sender, RoutedEventArgs e)
    {
        if (_updatingThemeChoice || sender is not WpfControls.RadioButton { Tag: string value } ||
            !Enum.TryParse<AppThemeMode>(value, ignoreCase: true, out var mode))
        {
            return;
        }

        _commands.SetThemeMode(mode);
    }

    private void OnSourceInitialized(object? sender, EventArgs e) => ApplyTheme();

    private void OnSettingsWindowActivated(object? sender, EventArgs e)
    {
        if (_commands.Settings.ThemeMode == AppThemeMode.System)
        {
            ApplyTheme();
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

    private void ViewNavigation_Checked(object sender, RoutedEventArgs e)
    {
        if (GeneralView is null || ShortcutLauncherView is null || sender is not WpfControls.RadioButton button)
        {
            return;
        }

        var showShortcuts = string.Equals(button.Tag as string, "Shortcuts", StringComparison.Ordinal);
        GeneralView.Visibility = showShortcuts ? Visibility.Collapsed : Visibility.Visible;
        ShortcutLauncherView.Visibility = showShortcuts ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshShortcutRows(string? selectedId = null)
    {
        selectedId ??= (ShortcutList.SelectedItem as ShortcutListItem)?.Definition.Id;
        var rows = _shortcutService.GetAll().Select(CreateShortcutRow).ToArray();
        ShortcutList.ItemsSource = rows;
        ShortcutList.SelectedItem = rows.FirstOrDefault(row => row.Definition.Id == selectedId);
        if (ShortcutList.SelectedItem is null && rows.Length > 0)
        {
            ShortcutList.SelectedIndex = 0;
        }

        RefreshShortcutEditor();
    }

    private ShortcutListItem CreateShortcutRow(ShortcutDefinition definition)
    {
        var validity = "可用";
        try
        {
            _shortcutLauncher.CreateStartInfo(definition);
        }
        catch (Exception)
        {
            validity = "失效";
        }

        return new ShortcutListItem(
            definition,
            GetShortcutTypeLabel(definition.Type),
            validity);
    }

    private static string GetShortcutTypeLabel(ShortcutType type) => type switch
    {
        ShortcutType.Program => "程序",
        ShortcutType.File => "文件",
        ShortcutType.Folder => "文件夹",
        ShortcutType.WindowsShortcut => "快捷方式",
        ShortcutType.WebUrl => "网页",
        ShortcutType.SteamGame => "Steam 游戏",
        _ => "未知",
    };

    private void ShortcutList_SelectionChanged(object sender, WpfControls.SelectionChangedEventArgs e)
    {
        RefreshShortcutEditor();
    }

    private void ShortcutList_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        try
        {
            e.Effects = ShortcutDropDataReader.ContainsSupportedFormat(e.Data)
                ? System.Windows.DragDropEffects.Link
                : System.Windows.DragDropEffects.None;
        }
        catch (Exception)
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void ShortcutList_Drop(object sender, System.Windows.DragEventArgs e)
    {
        e.Handled = true;
        try
        {
            var result = _shortcutDropHandler.AddDroppedItems(
                ShortcutDropDataReader.ExtractPaths(e.Data),
                ShortcutDropDataReader.ExtractTextValues(e.Data));
            e.Effects = result.AddedCount > 0
                ? System.Windows.DragDropEffects.Link
                : System.Windows.DragDropEffects.None;
            ShortcutUrlErrorText.Text = result switch
            {
                { AddedCount: > 0, DuplicateCount: 0, UnsupportedCount: 0, FailedCount: 0 } => "",
                { AddedCount: > 0 } => $"已添加 {result.AddedCount} 项，部分内容未加入",
                { DuplicateCount: > 0, UnsupportedCount: 0, FailedCount: 0 } => "拖入的项目已存在",
                { UnsupportedCount: > 0, FailedCount: 0 } => "不支持该拖入内容",
                _ => "添加失败，请稍后重试",
            };
        }
        catch (Exception)
        {
            e.Effects = System.Windows.DragDropEffects.None;
            ShortcutUrlErrorText.Text = "无法读取拖入内容";
        }
    }

    private void RefreshShortcutEditor()
    {
        var row = ShortcutList.SelectedItem as ShortcutListItem;
        var hasSelection = row is not null;
        var isProgram = row?.Definition.Type == ShortcutType.Program;
        ShortcutNameTextBox.IsEnabled = hasSelection;
        ShortcutArgumentsTextBox.IsEnabled = isProgram;
        ShortcutWorkingDirectoryTextBox.IsEnabled = isProgram;
        SaveShortcutButton.IsEnabled = hasSelection;
        DeleteShortcutButton.IsEnabled = hasSelection;
        MoveShortcutUpButton.IsEnabled = hasSelection && ShortcutList.SelectedIndex > 0;
        MoveShortcutDownButton.IsEnabled = hasSelection && ShortcutList.SelectedIndex < ShortcutList.Items.Count - 1;
        ShortcutNameTextBox.Text = row?.Definition.Name ?? "";
        ShortcutArgumentsTextBox.Text = isProgram ? row!.Definition.Arguments : "";
        ShortcutWorkingDirectoryTextBox.Text = isProgram ? row!.Definition.WorkingDirectory ?? "" : "";
    }

    private void AddShortcutUrlButton_Click(object sender, RoutedEventArgs e)
    {
        var result = _shortcutDropHandler.AddDroppedItems([], [ShortcutUrlTextBox.Text]);
        if (result.AddedCount > 0)
        {
            ShortcutUrlTextBox.Clear();
            ShortcutUrlErrorText.Text = "";
        }
        else if (result.DuplicateCount > 0)
        {
            ShortcutUrlErrorText.Text = "该网址已存在";
        }
        else if (result.UnsupportedCount > 0)
        {
            ShortcutUrlErrorText.Text = "支持 http、https 或 steam://rungameid/游戏ID";
        }
        else
        {
            ShortcutUrlErrorText.Text = "添加失败，请稍后重试";
        }
    }

    private void SaveShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        if (ShortcutList.SelectedItem is not ShortcutListItem row)
        {
            return;
        }

        var name = ShortcutNameTextBox.Text.Trim();
        if (name.Length == 0)
        {
            ShortcutUrlErrorText.Text = "名称不能为空";
            return;
        }

        if (row.Definition.Type == ShortcutType.Program)
        {
            var launchResult = _shortcutService.UpdateLaunchOptions(
                row.Definition.Id,
                ShortcutArgumentsTextBox.Text,
                ShortcutWorkingDirectoryTextBox.Text);
            if (!launchResult.Succeeded)
            {
                ShortcutUrlErrorText.Text = "工作目录不存在";
                return;
            }
        }

        var renameResult = _shortcutService.Rename(row.Definition.Id, name);
        ShortcutUrlErrorText.Text = renameResult.Succeeded ? "" : "保存失败";
    }

    private void MoveShortcutUpButton_Click(object sender, RoutedEventArgs e) => MoveSelectedShortcut(-1);

    private void MoveShortcutDownButton_Click(object sender, RoutedEventArgs e) => MoveSelectedShortcut(1);

    private void MoveSelectedShortcut(int offset)
    {
        if (ShortcutList.SelectedItem is not ShortcutListItem row)
        {
            return;
        }

        var destination = ShortcutList.SelectedIndex + offset;
        var result = _shortcutService.Move(row.Definition.Id, destination);
        ShortcutUrlErrorText.Text = result.Succeeded ? "" : "无法移动";
    }

    private void DeleteShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        if (ShortcutList.SelectedItem is not ShortcutListItem row)
        {
            return;
        }

        var result = _shortcutService.Delete(row.Definition.Id);
        ShortcutUrlErrorText.Text = result.Succeeded ? "" : "删除失败";
    }

    private void OnShortcutsChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(RefreshShortcutRows);
            return;
        }

        RefreshShortcutRows();
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
            var result = await _updates.CheckAsync(manual: true, _lifetimeCancellation.Token);
            if (_isClosed)
            {
                return;
            }

            UpdateStatusText.Text = GetUpdateStatusText(result);
            if (result.Status == UpdateCheckStatus.Available && result.AvailableUpdate is not null)
            {
                await PromptAndInstallUpdateAsync(result.AvailableUpdate);
            }
        }
        finally
        {
            if (!_isClosed)
            {
                CheckForUpdatesButton.IsEnabled = true;
            }
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

        var progress = new Progress<int>(value =>
        {
            if (!_isClosed)
            {
                UpdateStatusText.Text = $"正在下载更新 {value}%";
            }
        });
        if (!await _updates.DownloadUpdatesAsync(update, progress, _lifetimeCancellation.Token))
        {
            if (!_isClosed)
            {
                UpdateStatusText.Text = "下载失败，请稍后重试";
            }

            return;
        }

        if (_isClosed)
        {
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
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        _lifetimeCancellation.Cancel();
        _commands.SettingsChanged -= OnSettingsChanged;
        _shortcutService.Changed -= OnShortcutsChanged;
        SourceInitialized -= OnSourceInitialized;
        Activated -= OnSettingsWindowActivated;
        Closed -= OnSettingsWindowClosed;
        _lifetimeCancellation.Dispose();
    }

    private sealed record ShortcutListItem(
        ShortcutDefinition Definition,
        string TypeLabel,
        string ValidityLabel)
    {
        public string Name => Definition.Name;

        public string Target => Definition.Target;
    }
}
