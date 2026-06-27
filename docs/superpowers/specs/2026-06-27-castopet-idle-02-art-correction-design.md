# CastoPet Idle 02 Art Correction Design

## Goal

Create a new candidate `Castorice.Idle.02.png` that reduces the visible jump between `Idle.01` and `Idle.03` without changing the idle frame count, filenames, or runtime animation code.

## Problem

The idle stabilization diagnostics show that the character's bottom edge and horizontal center are already stable. The largest remaining visual jump is concentrated around `Idle.01 -> Idle.02 -> Idle.03`.

The current `Idle.02` has a much wider visible silhouette than its neighbors:

- `Idle.01`: visible x range about `53..266`
- `Idle.02`: visible x range about `36..282`
- `Idle.03`: visible x range about `53..266`

This makes the character appear to pulse or shake even though the canvas anchor is stable.

## Scope

Included:

- Generate or edit a new candidate for `Idle.02`.
- Use `Idle.01` and `Idle.03` as the primary continuity references.
- Keep the candidate as a separate file first.
- Validate the candidate with the existing idle diagnostics before it can replace the production asset.

Excluded:

- Changing `IdleFrameSequence.FrameCount`.
- Changing idle frame timing.
- Changing breathing transform constants.
- Editing `Idle.01` or `Idle.03`.
- Adding expression transition frames.
- Adding movement or mouse interaction.

## Candidate Requirements

The candidate should:

- Stay in the same 320x320 transparent canvas.
- Preserve the standard Castorice style, colors, face, costume, flowers, and proportions.
- Keep the bottom edge anchored at the same lower position as the other idle frames.
- Keep the visual center near the existing `159.5..160.0` range.
- Avoid the wide one-frame silhouette expansion seen in the current `Idle.02`.
- Look like a natural in-between pose between `Idle.01` and `Idle.03`.
- Allow only subtle movement in hair tips, sleeves, dress edges, and small accessories.
- Avoid blur, ghosting, double exposure, extra limbs, extra accessories, new facial expression, text, watermark, or background.

## Output Strategy

Use the built-in image generation/edit path first. Because the built-in tool cannot guarantee direct file-path editing, load the local reference images into context, generate a candidate image, then save the selected candidate into a project candidate path instead of overwriting the production file.

Candidate path:

```text
src/CastoPet/Assets/CandidateSet/Transparent/States/Idle/Castorice.Idle.02.candidate.png
```

If the generated output is not transparent, save it as a candidate preview only and do not wire it into the app. A production replacement must be a transparent 320x320 PNG.

## Validation

Before replacing the production `Idle.02`, validate:

- The candidate is 320x320.
- It has an alpha channel or a clean transparent background.
- The visible bottom edge matches the idle sequence.
- The visible center remains near the rest of the sequence.
- The silhouette width is closer to `Idle.01` and `Idle.03` than the current wide `Idle.02`.
- The app tests and release build still pass after any production replacement.

## Follow-Up

If a candidate passes diagnostics and visual inspection, replace `src/CastoPet/Assets/States/Idle/Castorice.Idle.02.png` in a separate implementation step and commit only that asset change.
