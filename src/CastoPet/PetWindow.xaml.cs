using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using CastoPet.Core;
using WpfControls = System.Windows.Controls;
using WpfInput = System.Windows.Input;
using WpfAnimation = System.Windows.Media.Animation;
using WpfColor = System.Windows.Media.Color;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfDataObject = System.Windows.IDataObject;
using WpfPoint = System.Windows.Point;
using WpfShapes = System.Windows.Shapes;
using WpfSize = System.Windows.Size;

namespace CastoPet;

public partial class PetWindow : Window
{
    private static readonly TimeSpan DefaultIdleFrameInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan DefaultBlinkFrameInterval = TimeSpan.FromMilliseconds(90);
    private static readonly TimeSpan DefaultPettingFrameInterval = TimeSpan.FromMilliseconds(80);
    private static readonly TimeSpan DefaultExpressionTransitionFrameInterval = TimeSpan.FromMilliseconds(55);
    private static readonly TimeSpan DefaultBlinkMinScheduleDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DefaultBlinkMaxScheduleDelay = TimeSpan.FromSeconds(7);
    private static readonly TimeSpan RadialWheelPointerProbeInterval = TimeSpan.FromMilliseconds(16);
    private static readonly TimeSpan RadialWheelHoldRevealDelay = TimeSpan.FromMilliseconds(120);
    private static readonly TimeSpan PettingFallbackDuration = TimeSpan.FromMilliseconds(240);
    private static readonly string[] UrlDropFormats = ["UniformResourceLocatorW", "UniformResourceLocator"];
    private const double DefaultMoveDistancePerFrame = 10;
    private const double DefaultMoveBaseSpeedPixelsPerSecond = 90;
    private const double DefaultMoveMinSpeedPixelsPerSecond = 80;
    private const double DefaultMoveMaxSpeedPixelsPerSecond = 105;
    private const double MinimumLeftDragThreshold = 6;
    private const double RightWheelDragThreshold = 14;
    private const double HoldIndicatorSize = 58;
    private const double HoldIndicatorRadius = 22;

    private readonly LoggingService _logger;
    private readonly PetRuntimeState _runtimeState = new();
    private readonly PetAnimationController _animationController = new();
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
    private readonly IReadOnlyList<ImageSource> _expressionTransitionInFrames;
    private readonly IReadOnlyList<ImageSource> _expressionTransitionOutFrames;
    private readonly IReadOnlyList<ImageSource> _moveFrames;
    private readonly IReadOnlyDictionary<string, PetExpressionAsset> _expressionAssetsById;
    private readonly WheelCatalogService _wheelCatalogService;
    private WheelCatalog _wheelCatalog;
    private readonly ShortcutService _shortcutService;
    private readonly ShortcutDropHandler _shortcutDrops;
    private readonly ShortcutLauncher _shortcutLauncher;
    private RadialWheelController _radialWheelController;
    private readonly ImageSource? _inputReactiveBase;
    private readonly PetActionDefinition _idleAction;
    private readonly PetActionDefinition _moveAction;
    private readonly PetActionDefinition _blinkAction;
    private readonly PetActionDefinition? _pettingAction;
    private readonly PetActionDefinition? _expressionTransitionInAction;
    private readonly PetActionDefinition? _expressionTransitionOutAction;
    private readonly InputReactiveState _inputReactiveState = new();
    private readonly WindowsInputHookService _inputHookService = new();
    private readonly DispatcherTimer _inputReactiveRenderTimer;
    private readonly PetPointerGestureClassifier _pointerGestures;
    private readonly WindowsCursorService _cursorService = new();
    private readonly List<RadialWheelItemVisual> _firstRingVisuals = new();
    private readonly List<RadialWheelItemVisual> _secondRingVisuals = new();
    private readonly Random _blinkRandom = new();
    private readonly Random _movementRandom = new();
    private PetWindowSettingsSnapshot? _pendingSettings;
    private PetMovementTarget _activeMovementTarget;
    private DateTime _nextWanderDecisionUtc = DateTime.MinValue;
    private WpfPoint _requestedRadialWheelOrigin;
    private WpfPoint _radialWheelOrigin;
    private WpfPoint _lastRadialWheelPointer;
    private bool _applySettingsOnSourceInitialized;
    private bool _isClickThrough;
    private bool _isDragging;
    private bool _isRadialWheelOpen;
    private bool _hasRadialWheelPointer;
    private bool _hasRadialWheelPointerEntered;
    private bool _activeMovementEnabled;
    private bool _pushCursorEnabled;
    private bool _inputReactiveModeEnabled;
    private bool _hasActiveMovementTarget;
    private string _secondRingContentKey = "closed";
    private PetExpressionAsset? _pendingExpressionAsset;
    private PetExpressionAsset? _activeExpressionAsset;
    private IReadOnlyList<ImageSource> _activeExpressionTransitionFrames = Array.Empty<ImageSource>();
    private bool _activeExpressionUsesSpecificTransition;
    private TimeSpan? _lastActiveMovementRenderTime;
    private TimeSpan? _lastManualCursorMovementTime;
    private TimeSpan? _cursorPushStartedAt;
    private int _activeMovementVisualDirection;
    private bool _dragMovementVisualApplied;
    private double _lastMovementDeltaX;
    private double _logicalLeft;
    private double _logicalTop;
    private double _moveFrameDistanceAccumulator;
    private double? _expectedCursorX;
    private double? _expectedCursorY;
    private int _moveFrameIndex;
    private bool _activeMovementRenderingSubscribed;
    private bool _radialWheelHoldRenderingSubscribed;
    private WpfControls.ContextMenu? _petContextMenu;

    private sealed class RadialWheelItemVisual(
        WpfShapes.Path sector,
        WpfControls.TextBlock label,
        bool isEnabled,
        RadialWheelRing ring)
    {
        public WpfShapes.Path Sector { get; } = sector;
        public WpfControls.TextBlock Label { get; } = label;
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
        _wheelCatalog = _wheelCatalogService.Current;
        _shortcutService = shortcutService ?? throw new ArgumentNullException(nameof(shortcutService));
        _shortcutDrops = shortcutDrops ?? throw new ArgumentNullException(nameof(shortcutDrops));
        _shortcutLauncher = shortcutLauncher ?? throw new ArgumentNullException(nameof(shortcutLauncher));
        _radialWheelController = new RadialWheelController(_wheelCatalog);
        _idleAction = assets.Skin.GetRequiredAction(PetActionKind.Idle);
        _moveAction = assets.Skin.GetRequiredAction(PetActionKind.Move);
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
        _idleFrameTimer = new DispatcherTimer { Interval = GetActionFrameInterval(_idleAction, DefaultIdleFrameInterval) };
        _idleFrameTimer.Tick += (_, _) => AdvanceIdleFrame();
        _blinkScheduleTimer = new DispatcherTimer();
        _blinkScheduleTimer.Tick += (_, _) => BeginBlink();
        _blinkFrameTimer = new DispatcherTimer { Interval = GetActionFrameInterval(_blinkAction, DefaultBlinkFrameInterval) };
        _blinkFrameTimer.Tick += (_, _) => AdvanceBlinkFrame();
        _pettingFrameTimer = new DispatcherTimer { Interval = GetActionFrameInterval(_pettingAction, DefaultPettingFrameInterval) };
        _pettingFrameTimer.Tick += (_, _) => AdvancePettingFrame();
        _dragRestoreTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _dragRestoreTimer.Tick += (_, _) => RestoreAfterDrag();
        _radialWheelPointerProbeTimer = new DispatcherTimer { Interval = RadialWheelPointerProbeInterval };
        _radialWheelPointerProbeTimer.Tick += (_, _) => ProbeRadialWheelPointer();
        _temporaryExpressionTimer = new DispatcherTimer { Interval = ExpressionWheelCatalog.ExpressionDuration };
        _temporaryExpressionTimer.Tick += (_, _) => RestoreAfterTemporaryExpression();
        _expressionTransitionFrameTimer = new DispatcherTimer { Interval = GetActionFrameInterval(_expressionTransitionInAction, DefaultExpressionTransitionFrameInterval) };
        _expressionTransitionFrameTimer.Tick += (_, _) => AdvanceExpressionTransitionFrame();
        _activeMovementProbeTimer = new DispatcherTimer { Interval = PetAnimationTimings.ActiveMovementProbeInterval };
        _activeMovementProbeTimer.Tick += (_, _) => ProbeActiveMovement();
        _inputReactiveRenderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _inputReactiveRenderTimer.Tick += (_, _) => RenderInputReactiveHighlights();
        _inputHookService.InputReceived += OnInputReactiveInputReceived;
        _pointerGestures = new PetPointerGestureClassifier(
            Math.Max(MinimumLeftDragThreshold, SystemParameters.MinimumHorizontalDragDistance),
            Math.Max(MinimumLeftDragThreshold, SystemParameters.MinimumVerticalDragDistance),
            RightWheelDragThreshold,
            WheelCatalog.HoldDelay);

        try
        {
            _defaultCharacter = assets.LoadDefaultCharacter();
            _draggingCharacter = assets.LoadDraggingCharacter();
            _idleFrames = assets.LoadIdleFrames();
            _blinkFrames = assets.LoadBlinkFrames();
            _pettingFrames = assets.LoadPettingFrames();
            _moveFrames = assets.LoadMoveFrames();
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
        Closed += (_, _) =>
        {
            _wheelCatalogService.Changed -= OnWheelCatalogChanged;
            StopInputReactiveMode();
            CancelPendingPointerGesture();
            StopPetting(restoreIdle: false);
            CloseRadialWheel(cancelController: true, restoreAnimation: false);
            _inputHookService.InputReceived -= OnInputReactiveInputReceived;
            _inputHookService.Dispose();
        };
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseRightButtonDown += OnMouseRightButtonDown;
        MouseRightButtonUp += OnMouseRightButtonUp;
        MouseMove += OnMouseMove;
        LostMouseCapture += OnLostMouseCapture;
        Deactivated += OnWindowDeactivated;
        PreviewKeyDown += OnPreviewKeyDown;
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
        commands.SettingsChanged += () => RefreshContextMenuChecks(menu);
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
            && !_isRadialWheelOpen
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
            HasActiveMovementTarget: _hasActiveMovementTarget,
            IsRadialWheelOpen: _isRadialWheelOpen,
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
        _hasActiveMovementTarget = false;
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
            && !_isRadialWheelOpen
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
        _pointerGestures.Press(PetPointerButton.Left, position.X, position.Y, DateTimeOffset.UtcNow);
        if (_pointerGestures.State != PetPointerGestureState.LeftPending)
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
        var intent = _pointerGestures.Release(
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
            _pointerGestures.Cancel();
        }
    }

    private void OnMouseRightButtonDown(object sender, WpfInput.MouseButtonEventArgs e)
    {
        if (_isClickThrough || e.ButtonState != WpfInput.MouseButtonState.Pressed)
        {
            return;
        }

        if (_isRadialWheelOpen)
        {
            e.Handled = true;
            return;
        }

        var position = e.GetPosition(RootGrid);
        _pointerGestures.Press(PetPointerButton.Right, position.X, position.Y, DateTimeOffset.UtcNow);
        if (_pointerGestures.State != PetPointerGestureState.RightPending)
        {
            CancelPendingPointerGesture();
            e.Handled = true;
            return;
        }

        _requestedRadialWheelOrigin = ToScreenPoint(position);
        CaptureMouse();
        StopRadialWheelHoldRendering();
        if (_wheelCatalog.Categories.Count > 0)
        {
            StartRadialWheelHoldRendering();
        }
        e.Handled = true;
    }

    private void OnMouseMove(object sender, WpfInput.MouseEventArgs e)
    {
        var position = e.GetPosition(RootGrid);
        if (_isRadialWheelOpen)
        {
            UpdateRadialWheelPointer(position, DateTimeOffset.UtcNow);
            return;
        }

        if (_pointerGestures.State == PetPointerGestureState.LeftPending)
        {
            if (e.LeftButton != WpfInput.MouseButtonState.Pressed)
            {
                CancelPendingPointerGesture();
                return;
            }

            if (_pointerGestures.Move(position.X, position.Y, DateTimeOffset.UtcNow) == PetPointerIntent.Drag)
            {
                BeginDragFromGesture();
            }

            return;
        }

        if (_pointerGestures.State != PetPointerGestureState.RightPending)
        {
            return;
        }

        if (e.RightButton != WpfInput.MouseButtonState.Pressed)
        {
            CancelPendingPointerGesture();
            return;
        }

        if (_wheelCatalog.Categories.Count == 0)
        {
            return;
        }

        if (_pointerGestures.Move(position.X, position.Y, DateTimeOffset.UtcNow) == PetPointerIntent.RadialWheel)
        {
            OpenRadialWheel();
        }
    }

    private void OnMouseRightButtonUp(object sender, WpfInput.MouseButtonEventArgs e)
    {
        StopRadialWheelHoldRendering();
        HideRadialWheelHoldFeedback();

        if (!_isRadialWheelOpen)
        {
            var position = e.GetPosition(RootGrid);
            var intent = _pointerGestures.Release(
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
        if (!_isRadialWheelOpen)
        {
            return;
        }

        var selection = RadialWheelSelector.GetSelection(
            _lastRadialWheelPointer.X,
            _lastRadialWheelPointer.Y,
            _wheelCatalog.Categories.Count,
            _radialWheelController.VisibleSecondLevelItems.Count);
        var result = selection.Ring == RadialWheelRing.Second
            ? _radialWheelController.Release()
            : _radialWheelController.Cancel();
        _pointerGestures.Release(
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
        if (_pointerGestures.State is PetPointerGestureState.LeftPending or PetPointerGestureState.RightPending)
        {
            CancelPendingPointerGesture(releaseCapture: false);
        }
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (!_isRadialWheelOpen)
        {
            CancelPendingPointerGesture();
        }
    }

    private void OnPreviewKeyDown(object sender, WpfInput.KeyEventArgs e)
    {
        if (!_isRadialWheelOpen || e.Key != WpfInput.Key.Escape)
        {
            return;
        }

        _radialWheelController.Cancel();
        CloseRadialWheel(cancelController: false);
        e.Handled = true;
    }

    private void BeginDrag()
    {
        StopPetting(restoreIdle: false);
        CancelTemporaryExpression();
        StopInputReactiveMode(restoreIdle: false);
        StopActiveMovementRendering();
        _hasActiveMovementTarget = false;
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
        _hasActiveMovementTarget = false;
        _dragRestoreTimer.Stop();
        StopIdleAnimation();
        StopBlinkAnimation();
        ResetMoveFrameState();
        ResetActiveMovementVisual();
        ResetCharacterTransitionAnimations();

        if (_pettingFrames.Count > 0)
        {
            CharacterImage.Source = _pettingFrames[0];
            _pettingFrameTimer.Interval = GetActionFrameInterval(_pettingAction, DefaultPettingFrameInterval);
            AnimatePettingCompression(TimeSpan.FromMilliseconds(
                _pettingFrameTimer.Interval.TotalMilliseconds * _pettingFrames.Count / 2));
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
            && !_isRadialWheelOpen
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
        _hasActiveMovementTarget = false;
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

        if (DateTime.UtcNow >= _nextWanderDecisionUtc)
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

        _logicalLeft = Left;
        _logicalTop = Top;
        _lastActiveMovementRenderTime = null;
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
        _lastActiveMovementRenderTime = null;
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

        if (_lastActiveMovementRenderTime is null)
        {
            _lastActiveMovementRenderTime = renderingTime;
            _logicalLeft = Left;
            _logicalTop = Top;
            return;
        }

        var elapsed = renderingTime - _lastActiveMovementRenderTime.Value;
        _lastActiveMovementRenderTime = renderingTime;
        if (elapsed <= TimeSpan.Zero)
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
                if (_hasActiveMovementTarget)
                {
                    _hasActiveMovementTarget = false;
                    _nextWanderDecisionUtc = DateTime.UtcNow.AddMilliseconds(_movementRandom.Next(1200, 2600));
                    ResetMoveFrameState();
                    ResetActiveMovementVisual();
                    ScheduleNextBlink();
                }

                StopActiveMovementRendering();
                return;
            }

            if (!_hasActiveMovementTarget)
            {
                StopIdleAnimation();
                StopBlinkAnimation();
            }

            _activeMovementTarget = mouseApproachTarget;
            _hasActiveMovementTarget = true;
        }
        else if (!_hasActiveMovementTarget || PetMovementPlanner.IsClose(Left, Top, _activeMovementTarget))
        {
            ChooseWanderTarget(width, height, bounds);
        }

        if (!_hasActiveMovementTarget)
        {
            ResetActiveMovementVisual();
            StopActiveMovementRendering();
            return;
        }

        if (PetMovementPlanner.IsClose(Left, Top, _activeMovementTarget))
        {
            _hasActiveMovementTarget = false;
            _nextWanderDecisionUtc = DateTime.UtcNow.AddMilliseconds(_movementRandom.Next(1200, 2600));
            ResetMoveFrameState();
            ResetActiveMovementVisual();
            ScheduleNextBlink();
            StopActiveMovementRendering();
            return;
        }

        var dx = _activeMovementTarget.Left - _logicalLeft;
        var dy = _activeMovementTarget.Top - _logicalTop;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        var step = CalculateMoveStep(_moveAction, elapsed, distance);
        if (step <= 0 || distance <= 0.001)
        {
            return;
        }

        var ratio = step / distance;
        var nextLeft = _logicalLeft + dx * ratio;
        var nextTop = _logicalTop + dy * ratio;
        var movementDeltaX = nextLeft - _logicalLeft;
        var movementDeltaY = nextTop - _logicalTop;

        _lastMovementDeltaX = movementDeltaX;
        _logicalLeft = nextLeft;
        _logicalTop = nextTop;
        Left = Math.Round(_logicalLeft);
        Top = Math.Round(_logicalTop);
        _runtimeState.SetRuntimePosition(Left, Top);
        AdvanceMoveFrame(step);
        ApplyActiveMovementVisual();
        TryPushCursor(renderingTime, movementDeltaX, movementDeltaY);

        if (PetMovementPlanner.IsClose(Left, Top, _activeMovementTarget))
        {
            _hasActiveMovementTarget = false;
            _nextWanderDecisionUtc = DateTime.UtcNow.AddMilliseconds(_movementRandom.Next(1200, 2600));
            ResetMoveFrameState();
            ScheduleNextBlink();
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
        if (DateTime.UtcNow < _nextWanderDecisionUtc)
        {
            return;
        }

        StopIdleAnimation();
        StopBlinkAnimation();

        const double range = 160;
        var targetLeft = Left + _movementRandom.NextDouble() * range * 2 - range;
        var targetTop = Top + _movementRandom.NextDouble() * range * 2 - range;

        _activeMovementTarget = PetMovementPlanner.ClampTarget(targetLeft, targetTop, width, height, bounds);
        _hasActiveMovementTarget = true;
    }

    private void AdvanceMoveFrame(double distance)
    {
        if (_moveFrames.Count == 0 || distance <= 0)
        {
            return;
        }

        _moveFrameDistanceAccumulator += distance;
        var distancePerFrame = _moveAction.DistancePerFrame ?? DefaultMoveDistancePerFrame;
        while (_moveFrameDistanceAccumulator >= distancePerFrame)
        {
            _moveFrameDistanceAccumulator -= distancePerFrame;
            _moveFrameIndex = (_moveFrameIndex + 1) % _moveFrames.Count;
            CharacterImage.Source = _moveFrames[_moveFrameIndex];
        }
    }

    private void ResetMoveFrameState()
    {
        _moveFrameDistanceAccumulator = 0;
        _moveFrameIndex = 0;

        if (!_isDragging && !_temporaryExpressionTimer.IsEnabled &&
            _animationController.ExpressionTransitionMode == PetExpressionTransitionMode.None)
        {
            CharacterImage.Source = GetCurrentIdleFrame();
            StartIdleAnimation();
        }
    }

    private void ApplyActiveMovementVisual()
    {
        var direction = _lastMovementDeltaX < 0 ? -1 : 1;
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
        if (_isRadialWheelOpen || _radialWheelController.IsOpen)
        {
            CloseRadialWheel(cancelController: true);
        }

        _wheelCatalog = _wheelCatalogService.Current;
        _radialWheelController = new RadialWheelController(_wheelCatalog);
        _secondRingContentKey = "closed";
        SecondRingSurface.Children.Clear();
        _secondRingVisuals.Clear();
        SecondRingSurface.Visibility = Visibility.Collapsed;
        BuildFirstRadialWheelRing();
    }

    private void BuildFirstRadialWheelRing()
    {
        FirstRingSurface.Children.Clear();
        _firstRingVisuals.Clear();
        for (var index = 0; index < _wheelCatalog.Categories.Count; index++)
        {
            var category = _wheelCatalog.Categories[index];
            _firstRingVisuals.Add(AddRadialWheelItem(
                FirstRingSurface,
                category.DisplayName,
                index,
                _wheelCatalog.Categories.Count,
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
        var items = _radialWheelController.VisibleSecondLevelItems;
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
                isSecondRing: true));
        }

        SecondRingSurface.Visibility = _radialWheelController.IsSecondLevelOpen
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private RadialWheelItemVisual AddRadialWheelItem(
        WpfControls.Canvas surface,
        string displayName,
        int index,
        int count,
        double innerRadius,
        double outerRadius,
        bool isEnabled,
        bool isSecondRing)
    {
        var ring = isSecondRing ? RadialWheelRing.Second : RadialWheelRing.First;
        var panel = new WpfControls.Canvas
        {
            Width = RadialWheelSurface.Width,
            Height = RadialWheelSurface.Height,
            IsHitTestVisible = false,
            Opacity = 1,
        };
        var sector = new WpfShapes.Path
        {
            Data = CreateRadialWheelSectorGeometry(index, count, innerRadius, outerRadius),
            Fill = CreateRadialWheelFillBrush(RadialWheelStyle.GetNormalFill(ring, isEnabled), isSelected: false),
            Stroke = CreateRadialWheelStrokeBrush(RadialWheelStyle.NormalStroke),
            StrokeThickness = RadialWheelStyle.NormalStrokeThickness,
        };
        panel.Children.Add(sector);

        var label = new WpfControls.TextBlock
        {
            Text = displayName,
            Foreground = new SolidColorBrush(WpfColor.FromArgb(242, 247, 244, 250)),
            FontSize = isSecondRing ? 11.5 : 13,
            FontWeight = FontWeights.Medium,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Width = isSecondRing ? 88 : 96,
            MaxHeight = 40,
            Opacity = isEnabled ? 0.9 : 0.58,
            RenderTransformOrigin = new WpfPoint(0.5, 0.5),
            RenderTransform = new ScaleTransform(1, 1),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = WpfColor.FromArgb(RadialWheelStyle.LabelShadowAlpha, 40, 25, 68),
                BlurRadius = RadialWheelStyle.LabelShadowBlurRadius,
                ShadowDepth = 0,
                Opacity = RadialWheelStyle.LabelShadowOpacity,
            },
        };
        var center = RadialWheelSurface.Width / 2;
        var labelRadius = (innerRadius + outerRadius) / 2;
        var labelAngle = count == 1
            ? -Math.PI / 2
            : -Math.PI / 2 + (index + 0.5) * Math.Tau / count;
        var labelCenter = PointOnWheel(center, labelRadius, labelAngle);
        WpfControls.Canvas.SetLeft(label, labelCenter.X - label.Width / 2);
        WpfControls.Canvas.SetTop(label, labelCenter.Y - (isSecondRing ? 15 : 12));
        panel.Children.Add(label);
        surface.Children.Add(panel);
        return new RadialWheelItemVisual(sector, label, isEnabled, ring);
    }

    private Geometry CreateRadialWheelSectorGeometry(
        int index,
        int count,
        double innerRadius,
        double outerRadius)
    {
        var center = RadialWheelSurface.Width / 2;
        if (count == 1)
        {
            return new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new EllipseGeometry(new WpfPoint(center, center), outerRadius, outerRadius),
                new EllipseGeometry(new WpfPoint(center, center), innerRadius, innerRadius));
        }

        var step = Math.Tau / count;
        var gap = RadialWheelStyle.SectorGapRadians;
        var startAngle = -Math.PI / 2 + index * step + gap;
        var endAngle = -Math.PI / 2 + (index + 1) * step - gap;
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
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget is null
            ? devicePoint
            : source.CompositionTarget.TransformFromDevice.Transform(devicePoint);
    }

    private void UpdateRadialWheelHoldGesture()
    {
        if (_pointerGestures.State != PetPointerGestureState.RightPending
            || WpfInput.Mouse.RightButton != WpfInput.MouseButtonState.Pressed)
        {
            CancelPendingPointerGesture();
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (_pointerGestures.UpdateHold(now) == PetPointerIntent.RadialWheel)
        {
            OpenRadialWheel();
            return;
        }

        UpdateRadialWheelHoldFeedback(_pointerGestures.GetRightHoldProgress(now, RadialWheelHoldRevealDelay));
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
        _pointerGestures.Cancel();
        if (releaseCapture)
        {
            ReleasePendingMouseCapture();
        }
    }

    private void ReleasePendingMouseCapture()
    {
        if (WpfInput.Mouse.Captured == this && !_isRadialWheelOpen && !_isDragging)
        {
            ReleaseMouseCapture();
        }
    }

    private void PositionRadialWheelOverlay(WpfPoint requestedOrigin)
    {
        var halfWidth = RadialWheelSurface.Width / 2;
        var halfHeight = RadialWheelSurface.Height / 2;
        var workArea = SystemParameters.WorkArea;
        var centerX = ClampWheelCenter(requestedOrigin.X, workArea.Left + halfWidth, workArea.Right - halfWidth);
        var centerY = ClampWheelCenter(requestedOrigin.Y, workArea.Top + halfHeight, workArea.Bottom - halfHeight);
        _radialWheelOrigin = new WpfPoint(centerX, centerY);
        RadialWheelOverlay.HorizontalOffset = centerX - halfWidth;
        RadialWheelOverlay.VerticalOffset = centerY - halfHeight;
    }

    private static double ClampWheelCenter(double value, double minimum, double maximum) =>
        minimum <= maximum ? Math.Clamp(value, minimum, maximum) : (minimum + maximum) / 2;

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
        if (_wheelCatalog.Categories.Count == 0 || WpfInput.Mouse.RightButton != WpfInput.MouseButtonState.Pressed)
        {
            CancelPendingPointerGesture();
            return;
        }

        StopPetting(restoreIdle: false);
        CancelTemporaryExpression();
        StopInputReactiveMode(restoreIdle: false);
        StopIdleAnimation();
        StopBlinkAnimation();
        _isRadialWheelOpen = true;
        _hasRadialWheelPointer = false;
        _hasRadialWheelPointerEntered = false;
        _radialWheelController.Open(DateTimeOffset.UtcNow);
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
        _pointerGestures.Cancel();
        if (cancelController && _radialWheelController.IsOpen)
        {
            _radialWheelController.Cancel();
        }

        _isRadialWheelOpen = false;
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
        if (_isRadialWheelOpen && _hasRadialWheelPointer)
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
            _wheelCatalog.Categories.Count,
            _radialWheelController.VisibleSecondLevelItems.Count);
        if (!_hasRadialWheelPointerEntered && pointerRegion.Ring == RadialWheelRing.Outside)
        {
            return;
        }

        _hasRadialWheelPointerEntered = true;
        _radialWheelController.UpdatePointer(pointer.X, pointer.Y, now);
        if (!_radialWheelController.IsOpen)
        {
            CloseRadialWheel(cancelController: false);
            return;
        }

        RefreshRadialWheelVisuals(forceSecondRingRebuild: false);
    }

    private void RefreshRadialWheelVisuals(bool forceSecondRingRebuild)
    {
        var contentKey = _radialWheelController.IsSecondLevelOpen
            ? $"{_radialWheelController.SelectedCategoryIndex}:{_radialWheelController.CurrentPage}:{string.Join(',', _radialWheelController.VisibleSecondLevelItems.Select(item => item.Id))}"
            : "closed";
        if (forceSecondRingRebuild || !string.Equals(_secondRingContentKey, contentKey, StringComparison.Ordinal))
        {
            _secondRingContentKey = contentKey;
            BuildSecondRadialWheelRing();
        }

        UpdateRadialWheelSelectionVisuals(_firstRingVisuals, _radialWheelController.SelectedCategoryIndex);
        UpdateRadialWheelSelectionVisuals(_secondRingVisuals, _radialWheelController.SelectedSecondLevelIndex);
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
            var fill = isSelected
                ? RadialWheelStyle.SelectedFill
                : RadialWheelStyle.GetNormalFill(visual.Ring, visual.IsEnabled);
            visual.Sector.Fill = CreateRadialWheelFillBrush(fill, isSelected);
            visual.Sector.Stroke = CreateRadialWheelStrokeBrush(
                isSelected ? RadialWheelStyle.SelectedStroke : RadialWheelStyle.NormalStroke);
            visual.Sector.StrokeThickness = isSelected
                ? RadialWheelStyle.SelectedStrokeThickness
                : RadialWheelStyle.NormalStrokeThickness;
            visual.Sector.Effect = isSelected
                ? new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = ToWpfColor(RadialWheelStyle.SelectedGlow),
                    BlurRadius = RadialWheelStyle.SelectedGlowBlurRadius,
                    ShadowDepth = 0,
                    Opacity = RadialWheelStyle.SelectedGlowOpacity,
                }
                : null;
            visual.Label.Opacity = isSelected ? 1 : visual.IsEnabled ? 0.9 : 0.58;
            visual.Label.FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Medium;
            if (visual.Label.RenderTransform is not ScaleTransform scaleTransform)
            {
                scaleTransform = new ScaleTransform(1, 1);
                visual.Label.RenderTransform = scaleTransform;
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
            e.Effects = ContainsSupportedDropFormat(e.Data)
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
            var paths = ExtractDroppedPaths(e.Data);
            var textValues = ExtractDroppedTextValues(e.Data);
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

    private static bool ContainsSupportedDropFormat(WpfDataObject data)
    {
        if (data.GetDataPresent(WpfDataFormats.FileDrop, autoConvert: true) ||
            data.GetDataPresent(WpfDataFormats.UnicodeText, autoConvert: true) ||
            data.GetDataPresent(WpfDataFormats.Text, autoConvert: true) ||
            data.GetDataPresent(WpfDataFormats.StringFormat, autoConvert: true))
        {
            return true;
        }

        return UrlDropFormats.Any(format => data.GetDataPresent(format, autoConvert: true));
    }

    private static IReadOnlyList<string> ExtractDroppedPaths(WpfDataObject data)
    {
        if (!data.GetDataPresent(WpfDataFormats.FileDrop, autoConvert: true))
        {
            return [];
        }

        return data.GetData(WpfDataFormats.FileDrop, autoConvert: true) is string[] paths
            ? paths.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray()
            : [];
    }

    private static IReadOnlyList<string> ExtractDroppedTextValues(WpfDataObject data)
    {
        string[] formats =
        [
            WpfDataFormats.UnicodeText,
            WpfDataFormats.Text,
            WpfDataFormats.StringFormat,
            .. UrlDropFormats,
        ];
        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var format in formats)
        {
            if (!data.GetDataPresent(format, autoConvert: true))
            {
                continue;
            }

            var value = ReadDroppedText(data.GetData(format, autoConvert: true), format);
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values.ToArray();
    }

    private static string? ReadDroppedText(object? value, string format)
    {
        if (value is string text)
        {
            return NormalizeDroppedText(text);
        }

        if (value is byte[] bytes)
        {
            var encoding = format.EndsWith('W') ? Encoding.Unicode : Encoding.UTF8;
            return NormalizeDroppedText(encoding.GetString(bytes));
        }

        if (value is not Stream stream)
        {
            return null;
        }

        var originalPosition = stream.CanSeek ? stream.Position : (long?)null;
        try
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            var encoding = format.EndsWith('W') ? Encoding.Unicode : Encoding.UTF8;
            using var reader = new StreamReader(
                stream,
                encoding,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: true);
            return NormalizeDroppedText(reader.ReadToEnd());
        }
        finally
        {
            if (originalPosition is long position)
            {
                stream.Position = position;
            }
        }
    }

    private static string? NormalizeDroppedText(string text)
    {
        var normalized = text.Trim('\0', ' ', '\t', '\r', '\n');
        return normalized.Length == 0 ? null : normalized;
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
        _expressionTransitionFrameTimer.Interval = _activeExpressionUsesSpecificTransition
            ? _pendingExpressionAsset.Definition.TransitionFrameInterval ?? DefaultExpressionTransitionFrameInterval
            : GetActionFrameInterval(_expressionTransitionInAction, DefaultExpressionTransitionFrameInterval);
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
        _expressionTransitionFrameTimer.Interval = _activeExpressionUsesSpecificTransition
            ? _activeExpressionAsset?.Definition.TransitionFrameInterval ?? DefaultExpressionTransitionFrameInterval
            : GetActionFrameInterval(_expressionTransitionOutAction, DefaultExpressionTransitionFrameInterval);
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

    private void ShowPendingExpression()
    {
        if (_pendingExpressionAsset is null || _isDragging || _isRadialWheelOpen)
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
        if (_isDragging || _isRadialWheelOpen)
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
        if (_isDragging || _isRadialWheelOpen)
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

    private static TimeSpan GetActionFrameInterval(PetActionDefinition? action, TimeSpan fallback)
    {
        return action?.FrameInterval ?? fallback;
    }

    private static double CalculateMoveStep(PetActionDefinition action, TimeSpan elapsed, double distanceToTarget)
    {
        if (elapsed <= TimeSpan.Zero || distanceToTarget <= 0)
        {
            return 0;
        }

        var baseSpeed = action.BaseSpeedPixelsPerSecond ?? DefaultMoveBaseSpeedPixelsPerSecond;
        var minSpeed = action.MinSpeedPixelsPerSecond ?? DefaultMoveMinSpeedPixelsPerSecond;
        var maxSpeed = action.MaxSpeedPixelsPerSecond ?? DefaultMoveMaxSpeedPixelsPerSecond;
        var speed = distanceToTarget > 240 ? maxSpeed
            : distanceToTarget < 80 ? minSpeed
            : baseSpeed;

        return Math.Min(distanceToTarget, speed * elapsed.TotalSeconds);
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
