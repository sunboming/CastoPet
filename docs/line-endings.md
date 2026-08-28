# Line Endings

CastoPet uses Windows, Visual Studio, and VS Code for development.

## Policy

- Maintained text uses CRLF in the working tree, including C#, XAML, project and solution
  files, PowerShell, JSON, Markdown, YAML, and web files. Git stores normalized text as LF.
- Unix shell scripts (`.sh`, `.bash`, `.zsh`) use LF in both places.
- Binary files, including character PNGs and icons, must not be normalized.
- Preserve encoding, BOM, indentation, trailing spaces, and final-newline presence.

`.gitattributes` defines Git conversion independently of the local `core.autocrlf`.
`.editorconfig` defines editor behavior. `.vscode/settings.json` also sets VS Code's
default line ending explicitly, with an LF override for shell scripts. Editor support
varies: configuration alone does not fix existing mixed files. Reopen files after external
normalization rather than saving stale editor contents over them.

## Check and Normalize

Run from the repository root with PowerShell 7 and Git available:

```powershell
pwsh -NoProfile -File eng/check-line-endings.ps1
pwsh -NoProfile -File tests/Tooling/LineEndings.Tests.ps1
```

The checker reads tracked paths and effective Git attributes. It checks the index for LF
and the working tree for each file's configured style. A mixed file fails even when Git
would normalize it during a future commit. Empty files and files with no line breaks are
allowed. Both Build and Package CI run the check.

To intentionally normalize tracked working-tree files:

```powershell
pwsh -NoProfile -File eng/check-line-endings.ps1 -Fix
pwsh -NoProfile -File eng/check-line-endings.ps1
```

Fix mode converts CRLF/LF only. It preserves non-newline bytes and BOMs, skips binary and
untracked files, and does not stage or commit anything. It refuses to rewrite links,
junctions, NUL-containing text, or standalone CR characters; review those separately.
Multiline string literals and byte-sensitive fixtures can observe newline changes, so
run the application tests after repository-wide normalization.

New files are checked after staging. Follow the editor defaults before staging; if the
check fails afterward, normalize and stage only the intended files again. Do not bulk-stage
an unrelated dirty worktree. If the index contains CRLF, review and renormalize those index
entries separately; `-Fix` changes only working files.

Generated outputs under `bin`, `obj`, ignored build directories, and untracked local
references are outside the check. No history rewrite is needed for this policy.
