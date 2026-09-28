#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncLegendaryMinerContract
{
    internal static async Task Run(CardEffectTestContext ctx, LegendaryMiner card, bool upgraded)
    {
        int amount = upgraded ? 4 : 3;
        await ctx.Play(card);
        ctx.AssertPower("miner installs per-point block", ctx.Self, "LegendaryMinerPower", amount);
        await SecondaryResourceCmd.Gain(ctx.Player, DesireResource.Id, 2);
        ctx.AssertEqual("actual gain gives block per point", amount * 2m, ctx.Self.Block);
        ctx.AssertEqual("resource gain committed", 2, Data.Desire.Get(ctx.Player));
        await SecondaryResourceCmd.Lose(ctx.Player, DesireResource.Id, 1);
        ctx.AssertEqual("ordinary loss gives no block", amount * 2m, ctx.Self.Block);
        ctx.AssertTrue("actual payment succeeds", await SecondaryResourceCmd.Spend(ctx.Player, DesireResource.Id, 1));
        ctx.AssertEqual("actual spending triggers once not twice", amount * 3m, ctx.Self.Block);
        ctx.AssertEqual("actual payment consumes balance", 0, Data.Desire.Get(ctx.Player));
        ctx.AssertTrue("insufficient payment rejected", !await SecondaryResourceCmd.Spend(ctx.Player, DesireResource.Id, 1));
        await SecondaryResourceCmd.Spend(ctx.Player, DesireResource.Id, 0);
        ctx.AssertEqual("failed or zero payment gives no block", amount * 3m, ctx.Self.Block);
        await SecondaryResourceCmd.Gain(ctx.Player, DesireResource.Id, 3);
        ctx.AssertEqual("subsequent gain remains per point", amount * 6m, ctx.Self.Block);
        await ctx.Play(ctx.Create<LegendaryMiner>(upgraded));
        ctx.AssertPower("two miner cards stack amounts", ctx.Self, "LegendaryMinerPower", amount * 2);
        await SecondaryResourceCmd.Gain(ctx.Player, DesireResource.Id, 1);
        ctx.AssertEqual("stacked gain block", amount * 8m, ctx.Self.Block);
        await SecondaryResourceCmd.Spend(ctx.Player, DesireResource.Id, 2);
        ctx.AssertEqual("stacked spending block", amount * 12m, ctx.Self.Block);
        await PowerCmd.Remove(ctx.Self.GetPower<LegendaryMinerPower>()!);
        await SecondaryResourceCmd.Gain(ctx.Player, DesireResource.Id, 1);
        await SecondaryResourceCmd.Spend(ctx.Player, DesireResource.Id, 1);
        ctx.AssertEqual("removed listener no longer grants block", amount * 12m, ctx.Self.Block);
    }
}
#endif
