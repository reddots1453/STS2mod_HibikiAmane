#if DEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncBurningDesireContract
{
    private sealed record Case(int[] Prior, int Held, int? Cost, bool Pay, int Hits);
    private static readonly Case[] Cases =
    [
        new([], 7, null, false, 1), new([], 1, null, true, 2),
        new([], 7, null, true, 2), new([2], 1, null, true, 4),
        new([2, 3], 7, null, true, 7), new([3], 0, null, false, 4),
        new([], 7, 0, true, 1), new([], 6, 3, true, 4),
        new([], 4, -1, true, 5),
    ];

    internal static async Task Run(CardEffectTestContext ctx, BurningDesire card, bool upgraded)
    {
        string expected = upgraded
            ? "造成4点伤害。\n本场战斗中每消耗〈欲望〉，重复1次。"
            : "造成3点伤害。\n本场战斗中每消耗〈欲望〉，重复1次。";
        var outside = ctx.Player.RunState.CreateCard(ModelDb.Card<BurningDesire>(), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        foreach (var (instance, pile) in new[] { (outside, PileType.Deck), (card, PileType.Hand) })
        {
            instance.UpdateDynamicVarPreview(CardPreviewMode.Normal, ctx.PrimaryEnemy, instance.DynamicVars);
            ctx.AssertEqual("miasma thunder full rendered text " + pile, expected,
                DesignSyncHolyTextContract.Normalize(instance.GetDescriptionForPile(pile, ctx.PrimaryEnemy)), effect: false);
        }
        ctx.AssertEqual("thunder attack", CardType.Attack, card.Type, effect: false);
        ctx.AssertEqual("thunder uncommon", CardRarity.Uncommon, card.Rarity, effect: false);
        ctx.AssertEqual("thunder directed target", TargetType.AnyEnemy, card.TargetType, effect: false);
        ctx.AssertEqual("thunder energy", 1, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);

        foreach (var entry in Cases)
        {
            await ctx.Reset();
            foreach (int prior in entry.Prior)
            {
                await Data.Desire.Set(ctx.Player, prior);
                ctx.AssertTrue("prior actual payment succeeds",
                    await SecondaryResourceCmd.Spend(ctx.Player, DesireResource.Id, prior));
            }
            await Data.Desire.Set(ctx.Player, entry.Held);
            var played = await ctx.Add<BurningDesire>(PileType.Hand, upgraded);
            if (entry.Cost is int cost)
                played.SecondaryCosts().Set(DesireResource.Id,
                    cost < 0 ? SecondaryResourceCost.X() : new SecondaryResourceCost(cost));
            ctx.AssertEqual("setup mutations do not count as payment", entry.Prior.Sum(), DesireCombatSpending.Get(ctx.Player));
            int energy = ctx.Player.PlayerCombatState!.Energy;
            if (entry.Pay) await played.SpendResources();
            int paid = entry.Pay ? (entry.Cost == -1 ? entry.Held : entry.Cost ?? 1) : 0;
            ctx.AssertEqual("actual payment amount counted once", entry.Hits - 1, DesireCombatSpending.Get(ctx.Player));
            ctx.AssertEqual("remaining resource is not repeat count", entry.Held - paid, Data.Desire.Get(ctx.Player));
            ctx.AssertEqual("energy does not inflate resource tally", energy - (entry.Pay ? 1 : 0), ctx.Player.PlayerCombatState.Energy);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(played, ctx.PrimaryEnemy);
            ctx.AssertDamage("cumulative spending determines actual damage", ctx.PrimaryEnemy, hp, (upgraded ? 4 : 3) * entry.Hits);
            ctx.AssertEqual("distinct damage segments match repeat count", entry.Hits,
                CombatManager.Instance.History.Entries.OfType<DamageReceivedEntry>()
                    .Count(hit => hit.CardSource == played && hit.Receiver == ctx.PrimaryEnemy));
            ctx.AssertEqual("first play does not double count payment", entry.Hits - 1, DesireCombatSpending.Get(ctx.Player));
            hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(played, ctx.PrimaryEnemy);
            ctx.AssertDamage("free repeated execution retains cumulative hits", ctx.PrimaryEnemy, hp, (upgraded ? 4 : 3) * entry.Hits);
            ctx.AssertEqual("free execution keeps separate attack segments", entry.Hits * 2,
                CombatManager.Instance.History.Entries.OfType<DamageReceivedEntry>()
                    .Count(hit => hit.CardSource == played && hit.Receiver == ctx.PrimaryEnemy));
            ctx.AssertEqual("repeat creates no second payment", entry.Hits - 1, DesireCombatSpending.Get(ctx.Player));
        }

        await ctx.Reset();
        await Data.Desire.Set(ctx.Player, 7);
        await SecondaryResourceCmd.Lose(ctx.Player, DesireResource.Id, 2);
        ctx.AssertEqual("loss is not spending", 0, DesireCombatSpending.Get(ctx.Player));
        ctx.AssertTrue("insufficient payment fails", !await SecondaryResourceCmd.Spend(ctx.Player, DesireResource.Id, 6));
        await SecondaryResourceCmd.Spend(ctx.Player, DesireResource.Id, 0);
        await SecondaryResourceCmd.Spend(ctx.Player, DesireResource.Id, -1);
        ctx.AssertEqual("failed and nonpositive payment not counted", 0, DesireCombatSpending.Get(ctx.Player));
        await SecondaryResourceCmd.Spend(ctx.Player, DesireResource.Id, 2);
        await Data.Desire.Set(ctx.Player, 1);
        await Data.Desire.Set(ctx.Player, 1);
        ctx.AssertEqual("refresh does not reset cumulative spending", 2, DesireCombatSpending.Get(ctx.Player));
        int round = ctx.Combat.RoundNumber;
        var side = ctx.Combat.CurrentSide;
        try
        {
            ctx.Combat.RoundNumber = round + 1;
            ctx.Combat.CurrentSide = CombatSide.Enemy;
            ctx.AssertEqual("round and side transition do not reset spending", 2, DesireCombatSpending.Get(ctx.Player));
        }
        finally { ctx.Combat.RoundNumber = round; ctx.Combat.CurrentSide = side; }

        // Hook boundary probes, not a claim that multiplayer/reload was run.
        var rules = new DesireResourceRules();
        await rules.AfterSecondaryResourceSpent(new SecondaryResourceSpendContext(
            new CombatState(), ctx.Player, DesireResource.Definition, null, 7, null));
        await rules.AfterSecondaryResourceSpent(new SecondaryResourceSpendContext(
            ctx.Combat, ctx.Player, new SecondaryResourceDefinition(defaultAmount: 0), null, 7, null));
        ctx.AssertEqual("stale combat and different resource rejected", 2, DesireCombatSpending.Get(ctx.Player));

        await ctx.Reset();
        await Data.Desire.Set(ctx.Player, 3);
        await ctx.ApplyPower<DesirePaidWithHpPower>(ctx.Self, 1);
        var replaced = await ctx.Add<BurningDesire>(PileType.Hand, upgraded);
        int ownHp = ctx.Self.CurrentHp;
        await replaced.SpendResources();
        ctx.AssertEqual("HP substitution leaves resource unchanged", 3, Data.Desire.Get(ctx.Player));
        ctx.AssertEqual("HP payment actually committed", ownHp - 1, ctx.Self.CurrentHp);
        ctx.AssertEqual("HP substitution is not desire spending", 0, DesireCombatSpending.Get(ctx.Player));
        int enemyHp = ctx.PrimaryEnemy.CurrentHp;
        await ctx.Play(replaced, ctx.PrimaryEnemy);
        ctx.AssertDamage("replaced cost grants only initial hit", ctx.PrimaryEnemy, enemyHp, upgraded ? 4 : 3);
        DesireCombatSpending.Close(ctx.Combat);
        await rules.AfterSecondaryResourceSpent(new SecondaryResourceSpendContext(
            ctx.Combat, ctx.Player, DesireResource.Definition, null, 7, null));
        ctx.AssertEqual("closed combat rejects late payment notifications", 0, DesireCombatSpending.Get(ctx.Player));
    }
}
#endif
