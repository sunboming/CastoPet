# CastoPet Settings Window Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a polished purple-and-white settings window backed by the same setting definitions and commands as the tray and pet context menus.

**Architecture:** A `SettingCatalog` provides one ordered definition for every boolean preference, including its group, direct-menu visibility, value accessor, and command binding. `TrayService`, `PetWindow`, and `SettingsWindow` render different views of that catalog while `MenuCommandService` remains the only side-effect and persistence layer; `SettingsWindowService` owns the single window instance.

**Tech Stack:** .NET 10, WPF/XAML, Windows Forms `NotifyIcon`, existing console-style C# test harness

---

## File Structure

- Create `src/CastoPet/Core/SettingDefinition.cs`: identifiers, groups, labels, descriptions, visibility, value accessor, and toggle action.
- Create `src/CastoPet/Core/SettingCatalog.cs`: ordered shared catalog creation from `MenuCommandService`.
- Create `src/CastoPet/Core/SettingsWindowService.cs`: single-instance window lifecycle.
- Create `src/CastoPet/SettingsWindow.xaml`: compact purple-and-white interface and switch styling.
- Create `src/CastoPet/SettingsWindow.xaml.cs`: catalog rendering and synchronization.
- Modify `src/CastoPet/Core/MenuCommandService.cs`: expose settings-window opening.
- Modify `src/CastoPet/Core/TrayService.cs`: render direct-menu definitions and add Settings.
- Modify `src/CastoPet/PetWindow.xaml.cs`: render the same direct-menu definitions and add Settings.
- Modify `src/CastoPet/App.xaml.cs`: compose and dispose the settings-window service.
- Modify `tests/CastoPet.Tests/Program.cs`: catalog, lifecycle, layout, and migration tests.

### Task 1: Define the shared setting catalog

**Files:**
- Create: `src/CastoPet/Core/SettingDefinition.cs`
- Create: `src/CastoPet/Core/SettingCatalog.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add failing catalog tests**

Register tests that construct a catalog from an `AppSettings` instance and toggle delegates, then assert stable IDs and order:

```csharp
var expected = new[]
{
    "topmost", "active-movement", "click-through", "push-cursor",
    "input-reactive-mode", "show-in-taskbar", "start-with-windows",
};
Assert.SequenceEqual(expected, definitions.Select(item => item.Id),
    "The settings catalog should contain every boolean setting exactly once.");
Assert.SequenceEqual(new[] { "topmost", "click-through" },
    definitions.Where(item => item.ShowInDirectMenu).Select(item => item.Id),
    "Only common settings should stay in direct menus.");
```

Add one test that mutates `settings.ActiveMovement` after catalog creation and verifies `GetValue()` reads the new value rather than cached state.

- [ ] **Step 2: Run the tests and verify RED**

Run `dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj`.

Expected: compilation fails because `SettingDefinition` and `SettingCatalog` do not exist.

- [ ] **Step 3: Implement the minimal catalog types**

```csharp
public enum SettingGroup { Behavior, Interaction, System }

public sealed record SettingDefinition(
    string Id,
    string Label,
    string Description,
    SettingGroup Group,
    bool ShowInDirectMenu,
    Func<bool> GetValue,
    Action Toggle);
```

Create all seven definitions in `SettingCatalog.Create(MenuCommandService commands)`. Keep Chinese labels centralized there. Use Behavior, Interaction, System order and mark only `topmost` and `click-through` for direct menus.

- [ ] **Step 4: Run the tests and verify GREEN**

Run the test project again. Expected: all catalog and existing tests pass.

- [ ] **Step 5: Commit the catalog**

Stage only the two core files and test file, inspect `git diff --cached --name-only`, then commit with `feat: add shared settings catalog`.

### Task 2: Add settings-window command and single-instance lifecycle

**Files:**
- Create: `src/CastoPet/Core/SettingsWindowService.cs`
- Modify: `src/CastoPet/Core/MenuCommandService.cs`
- Modify: `src/CastoPet/App.xaml.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add a failing lifecycle test**

Define an `ISettingsWindow` adapter with `Show`, `Activate`, `IsVisible`, and `Closed`. Test that opening twice calls the factory once, while closing clears the retained instance so a later open calls the factory a second time.

- [ ] **Step 2: Run the tests and verify RED**

Expected: compilation fails because the window host abstraction is absent.

- [ ] **Step 3: Implement lifecycle and command wiring**

`SettingsWindowService` receives a factory, keeps one window reference, subscribes to `Closed`, and exposes `ShowOrActivate()`. Add `ShowSettings()` to `MenuCommandService`, delegating to this service. Inject it from `App.OnStartup` and close it during shutdown.

Keep all setting toggle methods and `ApplyAndSave` unchanged.

- [ ] **Step 4: Run the tests and verify GREEN**

Run the full test project. Expected: all tests pass.

- [ ] **Step 5: Commit lifecycle wiring**

Inspect staged files before committing with `feat: add settings window lifecycle`.

### Task 3: Build the settings window and purple-white theme

**Files:**
- Create: `src/CastoPet/SettingsWindow.xaml`
- Create: `src/CastoPet/SettingsWindow.xaml.cs`
- Modify: `src/CastoPet/App.xaml.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add failing structural tests**

Read `SettingsWindow.xaml` as text and assert intentional anchors without pixel snapshots:

```csharp
Assert.Contains(xaml, "SettingsItemsHost", "The catalog host should be named.");
Assert.Contains(xaml, "#6F4AA8", "Active controls should use muted purple.");
Assert.Contains(xaml, "CloseButton", "The title bar should expose a close button.");
```

- [ ] **Step 2: Run the tests and verify RED**

Expected: failure because `SettingsWindow.xaml` does not exist.

- [ ] **Step 3: Implement the XAML shell**

Create a fixed window around 520x600 with a white surface, light lavender section bands, dark neutral text, and muted purple active states. Use a custom title row with `设置` and an icon-only close button. Define a reusable toggle style with stable 42x24 dimensions, keyboard focus cues, and disabled state.

Avoid nested cards. Use full-width groups separated by thin lavender rules and rows with label/description on the left and switch on the right.

- [ ] **Step 4: Render definitions and synchronize state**

Group definitions by `SettingGroup`, create rows from the catalog, and store toggles by definition ID. A toggle calls `definition.Toggle()`. `MenuCommandService.SettingsChanged` refreshes every control through `GetValue()` while suppressing recursive handlers. Unsubscribe when closed.

- [ ] **Step 5: Run tests and build Debug**

Run `dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj`, then `dotnet build src\CastoPet\CastoPet.csproj -c Debug`.

Expected: all tests pass; build has zero errors and zero warnings.

- [ ] **Step 6: Commit the settings UI**

Inspect staged paths and commit with `feat: add unified settings window`.

### Task 4: Migrate tray and pet context menus

**Files:**
- Modify: `src/CastoPet/Core/TrayService.cs`
- Modify: `src/CastoPet/PetWindow.xaml.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Replace obsolete menu-text tests with failing presentation tests**

Assert that the direct-menu projection returns only `topmost` and `click-through`, and that `TrayService.SettingsText` equals `设置`. Add a source-level regression assertion that the tray constructor and `AttachContextMenu` no longer manually add active movement, push cursor, input reactive mode, taskbar visibility, or startup entries.

- [ ] **Step 2: Run the tests and verify RED**

Expected: tests fail because low-frequency entries are still rendered and no Settings label exists.

- [ ] **Step 3: Render both menus from the catalog**

Update `TrayService` to create checked items only from definitions where `ShowInDirectMenu` is true, followed by Settings. Refresh checks by definition ID and `GetValue()`.

Update `PetWindow.AttachContextMenu` identically. Preserve Show/restore, separators, and Exit. Both menus call `commands.ShowSettings`.

- [ ] **Step 4: Run tests and verify GREEN**

Run the full test project. Expected: all tests pass.

- [ ] **Step 5: Commit menu migration**

Inspect staged paths and commit with `refactor: share settings menu definitions`.

### Task 5: End-to-end verification and polish

**Files:**
- Modify if required: `src/CastoPet/SettingsWindow.xaml`
- Modify if required: `src/CastoPet/SettingsWindow.xaml.cs`
- Modify if required: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Run the full automated suite**

Run the test project in Debug and Release. Expected: every test reports `PASS` and both processes return exit code 0.

- [ ] **Step 2: Clean and build both configurations**

```powershell
dotnet clean src\CastoPet\CastoPet.csproj -c Debug
dotnet clean src\CastoPet\CastoPet.csproj -c Release
dotnet build src\CastoPet\CastoPet.csproj -c Debug
dotnet build src\CastoPet\CastoPet.csproj -c Release
```

Expected: zero errors and zero warnings in both builds, with refreshed executables in the default `bin` directories.

- [ ] **Step 3: Perform the desktop smoke test**

Launch Debug and verify both right-click menus show only the two common switches plus Settings and existing commands; Settings stays single-instance; all seven switches appear in three groups; shared checks update immediately; settings survive restart; closing Settings leaves CastoPet running; and 100%/150% scaling has no clipping or overlap.

- [ ] **Step 4: Apply final visual adjustments**

Only after the smoke test, tune spacing, typography, lavender separators, hover feedback, and focus indicators. Do not change catalog behavior during this step.

- [ ] **Step 5: Re-run verification and commit**

Repeat tests and builds after polish. Inspect staged paths and commit with `style: polish settings window` only when files changed.

### Task 6: Redesign the settings window with a mist-lavender theme

**Files:**
- Modify: `src/CastoPet/SettingsWindow.xaml`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Update the visual contract test and verify RED**

Replace the original saturated-purple assertion with structural assertions for the approved redesign:

```csharp
Assert.Contains(xaml, "MiSans, Noto Sans SC, Microsoft YaHei UI", "The window should use the approved Chinese font stack.");
Assert.Contains(xaml, "#8C7AA5", "Active controls should use dusty mist violet.");
Assert.Contains(xaml, "#FAF9FC", "The main surface should use cool near-white.");
Assert.False(xaml.Contains("#6F4AA8", StringComparison.Ordinal), "The old saturated purple should be removed.");
```

Run `dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj -c Debug`.

Expected: the visual structure test fails because the existing XAML still uses `Microsoft YaHei UI`, `#6F4AA8`, and pure white.

- [ ] **Step 2: Implement the mist-lavender palette and font stack**

Update `SettingsWindow.xaml` to use the exact font stack and a restrained palette centered on `#FAF9FC` for the surface, `#F4F1F7` for group bands, `#E7E0EC` for dividers, `#8C7AA5` for active accents, `#35313A` for primary text, and `#7A7480` for descriptions.

Reduce the switch to a 40x22 track with a 16px thumb and softer shadow. Blend the title area into the surface, use medium-weight headings, reduce row and group spacing, and give the icon-only close button a subtle circular hover state. Keep all existing names, event handlers, and catalog behavior unchanged.

- [ ] **Step 3: Run the visual contract and full tests**

Run `dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj -c Debug`.

Expected: every test reports `PASS` and the process exits 0.

- [ ] **Step 4: Clean and rebuild Debug and Release**

Run clean and build for both configurations. Expected: zero warnings and zero errors; both EXE timestamps refresh.

- [ ] **Step 5: Launch Debug for user visual review**

Start the Debug executable. The user opens Settings from the pet or tray menu and checks the redesigned palette, font rendering, compact spacing, switch appearance, and close-button hover state. Any further visual adjustment remains isolated to XAML and the visual contract test.
