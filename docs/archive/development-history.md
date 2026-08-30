# CastoPet Development History

This document consolidates the dated design specifications and implementation plans that
were written while CastoPet was being built. It preserves the intent and outcome of each
stage without retaining obsolete task lists, machine-specific commands, or paths.

This is a historical record. For current contracts, start with [the documentation index](../README.md).

## 2026-06-25: Desktop Pet MVP

CastoPet began as a lightweight Windows desktop pet built with WPF and .NET. The initial
scope established the transparent pet window, tray integration, settings persistence,
single-instance handling, startup support, local logging, and a small dependency-free test
harness. Animation and richer interactions were intentionally outside the earliest MVP.

The static-MVP limitation was superseded almost immediately, but the service-oriented split
between window presentation and testable core behavior remains part of the project.

## 2026-06-26: Sprite Pipeline, Idle, and Blink

The project introduced an isolated candidate-expression workflow so generated artwork could
be reviewed before promotion into packaged resources. Character identity, colors, outfit,
and proportions were anchored to the standard Castorice reference, while expression images
were treated as emotional references only.

An eight-frame idle sequence replaced transform-only motion, followed by independently
scheduled random blinking. These designs originally referenced flat asset paths and fixed
sequence classes. Assets now use lifecycle-based directories, and animation metadata is
owned by skin and action definitions.

## 2026-06-27: Expressions and Animation Stabilization

The first radial expression wheel used a right-button hold gesture to select one of eight
temporary expressions. Shared transition-in and transition-out frames reduced abrupt image
swaps. Several stabilization passes investigated anchor drift, body jitter, frame continuity,
WPF transforms, and the problematic third idle frame.

The first single-ring wheel and its timing assumptions were later superseded by the
data-driven two-level wheel and the unified pointer gesture classifier. The diagnostic
conclusion remains relevant: sprite canvas alignment and frame continuity matter more than
adding visual interpolation to misaligned source frames.

## 2026-06-28: Movement and Input-Reactive Mode

Optional active movement added wandering and cursor approach behavior. Movement playback
became distance-driven so window travel and move frames advance from the same accumulated
distance instead of unrelated timers. A separate opt-in cursor-push setting allowed the pet
to nudge the pointer only while movement and safety conditions permit it.

The input-reactive mode added a Bongo Cat-style half-body composition, global keyboard and
mouse observation, and key highlights over a keyboard visual. Pure geometry and input state
were kept testable outside WPF, while Windows hooks and rendering remained platform code.

## 2026-07-03 to 2026-07-10: Data-Driven Skins and Timing

`PetSkinDefinition` and `PetActionDefinition` became the authoritative animation model for
built-in and external skins. Fixed sequence classes were retained only during migration and
then progressively retired. The built-in Castorice skin remained the default source of
packaged action definitions.

External JSON manifests enabled future skins without requiring a settings UI for skin
selection. Idle timing was aligned with its authored frame rate, and the model later gained
optional per-frame duration overrides while preserving `frameIntervalMs` as the default.
Current schema and validation rules are documented in [Skin manifest](../skin-manifest.md).

## 2026-07-11: Settings, Crash Records, and Updates

A dedicated settings window was introduced without creating a second settings system.
`SettingCatalog` supplies shared definitions, while tray menus, the pet context menu, and the
window present different views of the same commands and persisted values. The window later
received light, dark, and system-following glass-inspired themes.

Local crash reports and installer-aware update infrastructure were added with privacy and
failure isolation in mind. Crash reports stay local unless a future reporting service is
explicitly selected. Daily checks, manual checks, Velopack packaging, and separate update
identities later supported Stable and Preview build profiles. Those profiles were retired
when the repository restarted from the minimal 0.1 baseline.

## 2026-07-12 to 2026-07-16: Two-Level Wheel and Shortcuts

The radial interaction became a data-driven two-level catalog. Its first ring selects a
category and its second ring selects an expression or shortcut. Shortcut entries can be
created from supported dropped files, Windows shortcuts, internet shortcuts, URLs, and
registered URI schemes, then launched through a constrained service.

The wheel received repeated visual and interaction refinement: purple-white translucent
surfaces, clearer boundaries and labels, same-side second-ring placement, fast outward-motion
inference, outer-edge tolerance, and selected-sector feedback. Later pointer work separated
short right clicks from wheel gestures and short left clicks from dragging.

## Subsequent Consolidation

Later work split minimal Stable releases from feature-rich Preview builds, hardened external
skin path and image loading, added per-frame timing, improved crash metadata, introduced
stability monitoring and report visualization, and organized generated data outside source
directories. The active branches were subsequently restarted from one minimal 0.1 snapshot;
the former implementation remains on recovery branches. Current branch boundaries and risks
are maintained in [Branches and releases](../branches-and-releases.md) and
[Known risks](../known-risks.md).

## Superseded Material

The former `docs/superpowers/plans` and `docs/superpowers/specs` trees contained 36 dated
files. They were removed from the active tree because they duplicated each other, presented
implemented work as unchecked tasks, depended on an old agent workflow, and referenced paths
such as `sample/`, flat `Assets/States` directories, and the pre-consolidation `tools/` tree.

Their full text is retained by Git history. Use it only when investigating why an old change
was made; do not treat it as current setup or implementation guidance.
