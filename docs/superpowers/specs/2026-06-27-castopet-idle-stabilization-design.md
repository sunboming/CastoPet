# CastoPet Idle Stabilization Design

## Goal

Remove the visible "body jitter" from CastoPet's idle animation while keeping the idle sequence at exactly 8 frames and preserving the existing idle filenames.

## Current Problem

The current idle animation feels like the character is shaking instead of breathing naturally. The likely cause is a combination of PNG frame anchor drift and the recently added WPF breathing transform. If the image content moves inside the 320x320 canvas and the code also applies a transform, small frame-to-frame offsets become more noticeable.

The previous `Castorice.Idle.02.png` midpoint replacement improved numeric adjacent-frame difference, but it does not solve the root problem. The first stabilization pass should allow that frame to be restored to the original tracked asset before a more deliberate 8-frame alignment pass.

## Scope

This first phase is only about idle stabilization.

Included:

- Keep `IdleFrameSequence.FrameCount` at `8`.
- Keep these filenames unchanged:
  - `Castorice.Idle.00.png`
  - `Castorice.Idle.01.png`
  - `Castorice.Idle.02.png`
  - `Castorice.Idle.03.png`
  - `Castorice.Idle.04.png`
  - `Castorice.Idle.05.png`
  - `Castorice.Idle.06.png`
  - `Castorice.Idle.07.png`
- Restore `Castorice.Idle.02.png` from the original tracked version before re-evaluating the full sequence.
- Add a repeatable idle-frame diagnostics path that reports each frame's visible alpha bounds, bottom edge, center point, and adjacent-frame difference.
- Tune or disable the WPF idle breathing overlay so the PNG sequence can be evaluated without transform-induced jitter.
- Keep existing blink, expression wheel, and temporary expression behavior working.

Excluded:

- Adding more idle frames.
- Adding alternate idle states.
- Adding expression transition-frame sequences.
- Adding free movement, mouse following, or animated drag behavior.
- Using a browser or visual companion workflow.

## Design

### Idle Asset Diagnosis

Add a deterministic diagnostics utility in the test harness or a small core helper that reads PNG headers and alpha pixels for `Assets/States/Idle/*.png`.

For each frame it should calculate:

- Width and height.
- Visible alpha bounding box, using alpha greater than a small threshold.
- Horizontal center of the visible bounding box.
- Bottom edge of the visible bounding box.
- Adjacent-frame average delta, including the `07 -> 00` loop edge.

The purpose is not to force every frame to be identical. The purpose is to expose anchor drift so changes are deliberate. The most important values are bottom edge stability and visible center stability.

### Idle Breathing Overlay

The WPF idle breathing transform should not hide or amplify bad frame anchors. In this phase, reduce the transform amplitude to a near-neutral value or add a switchable constant that disables it by default.

The preferred first pass is:

- Keep the idle PNG frame playback active.
- Set `PetAnimationTimings.IdleBreathingTranslateY` to `0`.
- Set `PetAnimationTimings.IdleBreathingScaleDelta` to `0`.
- Keep the breathing helper in code so it can be re-enabled after the idle art is stable.

This makes the idle sequence easier to evaluate: if jitter remains, it is coming from the frames. If jitter disappears, the transform overlay was the main contributor.

### Asset Correction Strategy

Do not average two rendered frames as the final fix. Averaging can reduce numeric difference while creating a softer, less intentional image. Use it only as a diagnostic reference.

The correction target is a stable 8-frame loop:

- The character's feet and lower body should stay anchored.
- The head and body center should move minimally.
- Hair, sleeves, and dress edges can move, but they should not cause the whole silhouette to expand suddenly for one frame.
- The `07 -> 00` transition should be checked as carefully as the in-order transitions.

If a frame needs replacement, prefer a regenerated or manually corrected frame that preserves the character style and alpha edges. If a reliable generated replacement is not available, leave the original frame and reduce code-side motion first.

## Interaction Rules

- Idle playback remains the base state when the pet is not dragged, blinking, showing a temporary expression, or displaying the wheel.
- Dragging still has priority over idle.
- Blink still temporarily replaces idle frames.
- The expression wheel still cancels temporary expressions and pauses idle.
- No new movement or mouse-follow behavior is introduced in this phase.

## Testing

Automated tests should cover:

- Idle still declares exactly 8 frames.
- Idle frame interval remains intentional.
- Idle breathing values are neutral during stabilization.
- Idle asset diagnostics can read all 8 packaged idle PNGs.
- Every idle PNG remains within the existing display-size limit.

Manual validation should be text-only for this phase:

- A local app run may be used for direct human observation, but the visual companion/browser workflow remains disabled.
- Observe whether the idle character still appears to shake.
- If shaking remains, use diagnostics output to identify the next frame pair to correct.

## Follow-Up Order

After this idle stabilization phase is approved and implemented:

1. Add expression transition frames so expression changes stop feeling like hard cuts.
2. Add free movement, mouse-following, and animated drag behavior behind a clearer pet behavior state machine.

Those follow-up phases should each get their own design and plan.
