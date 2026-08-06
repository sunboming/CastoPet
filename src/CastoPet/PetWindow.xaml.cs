using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using CastoPet.Core;
using WpfControls = System.Windows.Controls;
using WpfInput = System.Windows.Input;
using WpfAnimation = System.Windows.Media.Animation;
using WpfColor = System.Windows.Media.Color;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfPoint = System.Windows.Point;
using WpfShapes = System.Windows.Shapes;
using WpfSize = System.Windows.Size;

namespace CastoPet;

public partial class PetWindow : Window
{
    private static readonly TimeSpan DefaultIdleFrameInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan DefaultBlinkFrameInterval = TimeSpan.FromMilliseconds(90);
    private static readonly TimeSpan DefaultPettingFrameInterval = TimeSpan.FromMilliseconds(80);
    private static readonly TimeSpan DefaultTurnFrameInterval = TimeSpan.FromMilliseconds(66.66666666666667);
    private static readonly TimeSpan DefaultExpressionTransitionFrameInterval = TimeSpan.FromMilliseconds(55);
    private static readonly TimeSpan DefaultBlinkMinScheduleDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DefaultBlinkMaxScheduleDelay = TimeSpan.FromSeconds(7);
    private static readonly TimeSpan RadialWheelPointerProbeInterval = TimeSpan.FromMilliseconds(16);
    private static readonly TimeSpan RadialWheelHoldRevealDelay = TimeSpan.FromMilliseconds(120);
    private static readonly TimeSpan PettingFallbackDuration = TimeSpan.FromMilliseconds(240);
    private const double MinimumLeftDragThreshold = 6;
    private const double RightWheelDragThreshold = 14;
    private const double HoldIndicatorSize = 58;
    private const double HoldIndicatorRadius = 22;

    private readonly LoggingService _logger;
    private readonly PetRuntimeState _runtimeState = new();
    private readonly PetAnimationController _animationController = new();
    private readonly PetMovementController _movementController;
    private readonly PetDirectionalMovementAnimator _directionalMovementAnimator = new();
    private readonly ImageSource _defaultCharacter;
    private readonly ImageSource _draggingCharacter;
    private readonly IReadOnlyList<ImageSource> _idleFrames;
    private readonly IReadOnlyList<ImageSource> _blinkFrames;
    private readonly IReadOnlyList<ImageSource> _pettingFrames;
    private readonly DispatcherTimer _idleFrameTimer;
    private readonly DispatcherTimer _blinkScheduleTimer;
    private readonly DispatcherTimer _blinkFrameTimer;
    private readonly DispatcherTimer _pettingFrameTimer;
    private readonly DispatcherTimer _dragRestoreTimer;
    private readonly DispatcherTimer _radialWheelPointerProbeTimer;
    private readonly DispatcherTimer _temporaryExpressionTimer;
    private readonly DispatcherTimer _expressionTransitionFrameTimer;
    private readonly DispatcherTimer _activeMovementProbeTimer;
    private readonly DispatcherTimer _turnFrameTimer;
    private readonly IReadOnlyList<ImageSource> _expressionTransitionInFrames;
    private readonly IReadOnlyList<ImageSource> _expressionTransitionOutFrames;
    private readonly IReadOnlyList<ImageSource> _moveFrames;
    private readonly IReadOnlyList<ImageSource> _moveLeftFrames;
    private readonly IReadOnlyList<ImageSource> _moveRightFrames;
    private readonly IReadOnlyList<ImageSource> _turnLeftFrames;
    private readonly IReadOnlyList<ImageSource> _turnRightFrames;
    private readonly IReadOnlyDictionary<string, PetExpressionAsset> _expressionAssetsById;
    private readonly WheelCatalogService _wheelCatalogService;
    private readonly PetInteractionCoordinator _interactions;
    private readonly ShortcutService _shortcutService;
    private readonly ShortcutDropHandler _shortcutDrops;
    private readonly ShortcutLauncher _shortcutLauncher;
    private readonly ImageSource? _inputReactiveBase;
    private readonly PetActionDefinition _idleAction;
    private readonly PetActionDefinition _moveAction;
    private readonly PetActionDefinition? _turnLeftAction;
    private readonly PetActionDefinition? _turnRightAction;
    private readonly PetActionDefinition _blinkAction;
    private readonly PetActionDefinition? _pettingAction;
    private readonly PetActionDefinition? _expressionTransitionInAction;
    private readonly PetActionDefinition? _expressionTransitionOutAction;
    private readonly InputReactiveState _inputReactiveState = new();
    private readonly WindowsInputHookService _inputHookService = new();
    private readonly DispatcherTimer _inputReactiveRenderTimer;
    private readonly WindowsCursorService _cursorService = new();
    private readonly List<RadialWheelItemVisual> _firstRingVisuals = new();
    private readonly List<RadialWheelItemVisual> _secondRingVisuals = new();
    private readonly Dictionary<string, ImageSource?> _shortcutIconCache = new(StringComparer.Ordinal);
    private readonly Random _blinkRandom = new();
    private PetWindowSettingsSnapshot? _pendingSettings;
    private WpfPoint _requestedRadialWheelOrigin;
    private WpfPoint _radialWheelOrigin;
    private WpfPoint _lastRadialWheelPointer;
    private bool _applySettingsOnSourceInitialized;
    private bool _isClickThrough;
    private bool _isDragging;
    private bool _hasRadialWheelPointer;
    private bool _hasRadialWheelPointerEntered;
    private bool _activeMovementEnabled;
    private bool _pushCursorEnabled;
    private bool _inputReactiveModeEnabled;
    private string _secondRingContentKey = "closed";
    private PetExpressionAsset? _pendingExpressionAsset;
    private PetExpressionAsset? _activeExpressionAsset;
    private IReadOnlyList<ImageSource> _activeExpressionTransitionFrames = Array.Empty<ImageSource>();
    private bool _activeExpressionUsesSpecificTransition;
    private TimeSpan? _lastManualCursorMovementTime;
    private TimeSpan? _cursorPushStartedAt;
    private int _activeMovementVisualDirection;
    private bool _dragMovementVisualApplied;
    private double? _expectedCursorX;
    private double? _expectedCursorY;
    private bool _activeMovementRenderingSubscribed;
    private bool _radialWheelHoldRenderingSubscribed;
    private bool _runtimeResourcesReleased;
    private WpfControls.ContextMenu? _petContextMenu;
    private MenuCommandService? _menuCommands;
    private Action? _menuSettingsChangedHandler;
    private WpfPoint _requestedRadialWheelOriginDevice;

    private sealed class RadialWheelItemVisual(
        WpfShapes.Path sector,
        WpfControls.TextBlock label,
        FrameworkElement content,
        bool isEnabled,
        RadialWheelRing ring)
    {
        public WpfShapes.Path Sector { get; } = sector;
        public WpfControls.TextBlock Label { get; } = label;
        public FrameworkElement Content { get; } = content;
        public bool IsEnabled { get; } = isEnabled;
        public RadialWheelRing Ring { get; } = ring;
        public bool IsSelected { get; set; }
    }

    private sealed record LegacyWheelDependencies(
        WheelCatalogService CatalogService,
        ShortcutService Shortcuts,
        ShortcutDropHandler Drops,
        ShortcutLauncher Launcher);

    public PetWindow(AssetService assets, LoggingService logger)
        : this(assets, logger, CreateLegacyWheelDependencies(assets, logger))
    {
    }

    private PetWindow(
        AssetService assets,
        LoggingService logger,
        LegacyWheelDependencies dependencies)
        : this(
            assets,
            logger,
            dependencies.CatalogService,
            dependencies.Shortcuts,
            dependencies.Drops,
            dependencies.Launcher)
    {
    }

    public PetWindow(
        AssetService assets,
        LoggingService logger,
        WheelCatalogService wheelCatalogService,
        ShortcutService shortcutService,
        ShortcutDropHandler shortcutDrops,
        ShortcutLauncher shortcutLauncher)
    {
        InitializeComponent();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wheelCatalogService = wheelCatalogService ?? throw new ArgumentNullException(nameof(wheelCatalogService));
        _shortcutService = shortcutService ?? throw new ArgumentNullException(nameof(shortcutService));
        _shortcutDrops = shortcutDrops ?? throw new ArgumentNullException(nameof(shortcutDrops));
        _shortcutLauncher = shortcutLauncher ?? throw new ArgumentNullException(nameof(shortcutLauncher));
        _idleAction = assets.Skin.GetRequiredAction(PetActionKind.Idle);
        _moveAction = assets.Skin.GetRequiredAction(PetActionKind.Move);
        _movementController = new PetMovementController(_moveAction);
        _turnLeftAction = assets.Skin.TryGetAction(PetActionKind.TurnLeft, out var turnLeftAction)
            ? turnLeftAction
            : null;
        _turnRightAction = assets.Skin.TryGetAction(PetActionKind.TurnRight, out var turnRightAction)
            ? turnRightAction
            : null;
        _blinkAction = assets.Skin.GetRequiredAction(PetActionKind.Blink);
        _pettingAction = assets.Skin.TryGetAction(PetActionKind.Petting, out var pettingAction)
            ? pettingAction
            : null;
        _expressionTransitionInAction = assets.Skin.TryGetAction(PetActionKind.ExpressionTransitionIn, out var transitionInAction)
            ? transitionInAction
            : null;
        _expressionTransitionOutAction = assets.Skin.TryGetAction(PetActionKind.ExpressionTransitionOut, out var transitionOutAction)
            ? transitionOutAction
            : null;
        _idleFrameTimer = new DispatcherTimer { Interval = PetFrameTiming.GetDuration(_idleAction, 0, DefaultIdleFrameInterval) };
        _idleFrameTimer.Tick += (_, _) => AdvanceIdleFrame();
        _blinkScheduleTimer = new DispatcherTimer();
        _blinkScheduleTimer.Tick += (_, _) => BeginBlink();
        _blinkFrameTimer = new DispatcherTimer { Interval = PetFrameTiming.GetDuration(_blinkAction, 0, DefaultBlinkFrameInterval) };
        _blinkFrameTimer.Tick += (_, _) => AdvanceBlinkFrame();
        _pettingFrameTimer = new DispatcherTimer { Interval = PetFrameTiming.GetDuration(_pettingAction, 0, DefaultPettingFrameInterval) };
        _pettingFrameTimer.Tick += (_, _) => AdvancePettingFrame();
        _dragRestoreTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _dragRestoreTimer.Tick += (_, _) => RestoreAfterDrag();
        _radialWheelPointerProbeTimer = new DispatcherTimer { Interval = RadialWheelPointerProbeInterval };
        _radialWheelPointerProbeTimer.Tick += (_, _) => ProbeRadialWheelPointer();
        _temporaryExpressionTimer = new DispatcherTimer { Interval = ExpressionWheelCatalog.ExpressionDuration };
        _temporaryExpressionTimer.Tick += (_, _) => RestoreAfterTemporaryExpression();
        _expressionTransitionFrameTimer = new DispatcherTimer { Interval = PetFrameTiming.GetDuration(_expressionTransitionInAction, 0, DefaultExpressionTransitionFrameInterval) };
        _expressionTransitionFrameTimer.Tick += (_, _) => AdvanceExpressionTransitionFrame();
        _activeMovementProbeTimer = new DispatcherTimer { Interval = PetAnimationTimings.ActiveMovementProbeInterval };
        _activeMovementProbeTimer.Tick += (_, _) => ProbeActiveMovement();
        _turnFrameTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = DefaultTurnFrameInterval };
        _turnFrameTimer.Tick += (_, _) => AdvanceTurnFrame();
        _inputReactiveRenderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _inputReactiveRenderTimer.Tick += (_, _) => RenderInputReactiveHighlights();
        _inputHookService.InputReceived += OnInputReactiveInputReceived;
        _interactions = new PetInteractionCoordinator(
            _wheelCatalogService.Current,
            new PetPointerGestureClassifier(
                Math.Max(MinimumLeftDragThreshold, SystemParameters.MinimumHorizontalDragDistance),
                Math.Max(MinimumLeftDragThreshold, SystemParameters.MinimumVerticalDragDistance),
                RightWheelDragThreshold,
                WheelCatalog.HoldDelay));

        try
        {
            _defaultCharacter = assets.LoadDefaultCharacter();
            _draggingCharacter = assets.LoadDraggingCharacter();
            _idleFrames = assets.LoadIdleFrames();
            _blinkFrames = assets.LoadBlinkFrames();
            _pettingFrames = assets.LoadPettingFrames();
            _moveFrames = assets.LoadMoveFrames();
            var moveLeftFrames = assets.LoadMoveLeftFrames();
            var moveRightFrames = assets.LoadMoveRightFrames();
            _moveLeftFrames = moveLeftFrames.Count > 0 ? moveLeftFrames : _moveFrames;
            _moveRightFrames = moveRightFrames.Count > 0 ? moveRightFrames : _moveFrames;
            _turnLeftFrames = assets.LoadTurnLeftFrames();
            _turnRightFrames = assets.LoadTurnRightFrames();
            _inputReactiveBase = assets.TryLoadInputReactiveBase();
            _expressionTransitionInFrames = assets.LoadExpressionTransitionInFrames();
            _expressionTransitionOutFrames = assets.LoadExpressionTransitionOutFrames();
            _expressionAssetsById = assets.LoadExpressionAssets();
            CharacterImage.Source = GetCurrentIdleFrame();
        }
        catch
        {
            _defaultCharacter = CharacterImage.Source;
            _draggingCharacter = CharacterImage.Source;
            _idleFrames = Array.Empty<ImageSource>();
            _blinkFrames = Array.Empty<ImageSource>();
            _pettingFrames = Array.Empty<ImageSource>();
            _moveFrames = Array.Empty<ImageSource>();
            _moveLeftFrames = Array.Empty<ImageSource>();
            _moveRightFrames = Array.Empty<ImageSource>();
            _turnLeftFrames = Array.Empty<ImageSource>();
            _turnRightFrames = Array.Empty<ImageSource>();
            _inputReactiveBase = null;
            _expressionTransitionInFrames = Array.Empty<ImageSource>();
            _expressionTransitionOutFrames = Array.Empty<ImageSource>();
            _expressionAssetsById = new Dictionary<string, PetExpressionAsset>(StringComparer.OrdinalIgnoreCase);
            System.Windows.MessageBox.Show(
                "CastoPet 无法加载内置角色图片 Castorice.png。",
                "CastoPet",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        _wheelCatalogService.Changed += OnWheelCatalogChanged;
        BuildFirstRadialWheelRing();

        Loaded += (_, _) =>
        {
            WindowPlacementService.MoveToBottomRight(this);
            StartIdleAnimation();
            ScheduleNextBlink();
            UpdateInputReactiveMode();
            UpdateActiveMovementTimer();
        };
        IsVisibleChanged += (_, _) =>
        {
            UpdateInputReactiveMode();
            UpdateActiveMovementTimer();
        };
        Closed += (_, _) => ShutdownRuntimeResources();
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseRightButtonDown += OnMouseRightButtonDown;
        MouseRightButtonUp += OnMouseRightButtonUp;
        MouseMove += OnMouseMove;
        LostMouseCapture += OnLostMouseCapture;
        Deactivated += OnWindowDeactivated;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void ShutdownRuntimeResources()
    {
        if (_runtimeResourcesReleased)
        {
            return;
        }

        _runtimeResourcesReleased = true;
        _wheelCatalogService.Changed -= OnWheelCatalogChanged;
        DetachContextMenuSubscriptions();
        if (_applySettingsOnSourceInitialized)
        {
            SourceInitialized -= ApplyPendingSettings;
            _applySettingsOnSourceInitialized = false;
        }

        StopInputReactiveMode(restoreIdle: false);
        CancelPendingPointerGesture();
        StopPetting(restoreIdle: false);
        CancelTemporaryExpression();
        CloseRadialWheel(cancelController: true, restoreAnimation: false);
        StopActiveMovementRendering();
        StopRadialWheelHoldRendering();

        _idleFrameTimer.Stop();
        _blinkScheduleTimer.Stop();
        _blinkFrameTimer.Stop();
        _pettingFrameTimer.Stop();
        _dragRestoreTimer.Stop();
        _radialWheelPointerProbeTimer.Stop();
        _temporaryExpressionTimer.Stop();
        _expressionTransitionFrameTimer.Stop();
        _activeMovementProbeTimer.Stop();
        _turnFrameTimer.Stop();
        _inputReactiveRenderTimer.Stop();

        RadialWheelOverlay.IsOpen = false;
        RadialWheelHoldOverlay.IsOpen = false;
        _inputHookService.InputReceived -= OnInputReactiveInputReceived;
        _inputHookService.Dispose();
        _shortcutIconCache.Clear();
        _firstRingVisuals.Clear();
        _secondRingVisuals.Clear();
        FirstRingSurface.Children.Clear();
        SecondRingSurface.Children.Clear();
        InputReactiveOverlay.Children.Clear();
        CharacterImage.Source = null;
    }

    private static LegacyWheelDependencies CreateLegacyWheelDependencies(
        AssetService assets,
        LoggingService logger)
    {
        var shortcutService = new ShortcutService(new AppPaths(), logger);
        shortcutService.Load();
        var catalogService = new WheelCatalogService(assets.Skin.Expressions, shortcutService);
        return new LegacyWheelDependencies(
            catalogService,
            shortcutService,
            new ShortcutDropHandler(shortcutService),
            new ShortcutLauncher(logger));
    }

    public void ApplySettings(AppSettings settings)
    {
        var snapshot = PetWindowSettingsSnapshot.FromSettings(settings);
        Topmost = snapshot.Topmost;
        ShowInTaskbar = snapshot.ShowInTaskbar;
        _isClickThrough = snapshot.ClickThrough;
        _activeMovementEnabled = snapshot.ActiveMovement;
        _pushCursorEnabled = snapshot.PushCursor;
        _inputReactiveModeEnabled = snapshot.InputReactiveMode;
        UpdateInputReactiveMode();
        UpdateActiveMovementTimer();

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
        menu.Items.Add(CreateMenuItem(TrayService.SettingsText, commands.ShowSettings));
        menu.Items.Add(new WpfControls.Separator());
        menu.Items.Add(CreateMenuItem(TrayService.ExitText, commands.Exit));

        menu.Opened += (_, _) => RefreshContextMenuChecks(menu);
        _petContextMenu = menu;
        _menuCommands = commands;
        _menuSettingsChangedHandler = () => RefreshContextMenuChecks(menu);
        commands.SettingsChanged += _menuSettingsChangedHandler;
    }

    private void DetachContextMenuSubscriptions()
    {
        if (_menuCommands is not null && _menuSettingsChangedHandler is not null)
        {
            _menuCommands.SettingsChanged -= _menuSettingsChangedHandler;
        }

        _menuCommands = null;
        _menuSettingsChangedHandler = null;
        if (_petContextMenu is null)
        {
            return;
        }

        _petContextMenu.IsOpen = false;
        _petContextMenu.PlacementTarget = null;
        _petContextMenu = null;
    }

    private void ShowPetContextMenu()
    {
        if (_petContextMenu is null)
        {
            return;
        }

        _petContextMenu.PlacementTarget = this;
        _petContextMenu.Placement = WpfControls.Primitives.PlacementMode.MousePoint;
        _petContextMenu.IsOpen = true;
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

    private bool CanShowInputReactiveMode()
    {
        return _inputReactiveModeEnabled
            && _inputReactiveBase is not null
            && IsVisible
            && !_isDragging
            && !_animationController.IsPetting
            && !_dragRestoreTimer.IsEnabled
            && !_interactions.IsRadialWheelOpen
            && !_temporaryExpressionTimer.IsEnabled
            && _animationController.ExpressionTransitionMode == PetExpressionTransitionMode.None;
    }

    private bool IsInputReactiveModeBlockingPassiveAnimation()
    {
        return _inputReactiveModeEnabled && _inputReactiveBase is not null;
    }

    private PetPassiveAnimationContext GetPassiveAnimationContext()
    {
        return new PetPassiveAnimationContext(
            PassiveAnimationAllowed: InputReactiveModePolicy.AllowsPassiveAnimation(
                IsInputReactiveModeBlockingPassiveAnimation()),
            IsDragging: _isDragging,
            HasActiveMovementTarget: _movementController.HasTarget,
            IsRadialWheelOpen: _interactions.IsRadialWheelOpen,
            HasTemporaryExpression: _temporaryExpressionTimer.IsEnabled);
    }

    private void UpdateInputReactiveMode()
    {
        if (!CanShowInputReactiveMode())
        {
            StopInputReactiveMode(restoreIdle: !_inputReactiveModeEnabled || _inputReactiveBase is null);
            return;
        }

        StopActiveMovementProbe();
        StopActiveMovementRendering();
        _movementController.CancelTarget();
        StopIdleAnimation();
        StopBlinkAnimation();
        ResetMoveFrameState();
        ResetActiveMovementVisual();

        CharacterImage.Source = _inputReactiveBase;
        InputReactiveOverlay.Visibility = Visibility.Visible;
        if (!_inputReactiveRenderTimer.IsEnabled)
        {
            _inputReactiveRenderTimer.Start();
        }

        _inputHookService.Start();
        RenderInputReactiveHighlights();
    }

    private void StopInputReactiveMode(bool restoreIdle = true)
    {
        _inputHookService.Stop();
        _inputReactiveRenderTimer.Stop();
        _inputReactiveState.Clear();
        InputReactiveOverlay.Children.Clear();
        InputReactiveOverlay.Visibility = Visibility.Collapsed;

        if (restoreIdle
            && !_isDragging
            && !_animationController.IsPetting
            && !_interactions.IsRadialWheelOpen
            && !_temporaryExpressionTimer.IsEnabled
            && _animationController.ExpressionTransitionMode == PetExpressionTransitionMode.None)
        {
            CharacterImage.Source = GetCurrentIdleFrame();
            StartIdleAnimation();
            ScheduleNextBlink();
        }
    }

    private void OnInputReactiveInputReceived(InputReactiveEvent inputEvent)
    {
        if (!CanShowInputReactiveMode())
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (!CanShowInputReactiveMode())
            {
                return;
            }

            _inputReactiveState.AddKey(inputEvent.Id, GetInputReactiveTime());
            RenderInputReactiveHighlights();
        });
    }

    private void RenderInputReactiveHighlights()
    {
        if (!CanShowInputReactiveMode())
        {
            InputReactiveOverlay.Children.Clear();
            return;
        }

        CharacterImage.Source = _inputReactiveBase;
        InputReactiveOverlay.Children.Clear();
        foreach (var key in InputKeyboardLayout.KeyIds)
        {
            if (InputKeyboardLayout.TryGetKeyBounds(key, out var bounds))
            {
                AddInputReactiveKeyVisual(key, bounds, isActive: false);
            }
        }

        foreach (var key in _inputReactiveState.GetActiveHighlights(GetInputReactiveTime()))
        {
            if (!InputKeyboardLayout.TryGetKeyBounds(key, out var bounds))
            {
                continue;
            }

            AddInputReactiveKeyVisual(key, bounds, isActive: true);
        }
    }

    private void AddInputReactiveKeyVisual(string key, System.Drawing.RectangleF bounds, bool isActive)
    {
        var rectangle = new WpfShapes.Rectangle
        {
            Width = bounds.Width,
            Height = bounds.Height,
            RadiusX = 5,
            RadiusY = 5,
            Fill = new SolidColorBrush(isActive
                ? WpfColor.FromArgb(190, 248, 238, 255)
                : WpfColor.FromArgb(92, 244, 236, 255)),
            Stroke = new SolidColorBrush(isActive
                ? WpfColor.FromArgb(230, 119, 78, 190)
                : WpfColor.FromArgb(132, 119, 78, 190)),
            StrokeThickness = isActive ? 1.4 : 0.8,
            IsHitTestVisible = false,
        };
        WpfControls.Canvas.SetLeft(rectangle, bounds.X);
        WpfControls.Canvas.SetTop(rectangle, bounds.Y);
        InputReactiveOverlay.Children.Add(rectangle);

        var label = new WpfControls.TextBlock
        {
            Text = InputKeyboardLayout.GetDisplayLabel(key),
            Width = bounds.Width,
            Height = bounds.Height,
            FontSize = bounds.Width > 34 ? 8 : 9,
            FontWeight = isActive ? FontWeights.Bold : FontWeights.SemiBold,
            Foreground = new SolidColorBrush(isActive
                ? WpfColor.FromArgb(255, 70, 38, 114)
                : WpfColor.FromArgb(210, 80, 48, 128)),
            TextAlignment = TextAlignment.Center,
            IsHitTestVisible = false,
        };
        WpfControls.Canvas.SetLeft(label, bounds.X);
        WpfControls.Canvas.SetTop(label, bounds.Y + 1);
        InputReactiveOverlay.Children.Add(label);
    }

    private static TimeSpan GetInputReactiveTime()
    {
        return TimeSpan.FromMilliseconds(Environment.TickCount64);
    }

    private void OnMouseLeftButtonDown(object sender, WpfInput.MouseButtonEventArgs e)
    {
        if (_isClickThrough || e.ButtonState != WpfInput.MouseButtonState.Pressed)
        {
            return;
        }

        var position = e.GetPosition(RootGrid);
        _interactions.PressPointer(PetPointerButton.Left, position.X, position.Y, DateTimeOffset.UtcNow);
        if (_interactions.PointerState != PetPointerGestureState.LeftPending)
        {
            CancelPendingPointerGesture();
            e.Handled = true;
            return;
        }

        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseLeftButtonUp(object sender, WpfInput.MouseButtonEventArgs e)
    {
        var position = e.GetPosition(RootGrid);
        var intent = _interactions.ReleasePointer(
            PetPointerButton.Left,
            position.X,
            position.Y,
            DateTimeOffset.UtcNow);
        ReleasePendingMouseCapture();
        if (intent == PetPointerIntent.Petting)
        {
            BeginPetting();
        }

        e.Handled = true;
    }

    private void BeginDragFromGesture()
    {
        ReleasePendingMouseCapture();
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
            _interactions.CancelPointerGesture();
        }
    }

    private void OnMouseRightButtonDown(object sender, WpfInput.MouseButtonEventArgs e)
    {
        if (_isClickThrough || e.ButtonState != WpfInput.MouseButtonState.Pressed)
        {
            return;
        }

        if (_interactions.IsRadialWheelOpen)
        {
            e.Handled = true;
            return;
        }

        var position = e.GetPosition(RootGrid);
        _interactions.PressPointer(PetPointerButton.Right, position.X, position.Y, DateTimeOffset.UtcNow);
        if (_interactions.PointerState != PetPointerGestureState.RightPending)
        {
            CancelPendingPointerGesture();
            e.Handled = true;
            return;
        }

        _requestedRadialWheelOriginDevice = RootGrid.PointToScreen(position);
        _requestedRadialWheelOrigin = FromDevicePoint(_requestedRadialWheelOriginDevice);
        CaptureMouse();
        StopRadialWheelHoldRendering();
        if (_interactions.HasWheelCategories)
        {
            StartRadialWheelHoldRendering();
        }
        e.Handled = true;
    }

    private void OnMouseMove(object sender, WpfInput.MouseEventArgs e)
    {
        var position = e.GetPosition(RootGrid);
        if (_interactions.IsRadialWheelOpen)
        {
            UpdateRadialWheelPointer(position, DateTimeOffset.UtcNow);
            return;
        }

        if (_interactions.PointerState == PetPointerGestureState.LeftPending)
        {
            if (e.LeftButton != WpfInput.MouseButtonState.Pressed)
            {
                CancelPendingPointerGesture();
                return;
            }

            if (_interactions.MovePointer(position.X, position.Y, DateTimeOffset.UtcNow) == PetPointerIntent.Drag)
            {
                BeginDragFromGesture();
            }

            return;
        }

        if (_interactions.PointerState != PetPointerGestureState.RightPending)
        {
            return;
        }

        if (e.RightButton != WpfInput.MouseButtonState.Pressed)
        {
            CancelPendingPointerGesture();
            return;
        }

        if (!_interactions.HasWheelCategories)
        {
            return;
        }

        if (_interactions.MovePointer(position.X, position.Y, DateTimeOffset.UtcNow) == PetPointerIntent.RadialWheel)
        {
            OpenRadialWheel();
        }
    }

    private void OnMouseRightButtonUp(object sender, WpfInput.MouseButtonEventArgs e)
    {
        StopRadialWheelHoldRendering();
        HideRadialWheelHoldFeedback();

        if (!_interactions.IsRadialWheelOpen)
        {
            var position = e.GetPosition(RootGrid);
            var intent = _interactions.ReleasePointer(
                PetPointerButton.Right,
                position.X,
                position.Y,
                DateTimeOffset.UtcNow);
            ReleasePendingMouseCapture();
            if (intent == PetPointerIntent.ContextMenu)
            {
                ShowPetContextMenu();
            }

            e.Handled = true;
            return;
        }

        UpdateRadialWheelPointer(e.GetPosition(RootGrid), DateTimeOffset.UtcNow);
        e.Handled = true;
        if (!_interactions.IsRadialWheelOpen)
        {
            return;
        }

        var selection = RadialWheelSelector.GetSelection(
            _lastRadialWheelPointer.X,
            _lastRadialWheelPointer.Y,
            _interactions.Catalog.Categories.Count,
            _interactions.RadialWheel.VisibleSecondLevelItems.Count);
        var result = selection.Ring == RadialWheelRing.Second
            ? _interactions.RadialWheel.Release()
            : _interactions.RadialWheel.Cancel();
        _interactions.ReleasePointer(
            PetPointerButton.Right,
            e.GetPosition(RootGrid).X,
            e.GetPosition(RootGrid).Y,
            DateTimeOffset.UtcNow);

        if (result.Kind == WheelReleaseKind.PageChanged)
        {
            RefreshRadialWheelVisuals(forceSecondRingRebuild: true);
            return;
        }

        CloseRadialWheel(cancelController: false);
        if (result.Kind == WheelReleaseKind.Execute && result.Action is not null)
        {
            ExecuteWheelAction(result.Action);
        }
    }

    private void OnLostMouseCapture(object sender, WpfInput.MouseEventArgs e)
    {
        if (_interactions.PointerState is PetPointerGestureState.LeftPending or PetPointerGestureState.RightPending)
        {
            CancelPendingPointerGesture(releaseCapture: false);
        }
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (!_interactions.IsRadialWheelOpen)
        {
            CancelPendingPointerGesture();
        }
    }

    private void OnPreviewKeyDown(object sender, WpfInput.KeyEventArgs e)
    {
        if (!_interactions.IsRadialWheelOpen || e.Key != WpfInput.Key.Escape)
        {
            return;
        }

        _interactions.RadialWheel.Cancel();
        CloseRadialWheel(cancelController: false);
        e.Handled = true;
    }

    private void BeginDrag()
    {
        StopPetting(restoreIdle: false);
        CancelTemporaryExpression();
        StopInputReactiveMode(restoreIdle: false);
        StopActiveMovementRendering();
        _movementController.CancelTarget();
        CancelDirectionalMovement();
        _isDragging = true;
        _dragRestoreTimer.Stop();
        StopIdleAnimation();
        StopBlinkAnimation();
        ResetCharacterTransitionAnimations();
        ApplyDragMovementVisual();
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
        UpdateInputReactiveMode();
        UpdateActiveMovementTimer();
    }

    private void RestoreAfterDrag()
    {
        _dragRestoreTimer.Stop();
        _animationController.ResetIdle();
        ResetActiveMovementVisual();
        CharacterImage.Source = GetCurrentIdleFrame();
        StartIdleAnimation();
        ScheduleNextBlink();
        UpdateInputReactiveMode();
        UpdateActiveMovementTimer();
    }

    private void BeginPetting()
    {
        StopPetting(restoreIdle: false);
        _animationController.BeginPetting(Math.Max(1, _pettingFrames.Count));
        CancelTemporaryExpression();
        StopInputReactiveMode(restoreIdle: false);
        StopActiveMovementProbe();
        StopActiveMovementRendering();
        _movementController.CancelTarget();
        _dragRestoreTimer.Stop();
        StopIdleAnimation();
        StopBlinkAnimation();
        ResetMoveFrameState();
        ResetActiveMovementVisual();
        ResetCharacterTransitionAnimations();

        if (_pettingFrames.Count > 0)
        {
            CharacterImage.Source = _pettingFrames[0];
            _pettingFrameTimer.Interval = PetFrameTiming.GetDuration(
                _pettingAction,
                0,
                DefaultPettingFrameInterval);
            var totalDuration = PetFrameTiming.GetTotalDuration(
                _pettingAction,
                _pettingFrames.Count,
                DefaultPettingFrameInterval);
            AnimatePettingCompression(TimeSpan.FromTicks(totalDuration.Ticks / 2));
            _pettingFrameTimer.Start();
            return;
        }

        var halfDuration = TimeSpan.FromMilliseconds(PettingFallbackDuration.TotalMilliseconds / 2);
        AnimatePettingCompression(halfDuration);
        _pettingFrameTimer.Interval = PettingFallbackDuration;
        _pettingFrameTimer.Start();
    }

    private void AnimatePettingCompression(TimeSpan halfDuration)
    {
        var easing = new WpfAnimation.QuadraticEase { EasingMode = WpfAnimation.EasingMode.EaseInOut };
        CharacterScaleTransform.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            new WpfAnimation.DoubleAnimation(1, 1.015, new Duration(halfDuration))
            {
                AutoReverse = true,
                EasingFunction = easing,
            });
        CharacterScaleTransform.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            new WpfAnimation.DoubleAnimation(1, 0.985, new Duration(halfDuration))
            {
                AutoReverse = true,
                EasingFunction = easing,
            });
        CharacterTranslateTransform.BeginAnimation(
            TranslateTransform.YProperty,
            new WpfAnimation.DoubleAnimation(0, 2, new Duration(halfDuration))
            {
                AutoReverse = true,
                EasingFunction = easing,
            });
    }

    private void AdvancePettingFrame()
    {
        if (!_animationController.IsPetting)
        {
            _pettingFrameTimer.Stop();
            return;
        }

        if (_pettingFrames.Count == 0)
        {
            CompletePetting();
            return;
        }

        var advance = _animationController.AdvancePetting(_pettingFrames.Count);
        if (advance.Completed)
        {
            CompletePetting();
            return;
        }

        CharacterImage.Source = _pettingFrames[advance.FrameIndex];
        _pettingFrameTimer.Interval = PetFrameTiming.GetDuration(
            _pettingAction,
            advance.FrameIndex,
            DefaultPettingFrameInterval);
    }

    private void CompletePetting()
    {
        StopPetting(restoreIdle: true);
    }

    private void StopPetting(bool restoreIdle)
    {
        var wasActive = _animationController.IsPetting || _pettingFrameTimer.IsEnabled;
        _pettingFrameTimer.Stop();
        if (!wasActive)
        {
            return;
        }

        _animationController.StopPetting();
        CharacterTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
        ResetCharacterTransitionAnimations();
        CharacterTranslateTransform.Y = 0;
        if (!restoreIdle)
        {
            return;
        }

        _animationController.ResetIdle();
        CharacterImage.Source = GetCurrentIdleFrame();
        StartIdleAnimation();
        ScheduleNextBlink();
        UpdateInputReactiveMode();
        UpdateActiveMovementTimer();
    }

    private bool CanRunActiveMovement()
    {
        return _activeMovementEnabled
            && InputReactiveModePolicy.AllowsPassiveAnimation(IsInputReactiveModeBlockingPassiveAnimation())
            && IsVisible
            && !_isClickThrough
            && !_isDragging
            && !_animationController.IsPetting
            && !_dragRestoreTimer.IsEnabled
            && !_interactions.IsRadialWheelOpen
            && !_temporaryExpressionTimer.IsEnabled
            && _animationController.ExpressionTransitionMode == PetExpressionTransitionMode.None;
    }

    private void UpdateActiveMovementTimer()
    {
        if (CanRunActiveMovement())
        {
            StartActiveMovementProbe();
            return;
        }

        StopActiveMovementProbe();
        StopActiveMovementRendering();
        _movementController.CancelTarget();
        ResetMoveFrameState();
        ResetActiveMovementVisual();
    }

    private void StartActiveMovementProbe()
    {
        if (!_activeMovementProbeTimer.IsEnabled)
        {
            _activeMovementProbeTimer.Start();
        }

        ProbeActiveMovement();
    }

    private void StopActiveMovementProbe()
    {
        _activeMovementProbeTimer.Stop();
    }

    private void ProbeActiveMovement()
    {
        if (!CanRunActiveMovement())
        {
            UpdateActiveMovementTimer();
            return;
        }

        if (_activeMovementRenderingSubscribed)
        {
            return;
        }

        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var bounds = GetCurrentMovementBounds();
        var cursor = GetCursorScreenPosition();
        var petCenterX = Left + width / 2;
        var petCenterY = Top + height / 2;
        var cursorDistance = Math.Sqrt(Math.Pow(cursor.X - petCenterX, 2) + Math.Pow(cursor.Y - petCenterY, 2));

        if (cursorDistance <= PetMovementPlanner.MouseInterestRadius)
        {
            if (!PetMovementPlanner.IsAtMouseApproachTarget(Left, Top, width, height, cursor.X, cursor.Y, bounds))
            {
                StartActiveMovementRendering();
            }

            return;
        }

        if (_movementController.IsWanderDue(DateTime.UtcNow))
        {
            StartActiveMovementRendering();
        }
    }

    private void StartActiveMovementRendering()
    {
        if (_activeMovementRenderingSubscribed)
        {
            return;
        }

        _movementController.BeginRendering(Left, Top);
        CompositionTarget.Rendering += OnActiveMovementRendering;
        _activeMovementRenderingSubscribed = true;
    }

    private void StopActiveMovementRendering()
    {
        if (!_activeMovementRenderingSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering -= OnActiveMovementRendering;
        _activeMovementRenderingSubscribed = false;
        _movementController.StopRendering();
        _expectedCursorX = null;
        _expectedCursorY = null;
        _cursorPushStartedAt = null;
    }

    private void OnActiveMovementRendering(object? sender, EventArgs e)
    {
        if (e is not RenderingEventArgs renderingArgs)
        {
            return;
        }

        AdvanceActiveMovement(renderingArgs.RenderingTime);
    }

    private void AdvanceActiveMovement(TimeSpan renderingTime)
    {
        if (!CanRunActiveMovement())
        {
            UpdateActiveMovementTimer();
            return;
        }

        if (!_movementController.HasRenderingTime)
        {
            _movementController.Advance(renderingTime, Left, Top);
            return;
        }

        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var bounds = GetCurrentMovementBounds();
        var cursor = GetCursorScreenPosition();
        var petCenterX = Left + width / 2;
        var petCenterY = Top + height / 2;
        var cursorDistance = Math.Sqrt(Math.Pow(cursor.X - petCenterX, 2) + Math.Pow(cursor.Y - petCenterY, 2));

        if (cursorDistance <= PetMovementPlanner.MouseInterestRadius)
        {
            var mouseApproachTarget = PetMovementPlanner.CalculateMouseApproachTarget(
                Left,
                Top,
                width,
                height,
                cursor.X,
                cursor.Y,
                bounds);
            if (PetMovementPlanner.IsClose(Left, Top, mouseApproachTarget))
            {
                if (_movementController.HasTarget)
                {
                    _movementController.CompleteTarget(DateTime.UtcNow);
                    FinishDirectionalMovement();
                }

                StopActiveMovementRendering();
                return;
            }

            if (!_movementController.HasTarget)
            {
                StopIdleAnimation();
                StopBlinkAnimation();
            }

            _movementController.SetTarget(mouseApproachTarget);
        }
        else if (!_movementController.HasTarget ||
            PetMovementPlanner.IsClose(Left, Top, _movementController.Target))
        {
            ChooseWanderTarget(width, height, bounds);
        }

        if (!_movementController.HasTarget)
        {
            ResetActiveMovementVisual();
            BeginReturnToFront();
            StopActiveMovementRendering();
            return;
        }

        if (PetMovementPlanner.IsClose(Left, Top, _movementController.Target))
        {
            _movementController.CompleteTarget(DateTime.UtcNow);
            FinishDirectionalMovement();
            StopActiveMovementRendering();
            return;
        }

        var requestedDirection = ResolveMovementDirection(_movementController.Target.Left - Left);
        if (EnsureMovementFacing(requestedDirection))
        {
            return;
        }

        var movement = _movementController.Advance(renderingTime, Left, Top);
        if (movement is null)
        {
            return;
        }

        Left = Math.Round(movement.Value.NextLeft);
        Top = Math.Round(movement.Value.NextTop);
        _runtimeState.SetRuntimePosition(Left, Top);
        AdvanceMoveFrame(movement.Value.Distance, ResolveMovementDirection(movement.Value.DeltaX));
        ApplyActiveMovementVisual();
        TryPushCursor(renderingTime, movement.Value.DeltaX, movement.Value.DeltaY);

        if (PetMovementPlanner.IsClose(Left, Top, _movementController.Target))
        {
            _movementController.CompleteTarget(DateTime.UtcNow);
            FinishDirectionalMovement();
            StopActiveMovementRendering();
        }
    }

    private PetMovementBounds GetCurrentMovementBounds()
    {
        return new PetMovementBounds(
            SystemParameters.WorkArea.Left,
            SystemParameters.WorkArea.Top,
            SystemParameters.WorkArea.Width,
            SystemParameters.WorkArea.Height);
    }

    private WpfPoint GetCursorScreenPosition()
    {
        var cursor = _cursorService.GetPosition();
        var point = new WpfPoint(cursor.X, cursor.Y);
        var source = PresentationSource.FromVisual(this);

        return source?.CompositionTarget is null
            ? point
            : source.CompositionTarget.TransformFromDevice.Transform(point);
    }

    private void TryPushCursor(TimeSpan renderingTime, double movementDeltaX, double movementDeltaY)
    {
        if (!_pushCursorEnabled || !_activeMovementEnabled)
        {
            _expectedCursorX = null;
            _expectedCursorY = null;
            _cursorPushStartedAt = null;
            return;
        }

        var cursor = GetCursorScreenPosition();
        if (_expectedCursorX is double expectedX
            && _expectedCursorY is double expectedY
            && CursorNudgePlanner.IsManualMovement(cursor.X, cursor.Y, expectedX, expectedY))
        {
            _lastManualCursorMovementTime = renderingTime;
            _expectedCursorX = cursor.X;
            _expectedCursorY = cursor.Y;
            _cursorPushStartedAt = null;
            return;
        }

        if (!CursorNudgePlanner.CanNudge(
            _cursorService.IsAnyMouseButtonPressed(),
            renderingTime,
            _lastManualCursorMovementTime,
            _cursorPushStartedAt))
        {
            _cursorPushStartedAt = null;
            return;
        }

        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var result = CursorNudgePlanner.CalculateNudge(
            cursor.X,
            cursor.Y,
            Left + width / 2,
            Top + height / 2,
            movementDeltaX,
            movementDeltaY,
            GetCurrentMovementBounds());

        if (!result.ShouldMove)
        {
            _expectedCursorX = cursor.X;
            _expectedCursorY = cursor.Y;
            _cursorPushStartedAt = null;
            return;
        }

        _cursorPushStartedAt ??= renderingTime;
        var devicePoint = ToDevicePoint(new WpfPoint(result.X, result.Y));
        _cursorService.SetPosition(devicePoint.X, devicePoint.Y);
        _expectedCursorX = result.X;
        _expectedCursorY = result.Y;
    }

    private WpfPoint ToDevicePoint(WpfPoint point)
    {
        var source = PresentationSource.FromVisual(this);

        return source?.CompositionTarget is null
            ? point
            : source.CompositionTarget.TransformToDevice.Transform(point);
    }

    private void ChooseWanderTarget(double width, double height, PetMovementBounds bounds)
    {
        if (!_movementController.TryChooseWanderTarget(
            DateTime.UtcNow,
            Left,
            Top,
            width,
            height,
            bounds))
        {
            return;
        }

        StopIdleAnimation();
        StopBlinkAnimation();
    }

    private void AdvanceMoveFrame(double distance, PetHorizontalDirection direction)
    {
        var frames = GetMoveFrames(direction);
        if (frames.Count == 0 || distance <= 0)
        {
            return;
        }

        var frame = _movementController.AdvanceMoveFrame(distance, frames.Count);
        if (frame.Changed)
        {
            CharacterImage.Source = frames[frame.FrameIndex];
        }
    }

    private bool EnsureMovementFacing(PetHorizontalDirection direction)
    {
        var turnFrames = GetTurnFrames(direction);
        if (turnFrames.Count == 0)
        {
            return false;
        }

        var started = _directionalMovementAnimator.RequestDirection(direction, turnFrames.Count);
        if (!_directionalMovementAnimator.IsTurning)
        {
            return false;
        }

        if (started)
        {
            ShowCurrentTurnFrame();
            StartTurnTimer();
        }

        return true;
    }

    private void AdvanceTurnFrame()
    {
        if (!_directionalMovementAnimator.IsTurning)
        {
            _turnFrameTimer.Stop();
            return;
        }

        var frames = GetTurnFrames(_directionalMovementAnimator.TurnDirection);
        if (frames.Count == 0)
        {
            CancelDirectionalMovement();
            RestoreIdleAfterDirectionalMovement();
            return;
        }

        _directionalMovementAnimator.Advance(frames.Count);
        if (_directionalMovementAnimator.IsTurning)
        {
            ShowCurrentTurnFrame();
            StartTurnTimer();
            return;
        }

        _turnFrameTimer.Stop();
        if (_movementController.HasTarget && _directionalMovementAnimator.Facing != PetFacingDirection.Front)
        {
            _movementController.ResumeAfterVisualPause(Left, Top);
            var direction = _directionalMovementAnimator.Facing == PetFacingDirection.Left
                ? PetHorizontalDirection.Left
                : PetHorizontalDirection.Right;
            var moveFrames = GetMoveFrames(direction);
            if (moveFrames.Count > 0)
            {
                CharacterImage.Source = moveFrames[_movementController.MoveFrameIndex % moveFrames.Count];
            }

            return;
        }

        RestoreIdleAfterDirectionalMovement();
    }

    private void FinishDirectionalMovement()
    {
        _movementController.ResetMoveFrames();
        ResetActiveMovementVisual();
        BeginReturnToFront();
        ScheduleNextBlink();
    }

    private void BeginReturnToFront()
    {
        var currentDirection = _directionalMovementAnimator.Facing == PetFacingDirection.Left
            ? PetHorizontalDirection.Left
            : PetHorizontalDirection.Right;
        var frames = GetTurnFrames(currentDirection);
        if (!_directionalMovementAnimator.RequestFront(frames.Count))
        {
            RestoreIdleAfterDirectionalMovement();
            return;
        }

        ShowCurrentTurnFrame();
        StartTurnTimer();
    }

    private void StartTurnTimer()
    {
        var action = _directionalMovementAnimator.TurnDirection == PetHorizontalDirection.Left
            ? _turnLeftAction
            : _turnRightAction;
        _turnFrameTimer.Interval = PetFrameTiming.GetDuration(
            action,
            _directionalMovementAnimator.FrameIndex,
            DefaultTurnFrameInterval);
        if (!_turnFrameTimer.IsEnabled)
        {
            _turnFrameTimer.Start();
        }
    }

    private void ShowCurrentTurnFrame()
    {
        var frames = GetTurnFrames(_directionalMovementAnimator.TurnDirection);
        if (_directionalMovementAnimator.FrameIndex >= 0 &&
            _directionalMovementAnimator.FrameIndex < frames.Count)
        {
            CharacterImage.Source = frames[_directionalMovementAnimator.FrameIndex];
        }
    }

    private IReadOnlyList<ImageSource> GetMoveFrames(PetHorizontalDirection direction)
    {
        return direction == PetHorizontalDirection.Left ? _moveLeftFrames : _moveRightFrames;
    }

    private IReadOnlyList<ImageSource> GetTurnFrames(PetHorizontalDirection direction)
    {
        return direction == PetHorizontalDirection.Left ? _turnLeftFrames : _turnRightFrames;
    }

    private PetHorizontalDirection ResolveMovementDirection(double deltaX)
    {
        if (deltaX < -0.001)
        {
            return PetHorizontalDirection.Left;
        }

        if (deltaX > 0.001)
        {
            return PetHorizontalDirection.Right;
        }

        return _directionalMovementAnimator.Facing == PetFacingDirection.Left
            ? PetHorizontalDirection.Left
            : PetHorizontalDirection.Right;
    }

    private void RestoreIdleAfterDirectionalMovement()
    {
        if (!_isDragging && !_temporaryExpressionTimer.IsEnabled &&
            _animationController.ExpressionTransitionMode == PetExpressionTransitionMode.None)
        {
            CharacterImage.Source = GetCurrentIdleFrame();
            StartIdleAnimation();
        }
    }

    private void CancelDirectionalMovement()
    {
        _turnFrameTimer.Stop();
        _directionalMovementAnimator.Reset();
    }

    private void ResetMoveFrameState()
    {
        _movementController.ResetMoveFrames();
        CancelDirectionalMovement();

        if (!_isDragging && !_temporaryExpressionTimer.IsEnabled &&
            _animationController.ExpressionTransitionMode == PetExpressionTransitionMode.None)
        {
            CharacterImage.Source = GetCurrentIdleFrame();
            StartIdleAnimation();
        }
    }

    private void ApplyActiveMovementVisual()
    {
        var direction = _movementController.LastDeltaX < 0 ? -1 : 1;
        if (_activeMovementVisualDirection == direction)
        {
            return;
        }

        _activeMovementVisualDirection = direction;
        AnimateCharacterScale(
            direction < 0 ? 1 - PetAnimationTimings.ActiveMovementScaleDelta : 1 + PetAnimationTimings.ActiveMovementScaleDelta,
            1 + PetAnimationTimings.ActiveMovementScaleDelta / 2,
            PetAnimationTimings.MovementVisualDuration);
    }

    private void ApplyDragMovementVisual()
    {
        if (_dragMovementVisualApplied)
        {
            return;
        }

        _dragMovementVisualApplied = true;
        AnimateCharacterScale(
            1 + PetAnimationTimings.DragMovementScaleDelta,
            1 - PetAnimationTimings.DragMovementScaleDelta,
            PetAnimationTimings.MovementVisualDuration);
    }

    private void ResetActiveMovementVisual()
    {
        if (_isDragging || _temporaryExpressionTimer.IsEnabled ||
            _animationController.ExpressionTransitionMode != PetExpressionTransitionMode.None)
        {
            return;
        }

        if (_activeMovementVisualDirection == 0 && !_dragMovementVisualApplied)
        {
            return;
        }

        _activeMovementVisualDirection = 0;
        _dragMovementVisualApplied = false;
        AnimateCharacterScale(1, 1, PetAnimationTimings.MovementVisualRestoreDuration);
    }

    private void AnimateCharacterScale(double scaleX, double scaleY, TimeSpan duration)
    {
        var animationDuration = new Duration(duration);
        var easing = new WpfAnimation.QuadraticEase { EasingMode = WpfAnimation.EasingMode.EaseOut };

        CharacterScaleTransform.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            new WpfAnimation.DoubleAnimation(scaleX, animationDuration) { EasingFunction = easing });
        CharacterScaleTransform.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            new WpfAnimation.DoubleAnimation(scaleY, animationDuration) { EasingFunction = easing });
    }

    private void OnWheelCatalogChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.CheckAccess())
        {
            RefreshWheelCatalog();
            return;
        }

        if (!Dispatcher.HasShutdownStarted)
        {
            _ = Dispatcher.InvokeAsync(RefreshWheelCatalog);
        }
    }

    private void RefreshWheelCatalog()
    {
        if (_interactions.IsRadialWheelOpen || _interactions.RadialWheel.IsOpen)
        {
            CloseRadialWheel(cancelController: true);
        }

        _interactions.UpdateCatalog(_wheelCatalogService.Current);
        _shortcutIconCache.Clear();
        _secondRingContentKey = "closed";
        SecondRingSurface.Children.Clear();
        _secondRingVisuals.Clear();
        SecondRingSurface.Visibility = Visibility.Collapsed;
        SecondRingBoundary.Visibility = Visibility.Collapsed;
        BuildFirstRadialWheelRing();
    }

    private void BuildFirstRadialWheelRing()
    {
        FirstRingSurface.Children.Clear();
        _firstRingVisuals.Clear();
        for (var index = 0; index < _interactions.Catalog.Categories.Count; index++)
        {
            var category = _interactions.Catalog.Categories[index];
            _firstRingVisuals.Add(AddRadialWheelItem(
                FirstRingSurface,
                category.DisplayName,
                index,
                _interactions.Catalog.Categories.Count,
                WheelCatalog.InnerRadius,
                WheelCatalog.FirstRingOuterRadius,
                isEnabled: true,
                isSecondRing: false));
        }
    }

    private void BuildSecondRadialWheelRing()
    {
        SecondRingSurface.Children.Clear();
        _secondRingVisuals.Clear();
        var items = _interactions.RadialWheel.VisibleSecondLevelItems;
        var arc = RadialWheelArcLayout.CreateSecondRingArc(
            _interactions.RadialWheel.SelectedCategoryIndex,
            _interactions.Catalog.Categories.Count,
            items.Count);
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            _secondRingVisuals.Add(AddRadialWheelItem(
                SecondRingSurface,
                item.DisplayName,
                index,
                items.Count,
                WheelCatalog.FirstRingOuterRadius,
                WheelCatalog.SecondRingOuterRadius,
                item.IsEnabled,
                isSecondRing: true,
                icon: LoadShortcutWheelIcon(item),
                arc: arc));
        }

        SecondRingSurface.Visibility = _interactions.RadialWheel.IsSecondLevelOpen
            ? Visibility.Visible
            : Visibility.Collapsed;
        SecondRingBoundary.Visibility = SecondRingSurface.Visibility;
        SecondRingBoundary.Data = CreateRadialWheelArcBoundaryGeometry(
            WheelCatalog.SecondRingOuterRadius,
            arc);
    }

    private RadialWheelItemVisual AddRadialWheelItem(
        WpfControls.Canvas surface,
        string displayName,
        int index,
        int count,
        double innerRadius,
        double outerRadius,
        bool isEnabled,
        bool isSecondRing,
        ImageSource? icon = null,
        RadialWheelArc? arc = null)
    {
        var ring = isSecondRing ? RadialWheelRing.Second : RadialWheelRing.First;
        var itemArc = arc ?? new RadialWheelArc(0, Math.Tau);
        var panel = new WpfControls.Canvas
        {
            Width = RadialWheelSurface.Width,
            Height = RadialWheelSurface.Height,
            IsHitTestVisible = false,
            Opacity = 1,
        };
        var sector = new WpfShapes.Path
        {
            Data = CreateRadialWheelSectorGeometry(index, count, innerRadius, outerRadius, itemArc),
            Fill = CreateRadialWheelFillBrush(RadialWheelStyle.GetNormalFill(ring, isEnabled), isSelected: false),
            Stroke = CreateRadialWheelStrokeBrush(RadialWheelStyle.NormalStroke),
            StrokeThickness = RadialWheelStyle.NormalStrokeThickness,
        };
        panel.Children.Add(sector);

        var label = new WpfControls.TextBlock
        {
            Text = displayName,
            Foreground = new SolidColorBrush(WpfColor.FromArgb(255, 56, 39, 72)),
            FontSize = isSecondRing ? 12 : 13.5,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Width = isSecondRing ? 88 : 96,
            MaxHeight = 40,
            Opacity = isEnabled ? 1 : 0.55,
        };
        TextOptions.SetTextFormattingMode(label, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(label, TextRenderingMode.Grayscale);
        FrameworkElement content = label;
        if (icon is not null)
        {
            var iconImage = new WpfControls.Image
            {
                Source = icon,
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 0, 0, 3),
                Stretch = Stretch.Uniform,
                Opacity = isEnabled ? 0.96 : 0.58,
            };
            var stack = new WpfControls.StackPanel
            {
                Width = label.Width,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            };
            label.MaxHeight = 30;
            stack.Children.Add(iconImage);
            stack.Children.Add(label);
            content = stack;
        }

        content.RenderTransformOrigin = new WpfPoint(0.5, 0.5);
        content.RenderTransform = new ScaleTransform(1, 1);
        var center = RadialWheelSurface.Width / 2;
        var labelRadius = (innerRadius + outerRadius) / 2;
        var labelAngle = -Math.PI / 2
            + itemArc.StartAngle
            + (index + 0.5) * itemArc.StepAngle(count);
        var labelCenter = PointOnWheel(center, labelRadius, labelAngle);
        WpfControls.Canvas.SetLeft(content, labelCenter.X - label.Width / 2);
        WpfControls.Canvas.SetTop(content, labelCenter.Y - (icon is not null ? 29 : isSecondRing ? 15 : 12));
        panel.Children.Add(content);
        surface.Children.Add(panel);
        return new RadialWheelItemVisual(sector, label, content, isEnabled, ring);
    }

    private ImageSource? LoadShortcutWheelIcon(WheelActionItem item)
    {
        if (item.ActionType != WheelActionType.Shortcut || string.IsNullOrWhiteSpace(item.ActionReference))
        {
            return null;
        }

        if (_shortcutIconCache.TryGetValue(item.ActionReference, out var cached))
        {
            return cached;
        }

        var shortcut = _shortcutService.GetAll().FirstOrDefault(
            candidate => string.Equals(candidate.Id, item.ActionReference, StringComparison.Ordinal));
        var icon = shortcut is null ? null : ShortcutIconService.TryLoadSmallIcon(shortcut);
        _shortcutIconCache[item.ActionReference] = icon;
        return icon;
    }

    private Geometry CreateRadialWheelSectorGeometry(
        int index,
        int count,
        double innerRadius,
        double outerRadius,
        RadialWheelArc arc)
    {
        var center = RadialWheelSurface.Width / 2;
        if (count == 1 && arc.SweepAngle >= Math.Tau - 1e-9)
        {
            return new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new EllipseGeometry(new WpfPoint(center, center), outerRadius, outerRadius),
                new EllipseGeometry(new WpfPoint(center, center), innerRadius, innerRadius));
        }

        var step = arc.StepAngle(count);
        var gap = RadialWheelStyle.SectorGapRadians;
        var startAngle = -Math.PI / 2 + arc.StartAngle + index * step + gap;
        var endAngle = -Math.PI / 2 + arc.StartAngle + (index + 1) * step - gap;
        var outerStart = PointOnWheel(center, outerRadius, startAngle);
        var outerEnd = PointOnWheel(center, outerRadius, endAngle);
        var innerEnd = PointOnWheel(center, innerRadius, endAngle);
        var innerStart = PointOnWheel(center, innerRadius, startAngle);
        var isLargeArc = endAngle - startAngle > Math.PI;
        var figure = new PathFigure
        {
            StartPoint = outerStart,
            IsClosed = true,
        };
        figure.Segments.Add(new ArcSegment(outerEnd, new WpfSize(outerRadius, outerRadius), 0, isLargeArc, SweepDirection.Clockwise, true));
        figure.Segments.Add(new LineSegment(innerEnd, true));
        figure.Segments.Add(new ArcSegment(innerStart, new WpfSize(innerRadius, innerRadius), 0, isLargeArc, SweepDirection.Counterclockwise, true));
        return new PathGeometry([figure]);
    }

    private Geometry CreateRadialWheelArcBoundaryGeometry(double radius, RadialWheelArc arc)
    {
        if (arc.SweepAngle <= 0)
        {
            return Geometry.Empty;
        }

        var center = RadialWheelSurface.Width / 2;
        var startAngle = -Math.PI / 2 + arc.StartAngle;
        var endAngle = startAngle + arc.SweepAngle;
        var figure = new PathFigure
        {
            StartPoint = PointOnWheel(center, radius, startAngle),
            IsClosed = false,
        };
        figure.Segments.Add(new ArcSegment(
            PointOnWheel(center, radius, endAngle),
            new WpfSize(radius, radius),
            0,
            arc.SweepAngle > Math.PI,
            SweepDirection.Clockwise,
            true));
        return new PathGeometry([figure]);
    }

    private static WpfPoint PointOnWheel(double center, double radius, double angle) =>
        new(center + Math.Cos(angle) * radius, center + Math.Sin(angle) * radius);

    private static WpfColor ToWpfColor(RadialWheelColor color) =>
        WpfColor.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);

    private static WpfColor ShiftRadialWheelColor(
        RadialWheelColor color,
        int redDelta,
        int greenDelta,
        int blueDelta,
        int alphaDelta = 0) =>
        WpfColor.FromArgb(
            (byte)Math.Clamp(color.Alpha + alphaDelta, 0, 255),
            (byte)Math.Clamp(color.Red + redDelta, 0, 255),
            (byte)Math.Clamp(color.Green + greenDelta, 0, 255),
            (byte)Math.Clamp(color.Blue + blueDelta, 0, 255));

    private static LinearGradientBrush CreateRadialWheelFillBrush(
        RadialWheelColor color,
        bool isSelected)
    {
        var highlightBoost = isSelected ? 12 : 0;
        return new LinearGradientBrush
        {
            StartPoint = new WpfPoint(0, 0),
            EndPoint = new WpfPoint(1, 1),
            GradientStops =
            {
                new GradientStop(ShiftRadialWheelColor(
                    color,
                    38 + highlightBoost,
                    25 + highlightBoost,
                    54 + highlightBoost,
                    12), 0),
                new GradientStop(ShiftRadialWheelColor(color, 24, -2, 28, 6), 0.32),
                new GradientStop(ToWpfColor(color), 0.64),
                new GradientStop(ShiftRadialWheelColor(color, -18, -16, 12, -4), 1),
            },
        };
    }

    private static LinearGradientBrush CreateRadialWheelStrokeBrush(RadialWheelColor color) =>
        new()
        {
            StartPoint = new WpfPoint(0, 0),
            EndPoint = new WpfPoint(1, 1),
            GradientStops =
            {
                new GradientStop(ToWpfColor(color), 0),
                new GradientStop(ShiftRadialWheelColor(color, 12, -4, 18, -20), 0.48),
                new GradientStop(ShiftRadialWheelColor(color, -20, -12, 5, -45), 1),
            },
        };

    private WpfPoint ToScreenPoint(WpfPoint localPoint)
    {
        var devicePoint = RootGrid.PointToScreen(localPoint);
        return FromDevicePoint(devicePoint);
    }

    private WpfPoint FromDevicePoint(WpfPoint devicePoint)
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget is null
            ? devicePoint
            : source.CompositionTarget.TransformFromDevice.Transform(devicePoint);
    }

    private void UpdateRadialWheelHoldGesture()
    {
        if (_interactions.PointerState != PetPointerGestureState.RightPending
            || WpfInput.Mouse.RightButton != WpfInput.MouseButtonState.Pressed)
        {
            CancelPendingPointerGesture();
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (_interactions.UpdateHold(now) == PetPointerIntent.RadialWheel)
        {
            OpenRadialWheel();
            return;
        }

        UpdateRadialWheelHoldFeedback(_interactions.GetRightHoldProgress(now, RadialWheelHoldRevealDelay));
    }

    private void StartRadialWheelHoldRendering()
    {
        if (_radialWheelHoldRenderingSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering += OnRadialWheelHoldRendering;
        _radialWheelHoldRenderingSubscribed = true;
    }

    private void StopRadialWheelHoldRendering()
    {
        if (!_radialWheelHoldRenderingSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRadialWheelHoldRendering;
        _radialWheelHoldRenderingSubscribed = false;
    }

    private void OnRadialWheelHoldRendering(object? sender, EventArgs e)
    {
        UpdateRadialWheelHoldGesture();
    }

    private void UpdateRadialWheelHoldFeedback(double progress)
    {
        if (progress <= 0)
        {
            HideRadialWheelHoldFeedback();
            return;
        }

        var clampedProgress = Math.Clamp(progress, 0, 0.999);
        var start = new WpfPoint(HoldIndicatorSize / 2, (HoldIndicatorSize / 2) - HoldIndicatorRadius);
        var angle = (-90 + (360 * clampedProgress)) * Math.PI / 180;
        var end = new WpfPoint(
            (HoldIndicatorSize / 2) + (HoldIndicatorRadius * Math.Cos(angle)),
            (HoldIndicatorSize / 2) + (HoldIndicatorRadius * Math.Sin(angle)));
        var figure = new PathFigure { StartPoint = start, IsClosed = false };
        figure.Segments.Add(new ArcSegment(
            end,
            new WpfSize(HoldIndicatorRadius, HoldIndicatorRadius),
            0,
            clampedProgress > 0.5,
            SweepDirection.Clockwise,
            true));
        RadialWheelHoldArc.Data = new PathGeometry([figure]);
        var workArea = SystemParameters.WorkArea;
        RadialWheelHoldOverlay.HorizontalOffset = Math.Clamp(
            _requestedRadialWheelOrigin.X - (HoldIndicatorSize / 2),
            workArea.Left,
            Math.Max(workArea.Left, workArea.Right - HoldIndicatorSize));
        RadialWheelHoldOverlay.VerticalOffset = Math.Clamp(
            _requestedRadialWheelOrigin.Y - (HoldIndicatorSize / 2),
            workArea.Top,
            Math.Max(workArea.Top, workArea.Bottom - HoldIndicatorSize));
        RadialWheelHoldOverlay.IsOpen = true;
    }

    private void HideRadialWheelHoldFeedback()
    {
        RadialWheelHoldOverlay.IsOpen = false;
        RadialWheelHoldArc.Data = null;
    }

    private void CancelPendingPointerGesture(bool releaseCapture = true)
    {
        StopRadialWheelHoldRendering();
        HideRadialWheelHoldFeedback();
        _interactions.CancelPointerGesture();
        if (releaseCapture)
        {
            ReleasePendingMouseCapture();
        }
    }

    private void ReleasePendingMouseCapture()
    {
        if (WpfInput.Mouse.Captured == this && !_interactions.IsRadialWheelOpen && !_isDragging)
        {
            ReleaseMouseCapture();
        }
    }

    private void PositionRadialWheelOverlay(WpfPoint requestedOrigin)
    {
        var placement = RadialWheelOverlayPlacement.Calculate(
            requestedOrigin.X,
            requestedOrigin.Y,
            RadialWheelSurface.Width,
            RadialWheelSurface.Height);
        _radialWheelOrigin = new WpfPoint(placement.CenterX, placement.CenterY);
        RadialWheelOverlay.HorizontalOffset = placement.Left;
        RadialWheelOverlay.VerticalOffset = placement.Top;
    }

    private void OnRadialWheelOverlayOpened(object? sender, EventArgs e) =>
        WindowsPopupPositioner.TryCenterAt(RadialWheelSurface, _requestedRadialWheelOriginDevice);

    private void AnimateRadialWheelOpen()
    {
        RadialWheelSurface.Opacity = 0;
        RadialWheelScaleTransform.ScaleX = PetAnimationTimings.WheelOpenStartScale;
        RadialWheelScaleTransform.ScaleY = PetAnimationTimings.WheelOpenStartScale;
        var duration = new Duration(PetAnimationTimings.WheelOpenDuration);
        var easing = new WpfAnimation.BackEase
        {
            Amplitude = 0.2,
            EasingMode = WpfAnimation.EasingMode.EaseOut,
        };
        RadialWheelSurface.BeginAnimation(UIElement.OpacityProperty, new WpfAnimation.DoubleAnimation(1, duration));
        RadialWheelScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
        RadialWheelScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
    }

    private void OpenRadialWheel()
    {
        StopRadialWheelHoldRendering();
        HideRadialWheelHoldFeedback();
        if (!_interactions.HasWheelCategories || WpfInput.Mouse.RightButton != WpfInput.MouseButtonState.Pressed)
        {
            CancelPendingPointerGesture();
            return;
        }

        StopPetting(restoreIdle: false);
        CancelTemporaryExpression();
        StopInputReactiveMode(restoreIdle: false);
        StopIdleAnimation();
        StopBlinkAnimation();
        if (!_interactions.TryOpenRadialWheel(DateTimeOffset.UtcNow))
        {
            return;
        }
        _hasRadialWheelPointer = false;
        _hasRadialWheelPointerEntered = false;
        _secondRingContentKey = "";
        PositionRadialWheelOverlay(_requestedRadialWheelOrigin);
        RadialWheelOverlay.IsOpen = true;
        AnimateRadialWheelOpen();
        CaptureMouse();
        _radialWheelPointerProbeTimer.Start();
        RefreshRadialWheelVisuals(forceSecondRingRebuild: true);
        UpdateActiveMovementTimer();
    }

    private void CloseRadialWheel(bool cancelController, bool restoreAnimation = true)
    {
        StopRadialWheelHoldRendering();
        HideRadialWheelHoldFeedback();
        _radialWheelPointerProbeTimer.Stop();
        _interactions.CloseRadialWheel(cancelController);
        _hasRadialWheelPointer = false;
        _hasRadialWheelPointerEntered = false;
        _secondRingContentKey = "closed";
        SecondRingSurface.Visibility = Visibility.Collapsed;
        RadialWheelOverlay.IsOpen = false;
        RadialWheelSurface.BeginAnimation(UIElement.OpacityProperty, null);
        RadialWheelSurface.Opacity = 1;
        RadialWheelScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        RadialWheelScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        RadialWheelScaleTransform.ScaleX = 1;
        RadialWheelScaleTransform.ScaleY = 1;
        if (WpfInput.Mouse.Captured == this)
        {
            ReleaseMouseCapture();
        }

        UpdateRadialWheelSelectionVisuals(_firstRingVisuals, -1);
        UpdateRadialWheelSelectionVisuals(_secondRingVisuals, -1);
        if (!restoreAnimation)
        {
            return;
        }

        UpdateInputReactiveMode();
        StartIdleAnimation();
        ScheduleNextBlink();
        UpdateActiveMovementTimer();
    }

    private void ProbeRadialWheelPointer()
    {
        if (_interactions.IsRadialWheelOpen && _hasRadialWheelPointer)
        {
            UpdateRadialWheelController(_lastRadialWheelPointer, DateTimeOffset.UtcNow);
        }
    }

    private void UpdateRadialWheelPointer(WpfPoint localPosition, DateTimeOffset now)
    {
        var screenPosition = ToScreenPoint(localPosition);
        _lastRadialWheelPointer = new WpfPoint(
            screenPosition.X - _radialWheelOrigin.X,
            screenPosition.Y - _radialWheelOrigin.Y);
        _hasRadialWheelPointer = true;
        UpdateRadialWheelController(_lastRadialWheelPointer, now);
    }

    private void UpdateRadialWheelController(WpfPoint pointer, DateTimeOffset now)
    {
        var pointerRegion = RadialWheelSelector.GetSelection(
            pointer.X,
            pointer.Y,
            _interactions.Catalog.Categories.Count,
            _interactions.RadialWheel.VisibleSecondLevelItems.Count);
        if (!_hasRadialWheelPointerEntered && pointerRegion.Ring == RadialWheelRing.Outside)
        {
            return;
        }

        _hasRadialWheelPointerEntered = true;
        _interactions.RadialWheel.UpdatePointer(pointer.X, pointer.Y, now);
        if (!_interactions.RadialWheel.IsOpen)
        {
            CloseRadialWheel(cancelController: false);
            return;
        }

        RefreshRadialWheelVisuals(forceSecondRingRebuild: false);
    }

    private void RefreshRadialWheelVisuals(bool forceSecondRingRebuild)
    {
        var contentKey = _interactions.RadialWheel.IsSecondLevelOpen
            ? $"{_interactions.RadialWheel.SelectedCategoryIndex}:{_interactions.RadialWheel.CurrentPage}:{string.Join(',', _interactions.RadialWheel.VisibleSecondLevelItems.Select(item => item.Id))}"
            : "closed";
        if (forceSecondRingRebuild || !string.Equals(_secondRingContentKey, contentKey, StringComparison.Ordinal))
        {
            _secondRingContentKey = contentKey;
            BuildSecondRadialWheelRing();
        }

        UpdateRadialWheelSelectionVisuals(_firstRingVisuals, _interactions.RadialWheel.SelectedCategoryIndex);
        UpdateRadialWheelSelectionVisuals(_secondRingVisuals, _interactions.RadialWheel.SelectedSecondLevelIndex);
    }

    private static void UpdateRadialWheelSelectionVisuals(
        IReadOnlyList<RadialWheelItemVisual> visuals,
        int selectedIndex)
    {
        for (var index = 0; index < visuals.Count; index++)
        {
            var visual = visuals[index];
            var isSelected = visual.IsEnabled && selectedIndex == index;
            if (visual.IsSelected == isSelected)
            {
                continue;
            }

            visual.IsSelected = isSelected;
            var fill = RadialWheelStyle.GetNormalFill(visual.Ring, visual.IsEnabled);
            visual.Sector.Fill = CreateRadialWheelFillBrush(fill, isSelected: false);
            visual.Sector.Stroke = CreateRadialWheelStrokeBrush(
                isSelected ? RadialWheelStyle.SelectedStroke : RadialWheelStyle.NormalStroke);
            visual.Sector.StrokeThickness = isSelected
                ? RadialWheelStyle.SelectedStrokeThickness
                : RadialWheelStyle.NormalStrokeThickness;
            visual.Sector.Effect = null;
            visual.Label.Opacity = isSelected ? 1 : visual.IsEnabled ? 1 : 0.55;
            visual.Label.FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold;
            visual.Label.Foreground = new SolidColorBrush(WpfColor.FromArgb(255, 56, 39, 72));
            if (visual.Content.RenderTransform is not ScaleTransform scaleTransform)
            {
                scaleTransform = new ScaleTransform(1, 1);
                visual.Content.RenderTransform = scaleTransform;
            }

            var targetScale = isSelected ? WheelCatalog.SelectedScale : 1;
            var duration = new Duration(PetAnimationTimings.WheelSelectionDuration);
            var easing = new WpfAnimation.QuadraticEase { EasingMode = WpfAnimation.EasingMode.EaseOut };
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new WpfAnimation.DoubleAnimation(targetScale, duration) { EasingFunction = easing });
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new WpfAnimation.DoubleAnimation(targetScale, duration) { EasingFunction = easing });
        }
    }

    private void ExecuteWheelAction(WheelActionItem action)
    {
        switch (action.ActionType)
        {
            case WheelActionType.Expression:
                ApplyTemporaryExpression(action.ActionReference);
                break;
            case WheelActionType.Shortcut:
                LaunchShortcut(action.ActionReference);
                break;
        }
    }

    private void LaunchShortcut(string? shortcutId)
    {
        var shortcut = _shortcutService.GetAll()
            .FirstOrDefault(candidate => string.Equals(candidate.Id, shortcutId, StringComparison.Ordinal));
        if (shortcut is null)
        {
            TryLogInfo($"Shortcut wheel reference '{shortcutId ?? "<missing>"}' was not found.");
            ApplyTemporaryExpression("confused");
            return;
        }

        var result = _shortcutLauncher.Launch(shortcut);
        if (!result.Succeeded)
        {
            ApplyTemporaryExpression("confused");
        }
    }

    private void OnPetDragOver(object sender, WpfDragEventArgs e)
    {
        try
        {
            e.Effects = ShortcutDropDataReader.ContainsSupportedFormat(e.Data)
                ? WpfDragDropEffects.Link
                : WpfDragDropEffects.None;
        }
        catch (Exception ex)
        {
            e.Effects = WpfDragDropEffects.None;
            TryLogError("Shortcut drag data could not be inspected.", ex);
        }

        e.Handled = true;
    }

    private void OnPetDrop(object sender, WpfDragEventArgs e)
    {
        e.Handled = true;
        try
        {
            var paths = ShortcutDropDataReader.ExtractPaths(e.Data);
            var textValues = ShortcutDropDataReader.ExtractTextValues(e.Data);
            var result = _shortcutDrops.AddDroppedItems(paths, textValues);
            ShowShortcutDropFeedback(result);
            e.Effects = WpfDragDropEffects.Link;
        }
        catch (Exception ex)
        {
            e.Effects = WpfDragDropEffects.None;
            TryLogError("Shortcut drop could not be processed.", ex);
            ApplyTemporaryExpression("confused");
        }
    }

    private void ShowShortcutDropFeedback(ShortcutDropResult result)
    {
        var nonAddedCount = result.DuplicateCount + result.UnsupportedCount + result.FailedCount;
        var (outcome, expressionId) = result switch
        {
            { AddedCount: > 0 } when nonAddedCount == 0 => ("success", "happy"),
            { DuplicateCount: > 0, AddedCount: 0, UnsupportedCount: 0, FailedCount: 0 } => ("duplicate", "proud"),
            { AddedCount: > 0 } => ("partial", "surprised"),
            { UnsupportedCount: > 0, AddedCount: 0, DuplicateCount: 0, FailedCount: 0 } => ("unsupported", "pouting"),
            _ => ("failed", "confused"),
        };
        TryLogInfo(
            $"Shortcut drop {outcome}: added={result.AddedCount}, duplicate={result.DuplicateCount}, " +
            $"unsupported={result.UnsupportedCount}, failed={result.FailedCount}.");
        ApplyTemporaryExpression(expressionId);
    }

    private void TryLogInfo(string message)
    {
        try
        {
            _logger.Info(message);
        }
        catch
        {
            // Feedback and launch failures must stay contained when logging is unavailable.
        }
    }

    private void TryLogError(string message, Exception exception)
    {
        try
        {
            _logger.Error(message, exception);
        }
        catch
        {
            // Drop failures must not escape through a secondary logging failure.
        }
    }

    private void AnimateCharacterImageSwap(ImageSource image)
    {
        StopIdleBreathing();
        ResetCharacterTransitionAnimations();
        CharacterImage.Opacity = PetAnimationTimings.ExpressionDimmedOpacity;
        CharacterScaleTransform.ScaleX = PetAnimationTimings.ExpressionEnterStartScale;
        CharacterScaleTransform.ScaleY = PetAnimationTimings.ExpressionEnterStartScale;
        CharacterTranslateTransform.Y = 0;
        CharacterImage.Source = image;

        var duration = new Duration(PetAnimationTimings.ExpressionEnterDuration);
        var easing = new WpfAnimation.QuadraticEase { EasingMode = WpfAnimation.EasingMode.EaseOut };

        CharacterImage.BeginAnimation(UIElement.OpacityProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
        CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
        CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
    }

    private void ApplyTemporaryExpression(string? expressionId)
    {
        if (string.IsNullOrWhiteSpace(expressionId) ||
            !_expressionAssetsById.TryGetValue(expressionId, out var asset))
        {
            TryLogInfo($"Expression wheel reference '{expressionId ?? "<missing>"}' was not found.");
            return;
        }

        _temporaryExpressionTimer.Stop();
        StopExpressionTransition();
        StopInputReactiveMode(restoreIdle: false);
        StopIdleAnimation();
        StopBlinkAnimation();
        _pendingExpressionAsset = asset;
        _activeExpressionAsset = asset;
        PlayExpressionTransitionIn();
        UpdateActiveMovementTimer();
    }

    private void CancelTemporaryExpression()
    {
        _temporaryExpressionTimer.Stop();
        StopExpressionTransition();
        _pendingExpressionAsset = null;
        _activeExpressionAsset = null;
        ResetCharacterTransitionAnimations();
        UpdateActiveMovementTimer();
    }

    private void ResetCharacterTransitionAnimations()
    {
        CharacterImage.BeginAnimation(UIElement.OpacityProperty, null);
        CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        CharacterImage.Opacity = 1;
        CharacterScaleTransform.ScaleX = 1;
        CharacterScaleTransform.ScaleY = 1;
    }

    private void RestoreAfterTemporaryExpression()
    {
        _temporaryExpressionTimer.Stop();
        _pendingExpressionAsset = null;
        StopIdleAnimation();
        StopBlinkAnimation();
        StopInputReactiveMode(restoreIdle: false);
        PlayExpressionTransitionOut();
        UpdateActiveMovementTimer();
    }

    private void PlayExpressionTransitionIn()
    {
        if (_pendingExpressionAsset is null)
        {
            return;
        }

        var specificFrames = _pendingExpressionAsset.TransitionFrames;
        _activeExpressionUsesSpecificTransition = specificFrames.Count > 0;
        _activeExpressionTransitionFrames = ExpressionTransitionPlanner.EnterFrames(specificFrames, _expressionTransitionInFrames);
        if (_activeExpressionTransitionFrames.Count == 0)
        {
            ShowPendingExpression();
            return;
        }

        ResetCharacterTransitionAnimations();
        _animationController.BeginExpressionTransition(PetExpressionTransitionMode.In, _activeExpressionTransitionFrames.Count);
        _expressionTransitionFrameTimer.Interval = GetExpressionTransitionFrameDuration(0);
        CharacterImage.Source = _activeExpressionTransitionFrames[_animationController.ExpressionTransitionFrameIndex];
        _expressionTransitionFrameTimer.Stop();
        _expressionTransitionFrameTimer.Start();
    }

    private void PlayExpressionTransitionOut()
    {
        var specificFrames = _activeExpressionAsset?.TransitionFrames ?? Array.Empty<ImageSource>();
        _activeExpressionUsesSpecificTransition = specificFrames.Count > 0;
        _activeExpressionTransitionFrames = ExpressionTransitionPlanner.ExitFrames(specificFrames, _expressionTransitionOutFrames);
        if (_activeExpressionTransitionFrames.Count == 0)
        {
            CompleteExpressionRestore();
            return;
        }

        ResetCharacterTransitionAnimations();
        _animationController.BeginExpressionTransition(PetExpressionTransitionMode.Out, _activeExpressionTransitionFrames.Count);
        _expressionTransitionFrameTimer.Interval = GetExpressionTransitionFrameDuration(0);
        CharacterImage.Source = _activeExpressionTransitionFrames[_animationController.ExpressionTransitionFrameIndex];
        _expressionTransitionFrameTimer.Stop();
        _expressionTransitionFrameTimer.Start();
    }

    private void AdvanceExpressionTransitionFrame()
    {
        var frames = _activeExpressionTransitionFrames;

        if (_animationController.ExpressionTransitionMode == PetExpressionTransitionMode.None || frames.Count == 0)
        {
            StopExpressionTransition();
            return;
        }

        var advance = _animationController.AdvanceExpressionTransition(frames.Count);
        if (!advance.Completed)
        {
            CharacterImage.Source = frames[advance.FrameIndex];
            _expressionTransitionFrameTimer.Interval = GetExpressionTransitionFrameDuration(advance.FrameIndex);
            return;
        }

        StopExpressionTransition();

        if (advance.CompletedMode == PetExpressionTransitionMode.In)
        {
            ShowPendingExpression();
            return;
        }

        CompleteExpressionRestore();
    }

    private TimeSpan GetExpressionTransitionFrameDuration(int frameIndex)
    {
        if (_activeExpressionUsesSpecificTransition)
        {
            var expression = _animationController.ExpressionTransitionMode == PetExpressionTransitionMode.In
                ? _pendingExpressionAsset
                : _activeExpressionAsset;
            return expression?.Definition.TransitionFrameInterval
                ?? DefaultExpressionTransitionFrameInterval;
        }

        var action = _animationController.ExpressionTransitionMode == PetExpressionTransitionMode.In
            ? _expressionTransitionInAction
            : _expressionTransitionOutAction;
        return PetFrameTiming.GetDuration(action, frameIndex, DefaultExpressionTransitionFrameInterval);
    }

    private void ShowPendingExpression()
    {
        if (_pendingExpressionAsset is null || _isDragging || _interactions.IsRadialWheelOpen)
        {
            return;
        }

        var asset = _pendingExpressionAsset;
        _pendingExpressionAsset = null;
        _activeExpressionAsset = asset;
        if (_activeExpressionUsesSpecificTransition)
        {
            ResetCharacterTransitionAnimations();
            CharacterImage.Source = asset.Image;
        }
        else
        {
            AnimateCharacterImageSwap(asset.Image);
        }
        _temporaryExpressionTimer.Start();
        UpdateActiveMovementTimer();
    }

    private void CompleteExpressionRestore()
    {
        if (_isDragging || _interactions.IsRadialWheelOpen)
        {
            return;
        }

        _animationController.ResetIdle();
        _pendingExpressionAsset = null;
        _activeExpressionAsset = null;
        _activeExpressionTransitionFrames = Array.Empty<ImageSource>();
        _activeExpressionUsesSpecificTransition = false;
        ResetCharacterTransitionAnimations();
        CharacterImage.Source = GetCurrentIdleFrame();
        StartIdleAnimation();
        ScheduleNextBlink();
        UpdateInputReactiveMode();
        UpdateActiveMovementTimer();
    }

    private void StopExpressionTransition()
    {
        _expressionTransitionFrameTimer.Stop();
        _animationController.StopExpressionTransition();
        _activeExpressionTransitionFrames = Array.Empty<ImageSource>();
    }

    private void StartIdleBreathing()
    {
        if (_isDragging || _interactions.IsRadialWheelOpen)
        {
            return;
        }

        var duration = new Duration(PetAnimationTimings.IdleBreathingCycleDuration);
        var easing = new WpfAnimation.SineEase { EasingMode = WpfAnimation.EasingMode.EaseInOut };

        var translate = new WpfAnimation.DoubleAnimation
        {
            From = 0,
            To = PetAnimationTimings.IdleBreathingTranslateY,
            Duration = duration,
            AutoReverse = true,
            RepeatBehavior = WpfAnimation.RepeatBehavior.Forever,
            EasingFunction = easing,
        };
        CharacterTranslateTransform.BeginAnimation(TranslateTransform.YProperty, translate);

        var scale = 1 + PetAnimationTimings.IdleBreathingScaleDelta;
        var scaleX = new WpfAnimation.DoubleAnimation
        {
            From = 1,
            To = scale,
            Duration = duration,
            AutoReverse = true,
            RepeatBehavior = WpfAnimation.RepeatBehavior.Forever,
            EasingFunction = easing,
        };
        CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
        CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleX.Clone());
    }

    private void StopIdleBreathing()
    {
        CharacterTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
        CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        CharacterTranslateTransform.Y = 0;
        CharacterScaleTransform.ScaleX = 1;
        CharacterScaleTransform.ScaleY = 1;
    }

    private void StartIdleAnimation()
    {
        if (!CanIdleAnimate())
        {
            return;
        }

        StartIdleBreathing();
        _idleFrameTimer.Interval = PetFrameTiming.GetDuration(
            _idleAction,
            _animationController.IdleFrameIndex,
            DefaultIdleFrameInterval);
        _idleFrameTimer.Start();
    }

    private void StopIdleAnimation()
    {
        _idleFrameTimer.Stop();
        StopIdleBreathing();
    }

    private void AdvanceIdleFrame()
    {
        if (!CanIdleAnimate())
        {
            StopIdleAnimation();
            return;
        }

        _animationController.AdvanceIdle(_idleFrames.Count);
        if (!_animationController.IsBlinking)
        {
            CharacterImage.Source = GetCurrentIdleFrame();
        }

        _idleFrameTimer.Interval = PetFrameTiming.GetDuration(
            _idleAction,
            _animationController.IdleFrameIndex,
            DefaultIdleFrameInterval);
    }

    private ImageSource GetCurrentIdleFrame()
    {
        return _idleFrames.Count == 0 ? _defaultCharacter : _idleFrames[_animationController.IdleFrameIndex];
    }

    private bool CanIdleAnimate()
    {
        return PetAnimationTimings.CharacterFrameAnimationEnabled
            && _animationController.CanRunIdle(GetPassiveAnimationContext(), _idleFrames.Count);
    }

    private void ScheduleNextBlink()
    {
        _blinkScheduleTimer.Stop();
        if (!CanBlink())
        {
            return;
        }

        var minDelay = _blinkAction.MinScheduleDelay ?? DefaultBlinkMinScheduleDelay;
        var maxDelay = _blinkAction.MaxScheduleDelay ?? DefaultBlinkMaxScheduleDelay;
        var minMs = (int)minDelay.TotalMilliseconds;
        var maxMs = (int)maxDelay.TotalMilliseconds;
        _blinkScheduleTimer.Interval = TimeSpan.FromMilliseconds(_blinkRandom.Next(minMs, maxMs + 1));
        _blinkScheduleTimer.Start();
    }

    private void BeginBlink()
    {
        _blinkScheduleTimer.Stop();
        if (!CanBlink())
        {
            return;
        }

        if (!_animationController.BeginBlink(_blinkFrames.Count))
        {
            return;
        }

        CharacterImage.Source = _blinkFrames[_animationController.BlinkFrameIndex];
        _blinkFrameTimer.Interval = PetFrameTiming.GetDuration(
            _blinkAction,
            _animationController.BlinkFrameIndex,
            DefaultBlinkFrameInterval);
        _blinkFrameTimer.Start();
    }

    private bool CanBlink()
    {
        return PetAnimationTimings.BlinkFrameAnimationEnabled
            && _animationController.CanBeginBlink(GetPassiveAnimationContext(), _blinkFrames.Count);
    }

    private void AdvanceBlinkFrame()
    {
        if (_isDragging || !_animationController.IsBlinking)
        {
            StopBlinkAnimation();
            return;
        }

        var advance = _animationController.AdvanceBlink(_blinkFrames.Count);
        if (advance.Completed)
        {
            StopBlinkAnimation();
            CharacterImage.Source = GetCurrentIdleFrame();
            ScheduleNextBlink();
            return;
        }

        CharacterImage.Source = _blinkFrames[advance.FrameIndex];
        _blinkFrameTimer.Interval = PetFrameTiming.GetDuration(
            _blinkAction,
            advance.FrameIndex,
            DefaultBlinkFrameInterval);
    }

    private void StopBlinkAnimation()
    {
        _blinkScheduleTimer.Stop();
        _blinkFrameTimer.Stop();
        _animationController.StopBlink();
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
