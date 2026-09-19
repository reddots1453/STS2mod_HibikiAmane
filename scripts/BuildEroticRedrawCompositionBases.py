from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps


MOD_ROOT = Path(__file__).resolve().parents[1]
ORIGINAL_ROOT = Path(r"D:\game_backup\butter\魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1\www\img")
ROOT = MOD_ROOT / "图片素材" / "第一批卡图V3试制" / "EroticRedrawV2_20260913"
REFERENCES = ROOT / "references"
OUTPUT = ROOT / "composition_bases"


def crop_event_panel(name: str) -> Image.Image:
    with Image.open(REFERENCES / name) as source:
        image = source.convert("RGB")
    # Original event CGs share a 1024x768 screen with an 804x604 inner illustration.
    panel = image.crop((110, 14, 914, 618))
    return panel.resize((1000, 760), Image.Resampling.LANCZOS)


def cubic_bezier(
    start: tuple[float, float],
    control_a: tuple[float, float],
    control_b: tuple[float, float],
    end: tuple[float, float],
    steps: int = 40,
) -> list[tuple[float, float]]:
    points = []
    for step in range(steps + 1):
        t = step / steps
        inverse = 1.0 - t
        x = (
            inverse**3 * start[0]
            + 3 * inverse**2 * t * control_a[0]
            + 3 * inverse * t**2 * control_b[0]
            + t**3 * end[0]
        )
        y = (
            inverse**3 * start[1]
            + 3 * inverse**2 * t * control_a[1]
            + 3 * inverse * t**2 * control_b[1]
            + t**3 * end[1]
        )
        points.append((x, y))
    return points


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)

    mappings = {"074_PleasureGarden_base.png": "Eve_18_0001.png"}
    for output_name, source_name in mappings.items():
        crop_event_panel(source_name).save(OUTPUT / output_name)

    with Image.open(MOD_ROOT / "图片素材" / "完成版卡图" / "019_BindingInsight_绳缚的心得.png") as source:
        accepted = source.convert("RGB")
    accepted.save(OUTPUT / "058_MasochisticTrance_clean_base.png")

    trance = ImageOps.mirror(accepted).convert("RGBA")
    overlay = Image.new("RGBA", trance.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    draw.ellipse((105, 55, 910, 735), outline=(90, 230, 255, 190), width=12)
    draw.arc((120, 70, 895, 720), 205, 345, fill=(195, 246, 255, 230), width=8)
    for x, y in ((520, 420), (600, 500), (470, 565), (665, 585)):
        draw.ellipse((x - 18, y - 18, x + 18, y + 18), outline=(220, 65, 245, 230), width=7)
        draw.line((x - 10, y, x + 10, y), fill=(220, 65, 245, 220), width=4)
        draw.line((x, y - 10, x, y + 10), fill=(220, 65, 245, 220), width=4)
    Image.alpha_composite(trance, overlay).convert("RGB").save(OUTPUT / "058_MasochisticTrance_base.png")

    bite = accepted.crop((0, 70, 700, 602)).resize((1000, 760), Image.Resampling.LANCZOS)
    overlay = Image.new("RGBA", bite.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    points = cubic_bezier((980, 610), (800, 585), (600, 535), (438, 520))
    draw.line(points, fill=(57, 18, 79, 235), width=48, joint="curve")
    draw.line(points, fill=(154, 63, 190, 190), width=12, joint="curve")
    bite = Image.alpha_composite(bite.convert("RGBA"), overlay).convert("RGB")
    bite.save(OUTPUT / "076_BiteInvader_base.png")

    def dark_gradient() -> Image.Image:
        image = Image.new("RGB", (1000, 760))
        pixels = image.load()
        for y in range(760):
            t = y / 759
            for x in range(1000):
                side = abs(x - 500) / 500
                pixels[x, y] = (
                    int(22 + 18 * t + 10 * side),
                    int(16 + 10 * t),
                    int(45 + 26 * t + 8 * (1 - side)),
                )
        return image

    # Clean semantic guide for Ecstasy Dew.  It deliberately contains only one
    # bottle and three rising drops so the diffusion pass cannot reinterpret a
    # second stopper or a magic circle as additional vessels.
    dew = dark_gradient().convert("RGBA")
    dew_glow = Image.new("RGBA", dew.size, (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(dew_glow)
    glow_draw.ellipse((230, 105, 760, 700), fill=(255, 55, 170, 92))
    dew = Image.alpha_composite(dew, dew_glow.filter(ImageFilter.GaussianBlur(70)))
    draw = ImageDraw.Draw(dew)
    bottle = [(390, 175), (610, 175), (610, 300), (715, 610), (650, 685), (350, 685), (285, 610), (390, 300)]
    draw.polygon(bottle, fill=(248, 225, 244, 80), outline=(255, 211, 242, 255), width=18)
    draw.rectangle((405, 160, 595, 315), outline=(255, 211, 242, 255), width=18)
    draw.rounded_rectangle((265, 430, 735, 675), radius=95, fill=(245, 56, 158, 210), outline=(255, 211, 242, 255), width=10)
    for x, y, radius in ((500, 115, 24), (455, 72, 17), (540, 42, 12)):
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=(255, 145, 208, 255), outline=(255, 234, 248, 255), width=5)
    dew.convert("RGB").save(OUTPUT / "050_EcstasyDewClean_base.png")

    # A higher-detail single-bottle guide cropped from the already accepted
    # local object-alchemy composition.  The crop removes its blue spell circle
    # and leaves enough surrounding pink energy for img2img to repaint.
    object_alchemy = OUTPUT / "056_ObjectAlchemy_base.png"
    if object_alchemy.exists():
        with Image.open(object_alchemy) as source:
            bottle_crop = source.convert("RGB").crop((0, 180, 600, 760))
        bottle_crop = ImageOps.pad(
            bottle_crop,
            (1000, 760),
            method=Image.Resampling.LANCZOS,
            color=(24, 16, 47),
            centering=(0.46, 0.5),
        )
        bottle_crop.save(OUTPUT / "050_EcstasyDewHighDetail_base.png")

    whip = dark_gradient().convert("RGBA")
    glow = Image.new("RGBA", whip.size, (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    whip_curve = cubic_bezier((140, 650), (310, 615), (260, 300), (500, 340))
    whip_curve += cubic_bezier((500, 340), (690, 390), (665, 160), (810, 220))[1:]
    glow_draw.line(whip_curve, fill=(255, 20, 190, 180), width=64, joint="curve")
    glow_draw.ellipse((720, 130, 900, 310), outline=(60, 225, 255, 190), width=28)
    whip = Image.alpha_composite(whip, glow.filter(ImageFilter.GaussianBlur(28)))
    draw = ImageDraw.Draw(whip)
    draw.rounded_rectangle((65, 612, 170, 688), radius=22, fill=(42, 25, 52, 255), outline=(217, 76, 188, 255), width=8)
    draw.line(whip_curve, fill=(39, 10, 48, 255), width=42, joint="curve")
    draw.line(whip_curve, fill=(255, 73, 198, 255), width=12, joint="curve")
    draw.ellipse((728, 138, 892, 302), outline=(27, 15, 37, 255), width=34)
    draw.arc((735, 145, 885, 295), 20, 160, fill=(255, 68, 200, 255), width=9)
    for line in (((792, 150), (815, 205)), ((855, 245), (900, 275)), ((748, 244), (713, 280))):
        draw.line(line, fill=(98, 238, 255, 255), width=10)
    draw.line(((810, 112), (810, 58)), fill=(100, 240, 255, 255), width=10)
    draw.line(((690, 220), (640, 220)), fill=(100, 240, 255, 255), width=10)
    draw.line(((930, 220), (980, 220)), fill=(100, 240, 255, 255), width=10)
    whip.convert("RGB").save(OUTPUT / "073_DesireWhip_base.png")

    appetite = dark_gradient().convert("RGBA")
    glow = Image.new("RGBA", appetite.size, (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    glow_draw.ellipse((95, 115, 405, 425), fill=(155, 40, 235, 130))
    glow_draw.ellipse((570, 280, 900, 650), fill=(98, 255, 105, 150))
    appetite = Image.alpha_composite(appetite, glow.filter(ImageFilter.GaussianBlur(55)))
    draw = ImageDraw.Draw(appetite)
    draw.ellipse((145, 165, 355, 375), fill=(12, 9, 24, 255), outline=(127, 48, 175, 255), width=14)
    for line in (((250, 175), (220, 245), (275, 275)), ((165, 275), (230, 290), (200, 350)), ((325, 235), (270, 255), (300, 365))):
        draw.line(line, fill=(228, 95, 255, 255), width=8)
    essence = cubic_bezier((350, 280), (460, 230), (500, 505), (655, 450))
    draw.line(essence, fill=(255, 247, 233, 255), width=42, joint="curve")
    draw.line(essence, fill=(255, 255, 255, 255), width=12, joint="curve")
    draw.ellipse((640, 360, 770, 500), fill=(111, 248, 117, 255), outline=(220, 255, 179, 255), width=10)
    draw.ellipse((735, 360, 865, 500), fill=(111, 248, 117, 255), outline=(220, 255, 179, 255), width=10)
    draw.polygon(((640, 425), (865, 425), (753, 620)), fill=(69, 218, 92, 255), outline=(220, 255, 179, 255))
    draw.line(((753, 385), (753, 595)), fill=(242, 255, 194, 220), width=8)
    appetite.convert("RGB").save(OUTPUT / "075_SemenAppetite_base.png")

    # Semen Appetite uses its accepted power icon as a semantic anchor.  The
    # guide adds a continuous pearly stream and vitality aura while leaving the
    # local diffusion model to repaint every visible surface.
    appetite_mouth = dark_gradient().convert("RGBA")
    aura = Image.new("RGBA", appetite_mouth.size, (0, 0, 0, 0))
    aura_draw = ImageDraw.Draw(aura)
    aura_draw.ellipse((350, 70, 980, 720), fill=(130, 245, 110, 88))
    appetite_mouth = Image.alpha_composite(appetite_mouth, aura.filter(ImageFilter.GaussianBlur(75)))
    icon_path = (
        MOD_ROOT
        / "图片素材"
        / "状态图标"
        / "正式素材"
        / "Power图标"
        / "256x256"
        / "semen_appetite_power_big.png"
    )
    with Image.open(icon_path) as source:
        mouth = source.convert("RGBA").resize((570, 570), Image.Resampling.LANCZOS)
    appetite_mouth.alpha_composite(mouth, (385, 95))
    stream_layer = Image.new("RGBA", appetite_mouth.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(stream_layer)
    stream = cubic_bezier((30, 610), (210, 610), (330, 510), (520, 415))
    draw.line(stream, fill=(255, 247, 233, 255), width=62, joint="curve")
    draw.line(stream, fill=(255, 255, 255, 255), width=20, joint="curve")
    appetite_mouth = Image.alpha_composite(appetite_mouth, stream_layer)
    appetite_mouth.convert("RGB").save(OUTPUT / "075_SemenAppetiteMouth_base.png")

    # Desire Whip V4: preserve a real source monster silhouette, then let img2img
    # repaint the whip impact instead of asking the model to invent anatomy.
    desire_whip = dark_gradient().convert("RGBA")
    with Image.open(ORIGINAL_ROOT / "enemies" / "crimsontowers-battler_tentaclemonster.png") as source:
        monster = source.convert("RGBA")
    monster = monster.crop(monster.getbbox())
    monster.thumbnail((455, 500), Image.Resampling.LANCZOS)
    monster_layer = Image.new("RGBA", desire_whip.size, (0, 0, 0, 0))
    monster_layer.alpha_composite(monster, (515, 145))
    desire_whip = Image.alpha_composite(desire_whip, monster_layer)
    whip_layer = Image.new("RGBA", desire_whip.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(whip_layer)
    curve = cubic_bezier((70, 660), (250, 610), (265, 325), (565, 340))
    draw.line(curve, fill=(247, 52, 193, 255), width=54, joint="curve")
    draw.line(curve, fill=(42, 8, 55, 255), width=32, joint="curve")
    draw.ellipse((505, 282, 635, 412), outline=(93, 235, 255, 245), width=16)
    draw.line(((570, 270), (570, 215)), fill=(93, 235, 255, 245), width=10)
    draw.line(((500, 348), (445, 348)), fill=(93, 235, 255, 245), width=10)
    draw.line(((640, 348), (695, 348)), fill=(93, 235, 255, 245), width=10)
    desire_whip = Image.alpha_composite(desire_whip, whip_layer)
    desire_whip.convert("RGB").save(OUTPUT / "073_DesireWhipMonster_base.png")

    # Clean semantic guide for Desire Whip: retain the real source monster but
    # omit the old circular reticle, which the model repeatedly treated as the
    # subject instead of an impact effect.
    clean_whip = dark_gradient().convert("RGBA")
    with Image.open(ORIGINAL_ROOT / "enemies" / "crimsontowers-battler_tentaclemonster.png") as source:
        clean_monster = source.convert("RGBA")
    clean_monster = clean_monster.crop(clean_monster.getbbox())
    clean_scale = min(560 / clean_monster.width, 590 / clean_monster.height)
    clean_monster = clean_monster.resize(
        (int(clean_monster.width * clean_scale), int(clean_monster.height * clean_scale)),
        Image.Resampling.LANCZOS,
    )
    monster_layer = Image.new("RGBA", clean_whip.size, (0, 0, 0, 0))
    monster_layer.alpha_composite(clean_monster, (410, 85))
    clean_whip = Image.alpha_composite(clean_whip, monster_layer)
    guide = Image.new("RGBA", clean_whip.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(guide)
    clean_curve = cubic_bezier((45, 690), (250, 650), (280, 330), (585, 365))
    draw.line(clean_curve, fill=(255, 70, 203, 255), width=58, joint="curve")
    draw.line(clean_curve, fill=(35, 8, 49, 255), width=34, joint="curve")
    for end in ((585, 290), (655, 365), (585, 440), (515, 365)):
        draw.line(((585, 365), end), fill=(105, 238, 255, 230), width=9)
    clean_whip = Image.alpha_composite(clean_whip, guide)
    clean_whip.convert("RGB").save(OUTPUT / "073_DesireWhipMonsterClean_base.png")

    # Bite V4: construct the exact original-game pose from independent layers.
    # This provides coherent arms, shoulders, jaw and mouth-tentacle contact before
    # the local model performs only a restrained style repaint.
    pictures = ORIGINAL_ROOT / "pictures"
    bite_layers = (
        "actor01_pose09_body_0005.png",
        "actor01_pose09_cloth_0004.png",
        "actor01_pose09_face_0040.png",
        "actor01_pose09_sexual_mouthtentacle.png",
    )
    composite = Image.new("RGBA", (922, 922), (0, 0, 0, 0))
    for layer_name in bite_layers:
        with Image.open(pictures / layer_name) as source:
            layer = source.convert("RGBA")
        composite = Image.alpha_composite(composite, layer)
    crop = composite.crop((250, 210, 750, 590)).resize((1000, 760), Image.Resampling.LANCZOS)
    bite_background = dark_gradient().convert("RGBA")
    bite_background.alpha_composite(crop)
    bite_background.convert("RGB").save(OUTPUT / "076_BiteOriginalLayers_base.png")

    bite_mask = Image.new("L", (1000, 760), 0)
    mask_draw = ImageDraw.Draw(bite_mask)
    mask_draw.ellipse((500, 295, 850, 485), fill=255)
    bite_mask = bite_mask.filter(ImageFilter.GaussianBlur(15))
    bite_mask.save(OUTPUT / "076_BiteMouth_mask.png")

    # Bite V5 close-up: retain only the original eyes, mouth, jaw and the exact
    # tentacle contact.  The raised arms and hands lie outside this crop, so the
    # redraw cannot accidentally turn them into competing focal anatomy.
    bite_heroine = Image.new("RGBA", (922, 922), (0, 0, 0, 0))
    for layer_name in bite_layers[:3]:
        with Image.open(pictures / layer_name) as source:
            layer = source.convert("RGBA")
        bite_heroine = Image.alpha_composite(bite_heroine, layer)
    heroine_mask = Image.new("L", bite_heroine.size, 0)
    mask_draw = ImageDraw.Draw(heroine_mask)
    # Central head/neck/upper-torso silhouette only; the original raised arms are
    # outside this contour and are deliberately discarded.
    mask_draw.ellipse((315, 230, 640, 610), fill=255)
    mask_draw.rectangle((0, 0, 922, 255), fill=0)
    bite_heroine.putalpha(ImageChops.multiply(bite_heroine.getchannel("A"), heroine_mask))
    with Image.open(pictures / "actor01_pose09_sexual_mouthtentacle.png") as source:
        bite_tentacle = source.convert("RGBA")
    bite_face_composite = Image.alpha_composite(bite_heroine, bite_tentacle)
    bite_face = bite_face_composite.crop((245, 215, 700, 561)).resize(
        (1000, 760), Image.Resampling.LANCZOS
    )
    bite_face_background = dark_gradient().convert("RGBA")
    bite_face_background.alpha_composite(bite_face)
    bite_face_background.convert("RGB").save(OUTPUT / "076_BiteFaceOnly_base.png")

    # Alternative Bite base: start from the accepted Reflective Barrier face and
    # cel shading, then add only the original mouth-contact tentacle.  This avoids
    # inheriting the raised arms from pose09 while giving img2img a high-quality
    # style and identity anchor from the outset.
    with Image.open(MOD_ROOT / "图片素材" / "完成版卡图" / "065_ReflectiveBarrier_反射屏障.png") as source:
        barrier = source.convert("RGBA")
    bite_style_face = ImageOps.fit(
        barrier.crop((250, 80, 780, 480)),
        (1000, 760),
        Image.Resampling.LANCZOS,
    )
    main_tentacle = bite_tentacle.crop((420, 300, 760, 470)).resize(
        (550, 275), Image.Resampling.LANCZOS
    )
    bite_style_face.alpha_composite(main_tentacle, (540, 163))
    bite_style_face.convert("RGB").save(OUTPUT / "076_BiteBarrierStyle_base.png")

    # Masochistic Trance V4: use a different complete original-game pose so the
    # card does not reuse Binding Insight's composition.  The source silhouette
    # locks every limb before the restrained local repaint.
    with Image.open(pictures / "actor01_pose04_stand00.png") as source:
        trance_pose = source.convert("RGBA")
    bbox = trance_pose.getbbox()
    if bbox is None:
        raise ValueError("actor01_pose04_stand00.png has no visible content")
    trance_pose = trance_pose.crop(bbox)
    trance_pose.thumbnail((650, 730), Image.Resampling.LANCZOS)
    trance_background = dark_gradient().convert("RGBA")
    trance_background.alpha_composite(
        trance_pose,
        ((1000 - trance_pose.width) // 2, 15 + (730 - trance_pose.height) // 2),
    )
    trance_background.convert("RGB").save(OUTPUT / "058_MasochisticTranceOriginalPose_base.png")

    # Masochistic Trance V5: compose the heroine from the original game's
    # independent body/outfit/expression/chain layers.  Keeping the scene to one
    # adult figure removes the malformed second body while preserving the exact
    # raised-arm anatomy and the card's restraint theme.
    trance_layers = (
        "actor01_pose04_body_0005.png",
        "actor01_pose04_cloth_0004.png",
        "actor01_pose04_face_0040.png",
        "actor01_pose04_sexual_chain.png",
    )
    trance_composite = Image.new("RGBA", (922, 922), (0, 0, 0, 0))
    for layer_name in trance_layers:
        with Image.open(pictures / layer_name) as source:
            layer = source.convert("RGBA")
        trance_composite = Image.alpha_composite(trance_composite, layer)
    trance_bbox = trance_composite.getbbox()
    if trance_bbox is None:
        raise ValueError("Masochistic Trance V5 layers have no visible content")
    trance_composite = trance_composite.crop(trance_bbox)
    trance_composite.thumbnail((610, 730), Image.Resampling.LANCZOS)
    trance_solo_background = dark_gradient().convert("RGBA")
    trance_solo_background.alpha_composite(
        trance_composite,
        ((1000 - trance_composite.width) // 2, 12 + (730 - trance_composite.height) // 2),
    )
    trance_solo_background.convert("RGB").save(OUTPUT / "058_MasochisticTranceSolo_base.png")

    print(f"wrote {len(mappings) + 11} composition bases to {OUTPUT}")


if __name__ == "__main__":
    main()
