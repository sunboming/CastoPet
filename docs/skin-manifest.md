# Skin Manifest

## Frame Timing

Each action can define a default frame duration with `frameIntervalMs` and optional
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

## External Skin Safety

External manifests loaded from disk keep the existing schema, but every image reference must
now meet the following rules:

- `resourceRoot` must be a local path relative to the manifest directory.
- Image paths must be relative to `resourceRoot` and remain inside it after canonicalization.
- UNC paths, rooted paths, URIs, symbolic links, and directory junctions are rejected.
- Referenced files must exist and use the `.png` extension with a valid PNG header.
- A manifest is limited to 512 KiB, 32 actions, 120 frames per action, 32 expressions,
  30 transition frames per expression, and 512 total image references.
- Each PNG is limited to 16 MiB, 4096 pixels per dimension, 16 megapixels, and a 4:1
  maximum aspect ratio.

Existing manifests do not need a schema-version change. A previous manifest remains compatible
when its resources already use local, contained PNG paths and stay within these budgets.
