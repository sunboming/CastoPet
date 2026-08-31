# Branches And Releases

CastoPet is one product with one Windows identity, package id, data directory, and update
feed. The former Stable and Preview build profiles were removed before the 0.1 baseline.
Feature boundaries are maintained by branches and reviewed source changes, not build-time
edition switches.

## Active Branches

### `main`

`main` is the integration branch for future development. New features, architecture work,
and experiments that may change the 0.1 behavior start here. Keep it buildable, but do not
assume every `main` commit is suitable for a 0.1 patch release.

The architectural issues in `项目问题.md` belong to `main`. Resolve them incrementally with
tests before reintroducing advanced interaction features.

### `release/0.1`

`release/0.1` is the maintenance and packaging branch for the 0.1.x line. It accepts:

- release-blocking bug fixes;
- focused stability, compatibility, update, and packaging fixes;
- documentation that describes the shipped 0.1 product;
- version and release-note changes.

Do not add experimental interactions, broad data-model refactors, or later-version features
to this branch. A 0.1 fix should normally be implemented on `release/0.1`, verified there,
then cherry-picked to `main` if `main` still needs it. Never merge a feature-rich future
`main` back into `release/0.1`.

## Recovery Branch

The former full-featured product history remains available through one read-only branch:

- `codex/archive-main-before-0.1` retains the former full-featured `main` history.

Do not develop on or merge this branch. Use it only for history inspection, comparison, or
targeted recovery. A required old change should be reviewed and reimplemented or
cherry-picked deliberately.

## Versioning

The repository uses three-part semantic versions because Velopack requires package versions
such as `0.1.0` and `0.1.1`. The public product can still be described as "CastoPet 0.1".

For a 0.1.x release:

1. Work from `release/0.1`.
2. Run `eng/prepare-release.ps1 -Bump Patch` (or choose `Minor`/`Major`).
3. Commit the version and release notes.
4. Run Debug and Release tests and builds.
5. Run `eng/release.ps1 -Version <version>` from a clean worktree.
6. Review and manually publish the generated Draft Release.

Tags use `v<version>`, for example `v0.1.0`. Installation and update assets are hosted in
GitHub Releases of `sunboming/CastoPet`.

## Current Baseline

The active branches were restarted from the same minimal 0.1 file snapshot. The restart
commit intentionally has no parent so normal development history remains concise. The
recovery branch preserves the previous product history when detailed investigation is needed.
