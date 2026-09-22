from __future__ import annotations

import hashlib
import json
import shutil
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[1]
RUNTIME = PROJECT / "MaidenSuccubus" / "images"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def copy_exact(source: Path, destination: Path, expected_hash: str | None = None) -> None:
    if not source.is_file():
        raise FileNotFoundError(f"Formal visual source is missing: {source}")
    if expected_hash is not None and sha256(source) != expected_hash.upper():
        raise RuntimeError(f"Formal visual source hash differs from manifest: {source}")

    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, destination)
    if sha256(destination) != sha256(source):
        raise RuntimeError(f"Runtime visual copy differs from source: {destination}")


def sync_card_art() -> int:
    source_root = PROJECT / "图片素材" / "完成版卡图"
    manifest = json.loads((source_root / "manifest.json").read_text(encoding="utf-8"))
    items = manifest["items"]
    classes = [item["class"] for item in items]
    if len(classes) != len(set(classes)):
        raise RuntimeError("Card-art manifest contains duplicate C# class names.")

    destination_root = RUNTIME / "cards"
    copy_exact(
        source_root / manifest["default"]["file"],
        destination_root / "default.png",
        manifest["default"]["sha256"],
    )
    for item in items:
        copy_exact(
            source_root / item["file"],
            destination_root / f"{item['class']}.png",
            item["sha256"],
        )
    return len(items) + 1


def sync_corruption_balance() -> int:
    source_root = PROJECT / "图片素材" / "堕落值天平"
    manifest = json.loads((source_root / "manifest.json").read_text(encoding="utf-8"))
    destination_root = RUNTIME / "ui" / "corruption"
    for state in manifest["states"].values():
        copy_exact(
            source_root / state["file"],
            destination_root / state["file"],
            state["sha256"],
        )
    return len(manifest["states"])


def sync_intents() -> int:
    source_root = PROJECT / "图片素材" / "状态图标" / "正式素材" / "意图图标"
    names = (
        "desire_gain_intent",
        "restraint_attack_intent",
        "restraint_power_intent",
        "restraint_skill_intent",
        "tear_clothing_intent",
    )
    count = 0
    for size, suffix in (("64x64", ""), ("256x256", "_big")):
        for name in names:
            file_name = f"{name}{suffix}.png"
            copy_exact(
                source_root / size / file_name,
                RUNTIME / "intents" / size / file_name,
            )
            count += 1
    return count


def sync_temptation() -> int:
    copy_exact(
        PROJECT
        / "图片素材"
        / "状态图标"
        / "正式素材"
        / "诱惑度UI"
        / "temptation_lipstick_64.png",
        RUNTIME / "ui" / "temptation" / "temptation_lipstick_64.png",
    )
    return 1


def sync_world_character_art() -> int:
    source_root = PROJECT / "图片素材" / "火堆与商店角色" / "正式素材"
    for file_name in ("hibiki_amane_rest_site.png", "hibiki_amane_merchant.png"):
        copy_exact(source_root / file_name, RUNTIME / "character" / file_name)
    return 2


def main() -> None:
    counts = {
        "card_art": sync_card_art(),
        "corruption_balance": sync_corruption_balance(),
        "intent_icons": sync_intents(),
        "temptation": sync_temptation(),
        "world_character_art": sync_world_character_art(),
    }
    print("Synchronized formal visual assets: " + ", ".join(
        f"{name}={count}" for name, count in counts.items()
    ))


if __name__ == "__main__":
    main()
