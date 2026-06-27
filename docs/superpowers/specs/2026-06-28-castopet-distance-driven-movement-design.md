# CastoPet Distance-Driven Movement Design

## Goal

Make active movement feel smoother by synchronizing character movement frames with actual window travel distance instead of playing animation frames on an unrelated timer.

## Current Evidence

- Static character movement felt much smoother than moving with idle frames and per-tick scale feedback.
- This suggests the main issue is not only window movement, but mismatch between visible character animation and actual window displacement.
- The next movement system should keep position updates and movement frames tied together.

## Asset Plan

Add a dedicated move cycle:

- Directory: `src/CastoPet/Assets/States/Move/`
- Files: `Castorice.Move.00.png` through `Castorice.Move.07.png`
- Format: `320x320` transparent PNG.
- Style: front-facing chibi Castorice, same standard colors and outfit as `Assets/Castorice.png`.
- Motion: small stepping loop with subtle hair, skirt, and ribbon sway.

The move frames should be used only while automatic movement is active. Idle and blink frames should stay paused during movement until movement feels stable.

## Runtime Model

Replace the movement timer with display-synchronized movement:

- Use `CompositionTarget.Rendering` for active movement ticks.
- Track high-precision logical position separately from WPF `Left` and `Top`.
- Move toward the current target using a fixed base speed with a small allowed range.
- Snap the final window position to integer pixels to reduce jitter.

Movement speed:

- Base speed: around `90 px/s`.
- Allowed range: around `80..105 px/s`.
- Use target distance and state to choose speed within this range, not arbitrary easing.

Frame selection:

- Drive move animation from accumulated travel distance.
- Advance one move frame after a fixed distance, for example every `10px`.
- This keeps the walk cycle visually linked to actual motion speed.
- Reset to frame `00` or idle image when movement stops.

## State Rules

Active movement remains opt-in through the existing `主动移动` setting.

Movement still pauses when:

- click-through is enabled,
- the expression wheel is open,
- the pet is being dragged,
- a temporary expression or expression transition is active.

While moving:

- play `Move` frames by distance,
- keep idle and blink frame timers disabled,
- avoid per-frame transform changes that fight the sprite animation.

When stopped:

- stop move frame progression,
- restore static idle image first,
- idle and blink can be restored in a later pass after movement is stable.

## Testing

Automated tests should cover:

- move frame sequence has 8 paths and a distance-per-frame constant,
- move frame paths use app resources,
- move speed constants stay within the chosen range,
- packaged move PNGs stay at display size,
- movement step calculation advances by speed * elapsed time and clamps to target without overshoot.

Manual validation:

- with active movement off, pet stays still,
- with active movement on, window movement remains smooth,
- move frames advance only when the pet actually travels,
- stopping near the cursor does not jitter,
- opening the expression wheel pauses movement and move frames.
