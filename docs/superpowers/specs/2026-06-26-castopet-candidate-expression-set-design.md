# CastoPet Candidate Expression Set Design

Date: 2026-06-26

## Summary

CastoPet will generate a new candidate sprite set without replacing the current production assets. The candidate set uses the existing program asset structure so approved files can be copied into place later, but it stays isolated until visual review is complete.

The standard character image at `src/CastoPet/Assets/Castorice.png` is the authority for character color, outfit, proportions, and identity. The images in `sample/` are expression references only. Their backgrounds, captions, partial framing, and color shifts should not be copied into the final desktop pet sprites.

## Scope

This design includes:

- A candidate core asset set matching the current program resource names.
- A candidate expression library with 12 extra full-body expression sprites.
- High-resolution green-screen source images for rework.
- 320x320 transparent PNG outputs for low-memory desktop use.
- Contact-sheet previews for visual review.

This design excludes:

- Replacing current production assets.
- Changing WPF runtime code.
- Adding expression switching behavior to the app.
- Adding new state-management logic.
- Using sample image backgrounds, captions, or cropped compositions.

## Candidate Directory

Candidate files should be written under:

```text
src/CastoPet/Assets/CandidateSet/
```

High-resolution green-screen source images:

```text
src/CastoPet/Assets/CandidateSet/Source/
  Castorice.png
  States/
    Castorice.Happy.png
    Castorice.Sleepy.png
    Castorice.Surprised.png
    Castorice.Dragging.png
    Idle/
      Castorice.Idle.00.png
      Castorice.Idle.01.png
      Castorice.Idle.02.png
      Castorice.Idle.03.png
      Castorice.Idle.04.png
      Castorice.Idle.05.png
      Castorice.Idle.06.png
      Castorice.Idle.07.png
    Blink/
      Castorice.Blink.00.png
      Castorice.Blink.01.png
      Castorice.Blink.02.png
  Expressions/
    Castorice.Expression.Happy.png
    Castorice.Expression.Shy.png
    Castorice.Expression.Sleepy.png
    Castorice.Expression.Surprised.png
    Castorice.Expression.Pouting.png
    Castorice.Expression.Confused.png
    Castorice.Expression.Proud.png
    Castorice.Expression.Worried.png
    Castorice.Expression.Crying.png
    Castorice.Expression.Excited.png
    Castorice.Expression.Bored.png
    Castorice.Expression.Affection.png
```

Transparent 320x320 desktop-ready images:

```text
src/CastoPet/Assets/CandidateSet/Transparent/
  Castorice.png
  States/
    Castorice.Happy.png
    Castorice.Sleepy.png
    Castorice.Surprised.png
    Castorice.Dragging.png
    Idle/
      Castorice.Idle.00.png
      Castorice.Idle.01.png
      Castorice.Idle.02.png
      Castorice.Idle.03.png
      Castorice.Idle.04.png
      Castorice.Idle.05.png
      Castorice.Idle.06.png
      Castorice.Idle.07.png
    Blink/
      Castorice.Blink.00.png
      Castorice.Blink.01.png
      Castorice.Blink.02.png
  Expressions/
    Castorice.Expression.Happy.png
    Castorice.Expression.Shy.png
    Castorice.Expression.Sleepy.png
    Castorice.Expression.Surprised.png
    Castorice.Expression.Pouting.png
    Castorice.Expression.Confused.png
    Castorice.Expression.Proud.png
    Castorice.Expression.Worried.png
    Castorice.Expression.Crying.png
    Castorice.Expression.Excited.png
    Castorice.Expression.Bored.png
    Castorice.Expression.Affection.png
```

If a generated expression is hard to classify after visual review, a numeric fallback name such as `Castorice.Expression.00.png` may be used, but semantic emotion names are preferred.

## Core Asset Set

The core set is generated first because it maps directly to the current app:

- `Castorice.png`: default calm full-body sprite.
- `Castorice.Happy.png`: happy full-body state.
- `Castorice.Sleepy.png`: sleepy full-body state.
- `Castorice.Surprised.png`: surprised full-body state.
- `Castorice.Dragging.png`: mildly surprised dragging state.
- `Castorice.Idle.00.png` through `Castorice.Idle.07.png`: quiet idle loop.
- `Castorice.Blink.00.png` through `Castorice.Blink.02.png`: blink sequence.

Idle frames should keep the default calm expression and use subtle breathing, hair sway, skirt movement, and small weight shifts. Blink frames should only change the eye state and restore to the current idle look after playback.

## Expression Library

The expression library contains 12 extra full-body sprites:

- `Happy`: smiling, warm, friendly.
- `Shy`: bashful, lightly blushing.
- `Sleepy`: tired or half-closed eyes.
- `Surprised`: wide-eyed and startled, not frightened.
- `Pouting`: mildly annoyed or puffed-cheek expression.
- `Confused`: puzzled or blank reaction.
- `Proud`: confident, pleased with herself.
- `Worried`: uneasy, hesitant, or small frown.
- `Crying`: teary or sad, still cute rather than distressed.
- `Excited`: bright, eager, high-energy expression.
- `Bored`: flat, unimpressed, or sleepy-bored expression.
- `Affection`: affectionate, clingy, or soft happy expression.

All expression sprites should be full-body desktop pet images, centered on a square canvas with consistent scale and padding. The visual pose can change enough to communicate the emotion, but the outfit, hair, eye color, headpiece, floral details, and chibi proportions should stay aligned with the standard `Castorice.png`.

## Reference Usage

Use all 16 files in `sample/` as a reference pool, then consolidate their ideas into the 12 expression categories above.

Rules:

- Extract expression intent only.
- Do not preserve sample backgrounds.
- Do not preserve sample captions or text.
- Do not preserve half-body or close-up composition.
- Do not adopt sample color shifts when they conflict with `Castorice.png`.
- Do not add extra characters, props, speech bubbles, watermarks, or UI text.

## Generation Workflow

Generation should happen in two batches.

Batch 1: core program assets.

Batch 2: expression library.

For each image:

1. Generate a high-resolution source sprite on a perfectly flat solid chroma-key background.
2. Save the source image under `CandidateSet/Source/`.
3. Remove the chroma-key background locally.
4. Save a transparent PNG under `CandidateSet/Transparent/`.
5. Downsample the transparent output to 320x320.
6. Validate transparency, scale, and visible character coverage.

After each batch, generate a contact-sheet preview showing file names and thumbnails. Preview sheets are review artifacts and should not be wired into the application.

## Acceptance Criteria

- Current production assets are not overwritten.
- Source and transparent candidate files are both retained.
- Transparent PNG outputs are 320x320.
- Transparent PNG corners are transparent.
- All sprites use full-body desktop pet framing.
- Core asset names match the current program naming structure.
- Expression assets use semantic emotion names where the emotion is clear.
- The 12 expression sprites are visually distinct.
- Character identity, outfit, hair color, eye color, and palette follow `src/CastoPet/Assets/Castorice.png`.
- `sample/` images influence expression only, not color, background, captions, or composition.

## Future Integration

After visual approval, the transparent core assets can be copied over the current production resources. Expression assets can later be wired into click reactions, random mood states, context menus, or a future expression-selection system. That integration is outside this design.
