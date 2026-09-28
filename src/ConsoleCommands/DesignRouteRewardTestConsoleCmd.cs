#if DEBUG
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;
using MaidenSuccubus.Patches;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>Destructive actual claim/model coverage, not a natural UI or whole-save test.</summary>
public sealed class DesignRouteRewardTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_route_reward";
    public override string Args => "confirm";
    public override string Description => "Destructive 42 trial reward claims; disposable single-player run only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        if (_running || player?.Character is not MaidenSuccubusCharacter || player.RunState is not RunState run
            || run.Players.Count != 1 || CombatManager.Instance.IsInProgress || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_route_reward confirm outside combat in a disposable single-player Maiden run.");
        return new CmdResult(Run(player, run), true, "Destructive reward tests started; see [DS27RewardTest].");
    }
    private static async Task Run(Player player, RunState run)
    {
        _running = true;
        bool previous = TestMode.IsOn;
        int checks = 0;
        void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException("DS27 reward: " + name);
            checks++;
        }
        async Task<FourthRouteRewardOffer> Prepare(FourthRouteQuest quest, FourthTrialPhase phase)
        {
            M5Progress.Handle.Modify(run, data => { data.FourthRouteQuestId = ""; data.FourthRouteTrial = null; });
            foreach (var relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
            await CardPileCmd.RemoveFromDeck(player.Deck.Cards.ToArray(), showPreview: false);
            for (int i = 0; i < 5; i++) await CardPileCmd.Add(run.CreateCard<MaidenStrike>(player), PileType.Deck, skipVisuals: true);
            M5Progress.Handle.Modify(run, data =>
            {
                data.FourthRouteQuestId = quest.ToString();
                data.FourthRouteAlignment = FourthRouteProgressService.AlignmentOf(quest).ToString();
                data.FourthRouteTrial = new() { Phase = phase, Progress = FourthRouteTrialRules.Target(quest, FourthRouteTrialRules.Number(phase)) };
                data.FourthRouteRelicStage = 0;
                data.FourthRouteRewardPending = true;
                data.FourthRouteRewardClaimed = false;
                data.FourthRouteRewardCorruptionApplied = false;
                data.FourthRouteOpening = null;
                data.FourthRouteEndingChecked = false;
            });
            await RelicCmd.Obtain(FourthRouteProgressService.CreateRelicPreview(quest, 0), player);
            if (phase == FourthTrialPhase.SecondReward)
                await RelicCmd.Obtain(ModelDb.Relic<FourthRouteFragmentRelic>().ToMutable(), player);
            CorruptionCmd.Set(run, 0);
            return new(quest, phase);
        }
        try
        {
            TestMode.IsOn = true;
            Check(Harmony.GetPatchInfo(AccessTools.Method(typeof(Hook), nameof(Hook.AfterCombatVictory)))?
                .Postfixes.Any(p => p.PatchMethod.DeclaringType == typeof(FourthRouteVictoryRewardPatch)) == true,
                "actual full victory task wrapper installed");
            var original = new TaskCompletionSource();
            var gated = FourthRouteVictoryRewardPatch.AfterVictory(original.Task, run, run.CurrentRoom as CombatRoom ?? null!);
            Check(!gated.IsCompleted, "reward bridge cannot run before original victory hooks");
            original.SetResult();
            await gated;
            bool faultPropagated = false;
            try { await FourthRouteVictoryRewardPatch.AfterVictory(Task.FromException(new InvalidOperationException("expected")), run, null!); }
            catch (InvalidOperationException ex) when (ex.Message == "expected") { faultPropagated = true; }
            Check(faultPropagated, "original victory failure not swallowed or rewarded");

            foreach (var quest in Enum.GetValues<FourthRouteQuest>())
            foreach (var phase in new[] { FourthTrialPhase.FirstReward, FourthTrialPhase.SecondReward, FourthTrialPhase.ThirdReward })
            {
                var offer = await Prepare(quest, phase);
                var preview = FourthRouteProgressService.CreateRelicPreview(quest, offer.Stage);
                string description = preview.DynamicDescription.GetFormattedText();
                Check(!description.Contains("以推进试炼") && !description.Contains("允许进入第四层"), "unowned preview does not reveal future tasks");
                var wrong = offer with { Quest = quest == FourthRouteQuest.Pride ? FourthRouteQuest.Humility : FourthRouteQuest.Pride };
                Check(!await FourthRouteRewardFlow.Claim(player, wrong), "stale route receipt cannot claim");
                var foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
                foreign.RunState = run;
                Check(!await FourthRouteRewardFlow.Claim(foreign, offer), "other character cannot claim");
                var selector = new TestCardSelector();
                selector.PrepareToSelect(player.Deck.Cards.Take(quest == FourthRouteQuest.Generosity && offer.Stage == 2 ? 2 : 1).ToArray());
                selector.PrepareToSelectCardReward((_, _) => default);
                using (CardSelectCmd.UseSelector(selector))
                    Check(await FourthRouteRewardFlow.Claim(player, offer), "actual stage replacement and pickup complete");
                Check(player.Relics.OfType<FourthRouteRelic>().Count() == 1
                    && player.Relics.OfType<FourthRouteRelic>().Single().Stage == offer.Stage, "one correct-stage relic, old dormant removed");
                Check(!player.Relics.OfType<FourthRouteFragmentRelic>().Any(), "second reward consumes actual fragment");
                int corruption = FourthRouteProgressService.AlignmentOf(quest) == FourthRouteAlignment.Dark ? 1 : -1;
                Check(CorruptionQuery.Get(run) == corruption, "one route-aligned corruption step");
                Check(FourthRouteProgressService.HasFourthActQualification(run) == (offer.Stage == 4),
                    "only completed third claim grants qualification; not an automatic boss reward");
                Check(!FourthRouteProgressService.CanEnterFourthAct(run), "qualification never enables unfinished act");
                int gold = player.Gold, cards = player.Deck.Cards.Count;
                Check(!await FourthRouteRewardFlow.Claim(player, offer), "double click cannot repeat claim");
                Check(player.Gold == gold && player.Deck.Cards.Count == cards && CorruptionQuery.Get(run) == corruption,
                    "duplicate receipt cannot repeat pickup or corruption");
                var nextPhase = phase == FourthTrialPhase.FirstReward ? FourthTrialPhase.SecondReward : FourthTrialPhase.FirstReward;
                M5Progress.Handle.Modify(run, data => data.FourthRouteTrial = new() { Phase = nextPhase });
                Check(!await FourthRouteRewardFlow.Claim(player, offer), "old page cannot claim newly pending phase");
            }

            var delayedOffer = await Prepare(FourthRouteQuest.Generosity, FourthTrialPhase.FirstReward);
            var delayedSelector = new TestCardSelector();
            var selection = delayedSelector.SetupForAsyncCardSelection();
            var chosen = player.Deck.Cards.First();
            using (CardSelectCmd.UseSelector(delayedSelector))
            {
                Task<bool> claim = FourthRouteRewardFlow.Claim(player, delayedOffer);
                Check(!claim.IsCompleted, "claim awaits native pickup selector");
                Check(!await FourthRouteRewardFlow.Claim(player, delayedOffer), "concurrent claim cannot open another selector");
                selection.SetResult([chosen]);
                Check(await claim && player.Deck.Cards.Count == 4, "original selector resolves once and removes one");
            }
            Check(CorruptionQuery.Get(run) == -1, "concurrent claim changed corruption once");
            MaidenSuccubusMod.Logger.Info($"[DS27RewardTest] PASS {checks}; native UI, actual end-of-combat sequencing and full save/load still require hand tests.");
        }
        catch (Exception ex) { MaidenSuccubusMod.Logger.Error("[DS27RewardTest] FAIL " + ex); throw; }
        finally { TestMode.IsOn = previous; _running = false; }
    }
}
#endif
