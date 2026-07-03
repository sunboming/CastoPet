# CastoPet Skin Action Definitions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce skin/action definition models and manifest loading so CastoPet can move away from hard-coded animation sequence constants while preserving the built-in Castorice skin.

**Architecture:** Add definition models in `CastoPet.Core`, keep old frame sequence classes as compatibility shims during this phase, and make `BuiltInPetSkins.Castorice` the authoritative definition for built-in assets. Add a JSON manifest loader for external skins and migrate `AssetService` plus `PetWindow` resource loading to use definitions.

**Tech Stack:** C#/.NET WPF, `System.Text.Json`, existing console-style test harness in `tests/CastoPet.Tests`.

---

## File Map

- `src/CastoPet/Core/PetActionKind.cs`: enum of supported action kinds.
- `src/CastoPet/Core/PetActionDefinition.cs`: immutable action metadata.
- `src/CastoPet/Core/PetSkinDefinition.cs`: immutable skin metadata and action lookup helpers.
- `src/CastoPet/Core/BuiltInPetSkins.cs`: built-in Castorice skin definition.
- `src/CastoPet/Core/PetSkinManifestLoader.cs`: external manifest JSON parser and validator.
- `src/CastoPet/Core/AssetService.cs`: add definition-based loading methods.
- `src/CastoPet/PetWindow.xaml.cs`: use built-in skin and action definitions for resource loading and timing.
- `tests/CastoPet.Tests/Program.cs`: add definition-centered and manifest tests.

### Task 1: Model Types and Built-In Castorice Skin

**Files:**
- Create: `src/CastoPet/Core/PetActionKind.cs`
- Create: `src/CastoPet/Core/PetActionDefinition.cs`
- Create: `src/CastoPet/Core/PetSkinDefinition.cs`
- Create: `src/CastoPet/Core/BuiltInPetSkins.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write the failing tests**

Add test registrations:

```csharp
("Built-in Castorice skin defines required actions", BuiltInCastoriceSkinDefinesRequiredActions),
("Built-in Castorice idle action preserves current frames", BuiltInCastoriceIdleActionPreservesCurrentFrames),
("Built-in Castorice move action preserves movement values", BuiltInCastoriceMoveActionPreservesMovementValues),
("Built-in Castorice blink action preserves schedule", BuiltInCastoriceBlinkActionPreservesSchedule),
```

Add test methods:

```csharp
static void BuiltInCastoriceSkinDefinesRequiredActions()
{
    var skin = BuiltInPetSkins.Castorice;

    Assert.Equal("castorice", skin.Id, "Built-in skin id should be stable.");
    Assert.Equal("Castorice", skin.DisplayName, "Built-in skin display name should be stable.");
    Assert.Equal("Assets/Castorice.png", skin.DefaultCharacterPath, "Default character path should stay compatible.");
    Assert.Equal("Assets/States/Castorice.Dragging.png", skin.DraggingCharacterPath, "Dragging path should stay compatible.");
    Assert.Equal("Assets/States/InputReactive/Castorice.InputReactive.Base.png", skin.InputReactiveBasePath, "Input reactive path should stay compatible.");
    Assert.True(skin.TryGetAction(PetActionKind.Idle, out _), "Castorice should define idle.");
    Assert.True(skin.TryGetAction(PetActionKind.Move, out _), "Castorice should define move.");
    Assert.True(skin.TryGetAction(PetActionKind.Blink, out _), "Castorice should define blink.");
    Assert.True(skin.TryGetAction(PetActionKind.ExpressionTransitionIn, out _), "Castorice should define transition in.");
    Assert.True(skin.TryGetAction(PetActionKind.ExpressionTransitionOut, out _), "Castorice should define transition out.");
}

static void BuiltInCastoriceIdleActionPreservesCurrentFrames()
{
    var idle = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Idle);

    Assert.Equal(8, idle.FramePaths.Count, "Idle should keep eight frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(200), idle.FrameInterval, "Idle frame timing should stay compatible.");
    Assert.Equal("Assets/States/Idle/Castorice.Idle.00.png", idle.FramePaths[0], "First idle frame path should stay compatible.");
    Assert.Equal("Assets/States/Idle/Castorice.Idle.07.png", idle.FramePaths[^1], "Last idle frame path should stay compatible.");
}

static void BuiltInCastoriceMoveActionPreservesMovementValues()
{
    var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);

    Assert.Equal(8, move.FramePaths.Count, "Move should keep eight frames.");
    Assert.Equal(10d, move.DistancePerFrame, "Move distance per frame should stay compatible.");
    Assert.Equal(90d, move.BaseSpeedPixelsPerSecond, "Move base speed should stay compatible.");
    Assert.Equal(80d, move.MinSpeedPixelsPerSecond, "Move min speed should stay compatible.");
    Assert.Equal(105d, move.MaxSpeedPixelsPerSecond, "Move max speed should stay compatible.");
}

static void BuiltInCastoriceBlinkActionPreservesSchedule()
{
    var blink = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Blink);

    Assert.Equal(3, blink.FramePaths.Count, "Blink should keep three frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(90), blink.FrameInterval, "Blink frame interval should stay compatible.");
    Assert.Equal(TimeSpan.FromSeconds(3), blink.MinScheduleDelay, "Blink min schedule should stay compatible.");
    Assert.Equal(TimeSpan.FromSeconds(7), blink.MaxScheduleDelay, "Blink max schedule should stay compatible.");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: compile failures for missing `BuiltInPetSkins`, `PetActionKind`, and definition types.

- [ ] **Step 3: Implement minimal model and built-in skin**

Create `PetActionKind.cs`:

```csharp
namespace CastoPet.Core;

public enum PetActionKind
{
    Idle,
    Move,
    Blink,
    ExpressionTransitionIn,
    ExpressionTransitionOut,
}
```

Create `PetActionDefinition.cs`:

```csharp
namespace CastoPet.Core;

public sealed record PetActionDefinition(
    string Id,
    PetActionKind Kind,
    IReadOnlyList<string> FramePaths,
    TimeSpan? FrameInterval = null,
    double? DistancePerFrame = null,
    TimeSpan? MinScheduleDelay = null,
    TimeSpan? MaxScheduleDelay = null,
    double? BaseSpeedPixelsPerSecond = null,
    double? MinSpeedPixelsPerSecond = null,
    double? MaxSpeedPixelsPerSecond = null);
```

Create `PetSkinDefinition.cs` with constructor fields and:

```csharp
public bool TryGetAction(PetActionKind kind, out PetActionDefinition action)
{
    action = Actions.FirstOrDefault(item => item.Kind == kind)!;
    return action is not null;
}

public PetActionDefinition GetRequiredAction(PetActionKind kind)
{
    return TryGetAction(kind, out var action)
        ? action
        : throw new InvalidOperationException($"Skin {Id} does not define action {kind}.");
}
```

Create `BuiltInPetSkins.cs` with `Castorice` populated from current constants and paths.

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/CastoPet/Core/PetActionKind.cs src/CastoPet/Core/PetActionDefinition.cs src/CastoPet/Core/PetSkinDefinition.cs src/CastoPet/Core/BuiltInPetSkins.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: add pet skin action definitions"
```

### Task 2: External Manifest Loader

**Files:**
- Create: `src/CastoPet/Core/PetSkinManifestLoader.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write the failing tests**

Add test registrations:

```csharp
("Pet skin manifest loader parses valid manifest", PetSkinManifestLoaderParsesValidManifest),
("Pet skin manifest loader rejects unsupported schema", PetSkinManifestLoaderRejectsUnsupportedSchema),
("Pet skin manifest loader resolves relative paths", PetSkinManifestLoaderResolvesRelativePaths),
```

Add test methods:

```csharp
static void PetSkinManifestLoaderParsesValidManifest()
{
    var json = """
    {
      "schemaVersion": 1,
      "id": "test-skin",
      "displayName": "Test Skin",
      "resourceRoot": "Assets",
      "defaultCharacter": "Castorice.png",
      "draggingCharacter": "States/Castorice.Dragging.png",
      "inputReactiveBase": "States/InputReactive/Castorice.InputReactive.Base.png",
      "actions": {
        "idle": { "kind": "idle", "frameIntervalMs": 200, "frames": ["States/Idle/00.png"] },
        "move": { "kind": "move", "distancePerFrame": 10, "baseSpeed": 90, "minSpeed": 80, "maxSpeed": 105, "frames": ["States/Move/00.png"] },
        "blink": { "kind": "blink", "frameIntervalMs": 90, "minScheduleDelayMs": 3000, "maxScheduleDelayMs": 7000, "frames": ["States/Blink/00.png"] }
      }
    }
    """;

    var skin = PetSkinManifestLoader.LoadFromJson(json);

    Assert.Equal("test-skin", skin.Id, "Manifest skin id should parse.");
    Assert.Equal("Assets/Castorice.png", skin.DefaultCharacterPath, "Default character should resolve against resource root.");
    Assert.Equal("Assets/States/Idle/00.png", skin.GetRequiredAction(PetActionKind.Idle).FramePaths[0], "Idle frame should resolve against resource root.");
}

static void PetSkinManifestLoaderRejectsUnsupportedSchema()
{
    var json = """{ "schemaVersion": 2, "id": "x", "displayName": "X", "defaultCharacter": "x.png", "actions": {} }""";

    Assert.Throws<InvalidOperationException>(() => PetSkinManifestLoader.LoadFromJson(json), "Unsupported schema should fail.");
}

static void PetSkinManifestLoaderResolvesRelativePaths()
{
    var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CastoPet.Tests", Guid.NewGuid().ToString("N"), "skin.json");
    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
    File.WriteAllText(path, """
    {
      "schemaVersion": 1,
      "id": "disk-skin",
      "displayName": "Disk Skin",
      "resourceRoot": "res",
      "defaultCharacter": "base.png",
      "actions": {
        "idle": { "kind": "idle", "frameIntervalMs": 200, "frames": ["idle/00.png"] },
        "move": { "kind": "move", "frames": ["move/00.png"] },
        "blink": { "kind": "blink", "frames": ["blink/00.png"] }
      }
    }
    """);

    var skin = PetSkinManifestLoader.LoadFromFile(path);

    Assert.Contains(skin.DefaultCharacterPath, "res", "Disk manifest paths should include resource root.");
    Assert.Contains(skin.GetRequiredAction(PetActionKind.Idle).FramePaths[0], "idle", "Disk manifest action paths should resolve.");
}
```

If `Assert.Throws` does not exist yet, add it to the local `Assert` helper:

```csharp
public static void Throws<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: compile failures for missing `PetSkinManifestLoader`.

- [ ] **Step 3: Implement manifest loader**

Create `PetSkinManifestLoader.cs` using `System.Text.Json`. Implement:

```csharp
public static PetSkinDefinition LoadFromJson(string json);
public static PetSkinDefinition LoadFromFile(string manifestPath);
```

Map `kind` strings:

- `idle` -> `PetActionKind.Idle`
- `move` -> `PetActionKind.Move`
- `blink` -> `PetActionKind.Blink`
- `expression-transition-in` -> `PetActionKind.ExpressionTransitionIn`
- `expression-transition-out` -> `PetActionKind.ExpressionTransitionOut`

Resolve paths with a helper:

```csharp
private static string ResolvePath(string? baseDirectory, string resourceRoot, string relativePath)
```

For JSON-only manifests, return `resourceRoot/relativePath` using forward slashes. For file manifests, return an absolute filesystem path using `Path.GetFullPath(Path.Combine(baseDirectory, resourceRoot, relativePath))`.

Validate schema version equals `1`, required id/display name/default character exist, and required actions `idle`, `move`, and `blink` exist.

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/CastoPet/Core/PetSkinManifestLoader.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: load pet skin manifests"
```

### Task 3: AssetService Definition Loading

**Files:**
- Modify: `src/CastoPet/Core/AssetService.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write the failing tests**

Add:

```csharp
("Asset service loads action frames from definitions", AssetServiceLoadsActionFramesFromDefinitions),
```

Add method:

```csharp
static void AssetServiceLoadsActionFramesFromDefinitions()
{
    using var temp = TempDirectory.Create();
    var logger = new LoggingService(new AppPaths(temp.Path));
    var service = new AssetService(logger);
    var idle = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Idle);

    var frames = service.LoadActionFrames(idle);

    Assert.Equal(idle.FramePaths.Count, frames.Count, "Action frame loader should load every frame in the definition.");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: compile failure for missing `LoadActionFrames`.

- [ ] **Step 3: Implement definition loading methods**

Make `LoadCharacter` internal/public enough for definition loading as needed, then add:

```csharp
public IReadOnlyList<ImageSource> LoadActionFrames(PetActionDefinition action)
{
    return action.FramePaths
        .Select(path => LoadCharacter(path, $"{action.Id} action frames"))
        .ToArray();
}

public ImageSource? TryLoadImage(string resourcePath, string resourceGroup)
{
    try
    {
        return LoadCharacter(resourcePath, resourceGroup);
    }
    catch
    {
        return null;
    }
}
```

Keep existing `LoadIdleFrames`, `LoadBlinkFrames`, and similar methods for compatibility.

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/CastoPet/Core/AssetService.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: load assets from action definitions"
```

### Task 4: PetWindow Uses Built-In Skin Definitions for Loading

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write the failing tests**

Add tests that ensure the old sequence values and built-in definitions stay aligned:

```csharp
("Built-in definitions stay aligned with compatibility sequences", BuiltInDefinitionsStayAlignedWithCompatibilitySequences),
```

Add:

```csharp
static void BuiltInDefinitionsStayAlignedWithCompatibilitySequences()
{
    var skin = BuiltInPetSkins.Castorice;

    Assert.Equal(IdleFrameSequence.FramePaths.Count, skin.GetRequiredAction(PetActionKind.Idle).FramePaths.Count, "Idle compatibility sequence should match definition.");
    Assert.Equal(MoveFrameSequence.FramePaths.Count, skin.GetRequiredAction(PetActionKind.Move).FramePaths.Count, "Move compatibility sequence should match definition.");
    Assert.Equal(BlinkFrameSequence.FramePaths.Count, skin.GetRequiredAction(PetActionKind.Blink).FramePaths.Count, "Blink compatibility sequence should match definition.");
    Assert.Equal(ExpressionTransitionSequence.InFramePaths.Count, skin.GetRequiredAction(PetActionKind.ExpressionTransitionIn).FramePaths.Count, "Transition-in compatibility sequence should match definition.");
    Assert.Equal(ExpressionTransitionSequence.OutFramePaths.Count, skin.GetRequiredAction(PetActionKind.ExpressionTransitionOut).FramePaths.Count, "Transition-out compatibility sequence should match definition.");
}
```

This test should pass before implementation if Task 1 is correct; it guards the migration.

- [ ] **Step 2: Run tests**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 3: Migrate loading in `PetWindow`**

In `PetWindow` constructor, create:

```csharp
var skin = BuiltInPetSkins.Castorice;
var idleAction = skin.GetRequiredAction(PetActionKind.Idle);
var blinkAction = skin.GetRequiredAction(PetActionKind.Blink);
var moveAction = skin.GetRequiredAction(PetActionKind.Move);
var transitionInAction = skin.GetRequiredAction(PetActionKind.ExpressionTransitionIn);
var transitionOutAction = skin.GetRequiredAction(PetActionKind.ExpressionTransitionOut);
```

Use:

```csharp
_defaultCharacter = assets.LoadCharacter(skin.DefaultCharacterPath, "Default character");
_draggingCharacter = assets.LoadCharacter(skin.DraggingCharacterPath, "Dragging character");
_idleFrames = assets.LoadActionFrames(idleAction);
_blinkFrames = assets.LoadActionFrames(blinkAction);
_moveFrames = assets.LoadActionFrames(moveAction);
_inputReactiveBase = assets.TryLoadImage(skin.InputReactiveBasePath, "Input reactive base");
_expressionTransitionInFrames = assets.LoadActionFrames(transitionInAction);
_expressionTransitionOutFrames = assets.LoadActionFrames(transitionOutAction);
```

Keep timer intervals from compatibility constants for this task if changing them would widen runtime behavior. A separate timing-migration task can move runtime timer fields to action definitions after resource loading is definition-based.

- [ ] **Step 4: Run tests and Release build**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
dotnet build src/CastoPet/CastoPet.csproj -c Release -o tmp\verify-build
```

Expected: tests pass and Release build has 0 warnings/errors.

- [ ] **Step 5: Commit**

```powershell
git add src/CastoPet/PetWindow.xaml.cs tests/CastoPet.Tests/Program.cs
git commit -m "refactor: load pet window assets from skin definition"
```

### Task 5: Final Verification and Cleanup

**Files:**
- Inspect: repository status.

- [ ] **Step 1: Run full verification**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
dotnet build src/CastoPet/CastoPet.csproj -c Release -o tmp\verify-build
```

Expected: tests pass and Release build has 0 warnings/errors.

- [ ] **Step 2: Remove verification build output**

Safely remove only `D:\Projects\CastoPet\tmp\verify-build` after confirming the resolved path is inside the workspace.

- [ ] **Step 3: Inspect final state**

Run:

```powershell
git status --short
git log --oneline -8
```

Expected: only existing unrelated untracked files remain: `.codex/`, `Castorice.png`, and `sample/`.
