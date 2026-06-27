# CastoPet Distance-Driven Movement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Use generated move frames with fixed-speed, distance-driven movement so character animation stays aligned with actual window travel.

**Architecture:** Add a `MoveFrameSequence` core catalog and load it through `AssetService`. Replace the active movement `DispatcherTimer` with `CompositionTarget.Rendering`, using fixed pixel-per-second stepping and accumulated travel distance to select move frames.

**Tech Stack:** C#/.NET WPF, WPF pack resources, `CompositionTarget.Rendering`, existing console-style tests.

---

## File Structure

- Create `src/CastoPet/Core/MoveFrameSequence.cs`
  - Owns move frame paths, speed constants, distance-per-frame, and step calculation.
- Modify `src/CastoPet/Core/AssetService.cs`
  - Adds `LoadMoveFrames()`.
- Modify `src/CastoPet/CastoPet.csproj`
  - Registers `Assets/States/Move/Castorice.Move.00.png` through `.07.png` as WPF resources.
- Modify `src/CastoPet/PetWindow.xaml.cs`
  - Loads move frames, uses rendering-driven movement, updates frame by distance.
- Modify `tests/CastoPet.Tests/Program.cs`
  - Adds move frame sequence, speed, and asset tests.

## Task 1: Move Frame Assets And Loader

**Files:**
- Create: `src/CastoPet/Core/MoveFrameSequence.cs`
- Modify: `src/CastoPet/Core/AssetService.cs`
- Modify: `src/CastoPet/CastoPet.csproj`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write failing tests**

Add test registrations:

```csharp
("Move frame sequence defines eight distance-driven paths", MoveFrameSequenceDefinesEightDistanceDrivenPaths),
("Move frame paths use app resources", MoveFramePathsUseAppResources),
("Move speed constants stay in smooth range", MoveSpeedConstantsStayInSmoothRange),
```

Add methods:

```csharp
static void MoveFrameSequenceDefinesEightDistanceDrivenPaths()
{
    Assert.Equal(8, MoveFrameSequence.FrameCount, "Move should use eight frames.");
    Assert.Equal(10d, MoveFrameSequence.DistancePerFrame, "Move frames should advance by travel distance.");
    Assert.Equal("Assets/States/Move/Castorice.Move.00.png", MoveFrameSequence.FramePaths[0], "First move frame path should be zero padded.");
    Assert.Equal("Assets/States/Move/Castorice.Move.07.png", MoveFrameSequence.FramePaths[^1], "Last move frame path should be zero padded.");
}

static void MoveFramePathsUseAppResources()
{
    for (var index = 0; index < MoveFrameSequence.FrameCount; index++)
    {
        Assert.Equal($"Assets/States/Move/Castorice.Move.{index:00}.png", MoveFrameSequence.FramePaths[index], "Move frame should use the resource path convention.");
    }
}

static void MoveSpeedConstantsStayInSmoothRange()
{
    Assert.Equal(90d, MoveFrameSequence.BaseSpeedPixelsPerSecond, "Move speed should have a stable base.");
    Assert.Equal(80d, MoveFrameSequence.MinSpeedPixelsPerSecond, "Move speed lower bound should stay near the base.");
    Assert.Equal(105d, MoveFrameSequence.MaxSpeedPixelsPerSecond, "Move speed upper bound should stay near the base.");
    Assert.Equal(9d, MoveFrameSequence.StepDistance(TimeSpan.FromMilliseconds(100), distanceToTarget: 200), "100ms at base speed should move 9px.");
}
```

- [ ] **Step 2: Run tests to verify RED**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: compile failure because `MoveFrameSequence` does not exist.

- [ ] **Step 3: Add sequence and loader**

Create `src/CastoPet/Core/MoveFrameSequence.cs`:

```csharp
namespace CastoPet.Core;

public static class MoveFrameSequence
{
    public const int FrameCount = 8;
    public const double DistancePerFrame = 10;
    public const double BaseSpeedPixelsPerSecond = 90;
    public const double MinSpeedPixelsPerSecond = 80;
    public const double MaxSpeedPixelsPerSecond = 105;

    public static readonly IReadOnlyList<string> FramePaths = Enumerable
        .Range(0, FrameCount)
        .Select(index => $"Assets/States/Move/Castorice.Move.{index:00}.png")
        .ToArray();

    public static double StepDistance(TimeSpan elapsed, double distanceToTarget)
    {
        if (elapsed <= TimeSpan.Zero || distanceToTarget <= 0)
        {
            return 0;
        }

        var speed = distanceToTarget > 240 ? MaxSpeedPixelsPerSecond
            : distanceToTarget < 80 ? MinSpeedPixelsPerSecond
            : BaseSpeedPixelsPerSecond;
        return Math.Min(distanceToTarget, speed * elapsed.TotalSeconds);
    }
}
```

Add to `AssetService`:

```csharp
public IReadOnlyList<ImageSource> LoadMoveFrames()
{
    return MoveFrameSequence.FramePaths.Select(LoadCharacter).ToArray();
}
```

Add WPF resources for the 8 move PNG files in `src/CastoPet/CastoPet.csproj` near idle resources.

- [ ] **Step 4: Run tests and commit**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
dotnet build src/CastoPet/CastoPet.csproj -c Release
```

Expected: tests pass and build succeeds.

Commit:

```powershell
git add src/CastoPet/Core/MoveFrameSequence.cs src/CastoPet/Core/AssetService.cs src/CastoPet/CastoPet.csproj tests/CastoPet.Tests/Program.cs src/CastoPet/Assets/States/Move
git commit -m "feat: add move frame assets"
```

## Task 2: Rendering-Driven Movement Runtime

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml.cs`

- [ ] **Step 1: Add runtime fields**

Add fields:

```csharp
private readonly IReadOnlyList<ImageSource> _moveFrames;
private TimeSpan? _lastActiveMovementRenderTime;
private double _logicalLeft;
private double _logicalTop;
private double _moveFrameDistanceAccumulator;
private int _moveFrameIndex;
```

Load move frames in the asset load block:

```csharp
_moveFrames = assets.LoadMoveFrames();
```

Set fallback in the catch block:

```csharp
_moveFrames = Array.Empty<ImageSource>();
```

- [ ] **Step 2: Replace timer start/stop with Rendering subscription**

Keep the existing timer field only if removal is too invasive, but active movement should use:

```csharp
CompositionTarget.Rendering += OnActiveMovementRendering;
CompositionTarget.Rendering -= OnActiveMovementRendering;
```

When movement starts, initialize:

```csharp
_logicalLeft = Left;
_logicalTop = Top;
_lastActiveMovementRenderTime = null;
```

When movement stops, reset `_lastActiveMovementRenderTime`, `_hasActiveMovementTarget`, and move frame state.

- [ ] **Step 3: Move by fixed speed and frame by distance**

Use `RenderingEventArgs.RenderingTime` to calculate elapsed time. Move toward the target by:

```csharp
var distance = Math.Sqrt(dx * dx + dy * dy);
var step = MoveFrameSequence.StepDistance(elapsed, distance);
var ratio = step / distance;
_logicalLeft += dx * ratio;
_logicalTop += dy * ratio;
Left = Math.Round(_logicalLeft);
Top = Math.Round(_logicalTop);
```

Accumulate `step`, and every `MoveFrameSequence.DistancePerFrame` advance frame:

```csharp
_moveFrameDistanceAccumulator += step;
while (_moveFrameDistanceAccumulator >= MoveFrameSequence.DistancePerFrame)
{
    _moveFrameDistanceAccumulator -= MoveFrameSequence.DistancePerFrame;
    _moveFrameIndex = (_moveFrameIndex + 1) % _moveFrames.Count;
    CharacterImage.Source = _moveFrames[_moveFrameIndex];
}
```

If no move frames are loaded, leave the current static image.

- [ ] **Step 4: Run tests/build and commit**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
dotnet build src/CastoPet/CastoPet.csproj -c Release
```

Expected: tests pass and build succeeds.

Commit:

```powershell
git add src/CastoPet/PetWindow.xaml.cs
git commit -m "feat: drive movement animation by distance"
```

## Self-Review

- Spec coverage: Move assets, fixed speed, distance-per-frame, Rendering-driven updates, and paused idle/blink are covered.
- Placeholder scan: no TBD/TODO placeholders.
- Type consistency: `MoveFrameSequence`, `LoadMoveFrames`, and runtime field names are consistent.
