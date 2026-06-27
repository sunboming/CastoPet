# CastoPet Expression Transition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a shared transition-in and transition-out sequence around expression-wheel temporary expressions.

**Architecture:** Add a small core catalog for transition metadata, package four shared transition PNG resources, load them through `AssetService`, then update `PetWindow` to play `idle -> transition-in -> expression -> transition-out -> idle` with one transition frame timer. Keep wheel UI, expression options, idle, blink, and drag behavior otherwise unchanged.

**Tech Stack:** C#/.NET 10 Windows, WPF `DispatcherTimer`, existing PNG resource packaging, existing console-style test harness.

---

## File Structure

Create or modify these files:

```text
docs/superpowers/plans/2026-06-27-castopet-expression-transition.md
src/CastoPet/Core/ExpressionTransitionSequence.cs
src/CastoPet/Core/AssetService.cs
src/CastoPet/PetWindow.xaml.cs
src/CastoPet/CastoPet.csproj
src/CastoPet/Assets/Expressions/Transition/Castorice.ExpressionTransition.In.00.png
src/CastoPet/Assets/Expressions/Transition/Castorice.ExpressionTransition.In.01.png
src/CastoPet/Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.00.png
src/CastoPet/Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.01.png
tests/CastoPet.Tests/Program.cs
```

Responsibilities:

- `ExpressionTransitionSequence`: central transition frame counts, frame interval, and resource paths.
- `AssetService`: load transition frames as `ImageSource` lists.
- `PetWindow.xaml.cs`: play shared expression transition frames and coordinate cancellation with drag/wheel/expression states.
- `CastoPet.csproj`: package transition PNGs as WPF resources.
- `Program.cs`: lock transition metadata and resource path conventions.

## Task 1: Add Expression Transition Metadata

**Files:**
- Create: `src/CastoPet/Core/ExpressionTransitionSequence.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add failing transition metadata tests**

In `tests/CastoPet.Tests/Program.cs`, add these test entries after `Expression wheel paths use app resources`:

```csharp
    ("Expression transition sequence defines shared frames", ExpressionTransitionSequenceDefinesSharedFrames),
    ("Expression transition paths use app resources", ExpressionTransitionPathsUseAppResources),
```

Add these methods after `ExpressionWheelPathsUseAppResources`:

```csharp
static void ExpressionTransitionSequenceDefinesSharedFrames()
{
    Assert.Equal(2, ExpressionTransitionSequence.InFrameCount, "Transition-in should use two shared frames.");
    Assert.Equal(2, ExpressionTransitionSequence.OutFrameCount, "Transition-out should use two shared frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(80), ExpressionTransitionSequence.FrameInterval, "Expression transition frames should be brief.");
    Assert.Equal(ExpressionTransitionSequence.InFrameCount, ExpressionTransitionSequence.InFramePaths.Count, "Transition-in paths should match frame count.");
    Assert.Equal(ExpressionTransitionSequence.OutFrameCount, ExpressionTransitionSequence.OutFramePaths.Count, "Transition-out paths should match frame count.");
}

static void ExpressionTransitionPathsUseAppResources()
{
    Assert.Equal("Assets/Expressions/Transition/Castorice.ExpressionTransition.In.00.png", ExpressionTransitionSequence.InFramePaths[0], "First transition-in path should use the transition resource convention.");
    Assert.Equal("Assets/Expressions/Transition/Castorice.ExpressionTransition.In.01.png", ExpressionTransitionSequence.InFramePaths[^1], "Last transition-in path should use the transition resource convention.");
    Assert.Equal("Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.00.png", ExpressionTransitionSequence.OutFramePaths[0], "First transition-out path should use the transition resource convention.");
    Assert.Equal("Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.01.png", ExpressionTransitionSequence.OutFramePaths[^1], "Last transition-out path should use the transition resource convention.");
}
```

- [ ] **Step 2: Run tests to verify RED**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: build fails because `ExpressionTransitionSequence` does not exist.

- [ ] **Step 3: Create `ExpressionTransitionSequence`**

Create `src/CastoPet/Core/ExpressionTransitionSequence.cs`:

```csharp
namespace CastoPet.Core;

public static class ExpressionTransitionSequence
{
    public const int InFrameCount = 2;
    public const int OutFrameCount = 2;
    public static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(80);

    public static readonly IReadOnlyList<string> InFramePaths = Enumerable
        .Range(0, InFrameCount)
        .Select(index => $"Assets/Expressions/Transition/Castorice.ExpressionTransition.In.{index:00}.png")
        .ToArray();

    public static readonly IReadOnlyList<string> OutFramePaths = Enumerable
        .Range(0, OutFrameCount)
        .Select(index => $"Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.{index:00}.png")
        .ToArray();
}
```

- [ ] **Step 4: Run tests to verify GREEN**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: every test prints `PASS`, including the two expression transition sequence tests.

- [ ] **Step 5: Commit**

```powershell
git add src\CastoPet\Core\ExpressionTransitionSequence.cs tests\CastoPet.Tests\Program.cs
git commit -m "feat: add expression transition metadata"
```

## Task 2: Add Shared Transition Resources

**Files:**
- Create: `src/CastoPet/Assets/Expressions/Transition/Castorice.ExpressionTransition.In.00.png`
- Create: `src/CastoPet/Assets/Expressions/Transition/Castorice.ExpressionTransition.In.01.png`
- Create: `src/CastoPet/Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.00.png`
- Create: `src/CastoPet/Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.01.png`
- Modify: `src/CastoPet/CastoPet.csproj`

- [ ] **Step 1: Create transition resource directory**

Run:

```powershell
New-Item -ItemType Directory -Force -Path src\CastoPet\Assets\Expressions\Transition
```

Expected: directory exists at `src\CastoPet\Assets\Expressions\Transition`.

- [ ] **Step 2: Copy temporary neutral transition resources**

Use existing idle frames as first-version neutral transition resources:

```powershell
Copy-Item -LiteralPath src\CastoPet\Assets\States\Idle\Castorice.Idle.00.png -Destination src\CastoPet\Assets\Expressions\Transition\Castorice.ExpressionTransition.In.00.png -Force
Copy-Item -LiteralPath src\CastoPet\Assets\States\Idle\Castorice.Idle.01.png -Destination src\CastoPet\Assets\Expressions\Transition\Castorice.ExpressionTransition.In.01.png -Force
Copy-Item -LiteralPath src\CastoPet\Assets\States\Idle\Castorice.Idle.01.png -Destination src\CastoPet\Assets\Expressions\Transition\Castorice.ExpressionTransition.Out.00.png -Force
Copy-Item -LiteralPath src\CastoPet\Assets\States\Idle\Castorice.Idle.00.png -Destination src\CastoPet\Assets\Expressions\Transition\Castorice.ExpressionTransition.Out.01.png -Force
```

Expected: four PNG files exist and are each `320x320`.

- [ ] **Step 3: Add resources to `.csproj`**

In `src/CastoPet/CastoPet.csproj`, add these resource includes after the existing expression resources:

```xml
    <Resource Include="Assets\Expressions\Transition\Castorice.ExpressionTransition.In.00.png" />
    <Resource Include="Assets\Expressions\Transition\Castorice.ExpressionTransition.In.01.png" />
    <Resource Include="Assets\Expressions\Transition\Castorice.ExpressionTransition.Out.00.png" />
    <Resource Include="Assets\Expressions\Transition\Castorice.ExpressionTransition.Out.01.png" />
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: every test prints `PASS`; `Packaged character assets are display sized` includes the new resources and passes.

- [ ] **Step 5: Build release**

Run:

```powershell
dotnet build CastoPet.sln -c Release
```

Expected: build succeeds with `0 个错误`.

- [ ] **Step 6: Commit**

```powershell
git add src\CastoPet\Assets\Expressions\Transition src\CastoPet\CastoPet.csproj
git commit -m "assets: add shared expression transition frames"
```

## Task 3: Load Transition Frames

**Files:**
- Modify: `src/CastoPet/Core/AssetService.cs`

- [ ] **Step 1: Add transition frame load methods**

In `src/CastoPet/Core/AssetService.cs`, add these methods after `LoadBlinkFrames`:

```csharp
    public IReadOnlyList<ImageSource> LoadExpressionTransitionInFrames()
    {
        return ExpressionTransitionSequence.InFramePaths.Select(LoadCharacter).ToArray();
    }

    public IReadOnlyList<ImageSource> LoadExpressionTransitionOutFrames()
    {
        return ExpressionTransitionSequence.OutFramePaths.Select(LoadCharacter).ToArray();
    }
```

- [ ] **Step 2: Run tests**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: every test prints `PASS`.

- [ ] **Step 3: Build release**

Run:

```powershell
dotnet build CastoPet.sln -c Release
```

Expected: build succeeds with `0 个错误`.

- [ ] **Step 4: Commit**

```powershell
git add src\CastoPet\Core\AssetService.cs
git commit -m "feat: load expression transition frames"
```

## Task 4: Play Transition Frames In `PetWindow`

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml.cs`

- [ ] **Step 1: Add transition fields**

In `src/CastoPet/PetWindow.xaml.cs`, add these fields after `_temporaryExpressionTimer`:

```csharp
    private readonly DispatcherTimer _expressionTransitionFrameTimer;
    private readonly IReadOnlyList<ImageSource> _expressionTransitionInFrames;
    private readonly IReadOnlyList<ImageSource> _expressionTransitionOutFrames;
```

Add these fields after `_selectedExpressionWheelIndex`:

```csharp
    private ImageSource? _pendingExpressionImage;
    private ExpressionTransitionMode _expressionTransitionMode;
    private int _expressionTransitionFrameIndex;
```

Add this enum inside `PetWindow`, before the constructor:

```csharp
    private enum ExpressionTransitionMode
    {
        None,
        In,
        Out,
    }
```

- [ ] **Step 2: Initialize transition timer**

In the constructor, after `_temporaryExpressionTimer.Tick += (_, _) => RestoreAfterTemporaryExpression();`, add:

```csharp
        _expressionTransitionFrameTimer = new DispatcherTimer { Interval = ExpressionTransitionSequence.FrameInterval };
        _expressionTransitionFrameTimer.Tick += (_, _) => AdvanceExpressionTransitionFrame();
```

- [ ] **Step 3: Load transition frames**

Inside the constructor `try` block, after `_blinkFrames = assets.LoadBlinkFrames();`, add:

```csharp
            _expressionTransitionInFrames = assets.LoadExpressionTransitionInFrames();
            _expressionTransitionOutFrames = assets.LoadExpressionTransitionOutFrames();
```

Inside the constructor `catch` block, after `_blinkFrames = Array.Empty<ImageSource>();`, add:

```csharp
            _expressionTransitionInFrames = Array.Empty<ImageSource>();
            _expressionTransitionOutFrames = Array.Empty<ImageSource>();
```

- [ ] **Step 4: Replace direct expression apply**

Replace `ApplyTemporaryExpression` with:

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
        StopExpressionTransition();
        StopIdleAnimation();
        StopBlinkAnimation();
        _pendingExpressionImage = image;
        PlayExpressionTransitionIn();
    }
```

- [ ] **Step 5: Replace cancellation**

Replace `CancelTemporaryExpression` with:

```csharp
    private void CancelTemporaryExpression()
    {
        _temporaryExpressionTimer.Stop();
        StopExpressionTransition();
        _pendingExpressionImage = null;
        ResetCharacterTransitionAnimations();
    }
```

- [ ] **Step 6: Replace temporary expression restore**

Replace `RestoreAfterTemporaryExpression` with:

```csharp
    private void RestoreAfterTemporaryExpression()
    {
        _temporaryExpressionTimer.Stop();
        _pendingExpressionImage = null;
        StopIdleAnimation();
        StopBlinkAnimation();
        PlayExpressionTransitionOut();
    }
```

- [ ] **Step 7: Add transition playback helpers**

Add these methods before `StartIdleBreathing`:

```csharp
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
    }

    private void StopExpressionTransition()
    {
        _expressionTransitionFrameTimer.Stop();
        _expressionTransitionMode = ExpressionTransitionMode.None;
        _expressionTransitionFrameIndex = 0;
    }
```

- [ ] **Step 8: Run tests**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: every test prints `PASS`.

- [ ] **Step 9: Build release**

Run:

```powershell
dotnet build CastoPet.sln -c Release
```

Expected: build succeeds with `0 个错误`.

- [ ] **Step 10: Commit**

```powershell
git add src\CastoPet\PetWindow.xaml.cs
git commit -m "feat: play expression transition frames"
```

## Task 5: Final Verification

**Files:**
- Modify only files required to fix failed checks.

- [ ] **Step 1: Run test harness**

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

Expected:

```text
已成功生成。
    0 个警告
    0 个错误
```

- [ ] **Step 3: Inspect git status**

Run:

```powershell
git status --short
```

Expected: only existing unrelated untracked entries remain:

```text
?? .codex/
?? Castorice.png
?? sample/
```

- [ ] **Step 4: Manual smoke test**

Run the app for direct observation:

```powershell
dotnet run --project src\CastoPet\CastoPet.csproj
```

Expected behavior:

- Selecting an expression from the wheel briefly shows transition-in frames before the expression.
- Temporary expression still holds for the existing duration.
- Transition-out frames play before returning to idle.
- Right-click wheel behavior remains unchanged.
- Left-button dragging cancels expression transition immediately.
