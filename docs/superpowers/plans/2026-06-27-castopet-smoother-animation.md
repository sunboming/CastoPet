# CastoPet Smoother Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve perceived animation smoothness with WPF easing, idle breathing, expression transitions, and wheel micro-animation while keeping the current PNG sprite assets.

**Architecture:** Add tested animation timing constants in `Core`, then use WPF transforms and opacity animations in `PetWindow` instead of adding more timers. Keep existing frame timers for idle/blink sprite sequencing, but centralize transition start/stop helpers so drag, wheel, blink, and temporary expressions do not fight each other.

**Tech Stack:** WPF animation APIs, C#/.NET 10, existing console-style test harness, existing PNG assets.

---

## File Structure

Create or modify these files:

```text
docs/superpowers/plans/2026-06-27-castopet-smoother-animation.md
src/CastoPet/Core/PetAnimationTimings.cs
src/CastoPet/PetWindow.xaml
src/CastoPet/PetWindow.xaml.cs
tests/CastoPet.Tests/Program.cs
```

Responsibilities:

- `PetAnimationTimings`: named timing and transform constants for idle breathing, expression transitions, wheel open animation, and selection emphasis.
- `PetWindow.xaml`: define stable render-transform groups for `CharacterImage` and `ExpressionWheelSurface`.
- `PetWindow.xaml.cs`: run WPF animations for breathing, expression enter/exit, wheel open, and wheel selection emphasis.
- `tests/CastoPet.Tests/Program.cs`: lock the timing ranges and subtle transform values.

## Task 1: Add Animation Timing Constants

**Files:**
- Create: `src/CastoPet/Core/PetAnimationTimings.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add the failing timing tests**

In `tests/CastoPet.Tests/Program.cs`, add these entries to the `tests` array after `Expression wheel style is text only with dividers`:

```csharp
    ("Pet animation timings are responsive", PetAnimationTimingsAreResponsive),
    ("Idle breathing values are subtle", IdleBreathingValuesAreSubtle),
```

Add these test methods after `ExpressionWheelStyleIsTextOnlyWithDividers`:

```csharp
static void PetAnimationTimingsAreResponsive()
{
    Assert.Equal(TimeSpan.FromMilliseconds(120), PetAnimationTimings.ExpressionEnterDuration, "Expression enter should be quick.");
    Assert.Equal(TimeSpan.FromMilliseconds(180), PetAnimationTimings.ExpressionExitDuration, "Expression exit should be smooth but short.");
    Assert.Equal(TimeSpan.FromMilliseconds(120), PetAnimationTimings.WheelOpenDuration, "Wheel open should feel immediate.");
    Assert.Equal(TimeSpan.FromMilliseconds(90), PetAnimationTimings.WheelSelectionDuration, "Selection emphasis should respond quickly.");
}

static void IdleBreathingValuesAreSubtle()
{
    Assert.Equal(TimeSpan.FromMilliseconds(1900), PetAnimationTimings.IdleBreathingCycleDuration, "Idle breathing should be slow.");
    Assert.True(PetAnimationTimings.IdleBreathingTranslateY <= 4, "Idle breathing vertical movement should remain subtle.");
    Assert.True(PetAnimationTimings.IdleBreathingScaleDelta <= 0.02, "Idle breathing scale should remain subtle.");
    Assert.Equal(0.96, PetAnimationTimings.ExpressionDimmedOpacity, "Expression transition should only slightly dim during swaps.");
    Assert.Equal(0.92, PetAnimationTimings.WheelOpenStartScale, "Wheel should open from a small scale change.");
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: build fails because `PetAnimationTimings` does not exist yet.

- [ ] **Step 3: Create `PetAnimationTimings`**

Create `src/CastoPet/Core/PetAnimationTimings.cs`:

```csharp
namespace CastoPet.Core;

public static class PetAnimationTimings
{
    public static readonly TimeSpan IdleBreathingCycleDuration = TimeSpan.FromMilliseconds(1900);
    public static readonly TimeSpan ExpressionEnterDuration = TimeSpan.FromMilliseconds(120);
    public static readonly TimeSpan ExpressionExitDuration = TimeSpan.FromMilliseconds(180);
    public static readonly TimeSpan WheelOpenDuration = TimeSpan.FromMilliseconds(120);
    public static readonly TimeSpan WheelSelectionDuration = TimeSpan.FromMilliseconds(90);

    public const double IdleBreathingTranslateY = 3;
    public const double IdleBreathingScaleDelta = 0.012;
    public const double ExpressionEnterStartScale = 0.985;
    public const double ExpressionDimmedOpacity = 0.96;
    public const double WheelOpenStartScale = 0.92;
}
```

- [ ] **Step 4: Run tests to verify constants pass**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: every test prints `PASS`, including the two new timing tests.

- [ ] **Step 5: Commit**

```powershell
git add src/CastoPet/Core/PetAnimationTimings.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: add pet animation timing constants"
```

## Task 2: Prepare Stable Render Transforms

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml`

- [ ] **Step 1: Add image render transforms**

In `src/CastoPet/PetWindow.xaml`, replace the self-closing `CharacterImage` element with this explicit element:

```xml
        <Image x:Name="CharacterImage"
               Stretch="Uniform"
               SnapsToDevicePixels="True"
               RenderOptions.BitmapScalingMode="HighQuality"
               RenderTransformOrigin="0.5,0.5">
            <Image.RenderTransform>
                <TransformGroup>
                    <ScaleTransform x:Name="CharacterScaleTransform" ScaleX="1" ScaleY="1" />
                    <TranslateTransform x:Name="CharacterTranslateTransform" X="0" Y="0" />
                </TransformGroup>
            </Image.RenderTransform>
        </Image>
```

- [ ] **Step 2: Add wheel surface render transform**

In the `ExpressionWheelSurface` canvas, add `RenderTransformOrigin` and a scale transform so it starts as:

```xml
            <Canvas x:Name="ExpressionWheelSurface"
                    Width="280"
                    Height="280"
                    RenderTransformOrigin="0.5,0.5">
                <Canvas.RenderTransform>
                    <ScaleTransform x:Name="ExpressionWheelScaleTransform" ScaleX="1" ScaleY="1" />
                </Canvas.RenderTransform>
```

Keep the existing ellipse children unchanged.

- [ ] **Step 3: Build to verify XAML names compile**

Run:

```powershell
dotnet build CastoPet.sln -c Release
```

Expected: build succeeds with `0 个错误`.

- [ ] **Step 4: Commit**

```powershell
git add src/CastoPet/PetWindow.xaml
git commit -m "feat: add pet animation transforms"
```

## Task 3: Add Idle Breathing Animation

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml.cs`

- [ ] **Step 1: Add animation namespace**

Add this using alias near the other aliases:

```csharp
using WpfAnimation = System.Windows.Media.Animation;
```

- [ ] **Step 2: Add breathing helpers**

Add these methods before `StartIdleAnimation`:

```csharp
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
```

- [ ] **Step 3: Wire breathing into idle start/stop**

Update `StartIdleAnimation` to call `StartIdleBreathing()` immediately before `_idleFrameTimer.Start();`:

```csharp
        StartIdleBreathing();
        _idleFrameTimer.Start();
```

Update `StopIdleAnimation` to stop breathing:

```csharp
    private void StopIdleAnimation()
    {
        _idleFrameTimer.Stop();
        StopIdleBreathing();
    }
```

- [ ] **Step 4: Run tests and build**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
dotnet build CastoPet.sln -c Release
```

Expected: all tests print `PASS`; release build reports `0 个错误`.

- [ ] **Step 5: Commit**

```powershell
git add src/CastoPet/PetWindow.xaml.cs
git commit -m "feat: add idle breathing animation"
```

## Task 4: Smooth Temporary Expression Transitions

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml.cs`

- [ ] **Step 1: Add general image transition helper**

Add this method before `ApplyTemporaryExpression`:

```csharp
    private void AnimateCharacterImageSwap(ImageSource image)
    {
        StopIdleBreathing();
        CharacterImage.Opacity = PetAnimationTimings.ExpressionDimmedOpacity;
        CharacterScaleTransform.ScaleX = PetAnimationTimings.ExpressionEnterStartScale;
        CharacterScaleTransform.ScaleY = PetAnimationTimings.ExpressionEnterStartScale;
        CharacterTranslateTransform.Y = 0;
        CharacterImage.Source = image;

        var duration = new Duration(PetAnimationTimings.ExpressionEnterDuration);
        var easing = new WpfAnimation.QuadraticEase { EasingMode = WpfAnimation.EasingMode.EaseOut };

        CharacterImage.BeginAnimation(OpacityProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
        CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
        CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
    }
```

- [ ] **Step 2: Use the helper when applying expressions**

In `ApplyTemporaryExpression`, replace:

```csharp
        CharacterImage.Source = image;
```

with:

```csharp
        AnimateCharacterImageSwap(image);
```

- [ ] **Step 3: Smooth restore after temporary expression**

Replace `RestoreAfterTemporaryExpression` with:

```csharp
    private void RestoreAfterTemporaryExpression()
    {
        _temporaryExpressionTimer.Stop();
        _idleFrameIndex = 0;

        var duration = new Duration(PetAnimationTimings.ExpressionExitDuration);
        var easing = new WpfAnimation.QuadraticEase { EasingMode = WpfAnimation.EasingMode.EaseOut };
        var fadeOut = new WpfAnimation.DoubleAnimation(PetAnimationTimings.ExpressionDimmedOpacity, duration)
        {
            EasingFunction = easing,
        };
        fadeOut.Completed += (_, _) =>
        {
            CharacterImage.Source = GetCurrentIdleFrame();
            CharacterImage.BeginAnimation(OpacityProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
            CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
            CharacterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
            StartIdleAnimation();
            ScheduleNextBlink();
        };

        StopIdleAnimation();
        StopBlinkAnimation();
        CharacterImage.BeginAnimation(OpacityProperty, fadeOut);
    }
```

- [ ] **Step 4: Reset opacity when drag begins**

In `BeginDrag`, before `CharacterImage.Source = _draggingCharacter;`, add:

```csharp
        CharacterImage.BeginAnimation(OpacityProperty, null);
        CharacterImage.Opacity = 1;
```

- [ ] **Step 5: Run tests and build**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
dotnet build CastoPet.sln -c Release
```

Expected: all tests print `PASS`; release build reports `0 个错误`.

- [ ] **Step 6: Commit**

```powershell
git add src/CastoPet/PetWindow.xaml.cs
git commit -m "feat: smooth expression transitions"
```

## Task 5: Smooth Wheel Open And Selection Emphasis

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml.cs`

- [ ] **Step 1: Add wheel open animation helper**

Add this method before `OpenExpressionWheel`:

```csharp
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

        ExpressionWheelOverlay.BeginAnimation(OpacityProperty, new WpfAnimation.DoubleAnimation(1, duration));
        ExpressionWheelScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
        ExpressionWheelScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new WpfAnimation.DoubleAnimation(1, duration) { EasingFunction = easing });
    }
```

- [ ] **Step 2: Call the wheel open animation**

In `OpenExpressionWheel`, after:

```csharp
        ExpressionWheelOverlay.Visibility = Visibility.Visible;
```

add:

```csharp
        AnimateExpressionWheelOpen();
```

- [ ] **Step 3: Animate label selection scale**

In `UpdateExpressionWheelVisualSelection`, replace:

```csharp
            _expressionWheelLabelVisuals[index].RenderTransform = new ScaleTransform(scale, scale);
```

with:

```csharp
            if (_expressionWheelLabelVisuals[index].RenderTransform is not ScaleTransform labelScale)
            {
                labelScale = new ScaleTransform(1, 1);
                _expressionWheelLabelVisuals[index].RenderTransform = labelScale;
            }

            var duration = new Duration(PetAnimationTimings.WheelSelectionDuration);
            var easing = new WpfAnimation.QuadraticEase { EasingMode = WpfAnimation.EasingMode.EaseOut };
            labelScale.BeginAnimation(ScaleTransform.ScaleXProperty, new WpfAnimation.DoubleAnimation(scale, duration) { EasingFunction = easing });
            labelScale.BeginAnimation(ScaleTransform.ScaleYProperty, new WpfAnimation.DoubleAnimation(scale, duration) { EasingFunction = easing });
```

- [ ] **Step 4: Reset wheel opacity on close**

In `CloseExpressionWheel`, after `ExpressionWheelOverlay.Visibility = Visibility.Collapsed;`, add:

```csharp
        ExpressionWheelOverlay.BeginAnimation(OpacityProperty, null);
        ExpressionWheelOverlay.Opacity = 1;
        ExpressionWheelScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        ExpressionWheelScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        ExpressionWheelScaleTransform.ScaleX = 1;
        ExpressionWheelScaleTransform.ScaleY = 1;
```

- [ ] **Step 5: Run tests and build**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
dotnet build CastoPet.sln -c Release
```

Expected: all tests print `PASS`; release build reports `0 个错误`.

- [ ] **Step 6: Commit**

```powershell
git add src/CastoPet/PetWindow.xaml.cs
git commit -m "feat: smooth expression wheel motion"
```

## Task 6: Final Verification And Manual Smoke Test

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

- Idle motion has a subtle breathing feel and does not visibly jump.
- Blink still occurs occasionally and does not interrupt drag or wheel states.
- Selecting an expression fades/scales into the selected expression instead of hard-cutting.
- Expression restore fades/scales back to idle.
- Wheel opens with a short fade/scale motion.
- Wheel selection emphasis animates instead of snapping.
- Left-button dragging remains immediate.

- [ ] **Step 5: Commit any final fixes**

If Step 1, Step 2, or Step 4 required changes, commit them:

```powershell
git add src/CastoPet tests/CastoPet.Tests
git commit -m "fix: polish smoother animation behavior"
```

Skip this step if there are no final fixes.
