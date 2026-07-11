# CastoPet Settings Window Design

## Goal

Add a dedicated settings window and move infrequently used tray-menu switches into it without creating a second settings implementation. Tray items and window controls must read and update the same setting definitions and command paths.

## Scope

The tray menu keeps:

- Show/restore
- Always on top
- Mouse click-through
- Settings
- Exit

The settings window displays all user-facing switches, including the two common tray settings, grouped as follows:

- Behavior: always on top, active movement
- Interaction: mouse click-through, push cursor, input reactive mode
- System: show taskbar icon, start with Windows

Skin selection, animation tuning, expression-wheel editing, and asset management remain out of scope.

## Shared Settings Model

Introduce a setting-definition catalog that describes each setting once. A definition contains a stable identifier, Chinese display label, group, current-value accessor, update command, and whether the item is shown directly in the tray.

`AppSettings` remains the persisted data model. `MenuCommandService` remains the only layer that applies settings to the pet window, updates Windows startup state, saves JSON, logs changes, and publishes the settings-changed event. The catalog delegates changes to this command layer instead of modifying `AppSettings` directly.

The tray and settings window both consume the catalog:

- `TrayService` filters definitions marked for direct tray display.
- `SettingsWindow` groups all definitions for the settings interface.
- Both refresh from the same `SettingsChanged` notification.

Moving a setting between the tray and settings window therefore changes only catalog presentation metadata.

## Window Lifecycle

`MenuCommandService` exposes a settings-window command and delegates window lifecycle to a single-instance `SettingsWindowService`. The first invocation creates and shows the window. Later invocations restore and activate the existing instance. Closing the settings window disposes only that window and does not exit CastoPet.

The settings window does not inherit the pet's always-on-top setting. It opens centered on the active screen with a stable compact size and is not resizable in the initial implementation.

## Interaction

Settings use immediate application:

1. The user changes a switch in either the tray or settings window.
2. The shared definition invokes the corresponding `MenuCommandService` command.
3. The command applies and persists the setting.
4. `SettingsChanged` causes both presentations to refresh.

There is no Apply button and no separate draft state. Startup-setting failure preserves the previous value and continues to show the existing warning.

## Visual Design

The window uses a mist-lavender and white palette with low saturation and restrained contrast:

- A cool near-white main surface replaces pure white.
- Very pale gray-lavender bands distinguish groups without card borders.
- Dusty muted violet is reserved for active switches, focus cues, and small accents.
- Primary text uses a softened charcoal; descriptions use a cool violet-gray.
- Dividers are faint and narrow so the interface does not look boxed in.
- Spacing is compact and consistent, with smaller gaps between related rows.

The font stack is `MiSans, Noto Sans SC, Microsoft YaHei UI`. MiSans is the preferred local face; the fallbacks preserve Chinese readability when the preferred font is unavailable. Headings use medium weight instead of large size or heavy bold styling.

The title bar visually blends into the main surface. Its close command uses a familiar icon with a subtle circular hover background rather than a prominent rectangular button. Setting switches use a slimmer track, softer thumb shadow, and low-saturation active color. Group labels, setting rows, and descriptions maintain clear hierarchy without strong borders or large decorative containers.

Controls have visible but quiet hover, focus, enabled, and disabled states. Text must remain readable at common Windows scaling levels, and the compact layout must not clip or overlap at 100% or 150% scaling.

## Components

- `SettingDefinition`: immutable presentation and command metadata for one setting.
- `SettingCatalog`: creates the ordered definitions and groups from `MenuCommandService`.
- `SettingsWindow`: WPF presentation that renders grouped definitions and refreshes their values.
- `SettingsWindowService`: maintains the single settings-window instance and activation behavior.
- `TrayService`: renders only catalog items marked for direct tray display and adds the Settings command.

These boundaries remain explicit so view lifecycle, presentation metadata, and setting side effects can be tested independently.

## Error Handling

- A failed startup-setting update leaves the switch unchanged and uses the existing warning.
- A failed JSON save follows existing logging behavior; the in-memory applied state remains active.
- Closing or reopening the settings window must not duplicate event subscriptions.
- Application shutdown closes the settings window without presenting confirmation.

## Testing

Automated tests cover:

- The catalog contains every current user-facing boolean setting exactly once.
- Only always-on-top and mouse click-through are marked for direct tray display.
- Each definition reads its value from the shared `AppSettings` instance.
- Invoking a definition routes through its command and produces the same state change as the tray entry.
- Groups and order remain stable.
- The settings window host reuses an open window and releases it after close where practical without fragile visual assertions.

Build verification covers Debug and Release configurations. A manual smoke test verifies synchronized tray/window state, immediate persistence after restart, single-instance behavior, startup failure behavior, and visual layout at common display scaling.

## Acceptance Criteria

- The tray contains only the approved common actions and a Settings entry.
- The settings window exposes all seven existing boolean settings in coherent groups.
- Changing a setting from either surface immediately updates the other surface.
- Existing settings persistence and runtime behavior remain unchanged.
- Repeated Settings commands never create multiple windows.
- The settings interface has a polished purple-to-white visual style with no overlapping or clipped content.
- Existing automated tests plus the new catalog tests pass in Debug and Release builds.
