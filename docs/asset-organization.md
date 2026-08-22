# Asset Organization

CastoPet assets are split by lifecycle and by whether they are application inputs or local
workspace material.

## Application Assets

- `src/CastoPet/Assets/Runtime/`: packaged runtime assets. The built-in Castorice skin currently lives under `Runtime/Castorice` and is referenced by WPF resource paths.
- `src/CastoPet/Assets/Skins/`: editable skin source data, layer files, manual animator data, and future skin manifests. These files are not treated as built-in packaged runtime resources unless a manifest loader or export step points at them.
- `src/CastoPet/Assets/CandidateSet/`: generated or experimental candidates for review before promotion. Candidate files are intentionally separate from runtime resources.

The built-in app path root is `Assets/Runtime/Castorice`. New built-in animation, expression, and input-reactive PNGs should be promoted there only after they are ready to ship.

The three application directories remain under `src` until a dedicated asset export pipeline
replaces direct project ownership. Do not move them as part of general repository cleanup.

## Workspace Assets

- `artwork/references/`: local character and expression references used to guide artwork. It is
  not packaged into CastoPet and is ignored by Git.
- `artwork/candidates/`: optional root-level review material that has not entered the source
  asset lifecycle. It is ignored by Git and may not exist in every checkout.
- `artifacts/generation/`: generated runs, prompts, previews, and intermediate output. It is
  working data rather than an application resource.

Reference, candidate, and generated files must be copied into the appropriate `src` lifecycle
directory only through an intentional review or promotion step.
