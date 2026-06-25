using System.Windows;
using System.Windows.Interop;
using CastoPet.Core;
using WpfControls = System.Windows.Controls;

namespace CastoPet;

public partial class PetWindow : Window
{
    private readonly LoggingService _logger;
    private AppSettings? _pendingSettings;
    private bool _applySettingsOnSourceInitialized;

    public PetWindow(AssetService assets, LoggingService logger)
    {
        InitializeComponent();
        _logger = logger;

        try
        {
            CharacterImage.Source = assets.LoadDefaultCharacter();
        }
        catch
        {
            System.Windows.MessageBox.Show(
                "CastoPet could not load the built-in character image Castorice.png.",
                "CastoPet",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        Loaded += (_, _) => WindowPlacementService.MoveToBottomRight(this);
    }

    public void ApplySettings(AppSettings settings)
    {
        Topmost = settings.Topmost;
        ShowInTaskbar = settings.ShowInTaskbar;

        if (new WindowInteropHelper(this).Handle == IntPtr.Zero)
        {
            _pendingSettings = settings;
            if (!_applySettingsOnSourceInitialized)
            {
                _applySettingsOnSourceInitialized = true;
                SourceInitialized += ApplyPendingSettings;
            }

            return;
        }

        ClickThroughService.Apply(this, settings.ClickThrough, settings.ShowInTaskbar);
    }

    public void AttachContextMenu(MenuCommandService commands)
    {
        var menu = new WpfControls.ContextMenu();

        menu.Items.Add(CreateMenuItem("Show/restore", commands.ShowOrRestore));
        menu.Items.Add(new WpfControls.Separator());
        menu.Items.Add(CreateCheckedMenuItem("Always on top", () => commands.Settings.Topmost, commands.ToggleTopmost));
        menu.Items.Add(CreateCheckedMenuItem("Mouse click-through", () => commands.Settings.ClickThrough, commands.ToggleClickThrough));
        menu.Items.Add(CreateCheckedMenuItem("Show taskbar icon", () => commands.Settings.ShowInTaskbar, commands.ToggleShowInTaskbar));
        menu.Items.Add(CreateCheckedMenuItem("Start with Windows", () => commands.Settings.StartWithWindows, commands.ToggleStartWithWindows));
        menu.Items.Add(new WpfControls.Separator());
        menu.Items.Add(CreateMenuItem("Exit", commands.Exit));

        menu.Opened += (_, _) => RefreshContextMenuChecks(menu, commands);
        ContextMenu = menu;
        commands.SettingsChanged += () => RefreshContextMenuChecks(menu, commands);
    }

    public void ShowOrRestore()
    {
        if (!IsVisible)
        {
            Show();
        }

        WindowState = WindowState.Normal;
        Activate();
        WindowPlacementService.MoveToBottomRight(this);
        _logger.Info("Pet window shown or restored.");
    }

    private void ApplyPendingSettings(object? sender, EventArgs e)
    {
        SourceInitialized -= ApplyPendingSettings;
        _applySettingsOnSourceInitialized = false;

        if (_pendingSettings is not null)
        {
            ClickThroughService.Apply(this, _pendingSettings.ClickThrough, _pendingSettings.ShowInTaskbar);
            _pendingSettings = null;
        }
    }

    private static WpfControls.MenuItem CreateMenuItem(string header, Action action)
    {
        var item = new WpfControls.MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private static WpfControls.MenuItem CreateCheckedMenuItem(string header, Func<bool> isChecked, Action action)
    {
        var item = new WpfControls.MenuItem
        {
            Header = header,
            IsCheckable = true,
            IsChecked = isChecked(),
        };
        item.SubmenuOpened += (_, _) => item.IsChecked = isChecked();
        item.Click += (_, _) => action();
        return item;
    }

    private static void RefreshContextMenuChecks(WpfControls.ContextMenu menu, MenuCommandService commands)
    {
        foreach (var item in menu.Items.OfType<WpfControls.MenuItem>())
        {
            var header = item.Header as string;
            item.IsChecked = header switch
            {
                "Always on top" => commands.Settings.Topmost,
                "Mouse click-through" => commands.Settings.ClickThrough,
                "Show taskbar icon" => commands.Settings.ShowInTaskbar,
                "Start with Windows" => commands.Settings.StartWithWindows,
                _ => item.IsChecked,
            };
        }
    }
}
