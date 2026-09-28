#if DEBUG
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>Explicit destructive pickup tests; never invoked by normal play.</summary>
public sealed class DesignGenerosityTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_generosity";
    public override string Args => "confirm";
    public override string Description => "Destructive Generosity pickup tests; disposable run only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (_running || issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState.Players.Count != 1 || CombatManager.Instance.IsInProgress
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_generosity confirm outside combat in a disposable single-player Maiden run.");
        return new CmdResult(Run(issuingPlayer), true, "Destructive tests started; see [DS27GenerosityTest].");
    }

    private static async Task Run(Player player)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 generosity: " + name);
            checks++;
        }
        async Task Reset(int count)
        {
            foreach (RelicModel relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
            await CardPileCmd.RemoveFromDeck(player.Deck.Cards.ToArray(), showPreview: false);
            for (int i = 0; i < count; i++)
                await CardPileCmd.Add(player.RunState.CreateCard<MaidenStrike>(player), PileType.Deck, skipVisuals: true);
        }
        GenerosityRouteRelic Create(int stage)
        {
            var relic = (GenerosityRouteRelic)ModelDb.Relic<GenerosityRouteRelic>().ToMutable();
            relic.Stage = stage;
            return relic;
        }
        try
        {
            TestMode.IsOn = true;
            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            foreach (int count in new[] { 0, 1, 2, 4 })
            {
                await Reset(count);
                CardModel[] candidates = player.Deck.Cards.ToArray();
                var eternal = player.RunState.CreateCard<AscendersBane>(player);
                await CardPileCmd.Add(eternal, PileType.Deck, skipVisuals: true);
                int wanted = stage is 1 or 2 ? Math.Min(stage, count) : 0;
                var relic = Create(stage);
                Check(relic.HasUponPickupEffect == (stage is 1 or 2), "pickup flag matches stage");
                var selector = new TestCardSelector();
                selector.PrepareToSelect(candidates.Take(wanted));
                using (CardSelectCmd.UseSelector(selector)) await RelicCmd.Obtain(relic, player);
                Check(player.Deck.Cards.Count == count + 1 - wanted, "native pickup removes correct stage count or all available");
                Check(player.Deck.Cards.Contains(eternal) && !eternal.IsRemovable, "eternal card never removed");
                Check(candidates.Take(wanted).All(card => !player.Deck.Cards.Contains(card)), "selected cards actually removed");
                Check(candidates.Skip(wanted).All(player.Deck.Cards.Contains), "unselected cards retained");
                await relic.AfterObtained();
                Check(player.Deck.Cards.Count == count + 1 - wanted, "duplicate pickup cannot remove more");
                var restored = Create(0);
                SavedProperties.From(relic)!.Fill(restored);
                restored.Owner = player;
                Check(restored.Stage == stage && restored.PickupEffectGranted == (stage is 1 or 2), "native save restores stage and receipt");
                await restored.AfterObtained();
                Check(player.Deck.Cards.Count == count + 1 - wanted, "saved receipt prevents another picker");
            }

            await Reset(5);
            foreach (int stage in new[] { 1, 2, 4 })
            {
                foreach (var old in player.Relics.OfType<GenerosityRouteRelic>().ToArray()) await RelicCmd.Remove(old);
                var selector = new TestCardSelector();
                selector.PrepareToSelect(player.Deck.Cards.Take(stage < 3 ? stage : 0).ToArray());
                using (CardSelectCmd.UseSelector(selector)) await RelicCmd.Obtain(Create(stage), player);
                Check(player.Deck.Cards.Count == (stage == 1 ? 4 : 2), "replacement gets new receipt without restoring earlier removals");
            }

            await Reset(4);
            var pendingRelic = Create(2);
            pendingRelic.Owner = player;
            CardModel[] selected = player.Deck.Cards.Take(2).ToArray();
            var delayed = new TestCardSelector();
            var selection = delayed.SetupForAsyncCardSelection();
            using (CardSelectCmd.UseSelector(delayed))
            {
                Task pending = pendingRelic.AfterObtained();
                Check(!pending.IsCompleted && pendingRelic.PickupEffectGranted, "receipt reserved before asynchronous choice");
                await pendingRelic.AfterObtained();
                Check(player.Deck.Cards.Count == 4, "reentrant pickup does not open second picker");
                await CardPileCmd.RemoveFromDeck(selected[0], showPreview: false);
                selection.SetResult(selected);
                await pending;
            }
            Check(player.Deck.Cards.Count == 2, "stale removed selection ignored while valid selection resolves");

            await Reset(4);
            var capped = Create(2);
            capped.Owner = player;
            CardModel[] excess = player.Deck.Cards.ToArray();
            var invalidSelector = new TestCardSelector();
            invalidSelector.PrepareToSelect([excess[0], excess[0], excess[1], excess[2]]);
            using (CardSelectCmd.UseSelector(invalidSelector)) await capped.AfterObtained();
            Check(player.Deck.Cards.Count == 2 && player.Deck.Cards.Contains(excess[2]), "duplicate and oversized selection cannot exceed removal count");
            await MaidenSuccubus.Debugging.GenerosityOfferingContract.Run(player, Check);
            MaidenSuccubusMod.Logger.Info($"[DS27GenerosityTest] PASS {checks} assertions; disposable run modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27GenerosityTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
