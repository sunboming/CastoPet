# CastoPet Input Reactive Mode Design

## Goal

Add a Bongo Cat style input reactive mode to CastoPet. When enabled, the pet switches to a special half-body keyboard composition and reacts to global keyboard and mouse input by lighting the corresponding keyboard area.

## User-Facing Behavior

- Add a persisted `InputReactiveMode` setting.
- Add an `输入响应模式` checked menu item to both the tray menu and pet context menu.
- When enabled, CastoPet shows a dedicated input-reactive visual: Castorice as a half-body/chibi desk-pet composition, face angled roughly down-left, with a Q-style keyboard in front.
- Keyboard input highlights the matching key for a short time.
- Mouse clicks trigger a short visual pulse near the keyboard or mouse-feedback area.
- When disabled, CastoPet returns to the existing normal pet state.

## Scope

First version includes:

- Mode toggle and settings persistence.
- Global keyboard and mouse input capture while the mode is enabled.
- Key highlight timing and cleanup.
- Keyboard layout mapping for common keys:
  - Letters A-Z
  - Number row 0-9
  - Space
  - Enter
  - Backspace
  - Shift
  - Ctrl
  - Alt
  - Arrow keys
- A base input-reactive asset plus code-drawn highlight overlay.

First version excludes:

- Per-key generated art files.
- Full hand/arm hitting animation.
- Audio or rhythm response.
- Displaying typed text.

## Visual Design

The base image should stay consistent with the current Castorice palette and asset style. The character is shown as a half-body desktop composition, looking approximately 45 degrees toward the lower-left keyboard area. The keyboard sits in the lower foreground and is simplified enough for readable key highlights at pet scale.

The overlay is rendered by WPF rather than baked into every image. Each key has a stable rectangle or rounded rectangle in pet-local coordinates. Pressed keys briefly fill with a soft violet-white highlight that fades out quickly.

## Runtime Priority

Input reactive mode becomes a high-priority display mode but does not override dragging.

Priority order:

1. Dragging
2. Expression wheel open
3. Temporary expression transition
4. Input reactive mode
5. Active movement
6. Idle/blink

While input reactive mode is enabled:

- Active movement and push cursor are paused.
- Idle/blink frame animation is paused.
- Dragging still works.
- Opening the expression wheel temporarily pauses input reactive visuals.

## Architecture

### Settings

Add `InputReactiveMode` to `AppSettings`, settings JSON round-trip tests, `PetWindowSettingsSnapshot`, `MenuCommandService`, `TrayService`, and the pet context menu refresh path.

### Input Capture

Create a Windows input hook service with a small interface so the runtime can start/stop it based on the mode. The service emits normalized events such as:

- `InputReactiveEvent.KeyDown(key)`
- `InputReactiveEvent.MouseDown(button)`

The implementation should avoid logging typed text and should not store input history beyond the short-lived highlight state.

### Keyboard Layout

Create `InputKeyboardLayout` in Core. It maps normalized key IDs to rectangles in the input-reactive visual coordinate space. Tests cover representative keys and unknown-key fallback.

### Highlight State

Create `InputReactiveState` in Core. It tracks currently active highlights with expiration timestamps. Tests cover adding a key, expiration after the configured duration, and independent mouse events.

### PetWindow Integration

`PetWindow` owns the WPF overlay and image swapping:

- Load the input-reactive base asset through `AssetService`.
- On mode enabled, switch `CharacterImage.Source` to the input-reactive base image.
- Draw highlight rectangles over the keyboard using a Canvas.
- Start the input hook only while the mode is enabled and the window is visible.
- Stop the input hook when disabled, hidden, dragging, or exiting.

## Assets

Add:

- `Assets/States/InputReactive/Castorice.InputReactive.Base.png`

Use code-drawn highlights for the first version. If the base asset is missing, mode should fail gracefully: log the missing resource and keep the normal pet visual instead of crashing.

## Testing

Tests should cover:

- Default setting is disabled.
- Settings round-trip includes `InputReactiveMode`.
- Tray/context menu exposes `输入响应模式`.
- Keyboard layout contains common keys and keeps key rectangles inside the 320x420 visual bounds.
- Highlight state expires pressed keys.
- `PetWindowSettingsSnapshot` copies the new setting.
- Asset path is included in project resources.

Manual verification after implementation:

- Enable mode from tray menu.
- Press `A`, `Space`, `Enter`, arrow keys and confirm corresponding highlights.
- Click left/right mouse and confirm short pulse.
- Open wheel and drag pet to confirm no interaction conflict.
- Disable mode and confirm normal idle/movement behavior returns.
