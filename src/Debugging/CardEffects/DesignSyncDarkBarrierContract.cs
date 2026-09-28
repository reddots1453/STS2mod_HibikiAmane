#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncDarkBarrierContract
{
    internal static async Task Run(CardEffectTestContext ctx, DarkFlameBarrier unused, bool upgraded)
    {
        ctx.AssertEqual("barrier normal energy", 1, unused.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        foreach (var (armor, amp, accept, duration, armorAfter, ampAfter) in new[]
        {
            (-1, 0, false, 1, 0, 0), (0, 0, false, 1, 0, 0),
            (1, 0, true, 2, 0, 0), (3, 0, false, 1, 3, 0),
            (-1, 1, true, 2, 0, 0), (-1, 2, true, 2, 0, 1), (3, 1, true, 2, 3, 0),
        })
        {
            await ctx.Reset();
            if (armor >= 0) await ctx.SetUpArmour(armor);
            if (amp > 0) await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, amp);
            var card = await ctx.Add<DarkFlameBarrier>(PileType.Hand, upgraded);
            await Data.Desire.Set(ctx.Player, 2);
            await PlayerCmd.SetEnergy(3, ctx.Player);
            var paid = await card.SpendResources();
            ctx.AssertEqual("barrier actual energy payment", 2, ctx.Player.PlayerCombatState!.Energy);
            ctx.AssertEqual("barrier actual secondary payment", 1, Data.Desire.Get(ctx.Player));
            await ctx.Play(card, selectedIndices: armor > 0 && amp == 0 ? [accept ? 0 : 1] : null);
            ctx.AssertEqual("block is unconditional and amplified once", amp > 0 ? (upgraded ? 13m : 9m) : (upgraded ? 9m : 6m), ctx.Self.Block);
            ctx.AssertPower("barrier duration independent of block", ctx.Self, "DarkFlameBarrierPower", duration);
            ctx.AssertPower("barrier armour payment", ctx.Self, "MagicArmorPower", armorAfter);
            ctx.AssertPower("barrier amplification payment", ctx.Self, "MagicAmplificationPower", ampAfter);
            ctx.AssertEqual("barrier exhausts", PileType.Exhaust, card.Pile!.Type);
            if (armor >= 0) await TransformationCmd.Exit(new BlockingPlayerChoiceContext(), ctx.Self);
            if (ctx.Self.GetPower<MagicAmplificationPower>() is { } remainingAmp) await PowerCmd.Remove(remainingAmp);
            var context = new BlockingPlayerChoiceContext();
            await CreatureCmd.LoseBlock(context, ctx.Self, ctx.Self.Block, null);
            var power = ctx.Self.GetPower<DarkFlameBarrierPower>()!;
            int hp = ctx.Self.CurrentHp;
            await CreatureCmd.Damage(context, ctx.Self, 10, ValueProp.Move, ctx.PrimaryEnemy);
            ctx.AssertDamage("nonburning attack unaffected", ctx.Self, hp, 10);
            await ctx.ApplyPower<BurningPower>(ctx.PrimaryEnemy, 1);
            hp = ctx.Self.CurrentHp;
            await CreatureCmd.Damage(context, ctx.Self, 10, ValueProp.Move, ctx.PrimaryEnemy);
            ctx.AssertDamage("burning attack halves actual damage", ctx.Self, hp, 5);
            hp = ctx.Self.CurrentHp;
            await CreatureCmd.Damage(context, ctx.Self, 10, ValueProp.Unpowered, ctx.PrimaryEnemy);
            ctx.AssertDamage("native unpowered damage bypasses barrier", ctx.Self, hp, 10);
            ctx.AssertEqual("other target unaffected", 1m, power.ModifyDamageMultiplicative(ctx.PrimaryEnemy, 10, ValueProp.Move, ctx.PrimaryEnemy, null, null));
            ctx.AssertEqual("missing source unaffected", 1m, power.ModifyDamageMultiplicative(ctx.Self, 10, ValueProp.Move, null, null, null));
            await ctx.ApplyPower<BurningPower>(ctx.Self, 1);
            ctx.AssertEqual("friendly source unaffected", 1m, power.ModifyDamageMultiplicative(ctx.Self, 10, ValueProp.Move, ctx.Self, null, null));
            await power.AfterPlayerTurnStart(context, ctx.Player);
            await power.AfterSideTurnEnd(context, CombatSide.Player, [ctx.Self]);
            ctx.AssertPower("individual player turn does not consume duration", ctx.Self, "DarkFlameBarrierPower", duration);
            for (int turn = 1; turn <= duration; turn++)
            {
                await power.AfterSideTurnEnd(context, CombatSide.Enemy, ctx.Enemies);
                ctx.AssertPower("enemy turn end decrements once", ctx.Self, "DarkFlameBarrierPower", duration - turn);
            }
            ctx.AssertTrue("expiry removes state", !ctx.Self.Powers.Contains(power));
            ctx.AssertEqual("expired reference cannot reduce damage", 1m, power.ModifyDamageMultiplicative(ctx.Self, 10, ValueProp.Move, ctx.PrimaryEnemy, null, null));
            await power.AfterSideTurnEnd(context, CombatSide.Enemy, ctx.Enemies);
            ctx.AssertTrue("expired callback remains inert", !ctx.Self.HasPower<DarkFlameBarrierPower>());
        }
        await ctx.Reset();
        var first = await ctx.Add<DarkFlameBarrier>(PileType.Hand, upgraded);
        await ctx.Play(first);
        await ctx.Play(ctx.Create<DarkFlameBarrier>(upgraded));
        ctx.AssertPower("repeated casts add duration not mitigation", ctx.Self, "DarkFlameBarrierPower", 2);
        await ctx.ApplyPower<BurningPower>(ctx.PrimaryEnemy, 1);
        ctx.AssertEqual("two durations still reduce only half", .5m,
            ctx.Self.GetPower<DarkFlameBarrierPower>()!.ModifyDamageMultiplicative(ctx.Self, 10, ValueProp.Move, ctx.PrimaryEnemy, null, null));
    }
}
#endif
