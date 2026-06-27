using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using CastoPet.Core;
using WpfControls = System.Windows.Controls;
using WpfInput = System.Windows.Input;
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
    private readonly IReadOnlyDictionary<ExpressionWheelItem, ImageSource> _expressionImages;
    private readonly List<ExpressionWheelItem> _expressionWheelItems = new();
    private readonly List<FrameworkElement> _expressionWheelItemVisuals = new();
    private readonly List<WpfShapes.Path> _expressionWheelSectorVisuals = new();
    private readonly List<WpfControls.TextBlock> _expressionWheelLabelVisuals = new();
    private readonly List<WpfShapes.Line> _expressionWheelDividerVisuals = new();
    private readonly Random _blinkRandom = new();
    private AppSettings? _pendingSettings;
    private WpfPoint _expressionWheelOrigin;
    private bool _applySettingsOnSourceInitialized;
    private bool _isClickThrough;
    private bool _isDragging;
    private bool _isBlinking;
    private bool _isExpressionWheelOpen;
    private int? _selectedExpressionWheelIndex;
    private int _idleFrameIndex;
    private int _blinkFrameIndex;

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

        try
        {
            _defaultCharacter = assets.LoadDefaultCharacter();
            _draggingCharacter = assets.LoadDraggingCharacter();
            _idleFrames = assets.LoadIdleFrames();
            _blinkFrames = assets.LoadBlinkFrames();
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
        };
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
        _isDragging = true;
        _dragRestoreTimer.Stop();
        StopIdleAnimation();
        StopBlinkAnimation();
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
        ScheduleNextBlink();
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
        _selectedExpressionWheelIndex = null;
        PositionExpressionWheelOverlay(_expressionWheelOrigin);
        ExpressionWheelOverlay.Visibility = Visibility.Visible;
        CaptureMouse();
        UpdateExpressionWheelVisualSelection();
    }

    private void CloseExpressionWheel()
    {
        _isExpressionWheelOpen = false;
        _selectedExpressionWheelIndex = null;
        ExpressionWheelOverlay.Visibility = Visibility.Collapsed;
        UpdateExpressionWheelVisualSelection();
        StartIdleAnimation();
        ScheduleNextBlink();
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
            _expressionWheelLabelVisuals[index].RenderTransform = new ScaleTransform(scale, scale);
        }
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
        StopIdleAnimation();
        StopBlinkAnimation();
        CharacterImage.Source = image;
        _temporaryExpressionTimer.Start();
    }

    private void CancelTemporaryExpression()
    {
        _temporaryExpressionTimer.Stop();
    }

    private void RestoreAfterTemporaryExpression()
    {
        _temporaryExpressionTimer.Stop();
        _idleFrameIndex = 0;
        CharacterImage.Source = GetCurrentIdleFrame();
        StartIdleAnimation();
        ScheduleNextBlink();
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
        if (!_isBlinking)
        {
            CharacterImage.Source = GetCurrentIdleFrame();
        }
    }

    private ImageSource GetCurrentIdleFrame()
    {
        return _idleFrames.Count == 0 ? _defaultCharacter : _idleFrames[_idleFrameIndex];
    }

    private void ScheduleNextBlink()
    {
        _blinkScheduleTimer.Stop();
        if (_isDragging || _isBlinking || _blinkFrames.Count == 0)
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
        if (_isDragging || _isBlinking || _blinkFrames.Count == 0)
        {
            return;
        }

        _isBlinking = true;
        _blinkFrameIndex = 0;
        CharacterImage.Source = _blinkFrames[_blinkFrameIndex];
        _blinkFrameTimer.Start();
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
                TrayService.ShowTaskbarIconText => commands.Settings.ShowInTaskbar,
                TrayService.StartWithWindowsText => commands.Settings.StartWithWindows,
                _ => item.IsChecked,
            };
        }
    }
}
