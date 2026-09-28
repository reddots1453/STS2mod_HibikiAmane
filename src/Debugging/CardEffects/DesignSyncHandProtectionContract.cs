#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Data;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncHandProtectionContract
{
    internal static async Task Run(CardEffectTestContext ctx, ClimaxBanCurse card)
    {
        const string expected = "虚无。\n如果这张牌在你的手牌中，你不会因欲望值满进入高潮平复。";
        var outside = ctx.Player.RunState.CreateCard<ClimaxBanCurse>(ctx.Player);
        DesignSyncCombatTextContract.AssertText(ctx, outside, PileType.Deck, expected, "protection run text");
        DesignSyncCombatTextContract.AssertText(ctx, card, PileType.Hand, expected, "protection combat text");
        ctx.AssertTrue("protection ethereal", card.Keywords.Contains(CardKeyword.Ethereal));
        var choice = new BlockingPlayerChoiceContext();
        var run = (RunState)ctx.Player.RunState;
        var saved = Desire.Handle.Get(run);
        bool oldPending = saved.PendingClimaxResolution;
        int oldCount = saved.PendingClimaxResolutions;
        bool oldFirst = saved.PendingFirstTurnStun;
        try
        {
            Desire.ClearPendingResolutions(run);
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            await Desire.Set(ctx.Player, 9);
            await Desire.Modify(ctx.Player, 1);
            ctx.AssertEqual("held card preserves maximum", 10, Desire.Get(ctx.Player));
            ctx.AssertPower("held card prevents penalty", ctx.Self, nameof(DesireStunPower), 0);
            ctx.AssertTrue("held card does not queue penalty", !Desire.Handle.Get(run).PendingClimaxResolution);
            var second = await ctx.Add<ClimaxBanCurse>(PileType.Hand);
            await CardPileCmd.Add(card, PileType.Discard, skipVisuals: true);
            ctx.AssertEqual("second held copy still protects", 10, Desire.Get(ctx.Player));
            await CardCmd.Exhaust(choice, second);
            ctx.AssertEqual("last copy leaving resolves maximum", 3, Desire.Get(ctx.Player));
            ctx.AssertPower("last departure applies once", ctx.Self, nameof(DesireStunPower), 1);
            await PowerCmd.Remove(ctx.Self.GetPower<DesireStunPower>()!);
            await Desire.ResolvePendingFirstTurnStun(choice, ctx.Player);
            ctx.AssertPower("departure leaves no duplicate pending", ctx.Self, nameof(DesireStunPower), 0);

            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            await Desire.Set(ctx.Player, 10);
            Desire.Handle.Modify(run, state => state.PendingClimaxResolution = true);
            await Desire.ResolvePendingFirstTurnStun(choice, ctx.Player);
            ctx.AssertEqual("pending resolution waits while protected", 10, Desire.Get(ctx.Player));
            ctx.AssertPower("pending entry obeys protection", ctx.Self, nameof(DesireStunPower), 0);
            ctx.AssertTrue("blocked pending preserved", Desire.Handle.Get(run).PendingClimaxResolution);
            await CardPileCmd.Add(card, PileType.Draw, skipVisuals: true);
            ctx.AssertEqual("pending last departure resolves", 3, Desire.Get(ctx.Player));
            ctx.AssertTrue("immediate settlement consumes pending", !Desire.Handle.Get(run).PendingClimaxResolution);
            await PowerCmd.Remove(ctx.Self.GetPower<DesireStunPower>()!);

            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            await Desire.Set(ctx.Player, 9);
            await CardPileCmd.Add(card, PileType.Discard, skipVisuals: true);
            ctx.AssertEqual("below maximum departure preserves amount", 9, Desire.Get(ctx.Player));
            ctx.AssertPower("below maximum departure no penalty", ctx.Self, nameof(DesireStunPower), 0);
            await Desire.Modify(ctx.Player, 1);
            ctx.AssertEqual("normal threshold restored after departure", 3, Desire.Get(ctx.Player));
        }
        finally
        {
            Desire.Handle.Modify(run, state =>
            {
                state.PendingClimaxResolution = oldPending;
                state.PendingClimaxResolutions = oldCount;
                state.PendingFirstTurnStun = oldFirst;
            });
        }
    }
}
#endif
