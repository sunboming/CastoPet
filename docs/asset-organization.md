# Asset Organization

CastoPet assets are split by lifecycle and by whether they are application inputs or local
workspace material.

## Packaged Application Assets

- `src/CastoPet/Assets/Runtime/`: packaged runtime assets. The built-in Castorice skin currently lives under `Runtime/Castorice` and is referenced by WPF resource paths.
- `src/CastoPet/Assets/AppIcon.ico`: packaged application, taskbar, window, and tray icon.

The built-in app path root is `Assets/Runtime/Castorice`. New built-in animation and expression PNGs should be promoted there only after they are ready to ship.

## Artwork Sources

- `artwork/authoring/Castorice/`: tracked editable skin source data, layer files, manual animator
  data, and future skin manifests. Moving this tree does not package it into the application.
- `artwork/candidates/Castorice/`: tracked generated or experimental candidates retained for
  review before promotion into authoring or runtime assets.
- `artwork/references/`: local character and expression references used to guide artwork. It is
  not packaged into CastoPet and is ignored by Git.
- `artifacts/generation/`: generated runs, prompts, previews, and intermediate output. It is
  working data rather than an application resource.

Authoring and candidate assets are versioned but never compiled by default. Only reviewed files
should be copied into `src/CastoPet/Assets/Runtime/` through an intentional promotion step.
