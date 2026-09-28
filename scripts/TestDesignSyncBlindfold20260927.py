"""Wiring/text regression only; native queue/UI behavior is tested in ms_test_blindfold."""
import json
import re
import unittest
from TestDesignSyncNeutral20260927 import read


class BlindfoldContracts(unittest.TestCase):
    def test_exact_formal_description_and_registration(self):
        design = read("DesignDoc.md")
        expected = "无法看到敌人意图。可以看到下一次遭遇战的内容。"
        self.assertIn("#### 眼罩 `[RELIC-EVENT-001 · READY]`\n\n" + expected, design)
        loc = json.loads(read("MaidenSuccubus/localization/zhs/relics.json"))
        self.assertEqual(loc["MAIDEN_SUCCUBUS_RELIC_BLINDFOLD.description"],
                         "无法看到敌人意图。\n可以看到下一次遭遇战的内容。")
        source = read("src/Relics/MvpRelics.cs")
        self.assertIn("[RegisterRelic(typeof(MSRelicPool))]\npublic sealed class Blindfold", source)
        self.assertEqual(json.loads(read("docs/content_contract_20260824.json"))["relics"].count("Blindfold"), 1)

    def test_local_character_actual_inventory_and_removal_guards(self):
        source = read("src/Core/Relics/BlindfoldPresentation.cs")
        for token in ("player?.Character is MaidenSuccubusCharacter", "LocalContext.IsMe(player)",
                      "!relic.HasBeenRemovedFromState && !relic.IsMelted", "!relic.IsMutable",
                      "!relic.Owner.Relics.Contains(relic)", "IsMonster: true, IsEnemy: true",
                      "combat.Players.FirstOrDefault(LocalContext.IsMe)"):
            self.assertIn(token, source)
        self.assertNotIn("state.Players.Any", read("src/Patches/BlindfoldIntentPatch.cs"))

    def test_preview_has_no_cached_act_or_rng_or_future_generation(self):
        source = read("src/Core/Relics/BlindfoldPresentation.cs")
        for token in ("Peek(run.Act, type)", "!act.IsMutable", "act.PullNextEncounter(type)", "encounter.AllPossibleMonsters",
                      "catch (DivideByZeroException)", "catch (ArgumentOutOfRangeException)",
                      "catch (InvalidOperationException)"):
            self.assertIn(token, source)
        code = re.sub(r"//[^\n]*", "", source)
        for forbidden in ("MarkRoomVisited(", ".Rng", "GenerateRooms(", "GenerateMonstersWithSlots(",
                          "SetBossEncounter(", "RunState.Create", "Traverse.", "static ActModel", "static RunState"):
            self.assertNotIn(forbidden, code)

    def test_category_labels_and_random_composition_are_not_misrepresented(self):
        loc = json.loads(read("MaidenSuccubus/localization/zhs/static_hover_tips.json"))
        for category in ("MONSTER", "ELITE", "BOSS"):
            stem = "MAIDENSUCCUBUS_BLINDFOLD_" + category
            self.assertIn("下一次", loc[stem + ".title"])
            self.assertIn("{Encounter}", loc[stem + ".description"])
            self.assertIn("可能出现的敌人：{Monsters}", loc[stem + ".description"])
        self.assertIn("不预测问号房间类型", loc["MAIDENSUCCUBUS_BLINDFOLD_MONSTER.description"])
        self.assertIn("没有可读取", loc["MAIDENSUCCUBUS_BLINDFOLD_UNAVAILABLE.description"])

    def test_hidden_control_is_restored_only_if_owned_and_memory_is_weak(self):
        source = read("src/Patches/BlindfoldIntentPatch.cs")
        for token in ("ConditionalWeakTable<NCreature, VisibilityBeforeHide>",
                      "new(node.IntentContainer.Visible)", "IntentContainer.Visible = false",
                      "Hidden.TryGetValue(__instance, out var previous)",
                      "IntentContainer.Visible = previous.Visible", "Hidden.Remove(__instance)",
                      "__result = Task.CompletedTask"):
            self.assertIn(token, source)
        self.assertNotIn("Colors.Transparent", source)
        self.assertEqual(source.count("Safe.Run("), 3)

    def test_both_hover_routes_preserve_power_tooltips(self):
        source = read("src/Patches/BlindfoldIntentPatch.cs")
        for token in ('HarmonyPatch(typeof(NIntent), "OnHovered")', "Creature ___owner",
                      "nameof(Creature.HoverTips), MethodType.Getter", "BlindfoldPresentation.PowerTips(__instance)"):
            self.assertIn(token, source)
        service = read("src/Core/Relics/BlindfoldPresentation.cs")
        self.assertIn("foreach (PowerModel power in creature.Powers)", service)
        self.assertIn("tips.MegaTryAddingTip(tip)", service)

    def test_acquisition_removal_refresh_is_awaited_and_safe(self):
        relic = read("src/Relics/MvpRelics.cs")
        self.assertIn("AfterObtained() => BlindfoldPresentation.Refresh(Owner)", relic)
        self.assertIn("AfterRemoved() => BlindfoldPresentation.Refresh(Owner)", relic)
        source = read("src/Core/Relics/BlindfoldPresentation.cs")
        for token in ("enemy => enemy.IsAlive", "GodotObject.IsInstanceValid(node)", "node.HideHoverTips()",
                      "refresh = node.RefreshIntents()", "await refresh", "catch (Exception ex)"):
            self.assertIn(token, source)
        self.assertNotIn("Task.Run", source)

    def test_read_only_runtime_command_has_real_native_queue_and_snapshot_assertions(self):
        source = read("src/ConsoleCommands/DesignBlindfoldTestConsoleCmd.cs")
        self.assertTrue(source.startswith("#if DEBUG"))
        self.assertIn('CmdName => "ms_test_blindfold"', source)
        for token in ("beforeActs == SnapshotActs()", "beforeRngs == Rngs()", "copy.MarkRoomVisited(type)",
                      "copy.PullNextEncounter(type)", "ActModel.FromSave(copy.ToSave())",
                      "NormalEncounterIds.Clear()", "SecondBossId = null", "player.NetId ^ 1UL",
                      "first.SequenceEqual", "enemy.HoverTips.SequenceEqual", "HasEffect(otherCharacter)",
                      "HasEffect(remote)", "HasEffect(detached)"):
            self.assertIn(token, source)
        for forbidden in ("liveAct.MarkRoomVisited", "run.Act.MarkRoomVisited", "RelicCmd.",
                          "CardPileCmd.", "CreatureCmd.", "TestMode.IsOn =", "LocalContext.NetId ="):
            self.assertNotIn(forbidden, source)


if __name__ == "__main__":
    unittest.main()
