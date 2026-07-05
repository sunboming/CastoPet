# Green Fringe Cleanup

CastoPet PNG cutouts can be checked and cleaned with:

```powershell
python tools\clean_green_fringe.py
python tools\clean_green_fringe.py --apply
```

Default behavior:

- Scans `src/CastoPet/Assets`.
- Excludes `CandidateSet`, `source`, `_green-clean-backup`, and `*-preview.png`.
- Detects visible green-dominant pixels on alpha edges.
- In `--apply` mode, replaces detected edge pixels with nearby non-green colors while preserving alpha.

Use this after generating or importing new transparent character, expression, idle, blink, move, or transition PNGs.
