# CastoPet Candidate Expression Set Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate an isolated candidate sprite set with core desktop-pet states plus 12 extra full-body expression sprites, while preserving the current production assets.

**Architecture:** Keep all generated candidates under `src/CastoPet/Assets/CandidateSet/` with a `Source/` tree for high-resolution green-screen files and a matching `Transparent/` tree for 320x320 transparent PNGs. Add lightweight validation scripts and tests so production resources remain low-memory while candidate assets can be reviewed safely before replacement.

**Tech Stack:** C#/.NET 10 console tests, Python with Pillow from the Codex workspace runtime, built-in image generation, local chroma-key removal helper, PNG contact sheets.

---

## File Structure

Create or modify these files:

```text
docs/superpowers/plans/2026-06-26-castopet-candidate-expression-set.md
tests/CastoPet.Tests/Program.cs
tools/candidate-assets/validate_candidate_set.py
tools/candidate-assets/make_contact_sheet.py
src/CastoPet/Assets/CandidateSet/Source/Castorice.png
src/CastoPet/Assets/CandidateSet/Source/States/
src/CastoPet/Assets/CandidateSet/Source/Expressions/
src/CastoPet/Assets/CandidateSet/Transparent/Castorice.png
src/CastoPet/Assets/CandidateSet/Transparent/States/
src/CastoPet/Assets/CandidateSet/Transparent/Expressions/
src/CastoPet/Assets/CandidateSet/core-preview.png
src/CastoPet/Assets/CandidateSet/expressions-preview.png
src/CastoPet/Assets/CandidateSet/full-preview.png
```

Responsibilities:

- `tests/CastoPet.Tests/Program.cs`: keep production packaged asset size checks scoped to active app resources, not candidate source images.
- `validate_candidate_set.py`: validate candidate transparent outputs, dimensions, alpha corners, and expected names.
- `make_contact_sheet.py`: create review contact sheets from candidate transparent PNGs.
- `CandidateSet/Source`: retain high-resolution green-screen source images for rework.
- `CandidateSet/Transparent`: retain 320x320 transparent PNGs ready for later app integration.

## Candidate Asset Names

Core candidate outputs:

```text
Castorice.png
States/Castorice.Happy.png
States/Castorice.Sleepy.png
States/Castorice.Surprised.png
States/Castorice.Dragging.png
States/Idle/Castorice.Idle.00.png
States/Idle/Castorice.Idle.01.png
States/Idle/Castorice.Idle.02.png
States/Idle/Castorice.Idle.03.png
States/Idle/Castorice.Idle.04.png
States/Idle/Castorice.Idle.05.png
States/Idle/Castorice.Idle.06.png
States/Idle/Castorice.Idle.07.png
States/Blink/Castorice.Blink.00.png
States/Blink/Castorice.Blink.01.png
States/Blink/Castorice.Blink.02.png
```

Expression candidate outputs:

```text
Expressions/Castorice.Expression.Happy.png
Expressions/Castorice.Expression.Shy.png
Expressions/Castorice.Expression.Sleepy.png
Expressions/Castorice.Expression.Surprised.png
Expressions/Castorice.Expression.Pouting.png
Expressions/Castorice.Expression.Confused.png
Expressions/Castorice.Expression.Proud.png
Expressions/Castorice.Expression.Worried.png
Expressions/Castorice.Expression.Crying.png
Expressions/Castorice.Expression.Excited.png
Expressions/Castorice.Expression.Bored.png
Expressions/Castorice.Expression.Affection.png
```

## Shared Image Prompt Constraints

Use this invariant block in every built-in image generation prompt:

```text
Use case: style-transfer
Asset type: CastoPet desktop pet sprite candidate
Input image role: use src/CastoPet/Assets/Castorice.png as the identity, palette, outfit, proportions, and style authority. Use sample images only as expression references.
Primary request: create one full-body chibi desktop pet sprite of the same Castorice character for the named state.
Subject constraints: same lavender hair, purple eyes, black-purple headpiece, flower crown, white and purple flower dress, twin side hair ribbons, same chibi proportions, same soft purple palette. Preserve character identity and outfit details from the standard Castorice.png.
Composition/framing: centered full body on a square canvas, generous padding, consistent scale, no crop.
Background: perfectly flat solid #00ff00 chroma-key background for background removal. No shadows, no gradients, no texture, no floor, no checkerboard.
Avoid: extra characters, props, speech bubbles, captions, subtitles, watermark, cropped body, changed outfit, realistic style, complex background, sample image background, sample image text.
```

## Task 1: Scope Production Asset Size Test

**Files:**
- Modify: `tests/CastoPet.Tests/Program.cs`

- [x] **Step 1: Update the production asset enumeration**

Replace the `assets` declaration inside `PackagedCharacterAssetsAreDisplaySized` with:

```csharp
var assetsRoot = System.IO.Path.Combine(workspace, "src", "CastoPet", "Assets");
var excludedSegments = new[]
{
    $"{System.IO.Path.DirectorySeparatorChar}CandidateSet{System.IO.Path.DirectorySeparatorChar}",
};
var assets = Directory
    .EnumerateFiles(assetsRoot, "*.png", SearchOption.AllDirectories)
    .Where(path => !System.IO.Path.GetFileName(path).Equals("blink-preview.png", StringComparison.OrdinalIgnoreCase))
    .Where(path => !excludedSegments.Any(segment => path.Contains(segment, StringComparison.OrdinalIgnoreCase)));
```

- [x] **Step 2: Run tests**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: all existing tests still print `PASS`.

- [x] **Step 3: Commit**

```powershell
git add tests/CastoPet.Tests/Program.cs
git commit -m "test: exclude candidate sources from packaged asset size check"
```

Skip this commit only if git is unavailable.

## Task 2: Add Candidate Validation Script

**Files:**
- Create: `tools/candidate-assets/validate_candidate_set.py`

- [x] **Step 1: Create the validation script**

Write `tools/candidate-assets/validate_candidate_set.py`:

```python
from __future__ import annotations

import argparse
from pathlib import Path
from PIL import Image


CORE_RELATIVE_PATHS = [
    "Castorice.png",
    "States/Castorice.Happy.png",
    "States/Castorice.Sleepy.png",
    "States/Castorice.Surprised.png",
    "States/Castorice.Dragging.png",
    "States/Idle/Castorice.Idle.00.png",
    "States/Idle/Castorice.Idle.01.png",
    "States/Idle/Castorice.Idle.02.png",
    "States/Idle/Castorice.Idle.03.png",
    "States/Idle/Castorice.Idle.04.png",
    "States/Idle/Castorice.Idle.05.png",
    "States/Idle/Castorice.Idle.06.png",
    "States/Idle/Castorice.Idle.07.png",
    "States/Blink/Castorice.Blink.00.png",
    "States/Blink/Castorice.Blink.01.png",
    "States/Blink/Castorice.Blink.02.png",
]

EXPRESSION_RELATIVE_PATHS = [
    "Expressions/Castorice.Expression.Happy.png",
    "Expressions/Castorice.Expression.Shy.png",
    "Expressions/Castorice.Expression.Sleepy.png",
    "Expressions/Castorice.Expression.Surprised.png",
    "Expressions/Castorice.Expression.Pouting.png",
    "Expressions/Castorice.Expression.Confused.png",
    "Expressions/Castorice.Expression.Proud.png",
    "Expressions/Castorice.Expression.Worried.png",
    "Expressions/Castorice.Expression.Crying.png",
    "Expressions/Castorice.Expression.Excited.png",
    "Expressions/Castorice.Expression.Bored.png",
    "Expressions/Castorice.Expression.Affection.png",
]


def validate_png(path: Path) -> list[str]:
    errors: list[str] = []
    if not path.exists():
        return [f"missing: {path}"]

    with Image.open(path) as image:
        rgba = image.convert("RGBA")
        if rgba.size != (320, 320):
            errors.append(f"{path}: expected 320x320, got {rgba.width}x{rgba.height}")

        corners = [
            rgba.getpixel((0, 0))[3],
            rgba.getpixel((319, 0))[3],
            rgba.getpixel((0, 319))[3],
            rgba.getpixel((319, 319))[3],
        ]
        if any(alpha != 0 for alpha in corners):
            errors.append(f"{path}: expected transparent corners, got alpha {corners}")

        alpha = rgba.getchannel("A")
        opaque_pixels = sum(1 for value in alpha.getdata() if value > 16)
        if opaque_pixels < 4000:
            errors.append(f"{path}: too few visible pixels ({opaque_pixels})")

    return errors


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default="src/CastoPet/Assets/CandidateSet/Transparent")
    parser.add_argument("--mode", choices=["core", "expressions", "all"], default="all")
    args = parser.parse_args()

    root = Path(args.root)
    expected: list[str] = []
    if args.mode in {"core", "all"}:
        expected.extend(CORE_RELATIVE_PATHS)
    if args.mode in {"expressions", "all"}:
        expected.extend(EXPRESSION_RELATIVE_PATHS)

    errors: list[str] = []
    for relative in expected:
        errors.extend(validate_png(root / relative))

    if errors:
        for error in errors:
            print(f"FAIL {error}")
        return 1

    print(f"PASS validated {len(expected)} candidate PNG files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
```

- [x] **Step 2: Run validation before assets exist**

Run:

```powershell
& 'C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' tools\candidate-assets\validate_candidate_set.py --mode core
```

Expected: it fails with `missing:` lines because candidate assets have not been generated yet.

- [x] **Step 3: Commit**

```powershell
git add tools/candidate-assets/validate_candidate_set.py
git commit -m "chore: add candidate sprite validation script"
```

## Task 3: Add Contact Sheet Script

**Files:**
- Create: `tools/candidate-assets/make_contact_sheet.py`

- [x] **Step 1: Create the contact sheet script**

Write `tools/candidate-assets/make_contact_sheet.py`:

```python
from __future__ import annotations

import argparse
from pathlib import Path
from PIL import Image, ImageDraw


def collect_images(root: Path) -> list[Path]:
    return sorted(path for path in root.rglob("*.png") if path.is_file())


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--columns", type=int, default=4)
    args = parser.parse_args()

    root = Path(args.input)
    paths = collect_images(root)
    if not paths:
        print(f"FAIL no png files under {root}")
        return 1

    cell = 180
    label_height = 36
    columns = max(1, args.columns)
    rows = (len(paths) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * cell, rows * (cell + label_height)), (245, 245, 245))
    draw = ImageDraw.Draw(sheet)

    for index, path in enumerate(paths):
        image = Image.open(path).convert("RGBA")
        image.thumbnail((cell - 16, cell - 16), Image.Resampling.LANCZOS)
        col = index % columns
        row = index // columns
        x = col * cell + (cell - image.width) // 2
        y = row * (cell + label_height) + 8
        sheet.paste(image, (x, y), image)
        label = path.relative_to(root).as_posix()
        draw.text((col * cell + 8, row * (cell + label_height) + cell), label[:28], fill=(20, 20, 20))

    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out)
    print(f"PASS wrote {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
```

- [x] **Step 2: Run contact sheet before assets exist**

Run:

```powershell
& 'C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' tools\candidate-assets\make_contact_sheet.py --input src\CastoPet\Assets\CandidateSet\Transparent --out src\CastoPet\Assets\CandidateSet\full-preview.png
```

Expected: it fails with `FAIL no png files`.

- [x] **Step 3: Commit**

```powershell
git add tools/candidate-assets/make_contact_sheet.py
git commit -m "chore: add candidate sprite contact sheet script"
```

## Task 4: Generate Core Source Images

**Files:**
- Create: `src/CastoPet/Assets/CandidateSet/Source/Castorice.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Castorice.Happy.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Castorice.Sleepy.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Castorice.Surprised.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Castorice.Dragging.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Idle/Castorice.Idle.00.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Idle/Castorice.Idle.01.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Idle/Castorice.Idle.02.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Idle/Castorice.Idle.03.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Idle/Castorice.Idle.04.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Idle/Castorice.Idle.05.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Idle/Castorice.Idle.06.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Idle/Castorice.Idle.07.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Blink/Castorice.Blink.00.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Blink/Castorice.Blink.01.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/States/Blink/Castorice.Blink.02.png`

- [x] **Step 1: Generate these built-in image prompts one asset at a time**

Use the shared prompt constraints from this plan and append one state-specific line:

```text
State: default calm expression, neutral standing pose, relaxed eyes, quiet desktop pet presence.
```

```text
State: happy expression, soft smile, friendly and warm, one small cheerful pose change.
```

```text
State: sleepy expression, half-closed tired eyes, soft drowsy posture.
```

```text
State: surprised expression, wide eyes, small open mouth, cute startled reaction, not frightened.
```

```text
State: dragging expression, mildly surprised as if gently picked up, arms slightly lifted, body subtly raised.
```

For idle frames, generate 8 similar full-body sprites with these state lines:

```text
State: idle frame 00, calm default expression, neutral body position.
State: idle frame 01, calm default expression, slight inhale, hair and skirt barely shift.
State: idle frame 02, calm default expression, gentle inhale peak, tiny upward body motion.
State: idle frame 03, calm default expression, body eases toward center, hair follows.
State: idle frame 04, calm default expression, neutral center, skirt settles.
State: idle frame 05, calm default expression, slight exhale, tiny downward body motion.
State: idle frame 06, calm default expression, exhale low point, hair and skirt softly lag.
State: idle frame 07, calm default expression, returning to neutral, loop-ready with frame 00.
```

For blink frames, generate 3 similar full-body sprites with these state lines:

```text
State: blink frame 00, same default pose, eyes half closed.
State: blink frame 01, same default pose, eyes fully closed.
State: blink frame 02, same default pose, eyes reopening half open.
```

- [x] **Step 2: Save generated files into the Source tree**

Copy each accepted generated image from Codex's generated image directory into the exact matching core path listed in this task's **Files** section.

- [x] **Step 3: Inspect the source tree**

Run:

```powershell
Get-ChildItem -LiteralPath src\CastoPet\Assets\CandidateSet\Source -Recurse -File
```

Expected: 16 PNG files exist for the core set.

## Task 5: Process Core Images To Transparent Outputs

**Files:**
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/Castorice.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Castorice.Happy.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Castorice.Sleepy.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Castorice.Surprised.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Castorice.Dragging.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Idle/Castorice.Idle.00.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Idle/Castorice.Idle.01.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Idle/Castorice.Idle.02.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Idle/Castorice.Idle.03.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Idle/Castorice.Idle.04.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Idle/Castorice.Idle.05.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Idle/Castorice.Idle.06.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Idle/Castorice.Idle.07.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Blink/Castorice.Blink.00.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Blink/Castorice.Blink.01.png`
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/States/Blink/Castorice.Blink.02.png`
- Create: `src/CastoPet/Assets/CandidateSet/core-preview.png`

- [x] **Step 1: Remove chroma key and resize core files**

Run this PowerShell command to process every core source PNG except expression files:

```powershell
$python = 'C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
$helper = 'C:\Users\lemon\.codex\skills\.system\imagegen\scripts\remove_chroma_key.py'
$sourceRoot = Resolve-Path 'src\CastoPet\Assets\CandidateSet\Source'
$outRoot = Resolve-Path 'src\CastoPet\Assets\CandidateSet'
Get-ChildItem -LiteralPath $sourceRoot -Recurse -Filter *.png |
    Where-Object { $_.FullName -notlike '*\Expressions\*' } |
    ForEach-Object {
        $relative = [System.IO.Path]::GetRelativePath($sourceRoot, $_.FullName)
        $out = Join-Path (Join-Path $outRoot 'Transparent') $relative
        $tmp = [System.IO.Path]::ChangeExtension($out, '.tmp.png')
        New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($out)) | Out-Null
        & $python $helper --input $_.FullName --out $tmp --auto-key border --soft-matte --transparent-threshold 12 --opaque-threshold 220 --despill
        & $python -c "from PIL import Image; from pathlib import Path; src=Path(r'$tmp'); out=Path(r'$out'); out.parent.mkdir(parents=True, exist_ok=True); im=Image.open(src).convert('RGBA'); im.thumbnail((320,320), Image.Resampling.LANCZOS); canvas=Image.new('RGBA',(320,320),(0,0,0,0)); canvas.paste(im,((320-im.width)//2,(320-im.height)//2),im); canvas.save(out); src.unlink()"
    }
```

- [x] **Step 2: Validate core transparent files**

Run:

```powershell
& 'C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' tools\candidate-assets\validate_candidate_set.py --mode core
```

Expected:

```text
PASS validated 16 candidate PNG files
```

- [x] **Step 3: Generate core contact sheet**

Run:

```powershell
& 'C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' tools\candidate-assets\make_contact_sheet.py --input src\CastoPet\Assets\CandidateSet\Transparent --out src\CastoPet\Assets\CandidateSet\core-preview.png
```

Expected: `PASS wrote src\CastoPet\Assets\CandidateSet\core-preview.png`.

- [x] **Step 4: Commit**

```powershell
git add src/CastoPet/Assets/CandidateSet/Source src/CastoPet/Assets/CandidateSet/Transparent src/CastoPet/Assets/CandidateSet/core-preview.png
git commit -m "art: add candidate core Castorice sprites"
```

## Task 6: Generate Expression Source Images

**Files:**
- Create: `src/CastoPet/Assets/CandidateSet/Source/Expressions/Castorice.Expression.Happy.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/Expressions/Castorice.Expression.Shy.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/Expressions/Castorice.Expression.Sleepy.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/Expressions/Castorice.Expression.Surprised.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/Expressions/Castorice.Expression.Pouting.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/Expressions/Castorice.Expression.Confused.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/Expressions/Castorice.Expression.Proud.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/Expressions/Castorice.Expression.Worried.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/Expressions/Castorice.Expression.Crying.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/Expressions/Castorice.Expression.Excited.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/Expressions/Castorice.Expression.Bored.png`
- Create: `src/CastoPet/Assets/CandidateSet/Source/Expressions/Castorice.Expression.Affection.png`

- [x] **Step 1: Generate these built-in image prompts one asset at a time**

Use the shared prompt constraints from this plan and append one expression-specific line:

```text
Expression: Happy, smiling, warm, friendly, cheerful full-body pose.
Expression: Shy, bashful, lightly blushing, modest posture.
Expression: Sleepy, tired half-closed eyes, drowsy full-body pose.
Expression: Surprised, wide eyes, small open mouth, startled but cute.
Expression: Pouting, mildly annoyed, puffed cheeks, cute displeased reaction.
Expression: Confused, puzzled blank reaction, small question-like facial expression, no text symbols.
Expression: Proud, confident and pleased, small self-satisfied pose.
Expression: Worried, uneasy and hesitant, small frown, gentle anxious mood.
Expression: Crying, teary sad expression, cute and soft rather than intense distress.
Expression: Excited, eager bright eyes, high-energy happy anticipation.
Expression: Bored, unimpressed flat expression, low-energy idle posture.
Expression: Affection, soft affectionate smile, clingy or caring mood, no extra character.
```

- [x] **Step 2: Save generated files into the Source expression tree**

Copy each accepted generated image from Codex's generated image directory into the exact matching expression path listed in this task's **Files** section.

- [x] **Step 3: Inspect expression source files**

Run:

```powershell
Get-ChildItem -LiteralPath src\CastoPet\Assets\CandidateSet\Source\Expressions -File
```

Expected: 12 expression PNG files exist.

## Task 7: Process Expression Images To Transparent Outputs

**Files:**
- Create: `src/CastoPet/Assets/CandidateSet/Transparent/Expressions/*.png`
- Create: `src/CastoPet/Assets/CandidateSet/expressions-preview.png`
- Create: `src/CastoPet/Assets/CandidateSet/full-preview.png`

- [x] **Step 1: Remove chroma key and resize expression files**

Run this PowerShell command to process every expression source PNG:

```powershell
$python = 'C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
$helper = 'C:\Users\lemon\.codex\skills\.system\imagegen\scripts\remove_chroma_key.py'
$sourceRoot = Resolve-Path 'src\CastoPet\Assets\CandidateSet\Source'
$outRoot = Resolve-Path 'src\CastoPet\Assets\CandidateSet'
Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'Expressions') -Recurse -Filter *.png |
    ForEach-Object {
        $relative = [System.IO.Path]::GetRelativePath($sourceRoot, $_.FullName)
        $out = Join-Path (Join-Path $outRoot 'Transparent') $relative
        $tmp = [System.IO.Path]::ChangeExtension($out, '.tmp.png')
        New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($out)) | Out-Null
        & $python $helper --input $_.FullName --out $tmp --auto-key border --soft-matte --transparent-threshold 12 --opaque-threshold 220 --despill
        & $python -c "from PIL import Image; from pathlib import Path; src=Path(r'$tmp'); out=Path(r'$out'); out.parent.mkdir(parents=True, exist_ok=True); im=Image.open(src).convert('RGBA'); im.thumbnail((320,320), Image.Resampling.LANCZOS); canvas=Image.new('RGBA',(320,320),(0,0,0,0)); canvas.paste(im,((320-im.width)//2,(320-im.height)//2),im); canvas.save(out); src.unlink()"
    }
```

- [x] **Step 2: Validate all candidate files**

Run:

```powershell
& 'C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' tools\candidate-assets\validate_candidate_set.py --mode all
```

Expected:

```text
PASS validated 28 candidate PNG files
```

- [x] **Step 3: Generate expression contact sheet**

Run:

```powershell
& 'C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' tools\candidate-assets\make_contact_sheet.py --input src\CastoPet\Assets\CandidateSet\Transparent\Expressions --out src\CastoPet\Assets\CandidateSet\expressions-preview.png
```

Expected: `PASS wrote src\CastoPet\Assets\CandidateSet\expressions-preview.png`.

- [x] **Step 4: Generate full contact sheet**

Run:

```powershell
& 'C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' tools\candidate-assets\make_contact_sheet.py --input src\CastoPet\Assets\CandidateSet\Transparent --out src\CastoPet\Assets\CandidateSet\full-preview.png
```

Expected: `PASS wrote src\CastoPet\Assets\CandidateSet\full-preview.png`.

- [x] **Step 5: Commit**

```powershell
git add src/CastoPet/Assets/CandidateSet tools/candidate-assets
git commit -m "art: add candidate Castorice expression sprites"
```

## Task 8: Final Verification

**Files:**
- Modify only files required to fix failed checks.

- [x] **Step 1: Run candidate validator**

Run:

```powershell
& 'C:\Users\lemon\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' tools\candidate-assets\validate_candidate_set.py --mode all
```

Expected:

```text
PASS validated 28 candidate PNG files
```

- [x] **Step 2: Run app tests**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: every test prints `PASS`.

- [x] **Step 3: Build release**

Run:

```powershell
dotnet build CastoPet.sln -c Release
```

Expected: `0 个警告`, `0 个错误`.

- [x] **Step 4: Confirm production resources were not changed**

Run:

```powershell
git status --short
```

Expected: no modified files under these production paths unless the user explicitly approved replacement:

```text
src/CastoPet/Assets/Castorice.png
src/CastoPet/Assets/States/
src/CastoPet/CastoPet.csproj
src/CastoPet/Core/
src/CastoPet/PetWindow.xaml
src/CastoPet/PetWindow.xaml.cs
```

- [x] **Step 5: Show preview paths to the user**

Report these files:

```text
src/CastoPet/Assets/CandidateSet/core-preview.png
src/CastoPet/Assets/CandidateSet/expressions-preview.png
src/CastoPet/Assets/CandidateSet/full-preview.png
```

Do not replace production resources in this task.
