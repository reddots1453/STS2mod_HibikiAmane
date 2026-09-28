#if DEBUG
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MaidenSuccubus.Cards;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncAcceleratedMotionContract
{
    internal static async Task Run(CardEffectTestContext ctx, AcceleratedMotion card, bool upgraded)
    {
        ctx.AssertEqual("accelerated cost", 0, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("accelerated rarity", CardRarity.Rare, card.Rarity, effect: false);
        ctx.AssertEqual("accelerated type", CardType.Skill, card.Type, effect: false);
        ctx.AssertEqual("accelerated target", TargetType.Self, card.TargetType, effect: false);
        ctx.AssertTrue("accelerated exhaust keyword", card.Keywords.Contains(CardKeyword.Exhaust), effect: false);
        string expected = $"抽{(upgraded ? 3 : 2)}张牌。\n拾起时，向牌组中加入1张复制。\n消耗。";
        var outside = ctx.Player.RunState.CreateCard<AcceleratedMotion>(ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        foreach (var (instance, pile) in new[] { ((CardModel)outside, PileType.Deck), ((CardModel)card, PileType.Hand) })
            ctx.AssertEqual("accelerated full card text " + pile, expected,
                Regex.Replace(instance.GetDescriptionForPile(pile), @"\[[^\]]*\]", ""), effect: false);

        int permanentCount = PileType.Deck.GetPile(ctx.Player).Cards.Count;
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, ctx.Player);
        ctx.AssertEqual("combat generation adds no permanent copy", permanentCount, PileType.Deck.GetPile(ctx.Player).Cards.Count);
        ctx.AssertTrue("combat generation does not mark pickup", !card.AddedPickupCopies);
        await ctx.AddFillerCards(PileType.Draw, 6);
        await ctx.Play(card);
        ctx.AssertEqual("accelerated exact draw", upgraded ? 3 : 2, PileType.Hand.GetPile(ctx.Player).Cards.Count);
        ctx.AssertEqual("accelerated play exhausts", PileType.Exhaust, card.Pile?.Type);
        ctx.AssertEqual("combat play adds no permanent copy", permanentCount, PileType.Deck.GetPile(ctx.Player).Cards.Count);

        foreach (bool enchanted in new[] { false, true })
        {
            var baseline = PileType.Deck.GetPile(ctx.Player).Cards.ToHashSet();
            var original = ctx.Player.RunState.CreateCard<AcceleratedMotion>(ctx.Player);
            if (upgraded) CardCmd.Upgrade(original);
            if (enchanted)
            {
                CardCmd.Enchant<Swift>(original, 4);
                original.AddKeyword(CardKeyword.Retain);
            }
            try
            {
                await CardPileCmd.Add(original, PileType.Deck, skipVisuals: true);
                var added = PileType.Deck.GetPile(ctx.Player).Cards.Except(baseline).OfType<AcceleratedMotion>().ToArray();
                ctx.AssertEqual("pickup adds original plus exactly one copy", 2, added.Length);
                var copy = added.Single(c => !ReferenceEquals(c, original));
                ctx.AssertTrue("both permanent instances suppress recursive pickup", original.AddedPickupCopies && copy.AddedPickupCopies);
                ctx.AssertTrue("run copy is independently owned in deck", copy.Owner == ctx.Player && copy.Pile?.Type == PileType.Deck && copy.CombatState == null);
                ctx.AssertEqual("pickup preserves upgrade", upgraded, copy.IsUpgraded);
                ctx.AssertEqual("pickup preserves draw value", upgraded ? 3m : 2m, copy.DynamicVars.Cards.BaseValue);
                ctx.AssertEqual("pickup preserves added native keyword", enchanted, copy.Keywords.Contains(CardKeyword.Retain));
                ctx.AssertTrue("pickup preserves full enchantment", enchanted
                    ? copy.Enchantment is Swift { Amount: 4, Status: EnchantmentStatus.Normal }
                    : copy.Enchantment == null);
                if (enchanted)
                {
                    ctx.AssertTrue("enchantment is deep cloned and rebound", !ReferenceEquals(copy.Enchantment, original.Enchantment)
                        && ReferenceEquals(copy.Enchantment!.Card, copy));
                    copy.RemoveKeyword(CardKeyword.Retain);
                    ctx.AssertTrue("copy keyword mutation cannot alter original", original.Keywords.Contains(CardKeyword.Retain));
                }
                int count = PileType.Deck.GetPile(ctx.Player).Cards.Count;
                await original.AfterCardChangedPiles(original, PileType.None, null);
                await copy.AfterCardChangedPiles(copy, PileType.None, null);
                ctx.AssertEqual("duplicate notifications cannot multiply deck", count, PileType.Deck.GetPile(ctx.Player).Cards.Count);

                foreach (var saved in new[] { original, copy })
                {
                    var resumed = (AcceleratedMotion)ctx.Player.RunState.LoadCard(saved.ToSerializable(), ctx.Player);
                    ctx.AssertTrue("saved pickup guard survives model loading", resumed.AddedPickupCopies);
                    ctx.AssertEqual("saved upgrade survives model loading", upgraded, resumed.IsUpgraded);
                    ctx.AssertTrue("saved enchantment survives model loading", enchanted
                        ? resumed.Enchantment is Swift { Amount: 4 }
                        : resumed.Enchantment == null);
                    int beforeLoadAdd = PileType.Deck.GetPile(ctx.Player).Cards.Count;
                    await CardPileCmd.Add(resumed, PileType.Deck, skipVisuals: true);
                    ctx.AssertEqual("loading picked card never grants another copy", beforeLoadAdd + 1, PileType.Deck.GetPile(ctx.Player).Cards.Count);
                }
                var combat = (AcceleratedMotion)ctx.Combat.CloneCard(copy);
                combat.DeckVersion = copy;
                count = PileType.Deck.GetPile(ctx.Player).Cards.Count;
                await CardPileCmd.AddGeneratedCardToCombat(combat, PileType.Hand, ctx.Player);
                ctx.AssertEqual("permanent copy entering combat does not replicate deck", count, PileType.Deck.GetPile(ctx.Player).Cards.Count);
            }
            finally
            {
                // Only cards created by this isolated pickup scenario are removed.
                var created = PileType.Deck.GetPile(ctx.Player).Cards.Except(baseline).OfType<AcceleratedMotion>().ToArray();
                if (created.Length > 0) await CardPileCmd.RemoveFromDeck(created, showPreview: false);
            }
        }
    }
}
#endif
