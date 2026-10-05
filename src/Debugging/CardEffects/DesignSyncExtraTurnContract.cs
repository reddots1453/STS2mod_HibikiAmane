#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

// Real card commands and native query/broadcast hooks. Clock advancement is
// explicit in this disposable fixture, not proof of the natural combat loop.
internal static class DesignSyncExtraTurnContract
{
    internal static async Task Run(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        ctx.AssertEqual("energy cost 2/1", upgraded ? 1 : 2, card.EnergyCost.Canonical, effect: false);
        var context = new BlockingPlayerChoiceContext();
        var foreign = Player.CreateForNewRun<Ironclad>(ctx.Player.UnlockState, ctx.Player.NetId + 1000);
        foreign.RunState = ctx.Player.RunState;
        await ctx.Play(card);
        var delayed = ctx.Self.GetPower<MultipleReproductionPower>()!;
        ctx.AssertEqual("first delayed stack", 1, delayed.Amount);
        ctx.AssertEqual("first pending grant", 1, delayed.PendingGrants);
        int notifications = 0;
        void Changed() => notifications++;
        delayed.PowerExtraIconAmountLabelsInvalidated += Changed;
        try
        {
            ctx.AssertTrue("normal play is delayed", delayed.DelayOneTurn);
            ctx.AssertEqual("captures personal turn", ctx.Player.PlayerCombatState!.TurnNumber, delayed.DelayAppliedOnTurn);
            CheckPresentation(ctx, delayed, true);
            for (int i = 0; i < 3; i++)
            {
                ctx.AssertTrue("repeated native query does not grant early", !Hook.ShouldTakeExtraTurn(ctx.Combat, ctx.Player));
                ctx.AssertTrue("repeated query does not mutate timing", delayed.DelayOneTurn);
            }
            ctx.AssertEqual("queries do not invalidate display", 0, notifications);
            await delayed.AfterPlayerTurnStart(context, ctx.Player);
            ctx.AssertTrue("same-turn start callback cannot mature freshly played card", delayed.DelayOneTurn);
            await delayed.AfterPlayerTurnStart(context, foreign);
            await delayed.AfterTakingExtraTurn(foreign);
            ctx.AssertTrue("foreign callbacks preserve pending grant", delayed.DelayOneTurn && ctx.Self.Powers.Contains(delayed));
            ctx.AssertTrue("foreign player cannot claim grant", !delayed.ShouldTakeExtraTurn(foreign));

            // Another source grants an immediate extra turn. Native query may
            // skip our listener entirely; native broadcast still reaches it.
            await ctx.ApplyPower<AmbergrisPower>(ctx.Self, 1);
            ctx.AssertTrue("native alternative grants current extra turn", Hook.ShouldTakeExtraTurn(ctx.Combat, ctx.Player));
            ctx.Player.PlayerCombatState.IncrementTurnNumber();
            await Hook.AfterTakingExtraTurn(ctx.Combat, ctx.Player);
            ctx.AssertTrue("alternative broadcast preserves delayed grant", ctx.Self.Powers.Contains(delayed) && delayed.DelayOneTurn);
            ctx.AssertPower("native alternative consumed", ctx.Self, nameof(AmbergrisPower), 0);
            await delayed.AfterPlayerTurnStart(context, ctx.Player);
            ctx.AssertTrue("next personal turn matures pending grant", !delayed.DelayOneTurn);
            ctx.AssertEqual("maturity invalidates display once", 1, notifications);
            CheckPresentation(ctx, delayed, false);
            await delayed.AfterPlayerTurnStart(context, ctx.Player);
            ctx.AssertEqual("duplicate start does not invalidate again", 1, notifications);
            ctx.AssertTrue("mature grant offers extra turn", Hook.ShouldTakeExtraTurn(ctx.Combat, ctx.Player));
            ctx.AssertTrue("mature query is repeatable", Hook.ShouldTakeExtraTurn(ctx.Combat, ctx.Player));
            await delayed.AfterTakingExtraTurn(foreign);
            ctx.AssertTrue("foreign extra turn cannot consume mature grant", ctx.Self.Powers.Contains(delayed));
            ctx.Player.PlayerCombatState.IncrementTurnNumber();
            await Hook.AfterTakingExtraTurn(ctx.Combat, ctx.Player);
            ctx.AssertTrue("mature grant consumed exactly once", !ctx.Self.Powers.Contains(delayed));
            ctx.AssertTrue("removed reference cannot grant turn", !delayed.ShouldTakeExtraTurn(ctx.Player));
            ctx.AssertTrue("no repeated extra turn after consumption", !Hook.ShouldTakeExtraTurn(ctx.Combat, ctx.Player));
            await delayed.AfterTakingExtraTurn(ctx.Player);
            await delayed.AfterPlayerTurnStart(context, ctx.Player);
            ctx.AssertEqual("stale callbacks do not publish display changes", 2, notifications);
        }
        finally { delayed.PowerExtraIconAmountLabelsInvalidated -= Changed; }

        await ctx.Reset();
        await ctx.SetUpArmour(1);
        await ctx.Play(ctx.Create<MultipleReproduction>(upgraded), selectedIndices: [0]);
        var immediate = ctx.Self.GetPower<MultipleReproductionPower>()!;
        ctx.AssertEqual("first immediate grant", 1, immediate.ReadyGrants);
        ctx.AssertTrue("overdraft grants current turn", !immediate.DelayOneTurn);
        CheckPresentation(ctx, immediate, false);
        ctx.AssertPower("overdraft payment", ctx.Self, "MagicArmorPower", 0);
        ctx.AssertTrue("immediate native query", Hook.ShouldTakeExtraTurn(ctx.Combat, ctx.Player));
        ctx.Player.PlayerCombatState!.IncrementTurnNumber();
        await Hook.AfterTakingExtraTurn(ctx.Combat, ctx.Player);
        ctx.AssertTrue("immediate grant consumed", !ctx.Self.Powers.Contains(immediate));
        ctx.AssertTrue("immediate grant not repeated", !Hook.ShouldTakeExtraTurn(ctx.Combat, ctx.Player));

        // Legacy saved states have no DelayAppliedOnTurn field.
        await ctx.Reset();
        await ctx.ApplyPower<MultipleReproductionPower>(ctx.Self, 1);
        var legacy = ctx.Self.GetPower<MultipleReproductionPower>()!;
        legacy.ReadyGrants = 0;
        legacy.PendingGrants = 0;
        legacy.DelayOneTurn = true;
        ctx.AssertEqual("legacy absent turn stamp", -1, legacy.DelayAppliedOnTurn);
        await legacy.AfterPlayerTurnStart(context, ctx.Player);
        ctx.AssertTrue("legacy state matures at next owner start", !legacy.DelayOneTurn);
        ctx.AssertTrue("legacy mature grant works", legacy.ShouldTakeExtraTurn(ctx.Player));
        ctx.AssertTrue("canonical power is inert", !ModelDb.Power<MultipleReproductionPower>().ShouldTakeExtraTurn(ctx.Player));

        await ctx.Reset();
        await ctx.Play(ctx.Create<MultipleReproduction>(upgraded));
        await ctx.Play(ctx.Create<MultipleReproduction>(upgraded));
        var stacked = ctx.Self.GetPower<MultipleReproductionPower>()!;
        ctx.AssertEqual("two delayed cards stack", 2, stacked.Amount);
        ctx.AssertEqual("two delayed grants remain pending", 2, stacked.PendingGrants);
        ctx.AssertEqual("stacked pending marker", "下2", stacked.GetPowerExtraIconAmountLabelSpecs().Single().Text);
        ctx.Player.PlayerCombatState!.IncrementTurnNumber();
        await stacked.AfterPlayerTurnStart(context, ctx.Player);
        ctx.AssertEqual("both pending grants mature", 2, stacked.ReadyGrants);
        ctx.AssertEqual("no pending grant after maturity", 0, stacked.PendingGrants);
        ctx.AssertTrue("first stacked extra turn offered", Hook.ShouldTakeExtraTurn(ctx.Combat, ctx.Player));
        ctx.Player.PlayerCombatState.IncrementTurnNumber();
        await Hook.AfterTakingExtraTurn(ctx.Combat, ctx.Player);
        ctx.AssertEqual("one grant remains after first extra turn", 1, stacked.Amount);
        ctx.AssertTrue("second stacked extra turn offered", Hook.ShouldTakeExtraTurn(ctx.Combat, ctx.Player));
        ctx.Player.PlayerCombatState.IncrementTurnNumber();
        await Hook.AfterTakingExtraTurn(ctx.Combat, ctx.Player);
        ctx.AssertTrue("both stacked grants consumed", !ctx.Self.Powers.Contains(stacked));

        await ctx.Reset();
        await ctx.Play(ctx.Create<MultipleReproduction>(upgraded));
        await ctx.SetUpArmour(1);
        await ctx.Play(ctx.Create<MultipleReproduction>(upgraded), selectedIndices: [0]);
        var mixed = ctx.Self.GetPower<MultipleReproductionPower>()!;
        ctx.AssertEqual("mixed timing total stacks", 2, mixed.Amount);
        ctx.AssertEqual("mixed timing ready grant", 1, mixed.ReadyGrants);
        ctx.AssertEqual("mixed timing pending grant", 1, mixed.PendingGrants);
        ctx.AssertEqual("mixed timing title", "多重再现·本回合与下回合", mixed.Title.GetFormattedText());
        ctx.AssertTrue("mixed timing markers", mixed.GetPowerExtraIconAmountLabelSpecs()
            .Select(spec => spec.Text).Order().SequenceEqual(new[] { "下", "本" }.Order()));
        ctx.AssertTrue("mixed ready grant offered", Hook.ShouldTakeExtraTurn(ctx.Combat, ctx.Player));
        ctx.Player.PlayerCombatState!.IncrementTurnNumber();
        await Hook.AfterTakingExtraTurn(ctx.Combat, ctx.Player);
        ctx.AssertEqual("next-turn grant survives first extra turn", 1, mixed.Amount);
        ctx.AssertEqual("surviving grant still pending", 1, mixed.PendingGrants);
        await mixed.AfterPlayerTurnStart(context, ctx.Player);
        ctx.AssertEqual("surviving grant matures", 1, mixed.ReadyGrants);
        ctx.AssertTrue("second mixed extra turn offered", Hook.ShouldTakeExtraTurn(ctx.Combat, ctx.Player));
        ctx.Player.PlayerCombatState.IncrementTurnNumber();
        await Hook.AfterTakingExtraTurn(ctx.Combat, ctx.Player);
        ctx.AssertTrue("mixed grants consumed separately", !ctx.Self.Powers.Contains(mixed));
    }

    private static void CheckPresentation(CardEffectTestContext ctx, MultipleReproductionPower power, bool delayed)
    {
        ctx.AssertEqual("timing marker", delayed ? "下" : "本", power.GetPowerExtraIconAmountLabelSpecs().Single().Text);
        ctx.AssertEqual("timing title", delayed ? "多重再现·下回合" : "多重再现·本回合", power.Title.GetFormattedText());
    }
}
#endif
