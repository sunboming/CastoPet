# Data-Driven Two-Level Wheel and Shortcut Launcher Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a data-driven two-level radial wheel with expression and shortcut categories, plus drag-to-add shortcut management and safe local launching.

**Architecture:** Introduce pure catalog, pagination, interaction-state, persistence, drop parsing, and launch services under `Core`. Keep `PetWindow` responsible for WPF rendering and input forwarding, while `SettingsWindow` consumes the same `ShortcutService` collection used to build the wheel catalog.

**Tech Stack:** .NET 10, C#, WPF, `System.Text.Json`, Windows shell process launching, existing executable test harness in `tests/CastoPet.Tests`.

---

## File Structure

**Create:**

- `src/CastoPet/Core/WheelActionType.cs`: supported wheel action identifiers.
- `src/CastoPet/Core/WheelActionItem.cs`: generic visible or disabled wheel action.
- `src/CastoPet/Core/WheelCategory.cs`: first-level category definition.
- `src/CastoPet/Core/WheelCatalog.cs`: immutable category collection and layout constants.
- `src/CastoPet/Core/WheelCatalogService.cs`: combines current expressions and shortcuts.
- `src/CastoPet/Core/RadialWheelSelector.cs`: ring-aware sector geometry.
- `src/CastoPet/Core/RadialWheelController.cs`: dwell, second-level, pagination, selection, and cancellation state.
- `src/CastoPet/Core/ShortcutType.cs`: Program, File, Folder, WindowsShortcut, and WebUrl.
- `src/CastoPet/Core/ShortcutDefinition.cs`: persisted launcher entry.
- `src/CastoPet/Core/ShortcutService.cs`: atomic JSON persistence, validation, de-duplication, ordering, and change notification.
- `src/CastoPet/Core/ShortcutDropHandler.cs`: converts WPF-neutral dropped paths and URLs into candidates.
- `src/CastoPet/Core/ShortcutLauncher.cs`: safe shell launch behavior.
- `src/CastoPet/Core/ShortcutDropResult.cs`: batch-add result counts.

**Modify:**

- `src/CastoPet/Core/AppPaths.cs`: add shortcut directory and file paths.
- `src/CastoPet/PetWindow.xaml`: rename/generalize wheel canvas and enable drop events.
- `src/CastoPet/PetWindow.xaml.cs`: render the catalog, forward wheel state, execute actions, and accept drops.
- `src/CastoPet/SettingsWindow.xaml`: add the Shortcut Launcher settings section.
- `src/CastoPet/SettingsWindow.xaml.cs`: list, edit, reorder, add, and remove shortcuts.
- `src/CastoPet/App.xaml.cs`: compose shared shortcut and wheel services.
- `tests/CastoPet.Tests/Program.cs`: register and implement all service, controller, safety, and integration tests.

**Retire after migration:**

- `src/CastoPet/Core/ExpressionWheelItem.cs`
- `src/CastoPet/Core/ExpressionWheelSelector.cs`
- Expression-only layout responsibilities in `src/CastoPet/Core/ExpressionWheelCatalog.cs`; retain only expression duration if moving it would create unrelated churn, otherwise replace the class completely.

---

### Task 1: Generic Wheel Catalog

**Files:**
- Create: `src/CastoPet/Core/WheelActionType.cs`
- Create: `src/CastoPet/Core/WheelActionItem.cs`
- Create: `src/CastoPet/Core/WheelCategory.cs`
- Create: `src/CastoPet/Core/WheelCatalog.cs`
- Create: `src/CastoPet/Core/WheelCatalogService.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Register failing catalog tests**

Add tests asserting that a catalog built from two expressions and two shortcuts has exactly two ordered categories, preserves expression references, and exposes disabled empty-state content when there are no shortcuts.

```csharp
var catalog = WheelCatalogService.Create(expressions, shortcuts);
Assert.Equal("expressions", catalog.Categories[0].Id, "Expressions should remain first.");
Assert.Equal("shortcuts", catalog.Categories[1].Id, "Shortcuts should remain second.");
Assert.Equal(WheelActionType.Expression, catalog.Categories[0].Items[0].ActionType, "Expression actions should be typed.");
Assert.False(WheelCatalogService.Create(expressions, []).Categories[1].Items[0].IsEnabled, "Empty shortcut help should be disabled.");
```

- [ ] **Step 2: Run the tests and verify failure**

Run: `dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj -c Debug`

Expected: build failure because `WheelCatalogService` and generic wheel types do not exist.

- [ ] **Step 3: Implement the immutable catalog model**

Use records with stable IDs and opaque action references:

```csharp
public enum WheelActionType { Expression, Shortcut, PreviousPage, NextPage, Disabled }

public sealed record WheelActionItem(
    string Id,
    string DisplayName,
    WheelActionType ActionType,
    string? ActionReference,
    bool IsEnabled = true);

public sealed record WheelCategory(
    string Id,
    string DisplayName,
    IReadOnlyList<WheelActionItem> Items);
```

Define `WheelCatalog.MaxVisibleItemsPerRing = 8`, `CategoryDwellDelay = 120 ms`, existing hold delay, inner/first/second ring radii, and selected scale in one place. Build expression references from `PetExpressionDefinition.Id` and shortcut references from `ShortcutDefinition.Id`.

- [ ] **Step 4: Run tests and commit**

Run: `dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj -c Debug`

Expected: all catalog tests pass.

```powershell
git add src/CastoPet/Core/Wheel*.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: add data-driven wheel catalog"
```

---

### Task 2: Ring Selection, Pagination, and Dwell State

**Files:**
- Create: `src/CastoPet/Core/RadialWheelSelector.cs`
- Create: `src/CastoPet/Core/RadialWheelController.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add failing geometry and controller tests**

Cover center, first ring, second ring, outside cancellation, 119/120 ms dwell boundaries, returning to center, and pagination with 9, 15, and 17 actions. Assert every rendered page contains at most eight sectors and that page controls map to controller actions rather than persisted actions.

```csharp
controller.Open(now);
controller.UpdatePointer(firstCategoryPoint, now);
Assert.False(controller.IsSecondLevelOpen, "Second level must not open immediately.");
controller.UpdatePointer(firstCategoryPoint, now + TimeSpan.FromMilliseconds(120));
Assert.True(controller.IsSecondLevelOpen, "Stable category dwell should open level two.");
Assert.True(controller.VisibleSecondLevelItems.Count <= 8, "A page cannot exceed eight sectors.");
```

- [ ] **Step 2: Verify the tests fail**

Run the Debug test harness and expect missing controller/selector types.

- [ ] **Step 3: Implement pure geometry and state transitions**

`RadialWheelSelector.GetSelection` returns a ring and sector index from pointer coordinates. `RadialWheelController` accepts catalog snapshots and timestamps so dwell logic is deterministic without WPF timers. It exposes selected category, second-level visibility, current page, visible items, and one release result.

Use an explicit result model:

```csharp
public sealed record WheelReleaseResult(
    WheelReleaseKind Kind,
    WheelActionItem? Action);
```

Previous/next release results update the current page and keep the wheel open; enabled action releases return Execute; disabled, center, and outside releases return Cancel.

- [ ] **Step 4: Run tests and commit**

Run the Debug test harness; expect all tests to pass.

```powershell
git add src/CastoPet/Core/RadialWheel*.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: add two-level wheel interaction state"
```

---

### Task 3: Shortcut Model and Atomic Persistence

**Files:**
- Create: `src/CastoPet/Core/ShortcutType.cs`
- Create: `src/CastoPet/Core/ShortcutDefinition.cs`
- Create: `src/CastoPet/Core/ShortcutService.cs`
- Modify: `src/CastoPet/Core/AppPaths.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add failing persistence tests**

Use a temporary `AppPaths` root. Test empty load, round trip, case-insensitive path duplicates, normalized URL duplicates, `.lnk` identity by link path, ordering, rename/delete, the 128-item limit, malformed-file backup, malformed-entry isolation, and `Changed` notification after successful writes.

```csharp
var first = service.TryAdd(candidate);
var duplicate = service.TryAdd(candidate with { Target = candidate.Target.ToUpperInvariant() });
Assert.True(first.Added, "First normalized path should be added.");
Assert.False(duplicate.Added, "Equivalent Windows paths should be duplicates.");
Assert.True(File.Exists(paths.ShortcutsFile), "Shortcut data should be persisted.");
```

- [ ] **Step 2: Verify tests fail**

Run the Debug test harness and expect missing shortcut model/service members.

- [ ] **Step 3: Implement paths and persistence**

Add:

```csharp
ShortcutsDirectory = Path.Combine(DataDirectory, "Shortcuts");
ShortcutsFile = Path.Combine(ShortcutsDirectory, "shortcuts.json");
```

Write JSON to `shortcuts.json.tmp`, flush/close it, then use `File.Move(temp, destination, true)`. Before recovering malformed JSON, copy it to `shortcuts.invalid-yyyyMMdd-HHmmss.json`. Keep all file-system failures contained and logged. Store arguments as a separate string field; never concatenate target and arguments into a command.

- [ ] **Step 4: Run tests and commit**

Run Debug tests and expect all persistence tests to pass.

```powershell
git add src/CastoPet/Core/AppPaths.cs src/CastoPet/Core/Shortcut*.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: persist shortcut launcher entries"
```

---

### Task 4: Drop Recognition and Batch Addition

**Files:**
- Create: `src/CastoPet/Core/ShortcutDropHandler.cs`
- Create: `src/CastoPet/Core/ShortcutDropResult.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add failing parser tests**

Create temporary `.exe`, ordinary file, directory, `.lnk`, and `.url` fixtures. Test direct HTTP/HTTPS text, unsupported schemes, arbitrary command text, mixed batches, and duplicates.

```csharp
var result = handler.AddDroppedItems([exePath, folderPath], []);
Assert.Equal(2, result.AddedCount, "Both supported paths should be added.");
Assert.Equal(0, result.FailedCount, "Supported paths should not fail.");
```

- [ ] **Step 2: Verify tests fail**

Run Debug tests and expect `ShortcutDropHandler` to be missing.

- [ ] **Step 3: Implement WPF-neutral drop parsing**

Accept `IReadOnlyList<string> paths` and `IReadOnlyList<string> textValues`, keeping `IDataObject` out of Core. Classify directories before extensions, classify `.exe` as Program, `.lnk` as WindowsShortcut, parse `.url` with an INI-style `URL=` line, and accept only absolute HTTP/HTTPS URIs from text.

Return:

```csharp
public sealed record ShortcutDropResult(
    int AddedCount,
    int DuplicateCount,
    int UnsupportedCount,
    int FailedCount);
```

- [ ] **Step 4: Run tests and commit**

Run Debug tests and expect parser/batch tests to pass.

```powershell
git add src/CastoPet/Core/ShortcutDrop*.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: add drag-to-register shortcuts"
```

---

### Task 5: Safe Shortcut Launching

**Files:**
- Create: `src/CastoPet/Core/ShortcutLauncher.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add failing launch-plan tests**

Do not launch real applications in unit tests. Make `ShortcutLauncher.CreateStartInfo` independently testable. Assert `UseShellExecute = true`, target remains in `FileName`, arguments remain separate, working directory is optional, missing targets are rejected, and non-HTTP URL schemes are rejected.

```csharp
var info = launcher.CreateStartInfo(program);
Assert.Equal(program.Target, info.FileName, "Target must remain a structured filename.");
Assert.Equal(program.Arguments, info.Arguments, "Arguments must remain separate.");
Assert.True(info.UseShellExecute, "Windows shell behavior should open associated targets.");
```

- [ ] **Step 2: Verify tests fail**

Run Debug tests and expect the launcher type to be missing.

- [ ] **Step 3: Implement validation and launching**

Expose `ShortcutLaunchResult Launch(ShortcutDefinition definition)`. Validate existence for all file-system types and HTTP/HTTPS for URLs, then call `Process.Start(startInfo)` inside a contained try/catch. Return a failure message and log exceptions; do not request elevation and never set `Verb = "runas"`.

- [ ] **Step 4: Run tests and commit**

Run Debug tests and expect all launch-plan tests to pass.

```powershell
git add src/CastoPet/Core/ShortcutLauncher.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: launch shortcuts safely"
```

---

### Task 6: Migrate PetWindow to the Two-Level Wheel

**Files:**
- Modify: `src/CastoPet/PetWindow.xaml`
- Modify: `src/CastoPet/PetWindow.xaml.cs`
- Delete: `src/CastoPet/Core/ExpressionWheelItem.cs`
- Delete: `src/CastoPet/Core/ExpressionWheelSelector.cs`
- Modify or delete: `src/CastoPet/Core/ExpressionWheelCatalog.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add failing source-integration regression tests**

Assert `PetWindow.xaml` contains a generic radial overlay with two ring surfaces and drop handlers. Assert `PetWindow.xaml.cs` uses `RadialWheelController`, dispatches expression references through the existing asset map, dispatches shortcut references through `ShortcutLauncher`, handles Escape, and no longer references `ExpressionWheelSelector`.

- [ ] **Step 2: Verify integration tests fail**

Run Debug tests and expect the new XAML/source assertions to fail.

- [ ] **Step 3: Generalize construction and rendering**

Change the constructor to receive shared services:

```csharp
public PetWindow(
    AssetService assets,
    LoggingService logger,
    WheelCatalogService wheelCatalogs,
    ShortcutDropHandler shortcutDrops,
    ShortcutLauncher shortcutLauncher)
```

Keep expression assets keyed by expression ID. Build first- and second-level visuals from controller snapshots. Use one sector-geometry helper parameterized by inner and outer radius. Preserve translucent purple fills, dividers, labels, open animation, and selected scaling.

- [ ] **Step 4: Wire pointer lifecycle**

On right-button hold, open the controller after the existing hold delay. On pointer movement, pass coordinates and `DateTimeOffset.UtcNow`, rebuild second-level visuals only when category/page content changes, and update selection styling otherwise. On release, execute the returned action or page transition. On Escape/outside cancellation, close both rings.

- [ ] **Step 5: Preserve expression behavior**

Resolve `ExpressionAction.ActionReference` to `PetExpressionAsset.Definition.Id` and invoke the existing temporary-expression transition path unchanged. Keep `ExpressionDuration` at two seconds.

- [ ] **Step 6: Wire WPF drop data**

Set `AllowDrop="True"` on the pet hit surface. In `DragOver`, accept only file-drop or string/URL data. In `Drop`, extract WPF data into neutral path/text lists, call `ShortcutDropHandler`, and trigger a short success, duplicate, partial, unsupported, or confused response without moving source files.

- [ ] **Step 7: Run tests and commit**

Run Debug tests and expect all old expression and new wheel tests to pass.

```powershell
git add src/CastoPet/PetWindow.xaml src/CastoPet/PetWindow.xaml.cs src/CastoPet/Core tests/CastoPet.Tests/Program.cs
git commit -m "feat: render two-level radial actions"
```

---

### Task 7: Compose Shared Services and Live Catalog Refresh

**Files:**
- Modify: `src/CastoPet/App.xaml.cs`
- Modify: `src/CastoPet/Core/WheelCatalogService.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add failing composition tests**

Assert application startup constructs one `ShortcutService`, passes it to both settings and catalog composition, and subscribes catalog refresh to `ShortcutService.Changed`. Add a service test showing a newly added shortcut appears in the next catalog snapshot without restarting.

- [ ] **Step 2: Verify tests fail**

Run Debug tests and expect composition assertions to fail.

- [ ] **Step 3: Compose services in startup**

Create and load the shortcut service after logging and settings initialization. Build `WheelCatalogService` with `skin.Expressions` and shortcut service. Build the drop handler and launcher once. Pass these instances to `PetWindow` and `SettingsWindow` so both surfaces operate on the same collection.

- [ ] **Step 4: Run tests and commit**

Run Debug tests and expect live-refresh tests to pass.

```powershell
git add src/CastoPet/App.xaml.cs src/CastoPet/Core/WheelCatalogService.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: compose shortcut and wheel services"
```

---

### Task 8: Shortcut Management Settings Page

**Files:**
- Modify: `src/CastoPet/SettingsWindow.xaml`
- Modify: `src/CastoPet/SettingsWindow.xaml.cs`
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add failing settings integration tests**

Assert the settings window exposes a Shortcut Launcher navigation item/list, add URL command, rename, move-up, move-down, delete, arguments, and working-directory controls. Add service-backed tests for reorder semantics and validity display.

- [ ] **Step 2: Verify tests fail**

Run Debug tests and expect settings structure assertions to fail.

- [ ] **Step 3: Add the shortcut settings surface**

Use an un-nested list layout consistent with the current lavender/white visual system. Each row shows name, type, target, and missing-target state. Use icon buttons with tooltips for move up, move down, and delete. Provide editing fields for selected item name, program arguments, and working directory, plus a small add-URL dialog or inline input.

Do not duplicate state in the window. Every mutation calls `ShortcutService`, then refreshes from `GetAll()`.

- [ ] **Step 4: Handle validation UX**

Disable program-only fields for non-program entries. Reject non-HTTP/HTTPS manual URLs inline. Confirm deletion only when the user explicitly presses delete; no automatic removal occurs for missing targets.

- [ ] **Step 5: Run tests and commit**

Run Debug tests and expect settings tests to pass.

```powershell
git add src/CastoPet/SettingsWindow.xaml src/CastoPet/SettingsWindow.xaml.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: manage launcher shortcuts in settings"
```

---

### Task 9: Full Verification and Documentation

**Files:**
- Modify: `docs/local-packaging.md` only if the new local data location needs documentation.
- Test: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Run the complete test harness in both configurations**

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj -c Debug
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj -c Release
```

Expected: every test prints `PASS`; neither process returns a non-zero exit code.

- [ ] **Step 2: Build both configurations cleanly**

```powershell
dotnet build CastoPet.sln -c Debug --no-restore
dotnet build CastoPet.sln -c Release --no-restore
```

Expected: zero warnings and zero errors.

- [ ] **Step 3: Perform manual interaction verification**

Verify all of the following in a Debug run:

- Right-button hold opens the first ring.
- A category opens level two only after a perceptible short dwell.
- Returning to center closes only level two.
- Escape and outer exit cancel.
- Existing eight expressions still play with transitions.
- More than eight shortcuts paginate without tiny sectors.
- Dropping each supported type adds it without moving the source.
- Duplicate and unsupported drops show distinct brief feedback.
- Settings edits and ordering update the wheel immediately.
- Missing targets are marked and fail without a crash.

- [ ] **Step 4: Check repository hygiene**

```powershell
git diff --check
git status --short
rg -n "ExpressionWheelSelector|new ExpressionWheelItem" src tests
```

Expected: no whitespace errors; no obsolete selector/item references; only intentional changes remain.

- [ ] **Step 5: Commit final verification or documentation changes**

```powershell
git add docs tests/CastoPet.Tests/Program.cs
git commit -m "test: verify shortcut wheel workflow"
```

Skip this commit when the verification task produces no file changes.
