# Movement Model Consolidation Plan

Status: PROPOSED. Requires explicit user approval before implementation.

This document records the movement-model discussion. It is not the current manifest
contract and does not authorize changes to action kinds, resource layout, or skin data.

## Current Approved Change

- Disable movement turn transitions when starting, switching sides, and stopping.
- Continue playing the existing left/right walking animations with unchanged movement
  speeds, distance-driven frame advancement, target selection, and boundary handling.
- Preserve all turn code, action definitions, and image assets. Do not automatically
  enable transitions again while implementing this future plan.
- Leave blink, petting, and expression transitions unchanged.

## Problem

`PetActionDefinition` mixes animation resources, playback timing, movement speed, and
trigger scheduling. Its nullable fields allow settings that an action never consumes.

The current `Move` action supplies shared speed and distance-per-frame settings even
when the renderer plays `MoveLeft` or `MoveRight`. Its images are only a fallback when
directional images are unavailable. Directional distance-per-frame metadata is not
independently consumed by the current movement controller.

## Proposed Model

Represent movement as one behavior with shared settings and directional animation
variants, rather than separate left/right behaviors or controller classes.

```text
Movement definition
|-- Shared movement settings
|   |-- Base / minimum / maximum speed
|   `-- Distance per animation frame
`-- Animation variants
    |-- Left walking clip
    |-- Right walking clip
    `-- Optional generic fallback clip
```

- Keep `Move` as the conceptual action kind. Left/right are variant selectors, not
  separate behavior kinds in the future internal model.
- Keep frame paths, default frame interval, and per-frame duration overrides in an
  animation clip definition. Do not make speed or trigger scheduling universal clip
  properties.
- Walking remains distance-driven. Do not silently replace it with a timer or add
  per-direction speed overrides when both directions intentionally share settings.
- Distinguish time-driven playback from distance-driven playback during validation;
  do not accept timing metadata as effective when the chosen playback policy ignores it.
- Place movement speed in movement settings and random trigger delays in scheduling
  settings. The exact types and manifest version must be reviewed before implementation.
- Do not remove turn or expression-transition kinds as a side effect of this change.

## Runtime Responsibilities

- `PetMovementPlanner`: target selection and common screen-boundary constraints.
- `PetMovementController`: displacement, shared speeds, and distance-driven frame index.
- `PetDirectionalMovementAnimator`: facing and optional turn state, kept separate from
  movement calculations. Turns remain disabled until separately approved.
- Presentation: choose the clip for the current direction and display its current frame.

Direction comes from movement intent or displacement; near-zero horizontal movement
retains the existing facing policy. Left/right collision handling uses common geometry,
not duplicated movement implementations. Random wandering and mouse approach continue
to differ in their target selection, not their walking implementation.

## Compatibility and Migration

1. Add characterization tests for current speed, frame progression, fallback selection,
   direction changes, arrival, and boundary behavior before migrating the model.
2. Introduce shared movement settings and directional clip selection internally, without
   changing the existing JSON contract or runtime behavior.
3. Adapt legacy `move`, `move-left`, and `move-right` entries in the loader. Shared values
   come from legacy `move`, matching current behavior. Never silently choose among
   conflicting directional settings; report conflicts before deciding how to migrate.
4. Remove the movement controller's dependency on the generic animation action. A new
   skin with complete directional clips must not need unused generic movement images.
5. Review and version the new manifest format and writer separately. Continue supporting
   old manifests; preserve their effective settings and resources during conversion.
6. Remove old internal directional enum entries and fixed loading wrappers only after
   their consumers have migrated. Legacy external identifiers remain supported by the
   compatibility adapter.

Do not delete or move source images, rewrite existing skin manifests, or replace the
current runtime path merely because this plan exists.

## Acceptance Checks

- Equivalent positions, speeds, and walking frame progression for left/right movement.
- No waiting for turn frames while transitions are disabled; stopping restores idle.
- Generic-frame fallback remains available, with tested behavior for missing variants.
- Old manifests load without mandatory edits; new-format round trips preserve settings.
- Conflicting metadata is visible rather than silently discarded.
- Debug/Release and Preview/Stable full tests and builds pass.
- Manual verification covers start, reversal, stop, screen edges, and mouse approach.
