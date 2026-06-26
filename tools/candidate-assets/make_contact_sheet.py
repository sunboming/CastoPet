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
