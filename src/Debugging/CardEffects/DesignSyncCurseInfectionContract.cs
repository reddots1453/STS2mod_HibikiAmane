#if DEBUG
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Keywords;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncCurseInfectionContract
{
    private const string Effect = "当这张牌被消耗时，抽2张牌，并将这一效果附加到随机手牌上。";
    private const string Hover = Effect + "不能叠加给已有该效果的牌。";
    private static string Plain(string text) => Regex.Replace(text, @"\[[^\]]*\]", "");

    internal static async Task Run(CardEffectTestContext ctx, CurseInfection card, bool upgraded)
    {
        var choice = new BlockingPlayerChoiceContext();
        CardPile Hand() => PileType.Hand.GetPile(ctx.Player);
        async Task ClearPiles()
        {
            foreach (CardPile pile in ctx.Player.Piles.Where(p => p.IsCombatPile))
                await CardPileCmd.RemoveFromCombat(pile.Cards.ToArray(), skipVisuals: true);
        }
        void CheckText(CardModel candidate)
        {
            string text = Plain(candidate.GetDescriptionForPile(PileType.Hand));
            ctx.AssertEqual("one additional effect sentence " + candidate.Id, 1,
                Regex.Matches(text, Regex.Escape(Effect)).Count);
            IHoverTip expected = HoverTipFactory.FromKeyword(CurseInfectionKeyword.Value);
            ctx.AssertTrue("annotation hover is present " + candidate.Id,
                candidate.HoverTips.Any(tip => tip.Id == expected.Id));
            ctx.AssertTrue("annotation hover has exact full text " + candidate.Id,
                candidate.HoverTips.OfType<HoverTip>().Any(tip => Plain(tip.Description) == Hover));
        }
        ctx.AssertEqual("cost 2/1", upgraded ? 1 : 2, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("rarity uncommon", CardRarity.Uncommon, card.Rarity, effect: false);
        ctx.AssertTrue("source has exhaust", card.Keywords.Contains(CardKeyword.Exhaust), effect: false);
        string sourceText = Plain(card.GetDescriptionForPile(PileType.Hand));
        ctx.AssertTrue("source formal text", sourceText.Contains(Effect), effect: false);
        ctx.AssertTrue("intrinsic source cannot receive duplicate annotation", !CurseInfectionStatus.TryApply(card));
        await ctx.AddFillerCards(PileType.Draw, 2);
        await ctx.Play(card);
        ctx.AssertEqual("source playing actually exhausts", PileType.Exhaust, card.Pile?.Type);
        ctx.AssertEqual("source draws exactly two not four", 2, Hand().Cards.Count);
        ctx.AssertEqual("source marks exactly one drawn hand card", 1, Hand().Cards.Count(CurseInfectionStatus.Has));
        CardModel recipient = Hand().Cards.Single(CurseInfectionStatus.Has);
        CombatEnchantmentCmd.ApplyVanilla<Sharp>(recipient, 3);
        ctx.AssertTrue("existing annotation allows actual enchantment", recipient.Enchantment is Sharp);
        ctx.AssertTrue("cannot duplicate marked recipient", !CurseInfectionStatus.TryApply(recipient));
        CheckText(recipient);
        await ctx.AddFillerCards(PileType.Draw, 2);
        await CardCmd.Exhaust(choice, recipient, skipVisuals: true);
        ctx.AssertEqual("recipient exhaust draws exactly two", 3, Hand().Cards.Count);
        ctx.AssertEqual("recipient passes effect exactly once", 1, Hand().Cards.Count(CurseInfectionStatus.Has));
        ctx.AssertTrue("annotation leaves enchantment intact", recipient.Enchantment is Sharp);

        foreach (Type type in new[] { typeof(MaidenStrike), typeof(MaidenDefend), typeof(MagicIndex), typeof(Dazed), typeof(Injury) })
        {
            await ClearPiles();
            CardModel target = await ctx.Add(type, PileType.Hand);
            CurseInfection inherent = await ctx.Add<CurseInfection>(PileType.Hand);
            string before = target.GetDescriptionForPile(PileType.Hand);
            ctx.AssertTrue("single legal candidate selected " + type.Name, CurseInfectionStatus.TryApplyToRandomHandCard(ctx.Player));
            ctx.AssertTrue("all card types accepted " + type.Name, CurseInfectionStatus.Has(target));
            ctx.AssertTrue("not an enchantment " + type.Name, target.Enchantment == null);
            ctx.AssertTrue("intrinsic source excluded " + type.Name, !CurseInfectionStatus.Has(inherent));
            int rng = ctx.Player.RunState.Rng.CombatCardSelection.ToSerializable().counter;
            ctx.AssertTrue("no stacking " + type.Name, !CurseInfectionStatus.TryApply(target));
            ctx.AssertTrue("no eligible recipient " + type.Name, !CurseInfectionStatus.TryApplyToRandomHandCard(ctx.Player));
            ctx.AssertEqual("no eligible target consumes no RNG " + type.Name, rng,
                ctx.Player.RunState.Rng.CombatCardSelection.ToSerializable().counter);
            ctx.AssertTrue("original description retained " + type.Name, target.GetDescriptionForPile(PileType.Hand).StartsWith(before));
            CheckText(target);
            await ctx.AddFillerCards(PileType.Draw, 2);
            await CardCmd.Exhaust(choice, target, skipVisuals: true);
            ctx.AssertEqual("actual exhaust of all types draws two " + type.Name, 3, Hand().Cards.Count);
            ctx.AssertEqual("actual exhaust of all types transfers once " + type.Name, 1, Hand().Cards.Count(CurseInfectionStatus.Has));
        }

        await ClearPiles();
        var deck = ctx.Player.RunState.CreateCard<MaidenStrike>(ctx.Player);
        await CardPileCmd.Add(deck, PileType.Deck, skipVisuals: true);
        try
        {
            CardModel combatCard = ctx.Combat.CloneCard(deck);
            await CardPileCmd.Add(combatCard, PileType.Hand, skipVisuals: true);
            CombatEnchantmentCmd.ApplyVanilla<Sharp>(combatCard, 2);
            ctx.AssertTrue("already enchanted card can receive annotation", CurseInfectionStatus.TryApply(combatCard));
            ctx.AssertTrue("permanent deck does not receive annotation", !CurseInfectionStatus.Has(deck));
            CardModel copy = ctx.Combat.CloneCard(combatCard);
            await CardPileCmd.Add(copy, PileType.Hand, skipVisuals: true);
            ctx.AssertTrue("combat copy inherits annotation", CurseInfectionStatus.Has(copy));
            ctx.AssertTrue("copy retains enchantment", copy.Enchantment is Sharp);
            ctx.AssertTrue("copy is independent instance", !ReferenceEquals(copy, combatCard));
            CheckText(copy);
            CardModel restored = CardModel.FromSerializable(copy.ToSerializable());
            ctx.AssertTrue("serialized card restores custom keyword", CurseInfectionStatus.Has(restored));
            ctx.AssertTrue("restored annotation cannot stack", !CurseInfectionStatus.TryApply(restored));
            CardModel fresh = ctx.Combat.CloneCard(deck);
            ctx.AssertTrue("fresh permanent clone is unmarked", !CurseInfectionStatus.Has(fresh));
        }
        finally { await CardPileCmd.RemoveFromDeck(deck, showPreview: false); }

        await ClearPiles();
        var fuel = await ctx.Add<MaidenDefend>(PileType.Draw);
        CurseInfectionStatus.TryApply(fuel);
        await ctx.AddFillerCards(PileType.Hand, 10);
        await ctx.AddFillerCards(PileType.Draw, 2);
        await CardCmd.Exhaust(choice, fuel, skipVisuals: true);
        ctx.AssertEqual("full hand remains at capacity", 10, Hand().Cards.Count);
        ctx.AssertEqual("full hand leaves two undrawn cards", 2, PileType.Draw.GetPile(ctx.Player).Cards.Count);
        ctx.AssertEqual("full hand still receives effect", 1, Hand().Cards.Count(CurseInfectionStatus.Has));

        await ClearPiles();
        fuel = await ctx.Add<MaidenDefend>(PileType.Hand);
        var sole = await ctx.Add<Injury>(PileType.Hand);
        CurseInfectionStatus.TryApply(fuel);
        await CardCmd.Exhaust(choice, fuel, skipVisuals: true);
        ctx.AssertEqual("empty deck draws no phantom cards", 1, Hand().Cards.Count);
        ctx.AssertTrue("empty deck still transfers to existing curse", CurseInfectionStatus.Has(sole));
        await ClearPiles();
        ctx.AssertTrue("empty hand transfer is a no-op", !CurseInfectionStatus.TryApplyToRandomHandCard(ctx.Player));

        // Controlled tasks exercise the async adapter, not natural exhaust dispatch.
        fuel = await ctx.Add<MaidenDefend>(PileType.Hand);
        CurseInfectionStatus.TryApply(fuel);
        await ctx.AddFillerCards(PileType.Draw, 2);
        foreach (bool cancel in new[] { false, true })
        {
            Task pending = cancel ? Task.FromCanceled(new CancellationToken(true))
                : Task.FromException(new InvalidOperationException("infection fixture failure"));
            CurseInfectionExhaustPatch.Postfix(ref pending, choice, fuel);
            bool propagated = false;
            try { await pending; }
            catch (OperationCanceledException) when (cancel) { propagated = true; }
            catch (InvalidOperationException ex) when (!cancel && ex.Message == "infection fixture failure") { propagated = true; }
            ctx.AssertTrue("original failure/cancellation propagates", propagated);
            ctx.AssertEqual("failed original cannot draw", 1, Hand().Cards.Count);
            ctx.AssertEqual("failed original leaves draw pile", 2, PileType.Draw.GetPile(ctx.Player).Cards.Count);
        }
        var completion = new TaskCompletionSource();
        Task wrapped = completion.Task;
        CurseInfectionExhaustPatch.Postfix(ref wrapped, choice, fuel);
        ctx.AssertTrue("adapter waits for full original completion", !wrapped.IsCompleted);
        ctx.AssertEqual("no premature draw before original completes", 1, Hand().Cards.Count);
        completion.SetResult();
        await wrapped;
        ctx.AssertEqual("successful original draws once afterwards", 3, Hand().Cards.Count);
        ctx.AssertEqual("successful original transfers one new mark", 2, Hand().Cards.Count(CurseInfectionStatus.Has));
    }
}
#endif
