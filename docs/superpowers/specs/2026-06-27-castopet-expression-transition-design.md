# CastoPet Expression Transition Design

## Goal

Make expression-wheel selections feel less like a hard image swap by adding a small shared transition sequence before and after temporary expressions.

## Current Problem

The current expression flow uses the selected expression image directly:

```text
idle frame -> selected expression -> idle frame
```

There is a short opacity and scale animation, but the underlying character artwork still changes in one step. That makes expression changes feel abrupt even when WPF easing is active.

## Scope

Included:

- Add a small shared expression transition sequence.
- Use the transition for every expression-wheel item.
- Keep the existing 8 expression-wheel items and wheel UI unchanged.
- Keep each selected expression temporary.
- Keep drag and wheel interactions higher priority than expression playback.

Excluded:

- Per-expression custom transition frames.
- New expression-wheel options.
- New free movement or mouse-follow behavior.
- Reworking idle art again.
- Replacing the full pet animation state machine.

## First-Version Animation Flow

Use one shared transition-in sequence and one shared transition-out sequence:

```text
idle -> transition-in frames -> selected expression -> transition-out frames -> idle
```

First-version frame counts:

- Transition-in: 2 frames.
- Transition-out: 2 frames.

Recommended paths:

```text
Assets/Expressions/Transition/Castorice.ExpressionTransition.In.00.png
Assets/Expressions/Transition/Castorice.ExpressionTransition.In.01.png
Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.00.png
Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.01.png
```

Transition frame interval should be short, around `80ms` per frame. The selected expression should still hold for the existing `ExpressionWheelCatalog.ExpressionDuration`.

## Resource Strategy

The first implementation should prefer simple, stable transition assets over ambitious generated animation. The transition art should look like a neutral micro-motion:

- slight eye/face settling,
- small hair and accessory movement,
- no strong emotion,
- same 320x320 transparent canvas,
- same character center and lower anchor as the existing expressions.

If high-quality generated transition art is not reliable, the fallback is to initially reuse existing neutral/idle frames as temporary neutral transition resources while the playback mechanism is implemented and tested. The mechanism is more important than perfect art in this phase.

## Code Design

Add a small catalog for transition metadata, similar to `IdleFrameSequence` and `BlinkFrameSequence`.

Suggested type:

```csharp
public static class ExpressionTransitionSequence
```

Responsibilities:

- expose `InFrameCount`,
- expose `OutFrameCount`,
- expose `FrameInterval`,
- expose `InFramePaths`,
- expose `OutFramePaths`.

Extend `AssetService` to load transition frames as `ImageSource` lists.

In `PetWindow`, replace direct temporary expression display with a simple expression playback state:

1. Stop idle and blink.
2. Play transition-in frames.
3. Show selected expression.
4. Start the temporary expression hold timer.
5. When the hold timer fires, play transition-out frames.
6. Restore idle and blink.

This should use one frame timer for transition playback rather than adding separate timers for in and out. The implementation should keep cancellation simple:

- Drag cancels transition and expression.
- Opening the wheel cancels transition and expression.
- Selecting a new expression restarts from transition-in for that expression.

## Tests

Automated tests should cover:

- Transition-in and transition-out frame counts are 2.
- Transition frame interval is `80ms`.
- Transition resource paths follow the expected naming convention.
- Packaged PNG resource size tests still pass.
- Existing expression wheel item tests still pass.

Manual validation:

- Selecting an expression should no longer feel like a single hard cut.
- Transition frames should be brief and not make wheel selection feel sluggish.
- Dragging during a transition should still immediately switch to drag behavior.
- Opening the wheel during an expression should still cancel the temporary expression state.

## Follow-Up

After the shared transition mechanism is stable, per-expression custom transition frames can be added for the most visible expressions first, such as `Happy`, `Shy`, and `Surprised`.
