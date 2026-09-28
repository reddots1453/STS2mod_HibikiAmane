#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncSignedLayerContract
{
    internal static async Task Healing(CardEffectTestContext ctx, bool upgraded)
    {
        await ctx.Reset();
        await ctx.ApplyPower<StrengthPower>(ctx.Self, -2);
        await ctx.ApplyPower<DexterityPower>(ctx.Self, 3);
        await ctx.ApplyPower<AmbergrisPower>(ctx.Self, 4); // Native hidden cleanup power.
        ctx.AssertEqual("native negative strength classification", PowerType.Debuff,
            ctx.Self.GetPower<StrengthPower>()!.TypeForCurrentAmount);
        ctx.AssertEqual("negative strength contributes two debuff layers", 2, PowerLayerQuery.CountDebuffLayers(ctx.Self));
        ctx.AssertEqual("only visible positive dexterity contributes buffs", 3, PowerLayerQuery.CountBuffLayers(ctx.Self));
        ctx.AssertTrue("native hidden power is really hidden", !ctx.Self.GetPower<AmbergrisPower>()!.IsVisible);
        var card = await ctx.Add<HealingArt>(PileType.Hand, upgraded);
        Preview(ctx, card, $"（恢复{(upgraded ? 14 : 10)}点生命值）");
        await ctx.ApplyPower<StrengthPower>(ctx.Self, 4); // -2 -> +2 switches native type.
        ctx.AssertEqual("crossing zero leaves no debuff layers", 0, PowerLayerQuery.CountDebuffLayers(ctx.Self));
        ctx.AssertEqual("crossing zero adds two buff layers", 5, PowerLayerQuery.CountBuffLayers(ctx.Self));
        Preview(ctx, card, $"（恢复{(upgraded ? 18 : 14)}点生命值）");
        await PowerCmd.Remove(ctx.Self.GetPower<StrengthPower>()!);
        Preview(ctx, card, $"（恢复{(upgraded ? 14 : 10)}点生命值）");
        var outside = ctx.Player.RunState.CreateCard<HealingArt>(ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        ctx.AssertTrue("permanent instance still omits combat total", !outside.GetDescriptionForPile(PileType.Deck).Contains("（恢复"));
        await CreatureCmd.SetCurrentHp(ctx.Self, ctx.Self.MaxHp - 50);
        int hp = ctx.Self.CurrentHp;
        await ctx.Play(card);
        ctx.AssertEqual("healing ignores negative and hidden states", upgraded ? 14 : 10, ctx.Self.CurrentHp - hp);
    }

    internal static async Task Judgment(CardEffectTestContext ctx, bool upgraded)
    {
        await ctx.Reset();
        await ctx.ApplyPower<StrengthPower>(ctx.PrimaryEnemy, -2);
        await ctx.ApplyPower<DexterityPower>(ctx.PrimaryEnemy, -3);
        var card = await ctx.Add<JudgmentBlade>(PileType.Hand, upgraded);
        ctx.AssertEqual("negative stat layers are magnitudes, not two instances", 5,
            PowerLayerQuery.CountDebuffLayers(ctx.PrimaryEnemy));
        Preview(ctx, card, $"（造成{(upgraded ? 32 : 22)}点伤害）", true);
        await ctx.ApplyPower<StrengthPower>(ctx.PrimaryEnemy, 4);
        Preview(ctx, card, $"（造成{(upgraded ? 22 : 16)}点伤害）", true);
        ctx.AssertEqual("positive target strength no longer boosts debuff damage", 3,
            PowerLayerQuery.CountDebuffLayers(ctx.PrimaryEnemy));
        int hp = ctx.PrimaryEnemy.CurrentHp;
        await ctx.Play(card, ctx.PrimaryEnemy);
        ctx.AssertDamage("actual damage matches negative dexterity preview", ctx.PrimaryEnemy, hp, upgraded ? 22 : 16);
    }

    internal static async Task LastStand(CardEffectTestContext ctx, bool upgraded)
    {
        await ctx.Reset();
        await ctx.ApplyPower<StrengthPower>(ctx.Self, -2);
        await ctx.ApplyPower<DexterityPower>(ctx.Self, -3);
        var card = await ctx.Add<LastStand>(PileType.Hand, upgraded);
        ctx.AssertEqual("five self-debuff layers", 5, PowerLayerQuery.CountDebuffLayers(ctx.Self));
        ctx.AssertEqual("negative stats not counted as self buffs", 0, PowerLayerQuery.CountBuffLayers(ctx.Self));
        // 3+5*3-2 / 4+5*4-2: native negative Strength still reduces attack damage.
        Preview(ctx, card, $"（造成{(upgraded ? 22 : 16)}点伤害）", true);
        DesignSyncVariationTextContract.LastStandTotal(ctx, card, upgraded, upgraded ? 22 : 16);
        int hp = ctx.PrimaryEnemy.CurrentHp;
        await ctx.Play(card, ctx.PrimaryEnemy);
        ctx.AssertDamage("self-debuff bonus plus native strength penalty", ctx.PrimaryEnemy, hp, upgraded ? 22 : 16);
    }

    internal static async Task Draw(CardEffectTestContext ctx, bool upgraded)
    {
        await ctx.Reset();
        await ctx.ApplyPower<StrengthPower>(ctx.PrimaryEnemy, -3);
        await ctx.AddFillerCards(PileType.Draw, 5);
        int hp = ctx.PrimaryEnemy.CurrentHp;
        await ctx.Play(ctx.Create<HolyCurse>(upgraded), ctx.PrimaryEnemy);
        ctx.AssertDamage("holy curse damage unchanged", ctx.PrimaryEnemy, hp, 7);
        ctx.AssertEqual("three cards drawn for negative three strength", 3, ctx.CountCards<StrikeIronclad>(PileType.Hand));
        ctx.AssertEqual("remaining draw cards unchanged", 2, ctx.CountCards<StrikeIronclad>(PileType.Draw));
    }

    internal static async Task Energy(CardEffectTestContext ctx, bool upgraded)
    {
        await ctx.Reset();
        await ctx.ApplyPower<StrengthPower>(ctx.PrimaryEnemy, -1);
        ctx.AssertEqual("negative stat alone satisfies debuff condition", 1, PowerLayerQuery.CountDebuffLayers(ctx.PrimaryEnemy));
        int energy = ctx.Player.PlayerCombatState!.Energy;
        int hp = ctx.PrimaryEnemy.CurrentHp;
        await ctx.Play(ctx.Create<SoulImpact>(upgraded), ctx.PrimaryEnemy);
        ctx.AssertEqual("negative stat enables energy reward", upgraded ? 3 : 2, ctx.Player.PlayerCombatState.Energy - energy);
        ctx.AssertDamage("energy branch keeps attack damage", ctx.PrimaryEnemy, hp, upgraded ? 14 : 12);
    }

    private static void Preview(CardEffectTestContext ctx, CardModel card, string ending, bool target = false) =>
        ctx.AssertTrue("live signed-layer preview " + ending,
            DesignSyncTextBatchContract.Text(card, PileType.Hand, target ? ctx.PrimaryEnemy : null)
                .EndsWith(ending, StringComparison.Ordinal));
}
#endif
