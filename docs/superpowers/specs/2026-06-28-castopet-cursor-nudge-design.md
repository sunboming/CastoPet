# CastoPet Cursor Nudge Design

## Goal

Let CastoPet gently push the mouse cursor while the pet is actively moving near the cursor.

## Behavior

- Add a separate `推动鼠标` setting.
- Default is off.
- Cursor push only works when `主动移动` is also on.
- Cursor push is suspended during click-through, dragging, expression wheel, temporary expression, and expression transitions.
- Cursor push only applies when the cursor is close to the pet, around `60px`.
- Each render frame can move the cursor only a tiny amount, clamped to `1..3px`.
- If the user moves the cursor manually, cursor push pauses briefly, around `1s`.

## Implementation

- Add `PushCursor` to `AppSettings`.
- Add a checked menu item in tray and window context menu.
- Add a pure `CursorNudgePlanner` for testable math and gating constants.
- Add a small Windows cursor service around `GetCursorPos` and `SetCursorPos`.
- Call cursor push from active movement rendering after the pet position changes.

## Safety

- The feature must be opt-in.
- The cursor must be clamped to the current work area.
- User manual cursor movement always wins.
