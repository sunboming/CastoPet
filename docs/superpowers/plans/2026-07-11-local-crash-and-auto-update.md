# CastoPet Local Crash Recording and Auto Update Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add sanitized local crash reports, once-per-day update checks, manual update UI, and Velopack-based local installer packaging without publishing remote artifacts.

**Architecture:** Pure policy and formatting code stays independent from WPF and Velopack for deterministic tests. `CrashReportService` owns local report files, `UpdateCheckPolicy` owns scheduling, and an `IUpdateService` adapter isolates Velopack; WPF composes these services and presents status without changing the existing boolean setting catalog.

**Tech Stack:** .NET 10 WPF, C# 14, Velopack 1.2.0, vpk 1.2.0, PowerShell packaging script, existing console-style test harness

---

## File Structure

- Modify `src/CastoPet/Core/AppPaths.cs`: add the crash-report directory.
- Modify `src/CastoPet/Core/AppSettings.cs`: persist acknowledged crash and daily update attempt.
- Create `src/CastoPet/Core/CrashReportFormatter.cs`: metadata formatting and path sanitization.
- Create `src/CastoPet/Core/CrashReportService.cs`: atomic report writing, discovery, acknowledgement support, and folder opening.
- Create `src/CastoPet/Core/UpdateCheckPolicy.cs`: daily automatic/manual decision logic.
- Create `src/CastoPet/Core/IUpdateService.cs`: UI-facing update contract and result types.
- Create `src/CastoPet/Core/VelopackUpdateService.cs`: installed-build detection and GitHub source adapter.
- Create `src/CastoPet/Core/UpdateCoordinator.cs`: serialization, timeout, status, and daily-attempt persistence.
- Create `src/CastoPet/CrashNotificationWindow.xaml`: local-only next-start crash notification.
- Create `src/CastoPet/CrashNotificationWindow.xaml.cs`: acknowledgement and open-folder actions.
- Modify `src/CastoPet/App.xaml.cs`: early crash handlers, notification, delayed daily update check, and Velopack startup hook.
- Modify `src/CastoPet/SettingsWindow.xaml`: system actions and update status section.
- Modify `src/CastoPet/SettingsWindow.xaml.cs`: open-folder and manual-update commands.
- Modify `src/CastoPet/CastoPet.csproj`: explicit version, runtime packaging metadata, and Velopack 1.2.0 reference.
- Create `tools/package-local.ps1`: clean, publish, validate semantic version, and run vpk without upload.
- Create `docs/local-packaging.md`: unsigned installer and SmartScreen expectations.
- Modify `tests/CastoPet.Tests/Program.cs`: all new policy, formatting, persistence, and project-structure tests.

### Task 1: Add crash paths and persisted state

**Files:**
- Modify: `src/CastoPet/Core/AppPaths.cs`
- Modify: `src/CastoPet/Core/AppSettings.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write failing tests**

Assert `AppPaths.CrashesDirectory` equals `Path.Combine(DataDirectory, "Crashes")`. Extend JSON round-trip tests for `LastAcknowledgedCrashId` and a nullable `LastAutomaticUpdateCheckDate` string in ISO `yyyy-MM-dd` form. Extend `Clone()` assertions.

- [ ] **Step 2: Verify RED**

Run `dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj -c Debug`.

Expected: compile failure because the new properties do not exist.

- [ ] **Step 3: Implement minimal path and settings properties**

```csharp
public string CrashesDirectory { get; }
public string? LastAcknowledgedCrashId { get; set; }
public string? LastAutomaticUpdateCheckDate { get; set; }
```

Initialize `CrashesDirectory` from `DataDirectory` and include both values in `Clone()`.

- [ ] **Step 4: Verify GREEN**

Run all tests and expect exit code 0.

### Task 2: Format, sanitize, and store crash reports

**Files:**
- Create: `src/CastoPet/Core/CrashReportFormatter.cs`
- Create: `src/CastoPet/Core/CrashReportService.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write failing formatter tests**

Use a nested exception containing a test user profile path. Assert output contains UTC timestamp, version, OS, architecture, outer and inner exception types, but does not contain the raw username or profile path. Assert a long log tail is bounded to the configured final line count.

- [ ] **Step 2: Verify RED**

Expected: compile failure because crash formatter types are absent.

- [ ] **Step 3: Implement formatter and sanitizer**

Create a `CrashReportContext` record and a pure `Format(context, exception, logLines)` method. Replace the current profile directory with `%USERPROFILE%`, replace the username case-insensitively, walk inner exceptions, and include only the last 80 log lines.

- [ ] **Step 4: Write failing report-service tests**

In a temporary `AppPaths`, call `TryWriteReport`, assert one UTF-8 file exists under `Crashes`, its ID is stable from its filename, and `GetLatestUnacknowledged` respects `LastAcknowledgedCrashId`. Add a test with an invalid crash-directory parent proving `TryWriteReport` returns false rather than throwing.

- [ ] **Step 5: Implement atomic storage**

Write to a `.tmp` file in the crash directory, then move to `crash-<UTC timestamp>-<guid>.txt`. Catch all file-system failures, log through `LoggingService`, and never rethrow from the crash path.

- [ ] **Step 6: Verify GREEN**

Run all tests and expect every crash test to pass.

### Task 3: Register crash handlers and show one-time local notification

**Files:**
- Create: `src/CastoPet/CrashNotificationWindow.xaml`
- Create: `src/CastoPet/CrashNotificationWindow.xaml.cs`
- Modify: `src/CastoPet/App.xaml.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write failing source-structure tests**

Assert `App.xaml.cs` registers `DispatcherUnhandledException`, `AppDomain.CurrentDomain.UnhandledException`, and `TaskScheduler.UnobservedTaskException`. Assert notification XAML contains `打开日志目录` and `忽略`.

- [ ] **Step 2: Verify RED**

Expected: assertions fail because handlers and notification do not exist.

- [ ] **Step 3: Register handlers before normal service startup**

Create `AppPaths`, `LoggingService`, and `CrashReportService` before other startup work. Each handler calls the same idempotent recorder guarded by `Interlocked.Exchange`. Do not mark fatal dispatcher exceptions handled. Call `e.SetObserved()` only after successfully recording an unobserved task exception.

- [ ] **Step 4: Implement the compact notification**

Match the mist-lavender settings theme. Open-folder launches Explorer with the crash directory as an argument. Both actions persist acknowledgement; closing the dialog behaves like Ignore so it does not appear every launch.

- [ ] **Step 5: Verify GREEN**

Run tests and build Debug with zero errors and warnings.

### Task 4: Implement update policy and coordinator without network dependencies

**Files:**
- Create: `src/CastoPet/Core/UpdateCheckPolicy.cs`
- Create: `src/CastoPet/Core/IUpdateService.cs`
- Create: `src/CastoPet/Core/UpdateCoordinator.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write failing daily-policy tests**

Assert no automatic check occurs when stored local date equals today, a check occurs when absent or older, and manual checks always run. Assert recording an attempt uses `yyyy-MM-dd` invariant formatting.

- [ ] **Step 2: Verify RED**

Expected: compile failure because update policy is absent.

- [ ] **Step 3: Implement policy**

Keep decisions pure using `DateOnly today` and persisted strings. Invalid stored values count as no previous check.

- [ ] **Step 4: Write failing coordinator tests**

Use a fake `IUpdateService` to prove: non-installed builds return `DevelopmentBuild`; network exceptions return `Failed`; manual retry bypasses the daily gate; an in-flight operation prevents a second request; and automatic attempts persist the date before awaiting the network call.

- [ ] **Step 5: Implement coordinator**

Use `SemaphoreSlim(1, 1)` for serialization and an eight-second linked cancellation token. Map exceptions to status records without showing dialogs in the core layer. Save the settings immediately after recording an automatic attempt.

- [ ] **Step 6: Verify GREEN**

Run all tests and expect exit code 0.

### Task 5: Add Velopack and the GitHub update adapter

**Files:**
- Modify: `src/CastoPet/CastoPet.csproj`
- Create: `src/CastoPet/Core/VelopackUpdateService.cs`
- Modify: `src/CastoPet/App.xaml.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Add failing project-contract tests**

Assert the project defines `<Version>0.1.0</Version>` and `<PackageReference Include="Velopack" Version="1.2.0" />`. Assert the repository URL constant is `https://github.com/sunboming/CastoPet-Releases`.

- [ ] **Step 2: Verify RED**

Expected: project-contract tests fail.

- [ ] **Step 3: Add the package and explicit version**

Add Velopack 1.2.0 and project version metadata. Restore from NuGet. Call `VelopackApp.Build().Run()` at application entry before normal WPF startup work, following Velopack requirements.

- [ ] **Step 4: Implement the adapter**

Construct `GithubSource` without a token and with prereleases disabled. Report development-build status when Velopack does not identify an installed package. Map release version and notes to the local contract, delegate download progress, and call apply-and-restart only after successful download.

- [ ] **Step 5: Verify GREEN**

Run tests plus Debug and Release builds. Expect zero errors and warnings.

### Task 6: Add update and crash actions to Settings

**Files:**
- Modify: `src/CastoPet/SettingsWindow.xaml`
- Modify: `src/CastoPet/SettingsWindow.xaml.cs`
- Modify: `src/CastoPet/App.xaml.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write failing visual-structure tests**

Assert Settings XAML exposes `OpenCrashReportsButton`, `CheckForUpdatesButton`, `UpdateStatusText`, and `CurrentVersionText`, while preserving the mist-lavender theme contract.

- [ ] **Step 2: Verify RED**

Expected: structure test fails for missing controls.

- [ ] **Step 3: Add compact system-action rows**

Below boolean groups, add an unframed Update section with current version, last status/time, icon-text commands for opening reports and checking updates, and no nested cards. Disable the update button while checking/downloading.

- [ ] **Step 4: Wire status and prompts**

Inject `CrashReportService` and `UpdateCoordinator` into Settings. Manual check updates status. When an update exists, display version and release notes with Update now/Later. Download progress updates the same status surface.

- [ ] **Step 5: Schedule the daily check**

After normal pet startup, delay ten seconds and invoke the coordinator only when policy allows. Automatic no-update/failure results stay silent; available updates use the same prompt.

- [ ] **Step 6: Verify GREEN**

Run all tests and build both configurations.

### Task 7: Create local unsigned installer packaging

**Files:**
- Create: `tools/package-local.ps1`
- Create: `docs/local-packaging.md`
- Modify: `.gitignore`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write failing packaging-contract tests**

Assert the script contains `dotnet publish`, `--self-contained true`, `win-x64`, `vpk pack`, the supplied version parameter, and no `vpk upload`, `gh release`, or network publication command. Assert local artifact directories are ignored by Git.

- [ ] **Step 2: Verify RED**

Expected: packaging-contract tests fail because the script is absent.

- [ ] **Step 3: Implement the packaging script**

Accept a mandatory semantic version defaulting to the project version, validate it, clean only repository-owned `artifacts/local-package`, publish self-contained Windows x64 output, install or restore the pinned vpk 1.2.0 local tool, and run:

```powershell
vpk pack --packId CastoPet --packVersion $Version --packDir $PublishDir --mainExe CastoPet.exe --outputDir $PackageDir
```

Do not include upload commands or credentials.

- [ ] **Step 4: Document unsigned local installation**

Document output paths, SmartScreen unknown-publisher behavior, install/uninstall checks, and the explicit statement that packaging does not publish.

- [ ] **Step 5: Verify GREEN**

Run all tests, then execute `powershell -ExecutionPolicy Bypass -File tools\package-local.ps1 -Version 0.1.0`.

Expected: an unsigned Setup executable and Velopack update assets exist under `artifacts/local-package/packages`.

### Task 8: Final verification

**Files:**
- Modify only files required by verified defects.

- [ ] **Step 1: Run Debug and Release tests**

Run the test project in both configurations. Expected: every test reports `PASS` and exits 0.

- [ ] **Step 2: Clean and rebuild Debug and Release**

Expected: both configurations have zero errors and zero warnings with refreshed EXEs.

- [ ] **Step 3: Perform local installer smoke test**

Install the unsigned local Setup, accept the expected SmartScreen warning if shown, launch CastoPet, verify Settings reports version `0.1.0` and enables installed-build update checking, open the crash directory, then uninstall CastoPet through its installed uninstaller.

- [ ] **Step 4: Verify no publication occurred**

Confirm no GitHub Release was created, no remote upload command ran, and all generated packages remain only under the ignored local artifacts directory.
