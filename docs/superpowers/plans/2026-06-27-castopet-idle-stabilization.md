# CastoPet Idle Stabilization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the most likely sources of idle body jitter while keeping the idle animation at exactly 8 frames.

**Architecture:** Treat this as a stabilization pass, not a new animation system. Restore the experimental blended idle frame, neutralize the WPF breathing transform, and add repeatable PNG diagnostics in the existing test harness so the next art correction is based on measured frame anchors.

**Tech Stack:** C#/.NET 10 Windows, WPF project assets, existing console-style test harness, `System.Drawing` in tests for PNG diagnostics.

---

## File Structure

Create or modify these files:

```text
docs/superpowers/plans/2026-06-27-castopet-idle-stabilization.md
src/CastoPet/Core/PetAnimationTimings.cs
src/CastoPet/Assets/States/Idle/Castorice.Idle.02.png
tests/CastoPet.Tests/Program.cs
```

Responsibilities:

- `PetAnimationTimings`: temporarily neutralize idle breathing transform values while preserving expression and wheel timings.
- `Castorice.Idle.02.png`: restore the original tracked frame from before the midpoint-blend experiment.
- `Program.cs`: update timing expectations and add idle PNG diagnostics helpers/tests.

## Task 1: Restore The Original Idle 02 Frame

**Files:**
- Modify: `src/CastoPet/Assets/States/Idle/Castorice.Idle.02.png`

- [ ] **Step 1: Restore `Idle.02` from the pre-blend commit**

Run:

```powershell
git restore --source=a07c373 -- src\CastoPet\Assets\States\Idle\Castorice.Idle.02.png
```

Expected: `git status --short` shows `M src/CastoPet/Assets/States/Idle/Castorice.Idle.02.png`.

- [ ] **Step 2: Verify the restored file size matches the original frame**

Run:

```powershell
Get-Item -LiteralPath src\CastoPet\Assets\States\Idle\Castorice.Idle.02.png | Select-Object Name,Length
```

Expected:

```text
Name                  Length
----                  ------
Castorice.Idle.02.png 115063
```

- [ ] **Step 3: Run packaged asset tests**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: all existing tests print `PASS`.

- [ ] **Step 4: Commit**

```powershell
git add src\CastoPet\Assets\States\Idle\Castorice.Idle.02.png
git commit -m "fix: restore original idle frame"
```

## Task 2: Neutralize Idle Breathing Transform

**Files:**
- Modify: `src/CastoPet/Core/PetAnimationTimings.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Rename and tighten the breathing test**

In `tests/CastoPet.Tests/Program.cs`, replace this test entry:

```csharp
    ("Idle breathing values are subtle", IdleBreathingValuesAreSubtle),
```

with:

```csharp
    ("Idle breathing values are neutral during stabilization", IdleBreathingValuesAreNeutralDuringStabilization),
```

Replace the existing `IdleBreathingValuesAreSubtle` method with:

```csharp
static void IdleBreathingValuesAreNeutralDuringStabilization()
{
    Assert.Equal(TimeSpan.FromMilliseconds(1900), PetAnimationTimings.IdleBreathingCycleDuration, "Idle breathing cycle duration should stay available for later tuning.");
    Assert.Equal(0d, PetAnimationTimings.IdleBreathingTranslateY, "Idle breathing vertical movement should be disabled while stabilizing frame anchors.");
    Assert.Equal(0d, PetAnimationTimings.IdleBreathingScaleDelta, "Idle breathing scale should be disabled while stabilizing frame anchors.");
    Assert.Equal(0.96, PetAnimationTimings.ExpressionDimmedOpacity, "Expression transition should only slightly dim during swaps.");
    Assert.Equal(0.92, PetAnimationTimings.WheelOpenStartScale, "Wheel should open from a small scale change.");
}
```

- [ ] **Step 2: Run tests to verify RED**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: the renamed idle breathing test fails because `IdleBreathingTranslateY` is currently `3` and `IdleBreathingScaleDelta` is currently `0.012`.

- [ ] **Step 3: Neutralize breathing constants**

In `src/CastoPet/Core/PetAnimationTimings.cs`, change:

```csharp
    public const double IdleBreathingTranslateY = 3;
    public const double IdleBreathingScaleDelta = 0.012;
```

to:

```csharp
    public const double IdleBreathingTranslateY = 0;
    public const double IdleBreathingScaleDelta = 0;
```

- [ ] **Step 4: Run tests to verify GREEN**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: every test prints `PASS`, including `Idle breathing values are neutral during stabilization`.

- [ ] **Step 5: Commit**

```powershell
git add src\CastoPet\Core\PetAnimationTimings.cs tests\CastoPet.Tests\Program.cs
git commit -m "fix: neutralize idle breathing transform"
```

## Task 3: Add Idle PNG Diagnostics

**Files:**
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add `System.Drawing` using**

At the top of `tests/CastoPet.Tests/Program.cs`, add:

```csharp
using System.Drawing;
```

The top of the file should become:

```csharp
using System.Drawing;
using CastoPet.Core;
```

- [ ] **Step 2: Add the failing diagnostics test entry**

In the `tests` array, add this entry after `Idle frame sequence defines eight slow frame paths`:

```csharp
    ("Idle frame diagnostics read all packaged frames", IdleFrameDiagnosticsReadAllPackagedFrames),
```

- [ ] **Step 3: Add the failing diagnostics test method**

Add this method after `IdleFrameSequenceDefinesEightSlowFramePaths`:

```csharp
static void IdleFrameDiagnosticsReadAllPackagedFrames()
{
    var diagnostics = ReadIdleFrameDiagnostics();

    Assert.Equal(IdleFrameSequence.FrameCount, diagnostics.Count, "Diagnostics should include all idle frames.");
    Assert.True(diagnostics.All(frame => frame.Width == AssetService.CharacterDecodePixelWidth), "Idle frames should keep the display width.");
    Assert.True(diagnostics.All(frame => frame.Height == AssetService.CharacterDecodePixelWidth), "Idle frames should keep the display height.");
    Assert.True(diagnostics.All(frame => frame.Bounds.Width > 0 && frame.Bounds.Height > 0), "Idle frames should have visible alpha bounds.");
    Assert.True(diagnostics.Max(frame => frame.Bounds.Bottom) - diagnostics.Min(frame => frame.Bounds.Bottom) <= 1, "Idle frame bottom edges should stay anchored.");
    Assert.True(diagnostics.Max(frame => frame.CenterX) - diagnostics.Min(frame => frame.CenterX) <= 1.0, "Idle frame centers should stay horizontally anchored.");
    Assert.Equal("Castorice.Idle.07.png", diagnostics[^1].Name, "Diagnostics should preserve frame order.");
}
```

This test references helper types that do not exist yet, so the project should fail to compile in the next step.

- [ ] **Step 4: Run tests to verify RED**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: build fails because `ReadIdleFrameDiagnostics` and the diagnostics record do not exist yet.

- [ ] **Step 5: Add diagnostics helpers**

Add these helper types and methods before `FindWorkspaceRoot`:

```csharp
static IReadOnlyList<IdleFrameDiagnostic> ReadIdleFrameDiagnostics()
{
    var workspace = FindWorkspaceRoot();
    var idleRoot = System.IO.Path.Combine(workspace, "src", "CastoPet", "Assets", "States", "Idle");
    var frames = Directory
        .EnumerateFiles(idleRoot, "Castorice.Idle.*.png", SearchOption.TopDirectoryOnly)
        .OrderBy(System.IO.Path.GetFileName, StringComparer.Ordinal)
        .ToArray();

    var diagnostics = new List<IdleFrameDiagnostic>();
    for (var index = 0; index < frames.Length; index++)
    {
        using var bitmap = new Bitmap(frames[index]);
        var bounds = FindVisibleBounds(bitmap);
        diagnostics.Add(new IdleFrameDiagnostic(
            Name: System.IO.Path.GetFileName(frames[index]),
            Width: bitmap.Width,
            Height: bitmap.Height,
            Bounds: bounds,
            CenterX: bounds.Left + bounds.Width / 2d,
            AdjacentAverageDelta: 0));
    }

    for (var index = 0; index < diagnostics.Count; index++)
    {
        var current = frames[index];
        var next = frames[(index + 1) % frames.Length];
        using var currentBitmap = new Bitmap(current);
        using var nextBitmap = new Bitmap(next);
        diagnostics[index] = diagnostics[index] with
        {
            AdjacentAverageDelta = CalculateAverageRgbaDelta(currentBitmap, nextBitmap),
        };
    }

    return diagnostics;
}

static Rectangle FindVisibleBounds(Bitmap bitmap)
{
    var minX = bitmap.Width;
    var minY = bitmap.Height;
    var maxX = -1;
    var maxY = -1;

    for (var y = 0; y < bitmap.Height; y++)
    {
        for (var x = 0; x < bitmap.Width; x++)
        {
            if (bitmap.GetPixel(x, y).A <= 8)
            {
                continue;
            }

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }
    }

    if (maxX < minX || maxY < minY)
    {
        return Rectangle.Empty;
    }

    return Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
}

static double CalculateAverageRgbaDelta(Bitmap current, Bitmap next)
{
    if (current.Width != next.Width || current.Height != next.Height)
    {
        throw new InvalidOperationException("Idle frames must have matching dimensions.");
    }

    long total = 0;
    long samples = 0;
    for (var y = 0; y < current.Height; y += 2)
    {
        for (var x = 0; x < current.Width; x += 2)
        {
            var a = current.GetPixel(x, y);
            var b = next.GetPixel(x, y);
            total += Math.Abs(a.R - b.R);
            total += Math.Abs(a.G - b.G);
            total += Math.Abs(a.B - b.B);
            total += Math.Abs(a.A - b.A);
            samples++;
        }
    }

    return total / (samples * 4d);
}

readonly record struct IdleFrameDiagnostic(
    string Name,
    int Width,
    int Height,
    Rectangle Bounds,
    double CenterX,
    double AdjacentAverageDelta);
```

- [ ] **Step 6: Run tests to verify GREEN**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: every test prints `PASS`, including `Idle frame diagnostics read all packaged frames`.

- [ ] **Step 7: Print diagnostics for human review**

Run this one-off command:

```powershell
Add-Type -AssemblyName System.Drawing; $files=Get-ChildItem -LiteralPath src\CastoPet\Assets\States\Idle -Filter *.png | Sort-Object Name; $bitmaps=@($files | ForEach-Object { [System.Drawing.Bitmap]::new($_.FullName) }); for($i=0;$i -lt $bitmaps.Count;$i++){ $bmp=$bitmaps[$i]; $minX=$bmp.Width; $minY=$bmp.Height; $maxX=-1; $maxY=-1; for($y=0;$y -lt $bmp.Height;$y++){ for($x=0;$x -lt $bmp.Width;$x++){ if($bmp.GetPixel($x,$y).A -gt 8){ if($x -lt $minX){$minX=$x}; if($y -lt $minY){$minY=$y}; if($x -gt $maxX){$maxX=$x}; if($y -gt $maxY){$maxY=$y} } } }; $center=($minX+$maxX+1)/2; Write-Output ("{0}: bbox=({1},{2})-({3},{4}) centerX={5:N1}" -f $files[$i].Name,$minX,$minY,$maxX,$maxY,$center) }; for($i=0;$i -lt $bitmaps.Count;$i++){ $a=$bitmaps[$i]; $b=$bitmaps[($i+1)%$bitmaps.Count]; $sum=0L; $count=0L; for($y=0;$y -lt $a.Height;$y+=2){ for($x=0;$x -lt $a.Width;$x+=2){ $pa=$a.GetPixel($x,$y); $pb=$b.GetPixel($x,$y); $sum += [Math]::Abs($pa.R-$pb.R)+[Math]::Abs($pa.G-$pb.G)+[Math]::Abs($pa.B-$pb.B)+[Math]::Abs($pa.A-$pb.A); $count++ } }; Write-Output ("diff {0}->{1}: avg_rgba_delta={2:N2}" -f $files[$i].Name,$files[($i+1)%$files.Count].Name,($sum/(4*$count))) }; $bitmaps | ForEach-Object { $_.Dispose() }
```

Expected after restoring original `Idle.02`: bottom edges should stay at `300` or within one pixel, centers should stay around `159`, and the largest adjacent deltas identify the remaining visual-art candidates for a later art pass.

- [ ] **Step 8: Commit**

```powershell
git add tests\CastoPet.Tests\Program.cs
git commit -m "test: add idle frame diagnostics"
```

## Task 4: Final Verification

**Files:**
- Modify only files required to fix failed checks.

- [ ] **Step 1: Run the full test harness**

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

- [ ] **Step 3: Inspect final status**

Run:

```powershell
git status --short
```

Expected: only the existing unrelated untracked entries remain:

```text
?? .codex/
?? Castorice.png
?? sample/
```

- [ ] **Step 4: Manual observation note**

Run the app locally only if a direct human observation pass is desired:

```powershell
dotnet run --project src\CastoPet\CastoPet.csproj
```

Expected behavior:

- Idle no longer has extra WPF breathing bob/scale.
- If visible body jitter remains, it is likely coming from the 8 PNG frames themselves.
- Right-click wheel, temporary expressions, blink, and dragging still work as before.
