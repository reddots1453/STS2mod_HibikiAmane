#if DEBUG
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Bootstrap;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Patches;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>Actual event/selector commands plus isolated completion-adapter probes.</summary>
public sealed class DesignVanillaEventTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_vanilla_events";
    public override string Args => "confirm";
    public override string Description => "Destructive DS27 vanilla event tests; disposable run only";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || CombatManager.Instance.IsInProgress || issuingPlayer.RunState.Players.Count != 1
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_vanilla_events confirm outside combat in a disposable single-player Maiden run.");
        if (_running) return new CmdResult(false, "Vanilla event test already running.");
        return new CmdResult(Run(issuingPlayer), true, "Started destructive tests; see [DS27VanillaEventTest] log.");
    }

    private static async Task Run(Player player)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        var run = (RunState)player.RunState;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 vanilla event: " + name);
            checks++;
        }
        async Task<T> Start<T>(Player? owner = null) where T : EventModel
        {
            var model = (T)ModelDb.Event<T>().ToMutable();
            await model.BeginEvent(owner ?? player, null, isPreFinished: false);
            return model;
        }
        async Task<CardModel[]> Deck(int attacks, int skills = 0)
        {
            await CardPileCmd.RemoveFromDeck(player.Deck.Cards.ToArray());
            for (int i = 0; i < attacks + skills; i++)
            {
                CardModel card = i < attacks ? run.CreateCard<MaidenStrike>(player) : run.CreateCard<MaidenDefend>(player);
                await CardPileCmd.Add(card, PileType.Deck, skipVisuals: true);
            }
            return player.Deck.Cards.ToArray();
        }
        EventOption Option(EventModel model, string suffix) =>
            model.CurrentOptions.Single(option => option.TextKey.EndsWith(".MS_" + suffix, StringComparison.Ordinal));
        void Gate(EventModel model, string suffix, bool unlocked)
        {
            var option = model.CurrentOptions.Single(o => o.TextKey.Contains(".MS_" + suffix));
            Check(option.IsLocked != unlocked, $"{model.Id}/{suffix} threshold {CorruptionQuery.Get(run)}");
        }
        async Task Choose(EventModel model, string suffix, params CardModel[] selected)
        {
            int corruption = CorruptionQuery.Get(run);
            var selector = new TestCardSelector();
            selector.PrepareToSelect(selected);
            using (CardSelectCmd.UseSelector(selector)) await Option(model, suffix).Chosen();
            Check(model.IsFinished, suffix + " finishes");
            Check(CorruptionQuery.Get(run) == corruption, suffix + " has no extra corruption");
        }
        try
        {
            TestMode.IsOn = true;
            foreach (RelicModel relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
            await Deck(3, 1);
            for (int value = -5; value <= 5; value++)
            {
                CorruptionCmd.Set(run, value);
                var choir = await Start<LuminousChoir>();
                Gate(choir, "CALM", value <= -3); Gate(choir, "SEIZE", value >= 3);
                var reflections = await Start<Reflections>();
                Gate(reflections, "ACCEPT", value <= -4); Gate(reflections, "SEIZE", value >= 4);
                var treasury = await Start<SunkenTreasury>();
                Gate(treasury, "SEE_THROUGH", value <= -4); Gate(treasury, "TAKE_ALL", value >= 2);
                var symbiote = await Start<Symbiote>();
                Gate(symbiote, "PURIFY", value <= -3); Gate(symbiote, "SYMBIOSE", value >= 3);
                var hollow = await Start<WhisperingHollow>();
                Gate(hollow, "PURIFY", value <= -2); Gate(hollow, "ABSORB", value >= 4);
            }

            for (int count = 0; count <= 2; count++)
            {
                CardModel[] deck = await Deck(count, 1); // Skill is deliberately ineligible.
                CorruptionCmd.Set(run, 3);
                var symbiote = await Start<Symbiote>();
                var option = symbiote.CurrentOptions.Single(o => o.TextKey.Contains(".MS_SYMBIOSE"));
                Check(option.IsLocked == (count == 0), "0/1/2 eligible cards lock boundary");
                if (count == 0)
                    Check(option.TextKey.EndsWith("_NO_CARDS"), "specific empty eligibility explanation");
                else
                {
                    // No fake selector: native selector automatically accepts <=2 legal cards.
                    await Choose(symbiote, "SYMBIOSE");
                    Check(deck.Take(count).All(c => c.Enchantment is Corrupted), "existing one/two attacks enchanted");
                    Check(deck[^1].Enchantment == null, "ineligible skill untouched");
                    var depleted = await Start<Symbiote>();
                    Check(depleted.CurrentOptions.Single(o => o.TextKey.Contains(".MS_SYMBIOSE")).IsLocked,
                        "fully enchanted deck cannot be selected again");
                }
            }

            CardModel[] remove = await Deck(3);
            CorruptionCmd.Set(run, -3);
            await Choose(await Start<Symbiote>(), "PURIFY", remove[1]);
            Check(player.Deck.Cards.Count == 2 && !player.Deck.Cards.Contains(remove[1]), "purify removes selected one");
            remove = await Deck(3);
            CorruptionCmd.Set(run, 3);
            await Choose(await Start<LuminousChoir>(), "SEIZE", remove[0], remove[2]);
            Check(player.Deck.Cards.Count == 1 && player.Deck.Cards[0] == remove[1], "choir removes two without adding curse");

            await Deck(1);
            await Data.Desire.Modify(player, 5 - Data.Desire.Get(player));
            CorruptionCmd.Set(run, -3);
            await Choose(await Start<LuminousChoir>(), "CALM");
            Check(Data.Desire.Get(player) == 0 && player.Deck.Cards.OfType<CalmMind>().Count() == 1, "calm clears resource and adds card");

            foreach (int count in new[] { 0, 2, 6 })
            {
                await Deck(count);
                CorruptionCmd.Set(run, -4);
                var reflections = await Start<Reflections>();
                int niche = run.Rng.Niche.ToSerializable().counter;
                int eventCounter = reflections.Rng.ToSerializable().counter;
                await Choose(reflections, "ACCEPT");
                Check(player.Deck.Cards.Count(c => c.IsUpgraded) == Math.Min(4, count), "upgrade zero/up to four");
                Check(run.Rng.Niche.ToSerializable().counter == niche, "does not consume unrelated run RNG");
                if (count > 1) Check(reflections.Rng.ToSerializable().counter > eventCounter, "uses event RNG");
            }
            CardModel original = (await Deck(1))[0];
            CardCmd.Upgrade(original);
            CardCmd.Enchant<Corrupted>(original, 1);
            CorruptionCmd.Set(run, 4);
            await Choose(await Start<Reflections>(), "SEIZE", original);
            Check(player.Deck.Cards.Count == 3 && player.Deck.Cards.All(c => c.IsUpgraded && c.Enchantment is Corrupted),
                "copies preserve upgrade and enchantment");
            Check(player.Deck.Cards.Distinct(ReferenceEqualityComparer.Instance).Count() == 3, "two independent copies");

            CorruptionCmd.Set(run, -4);
            var treasure = await Start<SunkenTreasury>();
            decimal gold = player.Gold;
            int relics = player.Relics.Count;
            // A random Old Coin could itself grant gold; isolate the event's
            // payment with the engine's relic override, restoring any prior probe.
            FieldInfo relicOverride = AccessTools.Field(typeof(TestRngInjector), "_relicOverride");
            object? previousRelicOverride = relicOverride.GetValue(null);
            try
            {
                TestRngInjector.SetRelicOverride<Circlet>();
                await Choose(treasure, "SEE_THROUGH");
            }
            finally { relicOverride.SetValue(null, previousRelicOverride); }
            Check(player.Gold == gold + treasure.DynamicVars["LargeChestGold"].BaseValue, "second chest gold");
            Check(player.Relics.Count == relics + 1, "one relic granted");
            foreach (RelicModel relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
            CorruptionCmd.Set(run, 2);
            treasure = await Start<SunkenTreasury>();
            gold = player.Gold;
            int greed = player.Deck.Cards.OfType<Greed>().Count();
            await Choose(treasure, "TAKE_ALL");
            Check(player.Gold == gold + treasure.DynamicVars["SmallChestGold"].BaseValue + treasure.DynamicVars["LargeChestGold"].BaseValue,
                "both chest gold");
            Check(player.Deck.Cards.OfType<Greed>().Count() == greed + 1, "one greed added");
            CorruptionCmd.Set(run, -2);
            gold = player.Gold;
            await Choose(await Start<WhisperingHollow>(), "PURIFY");
            Check(player.Gold == gold + 300, "tree grants exactly 300 gold");
            CorruptionCmd.Set(run, 4);
            int decay = player.Deck.Cards.OfType<Decay>().Count();
            await Choose(await Start<WhisperingHollow>(), "ABSORB");
            Check(player.Relics.OfType<WitheredTreeSoul>().Count() == 1 && player.Deck.Cards.OfType<Decay>().Count() == decay + 1,
                "tree grants branch and decay");

            // Adapter probes use real EventOption.Chosen with controlled callbacks, not vanilla narrative actions.
            MethodInfo finish = AccessTools.Method(typeof(EventModel), "SetEventFinished", [typeof(LocString)]);
            MethodInfo page = AccessTools.Method(typeof(EventModel), "SetEventState", [typeof(LocString), typeof(IEnumerable<EventOption>)]);
            const string positive = "WHISPERING_HOLLOW.pages.INITIAL.options.HUG";
            Task Finish(EventModel model) { finish.Invoke(model, [new LocString("events", "WHISPERING_HOLLOW.pages.MS_PURIFY.description")]); return Task.CompletedTask; }
            EventOption Probe(EventModel model, Func<Task> action, string key = positive) =>
                new(model, action, new LocString("events", key + ".title"), new LocString("events", key + ".description"), key, Array.Empty<IHoverTip>());
            CorruptionCmd.Set(run, 0);
            var pending = await Start<WhisperingHollow>();
            await Probe(pending, () => Task.CompletedTask).Chosen();
            Check(CorruptionQuery.Get(run) == 0, "no-op does not complete");
            await Probe(pending, () => { page.Invoke(pending, [new LocString("events", "WHISPERING_HOLLOW.pages.MS_PURIFY.description"), Array.Empty<EventOption>()]); return Task.CompletedTask; }).Chosen();
            Check(CorruptionQuery.Get(run) == 0, "non-bath intermediate page does not complete");
            foreach (bool cancel in new[] { false, true })
            {
                try
                {
                    await Probe(pending, async () => { await Task.Yield(); if (cancel) throw new OperationCanceledException(); throw new InvalidOperationException("fixture failure"); }).Chosen();
                    throw new InvalidOperationException("expected exception missing");
                }
                catch (OperationCanceledException) when (cancel) { }
                catch (InvalidOperationException ex) when (!cancel && ex.Message == "fixture failure") { }
                Check(CorruptionQuery.Get(run) == 0, "fault/cancel does not commit");
            }
            await Probe(pending, () => Finish(pending)).Chosen();
            Check(CorruptionQuery.Get(run) == 1, "successful callback commits once");
            await Probe(pending, () => Finish(pending)).Chosen();
            Check(CorruptionQuery.Get(run) == 1, "rebuilt finished option cannot duplicate");
            var bath = await Start<AbyssalBaths>();
            Task Advance() { page.Invoke(bath, [new LocString("events", "WHISPERING_HOLLOW.pages.MS_PURIFY.description"), Array.Empty<EventOption>()]); return Task.CompletedTask; }
            const string immerse = "ABYSSAL_BATHS.pages.INITIAL.options.IMMERSE";
            await Probe(bath, Advance, immerse).Chosen();
            Check(!bath.IsFinished && CorruptionQuery.Get(run) == 2, "first bath page advance counts");
            await Probe(bath, Advance, immerse).Chosen();
            Check(CorruptionQuery.Get(run) == 2, "rebuilt unfinished same-key option cannot duplicate");
            await Probe(bath, Advance, "ABYSSAL_BATHS.pages.INITIAL.options.LINGER").Chosen();
            Check(CorruptionQuery.Get(run) == 2, "linger is not mapped");

            Player foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
            foreign.RunState = run; // Owner is deliberately not the Maiden already in this run.
            var foreignEvent = await Start<WhisperingHollow>(foreign);
            Check(!foreignEvent.CurrentOptions.Any(o => o.TextKey.Contains(".MS_")), "other character gets no route options");
            Check(!MvpEventRules.Applies(foreignEvent), "owner isolation in mixed-character run context");
            await Probe(foreignEvent, () => Finish(foreignEvent)).Chosen();
            Check(CorruptionQuery.Get(run) == 2, "other owner's option does not affect Maiden corruption");
            foreach ((int before, string key, int expected) in new[] {
                (5, positive, 5), (-5, "SYMBIOTE.pages.INITIAL.options.KILL_WITH_FIRE", -5),
                (0, "SYMBIOTE.pages.INITIAL.options.KILL_WITH_FIRE", -1) })
            {
                CorruptionCmd.Set(run, before);
                var model = await Start<WhisperingHollow>();
                await Probe(model, () => Finish(model), key).Chosen();
                Check(CorruptionQuery.Get(run) == expected, "positive/negative and global bounds");
            }
            MaidenSuccubusMod.Logger.Info($"[DS27VanillaEventTest] PASS {checks} assertions; disposable run modified.");
        }
        catch (Exception ex) { MaidenSuccubusMod.Logger.Error("[DS27VanillaEventTest] FAIL " + ex); throw; }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
