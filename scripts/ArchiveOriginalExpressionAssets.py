from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
import shutil
from collections import Counter, defaultdict
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


DEFAULT_GAME_ROOT = Path(
    r"D:\game_backup\butter\魔法少女天穹法妮雅 超魔改 V56.5 魔改三合一1"
)
FACE_PATTERN = re.compile(r"^actor01_pose(?P<pose>\d+)_face_(?P<variant>.+)\.png$")
EYE_PATTERN = re.compile(r"^(?:actor01_pose)?(?P<pose>\d+)_eye_(?P<variant>\d+)\.png$")
SHORT_EYE_PATTERN = re.compile(r"^(?P<pose>\d+)_(?P<variant>\d+)\.png$")
MOUTH_PATTERN = re.compile(r"^(?:actor01_pose)?(?P<pose>\d+)_mouth_(?P<variant>\d+)\.png$")
SHORT_MOUTH_PATTERN = SHORT_EYE_PATTERN
SPECIAL_MOUTH_PATTERN = re.compile(r"^actor01_pose(?P<pose>\d+)_sexual_mouth.+\.png$")

COMMON_FACE_NAMES = {
    "0002": "平静／扑克脸",
    "0005": "喜悦／战斗结束",
    "0007": "严肃",
    "0013": "战斗",
    "0014": "低生命",
    "0015": "受伤",
    "0017": "鄙视／斜视",
    "0025": "张口",
    "0031": "羞耻",
    "0032": "羞耻不悦",
    "0033": "羞耻微笑",
    "0034": "高快感",
    "0035": "发情",
    "0036": "弱绝顶",
    "0037": "余韵",
    "0038": "催眠",
    "0040": "特殊余韵",
    "0041": "强绝顶",
    "0050": "魔人",
    "0060": "高发情",
    "0061": "极高发情",
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def checked_reset(output_root: Path, mod_root: Path) -> None:
    resolved_output = output_root.resolve()
    resolved_mod = mod_root.resolve()
    if resolved_output == resolved_mod or resolved_mod not in resolved_output.parents:
        raise RuntimeError(f"Refusing to reset output outside mod root: {resolved_output}")
    if resolved_output.exists():
        shutil.rmtree(resolved_output)
    resolved_output.mkdir(parents=True)


def copy_record(
    source: Path,
    destination: Path,
    category: str,
    pose: str | None,
    variant: str | None,
    source_root: Path,
    output_root: Path,
) -> dict[str, object]:
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)
    with Image.open(source) as image:
        bbox = image.getbbox()
        width, height = image.size
        mode = image.mode
    return {
        "category": category,
        "pose": pose,
        "variant": variant,
        "known_meaning": COMMON_FACE_NAMES.get(variant or ""),
        "source": source.relative_to(source_root).as_posix(),
        "archive": destination.relative_to(output_root).as_posix(),
        "width": width,
        "height": height,
        "mode": mode,
        "bbox": list(bbox) if bbox else None,
        "empty": bbox is None,
        "sha256": sha256(source),
    }


def checkerboard(size: tuple[int, int], cell: int = 8) -> Image.Image:
    image = Image.new("RGB", size, (224, 224, 224))
    pixels = image.load()
    for y in range(size[1]):
        for x in range(size[0]):
            if (x // cell + y // cell) % 2:
                pixels[x, y] = (246, 246, 246)
    return image


def make_contact_sheet(
    records: list[dict[str, object]],
    output_root: Path,
    pose: str,
) -> None:
    non_empty = [record for record in records if not record["empty"]]

    columns = 8
    tile_width = 180
    tile_height = 150
    label_height = 24
    rows = (len(records) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * tile_width, rows * tile_height), (38, 38, 43))
    draw = ImageDraw.Draw(sheet)
    font_path = Path(r"C:\Windows\Fonts\msyh.ttc")
    font = ImageFont.truetype(str(font_path), 13) if font_path.exists() else ImageFont.load_default()

    if non_empty:
        union_left = min(record["bbox"][0] for record in non_empty)  # type: ignore[index]
        union_top = min(record["bbox"][1] for record in non_empty)  # type: ignore[index]
        union_right = max(record["bbox"][2] for record in non_empty)  # type: ignore[index]
        union_bottom = max(record["bbox"][3] for record in non_empty)  # type: ignore[index]
        margin = 12
        union_box = (
            max(0, union_left - margin),
            max(0, union_top - margin),
            union_right + margin,
            union_bottom + margin,
        )
    else:
        union_box = (0, 0, 922, 922)

    for index, record in enumerate(records):
        column = index % columns
        row = index // columns
        tile_x = column * tile_width
        tile_y = row * tile_height
        preview_height = tile_height - label_height
        background = checkerboard((tile_width, preview_height))

        if not record["empty"]:
            archived = output_root / str(record["archive"])
            with Image.open(archived) as image:
                rgba = image.convert("RGBA")
                crop_box = (
                    max(0, union_box[0]),
                    max(0, union_box[1]),
                    min(rgba.width, union_box[2]),
                    min(rgba.height, union_box[3]),
                )
                crop = rgba.crop(crop_box)
                crop.thumbnail((tile_width - 12, preview_height - 10), Image.Resampling.LANCZOS)
                px = (tile_width - crop.width) // 2
                py = (preview_height - crop.height) // 2
                background.paste(crop, (px, py), crop)

        sheet.paste(background, (tile_x, tile_y))
        variant = str(record["variant"])
        meaning = record["known_meaning"]
        label = variant if not meaning else f"{variant} {meaning}"
        if record["empty"]:
            label += " [空]"
        draw.text((tile_x + 5, tile_y + preview_height + 5), label, fill=(245, 245, 245), font=font)

    destination = output_root / "表情总览" / f"pose{pose}_表情总览.png"
    destination.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(destination, format="PNG", optimize=True)


def write_indexes(output_root: Path, records: list[dict[str, object]]) -> None:
    json_path = output_root / "表情素材索引.json"
    json_path.write_text(
        json.dumps(records, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )

    csv_path = output_root / "表情素材索引.csv"
    fields = [
        "category",
        "pose",
        "variant",
        "known_meaning",
        "source",
        "archive",
        "width",
        "height",
        "mode",
        "bbox",
        "empty",
        "sha256",
    ]
    with csv_path.open("w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields)
        writer.writeheader()
        for record in records:
            row = dict(record)
            row["bbox"] = json.dumps(row["bbox"], ensure_ascii=False)
            writer.writerow(row)


def write_readme(output_root: Path, records: list[dict[str, object]]) -> None:
    counts = Counter(str(record["category"]) for record in records)
    face_records = [record for record in records if record["category"] == "基础表情差分"]
    pose_counts = Counter(str(record["pose"]) for record in face_records)
    empty_count = sum(bool(record["empty"]) for record in face_records)

    pose_lines = "\n".join(
        f"- `pose{pose}`：{count} 张"
        for pose, count in sorted(pose_counts.items(), key=lambda item: (len(item[0]), item[0]))
    )
    known_lines = "\n".join(
        f"- `{variant}`：{meaning}" for variant, meaning in COMMON_FACE_NAMES.items()
    )
    readme = f"""# 《魔法少女天穹法妮雅》响木天音表情素材

本目录由 `scripts/ArchiveOriginalExpressionAssets.py` 从原游戏素材自动整理。源文件保持原名、透明通道和像素数据不变；`表情总览/` 仅用于浏览，不应作为正式立绘图层。

## 内容

- 基础表情差分：{counts['基础表情差分']} 张。这是 `CallStand.js` 实际通过 `actor01_poseXX_face_YYYY` 加载的面部表情层。
- 眼罩覆盖层：{counts['眼罩覆盖层']} 张。原作注释为“眼罩”，不是基础表情；同时保留短文件名与 `actor01_pose...` 别名。
- 口球覆盖层：{counts['口球覆盖层']} 张。原作注释为“口球”，不是基础表情；同时保留短文件名与 `actor01_pose...` 别名。
- 特殊嘴部覆盖层：{counts['特殊嘴部覆盖层']} 张。
- 基础表情中的透明占位文件：{empty_count} 张，仍完整保留，避免误判原作资源缺失。

基础表情按姿势分组：

{pose_lines}

原作同时存在不补零的 `pose5` 与补零的 `pose05` 两套文件，本归档保持原名并分别存放，不擅自合并。`pose06` 的51张基础表情全部是透明占位；`CallStand.js`也明确在姿势06和10隐藏基础表情，因此这不是归档遗漏。原目录没有 `actor01_pose10_face_*` 文件。

## 自动状态可确认的常用表情编号

下列语义来自原作 `CallStand.js` 的 `AutoFaceId`。其他编号多由事件脚本直接指定，不能仅凭文件名可靠命名，因此索引只记录编号，不擅自补写语义。

{known_lines}

## 使用说明

- `基础表情差分/poseXX/`：原始可合成透明图层。
- `表情总览/`：同一姿势下所有编号的带标签缩略图，便于挑选。
- `表情素材索引.json`：程序读取用，含尺寸、透明包围盒、是否为空、SHA-256 和原始相对路径。
- `表情素材索引.csv`：人工筛选用，可直接用表格软件打开。
- `表情相关覆盖层/`：眼罩、口球和特殊嘴部图层，单独存放以免与面部表情混淆。

未收录 `hyp/`：原作代码将其用于催眠事件的乳交／摆锤等 cut-in 组件，不是响木天音站立绘表情。未收录 `actor02`、`actor03`：它们属于其他角色。
"""
    (output_root / "README.md").write_text(readme, encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description="Archive every original Hibiki Amane expression layer.")
    parser.add_argument("--game-root", type=Path, default=DEFAULT_GAME_ROOT)
    parser.add_argument(
        "--output",
        type=Path,
        default=Path(__file__).resolve().parents[1] / "图片素材" / "原作表情素材",
    )
    args = parser.parse_args()

    mod_root = Path(__file__).resolve().parents[1]
    pictures = args.game_root / "www" / "img" / "pictures"
    if not pictures.is_dir():
        raise FileNotFoundError(pictures)
    checked_reset(args.output, mod_root)

    records: list[dict[str, object]] = []
    faces_by_pose: dict[str, list[dict[str, object]]] = defaultdict(list)
    for source in sorted(pictures.glob("actor01_pose*_face_*.png")):
        match = FACE_PATTERN.match(source.name)
        if not match:
            continue
        pose = match.group("pose")
        variant = match.group("variant")
        destination = args.output / "基础表情差分" / f"pose{pose}" / source.name
        record = copy_record(
            source,
            destination,
            "基础表情差分",
            pose,
            variant,
            pictures,
            args.output,
        )
        records.append(record)
        faces_by_pose[pose].append(record)

    for directory, category, long_pattern, short_pattern in (
        ("eye", "眼罩覆盖层", EYE_PATTERN, SHORT_EYE_PATTERN),
        ("mouth", "口球覆盖层", MOUTH_PATTERN, SHORT_MOUTH_PATTERN),
    ):
        source_dir = pictures / directory
        for source in sorted(source_dir.glob("*.png")):
            match = long_pattern.match(source.name) or short_pattern.match(source.name)
            pose = match.group("pose") if match else None
            variant = match.group("variant") if match else None
            destination = args.output / "表情相关覆盖层" / category / source.name
            records.append(
                copy_record(
                    source,
                    destination,
                    category,
                    pose,
                    variant,
                    pictures,
                    args.output,
                )
            )

    for source in sorted(pictures.glob("actor01_pose*_sexual_mouth*.png")):
        match = SPECIAL_MOUTH_PATTERN.match(source.name)
        pose = match.group("pose") if match else None
        destination = args.output / "表情相关覆盖层" / "特殊嘴部" / source.name
        records.append(
            copy_record(
                source,
                destination,
                "特殊嘴部覆盖层",
                pose,
                source.stem.split("_sexual_", 1)[-1],
                pictures,
                args.output,
            )
        )

    for pose, pose_records in faces_by_pose.items():
        pose_records.sort(key=lambda record: str(record["variant"]))
        make_contact_sheet(pose_records, args.output, pose)

    records.sort(
        key=lambda record: (
            str(record["category"]),
            str(record["pose"]),
            str(record["variant"]),
            str(record["archive"]),
        )
    )
    write_indexes(args.output, records)
    write_readme(args.output, records)

    face_count = sum(record["category"] == "基础表情差分" for record in records)
    if face_count != 489:
        raise RuntimeError(f"Expected 489 actor01 face layers, found {face_count}.")
    print(f"Archived {len(records)} files ({face_count} base expressions) to {args.output}")


if __name__ == "__main__":
    main()
