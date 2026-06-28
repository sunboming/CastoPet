using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using CastoPet.Core;
using WpfControls = System.Windows.Controls;
using WpfInput = System.Windows.Input;
using WpfAnimation = System.Windows.Media.Animation;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;
using WpfShapes = System.Windows.Shapes;
using WpfSize = System.Windows.Size;

namespace CastoPet;

public partial class PetWindow : Window
{
    private readonly LoggingService _logger;
    private readonly PetRuntimeState _runtimeState = new();
    private readonly ImageSource _defaultCharacter;
    private readonly ImageSource _draggingCharacter;
    private readonly IReadOnlyList<ImageSource> _idleFrames;
    private readonly IReadOnlyList<ImageSource> _blinkFrames;
    private readonly DispatcherTimer _idleFrameTimer;
    private readonly DispatcherTimer _blinkScheduleTimer;
    private readonly DispatcherTimer _blinkFrameTimer;
    private readonly DispatcherTimer _dragRestoreTimer;
    private readonly DispatcherTimer _expressionWheelHoldTimer;
    private readonly DispatcherTimer _temporaryExpressionTimer;
    private readonly DispatcherTimer _expressionTransitionFrameTimer;
    private readonly IReadOnlyList<ImageSource> _expressionTransitionInFrames;
    private readonly IReadOnlyList<ImageSource> _expressionTransitionOutFrames;
    private readonly IReadOnlyList<ImageSource> _moveFrames;
    private readonly IReadOnlyDictionary<ExpressionWheelItem, ImageSource> _expressionImages;
    private readonly WindowsCursorService _cursorService = new();
    private readonly List<ExpressionWheelItem> _expressionWheelItems = new();
    private readonly List<FrameworkElement> _expressionWheelItemVisuals = new();
    private readonly List<WpfShapes.Path> _expressionWheelSectorVisuals = new();
    private readonly List<WpfControls.TextBlock> _expressionWheelLabelVisuals = new();
    private readonly List<WpfShapes.Line> _expressionWheelDividerVisuals = new();
    private readonly Random _blinkRandom = new();
    private readonly Random _movementRandom = new();
    private AppSettings? _pendingSettings;
    private PetMovementTarget _activeMovementTarget;
    private DateTime _nextWanderDecisionUtc = DateTime.MinValue;
    private WpfPoint _expressionWheelOrigin;
    private bool _applySettingsOnSourceInitialized;
    private bool _isClickThrough;
    private bool _isDragging;
    private bool _isBlinking;
    private bool _isExpressionWheelOpen;
    private bool _activeMovementEnabled;
    private bool _pushCursorEnabled;
    private bool _hasActiveMovementTarget;
    private int? _selectedExpressionWheelIndex;
    private ImageSource? _pendingExpressionImage;
    private TimeSpan? _lastActiveMovementRenderTime;
    private TimeSpan? _lastManualCursorMovementTime;
    private TimeSpan? _cursorPushStartedAt;
    private ExpressionTransitionMode _expressionTransitionMode;
    private int _expressionTransitionFrameIndex;
    private int _idleFrameIndex;
    private int _blinkFrameIndex;
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

    private enum ExpressionTransitionMode
    {
        None,
        In,
        Out,
    }

    public PetWindow(AssetService assets, LoggingService logger)
    {
        InitializeComponent();
        _logger = logger;
        _idleFrameTimer = new DispatcherTimer { Interval = IdleFrameSequence.FrameInterval };
        _idleFrameTimer.Tick += (_, _) => AdvanceIdleFrame();
        _blinkScheduleTimer = new DispatcherTimer();
        _blinkScheduleTimer.Tick += (_, _) => BeginBlink();
        _blinkFrameTimer = new DispatcherTimer { Interval = BlinkFrameSequence.FrameInterval };
        _blinkFrameTimer.Tick += (_, _) => AdvanceBlinkFrame();
        _dragRestoreTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _dragRestoreTimer.Tick += (_, _) => RestoreAfterDrag();
        _expressionWheelHoldTimer = new DispatcherTimer { Interval = ExpressionWheelCatalog.HoldDelay };
        _expressionWheelHoldTimer.Tick += (_, _) => OpenExpressionWheel();
        _temporaryExpressionTimer = new DispatcherTimer { Interval = ExpressionWheelCatalog.ExpressionDuration };
        _temporaryExpressionTimer.Tick += (_, _) => RestoreAfterTemporaryExpression();
        _expressionTransitionFrameTimer = new DispatcherTimer { Interval = ExpressionTransitionSequence.FrameInterval };
        _expressionTransitionFrameTimer.Tick += (_, _) => AdvanceExpressionTransitionFrame();

        try
        {
            _defaultCharacter = assets.LoadDefaultCharacter();
            _draggingCharacter = assets.LoadDraggingCharacter();
            _idleFrames = assets.LoadIdleFrames();
            _blinkFrames = assets.LoadBlinkFrames();
            _moveFrames = assets.LoadMoveFrames();
            _expressionTransitionInFrames = assets.LoadExpressionTransitionInFrames();
            _expressionTransitionOutFrames = assets.LoadExpressionTransitionOutFrames();
            _expressionImages = assets.LoadExpressionWheelImages();
            BuildExpressionWheel();
            CharacterImage.Source = GetCurrentIdleFrame();
        }
        catch
        {
            _defaultCharacter = CharacterImage.Source;
            _draggingCharacter = CharacterImage.Source;
            _idleFrames = Array.Empty<ImageSource>();
            _blinkFrames = Array.Empty<ImageSource>();
            _moveFrames = Array.Empty<ImageSource>();
            _expressionTransitionInFrames = Array.Empty<ImageSource>();
            _expressionTransitionOutFrames = Array.Empty<ImageSource>();
            _expressionImages = new Dictionary<ExpressionWheelItem, ImageSource>();
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
            ScheduleNextBlink();
            UpdateActiveMovementTimer();
        };
        IsVisibleChanged += (_, _) => UpdateActiveMovementTimer();
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseRightButtonDown += OnMouseRightButtonDown;
        MouseRightButtonUp += OnMouseRightButtonUp;
        MouseMove += OnMouseMove;
    }

    public void ApplySettings(AppSettings settings)
    {
        Topmost = settings.Topmost;
        ShowInTaskbar = settings.ShowInTaskbar;
        _isClickThrough = settings.ClickThrough;
        _activeMovementEnabled = settings.ActiveMovement;
        _pushCursorEnabled = settings.PushCursor;
        UpdateActiveMovementTimer();

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
        menu.Items.Add(CreateCheckedMenuItem(TrayService.ActiveMovementText, () => commands.Settings.ActiveMovement, commands.ToggleActiveMovement));
        menu.Items.Add(CreateCheckedMenuItem(TrayService.PushCursorText, () => commands.Settings.PushCursor, commands.TogglePushCursor));
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

    private void OnMouseLeftButtonDown(object sender, WpfInput.MouseButtonEventArgs e)
    {
        if (_isClickThrough || e.ButtonState != WpfInput.MouseButtonState.Pressed)
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

    private void OnMouseRightButtonDown(object sender, WpfInput.MouseButtonEventArgs e)
    {
        if (_isClickThrough || e.ButtonState != WpfInput.MouseButtonState.Pressed || _expressionWheelItems.Count == 0)
        {
            return;
        }

        _expressionWheelOrigin = e.GetPosition(RootGrid);
        _selectedExpressionWheelIndex = null;
        _expressionWheelHoldTimer.Stop();
        _expressionWheelHoldTimer.Start();
    }

    private void OnMouseMove(object sender, WpfInput.MouseEventArgs e)
    {
        if (!_isExpressionWheelOpen)
        {
            return;
        }

        UpdateExpressionWheelSelection(e.GetPosition(RootGrid));
    }

    private void OnMouseRightButtonUp(object sender, WpfInput.MouseButtonEventArgs e)
    {
        _expressionWheelHoldTimer.Stop();

        if (!_isExpressionWheelOpen)
        {
            return;
        }

        UpdateExpressionWheelSelection(e.GetPosition(RootGrid));
        var selectedIndex = _selectedExpressionWheelIndex;
        CloseExpressionWheel();
        ReleaseMouseCapture();
        e.Handled = true;

        if (selectedIndex is int index)
        {
            ApplyTemporaryExpression(index);
        }
    }

    private void BeginDrag()
    {
        CancelTemporaryExpression();
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
        UpdateActiveMovementTimer();
    }

    private void RestoreAfterDrag()
    {
        _dragRestoreTimer.Stop();
        _idleFrameIndex = 0;
        ResetActiveMovementVisual();
        CharacterImage.Source = GetCurrentIdleFrame();
        StartIdleAnimation();
        ScheduleNextBlink();
        UpdateActiveMovementTimer();
    }

    private bool CanRunActiveMovement()
    {
        return _activeMovementEnabled
            && IsVisible
            && !_isClickThrough
            && !_isDragging
            && !_dragRestoreTimer.IsEnabled
            && !_isExpressionWheelOpen
            && !_temporaryExpressionTimer.IsEnabled
            && _expressionTransitionMode == ExpressionTransitionMode.None;
    }

    private void UpdateActiveMovementTimer()
    {
        if (CanRunActiveMovement())
        {
            StartActiveMovementRendering();
            return;
        }

        StopActiveMovementRendering();
        _hasActiveMovementTarget = false;
        ResetMoveFrameState();
        ResetActiveMovementVisual();
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
            return;
        }

        if (PetMovementPlanner.IsClose(Left, Top, _activeMovementTarget))
        {
            _hasActiveMovementTarget = false;
            _nextWanderDecisionUtc = DateTime.UtcNow.AddMilliseconds(_movementRandom.Next(1200, 2600));
            ResetMoveFrameState();
            ResetActiveMovementVisual();
            ScheduleNextBlink();
            return;
        }

        var dx = _activeMovementTarget.Left - _logicalLeft;
        var dy = _activeMovementTarget.Top - _logicalTop;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        var step = MoveFrameSequence.StepDistance(elapsed, distance);
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
        while (_moveFrameDistanceAccumulator >= MoveFrameSequence.DistancePerFrame)
        {
            _moveFrameDistanceAccumulator -= MoveFrameSequence.DistancePerFrame;
            _moveFrameIndex = (_moveFrameIndex + 1) % _moveFrames.Count;
            CharacterImage.Source = _moveFrames[_moveFrameIndex];
        }
    }

    private void ResetMoveFrameState()
    {
        _moveFrameDistanceAccumulator = 0;
        _moveFrameIndex = 0;

        if (!_isDragging && !_temporaryExpressionTimer.IsEnabled && _expressionTransitionMode == ExpressionTransitionMode.None)
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
        if (_isDragging || _temporaryExpressionTimer.IsEnabled || _expressionTransitionMode != ExpressionTransitionMode.None)
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

    private void BuildExpressionWheel()
    {
        foreach (var item in ExpressionWheelCatalog.Items)
        {
            if (!_expressionImages.ContainsKey(item))
            {
                continue;
            }

            var visual = CreateExpressionWheelItemVisual(item);
            _expressionWheelItems.Add(item);
            _expressionWheelItemVisuals.Add(visual);
            ExpressionWheelSurface.Children.Add(visual);
        }

        PositionExpressionWheelItems();
        BuildExpressionWheelDividers();
    }

    private FrameworkElement CreateExpressionWheelItemVisual(ExpressionWheelItem item)
    {
        var itemIndex = _expressionWheelItems.Count;
        var itemCount = _expressionImages.Count;
        var panel = new WpfControls.Canvas
        {
            Width = ExpressionWheelSurface.Width,
            Height = ExpressionWheelSurface.Height,
            RenderTransformOrigin = new WpfPoint(0.5, 0.5),
            RenderTransform = new ScaleTransform(1, 1),
            Opacity = 1,
        };

        var sector = new WpfShapes.Path
        {
            Data = CreateExpressionWheelSectorGeometry(itemIndex, itemCount),
            Fill = new SolidColorBrush(WpfColor.FromArgb(78, 67, 43, 111)),
            Stroke = new SolidColorBrush(WpfColor.FromArgb(72, 234, 224, 255)),
            StrokeThickness = 0.75,
        };
        panel.Children.Add(sector);
        _expressionWheelSectorVisuals.Add(sector);

        var label = new WpfControls.TextBlock
        {
            Text = item.Label,
            Foreground = new SolidColorBrush(WpfColor.FromArgb(224, 255, 255, 255)),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Width = 78,
            Opacity = 0.78,
            RenderTransformOrigin = new WpfPoint(0.5, 0.5),
            RenderTransform = new ScaleTransform(1, 1),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = WpfColor.FromArgb(160, 45, 28, 72),
                BlurRadius = 7,
                ShadowDepth = 0,
                Opacity = 0.75,
            },
        };
        panel.Children.Add(label);
        _expressionWheelLabelVisuals.Add(label);

        return panel;
    }

    private void PositionExpressionWheelItems()
    {
        var center = ExpressionWheelSurface.Width / 2;
        var radius = 96d;

        for (var index = 0; index < _expressionWheelItemVisuals.Count; index++)
        {
            WpfControls.Canvas.SetLeft(_expressionWheelItemVisuals[index], 0);
            WpfControls.Canvas.SetTop(_expressionWheelItemVisuals[index], 0);

            var angle = -Math.PI / 2 + index * 2 * Math.PI / _expressionWheelItemVisuals.Count;
            var label = _expressionWheelLabelVisuals[index];
            var x = center + Math.Cos(angle) * radius - label.Width / 2;
            var y = center + Math.Sin(angle) * radius - 10;
            WpfControls.Canvas.SetLeft(label, x);
            WpfControls.Canvas.SetTop(label, y);
        }
    }

    private void BuildExpressionWheelDividers()
    {
        var count = _expressionWheelItems.Count;
        if (count == 0)
        {
            return;
        }

        var center = ExpressionWheelSurface.Width / 2;
        var innerRadius = ExpressionWheelCatalog.WheelInnerDiameter / 2;
        var outerRadius = ExpressionWheelCatalog.WheelOuterDiameter / 2;
        var halfStep = Math.PI / count;

        for (var index = 0; index < count; index++)
        {
            var angle = -Math.PI / 2 - halfStep + index * 2 * Math.PI / count;
            var inner = PointOnWheel(center, innerRadius, angle);
            var outer = PointOnWheel(center, outerRadius, angle);
            var divider = new WpfShapes.Line
            {
                X1 = inner.X,
                Y1 = inner.Y,
                X2 = outer.X,
                Y2 = outer.Y,
                Stroke = new SolidColorBrush(WpfColor.FromArgb(120, 233, 220, 255)),
                StrokeThickness = 1,
                SnapsToDevicePixels = true,
            };
            _expressionWheelDividerVisuals.Add(divider);
            ExpressionWheelSurface.Children.Add(divider);
        }
    }

    private Geometry CreateExpressionWheelSectorGeometry(int index, int count)
    {
        var center = ExpressionWheelSurface.Width / 2;
        var outerRadius = ExpressionWheelCatalog.WheelOuterDiameter / 2;
        var innerRadius = ExpressionWheelCatalog.WheelInnerDiameter / 2;
        var step = 2 * Math.PI / count;
        var gap = 0.012;
        var startAngle = -Math.PI / 2 + index * step - step / 2 + gap;
        var endAngle = -Math.PI / 2 + index * step + step / 2 - gap;
        var outerStart = PointOnWheel(center, outerRadius, startAngle);
        var outerEnd = PointOnWheel(center, outerRadius, endAngle);
        var innerEnd = PointOnWheel(center, innerRadius, endAngle);
        var innerStart = PointOnWheel(center, innerRadius, startAngle);

        var figure = new PathFigure
        {
            StartPoint = outerStart,
            IsClosed = true,
        };
        figure.Segments.Add(new ArcSegment(outerEnd, new WpfSize(outerRadius, outerRadius), 0, false, SweepDirection.Clockwise, true));
        figure.Segments.Add(new LineSegment(innerEnd, true));
        figure.Segments.Add(new ArcSegment(innerStart, new WpfSize(innerRadius, innerRadius), 0, false, SweepDirection.Counterclockwise, true));

        return new PathGeometry(new[] { figure });
    }

    private static WpfPoint PointOnWheel(double center, double radius, double angle)
    {
        return new WpfPoint(center + Math.Cos(angle) * radius, center + Math.Sin(angle) * radius);
    }

    private void PositionExpressionWheelOverlay(WpfPoint origin)
    {
        var maxLeft = Math.Max(0, RootGrid.ActualWidth - ExpressionWheelSurface.Width);
        var maxTop = Math.Max(0, RootGrid.ActualHeight - ExpressionWheelSurface.Height);
        var left = Math.Clamp(origin.X - ExpressionWheelSurface.Width / 2, 0, maxLeft);
        var top = Math.Clamp(origin.Y - ExpressionWheelSurface.Height / 2, 0, maxTop);
        WpfControls.Canvas.SetLeft(ExpressionWheelSurface, left);
        WpfControls.Canvas.SetTop(ExpressionWheelSurface, top);
        _expressionWheelOrigin = new WpfPoint(left + ExpressionWheelSurface.Width / 2, top + ExpressionWheelSurface.Height / 2);
    }

    private void AnimateExpressionWheelOpen()
    {
        ExpressionWheelOverlay.Opacity = 0;
        ExpressionWheelScaleTransform.ScaleX = PetAnimationTimings.WheelOpenStartScale;
        ExpressionWheelScaleTransform.ScaleY = PetAnimationTimings.WheelOpenStartScale;

        var duration = new Duration(PetAnimationTimings.WheelOpenDuration);
        var easing = new WpfAnimation.BackEase
        {
            Amplitude = 0.2,
            EasingMode = WpfAnimation.EasingMode.EaseOut,
        };

        ExpressionWheelOverlay.BeginAnimation(UIElement.OpacityProperty, new WpfAnimation.DoubleAnimation(1, duration));
        ExpressionWheelScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
        ExpressionWheelScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
    }

    private void OpenExpressionWheel()
    {
        _expressionWheelHoldTimer.Stop();
        if (_expressionWheelItems.Count == 0 || WpfInput.Mouse.RightButton != WpfInput.MouseButtonState.Pressed)
        {
            return;
        }

        CancelTemporaryExpression();
        StopIdleAnimation();
        StopBlinkAnimation();
        _isExpressionWheelOpen = true;
        UpdateActiveMovementTimer();
        _selectedExpressionWheelIndex = null;
        PositionExpressionWheelOverlay(_expressionWheelOrigin);
        ExpressionWheelOverlay.Visibility = Visibility.Visible;
        AnimateExpressionWheelOpen();
        CaptureMouse();
        UpdateExpressionWheelVisualSelection();
    }

    private void CloseExpressionWheel()
    {
        _isExpressionWheelOpen = false;
        _selectedExpressionWheelIndex = null;
        ExpressionWheelOverlay.Visibility = Visibility.Collapsed;
        ExpressionWheelOverlay.BeginAnimation(UIElement.OpacityProperty, null);
        ExpressionWheelOverlay.Opacity = 1;
        ExpressionWheelScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        ExpressionWheelScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        ExpressionWheelScaleTransform.ScaleX = 1;
        ExpressionWheelScaleTransform.ScaleY = 1;
        UpdateExpressionWheelVisualSelection();
        StartIdleAnimation();
        ScheduleNextBlink();
        UpdateActiveMovementTimer();
    }

    private void UpdateExpressionWheelSelection(WpfPoint position)
    {
        var vector = position - _expressionWheelOrigin;
        var distance = vector.Length;

        if (distance < ExpressionWheelCatalog.InnerRadius || distance > ExpressionWheelCatalog.OuterRadius)
        {
            _selectedExpressionWheelIndex = null;
            UpdateExpressionWheelVisualSelection();
            return;
        }

        var count = _expressionWheelItems.Count;
        if (count == 0)
        {
            _selectedExpressionWheelIndex = null;
            UpdateExpressionWheelVisualSelection();
            return;
        }

        var angle = Math.Atan2(vector.Y, vector.X) + Math.PI / 2;
        if (angle < 0)
        {
            angle += 2 * Math.PI;
        }

        _selectedExpressionWheelIndex = (int)Math.Round(angle / (2 * Math.PI / count)) % count;
        UpdateExpressionWheelVisualSelection();
    }

    private void UpdateExpressionWheelVisualSelection()
    {
        for (var index = 0; index < _expressionWheelItemVisuals.Count; index++)
        {
            var isSelected = _selectedExpressionWheelIndex == index;
            var scale = isSelected ? ExpressionWheelCatalog.SelectedScale : 1;
            _expressionWheelSectorVisuals[index].Fill = new SolidColorBrush(isSelected ? WpfColor.FromArgb(138, 113, 78, 174) : WpfColor.FromArgb(78, 67, 43, 111));
            _expressionWheelSectorVisuals[index].Stroke = new SolidColorBrush(isSelected ? WpfColor.FromArgb(200, 246, 235, 255) : WpfColor.FromArgb(72, 234, 224, 255));
            _expressionWheelSectorVisuals[index].StrokeThickness = isSelected ? 1.25 : 0.75;
            _expressionWheelLabelVisuals[index].Opacity = isSelected ? 1 : 0.78;
            _expressionWheelLabelVisuals[index].FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold;
            _expressionWheelLabelVisuals[index].Foreground = new SolidColorBrush(isSelected ? WpfColor.FromArgb(255, 255, 255, 255) : WpfColor.FromArgb(224, 255, 255, 255));
            if (_expressionWheelLabelVisuals[index].RenderTransform is not ScaleTransform labelScale)
            {
                labelScale = new ScaleTransform(1, 1);
                _expressionWheelLabelVisuals[index].RenderTransform = labelScale;
            }

            var duration = new Duration(PetAnimationTimings.WheelSelectionDuration);
            var easing = new WpfAnimation.QuadraticEase { EasingMode = WpfAnimation.EasingMode.EaseOut };
            labelScale.BeginAnimation(ScaleTransform.ScaleXProperty, new WpfAnimation.DoubleAnimation(scale, duration) { EasingFunction = easing });
            labelScale.BeginAnimation(ScaleTransform.ScaleYProperty, new WpfAnimation.DoubleAnimation(scale, duration) { EasingFunction = easing });
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

    private void ApplyTemporaryExpression(int index)
    {
        if (index < 0 || index >= _expressionWheelItems.Count)
        {
            return;
        }

        var item = _expressionWheelItems[index];
        if (!_expressionImages.TryGetValue(item, out var image))
        {
            return;
        }

        _temporaryExpressionTimer.Stop();
        StopExpressionTransition();
        StopIdleAnimation();
        StopBlinkAnimation();
        _pendingExpressionImage = image;
        PlayExpressionTransitionIn();
        UpdateActiveMovementTimer();
    }

    private void CancelTemporaryExpression()
    {
        _temporaryExpressionTimer.Stop();
        StopExpressionTransition();
        _pendingExpressionImage = null;
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
        _pendingExpressionImage = null;
        StopIdleAnimation();
        StopBlinkAnimation();
        PlayExpressionTransitionOut();
        UpdateActiveMovementTimer();
    }

    private void PlayExpressionTransitionIn()
    {
        if (_expressionTransitionInFrames.Count == 0)
        {
            ShowPendingExpression();
            return;
        }

        ResetCharacterTransitionAnimations();
        _expressionTransitionMode = ExpressionTransitionMode.In;
        _expressionTransitionFrameIndex = 0;
        CharacterImage.Source = _expressionTransitionInFrames[_expressionTransitionFrameIndex];
        _expressionTransitionFrameTimer.Stop();
        _expressionTransitionFrameTimer.Start();
    }

    private void PlayExpressionTransitionOut()
    {
        if (_expressionTransitionOutFrames.Count == 0)
        {
            CompleteExpressionRestore();
            return;
        }

        ResetCharacterTransitionAnimations();
        _expressionTransitionMode = ExpressionTransitionMode.Out;
        _expressionTransitionFrameIndex = 0;
        CharacterImage.Source = _expressionTransitionOutFrames[_expressionTransitionFrameIndex];
        _expressionTransitionFrameTimer.Stop();
        _expressionTransitionFrameTimer.Start();
    }

    private void AdvanceExpressionTransitionFrame()
    {
        var frames = _expressionTransitionMode == ExpressionTransitionMode.In
            ? _expressionTransitionInFrames
            : _expressionTransitionOutFrames;

        if (_expressionTransitionMode == ExpressionTransitionMode.None || frames.Count == 0)
        {
            StopExpressionTransition();
            return;
        }

        _expressionTransitionFrameIndex++;
        if (_expressionTransitionFrameIndex < frames.Count)
        {
            CharacterImage.Source = frames[_expressionTransitionFrameIndex];
            return;
        }

        var completedMode = _expressionTransitionMode;
        StopExpressionTransition();

        if (completedMode == ExpressionTransitionMode.In)
        {
            ShowPendingExpression();
            return;
        }

        CompleteExpressionRestore();
    }

    private void ShowPendingExpression()
    {
        if (_pendingExpressionImage is null || _isDragging || _isExpressionWheelOpen)
        {
            return;
        }

        var image = _pendingExpressionImage;
        _pendingExpressionImage = null;
        AnimateCharacterImageSwap(image);
        _temporaryExpressionTimer.Start();
        UpdateActiveMovementTimer();
    }

    private void CompleteExpressionRestore()
    {
        if (_isDragging || _isExpressionWheelOpen)
        {
            return;
        }

        _idleFrameIndex = 0;
        ResetCharacterTransitionAnimations();
        CharacterImage.Source = GetCurrentIdleFrame();
        StartIdleAnimation();
        ScheduleNextBlink();
        UpdateActiveMovementTimer();
    }

    private void StopExpressionTransition()
    {
        _expressionTransitionFrameTimer.Stop();
        _expressionTransitionMode = ExpressionTransitionMode.None;
        _expressionTransitionFrameIndex = 0;
    }

    private void StartIdleBreathing()
    {
        if (_isDragging || _isExpressionWheelOpen)
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

        _idleFrameIndex = (_idleFrameIndex + 1) % _idleFrames.Count;
        if (!_isBlinking)
        {
            CharacterImage.Source = GetCurrentIdleFrame();
        }
    }

    private ImageSource GetCurrentIdleFrame()
    {
        return _idleFrames.Count == 0 ? _defaultCharacter : _idleFrames[_idleFrameIndex];
    }

    private bool CanIdleAnimate()
    {
        return PetAnimationTimings.CharacterFrameAnimationEnabled
            && !_isDragging
            && !_hasActiveMovementTarget
            && !_isExpressionWheelOpen
            && !_temporaryExpressionTimer.IsEnabled
            && _expressionTransitionMode == ExpressionTransitionMode.None
            && _idleFrames.Count > 0;
    }

    private void ScheduleNextBlink()
    {
        _blinkScheduleTimer.Stop();
        if (!CanBlink())
        {
            return;
        }

        var minMs = (int)BlinkFrameSequence.MinScheduleDelay.TotalMilliseconds;
        var maxMs = (int)BlinkFrameSequence.MaxScheduleDelay.TotalMilliseconds;
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

        _isBlinking = true;
        _blinkFrameIndex = 0;
        CharacterImage.Source = _blinkFrames[_blinkFrameIndex];
        _blinkFrameTimer.Start();
    }

    private bool CanBlink()
    {
        return PetAnimationTimings.BlinkFrameAnimationEnabled
            && !_isDragging
            && !_isBlinking
            && !_hasActiveMovementTarget
            && !_isExpressionWheelOpen
            && !_temporaryExpressionTimer.IsEnabled
            && _expressionTransitionMode == ExpressionTransitionMode.None
            && _blinkFrames.Count > 0;
    }

    private void AdvanceBlinkFrame()
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
            CharacterImage.Source = GetCurrentIdleFrame();
            ScheduleNextBlink();
            return;
        }

        CharacterImage.Source = _blinkFrames[_blinkFrameIndex];
    }

    private void StopBlinkAnimation()
    {
        _blinkScheduleTimer.Stop();
        _blinkFrameTimer.Stop();
        _isBlinking = false;
        _blinkFrameIndex = 0;
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
                TrayService.ActiveMovementText => commands.Settings.ActiveMovement,
                TrayService.PushCursorText => commands.Settings.PushCursor,
                TrayService.ShowTaskbarIconText => commands.Settings.ShowInTaskbar,
                TrayService.StartWithWindowsText => commands.Settings.StartWithWindows,
                _ => item.IsChecked,
            };
        }
    }
}
