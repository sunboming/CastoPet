# Asset Organization

CastoPet assets are split by lifecycle:

- `src/CastoPet/Assets/Runtime/`: packaged runtime assets. The built-in Castorice skin currently lives under `Runtime/Castorice` and is referenced by WPF resource paths.
- `src/CastoPet/Assets/Skins/`: editable skin source data, layer files, manual animator data, and future skin manifests. These files are not treated as built-in packaged runtime resources unless a manifest loader or export step points at them.
- `src/CastoPet/Assets/CandidateSet/`: generated or experimental candidates for review before promotion. Candidate files are intentionally separate from runtime resources.

The built-in app path root is `Assets/Runtime/Castorice`. New built-in animation, expression, and input-reactive PNGs should be promoted there only after they are ready to ship.
