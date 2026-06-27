# CastoPet Active Movement Design

## Goal

Add optional active movement so CastoPet can move around on its own, approach the mouse pointer, and make dragging feel animated instead of static.

## User Choices

- Movement style: active and noticeable.
- Mouse approach distance: close to the pointer, around `20..40px`, without covering the cursor.
- Control: active movement must have a menu toggle.

## Scope

Included:

- Add an `ActiveMovement` setting, disabled by default.
- Add a checked menu item for active movement in both tray menu and window context menu.
- When active movement is enabled, the pet can choose movement targets and animate the window position toward them.
- Mouse proximity should influence the target so the pet moves toward the cursor.
- The pet should stop near the cursor instead of directly covering it.
- Dragging should get a small visual motion treatment, such as tilt or squash, while keeping actual drag responsive.

Excluded:

- New walking sprites.
- Physics simulation.
- Pathfinding around windows or obstacles.
- Multi-monitor advanced behavior beyond keeping the pet inside the current work area.
- Persistence of movement target/state across app restarts.

## Behavior Model

Use a small movement controller inside `PetWindow` for the first version. It should coordinate with existing state instead of replacing the full animation system.

Movement states:

- Idle: no active window movement.
- Wander: choose a random point in the current work area and ease toward it.
- ApproachMouse: move toward the mouse pointer when it is within an interest radius.
- Dragging: user drag has priority; automatic movement pauses.
- Suspended: wheel, temporary expression, or click-through-sensitive situations pause automatic movement.

Priority order:

1. Left-button drag.
2. Expression wheel.
3. Temporary expression / expression transition.
4. Mouse approach.
5. Random wander.
6. Idle.

## Movement Rules

Active movement only runs when:

- `AppSettings.ActiveMovement` is true,
- the pet window is visible,
- click-through is false,
- the expression wheel is closed,
- the user is not dragging,
- no temporary expression or expression transition is playing.

Mouse approach:

- Poll mouse position on a timer.
- If the pointer is within an interest radius, move toward a point near the pointer.
- Stop at an offset of around `32px` from the pointer, clamped to `20..40px`.
- Clamp the window target to the current work area so the full pet remains visible.

Wander:

- If the mouse is not interesting, occasionally choose a nearby target point.
- Keep wander movement slower and less frequent than mouse approach.
- Avoid constant movement; the pet should pause between movements.

## Visual Motion

Window movement should be eased, not instant:

- Use a movement tick timer, around `16ms` to `24ms`.
- Move a fraction of the remaining distance each tick.
- Stop when the remaining distance is small.

Character visual treatment:

- During automatic movement, apply a small horizontal scale or tilt based on movement direction.
- During dragging, apply a slightly stronger but brief tilt/squash effect.
- Reset transforms when movement stops.

The first version should reuse the current images. It should not require new movement sprites.

## Settings And Menu

Add:

```csharp
public bool ActiveMovement { get; set; }
```

Menu label:

```text
主动移动
```

Menu placement:

- Add it near existing interaction-related settings.
- It should be checked when enabled.
- Toggling should save settings and immediately start or stop movement behavior.

Default:

- Use `false` for first implementation so the new active behavior is opt-in and cannot surprise users after update.

## Testing

Automated tests should cover:

- `ActiveMovement` defaults to false.
- Settings JSON round-trips `ActiveMovement`.
- Invalid settings still fall back to defaults.
- Tray/menu text constants include `主动移动`.
- Movement target calculations clamp to the work area.
- Mouse approach target preserves a close but nonzero cursor offset.

Manual validation:

- With active movement off, existing behavior is unchanged.
- With active movement on, the pet approaches the mouse without covering it.
- The pet does not move while the expression wheel is open.
- The pet does not move while being dragged.
- Dragging still feels immediate.
- Turning the menu option off stops automatic movement.

## Follow-Up

After this first version works, movement sprites can be added. A later pass can add walking-specific frames, richer mouse reactions, and per-state animation assets.
