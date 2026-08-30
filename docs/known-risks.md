# Known Risks

This document covers the minimal 0.1 product on `release/0.1`. Risks from archived shortcut,
external-skin, radial-wheel, input-response, and active-movement code are not current runtime
risks because those features are not compiled or packaged in 0.1.

## High Priority

### Windows packages are unsigned

The current Velopack workflow produces unsigned Windows installers and update packages.
Windows can display an unknown-publisher warning, and users cannot verify the publisher with
a code-signing certificate.

GitHub account security is therefore part of the update trust boundary. Protect maintainers
with strong authentication, restrict release permissions, create releases as drafts, and
review every uploaded asset before publication. Add code signing before broad distribution
if the warning or update trust model is unacceptable.

## Operational Risks

### Updates depend on GitHub availability

Installed builds read releases from `https://github.com/sunboming/CastoPet`. Users who cannot
reach GitHub cannot receive automatic updates. Update failures are isolated from application
startup, so the installed version continues to run and can be updated later with a manually
downloaded installer.

### Crash reports remain local

Unhandled failures are recorded under the local CastoPet data directory and surfaced on the
next start. Reports are not uploaded automatically. This protects privacy but means users must
provide a report manually when requesting support.

Crash reports can include stack traces, application log lines, operating-system information,
and local source paths from development builds. Review a report before sharing it publicly.

## Development Boundary

The data-model and animation-controller concerns in `项目问题.md` affect future development on
`main`. They are not release blockers for the minimal 0.1 feature set unless a concrete crash,
corruption issue, or incompatible update is reproduced. Avoid broad refactors on
`release/0.1`; fix verified release defects narrowly and port them to `main` as needed.
