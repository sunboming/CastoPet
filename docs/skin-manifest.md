# Skin Manifest

## Frame Timing

Each time-driven action can define a default frame duration with `frameIntervalMs` and optional
per-frame overrides with `frameDurationsMs`.

```json
{
  "id": "idle",
  "kind": "idle",
  "frameIntervalMs": 100,
  "frameDurationsMs": [240, null, 60],
  "frames": ["Idle/00.png", "Idle/01.png", "Idle/02.png"]
}
```

- `frameDurationsMs` is optional, so existing manifests remain compatible.
- The array must contain exactly one entry for every item in `frames`.
- A positive number overrides `frameIntervalMs` for the matching frame.
- `null` uses `frameIntervalMs`; if that is also absent, the runtime action fallback is used.
- Zero, negative, non-finite, and mismatched values are rejected when the manifest loads.
- Manifest export preserves both numeric overrides and `null` fallback entries.

Per-frame timing is applied to time-driven idle, blink, petting, and generic expression
transition actions. Move animation remains distance-driven so its frames stay synchronized
with desktop movement.

## Unified Movement (Schema 3)

The writer now emits `schemaVersion: 3`. The loader continues to accept versions 1 and 2.
The required action kinds remain `idle`, `move`, and `blink`; petting and expression
transitions remain optional.

```json
{
  "id": "move",
  "kind": "move",
  "frames": ["Move/Fallback.png"],
  "movement": {
    "distancePerFrame": 10,
    "baseSpeedPixelsPerSecond": 90,
    "minSpeedPixelsPerSecond": 80,
    "maxSpeedPixelsPerSecond": 105,
    "leftFrames": ["MoveLeft/00.png", "MoveLeft/01.png"],
    "rightFrames": ["MoveRight/00.png", "MoveRight/01.png"]
  }
}
```

- One movement action owns both directions; all four numeric settings are shared.
  Omitted values default to 10, 90, 80, and 105 respectively. Values must be finite,
  positive, and satisfy minimum <= base <= maximum.
- `movement` is required for schema 3 Move and forbidden on other actions. Old flat
  speed/distance properties are rejected in schema 3.
- `frames` is an optional generic fallback. Without it, both directional lists must
  be nonempty. Directional lists may have different lengths.
- Move cannot define frame intervals, per-frame durations, or scheduling delays in
  schema 3. Its frame progression is driven by distance, not elapsed frame time.
- Missing optional variants or decode failures use generic frames. If no frame set can
  be decoded, movement retains the current visual rather than crashing.
- Schema 1/2 `move-left` / `move-right` (and `moveLeft` / `moveRight`) are merged
  during load. Shared settings come from old `move`; explicit conflicting values in
  directional entries cause validation errors rather than silent parameter selection.
- Legacy movement timing metadata was not used for walking and is not re-exported.
- Legacy `turn-left` / `turn-right` (and camel-case forms) are ignored before image
  resolution. Their images may be deleted. Schema 3 does not accept these retired kinds
  or separate directional actions.
- Existing manifest files are not automatically rewritten. Export produces schema 3,
  which requires this version of CastoPet or newer. External-file exports keep paths
  relative to the original resource root; exporting does not copy PNGs.

## External Skin Safety

External manifests loaded from disk must meet the following rules:

- `resourceRoot` must be a local path relative to the manifest directory.
- Image paths must be relative to `resourceRoot` and remain inside it after canonicalization.
- UNC paths, rooted paths, URIs, symbolic links, and directory junctions are rejected.
- Referenced files must exist and use the `.png` extension with a valid PNG header.
- A manifest is limited to 512 KiB, 32 actions, 120 frames per action, 32 expressions,
  30 transition frames per expression, and 512 total image references.
- Each movement direction is also limited to 120 frames; both lists and generic fallback
  frames contribute to the total reference budget and use the same path/PNG validation.
- Each PNG is limited to 16 MiB, 4096 pixels per dimension, 16 megapixels, and a 4:1
  maximum aspect ratio.

Existing manifests do not need a schema-version change. A previous manifest remains compatible
when its resources already use local, contained PNG paths and stay within these budgets.
