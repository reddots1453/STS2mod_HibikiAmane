from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageOps


def main() -> None:
    parser = argparse.ArgumentParser(description="Crop and resize a generated card illustration to the STS2 art canvas.")
    parser.add_argument("source", type=Path)
    parser.add_argument("target", type=Path)
    parser.add_argument("--crop", nargs=4, type=int, metavar=("LEFT", "TOP", "RIGHT", "BOTTOM"))
    args = parser.parse_args()

    with Image.open(args.source) as source:
        source = source.convert("RGB")
        if args.crop:
            source = source.crop(tuple(args.crop))
        image = ImageOps.fit(
            source,
            (1000, 760),
            method=Image.Resampling.LANCZOS,
            centering=(0.5, 0.48),
        )

    args.target.parent.mkdir(parents=True, exist_ok=True)
    image.save(args.target, format="PNG", optimize=True)
    print(f"wrote {args.target} ({image.width}x{image.height})")


if __name__ == "__main__":
    main()
