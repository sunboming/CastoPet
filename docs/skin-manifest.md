# Skin Manifest Timing

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
