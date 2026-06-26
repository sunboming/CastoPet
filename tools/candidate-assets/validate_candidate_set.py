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
