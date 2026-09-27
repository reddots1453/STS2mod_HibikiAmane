#if DEBUG
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignWrathTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_wrath";
    public override string Args => "confirm";
    public override string Description => "Destructive Wrath tests; disposable combat only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (_running || issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState.Players.Count != 1 || !CombatManager.Instance.IsInProgress
            || CombatManager.Instance.IsEnding || issuingPlayer.Creature.CombatState is not CombatState combat
            || combat.HittableEnemies.Count == 0 || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_wrath confirm in a disposable single-player Maiden combat.");
        return new CmdResult(Run(issuingPlayer, combat), true, "Destructive tests started; see [DS27WrathTest].");
    }

    private static async Task Run(Player player, CombatState combat)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 wrath: " + name);
            checks++;
        }
        var ctx = new CardEffectTestContext(combat, player);
        async Task<WrathRouteRelic> Install(int stage, CardModel? choice = null)
        {
            foreach (var old in player.Relics.OfType<WrathRouteRelic>().ToArray()) await RelicCmd.Remove(old);
            var relic = (WrathRouteRelic)ModelDb.Relic<WrathRouteRelic>().ToMutable();
            relic.Stage = stage;
            var selector = new TestCardSelector();
            if (choice != null) selector.PrepareToSelect([choice]);
            using (CardSelectCmd.UseSelector(selector)) await RelicCmd.Obtain(relic, player);
            return relic;
        }
        async Task PlayForDamage(CardModel card, int expected, string label)
        {
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            Check(hp - ctx.PrimaryEnemy.CurrentHp == expected, label);
        }
        try
        {
            TestMode.IsOn = true;
            await ctx.PrepareSuite();
            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            foreach (bool upgraded in new[] { false, true })
            {
                await ctx.Reset();
                var deck = player.RunState.CreateCard<MaidenStrike>(player);
                await CardPileCmd.Add(deck, PileType.Deck, skipVisuals: true);
                if (upgraded) CardCmd.Upgrade(deck);
                try
                {
                    var relic = await Install(stage, deck);
                    Check((deck.Enchantment is WrathEnchantment) == (stage is 1 or 2), "only two pickup stages enchant selected deck card");
                    if (deck.Enchantment == null) CardCmd.Enchant<WrathEnchantment>(deck, 1);
                    var originalEnchant = deck.Enchantment;
                    await relic.AfterObtained();
                    Check(ReferenceEquals(originalEnchant, deck.Enchantment), "repeated pickup does not enchant again");
                    var restored = (WrathRouteRelic)ModelDb.Relic<WrathRouteRelic>().ToMutable();
                    SavedProperties.From(relic)!.Fill(restored);
                    restored.Owner = player;
                    await restored.AfterObtained();
                    Check(ReferenceEquals(originalEnchant, deck.Enchantment), "saved receipt prevents second selection");
                    CardModel card = combat.CloneCard(deck);
                    int baseDamage = upgraded ? 9 : 6;
                    int awakening = stage >= 3 ? 6 : 0;
                    await PlayForDamage(card, baseDamage + 6 + awakening, "first play damage");
                    Check(((WrathEnchantment)card.Enchantment!).UsedThisCombat, "original spent after first play");
                    CardModel copy = PileType.Discard.GetPile(player).Cards.Single(candidate => candidate.CloneOf == card);
                    Check(copy.IsUpgraded == upgraded && copy.Enchantment is WrathEnchantment { UsedThisCombat: false },
                        "Wrath copy retains upgrade and fresh first-play effect");
                    Check(!((WrathEnchantment)deck.Enchantment!).UsedThisCombat, "permanent enchantment remains unused");
                    var ordinaryClone = combat.CloneCard(card);
                    Check(((WrathEnchantment)ordinaryClone.Enchantment!).UsedThisCombat, "general clone preserves used state");
                    var loaded = CardModel.FromSerializable(card.ToSerializable());
                    Check(((WrathEnchantment)loaded.Enchantment!).UsedThisCombat, "save/load preserves spent state");
                    int generatedBefore = player.Piles.Where(pile => pile.IsCombatPile).Sum(pile => pile.Cards.Count);
                    await PlayForDamage(card, baseDamage + awakening, "later original loses first-play bonus only");
                    Check(player.Piles.Where(pile => pile.IsCombatPile).Sum(pile => pile.Cards.Count) == generatedBefore,
                        "later original produces no extra copy");
                    await PlayForDamage(copy, baseDamage + 6 + awakening, "generated copy gets first-play damage");
                    CardModel grandchild = PileType.Discard.GetPile(player).Cards.Single(candidate => candidate.CloneOf == copy);
                    Check(grandchild.Enchantment is WrathEnchantment { UsedThisCombat: false }, "copy can create another fresh copy");
                    var nextCombatClone = combat.CloneCard(deck);
                    Check(nextCombatClone.Enchantment is WrathEnchantment { UsedThisCombat: false }, "fresh deck clone for later battle is unused");
                }
                finally { await CardPileCmd.RemoveFromDeck(deck, showPreview: false); }
            }

            await ctx.Reset();
            var awakened = await Install(4);
            var multi = await ctx.Add<TwinStrike>(PileType.Hand);
            CardCmd.Enchant<WrathEnchantment>(multi, 1);
            int multiBase = multi.DynamicVars.Damage.IntValue;
            await PlayForDamage(multi, (multiBase + 12) * 2, "multi-hit first play adds both bonuses per damage hit");
            Check(PileType.Discard.GetPile(player).Cards.Count(card => card.CloneOf == multi) == 1, "multi-hit produces only one copy");
            await PlayForDamage(multi, (multiBase + 6) * 2, "multi-hit later play retains awakened bonus");

            await ctx.Reset();
            var wings = await ctx.Add<LightWings>(PileType.Hand);
            CardCmd.Enchant<WrathEnchantment>(wings, 1);
            CardCmd.Enchant<WrathEnchantment>(wings, 1);
            CardCmd.Enchant<Sharp>(wings, 2);
            var charge = CardCmd.Enchant<ChargeEnchantment>(wings, 1)!;
            charge.Status = EnchantmentStatus.Disabled;
            await PlayForDamage(wings, 9 + 12 + 2 + 6, "two Wrath layers but one awakened bonus");
            var wingCopies = PileType.Discard.GetPile(player).Cards.Where(card => card.CloneOf == wings).ToArray();
            Check(wingCopies.Length == 2, "two layers produce two copies");
            foreach (var copy in wingCopies)
            {
                var layers = ((LayeredEnchantment)copy.Enchantment!).Layers;
                Check(layers.OfType<WrathEnchantment>().Count() == 2 && layers.OfType<WrathEnchantment>().All(wrath => !wrath.UsedThisCombat),
                    "all copied Wrath layers fresh");
                Check(layers.OfType<ChargeEnchantment>().Single().Status == EnchantmentStatus.Disabled,
                    "unrelated spent enchantment not reset");
                Check(layers.OfType<Sharp>().Single().Amount == 2, "other enchantment amount retained");
            }
            await PlayForDamage(wings, 9 + 2 + 6, "spent layered original keeps single awakened bonus");

            await ctx.Reset();
            var replay = await ctx.Add<LightWings>(PileType.Hand);
            CardCmd.Enchant<WrathEnchantment>(replay, 1);
            CardCmd.Enchant<Glam>(replay, 1);
            await PlayForDamage(replay, (9 + 12) + (9 + 6), "replay consumes first-play bonus only on first execution");
            Check(PileType.Discard.GetPile(player).Cards.Count(card => card.CloneOf == replay) == 1,
                "replay series makes one Wrath copy");
            await PlayForDamage(replay, 9 + 6, "later play has neither spent replay nor first-play bonus");

            await ctx.Reset();
            var sharpOnly = await ctx.Add<MaidenStrike>(PileType.Hand);
            CardCmd.Enchant<Sharp>(sharpOnly, 2);
            await PlayForDamage(sharpOnly, 8, "other enchantment does not qualify");
            var own = ctx.Create<MaidenStrike>();
            CardCmd.Enchant<WrathEnchantment>(own, 1);
            Check(awakened.ModifyDamageAdditive(ctx.PrimaryEnemy, 6, ValueProp.Unpowered, ctx.Self, own, null) == 0,
                "unpowered damage excluded");
            Player foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
            foreign.RunState = player.RunState;
            var foreignCard = player.RunState.CreateCard<StrikeIronclad>(foreign);
            foreignCard.EnchantInternal(ModelDb.Enchantment<WrathEnchantment>().ToMutable(), 1);
            Check(awakened.ModifyDamageAdditive(ctx.PrimaryEnemy, 6, ValueProp.Move, foreign.Creature, foreignCard, null) == 0,
                "other player's Wrath attack excluded");
            MaidenSuccubusMod.Logger.Info($"[DS27WrathTest] PASS {checks} assertions; disposable combat modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27WrathTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
