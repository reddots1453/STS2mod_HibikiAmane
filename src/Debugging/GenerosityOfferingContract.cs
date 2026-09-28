#if DEBUG
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Data;
using MaidenSuccubus.Patches;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Rewards;

namespace MaidenSuccubus.Debugging;

internal static class GenerosityOfferingContract
{
    internal static async Task Run(Player player, Action<bool, string> check)
    {
        var run = (RunState)player.RunState;
        async Task Prepare(FourthTrialPhase phase, int cards = 4)
        {
            foreach (var old in player.Relics.ToArray()) await RelicCmd.Remove(old);
            await CardPileCmd.RemoveFromDeck(player.Deck.Cards.ToArray(), showPreview: false);
            for (int i = 0; i < cards; i++)
                await CardPileCmd.Add(run.CreateCard<MaidenStrike>(player), PileType.Deck, skipVisuals: true);
            M5Progress.Handle.Modify(run, data =>
            {
                data.FourthRouteQuestId = FourthRouteQuest.Generosity.ToString();
                data.FourthRouteTrial = new() { Phase = phase };
                data.FourthRouteRelicStage = phase == FourthTrialPhase.Complete ? 4 : 0;
            });
            var relic = (GenerosityRouteRelic)ModelDb.Relic<GenerosityRouteRelic>().ToMutable();
            relic.Stage = phase == FourthTrialPhase.Complete ? 4 : 0;
            await RelicCmd.Obtain(relic, player);
        }
        GenerosityOfferingGroup NewGroup() => new(ModelDb.Relic<Circlet>().ToMutable(), player);
        foreach (var phase in Enum.GetValues<FourthTrialPhase>())
        {
            await Prepare(phase);
            bool expected = phase is FourthTrialPhase.First or FourthTrialPhase.Second or FourthTrialPhase.Third or FourthTrialPhase.Complete;
            check(GenerosityOffering.CanOffer(player) == expected, "offering visibility follows exact trial phase");
        }
        await Prepare(FourthTrialPhase.Complete, 0);
        check(!GenerosityOffering.CanOffer(player), "awakened with no removable cards hides no-benefit offering");
        var foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
        foreign.RunState = run;
        check(!GenerosityOffering.CanOffer(foreign), "other characters cannot offer this route's relic");

        foreach (var phase in new[] { FourthTrialPhase.First, FourthTrialPhase.Second, FourthTrialPhase.Third })
        {
            await Prepare(phase);
            var group = NewGroup();
            group.OnSkipped();
            check(FourthRouteProgressService.Trial(run).Phase == phase, "ordinary skip never grants offering credit");
            group = NewGroup();
            var restored = Reward.FromSerializable(group.ToSerializable(), player);
            check(restored is RelicReward reward && reward.Relic?.Id == group.RelicChoice.Relic!.Id,
                "native save restores the exact linked relic");
            check(await group.Choices[1].SelectUnsynchronized(), "explicit offering succeeds");
            check(group.SuccessfullySelected && group.Resolved && FourthRouteTrialRules.Pending(FourthRouteProgressService.Trial(run).Phase),
                "offering advances exactly one trial and completes the parent reward");
            check(!player.Relics.OfType<Circlet>().Any(), "offering never grants linked relic");
            check(!await group.Choices[0].SelectUnsynchronized() && !await group.Choices[1].SelectUnsynchronized(),
                "both branches reject replay after offering");
            check(!GenerosityOffering.CanOffer(player), "second reward's offer becomes unavailable while trial reward is pending");
        }

        await Prepare(FourthTrialPhase.First);
        var taking = NewGroup();
        check(await taking.Choices[0].SelectUnsynchronized() && player.Relics.OfType<Circlet>().Count() == 1,
            "take branch grants the original relic once");
        check(!await taking.Choices[1].SelectUnsynchronized() && FourthRouteProgressService.Trial(run).Phase == FourthTrialPhase.First,
            "taking relic prevents offering without trial progress");
        check(!await taking.Choices[0].SelectUnsynchronized() && player.Relics.OfType<Circlet>().Count() == 1, "duplicate take is harmless");

        await Prepare(FourthTrialPhase.Complete);
        var deleting = NewGroup();
        var selected = player.Deck.Cards.Take(2).ToArray();
        var selector = new TestCardSelector();
        var selection = selector.SetupForAsyncCardSelection();
        using (CardSelectCmd.UseSelector(selector))
        {
            Task<bool> pending = deleting.Choices[1].SelectUnsynchronized();
            check(!pending.IsCompleted, "awakened offering waits for actual card choice");
            check(!await deleting.Choices[0].SelectUnsynchronized(), "pending deletion locks mutually exclusive relic branch");
            check(!await deleting.Choices[1].SelectUnsynchronized(), "pending deletion cannot start a second picker");
            selection.SetResult(selected);
            check(await pending, "confirmed deletion completes offering");
        }
        check(player.Deck.Cards.Count == 2 && selected.All(card => !player.Deck.Cards.Contains(card)), "awakening removes selected two cards");
        check(!player.Relics.OfType<Circlet>().Any(), "awakening does not also grant relic");

        // Real native rewards stack and local/remote selection overload, not just direct callbacks.
        var previousSelector = RewardsSet.testSelector;
        try
        {
            foreach (bool remote in new[] { false, true })
            {
                await Prepare(FourthTrialPhase.First);
                var group = NewGroup();
                RewardsSet.testSelector = async set =>
                {
                    int encoded = GenerosityLocalRewardIndexPatch.IndexOf(set.Rewards, group.Choices[1]);
                    check(encoded == GenerosityOfferingRules.ChildIndexBase + 1, "child index deterministic across peers");
                    if (remote)
                    {
                        Task task = Task.CompletedTask;
                        check(!GenerosityRemoteRewardIndexPatch.Prefix(RunManager.Instance.RewardsSetSynchronizer,
                            player, encoded, ref task), "remote child handled by scoped adapter");
                        await task;
                    }
                    else await RunManager.Instance.RewardsSetSynchronizer.SelectLocalReward(group.Choices[1]);
                    check(RunManager.Instance.RewardsSetSynchronizer.IsRewardsSetCompleted(set), "native rewards stack completes after child selection");
                    check(!GenerosityCompletedMessagePatch.Prefix(RunManager.Instance.RewardsSetSynchronizer,
                        new RewardSelectedMessage { setId = set.Id, rewardIndex = encoded }, player.NetId),
                        "completed child message cannot select from a later reward set");
                };
                await new RewardsSet(player).WithCustomRewards([group]).Offer();
            }
            await Prepare(FourthTrialPhase.First);
            var allocated = ModelDb.Relic<Circlet>().ToMutable();
            RewardsSet.testSelector = async set =>
            {
                check(GenerosityOffering.IsPendingTreasure(allocated), "allocated treasure remains pending until choice");
                var group = (GenerosityOfferingGroup)set.Rewards.Single();
                check(group.Player == player && group.RelicChoice.Relic == allocated, "post-allocation choice belongs to recipient and same relic");
                await RunManager.Instance.RewardsSetSynchronizer.SelectLocalReward(group.Choices[1]);
            };
            await GenerosityOffering.ObtainAllocated(allocated, player);
            check(!GenerosityOffering.IsPendingTreasure(allocated) && !player.Relics.Contains(allocated), "offered treasure clears pending state without obtaining");
        }
        finally { RewardsSet.testSelector = previousSelector; }

        foreach (var method in new[] { GenerosityLocalRewardIndexPatch.TargetMethod(), GenerosityTreasureObtainPatch.TargetMethod() })
            check(Harmony.GetPatchInfo(method)?.Transpilers.Any(p => p.PatchMethod.DeclaringType == typeof(GenerosityLocalRewardIndexPatch)
                || p.PatchMethod.DeclaringType == typeof(GenerosityTreasureObtainPatch)) == true, "actual native async call site patched");
        check(GenerosityLocalRewardIndexPatch.Transpiler(PatchProcessor.GetOriginalInstructions(GenerosityLocalRewardIndexPatch.TargetMethod()))
            .Count(code => code.Calls(AccessTools.Method(typeof(GenerosityLocalRewardIndexPatch), nameof(GenerosityLocalRewardIndexPatch.IndexOf)))) == 1,
            "compiled native local selection IL actually rewritten, not merely registered");
        check(GenerosityTreasureObtainPatch.Transpiler(PatchProcessor.GetOriginalInstructions(GenerosityTreasureObtainPatch.TargetMethod()))
            .Count(code => code.Calls(AccessTools.Method(typeof(GenerosityOffering), nameof(GenerosityOffering.ObtainAllocated)))) == 1,
            "compiled native treasure IL actually rewritten, not merely registered");
    }
}
#endif
