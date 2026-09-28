#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncMagicBurstContract
{
    private sealed record Case(string Name, int Armor, int Amplification, int Dexterity, int Strength,
        bool Accept, int PreviewBase, int PreviewUpgraded, int ActualBase, int ActualUpgraded,
        int RemainingArmor, int RemainingAmplification);

    // Literal independent oracles. -1 means no form/armour power, not an amount.
    private static readonly Case[] Cases =
    [
        new("no resource", -1, 0, 2, 0, true, 7, 7, 7, 7, 0, 0),
        new("zero armour", 0, 0, 2, 0, true, 7, 7, 7, 7, 0, 0),
        new("last armour", 1, 0, 2, 0, true, 13, 16, 13, 16, 0, 0),
        new("three armour", 3, 0, 2, 0, true, 17, 22, 17, 22, 2, 0),
        new("five armour", 5, 0, 2, 0, true, 21, 28, 21, 28, 4, 0),
        new("declined armour", 1, 0, 2, 0, false, 13, 16, 7, 7, 1, 0),
        new("one amplification", -1, 1, 2, 0, true, 19, 24, 19, 24, 0, 0),
        new("two amplification", -1, 2, 2, 0, true, 22, 28, 22, 28, 0, 1),
        new("amplification before armour", 1, 1, 2, 0, true, 25, 33, 25, 33, 1, 0),
        new("negative dexterity", 1, 0, -2, 0, true, 9, 10, 9, 10, 0, 0),
        new("negative strength", 1, 0, 2, -2, true, 11, 14, 11, 14, 0, 0),
    ];

    internal static async Task Run(CardEffectTestContext ctx, MagicBurst unused, bool upgraded)
    {
        ctx.AssertEqual("one energy cost", 1, unused.EnergyCost.GetWithModifiers(CostModifiers.Local));
        foreach (Case entry in Cases)
        {
            await ctx.Reset();
            if (entry.Armor >= 0) await ctx.SetUpArmour(entry.Armor);
            if (entry.Amplification > 0) await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, entry.Amplification);
            if (entry.Dexterity != 0) await ctx.ApplyPower<DexterityPower>(ctx.Self, entry.Dexterity);
            if (entry.Strength != 0) await ctx.ApplyPower<StrengthPower>(ctx.Self, entry.Strength);
            await ctx.ApplyPower<AmbergrisPower>(ctx.Self, 3); // Hidden cleanup is not three buff layers.
            var card = await ctx.Add<MagicBurst>(PileType.Hand, upgraded);
            int preview = upgraded ? entry.PreviewUpgraded : entry.PreviewBase;
            for (int query = 0; query < 3; query++)
            {
                string full = DesignSyncCombatTextContract.ExpectedOutside(card, upgraded)
                    + $"\n（造成{preview}点伤害）";
                DesignSyncCombatTextContract.AssertText(ctx, card, PileType.Hand, full, entry.Name + " full forecast " + query);
                ctx.AssertPower(entry.Name + " preview never spends armour", ctx.Self, "MagicArmorPower", Math.Max(0, entry.Armor));
                ctx.AssertPower(entry.Name + " preview never spends amplification", ctx.Self, "MagicAmplificationPower", entry.Amplification);
                if (entry.Amplification > 0)
                    ctx.AssertTrue(entry.Name + " preview never reserves amplification",
                        ctx.Self.GetPower<MagicAmplificationPower>()!.CanPreviewOverdraft(card));
            }
            int hp = ctx.PrimaryEnemy.CurrentHp;
            bool hasChoice = entry.Amplification == 0 && entry.Armor > 0;
            await ctx.Play(card, ctx.PrimaryEnemy, selectedIndices: hasChoice ? [entry.Accept ? 0 : 1] : null);
            ctx.AssertDamage(entry.Name + " actual damage", ctx.PrimaryEnemy, hp, upgraded ? entry.ActualUpgraded : entry.ActualBase);
            ctx.AssertPower(entry.Name + " actual remaining armour", ctx.Self, "MagicArmorPower", entry.RemainingArmor);
            ctx.AssertPower(entry.Name + " amplification consumed once", ctx.Self, "MagicAmplificationPower", entry.RemainingAmplification);
            ctx.AssertEqual(entry.Name + " form preserved at zero", entry.Armor >= 0, TransformationCmd.IsTransformed(ctx.Self));
        }

        // Direct hook-level reservation checks, separate from the real command cases above.
        await ctx.Reset();
        await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
        var amplification = ctx.Self.GetPower<MagicAmplificationPower>()!;
        var ownerCard = ctx.Create<MagicBurst>(upgraded);
        var otherCard = ctx.Create<MagicBurst>(upgraded);
        CardPlay receipt = new()
        {
            Card = ownerCard, Player = ctx.Player, Target = ctx.PrimaryEnemy, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 },
            IsAutoPlay = true, PlayIndex = 0, PlayCount = 1,
        };
        ctx.AssertTrue("unreserved layer forecast", amplification.CanPreviewOverdraft(ownerCard));
        using (AmplificationConsumptionScope.Enter(ownerCard))
            ctx.AssertTrue("exempt child cannot forecast automatic release", !amplification.CanPreviewOverdraft(ownerCard));
        ctx.AssertTrue("exemption cleanup restores forecast", amplification.CanPreviewOverdraft(ownerCard));
        await amplification.BeforeCardPlayed(receipt);
        ctx.AssertTrue("reserved owner can forecast its release", amplification.CanPreviewOverdraft(ownerCard));
        ctx.AssertTrue("other card cannot borrow reserved layer", !amplification.CanPreviewOverdraft(otherCard));
        ctx.AssertTrue("read-only query left actual reservation available", amplification.TryReserveForOverdraft(ownerCard));
        ctx.AssertTrue("fully reserved layer cannot promise another release", !amplification.CanPreviewOverdraft(ownerCard));
        ctx.AssertEqual("reservation remains deferred", 1, amplification.Amount);
        await amplification.AfterCardPlayed(new BlockingPlayerChoiceContext(), receipt);
        ctx.AssertPower("actual end of series consumes once", ctx.Self, "MagicAmplificationPower", 0);
    }
}
#endif
