#if DEBUG
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Events;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>Opt-in destructive event regression for disposable single-player runs.</summary>
public sealed class DesignEventTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_events";
    public override string Args => "confirm";
    public override string Description => "Destructive DS27 UndeadGathering event test; disposable run only";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || CombatManager.Instance.IsInProgress
            || issuingPlayer.RunState.Players.Count != 1 || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_events confirm outside combat in a disposable single-player Maiden run.");
        if (_running) return new CmdResult(false, "Event test already running.");
        return new CmdResult(Run(issuingPlayer), true, "Started destructive event tests; see [DS27EventTest] log.");
    }

    private static async Task Run(Player player)
    {
        _running = true;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 event assertion failed: " + name);
            checks++;
        }
        async Task<UndeadGathering> Start()
        {
            UndeadGathering instance = (UndeadGathering)ModelDb.Event<UndeadGathering>().ToMutable();
            await instance.BeginEvent(player, null, isPreFinished: false);
            return instance;
        }
        try
        {
            var run = (RunState)player.RunState;
            foreach (RelicModel relic in player.Relics.ToArray())
                await RelicCmd.Remove(relic);
            CorruptionCmd.Set(run, 0);
            foreach ((int maximum, int expected) in new[] { (60, 6), (97, 9), (5, 0) })
            {
                await CreatureCmd.SetMaxAndCurrentHp(player.Creature, maximum);
                CardModel first = run.CreateCard<MaidenStrike>(player);
                CardModel second = run.CreateCard<MaidenDefend>(player);
                await CardPileCmd.Add(first, PileType.Deck, skipVisuals: true);
                await CardPileCmd.Add(second, PileType.Deck, skipVisuals: true);
                HashSet<CardModel> before = player.Deck.Cards.ToHashSet();
                UndeadGathering model = await Start();
                var prayer = model.CurrentOptions[1];
                string text = prayer.Description.GetFormattedText();
                Check(text.StartsWith($"失去{expected}点生命值。"), "displayed integer hp cost");
                Check(!text.Contains('%') && !text.Contains("{HpLoss}"), "no formula or unresolved variable");
                int corruptionBefore = CorruptionQuery.Get(run);
                TestCardSelector selector = new();
                selector.PrepareToSelect(new[] { first, second });
                using (CardSelectCmd.UseSelector(selector)) await prayer.Chosen();
                Check(player.Creature.CurrentHp == maximum - expected, "current hp reduced");
                Check(player.Creature.MaxHp == maximum, "maximum hp unchanged");
                Check(CorruptionQuery.Get(run) == corruptionBefore - 1, "prayer reduces corruption");
                Check(!player.Deck.Cards.Contains(first) && !player.Deck.Cards.Contains(second), "two originals transformed");
                CardModel[] added = player.Deck.Cards.Where(card => !before.Contains(card)).ToArray();
                Check(added.Length == 2 && added.All(card => card is MSHolyCard), "two holy replacements");
                int hpAfter = player.Creature.CurrentHp;
                await prayer.Chosen();
                Check(player.Creature.CurrentHp == hpAfter, "double click cannot pay twice");
            }
            await CreatureCmd.SetMaxAndCurrentHp(player.Creature, 60);
            UndeadGathering listen = await Start();
            int soulsBefore = player.Deck.Cards.OfType<Soul>().Count();
            await listen.CurrentOptions[0].Chosen();
            Check(player.Deck.Cards.OfType<Soul>().Count() == soulsBefore + 2, "listen adds two vanilla souls");

            CardModel enchantTarget = run.CreateCard<MaidenStrike>(player);
            await CardPileCmd.Add(enchantTarget, PileType.Deck, skipVisuals: true);
            CorruptionCmd.Set(run, 2);
            UndeadGathering locked = await Start();
            Check(locked.CurrentOptions[2].IsLocked, "learning locked at +2");
            CorruptionCmd.Set(run, 3);
            UndeadGathering learn = await Start();
            Check(!learn.CurrentOptions[2].IsLocked, "learning unlocked at +3");
            int cursesBefore = player.Deck.Cards.OfType<Normality>().Count();
            TestCardSelector enchantSelector = new();
            enchantSelector.PrepareToSelect(new[] { enchantTarget });
            using (CardSelectCmd.UseSelector(enchantSelector)) await learn.CurrentOptions[2].Chosen();
            Check(enchantTarget.Enchantment is NecromancyEnchantment, "necromancy applied");
            Check(player.Deck.Cards.OfType<Normality>().Count() == cursesBefore + 1, "normality added");
            Check(CorruptionQuery.Get(run) == 4, "learning adds corruption");
            MaidenSuccubusMod.Logger.Info($"[DS27EventTest] PASS {checks} assertions; disposable run was modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27EventTest] FAIL " + ex);
            throw;
        }
        finally { _running = false; }
    }
}
#endif
