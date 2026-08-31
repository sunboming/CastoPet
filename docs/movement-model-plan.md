# Unified Movement Model

> Historical design reference: active movement and cursor interaction are not part of the
> minimal 0.1 runtime. The implementation described below remains available on the recovery
> branches and must be reviewed against `项目问题.md` before any future `main` reintroduction.

Historical status: implemented in the archived full-feature branch after user approval.
Turn playback was removed from that implementation. See [Skin manifest](skin-manifest.md)
for the historical JSON schema reference; neither document is a supported 0.1 contract.

## Model

Movement is one behavior with shared settings and directional frame lists:

```text
PetActionDefinition (Kind = Move)
|-- Movement: PetMovementDefinition
|   |-- Settings: PetMovementSettings
|   |   |-- Base / minimum / maximum speed
|   |   `-- Distance per animation frame
|   |-- LeftFramePaths
|   `-- RightFramePaths
`-- FramePaths: optional generic fallback
```

- `PetActionKind.Move` is the only movement action kind. Direction is selected with
  `PetHorizontalDirection.Left/Right`, not a separate action or movement controller.
- Speed and distance-per-frame fields have moved out of the general action record.
- Walking remains distance-driven. Both directions use the same settings; their frame
  counts may differ (the built-in left/right clips retain five/seven frames).
- Idle, blink, petting, and expression transitions retain their time-driven playback.
  Further extraction of their clip and scheduling properties is outside this change.

## Runtime Responsibilities

- `PetMovementPlanner`: common target selection and screen-boundary constraints.
- `PetMovementController`: displacement, shared speeds, and distance-driven frame index;
  depends on `PetMovementSettings`, not the general animation action.
- Presentation: select direction immediately, lazily load/cache its frames, and retain
  the existing direction during near-zero horizontal motion.
- Stopping resets direction and restores idle; reversal does not pause movement.
- The turn animator, turn enums, turn timers, and turn asset loaders are removed.
  No movement flag can reactivate them.
- Mouse approach and random wandering still differ only in target selection. Cursor
  pushing, rendering timestamps, movement bounds, and passive-animation gates are retained.

## Compatibility

- Schema 1/2 `move-left` and `move-right` (including camel-case aliases) are adapted
  into a single Move definition. Shared settings come from `move`, with existing defaults.
- Conflicting explicit directional speed/distance settings are rejected with a diagnostic.
  The built-in Castorice metadata has no such conflict.
- Old turn entries are ignored before resolving their PNGs. Users may delete those images
  without breaking legacy manifests; newly exported schema 3 contains no turn entries.
- Missing/unreadable optional directional clips fall back to generic movement frames.
  With no decodable fallback the existing visual remains, without aborting movement.
- New schema 3 permits directional-only movement and exports nested shared settings.
- Existing manifests, source images, and generated animation assets are not rewritten
  or deleted. Resource cleanup remains the user's responsibility.

## Verification

Automated coverage includes shared left/right speed, distance accumulation across clip
length changes, rendering restart, immediate direction routing, idle restoration,
missing-frame fallback, legacy aliases, metadata conflicts, missing retired PNGs, schema 3
round trips, external path safety, and unchanged non-movement tests.

The archived implementation was verified in Debug and Release before the minimal 0.1
restart. Any future reintroduction must define and execute a new test and visual-acceptance
plan against the then-current `main` code instead of relying on this historical result.
