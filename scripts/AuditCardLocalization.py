import argparse
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

# These are implementation-only selector/proxy cards.  They are registered so
# the engine can render a real card choice, but DesignDoc intentionally defines
# the owning mechanic rather than a collectible card entry for each proxy.
TECHNICAL_CARD_TYPES = {
    "LibraryPileChoice",
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
    code = mask_csharp_literals(text)
    brace = code.find("{", class_match.end())
    terminator = code.find(";", class_match.end())
    if terminator >= 0 and (brace < 0 or terminator < brace):
        return ""
    if brace < 0:
        return ""
    depth = 0
    for index in range(brace, len(text)):
        if code[index] == "{":
            depth += 1
        elif code[index] == "}":
            depth -= 1
            if depth == 0:
                return text[brace : index + 1]
    return text[brace:]


def mask_csharp_literals(text: str) -> str:
    """Preserve offsets while excluding comments/strings from brace matching.

    This is a deliberately bounded source reader, not a C# compiler. Metadata
    expressions it cannot resolve are reported as unknown rather than guessed.
    """
    pattern = r'//[^\n]*|/\*[\s\S]*?\*/|"""[\s\S]*?"""|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\''
    return re.sub(pattern, lambda m: re.sub(r"[^\n]", " ", m.group()), text)


def registered_cards() -> tuple[
    dict[str, tuple[str, Path]],
    dict[str, tuple[str, str | None, Path]],
]:
    result: dict[str, tuple[str, Path]] = {}
    all_classes: dict[str, tuple[str, str | None, Path]] = {}
    class_pattern = re.compile(
        r"(?:public|internal|protected|private)?\s*(?:abstract\s+|sealed\s+)?"
        r"class\s+(?P<name>\w+)(?P<generic><[^>{}]+>)?"
        r"(?:\s*:\s*(?P<base>\w+(?:<[^>{}]+>)?))?"
    )
    registered_pattern = re.compile(
        r"\[RegisterCard\([^\]]+\)\]\s*"
        r"(?:\[[^\]]+\]\s*)*"
        r"(?:public\s+)?sealed\s+class\s+(?P<name>\w+)"
    )
    for path in CARDS_ROOT.rglob("*.cs"):
        text = path.read_text(encoding="utf-8-sig")
        code = mask_csharp_literals(text)
        for match in class_pattern.finditer(code):
            base = match.group("base")
            if base:
                base = generic_key(base)
            name = generic_key(match.group("name") + (match.group("generic") or ""))
            all_classes[name] = (class_block(text, match), base, path)
        for match in registered_pattern.finditer(code):
            result[match.group("name")] = (class_block(text, match), path)
    return result, all_classes


def generic_key(name: str) -> str:
    if "<" not in name:
        return name
    stem, args = name.split("<", 1)
    return f"{stem}`{len(args.split(','))}"


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
    value = re.sub(r"^\s*#+\s*", "", text).strip()
    value = re.sub(r"\s*`\[[^\]]+\]`\s*$", "", value).strip().strip("*`")
    value = re.sub(r"^\d+[.、]\s*", "", value)
    value = re.sub(r"^[（(]\s*", "", value)
    value = re.sub(r"^(?:衍生卡|衍生牌)(?:\s*[：:]\s*|\s+)", "", value)
    value = re.sub(r"[（(]\s*(?:(?:是)?(?:打击|防御)|事件衍生卡)\s*[）)]\s*$", "", value)
    return value.strip()


CARD_TYPE_LINE = re.compile(r"^(?:攻击|技能|能力|状态|诅咒)牌(?:\s|$)")


def next_content(lines: list[str], index: int) -> int | None:
    return next((i for i in range(index + 1, len(lines)) if lines[i].strip()), None)


def is_card_title(lines: list[str], index: int) -> bool:
    following = next_content(lines, index)
    return following is not None and bool(CARD_TYPE_LINE.match(lines[following].strip()))


def catalogue_bounds(lines: list[str]) -> tuple[int, int]:
    start = next((i for i, line in enumerate(lines)
                  if line.startswith("## ") and "CARD-POOL-001" in line), 0)
    end = next((i for i in range(start + 1, len(lines))
                if re.match(r"##\s+三、遗物", lines[i])), len(lines))
    return start, end


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
    start, end = catalogue_bounds(lines)
    # Real typed entries outrank prose and same-name route/relic headings.
    typed = [i for i in matches if is_card_title(lines, i)]
    in_catalogue = [i for i in typed if start <= i < end]
    if in_catalogue:
        return in_catalogue[0]
    if typed:
        return typed[0]
    # Status/curse entries omit their type row. Require an immediate rules row,
    # not a distant cost in the next card or prose elsewhere in the document.
    for index in matches:
        following = next_content(lines, index)
        if following is not None and re.match(
            r"^(?:(?:X|\d+)(?:/\d+)?费|无费用|不能被打出|无法被打出)",
            lines[following].strip(),
        ):
            return index
    return None


def design_body(title: str, lines: list[str]) -> list[str]:
    index = design_index(title, lines)
    if index is None:
        return []
    result: list[str] = []
    for i in range(index + 1, len(lines)):
        value = lines[i].strip()
        if not value:
            if result:
                break
            continue
        if value.startswith(("#", "**", "<a ")) or value in ("）", ")"):
            break
        if is_card_title(lines, i):
            break
        # Numbered Scriptures have no blank separator, and a closing wrapper
        # can be on the last rules line of an inline generated card.
        result.append(value)
        if value.endswith("。）"):
            break
    return result


def typed_design_entries(lines: list[str]) -> dict[str, int]:
    """Reverse coverage: named typed designs without a registered model.

    Untyped statuses and prose-only designs still require review; this is not
    advertised as a complete natural-language requirements extractor.
    """
    result: dict[str, int] = {}
    for i, line in enumerate(lines):
        if not CARD_TYPE_LINE.match(line.strip()):
            continue
        previous = next((j for j in range(i - 1, -1, -1) if lines[j].strip()), None)
        if previous is None:
            continue
        title = normalize_design_title(lines[previous])
        if not title or title.endswith(("：", ":")) or lines[previous].startswith("**"):
            continue
        result.setdefault(title, previous)
    return result


def design_context(title: str, lines: list[str]) -> str | None:
    index = design_index(title, lines)
    if index is None:
        return None
    return "\n".join([lines[index].strip(), *design_body(title, lines)])


def normalize_for_comparison(text: str) -> str:
    """Informational template similarity ONLY; preserve punctuation and lines.

    Numeric substitution cannot prove values, SmartFormat branches or rendered
    typography. Those remain explicitly pending independent/runtime contracts.
    """
    text = re.sub(r"\[[^\]]+\]", "", text)
    text = re.sub(r"\{[^{}]+\}", "数值", text)
    text = re.sub(r"\d+(?:/\d+)?", "数值", text)
    text = re.sub(r"^(?:X|数值)(?:/数值)?费(?:数值欲望)?\s*", "", text)
    return re.sub(r"[^\S\n]+", "", text).strip()


def design_effect(title: str, lines: list[str]) -> str | None:
    index = design_index(title, lines)
    if index is None:
        return None
    candidates = [line for line in design_body(title, lines)
                  if not CARD_TYPE_LINE.match(line)
                  and line != "power" and not line.startswith("卡图：")]
    return "\n".join(candidates) or None


def design_metadata(title: str, lines: list[str]) -> dict[str, object] | None:
    index = design_index(title, lines)
    if index is None:
        return None
    window = design_body(title, lines)
    context = " ".join(window)
    type_line = next(
        (line for line in window
         if CARD_TYPE_LINE.match(line)),
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
    effect = design_effect(title, lines) or ""
    cost_match = re.match(r"(?P<base>X|\d+)(?:/(?P<upgraded>\d+))?费", effect)
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
    names: list[str] = []
    paths: list[str] = []
    current: str | None = type_name
    visited: set[str] = set()
    while current and current not in visited and current in all_classes:
        visited.add(current)
        block, base, path = all_classes[current]
        blocks.append(block)
        names.append(current)
        paths.append(str(path.relative_to(ROOT)).replace("\\", "/"))
        current = base
    # Resolve only constant/enum forwarding through constructor parameters.
    # Never take a convenient literal from an unrelated method or base class.
    arguments: list[str] = []
    bindings: dict[str, str] = {}
    for name, block in zip(names, blocks):
        constructor = re.search(
            rf"\b{re.escape(name.split('`')[0])}\s*\((?P<params>[^()]*)\)\s*"
            r":\s*base\((?P<args>[^()]*)\)", mask_csharp_literals(block),
        )
        if not constructor:
            # No constructor means the language's implicit parameterless base
            # call. An explicit but unsupported constructor must stay unknown.
            if re.search(rf"\b{re.escape(name.split('`')[0])}\s*\(", mask_csharp_literals(block)):
                arguments = []
                break
            arguments = []
            continue
        parameters = [p.strip() for p in constructor.group("params").split(",") if p.strip()]
        bindings = {}
        for position, parameter in enumerate(parameters):
            declaration, _, default = parameter.partition("=")
            parameter_name = declaration.split()[-1]
            if position < len(arguments):
                bindings[parameter_name] = arguments[position]
            elif default:
                bindings[parameter_name] = default.strip()
        arguments = [bindings.get(arg.strip(), arg.strip())
                     for arg in constructor.group("args").split(",")]
        if len(arguments) >= 4 and arguments[1].startswith("CardType."):
            break
    resolved = dict.fromkeys(("cost", "type", "rarity", "target"))
    if len(arguments) >= 4:
        resolved["cost"] = int(arguments[0]) if re.fullmatch(r"-?\d+", arguments[0]) else None
        for position, field, enum in ((1, "type", "CardType"), (2, "rarity", "CardRarity"),
                                     (3, "target", "TargetType")):
            match = re.fullmatch(rf"{enum}\.(\w+)", arguments[position])
            resolved[field] = match.group(1) if match else None
    upgrade = inherited_method(blocks, "OnUpgrade")
    cost_upgrades = re.findall(r"EnergyCost\.UpgradeBy\(\s*(-?\d+)\s*\)", upgrade)
    # Conditional or other forms of mutation cannot be certified by a sum.
    unknown_cost_upgrade = "EnergyCost" in upgrade and (
        not cost_upgrades or bool(re.search(r"\b(?:if|switch)\b|\?", mask_csharp_literals(upgrade))))
    keyword_source = next((block for block in blocks if "CanonicalKeywords" in block), "")
    canonical_keyword_match = re.search(
        r"CanonicalKeywords\s*=>\s*\[(?P<body>.*?)\]\s*;",
        keyword_source,
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
        **resolved,
        "upgradedCost": (
            None if unknown_cost_upgrade else
            resolved["cost"] + sum(map(int, cost_upgrades))
            if resolved["cost"] is not None else None
        ),
        "unresolvedFields": [key for key, value in resolved.items() if value is None]
            + (["keywords"] if keyword_source and not canonical_keyword_match else [])
            + (["upgradedCost"] if unknown_cost_upgrade else []),
        "keywords": flat_keywords,
        "addsKeywords": sorted(set(re.findall(r"AddKeyword\(([^)]+)\)", upgrade))),
        "removesKeywords": sorted(set(re.findall(r"RemoveKeyword\(([^)]+)\)", upgrade))),
    }


def inherited_method(blocks: list[str], name: str) -> str:
    for i, block in enumerate(blocks):
        match = re.search(rf"\boverride\s+void\s+{name}\(\)\s*(=>|\{{)",
                          mask_csharp_literals(block))
        if not match:
            continue
        if match.group(1) == "=>":
            result = block[match.end():block.index(";", match.end())]
        else:
            # class_block accepts a match ending before the opening brace.
            prefix = re.search(rf"\boverride\s+void\s+{name}\(\)", block)
            result = class_block(block, prefix)
        if f"base.{name}()" in result:
            result += "\n" + inherited_method(blocks[i + 1:], name)
        return result
    return ""


def retired_compatibility(cards, design_lines, loc, read_source=None):
    """Check explicit retirement wiring; never infer retirement from missing design.

    This proves only the source gates. Save compatibility still requires the
    native ms_test_retired contract and UI verification in a running game.
    """
    read_source = read_source or (lambda path: (ROOT / path).read_text(encoding="utf-8-sig"))
    manifest = json.loads(read_source("docs/content_contract_20260824.json"))
    names = manifest.get("retiredCompatibility", {}).get("cards", [])
    if not names:
        return set(), ["retirement manifest missing explicit card identities"]
    failures = []
    code = lambda path: mask_csharp_literals(read_source(path))
    policy = code("src/Core/Routes/RetiredCardCatalog.cs")
    match = re.search(r"card\s+is\s+([\w\s]+);", policy)
    actual = set(re.split(r"\s+or\s+", match[1].strip())) if match else set()
    if set(names) != actual or len(names) != len(set(names)):
        failures.append("retirement manifest and exact runtime identities differ")
    if "cards.Where(card => !IsRetired(card))" not in policy:
        failures.append("retirement policy missing exclusion predicate")
    for pool in ("MSNeutralCardPool", "MSCorruptCardPool"):
        if "RetiredCardCatalog.Obtainable(base.FilterThroughEpochs(unlockState, cards))" not in code(f"src/Pools/{pool}.cs"):
            failures.append(f"retirement missing native acquisition filter: {pool}")
    if ".Where(card => !RetiredCardCatalog.IsRetired(card))" not in code("src/Core/Routes/AllMaidenSuccubusCards.cs"):
        failures.append("retirement missing direct canonical candidate filter")
    bases = code("src/Cards/MSCardBases.cs")
    if bases.count("base(cost, type, rarity, target, shouldShowInCardLibrary)") != 4:
        failures.append("retirement library visibility not forwarded through both base chains")
    for name in names:
        if name not in cards:
            failures.append(f"retired saved identity no longer registered: {name}")
            continue
        block = mask_csharp_literals(cards[name][0])
        for gate in ("CanBeGeneratedInCombat => false", "CanBeGeneratedByModifiers => false",
                     "shouldShowInCardLibrary: false"):
            if gate not in block:
                failures.append(f"retirement missing acquisition gate: {name}: {gate}")
        title = loc.get(f"MAIDEN_SUCCUBUS_CARD_{screaming_snake(name)}.title", "")
        if design_index(title, design_lines) is not None:
            failures.append(f"retirement contradicts current DesignDoc entry: {name}")
    return (set() if failures else set(names)), failures


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Static card audit; not a gameplay/rendering certification.")
    output = parser.add_mutually_exclusive_group()
    output.add_argument("--output", type=Path, help="Explicit independent JSON report path")
    output.add_argument("--no-write", action="store_true", help="Read-only audit; preserve all existing reports")
    parser.add_argument("--strict-review", action="store_true",
                        help="Fail on unresolved metadata or unverified full-text/rendering contracts")
    args = parser.parse_args(argv)
    loc = load_json_with_review_comments(LOC_PATH)
    power_loc = load_json_with_review_comments(POWER_LOC_PATH)
    static_hover_loc = load_json_with_review_comments(STATIC_HOVER_LOC_PATH)
    design_lines = DESIGN_PATH.read_text(encoding="utf-8-sig").splitlines()
    cards, all_classes = registered_cards()
    failures: list[str] = []
    report: list[dict[str, object]] = []
    retired, retirement_failures = retired_compatibility(cards, design_lines, loc)
    failures.extend(retirement_failures)

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
        failure_start = len(failures)
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
        if design_data:
            expected_type = DESIGN_TYPES.get(design_data["type"])
            if expected_type and source_data["type"] is not None and expected_type != source_data["type"]:
                failures.append(
                    f"DesignDoc type mismatch: {type_name}: "
                    f"expected {expected_type}, source {source_data['type']}"
                )
            expected_rarity = DESIGN_RARITIES.get(design_data["rarity"])
            if expected_rarity and source_data["rarity"] is not None and expected_rarity != source_data["rarity"]:
                failures.append(
                    f"DesignDoc rarity mismatch: {type_name}: "
                    f"expected {expected_rarity}, source {source_data['rarity']}"
                )
            expected_cost = design_data["baseCost"]
            if expected_cost and expected_cost != "X" and source_data["cost"] is not None \
                    and int(expected_cost) != source_data["cost"]:
                failures.append(
                    f"DesignDoc cost mismatch: {type_name}: "
                    f"expected {expected_cost}, source {source_data['cost']}"
                )
            expected_upgraded_cost = design_data["upgradedCost"]
            if expected_upgraded_cost and source_data["upgradedCost"] is not None \
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
        if type_name in TECHNICAL_CARD_TYPES:
            status = "TECHNICAL"
        elif type_name in retired:
            status = "RETIRED-COMPAT"
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
            "textReview": ("NOT_APPLICABLE" if status == "TECHNICAL" else
                           "LEGACY_PRESERVED_PENDING_RUNTIME" if status == "RETIRED-COMPAT" else
                           "PENDING_EXACT_RENDERED_CONTRACT"),
            "numericReview": "PENDING_RUNTIME_VARIABLES",
            "findings": failures[failure_start:],
            "hoverReferences": hover_references,
        })

    mapped_titles = {row.get("title") for row in report if row.get("status") == "DESIGN"}
    for title, index in typed_design_entries(design_lines).items():
        if title not in mapped_titles:
            report.append({
                "status": "DESIGN-ONLY", "title": title, "line": index + 1,
                "designEffect": design_effect(title, design_lines),
                "review": "PENDING_MODEL_OR_EXPLICIT_SCOPE_DECISION",
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

    report_path = None if args.no_write else (
        args.output or ROOT / ".review" / "card_localization_audit.json")
    if report_path is not None:
        report_path.parent.mkdir(parents=True, exist_ok=True)
        report_path.write_text(
            json.dumps(report, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )

    pending_text = sum(row.get("textReview") == "PENDING_EXACT_RENDERED_CONTRACT" for row in report)
    unresolved = sum(bool(row.get("source", {}).get("unresolvedFields")) for row in report)
    design_only = sum(row.get("status") == "DESIGN-ONLY" for row in report)
    retired_count = sum(row.get("status") == "RETIRED-COMPAT" for row in report)
    if args.strict_review and (pending_text or unresolved or design_only):
        failures.append(f"full review incomplete: pendingText={pending_text} unresolvedMetadata={unresolved} "
                        f"designOnly={design_only}")

    for failure in failures:
        print(failure)
    print(f"audited={len(cards)} failures={len(failures)} unresolvedMetadata={unresolved} "
          f"pendingText={pending_text} designOnly={design_only} retiredCompat={retired_count} report={report_path}")
    print("Static findings only: similarity does not verify numbers, punctuation, layout, or gameplay.")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
