"""Check the embedded monster catalogue's ID sets before game initialization."""

from collections import Counter
from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[1]
ASSIGNMENTS = (ROOT / "docs/EROTIC_ATTACK_ASSIGNMENTS.md").read_text(encoding="utf-8")
INTENTS = (ROOT / "docs/EROTIC_ATTACK_INTENTS.md").read_text(encoding="utf-8")
CATALOG = (ROOT / "src/Core/Intents/EroticAttackCatalog.cs").read_text(encoding="utf-8")
IDS = re.compile(r"`([A-Z0-9_]+)`")
SINGLE_ID = re.compile(r"`([A-Z0-9_]+)`(?:（.+）)?\Z")


def rows(document):
    for line in document.splitlines():
        if line.startswith("|"):
            yield [cell.strip() for cell in line.strip("|").split("|")]


def catalog_sets():
    roster = []
    steadfast = set()
    caps = {}
    thresholds = {}
    details = []
    recovery = []

    for cells in rows(ASSIGNMENTS):
        if len(cells) == 6 and "`" in cells[2]:
            for monster_id in IDS.findall(cells[2]):
                caps[monster_id] = cells[3:6]
        elif len(cells) == 5 and "`" in cells[1] and cells[3] in {"A", "B", "I", "S"}:
            for monster_id in IDS.findall(cells[1]):
                roster.append(monster_id)
                if cells[3] == "S":
                    steadfast.add(monster_id)
        elif len(cells) == 5 and SINGLE_ID.fullmatch(cells[0]):
            monster_id = SINGLE_ID.fullmatch(cells[0]).group(1)
            thresholds[monster_id] = cells[1:4]

    for cells in rows(INTENTS):
        if len(cells) == 4 and SINGLE_ID.fullmatch(cells[0]):
            details.append(SINGLE_ID.fullmatch(cells[0]).group(1))

    section = None
    for line in ASSIGNMENTS.splitlines():
        if line.startswith("### 弱怪"):
            section = "weak"
        elif line.startswith("### 强大怪物"):
            section = "strong"
        elif line.startswith("## 3."):
            section = None
        if not line.startswith("|") or section is None:
            continue
        cells = [cell.strip() for cell in line.strip("|").split("|")]
        if section == "weak" and len(cells) == 2:
            recovery.extend(IDS.findall(cells[1]))
        elif section == "strong" and len(cells) == 4 and SINGLE_ID.fullmatch(cells[1]):
            recovery.append(SINGLE_ID.fullmatch(cells[1]).group(1))

    return roster, steadfast, caps, thresholds, details, recovery


class MonsterRosterContract(unittest.TestCase):
    def test_embedded_tables_have_the_same_roster(self):
        roster, steadfast, caps, thresholds, details, recovery = catalog_sets()
        self.assertEqual(len(roster), 101)
        self.assertEqual(Counter(roster).most_common(1)[0][1], 1)
        self.assertEqual(Counter(details).most_common(1)[0][1], 1)
        self.assertEqual(set(details), set(roster))
        self.assertEqual(set(caps), set(roster) - steadfast)
        self.assertEqual(set(thresholds), set(caps))
        controlled = {monster_id for monster_id, values in caps.items() if values[1] != "—"}
        self.assertEqual(Counter(recovery).most_common(1)[0][1], 1)
        self.assertEqual(set(recovery), controlled)

    def test_runtime_cardinality_assertions_match_embedded_tables(self):
        roster, steadfast, *_ = catalog_sets()
        roster_match = re.search(r"result\.Count != (\d+)", CATALOG)
        steadfast_match = re.search(r"steadfastCount != (\d+)", CATALOG)
        self.assertIsNotNone(roster_match)
        self.assertIsNotNone(steadfast_match)
        self.assertEqual(int(roster_match.group(1)), len(roster))
        self.assertEqual(int(steadfast_match.group(1)), len(steadfast))
        self.assertIn(r'^`([A-Z0-9_]+)`(?:（.+）)?$', CATALOG)


if __name__ == "__main__":
    unittest.main()
