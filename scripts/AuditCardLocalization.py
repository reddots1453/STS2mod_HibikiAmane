import json
import re
import sys
from difflib import SequenceMatcher
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CARDS_ROOT = ROOT / "src" / "Cards"
LOC_PATH = ROOT / "MaidenSuccubus" / "localization" / "zhs" / "cards.json"
POWER_LOC_PATH = ROOT / "MaidenSuccubus" / "localization" / "zhs" / "powers.json"
DESIGN_PATH = ROOT / "DesignDoc.md"
POWERS_ROOT = ROOT / "src" / "Powers"

BUILTIN_VARS = {
    "IfUpgraded", "OnTable", "InCombat", "IsTargeting", "TargetType",
    "GainsBlock", "IsOstyAlive", "energyPrefix", "singleStarIcon",
}
KNOWN_FORMAT_ARGS = {"upgrade", "show"}
VAR_CLASS_NAMES = {
    "DamageVar": "Damage",
    "BlockVar": "Block",
    "CardsVar": "Cards",
    "EnergyVar": "Energy",
    "HealVar": "Heal",
    "ExtraDamageVar": "ExtraDamage",
    "CalculatedDamageVar": "CalculatedDamage",
    "CalculatedBlockVar": "CalculatedBlock",
    "CalculationBaseVar": "CalculationBase",
    "CalculationExtraVar": "CalculationExtra",
    "RepeatVar": "Repeat",
    "IntVar": None,
    "StringVar": None,
    "DynamicVar": None,
}


def load_json_with_review_comments(path: Path) -> dict[str, str]:
    """Load localization JSON while preserving full-line design review notes.

    Design review comments are intentionally kept beside the affected key as
    lines beginning with //.  The game loader accepts the current file; Python's
    strict json parser does not, so the audit removes only those full lines and
    never rewrites the localization source.
    """
    text = path.read_text(encoding="utf-8-sig")
    strict_json = "\n".join(
        line for line in text.splitlines()
        if not line.lstrip().startswith("//")
    )
    return json.loads(strict_json)


def screaming_snake(name: str) -> str:
    # Preserve terminal roman-numeral/acronym runs: CounterBarrierII ->
    # COUNTER_BARRIER_II, not COUNTER_BARRIER_I_I.
    value = re.sub(r"([a-z0-9])([A-Z])", r"\1_\2", name)
    value = re.sub(r"([A-Z]+)([A-Z][a-z])", r"\1_\2", value)
    return value.upper()


def class_block(text: str, class_match: re.Match[str]) -> str:
    brace = text.find("{", class_match.end())
    if brace < 0:
        return ""
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[brace : index + 1]
    return text[brace:]


def registered_cards() -> tuple[dict[str, str], dict[str, tuple[str, str | None]]]:
    result: dict[str, str] = {}
    all_classes: dict[str, tuple[str, str | None]] = {}
    class_pattern = re.compile(
        r"(?:public|internal|protected|private)?\s*(?:abstract\s+|sealed\s+)?"
        r"class\s+(?P<name>\w+)(?:<[^>{}]+>)?"
        r"(?:\s*:\s*(?P<base>[\w<>]+))?"
    )
    registered_pattern = re.compile(
        r"\[RegisterCard\([^\]]+\)\]\s*"
        r"(?:public\s+)?sealed\s+class\s+(?P<name>\w+)"
    )
    for path in CARDS_ROOT.rglob("*.cs"):
        text = path.read_text(encoding="utf-8-sig")
        for match in class_pattern.finditer(text):
            base = match.group("base")
            if base:
                base = base.split("<", 1)[0]
            all_classes[match.group("name")] = (class_block(text, match), base)
        for match in registered_pattern.finditer(text):
            result[match.group("name")] = class_block(text, match)
    return result, all_classes


def declared_vars(block: str) -> set[str]:
    names = set(BUILTIN_VARS)
    for match in re.finditer(
        r"new\s+(?:[\w.]+\.)?PowerVar<(?:(?:[\w.]+\.)?)(?P<type>\w+)>",
        block,
    ):
        names.add(match.group("type"))
    supported = "|".join(map(re.escape, VAR_CLASS_NAMES))
    for match in re.finditer(
        rf"new\s+(?:[\w.]+\.)?(?P<class>{supported})\s*\(",
        block,
    ):
        class_name = match.group("class")
        default = VAR_CLASS_NAMES[class_name]
        if default:
            names.add(default)
    for match in re.finditer(
        rf'new\s+(?:[\w.]+\.)?(?:{supported})\s*\(\s*"(?P<name>[^"]+)"',
        block,
    ):
        names.add(match.group("name"))
    for match in re.finditer(r'SecondaryResourceVars\.ForLocal\(\s*"(?P<name>[^"]+)"', block):
        names.add(match.group("name"))
    return names


def placeholders(text: str) -> set[str]:
    result = set()
    for body in re.findall(r"\{([^{}]+)\}", text):
        name = re.split(r"[:.]", body, maxsplit=1)[0]
        if name and name not in KNOWN_FORMAT_ARGS:
            result.add(name)
    return result


def design_context(title: str, lines: list[str]) -> str | None:
    matches = [i for i, line in enumerate(lines) if line.strip() == title]
    if not matches:
        return None
    # Prefer the card-pool portion of the document.
    index = next((i for i in matches if 495 <= i <= 1600), matches[0])
    context: list[str] = []
    for line in lines[index : min(index + 7, len(lines))]:
        if context and not line.strip():
            break
        context.append(line.strip())
    return " / ".join(context)


def normalize_for_comparison(text: str) -> str:
    text = re.sub(r"\[[^\]]+\]", "", text)
    text = re.sub(r"\{[^{}]+\}", "数值", text)
    text = re.sub(r"\d+(?:/\d+)?", "数值", text)
    text = re.sub(r"^[^。；]*?费\s*", "", text)
    text = re.sub(r"升级后(?:获得|移除|变化为)[^。；]+[。；]?", "", text)
    text = re.sub(r"(?:不能被打出|无法被打出|消耗|保留|虚无|固有|沉底|随身)[。；]?", "", text)
    return re.sub(r"[\s，。；：、（）()“”\-＋+]", "", text)


def design_effect(title: str, lines: list[str]) -> str | None:
    matches = [i for i, line in enumerate(lines) if line.strip() == title]
    if not matches:
        return None
    index = next((i for i in matches if 495 <= i <= 1600), matches[0])
    candidates = []
    for line in lines[index + 1 : min(index + 8, len(lines))]:
        stripped = line.strip()
        if not stripped:
            if candidates:
                break
            continue
        if stripped.startswith(("#", "**", "（", "(")):
            continue
        candidates.append(stripped)
    if not candidates:
        return None
    # Most entries are: title / card type+rarity / cost+effect. Statuses and
    # generated options may only have one effect line.
    for candidate in candidates:
        if "费" in candidate and any(word in candidate for word in (
            "造成", "获得", "抽", "失去", "选择", "回合", "打出", "将", "进入", "持续", "挣脱"
        )):
            return candidate
    return candidates[-1]


def main() -> int:
    loc = load_json_with_review_comments(LOC_PATH)
    power_loc = load_json_with_review_comments(POWER_LOC_PATH)
    design_lines = DESIGN_PATH.read_text(encoding="utf-8-sig").splitlines()
    cards, all_classes = registered_cards()
    failures: list[str] = []
    report: list[dict[str, object]] = []

    registered_prefixes: set[str] = set()
    referenced_power_types: set[str] = set()
    for type_name, block in sorted(cards.items()):
        prefix = f"MAIDEN_SUCCUBUS_CARD_{screaming_snake(type_name)}"
        registered_prefixes.add(prefix)
        title_key = f"{prefix}.title"
        description_key = f"{prefix}.description"
        title = loc.get(title_key)
        description = loc.get(description_key)
        allows_empty_description = type_name == "DrowsyStatus"
        if not title or description is None or (
            not description and not allows_empty_description
        ):
            failures.append(f"missing localization: {type_name}: {title_key} / {description_key}")
            continue

        variables = set(BUILTIN_VARS)
        requires_selection_prompt = False
        current: str | None = type_name
        visited: set[str] = set()
        while current and current not in visited and current in all_classes:
            visited.add(current)
            current_block, current_base = all_classes[current]
            requires_selection_prompt |= "SelectionScreenPrompt" in current_block
            variables.update(declared_vars(current_block))
            referenced_power_types.update(
                match.group("type")
                for match in re.finditer(
                    r"new\s+(?:[\w.]+\.)?PowerVar<"
                    r"(?:(?:[\w.]+\.)?)(?P<type>\w+)>",
                    current_block,
                )
            )
            current = current_base
        if requires_selection_prompt:
            prompt_key = f"{prefix}.selectionScreenPrompt"
            if not loc.get(prompt_key):
                failures.append(
                    f"missing selection prompt: {type_name}: {prompt_key}"
                )
        unknown = placeholders(description) - variables
        if unknown:
            failures.append(
                f"unknown SmartFormat vars: {type_name}: {sorted(unknown)} in {description!r}"
            )
        context = design_context(title, design_lines)
        effect = design_effect(title, design_lines)
        similarity = None
        if effect:
            similarity = SequenceMatcher(
                None,
                normalize_for_comparison(description),
                normalize_for_comparison(effect),
            ).ratio()
        if context is None:
            status = "NO-DESIGN"
        else:
            status = "DESIGN"
        report.append({
            "status": status,
            "type": type_name,
            "title": title,
            "description": description,
            "designEffect": effect,
            "designContext": context,
            "similarity": similarity,
        })

    for key in sorted(loc):
        match = re.match(r"(?P<prefix>MAIDEN_SUCCUBUS_CARD_.+)\.(title|description)$", key)
        if match and match.group("prefix") not in registered_prefixes:
            failures.append(f"orphan card localization: {key}")
            report.append({"status": "ORPHAN", "key": key, "text": loc[key]})

    power_source = "\n".join(
        path.read_text(encoding="utf-8-sig")
        for path in POWERS_ROOT.rglob("*.cs")
    )
    custom_power_types = set(re.findall(
        r"\[RegisterPower\]\s*(?:public\s+)?(?:sealed\s+)?"
        r"class\s+(\w+)",
        power_source,
    ))
    for power_type in sorted(referenced_power_types & custom_power_types):
        prefix = f"MAIDEN_SUCCUBUS_POWER_{screaming_snake(power_type)}"
        for suffix in ("title", "description"):
            key = f"{prefix}.{suffix}"
            if not power_loc.get(key):
                failures.append(
                    f"missing card-related power localization: {power_type}: {key}"
                )

    report_path = ROOT / ".review" / "card_localization_audit.json"
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )

    for failure in failures:
        print(failure)
    print(f"audited={len(cards)} failures={len(failures)} report={report_path}")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
