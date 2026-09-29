"""Check the embedded monster catalogue's ID sets before game initialization."""

from collections import Counter
from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[1]
GAME_MONSTERS = (
    ROOT.parents[1]
    / "_decompiled/sts2-v0.111.0/MegaCrit.Sts2.Core.Models.Monsters"
)
ASSIGNMENTS = (ROOT / "docs/EROTIC_ATTACK_ASSIGNMENTS.md").read_text(encoding="utf-8")
INTENTS = (ROOT / "docs/EROTIC_ATTACK_INTENTS.md").read_text(encoding="utf-8")
CATALOG = (ROOT / "src/Core/Intents/EroticAttackCatalog.cs").read_text(encoding="utf-8")
INVASION = (ROOT / "src/Commands/InvasionCmd.cs").read_text(encoding="utf-8")
IDS = re.compile(r"`([A-Z0-9_]+)`")
SINGLE_ID = re.compile(r"`([A-Z0-9_]+)`(?:（.+）)?\Z")
MOVE_ID = re.compile(r"`([A-Za-z0-9_]+)`\Z")


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
    def test_roster_ids_exist_in_declared_game_version(self):
        self.assertTrue(GAME_MONSTERS.is_dir(), GAME_MONSTERS)
        roster, *_ = catalog_sets()
        for monster_id in roster:
            class_name = "".join(part.title() for part in monster_id.split("_"))
            with self.subTest(monster=monster_id):
                self.assertTrue((GAME_MONSTERS / f"{class_name}.cs").is_file())

    def test_recovery_moves_exist_in_versioned_state_machines(self):
        strong_table = False
        checked = 0
        for line in ASSIGNMENTS.splitlines():
            if line.startswith("### 强大怪物"):
                strong_table = True
                continue
            if strong_table and line.startswith("## 3."):
                break
            if not strong_table or not line.startswith("|"):
                continue
            cells = [cell.strip() for cell in line.strip("|").split("|")]
            if len(cells) != 4:
                continue
            monster = SINGLE_ID.fullmatch(cells[1])
            move = MOVE_ID.fullmatch(cells[2])
            if monster is None or move is None:
                continue
            checked += 1
            class_name = "".join(part.title() for part in monster.group(1).split("_"))
            with self.subTest(monster=monster.group(1), move=move.group(1)):
                while (source_path := GAME_MONSTERS / f"{class_name}.cs").is_file():
                    source = source_path.read_text(encoding="utf-8")
                    if f'new MoveState("{move.group(1)}"' in source:
                        break
                    parent = re.search(rf"\bclass {class_name}\s*:\s*(\w+)", source)
                    if parent is None:
                        self.fail(f"{class_name} has no state {move.group(1)}")
                    class_name = parent.group(1)
                else:
                    self.fail(f"{class_name} source missing for {move.group(1)}")
        self.assertEqual(checked, 26)

    def test_embedded_tables_have_the_same_roster(self):
        roster, steadfast, caps, thresholds, details, recovery = catalog_sets()
        self.assertEqual(len(roster), 102)
        self.assertEqual(Counter(roster).most_common(1)[0][1], 1)
        self.assertEqual(Counter(details).most_common(1)[0][1], 1)
        self.assertEqual(set(details), set(roster))
        self.assertEqual(set(caps), set(roster) - steadfast)
        self.assertEqual(set(thresholds), set(caps))
        controlled = {monster_id for monster_id, values in caps.items() if values[1] != "—"}
        self.assertEqual(Counter(recovery).most_common(1)[0][1], 1)
        self.assertEqual(set(recovery), controlled)

    def test_parafright_is_only_steadfast_without_erotic_intents(self):
        roster, steadfast, caps, thresholds, details, recovery = catalog_sets()
        self.assertIn("PARAFRIGHT", roster)
        self.assertIn("PARAFRIGHT", steadfast)
        self.assertNotIn("PARAFRIGHT", caps)
        self.assertNotIn("PARAFRIGHT", thresholds)
        self.assertIn("PARAFRIGHT", details)
        self.assertNotIn("PARAFRIGHT", recovery)
        self.assertIn("if (preferred == \"S\")", CATALOG)

    def test_runtime_cardinality_assertions_match_embedded_tables(self):
        roster, steadfast, *_ = catalog_sets()
        roster_match = re.search(r"result\.Count != (\d+)", CATALOG)
        steadfast_match = re.search(r"steadfastCount != (\d+)", CATALOG)
        self.assertIsNotNone(roster_match)
        self.assertIsNotNone(steadfast_match)
        self.assertEqual(int(roster_match.group(1)), len(roster))
        self.assertEqual(int(steadfast_match.group(1)), len(steadfast))
        self.assertIn(r'^`([A-Z0-9_]+)`(?:（.+）)?$', CATALOG)

    def test_each_candidate_agrees_across_caps_thresholds_and_details(self):
        roster, steadfast, caps, thresholds, details, _ = catalog_sets()
        detailed = {
            SINGLE_ID.fullmatch(cells[0]).group(1): cells[1:4]
            for cells in rows(INTENTS)
            if len(cells) == 4 and SINGLE_ID.fullmatch(cells[0])
        }
        self.assertEqual(set(detailed), set(roster))
        self.assertEqual(set(caps), set(roster) - steadfast)
        for monster_id, cap_values in caps.items():
            with self.subTest(monster=monster_id):
                for cap, threshold, detail in zip(cap_values, thresholds[monster_id], detailed[monster_id]):
                    present = cap != "—"
                    self.assertEqual(threshold != "—", present)
                    self.assertEqual(detail != "—", present)
                    if present:
                        self.assertGreater(int(cap), 0)
                        self.assertGreater(int(threshold), 0)
                        self.assertEqual(int(threshold) % 5, 0)

    def test_every_intent_has_core_values_consumed_by_runtime_parser(self):
        _, steadfast, caps, _, _, _ = catalog_sets()
        detailed = {
            SINGLE_ID.fullmatch(cells[0]).group(1): cells[1:4]
            for cells in rows(INTENTS)
            if len(cells) == 4 and SINGLE_ID.fullmatch(cells[0])
        }
        registered = set(re.findall(
            r'"([^"]+)"', INVASION.split("public static async Task<bool> Resolve")[0]
        ))
        counts = [0, 0, 0]
        for monster_id, (a_cap, b_cap, i_cap) in caps.items():
            if monster_id in steadfast:
                continue
            desire, control, invasion = detailed[monster_id]
            with self.subTest(monster=monster_id):
                if a_cap != "—":
                    counts[0] += 1
                    self.assertRegex(desire, r"欲望增加[1-9]\d*")
                    if "造成" in desire:
                        self.assertRegex(desire, r"造成[1-9]\d*点伤害")
                    if "伤害" in desire and "次" in desire:
                        hits = re.search(r"伤害(\d+)次", desire)
                        self.assertIsNotNone(hits)
                        self.assertGreater(int(hits.group(1)), 1)
                if b_cap != "—":
                    counts[1] += 1
                    self.assertRegex(control, r"需要[1-9]\d*点格挡")
                    self.assertRegex(control, r"拘束(?:攻击|技能|能力)牌")
                    self.assertRegex(control, r"挣脱值[1-9]\d*")
                if i_cap != "—":
                    counts[2] += 1
                    self.assertRegex(invasion, r"造成[1-9]\d*点伤害")
                    curse = re.search(r"将(\d+)张“([^”]+)”加入牌组", invasion)
                    self.assertIsNotNone(curse)
                    self.assertEqual(curse.group(1), "1")
                    self.assertIn(curse.group(2), registered)
        self.assertEqual(counts, [84, 83, 72])

    def test_later_intent_requires_same_monster_prerequisite(self):
        _, _, caps, _, _, _ = catalog_sets()
        for monster_id, (_, prerequisite, later) in caps.items():
            with self.subTest(monster=monster_id):
                if later != "—":
                    self.assertNotEqual(prerequisite, "—")
        self.assertIn("if (spec.Control is null)", CATALOG)
        self.assertIn("Invasion intent requires this monster's control", CATALOG)

    def test_default_invasion_curse_is_resolved_to_registered_card(self):
        self.assertIn('CurseName: curse.Success ? curse.Groups[1].Value : "精液"', CATALOG)
        self.assertIn('"精液" => await Add<SemenCurse>(target)', INVASION)


if __name__ == "__main__":
    unittest.main()
