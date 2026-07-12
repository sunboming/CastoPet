from __future__ import annotations

import argparse
from pathlib import Path
from typing import Iterable

from PIL import Image


DEFAULT_ROOT = Path("src/CastoPet/Assets/Runtime/Castorice")
DEFAULT_EXCLUDED_PARTS = {
    "CandidateSet",
    "_green-clean-backup",
    "source",
}


def is_candidate_file(path: Path) -> bool:
    if path.suffix.lower() != ".png":
        return False
    if path.name.endswith("-preview.png"):
        return False
    return not any(part in DEFAULT_EXCLUDED_PARTS for part in path.parts)


def iter_pngs(root: Path) -> Iterable[Path]:
    for path in sorted(root.rglob("*.png")):
        if is_candidate_file(path):
            yield path


def is_green_fringe_pixel(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, a = pixel
    return (
        a > 8
        and g >= 80
        and g > r + 22
        and g > b + 22
        and g - max(r, b) >= 28
    )


def is_edge_pixel(pixels, x: int, y: int, width: int, height: int) -> bool:
    if pixels[x, y][3] < 245:
        return True

    for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
        if 0 <= nx < width and 0 <= ny < height and pixels[nx, ny][3] <= 8:
            return True

    return False


def replacement_rgb(pixels, x: int, y: int, width: int, height: int) -> tuple[int, int, int]:
    samples: list[tuple[int, int, int]] = []
    for radius in (1, 2, 3):
        samples.clear()
        for ny in range(max(0, y - radius), min(height, y + radius + 1)):
            for nx in range(max(0, x - radius), min(width, x + radius + 1)):
                if nx == x and ny == y:
                    continue
                r, g, b, a = pixels[nx, ny]
                if a <= 24 or is_green_fringe_pixel((r, g, b, a)):
                    continue
                samples.append((r, g, b))

        if samples:
            count = len(samples)
            return clamp_green(
                sum(r for r, _, _ in samples) // count,
                sum(g for _, g, _ in samples) // count,
                sum(b for _, _, b in samples) // count,
            )

    r, g, b, _ = pixels[x, y]
    return clamp_green(r, g, b)


def clamp_green(r: int, g: int, b: int) -> tuple[int, int, int]:
    if g >= 80 and g > r + 22 and g > b + 22 and g - max(r, b) >= 28:
        g = min(g, max(r, b) + 8)

    return r, g, b


def clean_image(path: Path, apply: bool) -> tuple[int, int]:
    image = Image.open(path).convert("RGBA")
    pixels = image.load()
    width, height = image.size
    targets: list[tuple[int, int]] = []

    for y in range(height):
        for x in range(width):
            pixel = pixels[x, y]
            if is_green_fringe_pixel(pixel) and is_edge_pixel(pixels, x, y, width, height):
                targets.append((x, y))

    if apply and targets:
        for x, y in targets:
            _, _, _, alpha = pixels[x, y]
            r, g, b = replacement_rgb(pixels, x, y, width, height)
            pixels[x, y] = (r, g, b, alpha)
        image.save(path)

    return len(targets), width * height


def run(root: Path, apply: bool, min_pixels: int) -> int:
    rows: list[tuple[int, Path]] = []
    for path in iter_pngs(root):
        count, _ = clean_image(path, apply)
        if count >= min_pixels:
            rows.append((count, path))

    rows.sort(reverse=True)
    action = "cleaned" if apply else "flagged"
    print(f"{action} files: {len(rows)}")
    for count, path in rows:
        print(f"{count:5d}  {path}")

    return sum(count for count, _ in rows)


def self_test() -> None:
    image = Image.new("RGBA", (5, 5), (0, 0, 0, 0))
    pixels = image.load()
    pixels[2, 2] = (220, 180, 240, 255)
    pixels[2, 1] = (20, 190, 40, 180)
    tmp = Path("__green_fringe_self_test.png")
    try:
        image.save(tmp)
        before, _ = clean_image(tmp, apply=False)
        clean_image(tmp, apply=True)
        after, _ = clean_image(tmp, apply=False)
    finally:
        if tmp.exists():
            tmp.unlink()

    if before != 1 or after != 0:
        raise SystemExit(f"self-test failed: before={before}, after={after}")

    print("self-test passed")


def main() -> None:
    parser = argparse.ArgumentParser(description="Detect or remove green chroma-key fringes from CastoPet PNG cutouts.")
    parser.add_argument("--root", type=Path, default=DEFAULT_ROOT, help="PNG root to scan.")
    parser.add_argument("--apply", action="store_true", help="Write cleaned PNG files. Without this flag, only scans.")
    parser.add_argument("--min-pixels", type=int, default=1, help="Only print files with at least this many flagged pixels.")
    parser.add_argument("--self-test", action="store_true", help="Run a small synthetic test and exit.")
    args = parser.parse_args()

    if args.self_test:
        self_test()
        return

    total = run(args.root, args.apply, args.min_pixels)
    print(f"total flagged pixels: {total}")


if __name__ == "__main__":
    main()
