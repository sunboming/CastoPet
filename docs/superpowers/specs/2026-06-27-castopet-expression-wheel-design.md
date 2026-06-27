# CastoPet Expression Wheel Design

## Goal

Add a first-version radial expression wheel to the desktop pet. The user holds the right mouse button on the pet to open a circular wheel, drags while still holding the button, and releases over an item to trigger that expression.

## User Interaction

- A normal short right click keeps the existing context menu behavior.
- Holding the right mouse button for about 250 ms opens the expression wheel.
- While the wheel is open, the right button remains held and mouse movement selects the nearest wheel item.
- Releasing the right button closes the wheel.
- If the cursor is over a valid item on release, the pet switches to that expression briefly.
- If the cursor is still near the center or outside the wheel on release, no expression is applied.
- Left-button dragging remains unchanged.

## First-Version Items

The wheel has eight expression items:

- Happy
- Shy
- Sleepy
- Surprised
- Pouting
- Confused
- Proud
- Crying

The item order should be stable and clockwise. The first version promotes only these eight transparent candidate expression PNGs into `src/CastoPet/Assets/Expressions/` and loads them from app resource paths shaped as `Assets/Expressions/Castorice.Expression.<Name>.png`.

## Expression Behavior

Expression selection is temporary. After a successful selection:

- Stop idle and blink animation.
- Set the character image to the selected expression.
- Keep it visible for about 2 seconds.
- Restore idle animation and blink scheduling afterward.

If the user starts dragging or opens the wheel during an active temporary expression, the temporary expression is cancelled.

## Visual Design

The wheel is an overlay inside `PetWindow`.

- The wheel background is semi-transparent with a refined purple tone.
- Each item sits in a separated wedge-like hit target.
- Wedges are divided by thin radial separators.
- The first version does not show expression preview images inside the wheel.
- Each item uses a short English emotion label.
- The selected wedge becomes brighter, and its label scales up around 1.18x.
- Non-selected labels stay slightly smaller and more transparent.
- The overlay should feel light and not obscure the pet more than necessary.

The first version does not need a separate transparent top-level window. Keeping the wheel inside the current pet window limits focus, click-through, topmost, and screen-boundary complexity.

## Implementation Shape

Add a small radial-wheel UI layer to `PetWindow.xaml`, hidden by default. `PetWindow.xaml.cs` owns the first-version interaction state:

- right-button press position
- hold timer
- whether the wheel is open
- selected wheel index
- temporary expression timer

Expression metadata should live in a small code structure rather than duplicated switch blocks. The metadata includes label and asset path.

`AssetService` should gain a focused expression-loading API that decodes expression PNGs at the same 320-pixel display width as other character assets.

## Error Handling

If an expression asset fails to load, log the failed path and exclude that item from the wheel rather than crashing the app.

If no expression assets load, the wheel should not open and the existing right-click context menu should continue to work.

## Testing

Add non-UI coverage for stable expression metadata:

- The first version defines exactly eight wheel expressions.
- The order starts with Happy and includes the expected eight expression names.
- Expression asset paths use the expected `Assets/Expressions/Castorice.Expression.<Name>.png` shape.

Run the existing app test harness and a release build after implementation.

## Out Of Scope

- Nested wheel pages.
- Keyboard shortcuts.
- Custom user-configurable expression order.
- More than eight items.
- Persisting the selected expression.
- Replacing the existing tray or context-menu commands.
