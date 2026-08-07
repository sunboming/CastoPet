# Known Risks

The following issues were identified during the 2026-08-07 security and stability review.
They are recorded for planned hardening and are not resolved by the stability runner.

## High Priority

### External skin paths are not contained

External skin manifests can currently use rooted paths, UNC paths, or `..` segments that
resolve outside the manifest directory. Loading an untrusted manifest can therefore read
unexpected local images, contact an SMB location, block startup, or expand the image-decoder
attack surface.

Planned direction: require canonical local PNG paths contained beneath the selected skin
root, reject UNC and rooted paths, and account for reparse-point escape.

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

### External inputs have no resource budget

Skin manifests, shortcut storage, internet shortcuts, drag text, frame counts, and image
dimensions do not have comprehensive size limits. Malformed or hostile local input can cause
long UI stalls or excessive memory use.

Planned direction: add byte, item-count, frame-count, file-size, and decoded-pixel limits before
expanding skin import UI.
