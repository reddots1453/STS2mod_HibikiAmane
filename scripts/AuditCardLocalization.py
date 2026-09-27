import json
import re
import sys
from difflib import SequenceMatcher
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CARDS_ROOT = ROOT / "src" / "Cards"
LOC_PATH = ROOT / "MaidenSuccubus" / "localization" / "zhs" / "cards.json"
POWER_LOC_PATH = ROOT / "MaidenSuccubus" / "localization" / "zhs" / "powers.json"
STATIC_HOVER_LOC_PATH = (
    ROOT / "MaidenSuccubus" / "localization" / "zhs" / "static_hover_tips.json"
)
DESIGN_PATH = ROOT / "DesignDoc.md"
POWERS_ROOT = ROOT / "src" / "Powers"
HOVER_SUPPORT_PATH = CARDS_ROOT / "CardHoverTipSupport.cs"

HOVER_PIPELINE_PATHS = (
    CARDS_ROOT / "MSCardBases.cs",
    CARDS_ROOT / "MvpGeneratedCards.cs",
    CARDS_ROOT / "Scriptures" / "ScriptureCardTemplate.cs",
    CARDS_ROOT / "Curses" / "SemenCurse.cs",
)

REQUIRED_HOVER_TERMS = {
    "断罪", "净化", "魔力解放", "魔力增幅", "圣言", "堕落值", "欲望",
    "挣脱", "拘束", "诱惑度", "魔装耐久", "燃烧", "破碎", "圣域",
    "虚弱", "易伤", "脆弱", "力量", "敏捷", "荆棘", "覆甲", "滑溜",
    "残影", "变身", "格挡", "击晕", "消耗", "保留", "虚无", "固有",
    "侵犯", "色情攻击",
}

STATIC_HOVER_CONTRACTS = {
    "魔力解放": "MAIDENSUCCUBUS_OVERDRAFT",
    "变奏": "MAIDENSUCCUBUS_VARIATION",
    "欲望": "MAIDENSUCCUBUS_SECONDARY_RESOURCE_DESIRE",
    "堕落值": "MAIDENSUCCUBUS_CORRUPTION_REFERENCE",
    "圣言": "MAIDENSUCCUBUS_SCRIPTURE",
    "挣脱": "MAIDENSUCCUBUS_ESCAPE_KEYWORD",
    "拘束": "MAIDENSUCCUBUS_CONTROL",
    "诱惑度": "MAIDENSUCCUBUS_TEMPTATION_REFERENCE",
    "变身": "MAIDENSUCCUBUS_TRANSFORMATION",
    "侵犯": "MAIDENSUCCUBUS_INVASION_REFERENCE",
    "色情攻击": "MAIDENSUCCUBUS_EROTIC_ATTACK_REFERENCE",
}

KEYWORD_HOVER_TOKENS = {
    "消耗": "[gold]消耗[/gold]",
    "保留": "[gold]保留[/gold]",
    "虚无": "[gold]虚无[/gold]",
    "固有": "[gold]固有[/gold]",
}

DERIVATIVE_HOVER_CONTRACTS = {
    "PleasureDrowning": "FromCard<ArousalStatus>",
    "Exhibitionist": "FromCard<NakedDesireStatus>",
    "MomentaryGrace": "FromCard<IceMist>",
    "Blizzard": "FromCard<IceMist>",
    "IceBreakingSlash": "FromCard<IceShard>",
    "IceShield": "FromCard<IceShard>",
    "GoddessOfIce": "FromCard<IceShard>",
    "Lullaby": "FromCard<DrowsyStatus>",
    "CounterBarrier": "FromCard<CounterBarrierII>",
    "InsectEggCurse": "FromCard<ArousalStatus>",
    "MagicResidueCurse": "FromCard<Slimed>",
    "VineSeedCurse": "FromCard<SporeMind>",
    "LewdMarkMinorCurse": "FromCard<ArousalStatus>",
    "LewdMarkSpreadCurse": "FromCard<ArousalStatus>",
    "LewdMarkCompleteCurse": "FromCard<ArousalStatus>",
    "MagicSword": "FromEnchantment<ChargeEnchantment>",
    "ForgeCharge": "FromEnchantment<ChargeEnchantment>",
    "YarusMemory": "FromEnchantment<SoulLinkEnchantment>",
    "ForgeNimble": "FromEnchantment<Adroit>",
}

ENCHANTMENT_TEXT_CONTRACTS = {
    "DarkStorm": ("FromEnchantment<Glam>", "[gold]附魔[/gold]：[purple]华彩[/purple]"),
    "SharpForge": ("FromEnchantment<Sharp>", "[gold]附魔[/gold]：[purple]锋利："),
    "TacticalAnalyzer": ("FromEnchantment<Steady>", "[gold]附魔[/gold]：[purple]稳定[/purple]"),
    "ForgeNimble": ("FromEnchantment<Adroit>", "[gold]附魔[/gold]：[purple]伶俐[/purple]"),
    "MagicSword": ("FromEnchantment<ChargeEnchantment>", "[gold]附魔[/gold]：[purple]充能：2[/purple]"),
    "ForgeCharge": ("FromEnchantment<ChargeEnchantment>", "[gold]附魔[/gold]：[purple]充能："),
    "YarusMemory": ("FromEnchantment<SoulLinkEnchantment>", "[gold]附魔[/gold]：[purple]灵魂联结[/purple]"),
    "ForgeStrike": ("FromEnchantment<Instinct>", "[gold]附魔[/gold]：[purple]本能[/purple]"),
    "FamiliarContract": ("FromEnchantment<FamiliarEnchantment>", "[gold]附魔[/gold]：[purple]使魔[/purple]"),
    "GaleSword": ("FromEnchantment<Swift>", "[gold]附魔[/gold]：[purple]迅捷：2[/purple]"),
    "ShiningSword": ("FromEnchantment<Vigorous>", "[gold]附魔[/gold]：[purple]活力：3[/purple]"),
    "FlameSword": ("FromEnchantment<TezcatarasEmber>", "[gold]附魔[/gold]：[purple]特兹卡塔拉的余烬[/purple]"),
}

DESIGN_CARD_START = 614
DESIGN_CARD_END = 1842

# These are implementation-only selector/proxy cards.  They are registered so
# the engine can render a real card choice, but DesignDoc intentionally defines
# the owning mechanic rather than a collectible card entry for each proxy.
TECHNICAL_CARD_TYPES = {
    "EnchantmentChoiceCard",
    "FourthRouteQuestChoice",
    "OverdraftAcceptChoice",
    "OverdraftDeclineChoice",
    "StigmaCondemnationChoice",
    "StigmaTargetChoice",
    "StigmaWeakChoice",
}

# These models are either basic cards, nested derivatives, event-only cards,
# or entries that DesignDoc explicitly marks as pending.  Their owning design
# section is still audited, but they do not have a standalone catalogue entry.
ALLOWED_NON_CATALOGUE_TYPES = {
    "CalmMind",
    "ClimaxBanCurse",
    "CounterBarrierII",
    "DrowsyStatus",
    "HypnosisCurse",
    "IceMist",
    "InsatiableGreed",
    "MaidenDefend",
    "MaidenStrike",
}

DESIGN_TYPES = {
    "攻击牌": "Attack",
    "技能牌": "Skill",
    "能力牌": "Power",
    "状态牌": "Status",
    "诅咒牌": "Curse",
}
DESIGN_RARITIES = {
    "基础": "Basic",
    "普通": "Common",
    "罕见": "Uncommon",
    "稀有": "Rare",
    "先古": "Ancient",
}

# STS2's Chinese card text colors rules/mechanics and named derivative cards
# gold, Desire pink, and Corruption/route language purple.  Enforcing this for
# every registered description prevents isolated regressions such as an
# uncolored Ice Shard or a literal vanilla debuff with no visual affordance.
REQUIRED_COLOR_TERMS = {
    "欲望": "pink",
    "堕落值": "purple",
    "魔力解放": "gold",
    "变奏": "gold",
    "圣言": "gold",
    "挣脱": "gold",
    "拘束": "gold",
    "变身": "gold",
    "诱惑度": "gold",
    "侵犯": "gold",
    "色情攻击": "pink",
    "断罪": "gold",
    "净化": "gold",
    "魔力增幅": "gold",
    "魔装耐久": "gold",
    "燃烧": "gold",
    "破碎": "gold",
    "圣域": "gold",
    "虚弱": "gold",
    "易伤": "gold",
    "脆弱": "gold",
    "力量": "gold",
    "敏捷": "gold",
    "荆棘": "gold",
    "覆甲": "gold",
    "滑溜": "gold",
    "残影": "gold",
    "击晕": "gold",
    "消耗": "gold",
    "保留": "gold",
    "虚无": "gold",
    "固有": "gold",
    "发情": "gold",
    "赤裸欲": "gold",
    "冰雾": "gold",
    "冰晶碎片": "gold",
    "困了": "gold",
    "功性魔防壁II": "gold",
    "功性魔防壁III": "gold",
    "功性魔防壁IV": "gold",
    "粘液": "gold",
    "孢子心灵": "gold",
    "附魔": "gold",
    "华彩": "purple",
    "稳定": "purple",
    "锋利": "purple",
    "伶俐": "purple",
    "充能": "purple",
    "灵魂联结": "purple",
}

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
    "CalculatedVar": None,
    "CalculationBaseVar": "CalculationBase",
    "CalculationExtraVar": "CalculationExtra",
    "RepeatVar": "Repeat",
    "IntVar": None,
    "StringVar": None,
    "DynamicVar": None,
    "DesireScaledDamageVar": "Damage",
    "CurrentEnergyHitsVar": "Hits",
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


def registered_cards() -> tuple[
    dict[str, tuple[str, Path]],
    dict[str, tuple[str, str | None, Path]],
]:
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
            all_classes[match.group("name")] = (class_block(text, match), base, path)
        for match in registered_pattern.finditer(text):
            result[match.group("name")] = (class_block(text, match), path)
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
    for match in re.finditer(
        r'description\.Add\(\s*"(?P<name>[^"]+)"', block
    ):
        names.add(match.group("name"))
    return names


def placeholders(text: str) -> set[str]:
    result = set()
    for body in re.findall(r"\{([^{}]+)\}", text):
        name = re.split(r"[:.]", body, maxsplit=1)[0]
        if name and name not in KNOWN_FORMAT_ARGS:
            result.add(name)
    return result


def has_uncolored_term(text: str, term: str, color: str) -> bool:
    colored = re.compile(
        rf"\[{re.escape(color)}\].*?{re.escape(term)}.*?\[/{re.escape(color)}\]"
    )
    return term in colored.sub("", text)


def normalize_design_title(text: str) -> str:
    value = text.strip().strip("*`")
    value = re.sub(r"^\d+[.、]\s*", "", value)
    value = re.sub(r"^(?:衍生卡|衍生牌)(?:\s*[：:]\s*|\s+)", "", value)
    value = re.sub(r"[（(]\s*(?:是)?(?:打击|防御)\s*[）)]\s*$", "", value)
    value = re.sub(r"^[（(]\s*", "", value)
    return value.strip()


def design_index(title: str, lines: list[str]) -> int | None:
    exact = [i for i, line in enumerate(lines) if line.strip() == title]
    normalized = [
        i for i, line in enumerate(lines)
        if normalize_design_title(line) == title
    ]
    matches = list(dict.fromkeys(exact + normalized))
    if not matches:
        return None
    # Prefer the actual card catalogue, then the event-derived-card section.
    in_catalogue = [i for i in matches if DESIGN_CARD_START <= i < DESIGN_CARD_END]
    if in_catalogue:
        return in_catalogue[0]

    # Generated cards can be specified inline under a relic or event.  Prefer a
    # same-name heading that is immediately followed by card type/cost metadata
    # over the owning route/relic heading (for example the nested 谦逊 card).
    card_type_pattern = re.compile(r"(?:攻击|技能|能力|状态|诅咒)牌")
    for index in matches:
        nearby = " ".join(line.strip() for line in lines[index + 1 : index + 4])
        if card_type_pattern.search(nearby) and re.search(r"(?:X|\d+)(?:/\d+)?费", nearby):
            return index
    return matches[0]


def design_context(title: str, lines: list[str]) -> str | None:
    index = design_index(title, lines)
    if index is None:
        return None
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
    text = re.sub(r"^(?:X|数值)(?:/数值)?费(?:数值欲望)?\s*", "", text)
    text = re.sub(r"升级后(?:获得|移除|变化为)[^。；]+[。；]?", "", text)
    text = re.sub(r"(?:不能被打出|无法被打出|消耗|保留|虚无|固有|沉底|随身)[。；]?", "", text)
    return re.sub(r"[\s，。；：、（）()“”\-＋+]", "", text)


def design_effect(title: str, lines: list[str]) -> str | None:
    index = design_index(title, lines)
    if index is None:
        return None
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
    # Most entries are: title / card type+rarity / cost+effect.  Return the
    # first effect-looking line in document order.  Searching for a later line
    # containing "费" first incorrectly paired numbered Scripture entries
    # with the following Scripture's effect.
    for candidate in candidates:
        if re.search(r"(?:攻击牌|技能牌|能力牌|状态牌|诅咒牌)\s+(?:基础|普通|罕见|稀有|先古)$", candidate):
            continue
        if candidate.startswith(("**", "卡图：")) or candidate == "power":
            continue
        if any(word in candidate for word in (
            "造成", "获得", "抽", "失去", "选择", "回合", "打出", "将", "进入",
            "持续", "挣脱", "移除", "变化", "给予", "恢复", "不能", "无法",
        )):
            return candidate
    return candidates[-1]


def design_metadata(title: str, lines: list[str]) -> dict[str, object] | None:
    index = design_index(title, lines)
    if index is None:
        return None
    window: list[str] = []
    for raw in lines[index + 1 : min(index + 14, len(lines))]:
        value = raw.strip()
        if not value and window:
            break
        if value.startswith("#"):
            break
        if value:
            window.append(value)
        if len(window) >= 8:
            break
    context = " ".join(window)
    type_line = next(
        (line for line in window
         if re.search(r"(?:攻击牌|技能牌|能力牌|状态牌|诅咒牌)", line)
         and not re.search(r"(?:打出|获得|选择|变化为|消耗)", line)),
        "",
    )
    card_type = next(
        (name for name in ("攻击牌", "技能牌", "能力牌", "状态牌", "诅咒牌")
         if name in type_line),
        None,
    )
    rarity = next(
        (name for name in ("基础", "普通", "罕见", "稀有", "先古")
         if name in type_line),
        None,
    )
    cost_match = re.search(r"(?P<base>X|\d+)(?:/(?P<upgraded>\d+))?费", context)
    base_keyword_context = re.sub(r"升级后[^。；]+[。；]?", "", context)
    base_keyword_context = re.split(r"[（(]衍生(?:卡|牌)", base_keyword_context, 1)[0]
    base_keywords: list[str] = []
    for phrase, keyword in (
        ("消耗。", "Exhaust"),
        ("虚无。", "Ethereal"),
        ("保留。", "Retain"),
        ("固有。", "Innate"),
        ("沉底。", "SinkingKeyword"),
        ("随身。", "PortableKeyword"),
    ):
        if phrase in base_keyword_context:
            base_keywords.append(keyword)
    if "不能被打出" in base_keyword_context or "无法被打出" in base_keyword_context:
        base_keywords.append("Unplayable")
    upgrade_adds: list[str] = []
    upgrade_removes: list[str] = []
    for phrase, keyword in (
        ("升级后获得保留", "Retain"),
        ("升级后获得固有", "Innate"),
        ("升级后获得消耗", "Exhaust"),
    ):
        if phrase in context:
            upgrade_adds.append(keyword)
    for phrase, keyword in (
        ("升级后移除消耗", "Exhaust"),
        ("升级后移除虚无", "Ethereal"),
    ):
        if phrase in context:
            upgrade_removes.append(keyword)
    return {
        "line": index + 1,
        "type": card_type,
        "rarity": rarity,
        "baseCost": cost_match.group("base") if cost_match else None,
        "upgradedCost": cost_match.group("upgraded") if cost_match else None,
        "baseKeywords": sorted(set(base_keywords)),
        "upgradeAdds": sorted(set(upgrade_adds)),
        "upgradeRemoves": sorted(set(upgrade_removes)),
        "context": context,
    }


def source_metadata(
    type_name: str,
    all_classes: dict[str, tuple[str, str | None, Path]],
) -> dict[str, object]:
    blocks: list[str] = []
    paths: list[str] = []
    current: str | None = type_name
    visited: set[str] = set()
    while current and current not in visited and current in all_classes:
        visited.add(current)
        block, base, path = all_classes[current]
        blocks.append(block)
        paths.append(str(path.relative_to(ROOT)).replace("\\", "/"))
        current = base
    combined = "\n".join(blocks)
    constructor = re.search(
        r":\s*base\(\s*(?P<cost>-?\d+)\s*,\s*"
        r"CardType\.(?P<type>\w+)\s*,\s*"
        r"CardRarity\.(?P<rarity>\w+)\s*,\s*"
        r"TargetType\.(?P<target>\w+)",
        combined,
    )
    cost_upgrade = re.search(r"EnergyCost\.UpgradeBy\(\s*(-?\d+)\s*\)", blocks[0])
    canonical_keyword_match = re.search(
        r"CanonicalKeywords\s*=>\s*\[(?P<body>.*?)\]\s*;",
        blocks[0],
        re.S,
    )
    canonical_keyword_source = (
        canonical_keyword_match.group("body") if canonical_keyword_match else ""
    )
    keywords = sorted(set(re.findall(
        r"(?:CardKeyword\.(\w+)|(PortableKeyword|SinkingKeyword)\.Value)",
        canonical_keyword_source,
    )))
    flat_keywords = sorted({first or second for first, second in keywords})
    return {
        "source": paths[0] if paths else None,
        "cost": int(constructor.group("cost")) if constructor else None,
        "upgradedCost": (
            int(constructor.group("cost")) + int(cost_upgrade.group(1))
            if constructor and cost_upgrade else
            int(constructor.group("cost")) if constructor else None
        ),
        "type": constructor.group("type") if constructor else None,
        "rarity": constructor.group("rarity") if constructor else None,
        "target": constructor.group("target") if constructor else None,
        "keywords": flat_keywords,
        "addsKeywords": sorted(set(re.findall(r"AddKeyword\(([^)]+)\)", blocks[0]))),
        "removesKeywords": sorted(set(re.findall(r"RemoveKeyword\(([^)]+)\)", blocks[0]))),
    }


def main() -> int:
    loc = load_json_with_review_comments(LOC_PATH)
    power_loc = load_json_with_review_comments(POWER_LOC_PATH)
    static_hover_loc = load_json_with_review_comments(STATIC_HOVER_LOC_PATH)
    design_lines = DESIGN_PATH.read_text(encoding="utf-8-sig").splitlines()
    cards, all_classes = registered_cards()
    failures: list[str] = []
    report: list[dict[str, object]] = []

    hover_support = HOVER_SUPPORT_PATH.read_text(encoding="utf-8-sig")
    for term in sorted(REQUIRED_HOVER_TERMS):
        if term in KEYWORD_HOVER_TOKENS:
            continue
        if f'"{term}"' not in hover_support:
            failures.append(f"missing hover resolver term: {term}")
    for term, prefix in STATIC_HOVER_CONTRACTS.items():
        if prefix not in hover_support:
            failures.append(f"wrong static hover resolver: {term}: {prefix}")
        for suffix in ("title", "description"):
            key = f"{prefix}.{suffix}"
            if not static_hover_loc.get(key):
                failures.append(f"missing static hover localization: {term}: {key}")
        reference_description = static_hover_loc.get(f"{prefix}.description", "")
        unresolved = placeholders(reference_description)
        if unresolved:
            failures.append(
                f"card reference hover requires runtime vars: {term}: "
                f"{sorted(unresolved)}"
            )
    for term, token in KEYWORD_HOVER_TOKENS.items():
        if token not in hover_support:
            failures.append(
                f"keyword hover must match complete rich-text token: {term}: {token}"
            )
    for pipeline_path in HOVER_PIPELINE_PATHS:
        pipeline_source = pipeline_path.read_text(encoding="utf-8-sig")
        if "FromDescriptionReferences(this)" not in pipeline_source:
            failures.append(
                "card base missing description hover pipeline: "
                f"{pipeline_path.relative_to(ROOT)}"
            )

    registered_prefixes: set[str] = set()
    referenced_power_types: set[str] = set()
    for type_name, (block, source_path) in sorted(cards.items()):
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

        required_derivative_hover = DERIVATIVE_HOVER_CONTRACTS.get(type_name)
        if required_derivative_hover and required_derivative_hover not in block:
            failures.append(
                f"missing named derivative hover: {type_name}: "
                f"{required_derivative_hover}"
            )

        enchantment_contract = ENCHANTMENT_TEXT_CONTRACTS.get(type_name)
        if enchantment_contract:
            hover_call, text_marker = enchantment_contract
            if hover_call not in block:
                failures.append(
                    f"missing named enchantment hover: {type_name}: {hover_call}"
                )
            if text_marker not in description:
                failures.append(
                    f"noncanonical enchantment text: {type_name}: {text_marker}"
                )

        variables = set(BUILTIN_VARS)
        requires_selection_prompt = False
        current: str | None = type_name
        visited: set[str] = set()
        while current and current not in visited and current in all_classes:
            visited.add(current)
            current_block, current_base, _ = all_classes[current]
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
        design_data = design_metadata(title, design_lines)
        source_data = source_metadata(type_name, all_classes)
        if design_data and source_data["cost"] is not None:
            expected_type = DESIGN_TYPES.get(design_data["type"])
            if expected_type and expected_type != source_data["type"]:
                failures.append(
                    f"DesignDoc type mismatch: {type_name}: "
                    f"expected {expected_type}, source {source_data['type']}"
                )
            expected_rarity = DESIGN_RARITIES.get(design_data["rarity"])
            if expected_rarity and expected_rarity != source_data["rarity"]:
                failures.append(
                    f"DesignDoc rarity mismatch: {type_name}: "
                    f"expected {expected_rarity}, source {source_data['rarity']}"
                )
            expected_cost = design_data["baseCost"]
            if expected_cost and expected_cost != "X" \
                    and int(expected_cost) != source_data["cost"]:
                failures.append(
                    f"DesignDoc cost mismatch: {type_name}: "
                    f"expected {expected_cost}, source {source_data['cost']}"
                )
            expected_upgraded_cost = design_data["upgradedCost"]
            if expected_upgraded_cost \
                    and int(expected_upgraded_cost) != source_data["upgradedCost"]:
                failures.append(
                    f"DesignDoc upgraded cost mismatch: {type_name}: "
                    f"expected {expected_upgraded_cost}, "
                    f"source {source_data['upgradedCost']}"
                )

        for term, color in REQUIRED_COLOR_TERMS.items():
            if has_uncolored_term(description, term, color):
                failures.append(
                    f"uncolored card term: {type_name}: {term} requires [{color}]"
                )
        hover_references = sorted(
            term for term in REQUIRED_HOVER_TERMS
            if (
                KEYWORD_HOVER_TOKENS.get(term, term) in description
                and term not in {"固有"}
            )
        )
        similarity = None
        if effect:
            similarity = SequenceMatcher(
                None,
                normalize_for_comparison(description),
                normalize_for_comparison(effect),
            ).ratio()
        if context is None and type_name in TECHNICAL_CARD_TYPES:
            status = "TECHNICAL"
        elif context is None:
            status = "NO-DESIGN"
            if type_name not in ALLOWED_NON_CATALOGUE_TYPES:
                failures.append(
                    f"registered card has no DesignDoc mapping: {type_name}: {title}"
                )
        else:
            status = "DESIGN"
        report.append({
            "status": status,
            "type": type_name,
            "title": title,
            "description": description,
            "designEffect": effect,
            "designContext": context,
            "design": design_data,
            "source": source_data,
            "similarity": similarity,
            "hoverReferences": hover_references,
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

    for key, text in sorted(power_loc.items()):
        if not key.endswith((".description", ".smartDescription")):
            continue
        for term, color in REQUIRED_COLOR_TERMS.items():
            if has_uncolored_term(text, term, color):
                failures.append(
                    f"uncolored power hover term: {key}: {term} requires [{color}]"
                )

    all_card_source = "\n".join(
        path.read_text(encoding="utf-8-sig")
        for path in CARDS_ROOT.rglob("*.cs")
    )
    scripture_preview = re.search(
        r"HoverTipFactory\.FromCard<\w*Scripture>", all_card_source
    )
    if scripture_preview:
        failures.append(
            "Scripture source cards must use the text-only Scripture hover; "
            f"found {scripture_preview.group(0)}"
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
