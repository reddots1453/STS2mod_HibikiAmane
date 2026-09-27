#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncExhaustContract
{
    internal static async Task Regeneration(CardEffectTestContext ctx, SuperRegeneration card, bool upgraded)
    {
        ctx.AssertEqual("regen cost", upgraded ? 1 : 2, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertTrue("regen actually exhausts", card.Keywords.Contains(CardKeyword.Exhaust), effect: false);
        string description = System.Text.RegularExpressions.Regex.Replace(card.GetDescriptionForPile(PileType.None), @"\[[^\]]*\]", "");
        ctx.AssertEqual("regen exact sentence and keyword order",
            "从消耗堆选择打出一张牌。\n消耗。\n魔力解放：将此牌放回手牌。", description, effect: false);
        ctx.AssertTrue("another regeneration cannot be selected", !SuperRegeneration.CanSelect(ctx.Create<SuperRegeneration>()));
        ctx.AssertTrue("unplayable cannot be selected", !SuperRegeneration.CanSelect(ctx.Create<Injury>()));
        var target = await ctx.Add<MaidenStrike>(PileType.Exhaust, upgraded);
        int hp = ctx.PrimaryEnemy.CurrentHp;
        await ctx.Play(card, selectedCards: [target]);
        ctx.AssertDamage("target actually played", ctx.PrimaryEnemy, hp, upgraded ? 9 : 6);
        ctx.AssertEqual("no payment stays exhausted", PileType.Exhaust, card.Pile?.Type);
        ctx.AssertEqual("target follows own discard", PileType.Discard, target.Pile?.Type);

        await ctx.Reset();
        card = await ctx.Add<SuperRegeneration>(PileType.Hand, upgraded);
        target = await ctx.Add<MaidenStrike>(PileType.Exhaust);
        await ctx.SetUpArmour(1);
        await ctx.ApplyPower<FeelNoPainPower>(ctx.Self, 3);
        await ctx.Play(card, selectedIndices: [0]); // Only exhausted strike, then accept release.
        ctx.AssertEqual("release returns regeneration itself", PileType.Hand, card.Pile?.Type);
        ctx.AssertEqual("release does not return the target", PileType.Discard, target.Pile?.Type);
        ctx.AssertPower<MagicArmorPower>("one armour spent", ctx.Self, 0);
        ctx.AssertTrue("zero armour preserves transformation", TransformationCmd.IsTransformed(ctx.Self));
        ctx.AssertEqual("real exhaust hook still triggers", 3, ctx.Self.Block);
        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), card);
        ctx.AssertEqual("return flag consumed, later exhaust stays", PileType.Exhaust, card.Pile?.Type);
        ctx.AssertEqual("second exhaust still triggers native listener", 6, ctx.Self.Block);

        await ctx.Reset();
        card = await ctx.Add<SuperRegeneration>(PileType.Hand, upgraded);
        await ctx.SetUpArmour(2);
        await ctx.Add<MaidenStrike>(PileType.Exhaust);
        await ctx.Add<MaidenStrike>(PileType.Exhaust);
        await ctx.Play(card, selectedIndices: [1]); // Second strike, then decline release.
        ctx.AssertEqual("declined release stays exhausted", PileType.Exhaust, card.Pile?.Type);
        ctx.AssertPower<MagicArmorPower>("decline never pays", ctx.Self, 2);

        await ctx.Reset();
        card = await ctx.Add<SuperRegeneration>(PileType.Hand, upgraded);
        await ctx.SetUpArmour(3);
        await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
        target = await ctx.Add<MaidenStrike>(PileType.Exhaust);
        await ctx.Play(card, selectedCards: [target]); // A payment dialog would reject this selection.
        ctx.AssertEqual("amplification release automatic", PileType.Hand, card.Pile?.Type);
        ctx.AssertPower<MagicAmplificationPower>("amplification paid once", ctx.Self, 0);
        ctx.AssertPower<MagicArmorPower>("amplification preserves armour", ctx.Self, 3);

        await ctx.Reset();
        card = await ctx.Add<SuperRegeneration>(PileType.Hand, upgraded);
        await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
        await ctx.Play(card); // Empty exhaust still permits the independent release clause.
        ctx.AssertEqual("empty exhaust release still returns self", PileType.Hand, card.Pile?.Type);
        ctx.AssertPower<MagicAmplificationPower>("empty exhaust pays exactly once", ctx.Self, 0);
    }

    internal static async Task Vortex(CardEffectTestContext ctx, BlackVortex card, bool upgraded)
    {
        ctx.AssertEqual("vortex cost unchanged", upgraded ? 1 : 2, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        await ctx.SetUpArmour(1);
        var pair = await ctx.AddFillerCards(PileType.Draw, 2);
        int hp = ctx.Enemies.Sum(enemy => enemy.CurrentHp);
        await ctx.Play(card, selectedIndices: [0]);
        ctx.AssertEqual("two attacks deal base damage", 12, hp - ctx.Enemies.Sum(enemy => enemy.CurrentHp));
        ctx.AssertEqual("release exhausts the pair", 2, ctx.CountCards<StrikeIronclad>(PileType.Exhaust));
        ctx.AssertPower<MagicArmorPower>("release pays armour", ctx.Self, 0);
        ctx.AssertTrue("child scopes disposed", pair.All(child => !AmplificationConsumptionScope.IsExempt(child)));

        await ctx.Reset();
        card = await ctx.Add<BlackVortex>(PileType.Hand, upgraded);
        await ctx.Add<StrikeIronclad>(PileType.Draw);
        await ctx.Add<StrikeIronclad>(PileType.Discard);
        hp = ctx.Enemies.Sum(enemy => enemy.CurrentHp);
        await ctx.Play(card);
        ctx.AssertEqual("short draw pile shuffles to play twice", 12, hp - ctx.Enemies.Sum(enemy => enemy.CurrentHp));
        ctx.AssertEqual("no payment exhausts nothing", 0, ctx.CountCards<StrikeIronclad>(PileType.Exhaust));

        await ctx.Reset();
        card = await ctx.Add<BlackVortex>(PileType.Hand, upgraded);
        await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 2);
        var six = await ctx.AddFillerCards(PileType.Draw, 6);
        hp = ctx.Enemies.Sum(enemy => enemy.CurrentHp);
        await ctx.Play(card);
        ctx.AssertEqual("two releases permit three pairs", 36, hp - ctx.Enemies.Sum(enemy => enemy.CurrentHp));
        ctx.AssertEqual("two paid pairs exhaust", 4, ctx.CountCards<StrikeIronclad>(PileType.Exhaust));
        ctx.AssertEqual("last unpaid pair discards", 2, ctx.CountCards<StrikeIronclad>(PileType.Discard));
        ctx.AssertPower<MagicAmplificationPower>("only outer releases spend layers", ctx.Self, 0);
        ctx.AssertTrue("all six exemptions removed", six.All(child => !AmplificationConsumptionScope.IsExempt(child)));

        await ctx.Reset();
        card = await ctx.Add<BlackVortex>(PileType.Hand, upgraded);
        await ctx.Add<MagiciansSecret>(PileType.Draw);
        var strike = await ctx.Add<StrikeIronclad>(PileType.Draw);
        await ctx.Play(card);
        ctx.AssertEqual("new amplification funds outer release", 1, ctx.CountCards<MagiciansSecret>(PileType.Exhaust));
        ctx.AssertEqual("second child did not steal new layer", PileType.Exhaust, strike.Pile?.Type);
        ctx.AssertPower<MagicAmplificationPower>("new layer settled once", ctx.Self, 0);

        await ctx.Reset();
        var exempt = await ctx.Add<StrikeIronclad>(PileType.Hand);
        var ordinary = await ctx.Add<StrikeIronclad>(PileType.Hand);
        using (var outer = AmplificationConsumptionScope.Enter(exempt))
        {
            var inner = AmplificationConsumptionScope.Enter(exempt);
            inner.Dispose();
            inner.Dispose();
            ctx.AssertTrue("nested scope and idempotent disposal", AmplificationConsumptionScope.IsExempt(exempt));
            ctx.AssertTrue("unrelated card isolated", !AmplificationConsumptionScope.IsExempt(ordinary));
            await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 2);
            await ctx.Play(exempt, ctx.PrimaryEnemy);
            ctx.AssertPower<MagicAmplificationPower>("exempt card never consumes", ctx.Self, 2);
            await ctx.Play(ordinary, ctx.PrimaryEnemy);
            ctx.AssertPower<MagicAmplificationPower>("unrelated card consumes normally", ctx.Self, 1);
        }
        ctx.AssertTrue("outer scope disposed", !AmplificationConsumptionScope.IsExempt(exempt));
        try
        {
            using var failed = AmplificationConsumptionScope.Enter(exempt);
            throw new InvalidOperationException("expected scope failure");
        }
        catch (InvalidOperationException) { }
        ctx.AssertTrue("exception cleanup", !AmplificationConsumptionScope.IsExempt(exempt));
        await ctx.Play(exempt, ctx.PrimaryEnemy);
        ctx.AssertPower<MagicAmplificationPower>("later normal play consumes", ctx.Self, 0);
    }
}
#endif
