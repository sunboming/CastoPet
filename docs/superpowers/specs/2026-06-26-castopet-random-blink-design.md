# CastoPet Random Blink Design

Date: 2026-06-26

## Summary

CastoPet will keep the existing 8-frame body idle animation and add an independent random blink animation. Blinking should feel occasional and natural rather than being tied to every body idle loop.

## Scope

This change includes:

- Generate 3 transparent PNG blink frames.
- Save frames under `src/CastoPet/Assets/States/Blink/`.
- Name frames `Castorice.Blink.00.png` through `Castorice.Blink.02.png`.
- Use the blink frames for half-closed, closed, and opening eyes.
- Trigger blink playback at a random interval between 3 and 7 seconds.
- Do not blink while dragging.
- Resume random blink scheduling after drag release.
- Return to the current body idle frame after blink playback ends.

This change excludes:

- Changing the existing 8-frame body idle loop.
- User-configurable blink frequency.
- Eye-layer compositing.
- Blink behavior during drag.

## Animation Direction

Blink frames should preserve the character identity, outfit, pose, scale, and transparent background. The only intended visible change is the eye state, with a slight natural eyelid shape. The expression should stay calm.

The blink sequence is:

- `Blink.00`: eyes half closed.
- `Blink.01`: eyes closed.
- `Blink.02`: eyes reopening or half open.

Each blink frame should display briefly, around 90ms per frame.

## Implementation

`BlinkFrameSequence` defines the frame paths, frame interval, and random scheduling bounds. `AssetService` loads the blink frame resources. `PetWindow` keeps the body idle timer and adds a blink schedule timer plus a blink playback timer.

When a blink starts, the body idle timer can continue advancing its index internally, but the displayed image is temporarily replaced by the blink frames. When the blink sequence finishes, `PetWindow` restores the current body idle frame. Dragging stops blink timers and shows the existing dragging image.

## Testing

Automated tests should cover blink frame count, frame timing, random interval bounds, and resource path naming. Manual verification should cover:

- App starts successfully.
- Body idle animation continues.
- Blink occurs occasionally without a fixed per-loop cadence.
- Dragging suppresses blink playback.
- Release build succeeds.
