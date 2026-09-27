#if DEBUG
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Characters.Starts;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MaidenSuccubus.Patches;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignOrbTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_orbs";
    public override string Args => "confirm";
    public override string Description => "Destructive DS27 orb/reward/trial tests; disposable run only";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || CombatManager.Instance.IsInProgress || issuingPlayer.RunState.Players.Count != 1
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_orbs confirm outside combat in a disposable single-player Maiden run.");
        if (_running) return new CmdResult(false, "Orb tests already running.");
        return new CmdResult(Run(issuingPlayer), true, "Started destructive orb tests; see [DS27OrbTest] log.");
    }

    private static async Task Run(Player player)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 orb assertion failed: " + name);
            checks++;
        }
        try
        {
            TestMode.IsOn = true;
            // Inspect installed patches, not merely attributes in source code.
            foreach (var (targetName, patchName, prefix) in new[]
            {
                ("SelectCharacter", "SelectPostfix", false),
                ("OnEmbarkPressed", "EmbarkPostfix", false),
                ("OnUnreadyPressed", "UnreadyPostfix", false),
                ("PlayerChanged", "PlayerChangedPostfix", false),
                ("BeginRun", "BeginPrefix", true),
                ("OnSubmenuClosed", "ClosePrefix", true),
            })
            {
                var info = Harmony.GetPatchInfo(AccessTools.Method(typeof(NCharacterSelectScreen), targetName));
                var installed = prefix ? info?.Prefixes : info?.Postfixes;
                Check(installed?.Any(p => p.PatchMethod == AccessTools.Method(typeof(StarterRelicSelectorUiPatch), patchName)) == true,
                    "installed starter UI patch " + targetName);
            }
            var startup = Harmony.GetPatchInfo(AccessTools.Method(typeof(RunManager), nameof(RunManager.FinalizeStartingRelics)));
            Check(startup?.Prefixes.Any(p => p.PatchMethod == AccessTools.Method(typeof(StarterRelicSelectionPatch), "Prefix")) == true,
                "installed synchronous starter initialization prefix");
            var run = (RunState)player.RunState;
            foreach (RelicModel relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
            foreach (bool sky in new[] { false, true })
            {
                RelicModel orb = sky ? ModelDb.Relic<SkyOrb>().ToMutable() : ModelDb.Relic<TwinSoulChalice>().ToMutable();
                await RelicCmd.Obtain(orb, player);
                for (int corruption = -5; corruption <= 5; corruption++)
                {
                    CorruptionCmd.Set(run, corruption);
                    bool multi = sky || corruption is -5 or -4;
                    bool maxHp = sky || corruption is 4 or 5;
                    string description = System.Text.RegularExpressions.Regex.Replace(
                        orb.DynamicDescription.GetFormattedText(), @"\[[^\]]*\]", "");
                    string expectedText = sky
                        ? "战斗结束时，获得5点最大生命值。你可以选择任意数量的卡牌奖励。"
                        : maxHp
                            ? "战斗结束时，获得5点最大生命值。堕落值＜4：变奏。"
                            : multi
                                ? "战斗结束时，恢复5点生命值。你可以选择任意数量的卡牌奖励。堕落值＞-4：变奏。"
                                : "战斗结束时，恢复5点生命值。堕落值≥4或≤-4：变奏。";
                    Check(description == expectedText, "formatted runtime description and active branch");
                    await CreatureCmd.SetMaxAndCurrentHp(player.Creature, 60);
                    await CreatureCmd.SetCurrentHp(player.Creature, 40);
                    // Exercise the real relic hook and HP commands, not fake counters.
                    // The hook does not read room; full victory dispatch is a separate hand test.
                    await orb.AfterCombatVictory(null!);
                    Check(player.Creature.MaxHp == (maxHp ? 65 : 60), "max HP branch " + corruption);
                    Check(player.Creature.CurrentHp == 45, "real heal or max HP gain also heals five");
                    await CreatureCmd.SetCurrentHp(player.Creature, player.Creature.MaxHp - 1);
                    await orb.AfterCombatVictory(null!);
                    Check(maxHp ? player.Creature.CurrentHp == player.Creature.MaxHp - 1
                        : player.Creature.CurrentHp == player.Creature.MaxHp, "heal clamps but max HP grows");

                    foreach (int take in new[] { 0, 1, 2, 3 })
                    {
                        var reward = new CardReward(CardCreationOptions.ForRoom(player, RoomType.Monster), 3, player);
                        List<Reward> rewards = [reward];
                        Check(!orb.TryModifyRewards(player, rewards, null) && rewards.Count == 1,
                            "never adds an extra reward object");
                        int calls = 0;
                        var seen = new HashSet<CardModel>();
                        var before = player.Deck.Cards.ToHashSet();
                        var selector = new TestCardSelector();
                        selector.PrepareToSelectCardReward((cards, _) =>
                        {
                            Check(cards.Count == 3 - seen.Count && cards.Count > 0,
                                "same shrinking offer, never empty or rerolled");
                            calls++;
                            if (seen.Count == take) return new CardRewardSelection();
                            Check(seen.Add(cards[0].Card), "no duplicate selected instance");
                            return new CardRewardSelection { card = cards[0].Card };
                        });
                        using (CardSelectCmd.UseSelector(selector)) await RewardsCmd.OfferCustom(player, rewards);
                        int expectedCount = multi ? take : Math.Min(1, take);
                        int expectedCalls = multi ? Math.Min(take + 1, 3) : 1;
                        Check(calls == expectedCalls, "selector stops on skip/last card " + take);
                        Check(player.Deck.Cards.Except(before).Count() == expectedCount, "actual cards added " + take);
                        Check(player.Deck.Cards.Except(before).All(seen.Contains), "selected instances, not copies of a fresh reward");
                    }
                }
                await RelicCmd.Remove(orb);
            }

            // No relic: actual dispatcher still delivers exactly one trial callback.
            Check(player.Relics.Count == 0, "all starter relics removed");
            Check(run.IterateHookListeners(null).OfType<FourthRouteLifecycle>().Count() == 1, "one run listener without starter");
            M5Progress.Handle.Modify(run, state =>
            {
                state.FourthRouteQuestId = FourthRouteQuest.Diligence.ToString();
                state.FourthRouteQuestProgress = 0;
                state.FourthRouteQuestCompleted = false;
                state.FourthRouteRewardPending = false;
            });
            await Hook.AfterRestSiteSmith(run, player);
            Check(M5Progress.Handle.Get(run).FourthRouteQuestProgress == 1, "no starter smith counted once");

            await RelicCmd.Obtain<TwinSoulChalice>(player);
            var touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
            Check(touch.SetupForPlayer(player), "real Orobas setup recognizes starter");
            Check(touch.UpgradedRelic == ModelDb.Relic<SkyOrb>().Id, "framework refinement mapping");
            await RelicCmd.Obtain(touch, player);
            Check(player.GetRelic<TwinSoulChalice>() == null && player.GetRelic<SkyOrb>() != null,
                "actual ancient acquisition replaces starter");
            await Hook.AfterRestSiteSmith(run, player);
            Check(M5Progress.Handle.Get(run).FourthRouteQuestProgress == 2, "upgraded starter smith still counted once");
            Check(run.IterateHookListeners(null).OfType<FourthRouteLifecycle>().Count() == 1, "replacement adds no second listener");

            foreach (StarterRelicKind kind in new[] { StarterRelicKind.Omnipotent, StarterRelicKind.Hero, (StarterRelicKind)99 })
            {
                foreach (RelicModel relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
                await RelicCmd.Obtain<Circlet>(player);
                await RelicCmd.Obtain<TwinSoulChalice>(player);
                var original = player.GetRelic<TwinSoulChalice>()!;
                int originalFloor = original.FloorAddedToDeck;
                StarterRelicChoice.Handle.Set(run, player.NetId, new StarterRelicChoiceState { Kind = kind });
                StarterRelicSelection.Apply(player);
                var expectedId = StarterRelicSelection.Preview(kind).Id;
                Check(player.Relics.Count == 2 && player.Relics[0] is Circlet && player.Relics[1].Id == expectedId,
                    "new-run choice replaces exactly the default slot");
                Check(player.Relics[1].FloorAddedToDeck == originalFloor, "starter floor preserved");
                Check(StarterRelicChoice.Handle.Get(player).Applied, "selection persisted as applied");
                var selectedInstance = player.Relics[1];
                StarterRelicChoice.Handle.Modify(player, state => state.Kind = StarterRelicChoice.Next(kind));
                StarterRelicSelection.Apply(player);
                Check(ReferenceEquals(selectedInstance, player.Relics[1]), "repeated initialization cannot replace acquired relics");
            }
            foreach (RelicModel relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
            await RelicCmd.Obtain<SkyOrb>(player);
            StarterRelicChoice.Handle.Set(run, player.NetId, new StarterRelicChoiceState { Kind = StarterRelicKind.Hero });
            StarterRelicSelection.Apply(player);
            Check(player.Relics.Single() is SkyOrb, "missing original starter never overwrites an ancient replacement");
            var foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
            var foreignRelics = foreign.Relics.ToArray();
            StarterRelicSelection.Apply(foreign);
            Check(foreign.Relics.SequenceEqual(foreignRelics), "other character remains unchanged");
            MaidenSuccubusMod.Logger.Info($"[DS27OrbTest] PASS {checks} assertions; disposable run modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27OrbTest] FAIL " + ex);
            throw;
        }
        finally
        {
            TestMode.IsOn = previousTestMode;
            _running = false;
        }
    }
}
#endif
