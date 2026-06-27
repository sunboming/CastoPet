# CastoPet Active Movement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an opt-in active movement mode so the pet can approach the mouse, wander gently, and show motion feedback while dragging.

**Architecture:** Keep UI orchestration in `PetWindow`, but put movement calculations in a small pure core type so clamp/approach/easing behavior can be tested without WPF. Settings and menus follow the existing `AppSettings` + `MenuCommandService` + `TrayService` pattern, with the feature disabled by default.

**Tech Stack:** C#/.NET WPF, `DispatcherTimer`, Windows Forms tray menu, existing console-style test project.

---

## File Structure

- Modify `src/CastoPet/Core/AppSettings.cs`
  - Add persisted `ActiveMovement` setting, default `false`, included in `Clone()`.
- Modify `src/CastoPet/Core/MenuCommandService.cs`
  - Add `ToggleActiveMovement()` and persist/apply settings through the existing `ApplyAndSave()` path.
- Modify `src/CastoPet/Core/TrayService.cs`
  - Add `主动移动` checked item near `鼠标穿透`.
- Create `src/CastoPet/Core/PetMovementPlanner.cs`
  - Pure math for mouse approach target, clamping, and eased movement step.
- Modify `src/CastoPet/PetWindow.xaml.cs`
  - Add active movement timer/state, menu context entry, movement gating, mouse approach, wander, and drag visual treatment.
- Modify `tests/CastoPet.Tests/Program.cs`
  - Add focused tests for settings round trip, menu label, and movement planner behavior.

## Task 1: Settings And Menu Toggle

**Files:**
- Modify: `tests/CastoPet.Tests/Program.cs`
- Modify: `src/CastoPet/Core/AppSettings.cs`
- Modify: `src/CastoPet/Core/MenuCommandService.cs`
- Modify: `src/CastoPet/Core/TrayService.cs`
- Modify: `src/CastoPet/PetWindow.xaml.cs`

- [ ] **Step 1: Write the failing settings/menu tests**

Add these test registrations near the existing settings and expression wheel tests:

```csharp
("Default active movement is disabled", DefaultActiveMovementIsDisabled),
("Settings round trip includes active movement", SettingsRoundTripIncludesActiveMovement),
("Tray menu exposes active movement text", TrayMenuExposesActiveMovementText),
```

Add these test methods:

```csharp
static void DefaultActiveMovementIsDisabled()
{
    var settings = AppSettings.Default;

    Assert.False(settings.ActiveMovement, "Active movement should default to false.");
}

static void SettingsRoundTripIncludesActiveMovement()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);

    var settings = new AppSettings
    {
        Topmost = false,
        ClickThrough = true,
        ShowInTaskbar = true,
        StartWithWindows = true,
        ActiveMovement = true,
    };

    service.Save(settings);
    var loaded = service.Load();

    Assert.True(loaded.ActiveMovement, "ActiveMovement should round trip.");
}

static void TrayMenuExposesActiveMovementText()
{
    Assert.Equal("主动移动", TrayService.ActiveMovementText, "Active movement menu text should be localized.");
}
```

Also extend existing tests:

```csharp
static void DefaultSettingsMatchMvpDefaults()
{
    var settings = AppSettings.Default;
    Assert.True(settings.Topmost, "Topmost should default to true.");
    Assert.False(settings.ClickThrough, "ClickThrough should default to false.");
    Assert.False(settings.ShowInTaskbar, "ShowInTaskbar should default to false.");
    Assert.False(settings.StartWithWindows, "StartWithWindows should default to false.");
    Assert.False(settings.ActiveMovement, "ActiveMovement should default to false.");
}
```

```csharp
static void SettingsRoundTripAsJson()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);

    var settings = new AppSettings
    {
        Topmost = false,
        ClickThrough = true,
        ShowInTaskbar = true,
        StartWithWindows = true,
        ActiveMovement = true,
    };

    service.Save(settings);
    var loaded = service.Load();

    Assert.False(loaded.Topmost, "Topmost should round trip.");
    Assert.True(loaded.ClickThrough, "ClickThrough should round trip.");
    Assert.True(loaded.ShowInTaskbar, "ShowInTaskbar should round trip.");
    Assert.True(loaded.StartWithWindows, "StartWithWindows should round trip.");
    Assert.True(loaded.ActiveMovement, "ActiveMovement should round trip.");
}
```

```csharp
static void InvalidSettingsFallsBackToDefaults()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    Directory.CreateDirectory(paths.DataDirectory);
    File.WriteAllText(paths.SettingsFile, "{not valid json");

    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);
    var loaded = service.Load();

    Assert.True(loaded.Topmost, "Invalid settings should return defaults.");
    Assert.False(loaded.ClickThrough, "Invalid settings should return defaults.");
    Assert.False(loaded.ActiveMovement, "Invalid settings should return defaults.");
    Assert.True(File.Exists(paths.LogFile), "Invalid settings should be logged.");
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test
```

Expected: FAIL because `AppSettings.ActiveMovement` and `TrayService.ActiveMovementText` do not exist.

- [ ] **Step 3: Add the setting and menu command**

Replace `src/CastoPet/Core/AppSettings.cs` with:

```csharp
namespace CastoPet.Core;

public sealed class AppSettings
{
    public bool Topmost { get; set; } = true;
    public bool ClickThrough { get; set; }
    public bool ShowInTaskbar { get; set; }
    public bool StartWithWindows { get; set; }
    public bool ActiveMovement { get; set; }

    public static AppSettings Default => new();

    public AppSettings Clone()
    {
        return new AppSettings
        {
            Topmost = Topmost,
            ClickThrough = ClickThrough,
            ShowInTaskbar = ShowInTaskbar,
            StartWithWindows = StartWithWindows,
            ActiveMovement = ActiveMovement,
        };
    }
}
```

Add this method to `src/CastoPet/Core/MenuCommandService.cs` after `ToggleClickThrough()`:

```csharp
public void ToggleActiveMovement()
{
    Settings.ActiveMovement = !Settings.ActiveMovement;
    ApplyAndSave("Active movement setting changed.");
}
```

- [ ] **Step 4: Add tray and context menu entries**

Update `src/CastoPet/Core/TrayService.cs` constants and fields:

```csharp
public const string MouseClickThroughText = "鼠标穿透";
public const string ActiveMovementText = "主动移动";
public const string ShowTaskbarIconText = "显示任务栏图标";
```

```csharp
private readonly Forms.ToolStripMenuItem _clickThroughItem;
private readonly Forms.ToolStripMenuItem _activeMovementItem;
private readonly Forms.ToolStripMenuItem _taskbarItem;
```

Update the constructor:

```csharp
_clickThroughItem = CreateCheckedItem(MouseClickThroughText, _commands.ToggleClickThrough);
_activeMovementItem = CreateCheckedItem(ActiveMovementText, _commands.ToggleActiveMovement);
_taskbarItem = CreateCheckedItem(ShowTaskbarIconText, _commands.ToggleShowInTaskbar);
```

Add the item near the interaction settings:

```csharp
menu.Items.Add(_topmostItem);
menu.Items.Add(_clickThroughItem);
menu.Items.Add(_activeMovementItem);
menu.Items.Add(_taskbarItem);
menu.Items.Add(_startupItem);
```

Update `RefreshChecks()`:

```csharp
private void RefreshChecks()
{
    _topmostItem.Checked = _commands.Settings.Topmost;
    _clickThroughItem.Checked = _commands.Settings.ClickThrough;
    _activeMovementItem.Checked = _commands.Settings.ActiveMovement;
    _taskbarItem.Checked = _commands.Settings.ShowInTaskbar;
    _startupItem.Checked = _commands.Settings.StartWithWindows;
}
```

Update `PetWindow.AttachContextMenu()`:

```csharp
menu.Items.Add(CreateCheckedMenuItem(TrayService.AlwaysOnTopText, () => commands.Settings.Topmost, commands.ToggleTopmost));
menu.Items.Add(CreateCheckedMenuItem(TrayService.MouseClickThroughText, () => commands.Settings.ClickThrough, commands.ToggleClickThrough));
menu.Items.Add(CreateCheckedMenuItem(TrayService.ActiveMovementText, () => commands.Settings.ActiveMovement, commands.ToggleActiveMovement));
menu.Items.Add(CreateCheckedMenuItem(TrayService.ShowTaskbarIconText, () => commands.Settings.ShowInTaskbar, commands.ToggleShowInTaskbar));
menu.Items.Add(CreateCheckedMenuItem(TrayService.StartWithWindowsText, () => commands.Settings.StartWithWindows, commands.ToggleStartWithWindows));
```

- [ ] **Step 5: Run tests and commit**

Run:

```powershell
dotnet test
```

Expected: PASS.

Commit:

```powershell
git add tests/CastoPet.Tests/Program.cs src/CastoPet/Core/AppSettings.cs src/CastoPet/Core/MenuCommandService.cs src/CastoPet/Core/TrayService.cs src/CastoPet/PetWindow.xaml.cs
git commit -m "feat: add active movement setting"
```

## Task 2: Movement Planner

**Files:**
- Create: `src/CastoPet/Core/PetMovementPlanner.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write failing planner tests**

Add these test registrations:

```csharp
("Movement planner clamps targets to work area", MovementPlannerClampsTargetsToWorkArea),
("Movement planner approaches mouse with cursor offset", MovementPlannerApproachesMouseWithCursorOffset),
("Movement planner eases toward target", MovementPlannerEasesTowardTarget),
```

Add these test methods:

```csharp
static void MovementPlannerClampsTargetsToWorkArea()
{
    var bounds = new PetMovementBounds(0, 0, 500, 400);

    var target = PetMovementPlanner.ClampTarget(
        left: 460,
        top: 390,
        windowWidth: 100,
        windowHeight: 120,
        bounds);

    Assert.Equal(400d, target.Left, "Target left should keep the full pet inside the work area.");
    Assert.Equal(280d, target.Top, "Target top should keep the full pet inside the work area.");
}

static void MovementPlannerApproachesMouseWithCursorOffset()
{
    var bounds = new PetMovementBounds(0, 0, 800, 600);

    var target = PetMovementPlanner.CalculateMouseApproachTarget(
        petLeft: 100,
        petTop: 100,
        petWidth: 100,
        petHeight: 100,
        mouseX: 300,
        mouseY: 150,
        bounds);

    var targetCenterX = target.Left + 50;
    var targetCenterY = target.Top + 50;
    var distance = Math.Sqrt(Math.Pow(targetCenterX - 300, 2) + Math.Pow(targetCenterY - 150, 2));

    Assert.True(distance >= PetMovementPlanner.MinMouseApproachOffset, "Target should not cover the cursor.");
    Assert.True(distance <= PetMovementPlanner.MaxMouseApproachOffset, "Target should stop close to the cursor.");
    Assert.True(target.Left > 100, "Target should move toward the mouse.");
}

static void MovementPlannerEasesTowardTarget()
{
    var next = PetMovementPlanner.StepToward(
        currentLeft: 0,
        currentTop: 0,
        target: new PetMovementTarget(100, 50));

    Assert.True(next.Left > 0, "Next left should move forward.");
    Assert.True(next.Left < 100, "Next left should ease instead of jumping.");
    Assert.True(next.Top > 0, "Next top should move forward.");
    Assert.True(next.Top < 50, "Next top should ease instead of jumping.");
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test
```

Expected: FAIL because `PetMovementPlanner`, `PetMovementBounds`, and `PetMovementTarget` do not exist.

- [ ] **Step 3: Add movement planner**

Create `src/CastoPet/Core/PetMovementPlanner.cs`:

```csharp
namespace CastoPet.Core;

public readonly record struct PetMovementBounds(double Left, double Top, double Width, double Height);

public readonly record struct PetMovementTarget(double Left, double Top);

public static class PetMovementPlanner
{
    public const double MouseInterestRadius = 360;
    public const double MouseApproachOffset = 32;
    public const double MinMouseApproachOffset = 20;
    public const double MaxMouseApproachOffset = 40;
    public const double StopDistance = 4;
    public const double MovementEase = 0.14;

    public static PetMovementTarget ClampTarget(
        double left,
        double top,
        double windowWidth,
        double windowHeight,
        PetMovementBounds bounds)
    {
        var maxLeft = bounds.Left + Math.Max(0, bounds.Width - windowWidth);
        var maxTop = bounds.Top + Math.Max(0, bounds.Height - windowHeight);

        return new PetMovementTarget(
            Math.Clamp(left, bounds.Left, maxLeft),
            Math.Clamp(top, bounds.Top, maxTop));
    }

    public static PetMovementTarget CalculateMouseApproachTarget(
        double petLeft,
        double petTop,
        double petWidth,
        double petHeight,
        double mouseX,
        double mouseY,
        PetMovementBounds bounds)
    {
        var petCenterX = petLeft + petWidth / 2;
        var petCenterY = petTop + petHeight / 2;
        var dx = mouseX - petCenterX;
        var dy = mouseY - petCenterY;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance <= 0.001)
        {
            return ClampTarget(
                mouseX - petWidth / 2 - MouseApproachOffset,
                mouseY - petHeight / 2,
                petWidth,
                petHeight,
                bounds);
        }

        var targetCenterX = mouseX - dx / distance * MouseApproachOffset;
        var targetCenterY = mouseY - dy / distance * MouseApproachOffset;

        return ClampTarget(
            targetCenterX - petWidth / 2,
            targetCenterY - petHeight / 2,
            petWidth,
            petHeight,
            bounds);
    }

    public static PetMovementTarget StepToward(
        double currentLeft,
        double currentTop,
        PetMovementTarget target)
    {
        return new PetMovementTarget(
            currentLeft + (target.Left - currentLeft) * MovementEase,
            currentTop + (target.Top - currentTop) * MovementEase);
    }

    public static bool IsClose(double currentLeft, double currentTop, PetMovementTarget target)
    {
        var dx = target.Left - currentLeft;
        var dy = target.Top - currentTop;

        return Math.Sqrt(dx * dx + dy * dy) <= StopDistance;
    }
}
```

- [ ] **Step 4: Run tests and commit**

Run:

```powershell
dotnet test
```

Expected: PASS.

Commit:

```powershell
git add tests/CastoPet.Tests/Program.cs src/CastoPet/Core/PetMovementPlanner.cs
git commit -m "feat: add active movement planner"
```

## Task 3: PetWindow Active Movement Runtime

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml.cs`

- [ ] **Step 1: Add fields, timer, and setting state**

Add alias:

```csharp
using Forms = System.Windows.Forms;
```

Add fields:

```csharp
private readonly DispatcherTimer _activeMovementTimer;
private readonly Random _movementRandom = new();
private AppSettings? _pendingSettings;
private PetMovementTarget _activeMovementTarget;
private DateTime _nextWanderDecisionUtc = DateTime.MinValue;
private bool _activeMovementEnabled;
private bool _hasActiveMovementTarget;
private double _lastMovementDeltaX;
private WpfPoint _dragStartPoint;
```

Initialize the timer in the constructor after `_expressionTransitionFrameTimer`:

```csharp
_activeMovementTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
_activeMovementTimer.Tick += (_, _) => AdvanceActiveMovement();
```

In the `Loaded` handler, after `ScheduleNextBlink();`, add:

```csharp
UpdateActiveMovementTimer();
```

- [ ] **Step 2: Persist active setting into the window**

Update `ApplySettings(AppSettings settings)` so it stores and reacts to the feature flag:

```csharp
public void ApplySettings(AppSettings settings)
{
    Topmost = settings.Topmost;
    ShowInTaskbar = settings.ShowInTaskbar;
    _isClickThrough = settings.ClickThrough;
    _activeMovementEnabled = settings.ActiveMovement;
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
```

- [ ] **Step 3: Add active movement helpers**

Add these methods near the drag helpers:

```csharp
private bool CanRunActiveMovement()
{
    return _activeMovementEnabled
        && IsVisible
        && !_isClickThrough
        && !_isDragging
        && !_isExpressionWheelOpen
        && !_temporaryExpressionTimer.IsEnabled
        && _expressionTransitionMode == ExpressionTransitionMode.None;
}

private void UpdateActiveMovementTimer()
{
    if (CanRunActiveMovement())
    {
        _activeMovementTimer.Start();
        return;
    }

    _activeMovementTimer.Stop();
    _hasActiveMovementTarget = false;
    ResetActiveMovementVisual();
}

private void AdvanceActiveMovement()
{
    if (!CanRunActiveMovement())
    {
        UpdateActiveMovementTimer();
        return;
    }

    var width = ActualWidth > 0 ? ActualWidth : Width;
    var height = ActualHeight > 0 ? ActualHeight : Height;
    var bounds = new PetMovementBounds(
        SystemParameters.WorkArea.Left,
        SystemParameters.WorkArea.Top,
        SystemParameters.WorkArea.Width,
        SystemParameters.WorkArea.Height);

    var cursor = Forms.Cursor.Position;
    var petCenterX = Left + width / 2;
    var petCenterY = Top + height / 2;
    var cursorDistance = Math.Sqrt(Math.Pow(cursor.X - petCenterX, 2) + Math.Pow(cursor.Y - petCenterY, 2));

    if (cursorDistance <= PetMovementPlanner.MouseInterestRadius)
    {
        _activeMovementTarget = PetMovementPlanner.CalculateMouseApproachTarget(
            Left,
            Top,
            width,
            height,
            cursor.X,
            cursor.Y,
            bounds);
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

    var next = PetMovementPlanner.StepToward(Left, Top, _activeMovementTarget);
    _lastMovementDeltaX = next.Left - Left;
    Left = next.Left;
    Top = next.Top;
    _runtimeState.SetRuntimePosition(Left, Top);

    ApplyActiveMovementVisual();

    if (PetMovementPlanner.IsClose(Left, Top, _activeMovementTarget))
    {
        _hasActiveMovementTarget = false;
        _nextWanderDecisionUtc = DateTime.UtcNow.AddMilliseconds(_movementRandom.Next(1200, 2600));
    }
}

private void ChooseWanderTarget(double width, double height, PetMovementBounds bounds)
{
    if (DateTime.UtcNow < _nextWanderDecisionUtc)
    {
        return;
    }

    var range = 160;
    var targetLeft = Left + _movementRandom.NextDouble() * range * 2 - range;
    var targetTop = Top + _movementRandom.NextDouble() * range * 2 - range;
    _activeMovementTarget = PetMovementPlanner.ClampTarget(targetLeft, targetTop, width, height, bounds);
    _hasActiveMovementTarget = true;
}

private void ApplyActiveMovementVisual()
{
    CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
    CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);

    var directionScale = _lastMovementDeltaX < 0 ? 0.992 : 1.008;
    CharacterScaleTransform.ScaleX = directionScale;
    CharacterScaleTransform.ScaleY = 1.004;
}

private void ApplyDragMovementVisual()
{
    CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
    CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
    CharacterScaleTransform.ScaleX = 1.018;
    CharacterScaleTransform.ScaleY = 0.986;
}

private void ResetActiveMovementVisual()
{
    if (_isDragging || _temporaryExpressionTimer.IsEnabled || _expressionTransitionMode != ExpressionTransitionMode.None)
    {
        return;
    }

    CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
    CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
    CharacterScaleTransform.ScaleX = 1;
    CharacterScaleTransform.ScaleY = 1;
}
```

- [ ] **Step 4: Pause and resume movement around high-priority states**

Update `BeginDrag()`:

```csharp
private void BeginDrag()
{
    CancelTemporaryExpression();
    _activeMovementTimer.Stop();
    _hasActiveMovementTarget = false;
    _isDragging = true;
    _dragRestoreTimer.Stop();
    StopIdleAnimation();
    StopBlinkAnimation();
    ResetCharacterTransitionAnimations();
    ApplyDragMovementVisual();
    CharacterImage.Source = _draggingCharacter;
}
```

Update `EndDrag()`:

```csharp
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
```

Update `RestoreAfterDrag()`:

```csharp
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
```

Update `OpenExpressionWheel()` after `_isExpressionWheelOpen = true;`:

```csharp
UpdateActiveMovementTimer();
```

Update `CloseExpressionWheel()` after `_isExpressionWheelOpen = false;`:

```csharp
UpdateActiveMovementTimer();
```

Update `ApplyTemporaryExpression()` before `PlayExpressionTransitionIn();`:

```csharp
UpdateActiveMovementTimer();
```

Update `CompleteExpressionRestore()` after `ScheduleNextBlink();`:

```csharp
UpdateActiveMovementTimer();
```

Update `CancelTemporaryExpression()` after `ResetCharacterTransitionAnimations();`:

```csharp
UpdateActiveMovementTimer();
```

- [ ] **Step 5: Run build/tests and commit**

Run:

```powershell
dotnet test
dotnet build src/CastoPet/CastoPet.csproj -c Release
```

Expected: tests PASS and Release build succeeds with 0 errors.

Commit:

```powershell
git add src/CastoPet/PetWindow.xaml.cs
git commit -m "feat: add active pet movement runtime"
```

## Task 4: Manual Validation Pass

**Files:**
- Modify only if validation reveals a defect.

- [ ] **Step 1: Launch the app**

Run:

```powershell
dotnet run --project src/CastoPet/CastoPet.csproj
```

Expected: CastoPet launches.

- [ ] **Step 2: Validate off state**

With `主动移动` unchecked:

- The pet should stay still except for existing idle/blink behavior.
- Dragging should still move immediately.
- Right-hold expression wheel should still open.

- [ ] **Step 3: Validate on state**

Enable `主动移动` from the tray menu or pet context menu:

- The menu item becomes checked.
- The pet moves toward the mouse when the cursor is nearby.
- The pet stops near the cursor instead of covering it.
- The pet occasionally wanders when the cursor is away.
- Opening the expression wheel pauses movement.
- Dragging pauses automatic movement and keeps drag responsive.
- Disabling `主动移动` stops movement.

- [ ] **Step 4: Final verification**

Run:

```powershell
dotnet test
dotnet build src/CastoPet/CastoPet.csproj -c Release
git status --short
```

Expected:

- Tests PASS.
- Release build succeeds with 0 errors.
- `git status --short` only shows intentional files or known unrelated untracked files.

## Self-Review

- Spec coverage:
  - `ActiveMovement` setting and menu toggle: Task 1.
  - Mouse approach with `20..40px` offset target: Task 2 planner constants and Task 3 runtime use.
  - Wandering and eased window movement: Task 3.
  - Pause during drag, wheel, temporary expression, transition, and click-through: Task 3 `CanRunActiveMovement()`.
  - Drag visual motion treatment: Task 3 `ApplyDragMovementVisual()`.
  - No new sprites/pathfinding/physics: all tasks reuse current assets and timers.
- Placeholder scan:
  - No TBD/TODO/later placeholders.
  - Every code step includes concrete code.
- Type consistency:
  - `PetMovementBounds`, `PetMovementTarget`, and `PetMovementPlanner` are defined before runtime use.
  - `ActiveMovementText` and `ToggleActiveMovement()` are introduced before menu wiring uses them.
