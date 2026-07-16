# Radial Wheel Visual Refinement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the two-level radial wheel more readable and polished while preserving its current geometry and interaction behavior.

**Architecture:** Introduce a WPF-independent internal style definition in `CastoPet.Core` that owns wheel colors, opacity, stroke, gap, and label-shadow values. `PetWindow` will consume that definition for initial rendering and selection refresh, and each rendered item will retain its ring identity so deselection restores the correct ring-specific style.

**Tech Stack:** C# 14, .NET 10, WPF, existing console-based CastoPet test harness

---

## File Structure

- Create `src/CastoPet/Core/RadialWheelStyle.cs`: WPF-independent wheel palette and numeric visual constants.
- Create `src/CastoPet/Properties/AssemblyInfo.cs`: expose internal style contracts to the existing test assembly.
- Modify `src/CastoPet/PetWindow.xaml`: refine the center circle fill and outline.
- Modify `src/CastoPet/PetWindow.xaml.cs`: consume centralized style values and preserve ring identity across selection updates.
- Modify `tests/CastoPet.Tests/Program.cs`: verify opacity hierarchy, ring distinction, and renderer integration.

### Task 1: Add the Radial Wheel Style Contract

**Files:**
- Create: `src/CastoPet/Core/RadialWheelStyle.cs`
- Create: `src/CastoPet/Properties/AssemblyInfo.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Register and write the failing style test**

Add this entry next to the existing radial-wheel tests:

```csharp
("Radial wheel style keeps readable ring hierarchy", RadialWheelStyleKeepsReadableRingHierarchy),
```

Add this test beside `RadialWheelLayoutKeepsGenericTwoRingGeometry`:

```csharp
static void RadialWheelStyleKeepsReadableRingHierarchy()
{
    var first = RadialWheelStyle.GetNormalFill(RadialWheelRing.First, isEnabled: true);
    var second = RadialWheelStyle.GetNormalFill(RadialWheelRing.Second, isEnabled: true);
    var firstDisabled = RadialWheelStyle.GetNormalFill(RadialWheelRing.First, isEnabled: false);
    var secondDisabled = RadialWheelStyle.GetNormalFill(RadialWheelRing.Second, isEnabled: false);

    Assert.Equal((byte)140, first.Alpha, "First-ring fill should be readable over the desktop.");
    Assert.Equal((byte)122, second.Alpha, "Second-ring fill should remain slightly lighter.");
    Assert.Equal((byte)84, firstDisabled.Alpha, "Disabled first-ring fill should remain subdued.");
    Assert.Equal((byte)72, secondDisabled.Alpha, "Disabled second-ring fill should remain subdued.");
    Assert.False(first.Equals(second), "The two normal ring fills should remain visually distinct.");
    Assert.True(RadialWheelStyle.SelectedFill.Alpha > first.Alpha, "Selection should be stronger than the first ring.");
    Assert.True(RadialWheelStyle.SelectedFill.Alpha > second.Alpha, "Selection should be stronger than the second ring.");
    Assert.Equal(0.016d, RadialWheelStyle.SectorGapRadians, "Sector dividers should use the refined gap.");
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj -c Debug
```

Expected: build failure reporting that `RadialWheelStyle` does not exist.

- [ ] **Step 3: Implement the WPF-independent style definition**

Create `src/CastoPet/Core/RadialWheelStyle.cs`:

```csharp
namespace CastoPet.Core;

internal readonly record struct RadialWheelColor(byte Alpha, byte Red, byte Green, byte Blue);

internal static class RadialWheelStyle
{
    private static readonly RadialWheelColor FirstRingFill = new(140, 66, 42, 110);
    private static readonly RadialWheelColor SecondRingFill = new(122, 86, 57, 132);
    private static readonly RadialWheelColor FirstRingDisabledFill = new(84, 66, 42, 110);
    private static readonly RadialWheelColor SecondRingDisabledFill = new(72, 86, 57, 132);

    public static readonly RadialWheelColor SelectedFill = new(191, 126, 87, 188);
    public static readonly RadialWheelColor NormalStroke = new(150, 236, 224, 255);
    public static readonly RadialWheelColor SelectedStroke = new(235, 250, 242, 255);

    public const double NormalStrokeThickness = 0.9;
    public const double SelectedStrokeThickness = 1.5;
    public const double SectorGapRadians = 0.016;
    public const byte LabelShadowAlpha = 120;
    public const double LabelShadowBlurRadius = 5;
    public const double LabelShadowOpacity = 0.58;

    public static RadialWheelColor GetNormalFill(RadialWheelRing ring, bool isEnabled) =>
        (ring, isEnabled) switch
        {
            (RadialWheelRing.First, true) => FirstRingFill,
            (RadialWheelRing.Second, true) => SecondRingFill,
            (RadialWheelRing.First, false) => FirstRingDisabledFill,
            (RadialWheelRing.Second, false) => SecondRingDisabledFill,
            _ => throw new ArgumentOutOfRangeException(nameof(ring), ring, "Only selectable wheel rings have sector fills."),
        };
}
```

Create `src/CastoPet/Properties/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CastoPet.Tests")]
```

- [ ] **Step 4: Run the test suite to verify it passes**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj -c Debug
```

Expected: every registered test prints `PASS` and the process exits with code 0.

- [ ] **Step 5: Commit the style contract**

```powershell
git add -- src/CastoPet/Core/RadialWheelStyle.cs src/CastoPet/Properties/AssemblyInfo.cs tests/CastoPet.Tests/Program.cs
git commit -m "refactor: centralize radial wheel styling"
```

### Task 2: Apply the Refined Style to Both Rings

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml`
- Modify: `src/CastoPet/PetWindow.xaml.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Register and write the failing renderer integration test**

Add this test entry:

```csharp
("Pet window consumes centralized radial wheel styling", PetWindowConsumesCentralizedRadialWheelStyling),
```

Add the test beside the existing PetWindow radial-wheel source tests:

```csharp
static void PetWindowConsumesCentralizedRadialWheelStyling()
{
    var workspace = FindWorkspaceRoot();
    var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "PetWindow.xaml.cs"));
    var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "PetWindow.xaml"));

    Assert.Contains(source, "RadialWheelStyle.GetNormalFill", "Initial and restored fills should use the shared style contract.");
    Assert.Contains(source, "visual.Ring", "Selection refresh should restore the visual's original ring style.");
    Assert.Contains(source, "RadialWheelStyle.SectorGapRadians", "Sector geometry should use the refined divider gap.");
    Assert.Contains(source, "RadialWheelStyle.LabelShadowOpacity", "Label shadows should use the refined style.");
    Assert.Contains(xaml, "Fill=\"#80352757\"", "The center should use the approved stronger fill.");
    Assert.Contains(xaml, "Stroke=\"#96ECE0FF\"", "The center should use the softened outline.");
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj -c Debug
```

Expected: the new test fails because `PetWindow` still contains the old hard-coded styling.

- [ ] **Step 3: Preserve ring identity on every rendered wheel item**

Extend `RadialWheelItemVisual` with a `RadialWheelRing ring` constructor argument and property:

```csharp
public RadialWheelRing Ring { get; } = ring;
```

In `AddRadialWheelItem`, derive the ring once:

```csharp
var ring = isSecondRing ? RadialWheelRing.Second : RadialWheelRing.First;
```

Pass `ring` when creating `RadialWheelItemVisual`.

- [ ] **Step 4: Replace hard-coded sector and label styling**

Add this conversion helper in `PetWindow`:

```csharp
private static WpfColor ToWpfColor(RadialWheelColor color) =>
    WpfColor.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
```

Construct sectors from the shared style and stop reducing the whole disabled panel opacity:

```csharp
Opacity = 1,
```

```csharp
Fill = new SolidColorBrush(ToWpfColor(RadialWheelStyle.GetNormalFill(ring, isEnabled))),
Stroke = new SolidColorBrush(ToWpfColor(RadialWheelStyle.NormalStroke)),
StrokeThickness = RadialWheelStyle.NormalStrokeThickness,
```

Use the shared label-shadow values:

```csharp
Color = WpfColor.FromArgb(RadialWheelStyle.LabelShadowAlpha, 40, 25, 68),
BlurRadius = RadialWheelStyle.LabelShadowBlurRadius,
ShadowDepth = 0,
Opacity = RadialWheelStyle.LabelShadowOpacity,
```

Replace the local geometry gap with:

```csharp
var gap = RadialWheelStyle.SectorGapRadians;
```

- [ ] **Step 5: Restore ring-specific styles during selection refresh**

Replace the selection fill, stroke, and thickness assignments with:

```csharp
var fill = isSelected
    ? RadialWheelStyle.SelectedFill
    : RadialWheelStyle.GetNormalFill(visual.Ring, visual.IsEnabled);
visual.Sector.Fill = new SolidColorBrush(ToWpfColor(fill));
visual.Sector.Stroke = new SolidColorBrush(ToWpfColor(
    isSelected ? RadialWheelStyle.SelectedStroke : RadialWheelStyle.NormalStroke));
visual.Sector.StrokeThickness = isSelected
    ? RadialWheelStyle.SelectedStrokeThickness
    : RadialWheelStyle.NormalStrokeThickness;
```

Keep the existing label opacity, font-weight, scale, duration, and easing behavior unchanged.

- [ ] **Step 6: Refine the center circle**

Change the center ellipse in `PetWindow.xaml` to:

```xml
Fill="#80352757"
Stroke="#96ECE0FF"
StrokeThickness="0.9"
```

- [ ] **Step 7: Run the test suite to verify it passes**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj -c Debug
```

Expected: every registered test prints `PASS` and the process exits with code 0.

- [ ] **Step 8: Commit the renderer changes**

```powershell
git add -- src/CastoPet/PetWindow.xaml src/CastoPet/PetWindow.xaml.cs tests/CastoPet.Tests/Program.cs
git commit -m "style: refine radial wheel appearance"
```

### Task 3: Verify Debug and Release Outputs

**Files:**
- Verify only; no planned source changes.

- [ ] **Step 1: Run the full Debug test suite**

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj -c Debug
```

Expected: all tests pass with exit code 0.

- [ ] **Step 2: Build the Debug application**

```powershell
dotnet build src\CastoPet\CastoPet.csproj -c Debug --no-restore
```

Expected: build succeeds with 0 errors.

- [ ] **Step 3: Run the full Release test suite**

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj -c Release
```

Expected: all tests pass with exit code 0.

- [ ] **Step 4: Build the Release application**

```powershell
dotnet build src\CastoPet\CastoPet.csproj -c Release --no-restore
```

Expected: build succeeds with 0 errors and refreshes `src/CastoPet/bin/Release/net10.0-windows/CastoPet.exe`.

- [ ] **Step 5: Inspect the final diff and working tree**

```powershell
git diff --check
git status --short
```

Expected: no whitespace errors; only known local untracked resources remain outside the committed changes.
