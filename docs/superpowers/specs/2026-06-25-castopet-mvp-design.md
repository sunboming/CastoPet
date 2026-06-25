# CastoPet MVP Design

Date: 2026-06-25

## Summary

CastoPet MVP is a low-resource Windows desktop virtual pet shell. The first version focuses on showing a single static character on the desktop with reliable system controls. It does not include pet growth, chat, animation, dragging, or click feedback.

The app uses C# and WPF. WPF is preferred because it supports transparent borderless windows, topmost behavior, tray integration, and PNG display with lower overhead than Electron and lower implementation cost than raw Win32.

## Product Scope

The MVP includes:

- A transparent, borderless WPF pet window.
- A static built-in character image named `Castorice.png`.
- A fixed default position near the bottom-right of the screen.
- Topmost mode, enabled by default and user-toggleable.
- Mouse click-through mode, disabled by default and user-toggleable.
- System tray menu and character right-click menu.
- Show/restore behavior.
- Toggleable start with Windows.
- Toggleable taskbar icon visibility.
- Single-instance behavior.
- Basic persisted settings.
- Lightweight local logging.
- User-visible prompts for key failures.

The MVP excludes:

- Dragging the pet.
- Saving custom position.
- Left-click behavior.
- Click feedback.
- Expressions, animations, or state images.
- Growth or pet stats.
- AI chat.
- A standalone settings window.
- Auto-update.
- Special multi-monitor handling.
- Final release packaging decisions.
- External asset override or asset pack loading.

## Architecture

The application should stay small, but responsibilities should not all live inside the WPF window class.

### App

Owns application startup and shutdown. It initializes logging, settings, single-instance handling, the main pet window, and tray services.

### PetWindow

Owns the visible desktop character window. It applies:

- Transparent borderless presentation.
- Fixed bottom-right positioning.
- Built-in `Castorice.png` display.
- Topmost state.
- Click-through state.
- Taskbar visibility state.
- Character right-click menu.

The window does not implement dragging or left-click behavior in the MVP.

### TrayService

Owns the system tray icon and tray menu. It exposes the same core commands as the character right-click menu where practical:

- Show/restore.
- Always on top.
- Mouse click-through.
- Show taskbar icon.
- Start with Windows.
- Exit.

When mouse click-through is enabled, the tray menu is the required recovery path because the character window no longer receives mouse input.

### SettingsService

Reads and writes user settings from:

`%LocalAppData%\CastoPet\settings.json`

Persisted fields:

- `Topmost`
- `ClickThrough`
- `ShowInTaskbar`
- `StartWithWindows`

Not persisted:

- Hidden or visible state.
- Window position.
- Window size.
- Monitor identity.

Default values:

- `Topmost = true`
- `ClickThrough = false`
- `ShowInTaskbar = false`
- `StartWithWindows = false`

### StartupService

Manages current-user start with Windows. It should avoid requiring administrator privileges. Failures must be logged and shown to the user.

### AssetService

Loads the built-in `Castorice.png` image for MVP.

External asset override is not part of the first version. The code should avoid naming decisions that make future asset packs difficult, but no manifest format, external directory, or override file name is specified for MVP.

### LoggingService

Writes lightweight local logs under:

`%LocalAppData%\CastoPet\logs`

Logs should cover startup, shutdown, key state changes, and exceptions. Normal successful menu actions should stay quiet unless useful for diagnosing a problem.

### SingleInstanceService

Prevents multiple CastoPet instances. A second launch should not create a second pet window. It should ask the existing instance to show or restore the pet to the bottom-right default position.

## Interaction Design

### Startup

On normal startup, CastoPet shows the pet near the bottom-right of the screen. The hidden state is not restored from a previous run.

### Show/Restore

`Show/Restore` is a single command:

- If the pet is hidden, show it.
- If the pet is already visible, move it back near the bottom-right default position.

This command also acts as the recovery path if the user loses track of the pet.

### Always On Top

Enabled by default. Toggling it applies immediately and persists to settings.

### Mouse Click-Through

Disabled by default. Toggling it applies immediately and persists to settings.

When enabled, mouse input passes through the pet window to windows behind it. The user must use the system tray menu to disable click-through.

### Taskbar Icon

Hidden by default. Toggling taskbar visibility applies immediately and persists to settings.

### Start With Windows

Disabled by default. Toggling it updates the current-user startup registration and persists the desired setting only if the operation succeeds. If startup registration fails, the app logs the failure and informs the user.

### Exit

Exit closes the pet window, removes the tray icon, flushes logs as needed, and ends the app process.

## Error Handling

Configuration read failure:

- Log the error.
- Start with default settings.

Configuration save failure:

- Log the error.
- Show a user-visible prompt when the failed operation affects a user setting.

Built-in image load failure:

- Log the error.
- Show a user-visible prompt.

Start with Windows failure:

- Log the error.
- Show a user-visible prompt.
- Keep the setting consistent with the actual startup registration result.

Single-instance communication failure:

- Log the error.
- Avoid crashing when possible.

The MVP does not use tray balloon notifications. Key errors use a normal lightweight prompt.

## Testing Plan

Manual and automated checks should cover:

- First launch shows one pet window near the bottom-right of the screen.
- A second launch does not create another pet window.
- A second launch restores the existing pet if it is hidden.
- Show/restore shows a hidden pet.
- Show/restore moves a visible pet back near the bottom-right default position.
- Always on top toggles immediately and persists after restart.
- Mouse click-through toggles immediately and persists after restart.
- When click-through is enabled, the tray menu can disable it.
- Taskbar icon visibility toggles immediately and persists after restart.
- Start with Windows can be enabled and disabled.
- Startup registration failure is logged and shown to the user.
- Corrupt settings fall back to defaults without blocking startup.
- `Castorice.png` loads successfully from built-in resources.
- Missing or invalid built-in image produces a logged, user-visible error.

## Future Extensions

The following are intentionally outside MVP but should remain possible:

- Dragging the pet.
- Saving custom position.
- Size controls or automatic screen-based sizing.
- Multi-monitor placement management.
- Multiple character assets.
- Expression and idle animation systems.
- Settings window.
- External asset packs.
- Auto-update.
- Green-folder or installer-based distribution.

## Open Decisions

Release packaging is not decided. The MVP should not assume either a portable folder distribution or an installer. User data, settings, logs, and startup registration should follow Windows user-level conventions so either packaging route remains viable.
