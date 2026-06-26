# CastoPet Idle Frames Design

Date: 2026-06-26

## Summary

CastoPet will replace the current transform-only idle animation with an 8-frame image sequence. The goal is a more natural desktop pet idle motion with breathing, subtle hair and skirt movement, and small body weight shifts.

## Scope

This change includes:

- Generate 8 transparent PNG idle frames.
- Save frames under `src/CastoPet/Assets/States/Idle/`.
- Name frames `Castorice.Idle.00.png` through `Castorice.Idle.07.png`.
- Play frames in a slow loop, about 200ms per frame.
- Pause idle playback while dragging.
- Show `Castorice.Dragging.png` while dragging.
- After drag release, wait 0.3 seconds, restore idle frame 0, and resume frame playback.

This change excludes:

- Live2D or skeletal animation.
- Runtime image generation.
- User-configurable animation speed.
- Complex random animation scheduling.
- Blink-only effects.

## Animation Direction

Idle frames should prioritize natural motion over perfect pixel consistency. Small differences in hair, skirt, accessories, or pose are acceptable if the overall character identity remains recognizable.

The desired loop is:

- Gentle breathing motion.
- Hair and skirt lightly swaying.
- Slight body weight shift.
- Calm, non-distracting pace suitable for a persistent desktop pet.

## Implementation

`AssetService` loads the 8 idle frame resources. `PetWindow` keeps the default and dragging images, plus an idle frame list. A `DispatcherTimer` advances the idle frame index every 200ms while the pet is not being dragged.

The old transform idle animation should be removed or reduced so it does not fight the frame animation. Dragging behavior remains unchanged except for pausing and resuming the frame timer.

## Testing

Automated tests should cover frame path generation or frame timing configuration where practical. Manual verification should cover:

- App starts successfully.
- Idle frames loop visibly.
- Dragging pauses idle playback and shows dragging image.
- Releasing drag restores idle playback after 0.3 seconds.
- Release build succeeds.
