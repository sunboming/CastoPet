# CastoPet Random Blink Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add natural random blinking on top of the existing body idle animation.

**Architecture:** Add a small blink sequence configuration class in `CastoPet.Core`, load blink resources through `AssetService`, and let `PetWindow` run a separate random scheduling timer plus short blink playback timer. Dragging stops blink playback and scheduling, then idle and blinking resume after release.

**Tech Stack:** C# 10/.NET 10 WPF, `DispatcherTimer`, WPF pack resources, existing lightweight console tests.

---

### Task 1: Blink Sequence Configuration

**Files:**
- Create: `src/CastoPet/Core/BlinkFrameSequence.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write the failing test**

Add a test named `BlinkFrameSequenceDefinesRandomBlinkFrames` to `tests/CastoPet.Tests/Program.cs`:

```csharp
static void BlinkFrameSequenceDefinesRandomBlinkFrames()
{
    Assert.Equal(3, BlinkFrameSequence.FrameCount, "Blink should use three frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(90), BlinkFrameSequence.FrameInterval, "Blink frames should advance quickly.");
    Assert.Equal(TimeSpan.FromSeconds(3), BlinkFrameSequence.MinScheduleDelay, "Blink should not repeat too frequently.");
    Assert.Equal(TimeSpan.FromSeconds(7), BlinkFrameSequence.MaxScheduleDelay, "Blink should remain occasional.");
    Assert.Equal("Assets/States/Blink/Castorice.Blink.00.png", BlinkFrameSequence.FramePaths[0], "First blink frame path should be zero padded.");
    Assert.Equal("Assets/States/Blink/Castorice.Blink.02.png", BlinkFrameSequence.FramePaths[^1], "Last blink frame path should be zero padded.");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj`

Expected: compile failure because `BlinkFrameSequence` does not exist.

- [ ] **Step 3: Write minimal implementation**

Create `src/CastoPet/Core/BlinkFrameSequence.cs`:

```csharp
namespace CastoPet.Core;

public static class BlinkFrameSequence
{
    public const int FrameCount = 3;
    public static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(90);
    public static readonly TimeSpan MinScheduleDelay = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan MaxScheduleDelay = TimeSpan.FromSeconds(7);

    public static readonly IReadOnlyList<string> FramePaths = Enumerable
        .Range(0, FrameCount)
        .Select(index => $"Assets/States/Blink/Castorice.Blink.{index:00}.png")
        .ToArray();
}
```

- [ ] **Step 4: Run test to verify it passes after resources are present**

Run: `dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj`

Expected after Task 2 resource creation: all tests pass.

### Task 2: Blink Assets and Resource Registration

**Files:**
- Create: `src/CastoPet/Assets/States/Blink/Castorice.Blink.00.png`
- Create: `src/CastoPet/Assets/States/Blink/Castorice.Blink.01.png`
- Create: `src/CastoPet/Assets/States/Blink/Castorice.Blink.02.png`
- Modify: `src/CastoPet/CastoPet.csproj`

- [ ] **Step 1: Generate three blink frames**

Use the existing Castorice sprite as visual reference. Generate three chroma-key source images and remove the green background locally to create transparent PNGs.

- [ ] **Step 2: Register resources**

Add these resource entries to `src/CastoPet/CastoPet.csproj`:

```xml
<Resource Include="Assets\States\Blink\Castorice.Blink.00.png" />
<Resource Include="Assets\States\Blink\Castorice.Blink.01.png" />
<Resource Include="Assets\States\Blink\Castorice.Blink.02.png" />
```

### Task 3: Runtime Blink Playback

**Files:**
- Modify: `src/CastoPet/Core/AssetService.cs`
- Modify: `src/CastoPet/PetWindow.xaml.cs`

- [ ] **Step 1: Load blink frames**

Add to `AssetService`:

```csharp
public IReadOnlyList<ImageSource> LoadBlinkFrames()
{
    return BlinkFrameSequence.FramePaths.Select(LoadCharacter).ToArray();
}
```

- [ ] **Step 2: Add timers and playback state**

Add to `PetWindow`:

```csharp
private readonly IReadOnlyList<ImageSource> _blinkFrames;
private readonly DispatcherTimer _blinkScheduleTimer;
private readonly DispatcherTimer _blinkFrameTimer;
private readonly Random _blinkRandom = new();
private bool _isBlinking;
private int _blinkFrameIndex;
```

- [ ] **Step 3: Implement scheduling and playback**

Add methods that schedule the next blink between 3 and 7 seconds, show blink frames at 90ms intervals, restore the current idle frame after the last blink frame, and stop all blink timers during drag.

### Task 4: Verification and Commit

**Files:**
- All files changed above.

- [ ] **Step 1: Run tests**

Run: `dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj`

Expected: all tests pass.

- [ ] **Step 2: Build release**

Run: `dotnet build CastoPet.sln -c Release`

Expected: `0 个警告`, `0 个错误`.

- [ ] **Step 3: Runtime smoke test**

Run the Release exe, start it a second time, confirm only one `CastoPet.exe` process remains, then stop the test process.

- [ ] **Step 4: Commit**

Commit message:

```bash
git commit -m "feat: add random blink animation"
```
