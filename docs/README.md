# CastoPet Documentation

This directory separates current project contracts from historical development notes.
Current documents describe the repository as it exists today and take precedence over
older design decisions.

## Current Documents

| Document | Purpose |
| --- | --- |
| [Asset organization](asset-organization.md) | Lifecycle and ownership of runtime, editable, candidate, and reference assets. |
| [Branches and releases](branches-and-releases.md) | Responsibilities of `main`, `release/0.1`, the recovery branch, and version lines. |
| [Known risks](known-risks.md) | Unresolved security and stability risks plus important resolved boundaries. |
| [Release process](releasing.md) | One-command packaging, tagging, and controlled Draft Release creation. |
| [Release candidate testing](release-candidate-testing.md) | Candidate package verification and installed-update acceptance. |
| [Line endings](line-endings.md) | CRLF working-tree policy, LF Git storage, editor settings, and verification commands. |

## Retained Design References

The [skin manifest](skin-manifest.md) and [unified movement model](movement-model-plan.md)
describe code from the archived full-featured history. They are retained for future redesign
work on `main`, but they are not supported 0.1 runtime contracts and must not be copied back
without reviewing the current data model.

## Historical Documents

[Development history](archive/development-history.md) consolidates the former dated
design specifications and implementation plans. It records why major features were
introduced and which designs were later superseded, but it is not an implementation
contract.

The original detailed plans remain available in Git history before the documentation
consolidation. Do not copy paths, commands, task lists, or architecture from an old plan
without checking the current source tree first.

## Maintenance Rules

- Update a current document when a supported behavior, format, build command, or safety
  boundary changes.
- Record only durable decisions in current documents; temporary implementation checklists
  belong in tasks or commit history.
- Move superseded design rationale into the development history instead of leaving an
  apparently active plan with unchecked tasks.
- Use repository-relative paths and verify them before publishing documentation changes.
