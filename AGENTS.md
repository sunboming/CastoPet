# CastoPet Development Instructions

These instructions apply to the entire repository.

## Workflow

- Implement small visual, copy, and parameter adjustments directly. They do not require a separate design document or implementation plan.
- Confirm the design with the user before implementing a new feature, a significant behavior change, or an architectural change.
- Use test-driven development for behavior changes and bug fixes: add a failing test, implement the change, and verify the test passes.
- Pure visual adjustments do not require automated tests when no stable behavior can be asserted. They still require a build check and user visual confirmation.
- Run focused tests while developing. Run the full relevant test suite before declaring an independent feature complete.
- Run both Debug and Release tests and builds for release-related, packaging, shared-runtime, or broad cross-module changes.
- Subagents are optional. Use them only when delegation materially helps; do not require them for routine work.

## Git

- Automatically commit a completed and verified independent feature.
- Do not commit pure visual tuning or experimental changes until the user confirms the result.
- Keep commits focused on the current task. Do not include unrelated user changes or local untracked files.
- Do not push, create a pull request, rewrite history, or discard existing changes unless the user explicitly requests it.

## Project Safety

- Follow `.gitattributes` and `.editorconfig`: maintained text uses CRLF in the working tree and LF in Git; Unix shell scripts use LF. Preserve existing encoding/BOM and never normalize binary assets.
- After text edits, run `pwsh -NoProfile -File eng/check-line-endings.ps1`. Use `-Fix` only for intentional line-ending normalization; it processes tracked files without staging them. Ensure new files follow the same rules before staging, then rerun the check.
- Preserve existing project patterns and keep changes narrowly scoped.
- Do not move, delete, or overwrite source images and generated animation assets unless the task explicitly requires it.
- If a running `CastoPet.exe` locks the normal Debug or Release output during a requested build, terminate that CastoPet process and retry the build without asking for separate confirmation.
- Report whether verification used Debug or Release builds, and provide the executable path when producing a build for user testing.
