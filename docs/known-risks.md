# Known Risks

The following issues were identified during the 2026-08-07 security and stability review.
Resolved items remain recorded so the implemented boundary is explicit.

## High Priority

### Public updates are unsigned

The current Velopack packaging workflow produces unsigned Windows installers and update
packages. The public GitHub release repository is therefore the primary update trust boundary.
A compromised release account or token could distribute an untrusted package to users who
approve the update prompt.

Planned direction: add Windows code signing before broad distribution. Until then, protect
release credentials with strong account controls and consider limiting automatic installation.

## Medium Priority

### Shortcut file safety uses an extension blocklist

The file launcher blocks common scripts and installers but does not cover every Windows file
type that can execute code through ShellExecute. Examples include control-panel applets and
other registered executable containers.

Planned direction: replace the incomplete blocklist with an explicit policy for intended
document and media types, while treating programs and Windows shortcuts as executable content.

### Some external inputs still need resource budgets

Skin manifests and their PNG resources now have byte, item-count, frame-count, file-size,
decoded-pixel, path-containment, and reparse-point limits. Shortcut storage, internet
shortcuts, and drag text do not yet have equally comprehensive byte limits. Malformed or
hostile local input can still cause avoidable UI work in those remaining surfaces.

Planned direction: add byte and item-size limits before expanding shortcut import surfaces.

## Resolved

### External skin paths and resources are bounded

Resolved on 2026-08-13. External skin loading now accepts only canonical local PNG paths
contained beneath the declared skin resource root. Rooted paths, UNC paths, traversal escapes,
symbolic links, and directory junctions are rejected before WPF image decoding. Manifest,
frame, file-size, dimension, decoded-pixel, and aspect-ratio budgets are enforced at the same
boundary. See `docs/skin-manifest.md` for the current limits.
