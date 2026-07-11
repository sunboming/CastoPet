# CastoPet Local Crash Recording and Auto Update Design

## Goal

Add privacy-preserving local crash recording and installer-aware automatic update infrastructure. Build and test an unsigned installer locally without publishing a GitHub Release or uploading any artifact.

## Scope

Included:

- Local crash report creation and next-start notification.
- A settings action that opens the crash-report directory.
- Daily automatic update checks and user-triggered manual checks.
- Update availability, failure, and current-version status in Settings.
- Velopack integration for installed builds.
- Explicit semantic versioning and a local unsigned package command.
- A locally generated test installer for installation verification.

Deferred:

- Automatic crash-report upload.
- Sentry, CrashSight, or another hosted crash service.
- Creation or publication of the first GitHub Release.
- Uploading assets to `sunboming/CastoPet-Releases`.
- Code-signing certificate configuration.
- A secondary update mirror.

## Application Data Paths

`AppPaths` remains the single owner of per-user application data paths. Crash reports are stored at:

`%LocalAppData%\CastoPet\Crashes`

This sits beside the existing settings and logs:

- `%LocalAppData%\CastoPet\settings.json`
- `%LocalAppData%\CastoPet\logs`
- `%LocalAppData%\CastoPet\Crashes`

The UI always provides an Open crash reports command so users do not need to locate the directory manually.

## Local Crash Recording

Register handlers for WPF dispatcher exceptions, unobserved task exceptions, and application-domain unhandled exceptions as early as practical in startup. A `CrashReportService` converts exceptions into a stable text report and writes it atomically to `Crashes` using a timestamp and unique suffix.

A report contains:

- UTC crash timestamp.
- CastoPet semantic version and build configuration marker where available.
- Windows version and process architecture.
- Exception type, message, stack trace, and inner exception chain.
- A bounded tail of the existing application log when it can be read safely.

Reports exclude settings contents, key or mouse input events, skin manifest contents, and user identifiers. Before writing, a sanitizer replaces the current user profile path, username, and other discovered absolute user paths with neutral placeholders.

Fatal exceptions are recorded and allowed to terminate normally. Dispatcher exceptions are recorded once; the application does not mark an unknown fatal exception as handled merely to continue in an unsafe state.

## Crash Notification

Each report has a deterministic identity. `AppSettings` stores the last acknowledged crash identity. On startup, if a newer report exists, CastoPet opens a compact notification after the pet window is ready. It offers:

- Open crash reports.
- Ignore.

Ignoring records the identity so the same report is not shown again. No consent or upload text is shown because no report leaves the device.

The Settings window adds an Open crash reports command under System. This is an action row, not a boolean setting, and therefore does not enter `SettingCatalog`.

## Update Source and Release Ownership

The private development repository is:

`git@github.com:sunboming/CastoPet.git`

The future public update repository is:

`git@github.com:sunboming/CastoPet-Releases.git`

The client uses the public HTTPS repository URL without a token:

`https://github.com/sunboming/CastoPet-Releases`

The first implementation uses Velopack `GithubSource`. An internal `IUpdateService` boundary keeps UI and scheduling independent from Velopack so a future official HTTPS mirror can be added without changing settings behavior.

## Update Scheduling

Automatic checks obey local calendar days:

1. Startup loads `LastAutomaticUpdateCheckDate` from settings.
2. If it equals the current local date, no automatic check is scheduled.
3. Otherwise, CastoPet waits ten seconds after normal startup.
4. It records today's attempt before making the request, ensuring no repeated automatic request that day even when GitHub is unavailable.
5. It checks stable releases only.

Manual checks ignore the daily gate and can be run at any time from Settings.

Network checks use an eight-second timeout. Timeout, DNS, rate-limit, offline, and GitHub errors do not block startup or interrupt pet behavior. Automatic failures are logged silently. Manual failures update the visible status to `检查失败，请稍后重试`.

## Update UI

Settings gains an Update section containing:

- Current application version.
- Last update-check status and time.
- A Check for updates command.

The command is disabled while a check or download is in progress. If a stable update exists, a compact prompt displays target version and release notes with:

- Update now.
- Later.

Update now downloads through Velopack with progress feedback. The current installation remains untouched until the package has downloaded and passed Velopack validation. The app then asks Velopack to apply the update and restart. Download or validation failure preserves the current version and returns the UI to a retryable state.

## Installed and Development Builds

Update checks run only when Velopack identifies the application as an installed package. Debug runs, `dotnet build` outputs, and direct Release build outputs report `开发版本不支持自动更新` for manual checks and skip automatic checks.

This prevents development binaries from treating GitHub assets as an applicable installation update.

## Versioning and Packaging

The project receives an explicit semantic version beginning at `0.1.0`. Local packaging publishes a Windows x64 self-contained build and invokes Velopack to create an unsigned setup executable and update packages in a local artifacts directory.

The packaging command must:

- Start from a clean output directory owned by the repository.
- Never upload or publish artifacts.
- Fail when version input is invalid.
- Produce a complete installer that can install, launch, and uninstall CastoPet per-user without administrator privileges.

Code signing remains an optional packaging input. With no certificate configured, the command produces an unsigned test installer and documents the expected Windows unknown-publisher warning.

## Failure Handling

- Crash-report write failure falls back to the normal log and never causes a second crash loop.
- A malformed or unreadable prior report is ignored and logged.
- GitHub network failure does not modify the installed application.
- Update download failure leaves the current version runnable.
- Update checks are serialized to prevent concurrent automatic and manual operations.
- Velopack package operations are not invoked in non-installed builds.

## Testing

Automated tests cover:

- Crash directory construction.
- Report naming, required metadata, bounded log tail, and path sanitization.
- A report write failure does not escape the crash handler.
- New-report detection and acknowledgement.
- Daily automatic-check decisions across same-day and next-day launches.
- Manual checks bypass the daily gate.
- Non-installed builds skip automatic updates.
- Network failure maps to a retryable UI status.
- Concurrent checks are rejected or share one operation.
- Settings persistence includes acknowledgement and last-check date.
- Project version is explicit and valid semantic versioning.

Verification includes Debug and Release tests/builds plus local Velopack packaging. A manual local smoke test installs the unsigned setup, launches CastoPet, confirms the installed-build update path is enabled, opens the crash directory, and uninstalls cleanly. No GitHub Release or remote upload is part of acceptance.

## Acceptance Criteria

- Fatal unhandled exceptions produce a sanitized local report under `%LocalAppData%\CastoPet\Crashes`.
- A new local crash is announced once on the next startup.
- Settings can open the crash-report directory.
- Automatic update checks occur at most once per local calendar day.
- Manual update checks remain available regardless of the daily gate.
- GitHub and network failures never prevent startup or damage the current installation.
- Only Velopack-installed builds can update.
- A local unsigned test installer is generated and verified without creating or uploading a GitHub Release.
