#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncAllCurseBiteContract
{
    internal static async Task Run(CardEffectTestContext ctx, AllCurseBite card, bool upgraded)
    {
        ctx.AssertEqual("cost remains 3/2", upgraded ? 2 : 3,
            card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        var choice = new BlockingPlayerChoiceContext();
        var deck = ctx.Player.RunState.CreateCard<AllCurseBite>(ctx.Player);
        await CardPileCmd.Add(deck, PileType.Deck, skipVisuals: true);
        try
        {
            int accumulated = 0;
            foreach (PileType pile in new[] { PileType.Draw, PileType.Discard, PileType.Hand, PileType.Exhaust, PileType.Play })
            {
                await CardPileCmd.Add(card, pile, skipVisuals: true);
                var fuel = await ctx.Add<MaidenStrike>(PileType.Hand);
                await CardCmd.Exhaust(choice, fuel, skipVisuals: true);
                accumulated += 6;
                ctx.AssertEqual("grows in " + pile, accumulated, card.ExhaustedAttackDamage);
                ctx.AssertEqual("permanent deck never grows", 0, deck.ExhaustedAttackDamage);
            }
            await CardCmd.Exhaust(choice, await ctx.Add<MaidenDefend>(PileType.Hand), skipVisuals: true);
            ctx.AssertEqual("skill contributes no damage", 30, card.ExhaustedAttackDamage);
            var upgradedFuel = await ctx.Add<MaidenStrike>(PileType.Hand, upgraded: true);
            await CardCmd.Exhaust(choice, upgradedFuel, skipVisuals: true);
            ctx.AssertEqual("upgraded attack contributes nine", 39, card.ExhaustedAttackDamage);
            // No other modifiers are present; exact real damage proves calculation feeds OnPlay.
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("grown real attack", ctx.PrimaryEnemy, hp, 40);

            var clone = (AllCurseBite)ctx.Combat.CloneCard(card);
            await CardPileCmd.Add(clone, PileType.Hand, skipVisuals: true);
            ctx.AssertEqual("combat clone keeps growth", 39, clone.ExhaustedAttackDamage);
            var loaded = (AllCurseBite)CardModel.FromSerializable(card.ToSerializable());
            ctx.AssertEqual("serialized combat growth survives", 39, loaded.ExhaustedAttackDamage);
            ctx.AssertEqual("serialized dynamic extra synchronized", 39m, loaded.DynamicVars.ExtraDamage.BaseValue);
            var fresh = (AllCurseBite)ctx.Combat.CloneCard(deck);
            await CardPileCmd.Add(fresh, PileType.Draw, skipVisuals: true);
            ctx.AssertEqual("fresh permanent clone starts clean", 0, fresh.ExhaustedAttackDamage);

            // Exhaust one grown calculated-damage attack. Every copy, including the
            // source itself, must absorb the SAME pre-listener 40, never 1 or 80.
            await CardCmd.Exhaust(choice, clone, skipVisuals: true);
            ctx.AssertEqual("receiver absorbs calculated forty", 79, card.ExhaustedAttackDamage);
            ctx.AssertEqual("self-exhaust absorbs same forty", 79, clone.ExhaustedAttackDamage);
            ctx.AssertEqual("new copy absorbs same forty", 40, fresh.ExhaustedAttackDamage);
            ctx.AssertEqual("permanent copy still zero", 0, deck.ExhaustedAttackDamage);
            ctx.AssertEqual("source now deals eighty after scope released", 80, ExhaustDamageSnapshot.Get(clone));
            // Direct defensive hook is deliberately separate from native dispatch proof.
            await deck.AfterCardExhausted(choice, clone, false);
            ctx.AssertEqual("direct hook cannot mutate permanent card", 0, deck.ExhaustedAttackDamage);
            var attackWithoutDamage = ctx.Create<Judgment>();
            attackWithoutDamage.DynamicVars.Damage.BaseValue = -2;
            ctx.AssertEqual("negative damage contributes zero", 0, ExhaustDamageSnapshot.ReadCardDamage(attackWithoutDamage));

            using (ExhaustDamageSnapshot.Capture(clone))
            {
                clone.ExhaustedAttackDamage = 100;
                ctx.AssertEqual("outer snapshot ignores later mutation", 80, ExhaustDamageSnapshot.Get(clone));
                using (ExhaustDamageSnapshot.Capture(clone))
                    ctx.AssertEqual("nested event has independent value", 101, ExhaustDamageSnapshot.Get(clone));
                ctx.AssertEqual("nested exit restores outer", 80, ExhaustDamageSnapshot.Get(clone));
            }
            ctx.AssertEqual("outer exit releases snapshot", 101, ExhaustDamageSnapshot.Get(clone));
            var scope = ExhaustDamageSnapshot.Capture(clone);
            clone.ExhaustedAttackDamage = 110;
            try
            {
                await ExhaustDamageSnapshot.Complete(Task.FromException(new InvalidOperationException("fixture")), scope);
            }
            catch (InvalidOperationException ex) when (ex.Message == "fixture") { }
            ctx.AssertEqual("faulted task releases snapshot", 111, ExhaustDamageSnapshot.Get(clone));
        }
        finally
        {
            if (deck.Pile?.Type == PileType.Deck)
                await CardPileCmd.RemoveFromDeck([deck], showPreview: false);
        }
    }
}
#endif
