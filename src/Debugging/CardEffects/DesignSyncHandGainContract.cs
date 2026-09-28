#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Data;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncHandGainContract
{
    internal static async Task Run(CardEffectTestContext ctx, AphrodisiacPoisoningCurse card)
    {
        var choice = new BlockingPlayerChoiceContext();
        ctx.AssertTrue("gain curse ethereal", card.Keywords.Contains(CardKeyword.Ethereal));
        ctx.AssertEqual("gain curse cannot upgrade", 0, card.MaxUpgradeLevel);
        await CardPileCmd.Add(card, PileType.Draw, skipVisuals: true);
        await Desire.Set(ctx.Player, 0);
        await Desire.Modify(ctx.Player, 1);
        ctx.AssertEqual("draw pile does not modify gain", 1, Desire.Get(ctx.Player));
        await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
        await Desire.Modify(ctx.Player, 1);
        ctx.AssertEqual("one held copy adds one", 3, Desire.Get(ctx.Player));
        await Desire.Set(ctx.Player, 2);
        ctx.AssertEqual("set is not gain", 2, Desire.Get(ctx.Player));
        await Desire.Modify(ctx.Player, -1);
        ctx.AssertEqual("loss is not gain", 1, Desire.Get(ctx.Player));
        await Desire.Modify(ctx.Player, 0);
        ctx.AssertEqual("zero is not gain", 1, Desire.Get(ctx.Player));
        await SecondaryResourceCmd.Gain(ctx.Player, DesireResource.Id, 0);
        ctx.AssertEqual("zero raw gain adds no bonus", 1, Desire.Get(ctx.Player));
        await SecondaryResourceCmd.Spend(ctx.Player, DesireResource.Id, 1, card);
        ctx.AssertEqual("payment is not gain", 0, Desire.Get(ctx.Player));
        await Desire.Set(ctx.Player, 1);
        await Desire.Modify(ctx.Player, 2);
        ctx.AssertEqual("bonus per gain not per unit", 4, Desire.Get(ctx.Player));

        var second = await ctx.Add<AphrodisiacPoisoningCurse>(PileType.Hand);
        await Desire.Set(ctx.Player, 0);
        await Desire.Modify(ctx.Player, 1);
        ctx.AssertEqual("two held copies stack once each", 3, Desire.Get(ctx.Player));
        await CardCmd.Exhaust(choice, second);
        await Desire.Modify(ctx.Player, 1);
        ctx.AssertEqual("exhaust immediately removes second bonus", 5, Desire.Get(ctx.Player));
        await CardPileCmd.Add(card, PileType.Discard, skipVisuals: true);
        await Desire.Modify(ctx.Player, 1);
        ctx.AssertEqual("discard immediately removes first bonus", 6, Desire.Get(ctx.Player));

        await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
        await Desire.Set(ctx.Player, 0);
        Desire.AmountHandle.Modify(ctx.Player, state => { state.HasValue = true; state.Amount = 4; });
        DesirePersistenceCoordinator.RestoreForCombat(ctx.Player, ctx.Combat);
        ctx.AssertEqual("combat restore is not gain", 4, Desire.Get(ctx.Player));
        await Desire.Modify(ctx.Player, 1);
        ctx.AssertEqual("gain modifier still active after restore", 6, Desire.Get(ctx.Player));
        await Desire.Set(ctx.Player, 9);
        await Desire.Modify(ctx.Player, 1);
        ctx.AssertEqual("overflow reset does not receive bonus", 3, Desire.Get(ctx.Player));
        ctx.AssertPower("full penalty still applies once", ctx.Self, nameof(DesireStunPower), 1);
    }
}
#endif
