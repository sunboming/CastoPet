# Release Candidate Testing

Every public release must pass package verification and an isolated Windows smoke test before
the GitHub Draft Release is published. A GitHub draft is not visible to the production update
source, so opening the draft page is not an update test.

## Automated Candidate Verification

Build from the clean, committed release branch. `eng/package.ps1` automatically invokes the
candidate verifier; it may also be rerun directly:

```powershell
pwsh -NoProfile -File eng/package.ps1
pwsh -NoProfile -File eng/verify-release-candidate.ps1
```

The verifier checks the central version, source commit, clean-build marker, required public
assets, SHA256 values, update feed, nupkg metadata, embedded release notes, and portable root
executable. A failure blocks `eng/release.ps1` before the branch or tag is pushed.

## Installed Update Test

Use a disposable Windows VM or test account. Keep a copy of its state before applying the
candidate.

1. Install the previous public version and confirm its displayed version.
2. Build the candidate, then resolve its package directory:

```powershell
$version = ([xml](Get-Content Directory.Build.props -Raw)).Project.PropertyGroup.VersionPrefix
$packageDirectory = (Resolve-Path "artifacts/packages/$version/packages").Path
```

3. Start the installed previous version's root execution stub with the explicit local source:

```powershell
$installedExe = "$env:LOCALAPPDATA\CastoPet\CastoPet.exe"
& $installedExe --test-update-source $packageDirectory
```

4. Trigger update checks repeatedly and confirm only one update window is shown.
5. Confirm the target version and complete release notes are displayed.
6. Download and apply the update, then confirm the application restarts on the candidate
   version with settings and local crash history preserved.
7. Restart normally without `--test-update-source` and confirm the application returns to the
   public GitHub update source.

The explicit source accepts only an existing absolute local directory and is never enabled by
normal startup. Public `0.1.1` predates this test hook, so the exact `0.1.1 -> 0.1.2` production
binary path is a one-time exception. Record that limitation in the `0.1.2` release review.

## Distribution Smoke Test

- Install the MSI to a custom directory; verify launch, repair, and uninstall.
- Extract the portable ZIP; verify settings, logs, and crashes stay under its `UserData`.
- Confirm the installed and portable copies do not share settings or crash reports.
- Compare the four Draft Release attachment names and GitHub digests with the verified local
  candidate before selecting **Publish release**.
