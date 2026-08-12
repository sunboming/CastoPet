# CastoPet build editions

CastoPet uses one source tree with two build-time feature profiles. Preview is the
default for local development. Stable is the minimal public release profile.

## Feature boundary

| Capability | Stable | Preview |
| --- | --- | --- |
| Idle and blink animation | Yes | Yes |
| Dragging | Yes | Yes |
| Tray show, settings, and exit | Yes | Yes |
| Always on top, click-through, taskbar icon, startup | Yes | Yes |
| Local crash reports and updates | Yes | Yes |
| Left-click petting | No | Yes |
| Radial wheel and expressions | No | Yes |
| Shortcut launcher and drop import | No | Yes |
| Active movement and cursor push | No | Yes |
| Input-reactive mode | No | Yes |
| External skin manifests | No | Yes |

Stable ignores persisted Preview-only settings without deleting them. Returning to
Preview restores their saved values.

The editions also use separate Windows identities. Stable owns `%LocalAppData%\CastoPet`,
the `CastoPet` startup entry, package id, and update feed. Preview owns
`%LocalAppData%\CastoPet-Preview`, a separate startup entry and package id, and does not
consume Stable updates. On its first run, Preview copies legacy settings and shortcuts
from the old shared directory without moving logs, crash reports, or deleting source data.

## Build commands

Preview remains compatible with the normal build command:

```powershell
dotnet build CastoPet.sln -c Release
```

Build the minimal public version explicitly:

```powershell
dotnet restore CastoPet.sln -p:CastoPetEdition=Stable
dotnet build CastoPet.sln -c Release --no-restore -p:CastoPetEdition=Stable
```

The Stable executable is written to:

```text
src/CastoPet/bin/Stable/Release/net10.0-windows/CastoPet.exe
```

Create a self-contained installer and Velopack update payload from a clean commit:

```powershell
./eng/package.ps1 -Edition Stable -Version 0.1.0
```

Preview uses a separate package id and should use a prerelease version for clarity:

```powershell
./eng/package.ps1 -Edition Preview -Version 0.2.0-preview.1
```

Packages and `build-metadata.json` are written below
`artifacts/packages/<Edition>/<Version>`. The metadata records the edition, package id,
source commit, dirty-state flag, and SHA-256 for every package file. Packaging rejects a
dirty worktree unless `-AllowDirty` is supplied for a local smoke test. The manual Package
workflow creates only a seven-day Actions artifact; it does not publish a GitHub Release.

Do not publish the default `src/CastoPet/bin/Release` output as the minimal first
release; that path contains the Preview edition.

## Adding a feature

Add the capability to `CastoPetFeatureProfile` first, then gate runtime composition,
settings visibility, interactions, and packaged resources through that profile. Keep
edition preprocessor symbols centralized in the profile instead of scattering
`#if` directives through feature code.
