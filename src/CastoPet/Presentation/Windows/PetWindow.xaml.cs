using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using WpfControls = System.Windows.Controls;

using CastoPet.Application.Menus;
using CastoPet.Application.Settings;
using CastoPet.Core.Animation;
using CastoPet.Core.Product;
using CastoPet.Core.Settings;
using CastoPet.Infrastructure.Assets;
using CastoPet.Infrastructure.Diagnostics;
using CastoPet.Infrastructure.Platform;

namespace CastoPet.Presentation.Windows;

public partial class PetWindow : Window, IPetCommandTarget
{
    private static readonly TimeSpan DefaultIdleFrameInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan DefaultBlinkFrameInterval = TimeSpan.FromMilliseconds(90);
    private static readonly TimeSpan DefaultBlinkMinScheduleDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DefaultBlinkMaxScheduleDelay = TimeSpan.FromSeconds(7);
    private const double MinimumDragThreshold = 6;

    private readonly LoggingService _logger;
    private readonly PetRuntimeState _runtimeState = new();
    private readonly Random _blinkRandom = new();
    private readonly PetActionDefinition _idleAction;
    private readonly PetActionDefinition _blinkAction;
    private readonly IReadOnlyList<ImageSource> _idleFrames;
    private readonly IReadOnlyList<ImageSource> _blinkFrames;
    private readonly ImageSource? _defaultCharacter;
    private readonly DispatcherTimer _idleFrameTimer;
    private readonly DispatcherTimer _blinkScheduleTimer;
    private readonly DispatcherTimer _blinkFrameTimer;
    private PetWindowSettingsSnapshot? _pendingSettings;
    private WpfControls.ContextMenu? _petContextMenu;
    private MenuCommandService? _menuCommands;
    private Action? _menuSettingsChangedHandler;
    private System.Windows.Point _leftPressPosition;
    private int _idleFrameIndex;
    private int _blinkFrameIndex;
    private bool _leftPointerPending;
    private bool _isDragging;
    private bool _isBlinking;
    private bool _isClickThrough;
    private bool _applySettingsOnSourceInitialized;
    private bool _runtimeResourcesReleased;

    public PetWindow(AssetService assets, LoggingService logger)
    {
        InitializeComponent();
        ArgumentNullException.ThrowIfNull(assets);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _idleAction = assets.Skin.GetRequiredAction(PetActionKind.Idle);
        _blinkAction = assets.Skin.GetRequiredAction(PetActionKind.Blink);

        try
        {
            _defaultCharacter = assets.LoadDefaultCharacter();
            _idleFrames = assets.LoadIdleFrames();
            _blinkFrames = assets.LoadBlinkFrames();
            CharacterImage.Source = CurrentIdleFrame;
        }
        catch
        {
            _defaultCharacter = CharacterImage.Source;
            _idleFrames = Array.Empty<ImageSource>();
            _blinkFrames = Array.Empty<ImageSource>();
            System.Windows.MessageBox.Show(
                "CastoPet 无法加载内置角色图片 Castorice.png。",
                "CastoPet",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        _idleFrameTimer = new DispatcherTimer();
        _idleFrameTimer.Tick += OnIdleFrameTick;
        _blinkScheduleTimer = new DispatcherTimer();
        _blinkScheduleTimer.Tick += OnBlinkScheduleTick;
        _blinkFrameTimer = new DispatcherTimer();
        _blinkFrameTimer.Tick += OnBlinkFrameTick;

        Loaded += OnLoaded;
        Closed += OnClosed;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseRightButtonUp += OnMouseRightButtonUp;
        MouseMove += OnMouseMove;
        LostMouseCapture += OnLostMouseCapture;
    }

    private ImageSource? CurrentIdleFrame => _idleFrames.Count > 0
        ? _idleFrames[_idleFrameIndex % _idleFrames.Count]
        : _defaultCharacter;

    public void ApplySettings(AppSettings settings)
    {
        var snapshot = PetWindowSettingsSnapshot.FromSettings(settings);
        Topmost = snapshot.Topmost;
        ShowInTaskbar = snapshot.ShowInTaskbar;
        _isClickThrough = snapshot.ClickThrough;

        if (new WindowInteropHelper(this).Handle == IntPtr.Zero)
        {
            _pendingSettings = snapshot;
            if (!_applySettingsOnSourceInitialized)
            {
                _applySettingsOnSourceInitialized = true;
                SourceInitialized += ApplyPendingSettings;
            }

            return;
        }

        ClickThroughService.Apply(this, snapshot.ClickThrough, snapshot.ShowInTaskbar);
    }

    public void AttachContextMenu(MenuCommandService commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        DetachContextMenuSubscriptions();

        var menu = new WpfControls.ContextMenu();
        var directSettings = SettingCatalog.Create(commands)
            .Where(definition => definition.ShowInDirectMenu)
            .ToArray();
        menu.Items.Add(CreateMenuItem(TrayService.ShowOrRestoreText, commands.ShowOrRestore));
        menu.Items.Add(new WpfControls.Separator());
        foreach (var definition in directSettings)
        {
            menu.Items.Add(CreateCheckedMenuItem(definition));
        }

        menu.Items.Add(new WpfControls.Separator());
        menu.Items.Add(CreateMenuItem(TrayService.OpenCrashReportsText, commands.OpenCrashReports));
        menu.Items.Add(CreateMenuItem(TrayService.CheckForUpdatesText, commands.CheckForUpdates));
        menu.Items.Add(new WpfControls.Separator());
        menu.Items.Add(CreateMenuItem(TrayService.ExitText, commands.Exit));
        menu.Opened += (_, _) => RefreshContextMenuChecks(menu);

        _petContextMenu = menu;
        _menuCommands = commands;
        _menuSettingsChangedHandler = () => RefreshContextMenuChecks(menu);
        commands.SettingsChanged += _menuSettingsChangedHandler;
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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        WindowPlacementService.MoveToBottomRight(this);
        StartIdleAnimation();
        ScheduleNextBlink();
    }

    private void OnClosed(object? sender, EventArgs e) => ShutdownRuntimeResources();

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isClickThrough || e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        _leftPressPosition = e.GetPosition(this);
        _leftPointerPending = CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_leftPointerPending || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var position = e.GetPosition(this);
        var thresholdX = Math.Max(MinimumDragThreshold, SystemParameters.MinimumHorizontalDragDistance);
        var thresholdY = Math.Max(MinimumDragThreshold, SystemParameters.MinimumVerticalDragDistance);
        if (Math.Abs(position.X - _leftPressPosition.X) < thresholdX &&
            Math.Abs(position.Y - _leftPressPosition.Y) < thresholdY)
        {
            return;
        }

        BeginWindowDrag();
        e.Handled = true;
    }

    private void BeginWindowDrag()
    {
        _leftPointerPending = false;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        _isDragging = true;
        StopPassiveAnimations();
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
            _isDragging = false;
            CharacterImage.Source = CurrentIdleFrame;
            StartIdleAnimation();
            ScheduleNextBlink();
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        CancelPendingLeftPointer();
        e.Handled = true;
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isClickThrough && _petContextMenu is not null)
        {
            _petContextMenu.PlacementTarget = this;
            _petContextMenu.Placement = WpfControls.Primitives.PlacementMode.MousePoint;
            _petContextMenu.IsOpen = true;
            e.Handled = true;
        }
    }

    private void OnLostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging)
        {
            _leftPointerPending = false;
        }
    }

    private void CancelPendingLeftPointer()
    {
        _leftPointerPending = false;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }
    }

    private void StartIdleAnimation()
    {
        if (_isDragging || _isBlinking || _idleFrames.Count == 0)
        {
            return;
        }

        _idleFrameTimer.Interval = PetFrameTiming.GetDuration(_idleAction, _idleFrameIndex, DefaultIdleFrameInterval);
        _idleFrameTimer.Start();
    }

    private void OnIdleFrameTick(object? sender, EventArgs e)
    {
        if (_isDragging || _isBlinking || _idleFrames.Count == 0)
        {
            _idleFrameTimer.Stop();
            return;
        }

        _idleFrameIndex = (_idleFrameIndex + 1) % _idleFrames.Count;
        CharacterImage.Source = CurrentIdleFrame;
        _idleFrameTimer.Interval = PetFrameTiming.GetDuration(_idleAction, _idleFrameIndex, DefaultIdleFrameInterval);
    }

    private void ScheduleNextBlink()
    {
        _blinkScheduleTimer.Stop();
        if (_isDragging || _blinkFrames.Count == 0)
        {
            return;
        }

        var minMs = (int)DefaultBlinkMinScheduleDelay.TotalMilliseconds;
        var maxMs = (int)DefaultBlinkMaxScheduleDelay.TotalMilliseconds;
        _blinkScheduleTimer.Interval = TimeSpan.FromMilliseconds(_blinkRandom.Next(minMs, maxMs + 1));
        _blinkScheduleTimer.Start();
    }

    private void OnBlinkScheduleTick(object? sender, EventArgs e)
    {
        _blinkScheduleTimer.Stop();
        if (_isDragging || _blinkFrames.Count == 0)
        {
            ScheduleNextBlink();
            return;
        }

        _isBlinking = true;
        _blinkFrameIndex = 0;
        _idleFrameTimer.Stop();
        CharacterImage.Source = _blinkFrames[0];
        _blinkFrameTimer.Interval = PetFrameTiming.GetDuration(_blinkAction, 0, DefaultBlinkFrameInterval);
        _blinkFrameTimer.Start();
    }

    private void OnBlinkFrameTick(object? sender, EventArgs e)
    {
        if (_isDragging || !_isBlinking)
        {
            StopBlinkAnimation();
            return;
        }

        _blinkFrameIndex++;
        if (_blinkFrameIndex >= _blinkFrames.Count)
        {
            StopBlinkAnimation();
            CharacterImage.Source = CurrentIdleFrame;
            StartIdleAnimation();
            ScheduleNextBlink();
            return;
        }

        CharacterImage.Source = _blinkFrames[_blinkFrameIndex];
        _blinkFrameTimer.Interval = PetFrameTiming.GetDuration(_blinkAction, _blinkFrameIndex, DefaultBlinkFrameInterval);
    }

    private void StopBlinkAnimation()
    {
        _blinkFrameTimer.Stop();
        _isBlinking = false;
        _blinkFrameIndex = 0;
    }

    private void StopPassiveAnimations()
    {
        _idleFrameTimer.Stop();
        _blinkScheduleTimer.Stop();
        StopBlinkAnimation();
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

    private void ShutdownRuntimeResources()
    {
        if (_runtimeResourcesReleased)
        {
            return;
        }

        _runtimeResourcesReleased = true;
        CancelPendingLeftPointer();
        StopPassiveAnimations();
        CharacterImage.Source = null;
        DetachContextMenuSubscriptions();
        if (_applySettingsOnSourceInitialized)
        {
            SourceInitialized -= ApplyPendingSettings;
            _applySettingsOnSourceInitialized = false;
        }
    }

    private void DetachContextMenuSubscriptions()
    {
        if (_menuCommands is not null && _menuSettingsChangedHandler is not null)
        {
            _menuCommands.SettingsChanged -= _menuSettingsChangedHandler;
        }

        _menuCommands = null;
        _menuSettingsChangedHandler = null;
        if (_petContextMenu is not null)
        {
            _petContextMenu.IsOpen = false;
            _petContextMenu.PlacementTarget = null;
            _petContextMenu = null;
        }
    }

    private static WpfControls.MenuItem CreateMenuItem(string header, Action action)
    {
        var item = new WpfControls.MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private static WpfControls.MenuItem CreateCheckedMenuItem(SettingDefinition definition)
    {
        var item = new WpfControls.MenuItem
        {
            Header = definition.Label,
            IsCheckable = true,
            IsChecked = definition.GetValue(),
            Tag = definition,
        };
        item.Click += (_, _) => definition.Toggle();
        return item;
    }

    private static void RefreshContextMenuChecks(WpfControls.ContextMenu menu)
    {
        foreach (var item in menu.Items.OfType<WpfControls.MenuItem>())
        {
            if (item.Tag is SettingDefinition definition)
            {
                item.IsChecked = definition.GetValue();
            }
        }
    }
}
