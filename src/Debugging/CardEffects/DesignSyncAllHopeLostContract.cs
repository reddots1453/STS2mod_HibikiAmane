#if DEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Data;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncAllHopeLostContract
{
    private sealed record Case(int Energy, int Desire, bool Auto, bool Replay, bool HpPayment = false);
    private static readonly Case[] Cases =
    [
        new(3, 2, false, false), new(3, 2, false, true),
        new(3, 2, true, false), new(3, 2, true, true),
        new(0, 2, false, false), new(3, 0, false, false),
        new(1, 4, false, false, true),
    ];

    internal static async Task Run(CardEffectTestContext ctx, AllHopeLost card, bool upgraded)
    {
        var outside = ctx.Player.RunState.CreateCard<AllHopeLost>(ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        ctx.AssertEqual("dual X full run text", upgraded ? "造成6Y点伤害X+1次。" : "造成6Y点伤害X次。",
            DesignSyncHolyTextContract.Normalize(outside.GetDescriptionForPile(PileType.Deck)), effect: false);

        foreach (var entry in Cases)
        {
            await ctx.Reset();
            await PlayerCmd.SetEnergy(entry.Energy, ctx.Player);
            await Desire.Set(ctx.Player, entry.Desire);
            var played = await ctx.Add<AllHopeLost>(PileType.Hand, upgraded);
            if (entry.HpPayment) await ctx.ApplyPower<DesirePaidWithHpPower>(ctx.Self, 1);
            int hits = entry.Energy + (upgraded ? 1 : 0);
            played.UpdateDynamicVarPreview(CardPreviewMode.Normal, ctx.PrimaryEnemy, played.DynamicVars);
            ctx.AssertEqual("dual X full combat text", $"造成{6 * entry.Desire}点伤害{hits}次。",
                DesignSyncHolyTextContract.Normalize(played.GetDescriptionForPile(PileType.Hand, ctx.PrimaryEnemy)), effect: false);
            ctx.AssertEqual("dual X damage preview", 6m * entry.Desire, played.DynamicVars.Damage.PreviewValue);
            ctx.AssertEqual("dual X hit preview", (decimal)hits, played.DynamicVars["Hits"].PreviewValue);
            if (entry.Replay) CardCmd.Enchant<Glam>(played, 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int ownHp = ctx.Self.CurrentHp;
            if (entry.Auto)
                await ctx.Play(played, ctx.PrimaryEnemy);
            else
            {
                (int energy, int stars) = await played.SpendResources();
                await played.OnPlayWrapper(new BlockingPlayerChoiceContext(), ctx.PrimaryEnemy, isAutoPlay: false,
                    new ResourceInfo { EnergySpent = energy, EnergyValue = energy, StarsSpent = stars, StarValue = stars },
                    skipCardPileVisuals: true);
            }
            int repeats = entry.Replay ? 2 : 1;
            ctx.AssertDamage("dual X captured Y survives every replay", ctx.PrimaryEnemy, hp, 6 * entry.Desire * hits * repeats);
            ctx.AssertEqual("dual X actual hit count", hits * repeats,
                CombatManager.Instance.History.Entries.OfType<DamageReceivedEntry>()
                    .Count(hit => hit.CardSource == played && hit.Receiver == ctx.PrimaryEnemy));
            ctx.AssertEqual("dual X energy paid once", entry.Auto ? entry.Energy : 0, ctx.Player.PlayerCombatState!.Energy);
            ctx.AssertEqual("dual X desire paid once", entry.Auto || entry.HpPayment ? entry.Desire : 0, Desire.Get(ctx.Player));
            ctx.AssertEqual("dual X HP replacement", ownHp - (entry.HpPayment ? entry.Desire : 0), ctx.Self.CurrentHp);

            // A later play of this same instance must capture NEW resources, not its old payment.
            await PlayerCmd.SetEnergy(1, ctx.Player);
            await Desire.Set(ctx.Player, 1);
            hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(played, ctx.PrimaryEnemy);
            ctx.AssertDamage("dual X later play recaptures resources", ctx.PrimaryEnemy, hp, upgraded ? 12 : 6);
        }
    }
}
#endif
