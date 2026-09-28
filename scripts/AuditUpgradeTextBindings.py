"""Read-only, bounded upgrade-scalar -> description dependency audit.

This is not a C# compiler, value/punctuation validator or engine renderer. It
detects a changed DynamicVar no longer referenced by text (e.g. a literal 1 in
CycloneRupture). Direct UpgradeValueBy calls, inherited OnUpgrade methods, native
calculated variables and StringVar updates are supported. No report is written.
"""
import argparse
import json
import re
from collections import Counter

from AuditCardLocalization import (
    LOC_PATH, inherited_method, load_json_with_review_comments,
    mask_csharp_literals, placeholders, registered_cards, screaming_snake,
)

VAR = r'\bDynamicVars\s*(?:\.\s*(?P<property>\w+)|\[\s*"(?P<index>\w+)"\s*\])'


def live_matches(pattern, source):
    """Ignore expressions occurring only inside comments or string literals."""
    masked = mask_csharp_literals(source)
    for match in re.finditer(pattern, source, re.S):
        if masked[match.start():match.start() + 1].strip():
            yield match


def variable_name(match):
    return match.group("property") or match.group("index")


def upgrade_writes(upgrade):
    writes = set()
    unknown = []
    for match in live_matches(VAR + r'\s*\.\s*(?P<operation>\w+)\s*\(', upgrade):
        operation = match.group("operation")
        if operation == "UpgradeValueBy":
            writes.add(variable_name(match))
        else:
            unknown.append(f"{variable_name(match)}.{operation}")
    for match in live_matches(VAR + r'\s*\.\s*(?:BaseValue|IntValue)\s*=(?!=)', upgrade):
        unknown.append(variable_name(match) + " assignment")
    return writes, sorted(set(unknown))


def dependencies(class_source, upgrade):
    """Return supported derived output -> input names, with concrete evidence.

    Native calculated vars use CalculationBase plus their documented extra var;
    a StringVar is accepted only when OnUpgrade actually updates its StringValue
    from a DynamicVar. Merely declaring a constant icon string cannot pass.
    """
    result = {}
    for match in live_matches(r'\bnew\s+Calculated(?P<kind>Damage|Block)Var\s*\(', class_source):
        kind = match.group("kind")
        extra = "ExtraDamage" if kind == "Damage" else "CalculationExtra"
        result["Calculated" + kind] = {"CalculationBase", extra}
    for match in live_matches(r'\bnew\s+CalculatedVar\s*\(\s*"(?P<name>\w+)"', class_source):
        result[match.group("name")] = {"CalculationBase", "CalculationExtra"}
    string_update = (
        r'\(\s*\(\s*StringVar\s*\)\s*DynamicVars\[\s*"(?P<output>\w+)"\s*\]\s*\)'
        r'\s*\.\s*StringValue\s*=\s*(?P<expression>[^;]+);'
    )
    for match in live_matches(string_update, upgrade):
        inputs = {variable_name(m) for m in live_matches(VAR, match.group("expression"))}
        if inputs:
            result.setdefault(match.group("output"), set()).update(inputs)
    return result


def inspect_upgrade(blocks, descriptions):
    upgrade = inherited_method(blocks, "OnUpgrade")
    writes, unknown = upgrade_writes(upgrade)
    references = set().union(*(placeholders(text) for text in descriptions))
    edges = dependencies("\n".join(blocks), upgrade)
    covered = set(references)
    pending = list(references)
    while pending:
        for dependency in edges.get(pending.pop(), set()):
            if dependency not in covered:
                covered.add(dependency)
                pending.append(dependency)
    missing = sorted(writes - covered)
    return {
        "status": "UNRESOLVED" if unknown else "MISSING_BINDING" if missing else "BOUND" if writes else "NO_DIRECT_SCALAR_UPGRADE",
        "writes": sorted(writes), "direct": sorted(writes & references),
        "indirect": sorted((writes & covered) - references),
        "missing": missing, "unsupported": unknown,
        "dependencies": {name: sorted(values) for name, values in sorted(edges.items()) if name in covered},
    }


def inherited_blocks(name, classes):
    blocks = []
    seen = set()
    while name in classes:
        if name in seen:
            raise ValueError("Inheritance cycle: " + name)
        seen.add(name)
        block, parent, _ = classes[name]
        blocks.append(block)
        name = parent
    return blocks


def audit(cards, classes, localization):
    rows = []
    for name in sorted(cards):
        prefix = "MAIDEN_SUCCUBUS_CARD_" + screaming_snake(name) + ".description"
        descriptions = [text for key, text in localization.items() if key.startswith(prefix)]
        row = inspect_upgrade(inherited_blocks(name, classes), descriptions)
        row["model"] = name
        row["descriptionKeys"] = sorted(key for key in localization if key.startswith(prefix))
        if not descriptions:
            row["status"] = "MISSING_DESCRIPTION"
        rows.append(row)
    return rows


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", action="store_true", help="Print evidence to stdout; never writes report files.")
    args = parser.parse_args(argv)
    cards, classes = registered_cards()
    rows = audit(cards, classes, load_json_with_review_comments(LOC_PATH))
    counts = Counter(row["status"] for row in rows)
    failures = [row for row in rows if row["status"] not in ("BOUND", "NO_DIRECT_SCALAR_UPGRADE")]
    if args.json:
        print(json.dumps(rows, ensure_ascii=False, indent=2))
    else:
        for row in failures:
            print(f"{row['model']}: {row['status']}: missing={row['missing']} unsupported={row['unsupported']}")
        print(f"registered={len(rows)} statuses={dict(sorted(counts.items()))} "
              f"scalarBindings={sum(len(row['writes']) for row in rows)} "
              f"indirectBindings={sum(len(row['indirect']) for row in rows)} failures={len(failures)}")
        print("Bounded source dependency check only: no claim about exact values, branch coverage, formatting, gameplay or arbitrary helper-driven upgrades.")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
