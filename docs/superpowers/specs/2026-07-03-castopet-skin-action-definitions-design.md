# CastoPet Skin and Action Definitions Design

## Goal

Move CastoPet away from hard-coded frame sequence classes as the primary animation source. The app should load built-in and external pet assets through `PetSkinDefinition` and `PetActionDefinition`, while preserving the current built-in Castorice resource paths as the default skin.

## Current Problem

Animation metadata is currently spread across fixed classes such as `IdleFrameSequence`, `MoveFrameSequence`, `BlinkFrameSequence`, and `ExpressionTransitionSequence`. These classes hard-code frame counts, frame paths, frame intervals, and movement timing. This makes the current app work, but it does not scale cleanly to external skins or editor-exported manifests.

## Scope

First migration phase includes:

- Add `PetActionDefinition` and `PetSkinDefinition`.
- Add a built-in Castorice skin definition that preserves current resource paths.
- Add an external JSON manifest loader.
- Change resource loading to support loading action frames from a skin definition.
- Start moving tests from fixed sequence constants toward action definitions.
- Keep existing resource paths compatible so editor-exported PNG files can still overwrite current assets.

First migration phase excludes:

- Skin selection UI.
- Hot switching skins while the pet is running.
- Full editor integration.
- Removing every old `*FrameSequence` class in one pass.
- Validating image dimensions inside the manifest loader.

## Migration Strategy

Use a compatibility-layer migration.

The old sequence classes can remain temporarily as compatibility facades or supporting constants, but the default Castorice skin becomes the authoritative source for action frame paths and animation timing. `AssetService` gains definition-based loading methods first. `PetWindow` can then be migrated in smaller steps to use the current skin's action definitions instead of directly calling sequence classes.

This avoids a large rewrite and keeps existing behavior stable.

## Core Model

### `PetActionDefinition`

Represents one named animation/action.

Fields:

- `Id`: stable string id such as `idle`, `move`, `blink`, `expression-transition-in`.
- `Kind`: enum value such as `Idle`, `Move`, `Blink`, `ExpressionTransitionIn`, `ExpressionTransitionOut`.
- `FramePaths`: ordered resource/file paths.
- `FrameInterval`: optional frame timing for time-driven animations.
- `DistancePerFrame`: optional distance-driven frame stepping for movement.
- `MinScheduleDelay`: optional random scheduling lower bound for blink.
- `MaxScheduleDelay`: optional random scheduling upper bound for blink.
- `BaseSpeedPixelsPerSecond`: optional movement speed.
- `MinSpeedPixelsPerSecond`: optional movement speed lower bound.
- `MaxSpeedPixelsPerSecond`: optional movement speed upper bound.

### `PetSkinDefinition`

Represents a skin and all actions/assets needed by runtime.

Fields:

- `Id`
- `DisplayName`
- `ResourceRoot`
- `DefaultCharacterPath`
- `DraggingCharacterPath`
- `InputReactiveBasePath`
- `Actions`
- `Expressions`

The skin exposes helper lookup methods such as:

- `GetRequiredAction(PetActionKind kind)`
- `TryGetAction(PetActionKind kind, out PetActionDefinition action)`

## Built-In Castorice Skin

Add `BuiltInPetSkins.Castorice`.

It preserves current paths:

- `Assets/Castorice.png`
- `Assets/States/Castorice.Dragging.png`
- `Assets/States/InputReactive/Castorice.InputReactive.Base.png`
- `Assets/States/Idle/Castorice.Idle.00.png` through `.07.png`
- `Assets/States/Move/Castorice.Move.00.png` through `.07.png`
- `Assets/States/Blink/Castorice.Blink.00.png` through `.02.png`
- `Assets/Expressions/Transition/Castorice.ExpressionTransition.In.00.png` through `.03.png`
- `Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.00.png` through `.03.png`

The current editor can continue exporting PNGs over those paths. The app will still pick them up because the built-in definition points to the same locations.

## External Manifest

Add `PetSkinManifestLoader`.

Manifest format, version 1:

```json
{
  "schemaVersion": 1,
  "id": "castorice",
  "displayName": "Castorice",
  "resourceRoot": "Assets",
  "defaultCharacter": "Castorice.png",
  "draggingCharacter": "States/Castorice.Dragging.png",
  "inputReactiveBase": "States/InputReactive/Castorice.InputReactive.Base.png",
  "actions": {
    "idle": {
      "kind": "idle",
      "frameIntervalMs": 200,
      "frames": [
        "States/Idle/Castorice.Idle.00.png"
      ]
    },
    "move": {
      "kind": "move",
      "distancePerFrame": 10,
      "baseSpeed": 90,
      "minSpeed": 80,
      "maxSpeed": 105,
      "frames": []
    },
    "blink": {
      "kind": "blink",
      "frameIntervalMs": 90,
      "minScheduleDelayMs": 3000,
      "maxScheduleDelayMs": 7000,
      "frames": []
    }
  },
  "expressions": {
    "happy": "Expressions/Castorice.Expression.Happy.png"
  }
}
```

Rules:

- `schemaVersion` must be `1`.
- `id`, `displayName`, `defaultCharacter`, and required actions must exist.
- Manifest-relative paths resolve against `resourceRoot`.
- If the manifest file comes from disk, relative paths resolve against the manifest file directory plus `resourceRoot`.
- Built-in paths can remain app resource paths starting with `Assets/`.
- Loader returns a validated `PetSkinDefinition`.
- Loader failures should be logged by the caller and fall back to `BuiltInPetSkins.Castorice`.

## Runtime Loading

`AssetService` should gain definition-based methods:

- `LoadCharacter(string path, string resourceGroup)` remains the internal primitive.
- `LoadActionFrames(PetActionDefinition action)` loads all frame paths for an action.
- `TryLoadImage(string path, string resourceGroup)` supports optional assets such as input reactive base.

`PetWindow` should eventually receive or construct a current `PetSkinDefinition`, initially the built-in Castorice skin. It should use the skin to load:

- idle frames
- blink frames
- move frames
- expression transition frames
- default character
- dragging character
- input reactive base

## Test Strategy

New tests should prioritize definitions:

- Built-in Castorice skin has id/display name.
- Built-in Castorice skin contains required actions: idle, move, blink, expression transition in/out.
- Built-in action definitions preserve current frame counts, frame intervals, and resource paths.
- Movement action definition preserves distance and speed values.
- Blink action definition preserves schedule delays.
- Manifest loader parses a minimal valid manifest.
- Manifest loader rejects unsupported schema versions.
- Manifest loader resolves relative paths consistently.
- `AssetService` can load frames from a `PetActionDefinition`.

Existing fixed sequence tests can be kept initially, but new behavior should be covered through the definition model first.

## Implementation Order

1. Add model types and built-in Castorice skin.
2. Add definition-centered tests.
3. Add manifest loader and manifest tests.
4. Add `AssetService` definition-based loading methods.
5. Migrate `PetWindow` construction/loading to use `BuiltInPetSkins.Castorice`.
6. Keep compatibility sequence classes until direct runtime references are removed.
7. Later, remove or downgrade old sequence tests after definition tests cover the same behavior.
