#if DEBUG
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Saves.Runs;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncWindGodCloakContract
{
    private static BlockingPlayerChoiceContext Choice() => new();
    private static WindGodCloakPower Power(CardEffectTestContext ctx) => ctx.Self.Powers.OfType<WindGodCloakPower>().Single();

    private static async Task PayAndPlay(CardEffectTestContext ctx, CardModel card)
    {
        int before = ctx.Player.PlayerCombatState!.Energy;
        (int energy, int stars) = await card.SpendResources();
        var resources = new ResourceInfo { EnergySpent = energy, EnergyValue = energy, StarsSpent = stars, StarValue = stars };
        await card.OnPlayWrapper(Choice(), ctx.PrimaryEnemy, isAutoPlay: false, resources, skipCardPileVisuals: true);
        ctx.AssertEqual("real resources paid", energy, before - ctx.Player.PlayerCombatState!.Energy);
    }

    internal static async Task Run(CardEffectTestContext ctx, WindGodCloak card, bool upgraded)
    {
        ctx.AssertEqual("Cloak cost", upgraded ? 0 : 1, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("Cloak rarity", CardRarity.Uncommon, card.Rarity, effect: false);
        ctx.AssertEqual("Cloak exact runtime text", "将你在每回合打出的第一张耗能为0的牌的复制加入你的手牌。",
            Regex.Replace(card.GetDescriptionForPile(PileType.None), @"\[[^\]]*\]", ""), effect: false);
        await ctx.Play(card);
        ctx.AssertTrue("activation does not consume first play", !Power(ctx).CopiedThisTurn);
        ctx.AssertEqual("no retrospective self-copy", 0, ctx.CountCards<WindGodCloak>(PileType.Hand));

        var paid = await ctx.Add<StrikeIronclad>(PileType.Hand);
        await PayAndPlay(ctx, paid);
        ctx.AssertTrue("paid one-energy play not eligible", !Power(ctx).CopiedThisTurn);
        ctx.AssertEqual("paid card not copied", 0, ctx.CountCards<StrikeIronclad>(PileType.Hand));
        var discounted = await ctx.Add<StrikeIronclad>(PileType.Hand);
        discounted.EnergyCost.SetThisCombat(0);
        CombatEnchantmentCmd.ApplyVanilla<Sharp>(discounted, 3);
        await PayAndPlay(ctx, discounted);
        var copy = PileType.Hand.GetPile(ctx.Player).Cards.OfType<StrikeIronclad>().Single();
        ctx.AssertTrue("actually discounted card copied", Power(ctx).CopiedThisTurn);
        ctx.AssertTrue("copy carries independent enchantment", copy.Enchantment is Sharp
            && !ReferenceEquals(copy.Enchantment, discounted.Enchantment));
        ctx.AssertTrue("copy has no permanent deck link", copy.DeckVersion == null);
        await PayAndPlay(ctx, copy);
        ctx.AssertEqual("copy cannot recursively trigger this turn", 0, ctx.CountCards<StrikeIronclad>(PileType.Hand));
        await Power(ctx).BeforeSideTurnStart(Choice(), CombatSide.Enemy, ctx.Enemies, ctx.Combat);
        ctx.AssertTrue("enemy turn does not reset", Power(ctx).CopiedThisTurn);
        await Power(ctx).BeforeSideTurnStart(Choice(), CombatSide.Player, [], ctx.Combat);
        ctx.AssertTrue("another player's extra turn does not reset owner", Power(ctx).CopiedThisTurn);
        var restored = (WindGodCloakPower)ModelDb.Power<WindGodCloakPower>().ToMutable();
        SavedProperties.From(Power(ctx))!.Fill(restored);
        ctx.AssertTrue("used flag survives native saved properties", restored.CopiedThisTurn);
        await Power(ctx).BeforeSideTurnStart(Choice(), CombatSide.Player, [ctx.Self], ctx.Combat);
        ctx.AssertTrue("own next turn resets", !Power(ctx).CopiedThisTurn);

        // Native AutoPlay uses EnergySpent=0 but EnergyValue=the card's cost.
        var auto = await ctx.Add<StrikeIronclad>(PileType.Draw);
        ctx.AssertEqual("auto-play nominal cost is still one", 1, auto.EnergyCost.GetWithModifiers(CostModifiers.All));
        await ctx.Play(auto, ctx.PrimaryEnemy);
        ctx.AssertEqual("unpaid one-cost auto-play qualifies", 1, ctx.CountCards<StrikeIronclad>(PileType.Hand));

        foreach (int energy in new[] { 0, 3 })
        {
            await ctx.Reset();
            await ctx.ApplyPower<WindGodCloakPower>(ctx.Self, 1);
            await PlayerCmd.SetEnergy(energy, ctx.Player);
            var x = await ctx.Add<Whirlwind>(PileType.Hand);
            await PayAndPlay(ctx, x);
            ctx.AssertEqual("manual X uses actual energy spent", energy == 0, Power(ctx).CopiedThisTurn);
            ctx.AssertEqual("manual X copy count", energy == 0 ? 1 : 0, ctx.CountCards<Whirlwind>(PileType.Hand));
        }
        await ctx.Reset();
        await ctx.ApplyPower<WindGodCloakPower>(ctx.Self, 1);
        await PlayerCmd.SetEnergy(3, ctx.Player);
        await ctx.Play(await ctx.Add<Whirlwind>(PileType.Hand));
        ctx.AssertEqual("auto X preserves unspent energy", 3, ctx.Player.PlayerCombatState!.Energy);
        ctx.AssertEqual("auto X positive effect value still zero payment", 1, ctx.CountCards<Whirlwind>(PileType.Hand));

        await ctx.Reset();
        var replayed = await ctx.Add<WindGodCloak>(PileType.Hand, upgraded);
        CombatEnchantmentCmd.ApplyVanilla<Glam>(replayed, 1);
        await ctx.Play(replayed);
        ctx.AssertPower<WindGodCloakPower>("two real applications via Glam", ctx.Self, 2);
        ctx.AssertTrue("entire activation replay series ignored", !Power(ctx).CopiedThisTurn);
        ctx.AssertEqual("replayed activation never self-copies", 0, ctx.CountCards<WindGodCloak>(PileType.Hand));
        await ctx.Play(await ctx.Add<StrikeIronclad>(PileType.Hand), ctx.PrimaryEnemy);
        ctx.AssertEqual("two stacks create two copies", 2, ctx.CountCards<StrikeIronclad>(PileType.Hand));

        await ctx.Reset();
        await ctx.ApplyPower<WindGodCloakPower>(ctx.Self, 1);
        await ctx.Play(await ctx.Add<WindGodCloak>(PileType.Hand, upgraded));
        ctx.AssertPower<WindGodCloakPower>("existing power stacked", ctx.Self, 2);
        ctx.AssertEqual("new layer is not retroactive on its own activation", 1, ctx.CountCards<WindGodCloak>(PileType.Hand));

        await ctx.Reset();
        await ctx.ApplyPower<WindGodCloakPower>(ctx.Self, 1);
        await ctx.Add<StrikeIronclad>(PileType.Draw);
        await ctx.Add<StrikeIronclad>(PileType.Draw);
        await ctx.Play(await ctx.Add<BlackVortex>(PileType.Hand));
        ctx.AssertEqual("outer play keeps first reservation", 1, ctx.CountCards<BlackVortex>(PileType.Hand));
        ctx.AssertEqual("nested free child cannot steal first", 0, ctx.CountCards<StrikeIronclad>(PileType.Hand));

        await ctx.Reset();
        await ctx.ApplyPower<WindGodCloakPower>(ctx.Self, 1);
        await ctx.AddFillerCards(PileType.Hand, 10, type: CardType.Skill);
        await ctx.Play(await ctx.Add<StrikeIronclad>(PileType.Draw), ctx.PrimaryEnemy);
        ctx.AssertEqual("full hand limit respected", 10, PileType.Hand.GetPile(ctx.Player).Cards.Count);
        ctx.AssertEqual("native overflow retains copy in discard", 2, ctx.CountCards<StrikeIronclad>(PileType.Discard));
        ctx.AssertTrue("overflow still consumes first trigger", Power(ctx).CopiedThisTurn);

        // Synthetic hook-order regression for reentrant play of the SAME model;
        // real nested commands above exercise the normal different-model case.
        await ctx.Reset();
        await ctx.ApplyPower<WindGodCloakPower>(ctx.Self, 1);
        var same = await ctx.Add<StrikeIronclad>(PileType.Discard);
        var play = new CardPlay
        {
            Card = same, Player = ctx.Player, Target = ctx.PrimaryEnemy, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 1, StarsSpent = 0, StarValue = 0 },
            IsAutoPlay = true, PlayIndex = 0, PlayCount = 1,
        };
        await Power(ctx).BeforeCardPlayed(play);
        var clonedPower = (WindGodCloakPower)Power(ctx).ClonePreservingMutability();
        await clonedPower.AfterCardPlayed(Choice(), play);
        ctx.AssertEqual("power clone cannot inherit in-flight copy", 0, ctx.CountCards<StrikeIronclad>(PileType.Hand));
        await Power(ctx).BeforeCardPlayed(play);
        await Power(ctx).AfterCardPlayed(Choice(), play);
        ctx.AssertEqual("inner same-instance completion waits", 0, ctx.CountCards<StrikeIronclad>(PileType.Hand));
        await Power(ctx).AfterCardPlayed(Choice(), play);
        ctx.AssertEqual("outer same-instance completion copies once", 1, ctx.CountCards<StrikeIronclad>(PileType.Hand));
        await Power(ctx).AfterCardPlayed(Choice(), play);
        ctx.AssertEqual("duplicate after callback cannot recopy", 1, ctx.CountCards<StrikeIronclad>(PileType.Hand));
    }
}
#endif
