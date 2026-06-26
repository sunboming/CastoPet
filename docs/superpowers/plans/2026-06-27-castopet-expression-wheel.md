# CastoPet Expression Wheel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a semi-transparent right-button hold radial wheel that lets the user choose one of eight temporary desktop-pet expressions.

**Architecture:** Promote eight approved expression PNGs into app resources, define stable expression metadata in `Core`, and load expression images through `AssetService`. Add a lightweight radial overlay inside `PetWindow` so right-button hold opens the wheel, movement selects an item, and release applies a temporary expression without changing left-button dragging or the existing short right-click context menu.

**Tech Stack:** WPF/XAML, C#/.NET 10, existing console-style test harness, existing PNG assets.

---

## File Structure

Create or modify these files:

```text
docs/superpowers/plans/2026-06-27-castopet-expression-wheel.md
src/CastoPet/CastoPet.csproj
src/CastoPet/Assets/Expressions/Castorice.Expression.Happy.png
src/CastoPet/Assets/Expressions/Castorice.Expression.Shy.png
src/CastoPet/Assets/Expressions/Castorice.Expression.Sleepy.png
src/CastoPet/Assets/Expressions/Castorice.Expression.Surprised.png
src/CastoPet/Assets/Expressions/Castorice.Expression.Pouting.png
src/CastoPet/Assets/Expressions/Castorice.Expression.Confused.png
src/CastoPet/Assets/Expressions/Castorice.Expression.Proud.png
src/CastoPet/Assets/Expressions/Castorice.Expression.Crying.png
src/CastoPet/Core/ExpressionWheelItem.cs
src/CastoPet/Core/ExpressionWheelCatalog.cs
src/CastoPet/Core/AssetService.cs
src/CastoPet/PetWindow.xaml
src/CastoPet/PetWindow.xaml.cs
tests/CastoPet.Tests/Program.cs
```

Responsibilities:

- `Assets/Expressions`: app-packaged 320x320 transparent expression resources used by the wheel.
- `ExpressionWheelItem`: immutable metadata for one wheel item.
- `ExpressionWheelCatalog`: stable first-version expression order, labels, paths, hold duration, expression duration, and selection geometry constants.
- `AssetService`: load expression images using the same decode width and logging style as other character assets.
- `PetWindow.xaml`: visual radial overlay.
- `PetWindow.xaml.cs`: right-button hold/open/select/release behavior and temporary expression state.
- `tests/CastoPet.Tests/Program.cs`: non-UI tests for the expression catalog and resource files.

## Task 1: Promote The Eight Expression Resources

**Files:**
- Create: `src/CastoPet/Assets/Expressions/*.png`
- Modify: `src/CastoPet/CastoPet.csproj`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Copy the selected expression PNGs**

Run:

```powershell
$target = 'src\CastoPet\Assets\Expressions'
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -LiteralPath 'src\CastoPet\Assets\CandidateSet\Transparent\Expressions\Castorice.Expression.Happy.png' -Destination "$target\Castorice.Expression.Happy.png" -Force
Copy-Item -LiteralPath 'src\CastoPet\Assets\CandidateSet\Transparent\Expressions\Castorice.Expression.Shy.png' -Destination "$target\Castorice.Expression.Shy.png" -Force
Copy-Item -LiteralPath 'src\CastoPet\Assets\CandidateSet\Transparent\Expressions\Castorice.Expression.Sleepy.png' -Destination "$target\Castorice.Expression.Sleepy.png" -Force
Copy-Item -LiteralPath 'src\CastoPet\Assets\CandidateSet\Transparent\Expressions\Castorice.Expression.Surprised.png' -Destination "$target\Castorice.Expression.Surprised.png" -Force
Copy-Item -LiteralPath 'src\CastoPet\Assets\CandidateSet\Transparent\Expressions\Castorice.Expression.Pouting.png' -Destination "$target\Castorice.Expression.Pouting.png" -Force
Copy-Item -LiteralPath 'src\CastoPet\Assets\CandidateSet\Transparent\Expressions\Castorice.Expression.Confused.png' -Destination "$target\Castorice.Expression.Confused.png" -Force
Copy-Item -LiteralPath 'src\CastoPet\Assets\CandidateSet\Transparent\Expressions\Castorice.Expression.Proud.png' -Destination "$target\Castorice.Expression.Proud.png" -Force
Copy-Item -LiteralPath 'src\CastoPet\Assets\CandidateSet\Transparent\Expressions\Castorice.Expression.Crying.png' -Destination "$target\Castorice.Expression.Crying.png" -Force
```

Expected: `src\CastoPet\Assets\Expressions` contains exactly the eight copied PNGs.

- [ ] **Step 2: Add expression resources to the project file**

Add these entries to the existing `ItemGroup` in `src/CastoPet/CastoPet.csproj`, after the state resources:

```xml
    <Resource Include="Assets\Expressions\Castorice.Expression.Happy.png" />
    <Resource Include="Assets\Expressions\Castorice.Expression.Shy.png" />
    <Resource Include="Assets\Expressions\Castorice.Expression.Sleepy.png" />
    <Resource Include="Assets\Expressions\Castorice.Expression.Surprised.png" />
    <Resource Include="Assets\Expressions\Castorice.Expression.Pouting.png" />
    <Resource Include="Assets\Expressions\Castorice.Expression.Confused.png" />
    <Resource Include="Assets\Expressions\Castorice.Expression.Proud.png" />
    <Resource Include="Assets\Expressions\Castorice.Expression.Crying.png" />
```

- [ ] **Step 3: Run the existing test harness**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: every existing test prints `PASS`. The packaged asset size test should include the new `Assets/Expressions` files and pass because each file is 320x320.

- [ ] **Step 4: Commit**

```powershell
git add src/CastoPet/CastoPet.csproj src/CastoPet/Assets/Expressions
git commit -m "art: promote expression wheel assets"
```

## Task 2: Add Expression Wheel Metadata And Tests

**Files:**
- Create: `src/CastoPet/Core/ExpressionWheelItem.cs`
- Create: `src/CastoPet/Core/ExpressionWheelCatalog.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add failing tests for expression metadata**

In `tests/CastoPet.Tests/Program.cs`, add these tests to the `tests` array after `Blink frame sequence defines random blink frames`:

```csharp
    ("Expression wheel defines eight items", ExpressionWheelDefinesEightItems),
    ("Expression wheel paths use app resources", ExpressionWheelPathsUseAppResources),
```

Add these test methods after `BlinkFrameSequenceDefinesRandomBlinkFrames`:

```csharp
static void ExpressionWheelDefinesEightItems()
{
    Assert.Equal(8, ExpressionWheelCatalog.Items.Count, "Expression wheel should use eight first-version items.");
    Assert.Equal("Happy", ExpressionWheelCatalog.Items[0].Label, "First expression should be Happy.");
    Assert.Equal("Shy", ExpressionWheelCatalog.Items[1].Label, "Second expression should be Shy.");
    Assert.Equal("Sleepy", ExpressionWheelCatalog.Items[2].Label, "Third expression should be Sleepy.");
    Assert.Equal("Surprised", ExpressionWheelCatalog.Items[3].Label, "Fourth expression should be Surprised.");
    Assert.Equal("Pouting", ExpressionWheelCatalog.Items[4].Label, "Fifth expression should be Pouting.");
    Assert.Equal("Confused", ExpressionWheelCatalog.Items[5].Label, "Sixth expression should be Confused.");
    Assert.Equal("Proud", ExpressionWheelCatalog.Items[6].Label, "Seventh expression should be Proud.");
    Assert.Equal("Crying", ExpressionWheelCatalog.Items[7].Label, "Eighth expression should be Crying.");
    Assert.Equal(TimeSpan.FromMilliseconds(250), ExpressionWheelCatalog.HoldDelay, "Wheel hold delay should be short but deliberate.");
    Assert.Equal(TimeSpan.FromSeconds(2), ExpressionWheelCatalog.ExpressionDuration, "Selected expression should be temporary.");
}

static void ExpressionWheelPathsUseAppResources()
{
    foreach (var item in ExpressionWheelCatalog.Items)
    {
        var expected = $"Assets/Expressions/Castorice.Expression.{item.Label}.png";
        Assert.Equal(expected, item.ResourcePath, $"{item.Label} should use the expression resource path convention.");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: build fails because `ExpressionWheelCatalog` does not exist yet.

- [ ] **Step 3: Create `ExpressionWheelItem`**

Create `src/CastoPet/Core/ExpressionWheelItem.cs`:

```csharp
namespace CastoPet.Core;

public sealed record ExpressionWheelItem(string Label, string ResourcePath);
```

- [ ] **Step 4: Create `ExpressionWheelCatalog`**

Create `src/CastoPet/Core/ExpressionWheelCatalog.cs`:

```csharp
namespace CastoPet.Core;

public static class ExpressionWheelCatalog
{
    public const int ItemCount = 8;
    public const double InnerRadius = 34;
    public const double OuterRadius = 124;
    public static readonly TimeSpan HoldDelay = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan ExpressionDuration = TimeSpan.FromSeconds(2);

    public static readonly IReadOnlyList<ExpressionWheelItem> Items = new[]
    {
        Create("Happy"),
        Create("Shy"),
        Create("Sleepy"),
        Create("Surprised"),
        Create("Pouting"),
        Create("Confused"),
        Create("Proud"),
        Create("Crying"),
    };

    private static ExpressionWheelItem Create(string label)
    {
        return new ExpressionWheelItem(label, $"Assets/Expressions/Castorice.Expression.{label}.png");
    }
}
```

- [ ] **Step 5: Run tests to verify metadata passes**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: every test prints `PASS`, including the two expression wheel tests.

- [ ] **Step 6: Commit**

```powershell
git add src/CastoPet/Core/ExpressionWheelItem.cs src/CastoPet/Core/ExpressionWheelCatalog.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: add expression wheel metadata"
```

## Task 3: Load Expression Assets

**Files:**
- Modify: `src/CastoPet/Core/AssetService.cs`

- [ ] **Step 1: Add expression loading API**

In `src/CastoPet/Core/AssetService.cs`, add this method after `LoadBlinkFrames`:

```csharp
    public IReadOnlyDictionary<ExpressionWheelItem, ImageSource> LoadExpressionWheelImages()
    {
        var images = new Dictionary<ExpressionWheelItem, ImageSource>();

        foreach (var item in ExpressionWheelCatalog.Items)
        {
            try
            {
                images[item] = LoadCharacter(item.ResourcePath);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load expression wheel image {item.ResourcePath}.", ex);
            }
        }

        return images;
    }
```

- [ ] **Step 2: Run tests**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: every test prints `PASS`.

- [ ] **Step 3: Commit**

```powershell
git add src/CastoPet/Core/AssetService.cs
git commit -m "feat: load expression wheel assets"
```

## Task 4: Add The Semi-Transparent Wheel Overlay

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml`

- [ ] **Step 1: Replace the root grid content**

In `src/CastoPet/PetWindow.xaml`, keep the existing `Window` attributes and replace the inner `<Grid Background="Transparent">...</Grid>` with:

```xml
    <Grid x:Name="RootGrid" Background="Transparent">
        <Image x:Name="CharacterImage"
               Stretch="Uniform"
               SnapsToDevicePixels="True"
               RenderOptions.BitmapScalingMode="HighQuality" />

        <Canvas x:Name="ExpressionWheelOverlay"
                Visibility="Collapsed"
                IsHitTestVisible="False">
            <Canvas x:Name="ExpressionWheelSurface"
                    Width="280"
                    Height="280">
                <Ellipse Width="248"
                         Height="248"
                         Canvas.Left="16"
                         Canvas.Top="16"
                         Fill="#66352A48"
                         Stroke="#99D9C8FF"
                         StrokeThickness="1.5" />
                <Ellipse Width="72"
                         Height="72"
                         Canvas.Left="104"
                         Canvas.Top="104"
                         Fill="#55352A48"
                         Stroke="#66FFFFFF"
                         StrokeThickness="1" />
            </Canvas>
        </Canvas>
    </Grid>
```

The code-behind will populate eight item visuals into `ExpressionWheelOverlay` after assets load.

- [ ] **Step 2: Build to verify XAML compiles**

Run:

```powershell
dotnet build CastoPet.sln -c Release
```

Expected: build succeeds with `0 个错误`.

- [ ] **Step 3: Commit**

```powershell
git add src/CastoPet/PetWindow.xaml
git commit -m "feat: add expression wheel overlay"
```

## Task 5: Implement Wheel Interaction And Temporary Expressions

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml.cs`

- [ ] **Step 1: Add required usings**

At the top of `src/CastoPet/PetWindow.xaml.cs`, add:

```csharp
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
```

- [ ] **Step 2: Add fields**

Inside `PetWindow`, add these fields after `_dragRestoreTimer`:

```csharp
    private readonly DispatcherTimer _expressionWheelHoldTimer;
    private readonly DispatcherTimer _temporaryExpressionTimer;
    private readonly IReadOnlyDictionary<ExpressionWheelItem, ImageSource> _expressionImages;
    private readonly List<ExpressionWheelItem> _expressionWheelItems = new();
    private readonly List<FrameworkElement> _expressionWheelItemVisuals = new();
    private Point _expressionWheelOrigin;
    private bool _isExpressionWheelOpen;
    private int? _selectedExpressionWheelIndex;
```

- [ ] **Step 3: Initialize timers, load images, and wire mouse events**

In the constructor, after `_dragRestoreTimer` initialization, add:

```csharp
        _expressionWheelHoldTimer = new DispatcherTimer { Interval = ExpressionWheelCatalog.HoldDelay };
        _expressionWheelHoldTimer.Tick += (_, _) => OpenExpressionWheel();
        _temporaryExpressionTimer = new DispatcherTimer { Interval = ExpressionWheelCatalog.ExpressionDuration };
        _temporaryExpressionTimer.Tick += (_, _) => RestoreAfterTemporaryExpression();
```

Inside the existing `try` block after `_blinkFrames = assets.LoadBlinkFrames();`, add:

```csharp
            _expressionImages = assets.LoadExpressionWheelImages();
            BuildExpressionWheel();
```

Inside the existing `catch` block after `_blinkFrames = Array.Empty<ImageSource>();`, add:

```csharp
            _expressionImages = new Dictionary<ExpressionWheelItem, ImageSource>();
```

After `MouseLeftButtonDown += OnMouseLeftButtonDown;`, add:

```csharp
        MouseRightButtonDown += OnMouseRightButtonDown;
        MouseRightButtonUp += OnMouseRightButtonUp;
        MouseMove += OnMouseMove;
```

- [ ] **Step 4: Add right-button handlers**

Add these methods before `BeginDrag`:

```csharp
    private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isClickThrough || e.ButtonState != MouseButtonState.Pressed || _expressionImages.Count == 0)
        {
            return;
        }

        _expressionWheelOrigin = e.GetPosition(RootGrid);
        _selectedExpressionWheelIndex = null;
        _expressionWheelHoldTimer.Stop();
        _expressionWheelHoldTimer.Start();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isExpressionWheelOpen)
        {
            return;
        }

        UpdateExpressionWheelSelection(e.GetPosition(RootGrid));
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _expressionWheelHoldTimer.Stop();

        if (!_isExpressionWheelOpen)
        {
            return;
        }

        UpdateExpressionWheelSelection(e.GetPosition(RootGrid));
        var selectedIndex = _selectedExpressionWheelIndex;
        CloseExpressionWheel();
        e.Handled = true;

        if (selectedIndex is int index)
        {
            ApplyTemporaryExpression(index);
        }
    }
```

- [ ] **Step 5: Add wheel visual construction**

Add these methods after `ApplyPendingSettings`:

```csharp
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
    }

    private FrameworkElement CreateExpressionWheelItemVisual(ExpressionWheelItem item)
    {
        var panel = new StackPanel
        {
            Width = 58,
            Height = 68,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1, 1),
            Opacity = 0.78,
        };

        panel.Children.Add(new Border
        {
            Width = 46,
            Height = 46,
            CornerRadius = new CornerRadius(23),
            Background = new SolidColorBrush(Color.FromArgb(118, 53, 42, 72)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(150, 217, 200, 255)),
            BorderThickness = new Thickness(1),
            Child = new Image
            {
                Source = _expressionImages[item],
                Stretch = Stretch.Uniform,
                Margin = new Thickness(3),
            },
        });

        panel.Children.Add(new TextBlock
        {
            Text = item.Label,
            Foreground = Brushes.White,
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        return panel;
    }

    private void PositionExpressionWheelItems()
    {
        var center = ExpressionWheelOverlay.Width / 2;
        var radius = 96d;

        for (var index = 0; index < _expressionWheelItemVisuals.Count; index++)
        {
            var angle = -Math.PI / 2 + index * 2 * Math.PI / _expressionWheelItemVisuals.Count;
            var x = center + Math.Cos(angle) * radius - _expressionWheelItemVisuals[index].Width / 2;
            var y = center + Math.Sin(angle) * radius - _expressionWheelItemVisuals[index].Height / 2;
            Canvas.SetLeft(_expressionWheelItemVisuals[index], x);
            Canvas.SetTop(_expressionWheelItemVisuals[index], y);
        }
    }

    private void PositionExpressionWheelOverlay(Point origin)
    {
        var left = Math.Clamp(origin.X - ExpressionWheelSurface.Width / 2, 0, Math.Max(0, RootGrid.ActualWidth - ExpressionWheelSurface.Width));
        var top = Math.Clamp(origin.Y - ExpressionWheelSurface.Height / 2, 0, Math.Max(0, RootGrid.ActualHeight - ExpressionWheelSurface.Height));
        Canvas.SetLeft(ExpressionWheelSurface, left);
        Canvas.SetTop(ExpressionWheelSurface, top);
        _expressionWheelOrigin = new Point(left + ExpressionWheelSurface.Width / 2, top + ExpressionWheelSurface.Height / 2);
    }
```

- [ ] **Step 6: Add wheel open, close, and selection logic**

Add these methods after `PositionExpressionWheelItems`:

```csharp
    private void OpenExpressionWheel()
    {
        _expressionWheelHoldTimer.Stop();
        if (_expressionImages.Count == 0 || Mouse.RightButton != MouseButtonState.Pressed)
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

    private void UpdateExpressionWheelSelection(Point position)
    {
        var vector = position - _expressionWheelOrigin;
        var distance = vector.Length;

        if (distance < ExpressionWheelCatalog.InnerRadius || distance > ExpressionWheelCatalog.OuterRadius)
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

        var count = _expressionWheelItems.Count;
        if (count == 0)
        {
            _selectedExpressionWheelIndex = null;
            UpdateExpressionWheelVisualSelection();
            return;
        }

        _selectedExpressionWheelIndex = (int)Math.Round(angle / (2 * Math.PI / count)) % count;
        UpdateExpressionWheelVisualSelection();
    }

    private void UpdateExpressionWheelVisualSelection()
    {
        for (var index = 0; index < _expressionWheelItemVisuals.Count; index++)
        {
            var isSelected = _selectedExpressionWheelIndex == index;
            _expressionWheelItemVisuals[index].Opacity = isSelected ? 1 : 0.78;
            _expressionWheelItemVisuals[index].RenderTransform = new ScaleTransform(isSelected ? 1.18 : 1, isSelected ? 1.18 : 1);
        }
    }
```

- [ ] **Step 7: Add temporary expression logic**

Add these methods before `StartIdleAnimation`:

```csharp
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
```

Update `BeginDrag` so the first line of the method body is:

```csharp
        CancelTemporaryExpression();
```

- [ ] **Step 8: Run tests and build**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
dotnet build CastoPet.sln -c Release
```

Expected: all tests print `PASS`; release build reports `0 个错误`.

- [ ] **Step 9: Commit**

```powershell
git add src/CastoPet/PetWindow.xaml.cs
git commit -m "feat: add expression wheel interaction"
```

## Task 6: Final Verification

**Files:**
- Modify only files required to fix failed checks.

- [ ] **Step 1: Run the app test harness**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: every test prints `PASS`.

- [ ] **Step 2: Build release**

Run:

```powershell
dotnet build CastoPet.sln -c Release
```

Expected: `0 个警告`, `0 个错误`.

- [ ] **Step 3: Inspect git status**

Run:

```powershell
git status --short
```

Expected: only intentionally modified files are listed. Existing unrelated untracked entries may remain:

```text
?? .codex/
?? Castorice.png
?? sample/
```

- [ ] **Step 4: Manual smoke test**

Run the app:

```powershell
dotnet run --project src\CastoPet\CastoPet.csproj
```

Expected manual behavior:

- Short right click opens the existing context menu.
- Holding right click for about 250 ms opens a semi-transparent radial wheel.
- Dragging while holding right click highlights one wheel item by scaling it up.
- Releasing right click over an item applies that expression for about 2 seconds.
- Releasing near the center or outside the wheel closes the wheel without changing expression.
- Left-click dragging still moves the pet.

- [ ] **Step 5: Commit any final fixes**

If Step 1, Step 2, or Step 4 required changes, commit them:

```powershell
git add src/CastoPet tests/CastoPet.Tests
git commit -m "fix: polish expression wheel behavior"
```

Skip this step if there are no final fixes.
