# Release Process

CastoPet publishes installers and Velopack update assets from GitHub Releases in the
public source repository: `sunboming/CastoPet`.

## Prerequisites

- Install the .NET 10 SDK and PowerShell 7.
- Install GitHub CLI and authenticate with `gh auth login`.
- Use an account that can push tags and create releases in `sunboming/CastoPet`.
- Commit every intended release change. The working tree must be clean.
- Set `VersionPrefix` in `Directory.Build.props` to the release version and commit it.
- For a 0.1.x release, check out `release/0.1`; do not package a future `main` snapshot.

## Create A Draft Release

Run the release helper from the repository root:

```powershell
pwsh -NoProfile -File eng/release.ps1 -Version 0.1.2
```

To supply reviewed release notes:

```powershell
pwsh -NoProfile -File eng/release.ps1 -Version 0.1.2 -NotesFile CHANGELOG.md
```

The helper verifies the repository and version, runs the existing packaging workflow,
pushes the current branch, creates and pushes `v<version>`, and uploads four public assets
to a Draft GitHub Release:

By default, release notes come from `docs/release-notes/<version>.md`. The helper requires
that file and passes it to both Velopack packaging and GitHub, so the in-app update prompt
and the Release page use the same Markdown. `-NotesFile` may override the default path.

- `CastoPet-win-Setup.msi` for normal installation with selectable scope and directory;
- `CastoPet-win-Portable.zip` for portable use;
- `CastoPet-<version>-full.nupkg` for installed-client updates;
- `releases.win.json` as the Velopack update feed.

The MSI uses Velopack's `Either` installation scope. Its standard Windows Installer wizard
lets the user select the installation directory and provides modify, repair, and uninstall
maintenance flows when it is run again. The packaging directory and CI artifact retain the
one-click `CastoPet-win-Setup.exe`, `assets.win.json`, the legacy `RELEASES` feed, and
`build-metadata.json` for deployment tooling, diagnostics, and traceability. These internal
files are not uploaded to the public GitHub Release.

The portable package stores settings, logs, and crash reports under `UserData` beside the
extracted application. It does not share `%LocalAppData%\CastoPet` with the installed build.

The script intentionally pushes whichever named branch is currently checked out. Confirm
the branch before running it:

```powershell
git branch --show-current
```

The command is safe to rerun after a partial failure. A matching existing draft receives
fresh assets with replacement enabled. A tag that points to another commit or an already
published release stops the operation.

`-SkipTests` is available only when tests have already been run against the exact release
commit. Normal releases should omit it.

## Publish

Review the draft on GitHub before selecting **Publish release**. Check the version, source
tag, release notes, installer, portable archive, full update package, and
`releases.win.json`. CastoPet clients cannot discover the release while it remains a draft.

The helper never publishes a draft automatically and never modifies an existing published
release.
