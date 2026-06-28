# CastoPet Input Reactive Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Bongo Cat style input reactive mode that swaps CastoPet into a keyboard visual and highlights keys/mouse feedback from global input.

**Architecture:** Keep pure input state and keyboard geometry in `CastoPet.Core` so they can be tested without WPF. `PetWindow` owns WPF image swapping and overlay rendering, while a Windows hook service owns global keyboard/mouse capture and is started only when the mode is active.

**Tech Stack:** C#/.NET WPF, Win32 low-level keyboard/mouse hooks, existing console-style test harness in `tests/CastoPet.Tests`.

---

## File Map

- `src/CastoPet/Core/AppSettings.cs`: add persisted `InputReactiveMode`.
- `src/CastoPet/Core/PetWindowSettingsSnapshot.cs`: copy `InputReactiveMode` for window runtime state.
- `src/CastoPet/Core/MenuCommandService.cs`: toggle mode and save settings.
- `src/CastoPet/Core/TrayService.cs`: tray menu checked item.
- `src/CastoPet/PetWindow.xaml`: add input reactive highlight overlay canvas.
- `src/CastoPet/PetWindow.xaml.cs`: mode priority, asset swap, overlay drawing, hook start/stop.
- `src/CastoPet/Core/AssetService.cs`: load optional input reactive base asset.
- `src/CastoPet/Core/InputKeyboardLayout.cs`: map keys to pet-local rectangles.
- `src/CastoPet/Core/InputReactiveState.cs`: track active highlights and expirations.
- `src/CastoPet/Core/InputReactiveEvent.cs`: normalized keyboard/mouse event type.
- `src/CastoPet/Core/WindowsInputHookService.cs`: low-level keyboard/mouse hook implementation.
- `src/CastoPet/Assets/States/InputReactive/Castorice.InputReactive.Base.png`: generated base image.
- `src/CastoPet/CastoPet.csproj`: package generated asset.
- `tests/CastoPet.Tests/Program.cs`: behavior tests.

### Task 1: Settings and Menus

**Files:**
- Modify: `src/CastoPet/Core/AppSettings.cs`
- Modify: `src/CastoPet/Core/PetWindowSettingsSnapshot.cs`
- Modify: `src/CastoPet/Core/MenuCommandService.cs`
- Modify: `src/CastoPet/Core/TrayService.cs`
- Modify: `src/CastoPet/PetWindow.xaml.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write the failing tests**

Add test registrations near the existing settings/menu tests:

```csharp
("Default input reactive mode is disabled", DefaultInputReactiveModeIsDisabled),
("Settings round trip includes input reactive mode", SettingsRoundTripIncludesInputReactiveMode),
("Tray menu exposes input reactive mode text", TrayMenuExposesInputReactiveModeText),
("Pet window settings snapshot copies input reactive mode", PetWindowSettingsSnapshotCopiesInputReactiveMode),
```

Add test methods:

```csharp
static void DefaultInputReactiveModeIsDisabled()
{
    Assert.False(AppSettings.Default.InputReactiveMode, "Input reactive mode should default to false.");
}

static void SettingsRoundTripIncludesInputReactiveMode()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);

    var settings = new AppSettings
    {
        InputReactiveMode = true,
    };

    service.Save(settings);
    var loaded = service.Load();

    Assert.True(loaded.InputReactiveMode, "InputReactiveMode should round trip.");
}

static void TrayMenuExposesInputReactiveModeText()
{
    Assert.Equal("输入响应模式", TrayService.InputReactiveModeText, "Input reactive menu text should be localized.");
}

static void PetWindowSettingsSnapshotCopiesInputReactiveMode()
{
    var settings = new AppSettings
    {
        InputReactiveMode = true,
    };

    var snapshot = PetWindowSettingsSnapshot.FromSettings(settings);

    Assert.True(snapshot.InputReactiveMode, "Input reactive mode should be copied for window runtime state.");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: compile failures for missing `InputReactiveMode`, `TrayService.InputReactiveModeText`, or `PetWindowSettingsSnapshot.InputReactiveMode`.

- [ ] **Step 3: Implement settings and menu wiring**

Add to `AppSettings`:

```csharp
public bool InputReactiveMode { get; set; }
```

Copy it in `Clone()`:

```csharp
InputReactiveMode = InputReactiveMode,
```

Change `PetWindowSettingsSnapshot` to:

```csharp
public sealed record PetWindowSettingsSnapshot(
    bool Topmost,
    bool ClickThrough,
    bool ShowInTaskbar,
    bool ActiveMovement,
    bool PushCursor,
    bool InputReactiveMode)
{
    public static PetWindowSettingsSnapshot FromSettings(AppSettings settings)
    {
        return new PetWindowSettingsSnapshot(
            settings.Topmost,
            settings.ClickThrough,
            settings.ShowInTaskbar,
            settings.ActiveMovement,
            settings.PushCursor,
            settings.InputReactiveMode);
    }
}
```

Add `MenuCommandService.ToggleInputReactiveMode()`:

```csharp
public void ToggleInputReactiveMode()
{
    Settings.InputReactiveMode = !Settings.InputReactiveMode;
    ApplyAndSave("Input reactive mode setting changed.");
}
```

Add to `TrayService`:

```csharp
public const string InputReactiveModeText = "输入响应模式";
```

Create and refresh `_inputReactiveModeItem`, and insert it after `PushCursorText`.

Add the same checked item to `PetWindow.AttachContextMenu` and `RefreshContextMenuChecks`.

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/CastoPet/Core/AppSettings.cs src/CastoPet/Core/PetWindowSettingsSnapshot.cs src/CastoPet/Core/MenuCommandService.cs src/CastoPet/Core/TrayService.cs src/CastoPet/PetWindow.xaml.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: add input reactive mode setting"
```

### Task 2: Keyboard Layout and Highlight State

**Files:**
- Create: `src/CastoPet/Core/InputKeyboardLayout.cs`
- Create: `src/CastoPet/Core/InputReactiveState.cs`
- Create: `src/CastoPet/Core/InputReactiveEvent.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write the failing tests**

Add tests for representative key geometry and highlight expiration:

```csharp
static void InputKeyboardLayoutMapsCommonKeys()
{
    Assert.True(InputKeyboardLayout.TryGetKeyBounds("A", out var a), "A should have a key rectangle.");
    Assert.True(InputKeyboardLayout.TryGetKeyBounds("Space", out var space), "Space should have a key rectangle.");
    Assert.True(InputKeyboardLayout.TryGetKeyBounds("Enter", out var enter), "Enter should have a key rectangle.");
    Assert.False(InputKeyboardLayout.TryGetKeyBounds("Unknown", out _), "Unknown keys should not map to a rectangle.");
    Assert.True(a.X >= 0 && a.Y >= 0 && a.Right <= InputKeyboardLayout.VisualWidth && a.Bottom <= InputKeyboardLayout.VisualHeight, "A should fit inside the visual bounds.");
    Assert.True(space.Width > a.Width, "Space should be wider than a letter key.");
    Assert.True(enter.Height >= a.Height, "Enter should be at least as tall as a letter key.");
}

static void InputReactiveStateExpiresHighlights()
{
    var state = new InputReactiveState();

    state.AddKey("A", TimeSpan.Zero);
    Assert.True(state.GetActiveHighlights(TimeSpan.FromMilliseconds(100)).Contains("A"), "A should remain active before expiration.");
    Assert.False(state.GetActiveHighlights(TimeSpan.FromMilliseconds(300)).Contains("A"), "A should expire after the highlight duration.");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: compile failures for missing `InputKeyboardLayout` and `InputReactiveState`.

- [ ] **Step 3: Implement layout and state**

Create `InputReactiveEvent.cs`:

```csharp
namespace CastoPet.Core;

public enum InputReactiveEventKind
{
    KeyDown,
    MouseDown,
}

public readonly record struct InputReactiveEvent(InputReactiveEventKind Kind, string Id);
```

Create `InputKeyboardLayout.cs` with `VisualWidth = 320`, `VisualHeight = 420`, and a dictionary of `System.Drawing.RectangleF` bounds for letters, number row, `Space`, `Enter`, `Backspace`, `Shift`, `Ctrl`, `Alt`, and arrow keys. Use stable pet-local coordinates in the lower keyboard area, for example letter keys around `Y=290..350` and `Space` around `Y=365`.

Create `InputReactiveState.cs`:

```csharp
namespace CastoPet.Core;

public sealed class InputReactiveState
{
    public static readonly TimeSpan HighlightDuration = TimeSpan.FromMilliseconds(160);
    private readonly Dictionary<string, TimeSpan> _expiresAt = new(StringComparer.OrdinalIgnoreCase);

    public void AddKey(string key, TimeSpan now)
    {
        _expiresAt[key] = now + HighlightDuration;
    }

    public IReadOnlyList<string> GetActiveHighlights(TimeSpan now)
    {
        foreach (var key in _expiresAt.Where(pair => pair.Value <= now).Select(pair => pair.Key).ToArray())
        {
            _expiresAt.Remove(key);
        }

        return _expiresAt.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public void Clear()
    {
        _expiresAt.Clear();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/CastoPet/Core/InputKeyboardLayout.cs src/CastoPet/Core/InputReactiveState.cs src/CastoPet/Core/InputReactiveEvent.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: add input reactive keyboard state"
```

### Task 3: Base Asset and Asset Loading

**Files:**
- Create: `src/CastoPet/Assets/States/InputReactive/Castorice.InputReactive.Base.png`
- Modify: `src/CastoPet/Core/AssetService.cs`
- Modify: `src/CastoPet/CastoPet.csproj`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write the failing tests**

Add:

```csharp
static void InputReactiveAssetPathUsesAppResource()
{
    Assert.Equal("Assets/States/InputReactive/Castorice.InputReactive.Base.png", AssetService.InputReactiveBasePath, "Input reactive base should use an app resource path.");
}
```

Add a static packaging test that checks `src/CastoPet/CastoPet.csproj` contains:

```xml
<Resource Include="Assets\States\InputReactive\Castorice.InputReactive.Base.png" />
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: missing `AssetService.InputReactiveBasePath` or missing resource declaration.

- [ ] **Step 3: Generate and add the base asset**

Create a 320x420 transparent PNG at:

```text
src/CastoPet/Assets/States/InputReactive/Castorice.InputReactive.Base.png
```

Use the current Castorice character palette. The composition is a half-body/chibi desk-pet pose, face angled roughly toward the lower-left keyboard area, with a simplified keyboard in the lower foreground. Keep the keyboard key grid aligned to `InputKeyboardLayout` bounds.

Add to `AssetService`:

```csharp
public const string InputReactiveBasePath = "Assets/States/InputReactive/Castorice.InputReactive.Base.png";

public ImageSource? TryLoadInputReactiveBase()
{
    try
    {
        return LoadCharacter(InputReactiveBasePath, "Input reactive base");
    }
    catch
    {
        return null;
    }
}
```

Add the resource to `CastoPet.csproj`.

- [ ] **Step 4: Run tests and build**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
dotnet build src/CastoPet/CastoPet.csproj -c Release -o tmp\verify-build
```

Expected: all tests pass and build has 0 warnings/errors.

- [ ] **Step 5: Commit**

```powershell
git add src/CastoPet/Assets/States/InputReactive/Castorice.InputReactive.Base.png src/CastoPet/Core/AssetService.cs src/CastoPet/CastoPet.csproj tests/CastoPet.Tests/Program.cs
git commit -m "feat: add input reactive base asset"
```

### Task 4: Windows Input Hook Service

**Files:**
- Create: `src/CastoPet/Core/WindowsInputHookService.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write the failing tests**

Add pure normalization tests:

```csharp
static void WindowsInputHookNormalizesCommonKeys()
{
    Assert.Equal("A", WindowsInputHookService.NormalizeVirtualKey(0x41), "VK_A should normalize to A.");
    Assert.Equal("Space", WindowsInputHookService.NormalizeVirtualKey(0x20), "VK_SPACE should normalize to Space.");
    Assert.Equal("Enter", WindowsInputHookService.NormalizeVirtualKey(0x0D), "VK_RETURN should normalize to Enter.");
    Assert.Equal("Left", WindowsInputHookService.NormalizeVirtualKey(0x25), "VK_LEFT should normalize to Left.");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: missing `WindowsInputHookService`.

- [ ] **Step 3: Implement hook service**

Implement a disposable service that uses `SetWindowsHookEx` with `WH_KEYBOARD_LL` and `WH_MOUSE_LL`. It exposes:

```csharp
public sealed class WindowsInputHookService : IDisposable
{
    public event Action<InputReactiveEvent>? InputReceived;
    public bool IsRunning { get; }
    public void Start();
    public void Stop();
    public static string? NormalizeVirtualKey(int virtualKey);
}
```

Only emit key IDs, not typed text. For mouse, emit `MouseLeft`, `MouseRight`, and `MouseMiddle`.

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/CastoPet/Core/WindowsInputHookService.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: add input hook service"
```

### Task 5: PetWindow Visual Integration

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml`
- Modify: `src/CastoPet/PetWindow.xaml.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write the failing tests**

Add focused pure tests if helpers are introduced, such as:

```csharp
static void InputReactiveModePausesActiveMovement()
{
    var snapshot = new PetWindowSettingsSnapshot(
        Topmost: true,
        ClickThrough: false,
        ShowInTaskbar: false,
        ActiveMovement: true,
        PushCursor: true,
        InputReactiveMode: true);

    Assert.True(snapshot.InputReactiveMode, "Input mode should be available to suppress active movement in PetWindow.");
}
```

Use this as a guard while the WPF integration is manually verified.

- [ ] **Step 2: Run test to verify it fails if helper shape is missing**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: failure only if the helper/constructor shape is not yet aligned.

- [ ] **Step 3: Implement WPF overlay and runtime priority**

Add to `PetWindow.xaml` above the expression wheel overlay:

```xml
<Canvas x:Name="InputReactiveOverlay"
        Width="320"
        Height="420"
        IsHitTestVisible="False"
        Visibility="Collapsed" />
```

In `PetWindow.xaml.cs`:

- Add fields for `_inputReactiveModeEnabled`, `_inputReactiveBase`, `_inputReactiveState`, `_inputHookService`, and `_inputReactiveRenderTimer`.
- Load `_inputReactiveBase = assets.TryLoadInputReactiveBase();`.
- In `ApplySettings`, set `_inputReactiveModeEnabled = snapshot.InputReactiveMode;`.
- Make `CanRunActiveMovement`, `CanIdleAnimate`, and `CanBlink` return false when `_inputReactiveModeEnabled` is active.
- When mode is enabled and base image exists, set `CharacterImage.Source = _inputReactiveBase`, show overlay, start input hook and render timer.
- When disabled, stop hook/timer, clear state, hide overlay, and restore current idle frame.
- Handle `InputReceived` by adding highlights to `InputReactiveState`.
- Render active keys by clearing `InputReactiveOverlay.Children` and adding rounded rectangles from `InputKeyboardLayout`.

- [ ] **Step 4: Run tests and Release build**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
dotnet build src/CastoPet/CastoPet.csproj -c Release -o tmp\verify-build
```

Expected: all tests pass and build has 0 warnings/errors.

- [ ] **Step 5: Manual smoke test**

Launch the app, enable `输入响应模式`, press `A`, `Space`, `Enter`, arrow keys, and click left/right mouse. Confirm the overlay pulses and drag/wheel interactions still work.

- [ ] **Step 6: Commit**

```powershell
git add src/CastoPet/PetWindow.xaml src/CastoPet/PetWindow.xaml.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: render input reactive mode"
```

### Task 6: Final Verification and Cleanup

**Files:**
- Inspect: repository status and generated assets.

- [ ] **Step 1: Run full verification**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
dotnet build src/CastoPet/CastoPet.csproj -c Release -o tmp\verify-build
```

Expected: all tests pass and build has 0 warnings/errors.

- [ ] **Step 2: Remove verification build output**

Safely remove only `D:\Projects\CastoPet\tmp\verify-build` after checking the resolved path is inside the workspace.

- [ ] **Step 3: Inspect status**

Run:

```powershell
git status --short
git log --oneline -8
```

Expected: only existing unrelated untracked files remain: `.codex/`, `Castorice.png`, and `sample/`.
