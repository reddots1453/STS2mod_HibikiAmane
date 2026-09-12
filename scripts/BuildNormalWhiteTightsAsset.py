from __future__ import annotations

import argparse
import colorsys
import hashlib
import shutil
import statistics
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter


CANVAS_SIZE = (922, 1250)
SOURCE_LAYER_SIZE = (922, 922)
LEG_TINT_BOX = (420, 675, 620, 1080)
LEG_TINT_RAMP_END = 735
OPTION_FADE_START = 820
OPTION_FADE_END = 922
TINT_RGB = (246, 249, 255)
TINT_STRENGTH = 0.68
FOOT_DEFRINGE_BOX = (400, 1060, 650, 1170)
FOOT_TIGHTS_BOX = (415, 940, 610, 1120)
TIGHTS_REFERENCE_BOX = (420, 850, 600, 1010)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def tint_extended_legs(image: Image.Image) -> Image.Image:
    """Continue the source tights material across the STS2-only leg extension."""
    result = image.convert("RGBA")
    pixels = result.load()
    left, top, right, bottom = LEG_TINT_BOX

    for y in range(top, min(bottom, result.height)):
        ramp = min(1.0, max(0.0, (y - top) / (LEG_TINT_RAMP_END - top)))
        strength = TINT_STRENGTH * ramp
        for x in range(left, min(right, result.width)):
            red, green, blue, alpha = pixels[x, y]
            if alpha < 16:
                continue

            hue, saturation, value = colorsys.rgb_to_hsv(
                red / 255.0,
                green / 255.0,
                blue / 255.0,
            )
            is_skin = (
                (hue <= 0.13 or hue >= 0.95)
                and 0.08 <= saturation <= 0.68
                and value >= 0.38
            )
            if not is_skin:
                continue

            pixels[x, y] = (
                round(red * (1.0 - strength) + TINT_RGB[0] * strength),
                round(green * (1.0 - strength) + TINT_RGB[1] * strength),
                round(blue * (1.0 - strength) + TINT_RGB[2] * strength),
                alpha,
            )

    return result


def fade_source_bottom(option: Image.Image) -> Image.Image:
    """Remove the source canvas's hard lower edge before joining the extension."""
    result = option.convert("RGBA")
    pixels = result.load()
    fade_height = OPTION_FADE_END - OPTION_FADE_START

    for y in range(OPTION_FADE_START, min(OPTION_FADE_END, result.height)):
        factor = 1.0 - (y - OPTION_FADE_START) / fade_height
        for x in range(result.width):
            red, green, blue, alpha = pixels[x, y]
            pixels[x, y] = (red, green, blue, round(alpha * factor))

    return result


def mask_behind_cloth(option: Image.Image, cloth: Image.Image) -> Image.Image:
    """Keep the baked school uniform pixel-identical while placing tights behind it."""
    result = option.copy()
    pixels = result.load()
    cloth_alpha = cloth.getchannel("A")
    for y in range(result.height):
        for x in range(result.width):
            if cloth_alpha.getpixel((x, y)) > 0:
                red, green, blue, _ = pixels[x, y]
                pixels[x, y] = (red, green, blue, 0)
    return result


def remove_foot_halo(image: Image.Image) -> Image.Image:
    """Replace white matte contamination around shoes with brown edge colours."""
    result = image.copy()
    source = image.copy()
    pixels = result.load()
    source_pixels = source.load()
    left, top, right, bottom = FOOT_DEFRINGE_BOX

    for y in range(top, min(bottom, image.height)):
        for x in range(left, min(right, image.width)):
            red, green, blue, alpha = source_pixels[x, y]
            if alpha == 0 or min(red, green, blue) < 150:
                continue
            if max(red, green, blue) - min(red, green, blue) > 60:
                continue

            touches_transparency = False
            for ny in range(max(0, y - 2), min(image.height, y + 3)):
                for nx in range(max(0, x - 2), min(image.width, x + 3)):
                    if source_pixels[nx, ny][3] < 16:
                        touches_transparency = True
                        break
                if touches_transparency:
                    break
            if not touches_transparency:
                continue

            nearest_shoe: tuple[int, int, int, int] | None = None
            nearest_distance = 10_000
            for ny in range(max(0, y - 10), min(image.height, y + 11)):
                for nx in range(max(0, x - 10), min(image.width, x + 11)):
                    nr, ng, nb, na = source_pixels[nx, ny]
                    luminance = 0.2126 * nr + 0.7152 * ng + 0.0722 * nb
                    if na < 220 or luminance >= 160:
                        continue
                    distance = (nx - x) ** 2 + (ny - y) ** 2
                    if distance < nearest_distance:
                        nearest_distance = distance
                        nearest_shoe = (nr, ng, nb, na)

            if nearest_shoe is not None:
                pixels[x, y] = (*nearest_shoe[:3], alpha)

    return result


def harmonize_feet_with_tights(
    image: Image.Image,
    bare_legs: Image.Image,
) -> Image.Image:
    """Continue the tights colour over the original ankle-sock artwork.

    The STS2 extension originally preserved the source portrait's opaque white
    ankle socks below a newly tinted, translucent-white leg.  The resulting
    cool-white foot and warm-white calf met at a visible cuff.  Use the bare-leg
    source only as a classification mask, then match those sock pixels to the
    actual tights palette already present immediately above them.
    """
    result = image.copy()
    before = image.copy()
    mask_source = bare_legs.convert("RGBA")
    pixels = result.load()
    before_pixels = before.load()
    mask_pixels = mask_source.load()

    def is_skin(red: int, green: int, blue: int, alpha: int) -> bool:
        if alpha < 32:
            return False
        hue, saturation, value = colorsys.rgb_to_hsv(
            red / 255.0,
            green / 255.0,
            blue / 255.0,
        )
        return (
            (hue <= 0.13 or hue >= 0.95)
            and 0.08 <= saturation <= 0.68
            and value >= 0.38
        )

    def is_sock(red: int, green: int, blue: int, alpha: int) -> bool:
        if alpha < 32:
            return False
        _, saturation, value = colorsys.rgb_to_hsv(
            red / 255.0,
            green / 255.0,
            blue / 255.0,
        )
        return saturation <= 0.20 and value >= 0.38

    reference_colours: list[tuple[int, int, int]] = []
    left, top, right, bottom = TIGHTS_REFERENCE_BOX
    for y in range(top, bottom):
        for x in range(left, right):
            mask_pixel = mask_pixels[x, y]
            if is_skin(*mask_pixel):
                red, green, blue, alpha = before_pixels[x, y]
                if alpha >= 128:
                    reference_colours.append((red, green, blue))

    sock_colours: list[tuple[int, int, int]] = []
    left, top, right, bottom = FOOT_TIGHTS_BOX
    for y in range(top, bottom):
        for x in range(left, right):
            mask_pixel = mask_pixels[x, y]
            if is_sock(*mask_pixel):
                red, green, blue, alpha = before_pixels[x, y]
                if alpha >= 128:
                    sock_colours.append((red, green, blue))

    if not reference_colours or not sock_colours:
        raise RuntimeError("Unable to sample tights and foot palettes.")

    target = tuple(
        round(statistics.median(colour[channel] for colour in reference_colours))
        for channel in range(3)
    )
    sock_median = tuple(
        round(statistics.median(colour[channel] for colour in sock_colours))
        for channel in range(3)
    )
    shift = tuple(target[channel] - sock_median[channel] for channel in range(3))

    for y in range(top, bottom):
        for x in range(left, right):
            mask_pixel = mask_pixels[x, y]
            if not is_sock(*mask_pixel):
                continue
            red, green, blue, alpha = pixels[x, y]
            if alpha < 16:
                continue

            # Preserve local folds and highlights while moving the complete
            # ankle/foot material into the same warm-white range as the tights.
            pixels[x, y] = (
                max(0, min(255, red + shift[0])),
                max(0, min(255, green + shift[1])),
                max(0, min(255, blue + shift[2])),
                alpha,
            )

    # Build the old cuff seam from the source garment classes and soften only
    # that narrow interior band.  A feathered mask avoids per-column striping;
    # eroding the alpha silhouette keeps the outer ankle edge crisp.
    skin_mask = Image.new("L", result.size, 0)
    sock_mask = Image.new("L", result.size, 0)
    skin_mask_pixels = skin_mask.load()
    sock_mask_pixels = sock_mask.load()
    for y in range(940, 1070):
        for x in range(420, 600):
            mask_pixel = mask_pixels[x, y]
            if is_skin(*mask_pixel):
                skin_mask_pixels[x, y] = 255
            if is_sock(*mask_pixel):
                sock_mask_pixels[x, y] = 255

    seam_mask = ImageChops.multiply(
        skin_mask.filter(ImageFilter.MaxFilter(13)),
        sock_mask.filter(ImageFilter.MaxFilter(13)),
    )
    interior_mask = result.getchannel("A").filter(ImageFilter.MinFilter(5))
    seam_mask = ImageChops.multiply(seam_mask, interior_mask)
    seam_mask = seam_mask.filter(ImageFilter.GaussianBlur(1.6))
    softened = result.filter(ImageFilter.GaussianBlur(2.2))
    result = Image.composite(softened, result, seam_mask)

    return result


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Build the school-uniform portrait with original white tights."
    )
    parser.add_argument(
        "--source",
        type=Path,
        default=Path(
            r"D:\game_backup\butter\魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1"
            r"\www\img\pictures\actor01_pose01_option_0012.png"
        ),
        help="Original-game PID 12 white-tights layer.",
    )
    parser.add_argument(
        "--character-dir",
        type=Path,
        default=Path(__file__).resolve().parents[1]
        / "MaidenSuccubus"
        / "images"
        / "character",
    )
    args = parser.parse_args()

    character_dir = args.character_dir
    output_path = character_dir / "character_normal.png"
    base_path = character_dir / "character_normal_bare_legs.png"
    local_option_path = character_dir / "white_tights_option_0012.png"
    cloth_path = character_dir / "cloth_0001.png"

    if not args.source.exists():
        raise FileNotFoundError(args.source)
    if not output_path.exists():
        raise FileNotFoundError(output_path)
    if not base_path.exists():
        shutil.copyfile(output_path, base_path)
    shutil.copyfile(args.source, local_option_path)

    base = Image.open(base_path).convert("RGBA")
    option = Image.open(local_option_path).convert("RGBA")
    cloth = Image.open(cloth_path).convert("RGBA")
    if base.size != CANVAS_SIZE:
        raise ValueError(f"Expected {base_path.name} to be {CANVAS_SIZE}.")
    if option.size != SOURCE_LAYER_SIZE or cloth.size != SOURCE_LAYER_SIZE:
        raise ValueError("Expected source option and school uniform layers to be 922x922.")

    result = tint_extended_legs(base)
    option = mask_behind_cloth(fade_source_bottom(option), cloth)
    result.alpha_composite(option, (0, 0))
    result = remove_foot_halo(result)
    result = harmonize_feet_with_tights(result, base)
    result.save(output_path, format="PNG", optimize=True)

    if sha256(args.source) != sha256(local_option_path):
        raise RuntimeError("Local PID 12 layer differs from the original source file.")

    print(
        "Built character_normal.png at 922x1250 with the original PID 12 "
        "white-tights layer and a seamless lower-leg extension."
    )


if __name__ == "__main__":
    main()
