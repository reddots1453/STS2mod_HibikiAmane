#if DEBUG
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
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
    public override string Args => "confirm [merchant]";
    public override string Description => "Destructive trial reward tests; optional merchant mode tests native fragment purchases in a disposable shop";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? player, string[] args)
    {
        bool merchant = args.Length == 2 && args[1] == "merchant";
        if (_running || player?.Character is not MaidenSuccubusCharacter || player.RunState is not RunState run
            || run.Players.Count != 1 || CombatManager.Instance.IsInProgress || args.Length < 1 || args[0] != "confirm"
            || args.Length != 1 && !merchant || merchant && run.CurrentRoom is not MerchantRoom)
            return new CmdResult(false, "Use ms_test_route_reward confirm [merchant] in a disposable single-player Maiden run; merchant mode requires an actual shop room. Tests remove your deck/relics and alter gold.");
        return new CmdResult(Run(player, run, merchant), true, "Destructive reward tests started; see [DS27RewardTest].");
    }
    private static async Task Run(Player player, RunState run, bool merchant)
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
                data.FourthRouteFragmentPending = false;
                data.FourthRouteFragmentOffered = false;
                data.FourthRouteFragmentPurchased = false;
                data.FourthRouteSacrificeCompleted = false;
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
            if (merchant)
            {
                TestMode.IsOn = false;
                await CheckMerchant(player, run, Prepare, Check);
                MaidenSuccubusMod.Logger.Info($"[DS27RewardTest] PASS merchant {checks}; real inventory generation and purchase wrapper tested; natural room travel, rendering and full save/load still require hand tests.");
                return;
            }
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

            // Subsequent assertions use the live relic inventory, whose
            // holders are intentionally not created in engine TestMode.
            TestMode.IsOn = false;

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

    private static async Task CheckMerchant(Player player, RunState run,
        Func<FourthRouteQuest, FourthTrialPhase, Task<FourthRouteRewardOffer>> prepare,
        Action<bool, string> check)
    {
        check(Harmony.GetPatchInfo(AccessTools.Method(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant)))?
            .Postfixes.Any(p => p.PatchMethod.DeclaringType == typeof(FourthRouteMerchantPatch)) == true,
            "native merchant creation patch installed");
        async Task PrepareFragment()
        {
            var offer = await prepare(FourthRouteQuest.Pride, FourthTrialPhase.FirstReward);
            check(await FourthRouteRewardFlow.Claim(player, offer), "first reward actually enters fragment phase");
            check(FourthRouteProgressService.Trial(run).Phase == FourthTrialPhase.Fragment
                && M5Progress.Handle.Get(run).FourthRouteFragmentPending, "first claim makes fragment eligible without granting one");
        }

        await PrepareFragment();
        var foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
        foreign.RunState = run;
        var foreignInventory = new MerchantInventory(foreign);
        FourthRouteMerchantPatch.Postfix(foreignInventory);
        check(foreignInventory.RelicEntries.Count == 0 && !M5Progress.Handle.Get(run).FourthRouteFragmentOffered,
            "other-character patch invocation cannot consume this run's fragment offer");
        MerchantInventory skipped = MerchantInventory.CreateForNormalMerchant(player);
        MerchantRelicEntry skippedFragment = skipped.RelicEntries.Single(entry => entry.Model is FourthRouteFragmentRelic);
        check(skipped.RelicEntries.Count == 3 && skippedFragment.Cost == 100, "one native relic slot replaced at exact fragment price");
        string routeTitle = FourthRouteProgressService.CreateRelicPreview(FourthRouteQuest.Pride).Id.Entry + ".title";
        check(((FourthRouteFragmentRelic)skippedFragment.Model!).RouteTitleKey == routeTitle,
            "merchant fragment is named for the selected route");
        check(M5Progress.Handle.Get(run).FourthRouteFragmentOffered, "generation records the one offer");
        var afterSkip = MerchantInventory.CreateForNormalMerchant(player);
        check(!afterSkip.RelicEntries.Any(entry => entry.Model is FourthRouteFragmentRelic)
            && FourthRouteProgressService.Trial(run).Phase == FourthTrialPhase.Fragment
            && !M5Progress.Handle.Get(run).FourthRouteFragmentPurchased,
            "subsequent inventory does not repeat an unpurchased fragment or unlock trial");

        await PrepareFragment();
        MerchantInventory inventory = MerchantInventory.CreateForNormalMerchant(player);
        MerchantRelicEntry fragment = inventory.RelicEntries.Single(entry => entry.Model is FourthRouteFragmentRelic);
        var fragmentModel = fragment.Model!;
        var routeRelic = player.Relics.OfType<FourthRouteRelic>().Single();
        int corruption = CorruptionQuery.Get(run);
        await PlayerCmd.LoseGold(player.Gold, player, GoldLossType.Spent);
        await PlayerCmd.GainGold(99, player);
        int purchaseNotifications = 0, inventoryUpdates = 0;
        fragment.PurchaseCompleted += (_, _) => purchaseNotifications++;
        inventory.RelicEntries.First(entry => entry != fragment).EntryUpdated += () => inventoryUpdates++;
        check(!await fragment.OnTryPurchaseWrapper(inventory), "native purchase rejects 99 gold");
        check(player.Gold == 99 && fragment.IsStocked && purchaseNotifications == 0
            && !player.Relics.Contains(fragmentModel) && !M5Progress.Handle.Get(run).FourthRouteFragmentPurchased
            && FourthRouteProgressService.Trial(run).Phase == FourthTrialPhase.Fragment,
            "failed purchase preserves stock, gold, offer and trial state");
        await PlayerCmd.GainGold(1, player);
        check(await fragment.OnTryPurchaseWrapper(inventory), "native purchase succeeds with exactly 100 gold");
        check(player.Gold == 0 && !fragment.IsStocked && purchaseNotifications == 1 && inventoryUpdates > 0,
            "purchase pays native price, clears item and notifies original inventory handler");
        check(player.Relics.Contains(fragmentModel) && player.Relics.OfType<FourthRouteFragmentRelic>().Count() == 1,
            "purchased fragment remains visible in relic inventory");
        check(ReferenceEquals(player.Relics.OfType<FourthRouteRelic>().Single(), routeRelic) && routeRelic.Stage == 1
            && CorruptionQuery.Get(run) == corruption, "purchase neither upgrades route relic nor changes corruption");
        var trial = FourthRouteProgressService.Trial(run);
        check(trial.Phase == FourthTrialPhase.Second && trial.Progress == 0
            && M5Progress.Handle.Get(run).FourthRouteFragmentPurchased && !M5Progress.Handle.Get(run).FourthRouteFragmentPending,
            "purchase unlocks only second trial with fresh progress");
        check(!await fragment.OnTryPurchaseWrapper(inventory) && purchaseNotifications == 1
            && player.Relics.OfType<FourthRouteFragmentRelic>().Count() == 1,
            "sold entry cannot purchase or grant the fragment twice");
        check(!MerchantInventory.CreateForNormalMerchant(player).RelicEntries.Any(entry => entry.Model is FourthRouteFragmentRelic),
            "later inventory never repeats purchased fragment");
        await FourthRouteProgressService.AddProgress(player, FourthRouteQuest.Pride, 2);
        check(await FourthRouteRewardFlow.Claim(player, new(FourthRouteQuest.Pride, FourthTrialPhase.SecondReward)),
            "second trial reward can claim after native fragment purchase");
        check(!player.Relics.Contains(fragmentModel) && player.Relics.OfType<FourthRouteRelic>().Single().Stage == 2
            && CorruptionQuery.Get(run) == corruption + 1,
            "second reward absorbs actual purchased fragment and upgrades relic once");
    }
}
#endif
