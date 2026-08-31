# Release Process

CastoPet publishes installers and Velopack update assets from GitHub Releases in the
public source repository: `sunboming/CastoPet`.

## Prerequisites

- Install the .NET 10 SDK and PowerShell 7.
- Install GitHub CLI and authenticate with `gh auth login`.
- Use an account that can push tags and create releases in `sunboming/CastoPet`.
- Commit every intended release change. The working tree must be clean.
- For a 0.1.x release, check out `release/0.1`; do not package a future `main` snapshot.

## Prepare A Version

Start from a clean worktree and select the semantic-version component to increment:

```powershell
pwsh -NoProfile -File eng/prepare-release.ps1 -Bump Patch
```

The `release/0.1` branch accepts only `Patch`. To start a new version line, run `Minor` or
`Major` on a release-ready `main`, commit the prepared version, and create the matching
branch such as `release/0.2` or `release/1.0`. The helper updates
`Directory.Build.props` and creates the matching file under `docs/release-notes/`. It
intentionally does not test, commit, tag, push, or publish. Review the generated notes,
run both Debug and Release verification, and commit the prepared version before continuing.

## Create A Draft Release

Run the release helper from the repository root:

```powershell
$version = ([xml](Get-Content Directory.Build.props -Raw)).Project.PropertyGroup.VersionPrefix
pwsh -NoProfile -File eng/release.ps1 -Version $version
```

The helper verifies the repository and version, runs the existing packaging workflow,
pushes the current branch, creates and pushes `v<version>`, and uploads four public assets
to a Draft GitHub Release:

By default, release notes come from the version-matched file under `docs/release-notes/`.
The helper requires that file and passes it to both Velopack packaging and GitHub, so the
in-app update prompt and the Release page use the same Markdown. A reviewed alternative can
be supplied with `-NotesFile`, but it must be an existing repository file.

- `CastoPet-win-Setup.msi` for normal installation with selectable scope and directory;
- `CastoPet-win-Portable.zip` for portable use;
- the versioned `CastoPet` full nupkg for installed-client updates;
- `releases.win.json` as the Velopack update feed.

The MSI uses Velopack's `Either` installation scope. Its standard Windows Installer wizard
lets the user select the installation directory and provides modify, repair, and uninstall
maintenance flows when it is run again. The packaging directory and CI artifact retain the
one-click `CastoPet-win-Setup.exe`, `assets.win.json`, the legacy `RELEASES` feed, and
`build-metadata.json` for deployment tooling, diagnostics, and traceability. These internal
files are not uploaded to the public GitHub Release.

The portable package stores settings, logs, and crash reports under `UserData` beside the
extracted application. It does not share `%LocalAppData%\CastoPet` with the installed build.

The script requires a branch matching the version line, for example `release/0.1` for
0.1.2. Confirm the branch before running it:

```powershell
git branch --show-current
```

The command is safe to rerun after a partial failure. A matching existing draft receives
fresh assets with replacement enabled. A tag that points to another commit or an already
published release stops the operation.

`-SkipTests` is available only when tests have already been run against the exact release
commit. Normal releases should omit it.

## Publish

Complete [release candidate testing](release-candidate-testing.md), including an installed
update from the previous public version when the previous client supports the local test
source. Record any documented one-time exception before publishing.

Review the draft on GitHub before selecting **Publish release**. Check the version, source
tag, release notes, installer, portable archive, full update package, and
`releases.win.json`. CastoPet clients cannot discover the release while it remains a draft.

The helper never publishes a draft automatically and never modifies an existing published
release.
