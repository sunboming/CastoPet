# CastoPet Smoother Animation Design

## Goal

Improve the perceived smoothness of CastoPet animation without requiring a large new sprite library. The first pass should make idle motion, expression changes, dragging restore, and expression-wheel feedback feel less abrupt.

## Reference Direction

VPet-style desktop pets feel smoother because animation is treated as a stateful playback system rather than a few independent timers that directly swap images. CastoPet should move in that direction incrementally:

- Keep the current PNG sprite assets.
- Add transform and opacity easing around sprite changes.
- Centralize animation state transitions so idle, blink, drag, wheel, and temporary expressions do not fight each other.
- Leave large multi-action animation libraries for later.

## First-Version Scope

This version focuses on four improvements:

1. Idle breathing motion
   - Keep the existing idle PNG frame sequence.
   - Add a subtle always-on WPF transform loop while idle is active.
   - Use very small vertical movement and scale changes so the pet feels alive without bobbing aggressively.

2. Expression transition smoothing
   - When a wheel expression is selected, fade/scale into the expression instead of hard switching.
   - Hold the expression for the existing short duration.
   - Fade/scale back to idle afterward.

3. Wheel micro-animation
   - When the wheel opens, scale and fade the wheel surface in over a short duration.
   - When selection changes, animate the label/sector emphasis instead of snapping instantly.

4. Animation state coordination
   - Add a small state-oriented helper or controller so the window has one place responsible for stopping idle, blink, wheel, drag, and expression timers during transitions.
   - Avoid a large framework rewrite in this pass.

## Interaction Rules

- Left-button dragging remains the highest-priority interaction.
- Opening the expression wheel cancels an active temporary expression.
- Starting drag cancels wheel and temporary expression states.
- Blink should not interrupt drag, wheel, or temporary expression display.
- Idle breathing resumes after drag restore and after temporary expression restore.

## Visual Timing

Use short timings so the desktop pet stays responsive:

- Idle frame interval can stay near the current value unless tests and visual review justify changing it.
- Idle transform loop should be slow, around 1.6 to 2.4 seconds per cycle.
- Expression enter transition should be around 120 ms.
- Expression exit transition should be around 160 to 220 ms.
- Wheel open transition should be around 100 to 140 ms.
- Selection emphasis should be around 80 to 120 ms.

## Implementation Shape

Introduce named constants for animation timings and transform amplitudes in a focused core class. Add tests for those constants so future tuning is intentional.

In `PetWindow`, use WPF `Storyboard` or direct animation APIs for:

- `CharacterImage.RenderTransform`
- `CharacterImage.Opacity`
- `ExpressionWheelSurface.RenderTransform`
- expression wheel label and sector emphasis

The implementation should avoid creating many new timers. Existing timers may remain for frame sequencing, but visual easing should be handled by WPF animations.

## Testing

Automated tests should cover:

- Animation timing constants.
- Idle breathing values are subtle.
- Expression transition durations are short.
- Existing idle, blink, expression wheel, and packaged asset tests still pass.

Manual smoke testing is still required for perceived smoothness:

- The pet should idle without visible stutter.
- Selecting an expression should not hard-cut.
- The wheel should open and highlight smoothly.
- Dragging should still feel immediate.

## Out Of Scope

- Generating a large VPet-sized animation library.
- Adding new interactions such as petting, feeding, walking, or climbing.
- Replacing the current PNG asset pipeline.
- Building a full animation graph editor.
- Persisting animation state across app restarts.
