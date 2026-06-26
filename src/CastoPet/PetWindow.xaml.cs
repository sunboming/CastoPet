using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using CastoPet.Core;
using WpfControls = System.Windows.Controls;

namespace CastoPet;

public partial class PetWindow : Window
{
    private readonly LoggingService _logger;
    private readonly PetRuntimeState _runtimeState = new();
    private readonly ImageSource _defaultCharacter;
    private readonly ImageSource _draggingCharacter;
    private readonly IReadOnlyList<ImageSource> _idleFrames;
    private readonly DispatcherTimer _idleFrameTimer;
    private readonly DispatcherTimer _dragRestoreTimer;
    private AppSettings? _pendingSettings;
    private bool _applySettingsOnSourceInitialized;
    private bool _isClickThrough;
    private bool _isDragging;
    private int _idleFrameIndex;

    public PetWindow(AssetService assets, LoggingService logger)
    {
        InitializeComponent();
        _logger = logger;
        _idleFrameTimer = new DispatcherTimer { Interval = IdleFrameSequence.FrameInterval };
        _idleFrameTimer.Tick += (_, _) => AdvanceIdleFrame();
        _dragRestoreTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _dragRestoreTimer.Tick += (_, _) => RestoreAfterDrag();

        try
        {
            _defaultCharacter = assets.LoadDefaultCharacter();
            _draggingCharacter = assets.LoadDraggingCharacter();
            _idleFrames = assets.LoadIdleFrames();
            CharacterImage.Source = GetCurrentIdleFrame();
        }
        catch
        {
            _defaultCharacter = CharacterImage.Source;
            _draggingCharacter = CharacterImage.Source;
            _idleFrames = Array.Empty<ImageSource>();
            System.Windows.MessageBox.Show(
                "CastoPet 无法加载内置角色图片 Castorice.png。",
                "CastoPet",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        Loaded += (_, _) =>
        {
            WindowPlacementService.MoveToBottomRight(this);
            StartIdleAnimation();
        };
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    public void ApplySettings(AppSettings settings)
    {
        Topmost = settings.Topmost;
        ShowInTaskbar = settings.ShowInTaskbar;
        _isClickThrough = settings.ClickThrough;

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

        menu.Items.Add(CreateMenuItem(TrayService.ShowOrRestoreText, commands.ShowOrRestore));
        menu.Items.Add(new WpfControls.Separator());
        menu.Items.Add(CreateCheckedMenuItem(TrayService.AlwaysOnTopText, () => commands.Settings.Topmost, commands.ToggleTopmost));
        menu.Items.Add(CreateCheckedMenuItem(TrayService.MouseClickThroughText, () => commands.Settings.ClickThrough, commands.ToggleClickThrough));
        menu.Items.Add(CreateCheckedMenuItem(TrayService.ShowTaskbarIconText, () => commands.Settings.ShowInTaskbar, commands.ToggleShowInTaskbar));
        menu.Items.Add(CreateCheckedMenuItem(TrayService.StartWithWindowsText, () => commands.Settings.StartWithWindows, commands.ToggleStartWithWindows));
        menu.Items.Add(new WpfControls.Separator());
        menu.Items.Add(CreateMenuItem(TrayService.ExitText, commands.Exit));

        menu.Opened += (_, _) => RefreshContextMenuChecks(menu, commands);
        ContextMenu = menu;
        commands.SettingsChanged += () => RefreshContextMenuChecks(menu, commands);
    }

    public void ShowOrRestore()
    {
        var action = _runtimeState.GetShowRestoreAction(IsVisible);

        if (action == PetShowRestoreAction.ShowAtRuntimePosition)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            Left = _runtimeState.Left;
            Top = _runtimeState.Top;
            _logger.Info("Pet window shown at runtime position.");
            return;
        }

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

    private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isClickThrough || e.ButtonState != System.Windows.Input.MouseButtonState.Pressed)
        {
            return;
        }

        BeginDrag();
        try
        {
            DragMove();
            _runtimeState.SetRuntimePosition(Left, Top);
        }
        catch (InvalidOperationException ex)
        {
            _logger.Error("Pet drag operation failed.", ex);
        }
        finally
        {
            EndDrag();
        }
    }

    private void BeginDrag()
    {
        _isDragging = true;
        _dragRestoreTimer.Stop();
        StopIdleAnimation();
        CharacterImage.Source = _draggingCharacter;
    }

    private void EndDrag()
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        _dragRestoreTimer.Stop();
        _dragRestoreTimer.Start();
    }

    private void RestoreAfterDrag()
    {
        _dragRestoreTimer.Stop();
        _idleFrameIndex = 0;
        CharacterImage.Source = GetCurrentIdleFrame();
        StartIdleAnimation();
    }

    private void StartIdleAnimation()
    {
        if (_isDragging || _idleFrames.Count == 0)
        {
            return;
        }

        _idleFrameTimer.Start();
    }

    private void StopIdleAnimation()
    {
        _idleFrameTimer.Stop();
    }

    private void AdvanceIdleFrame()
    {
        if (_isDragging || _idleFrames.Count == 0)
        {
            return;
        }

        _idleFrameIndex = (_idleFrameIndex + 1) % _idleFrames.Count;
        CharacterImage.Source = GetCurrentIdleFrame();
    }

    private ImageSource GetCurrentIdleFrame()
    {
        return _idleFrames.Count == 0 ? _defaultCharacter : _idleFrames[_idleFrameIndex];
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
                TrayService.AlwaysOnTopText => commands.Settings.Topmost,
                TrayService.MouseClickThroughText => commands.Settings.ClickThrough,
                TrayService.ShowTaskbarIconText => commands.Settings.ShowInTaskbar,
                TrayService.StartWithWindowsText => commands.Settings.StartWithWindows,
                _ => item.IsChecked,
            };
        }
    }
}
