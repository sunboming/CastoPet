# Radial Wheel Visual Refinement Design

## Goal

Improve the readability and visual hierarchy of the existing two-level radial wheel. The wheel currently appears too transparent against varied desktop backgrounds, and deselected sectors can lose the visual distinction between the first and second rings after selection updates.

## Scope

- Keep the current wheel dimensions, two-ring layout, labels, selection scaling, and right-button interaction unchanged.
- Retain the purple-to-white visual direction.
- Adjust only sector fills, outlines, label treatment, center treatment, and divider spacing.
- Do not add preview images, blur shaders, new settings, or animation behavior.

## Visual Design

### Normal State

- First-ring sectors use a deeper purple with alpha 140 (approximately 55% opacity).
- Second-ring sectors use a slightly lighter purple with alpha 122 (approximately 48% opacity).
- Disabled first-ring and second-ring sectors use alpha 84 and 72 respectively, with reduced label contrast.
- Ring-specific colors remain stable after selection changes.

### Selected State

- Selected sectors use a brighter purple with alpha 191 (approximately 75% opacity).
- The existing 1.18 scale feedback remains unchanged.
- The selected outline becomes brighter and slightly thicker without becoming visually heavy.

### Dividers and Labels

- Increase the sector angular gap from 0.012 to 0.016 radians so adjacent sectors have clearer separation.
- Use a soft purple-white outline with alpha 150 and thickness 0.9 for normal sectors, and a near-white outline with alpha 235 and thickness 1.5 for selected sectors.
- Reduce label shadow color alpha from 170 to 120, effect opacity from 0.78 to 0.58, and blur radius from 7 to 5.

### Center

- Increase the center fill alpha from 120 to 128, keeping it quieter than selectable sectors.
- Use the same softened outline family as the sectors.

## Implementation Shape

Centralize the wheel palette and style values in a small internal style definition used by both sector construction and selection refresh. Each radial item visual records which ring it belongs to so deselection can restore the correct ring-specific fill instead of a shared fallback color.

## Testing

- Add focused unit assertions for the centralized style values and their relative opacity hierarchy.
- Verify first-ring and second-ring normal fills remain distinct.
- Verify selected opacity is higher than both normal ring opacities.
- Run the full Debug and Release test suites and builds.

## Acceptance Criteria

- The wheel remains translucent but is clearly readable over common light and dark desktop backgrounds.
- The first and second rings remain visually distinct before and after hovering or selecting items.
- Selected items remain the strongest visual state through both scale and color.
- Existing wheel interaction and layout behavior do not change.
