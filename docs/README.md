# CastoPet Documentation

This directory separates current project contracts from historical development notes.
Current documents describe the repository as it exists today and take precedence over
older design decisions.

## Current Documents

| Document | Purpose |
| --- | --- |
| [Asset organization](asset-organization.md) | Lifecycle and ownership of runtime, editable, candidate, and reference assets. |
| [Build editions](build-editions.md) | Stable and Preview feature boundaries, build commands, identities, and packaging. |
| [Known risks](known-risks.md) | Unresolved security and stability risks plus important resolved boundaries. |
| [Skin manifest](skin-manifest.md) | External skin timing schema, validation rules, and resource budgets. |
| [Unified movement model](movement-model-plan.md) | Shared movement settings, directional variants, and removal of turn playback. |
| [Line endings](line-endings.md) | CRLF working-tree policy, LF Git storage, editor settings, and verification commands. |

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
