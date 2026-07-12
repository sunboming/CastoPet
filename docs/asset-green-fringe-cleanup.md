# Green Fringe Cleanup

CastoPet PNG cutouts can be checked and cleaned with:

```powershell
python tools\clean_green_fringe.py
python tools\clean_green_fringe.py --apply
```

Default behavior:

- Scans `src/CastoPet/Assets/Runtime/Castorice`.
- Excludes `CandidateSet`, `source`, `_green-clean-backup`, and `*-preview.png` if you override `--root` to a wider asset folder.
- Detects visible green-dominant pixels on alpha edges.
- In `--apply` mode, replaces detected edge pixels with nearby non-green colors while preserving alpha.

Use this after generating or importing new transparent character, expression, idle, blink, move, or transition PNGs.

To scan editable skin sources or candidate images instead, pass an explicit root:

```powershell
python tools\clean_green_fringe.py --root src\CastoPet\Assets\Skins\Castorice
python tools\clean_green_fringe.py --root src\CastoPet\Assets\CandidateSet\Transparent
```
