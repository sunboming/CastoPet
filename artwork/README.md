# Artwork Workspace

This directory contains tracked authoring and review material that is not packaged directly by
the WPF project, plus local reference images used during artwork production.

- `authoring/Castorice/`: editable skin sources, layers, and animator definitions.
- `candidates/Castorice/`: generated candidates retained for review and promotion.
- `references/character/`: standard character reference images.
- `references/expressions/`: expression reference images.

Only `references/` is local and ignored by Git. Authoring and candidate assets are tracked so a
fresh checkout contains the complete editable source history. Packaged application assets remain
under `src/CastoPet/Assets/Runtime/`; promotion into that directory is an explicit release step.
