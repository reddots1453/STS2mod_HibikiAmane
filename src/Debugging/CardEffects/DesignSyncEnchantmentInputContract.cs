#if DEBUG
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncEnchantmentInputContract
{
    internal static readonly Type[] Types = [typeof(MagicIndex), typeof(MagicSword), typeof(GaleSword), typeof(ShiningSword)];
    private static string Plain(string text) => Regex.Replace(text, @"\[[^\]]*\]", "");

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        if (!Types.Contains(card.GetType())) return;
        string expected = card switch
        {
            MagicIndex => "每当你抽到1张附魔牌时，抽1张牌。",
            MagicSword => $"造成{(upgraded ? 18 : 12)}点伤害。\n拾起时，为这张牌附魔：充能：2。",
            GaleSword => $"造成{(upgraded ? 14 : 11)}点伤害。\n拾起时，为这张牌附魔：迅捷：2。",
            ShiningSword => $"造成{(upgraded ? 6 : 4)}点伤害2次。\n拾起时，为这张牌附魔：活力：3。",
            _ => throw new InvalidOperationException()
        };
        int cost = card is MagicSword ? 2 : card is MagicIndex && upgraded ? 0 : 1;
        ctx.AssertEqual("enchantment input exact cost", cost, card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("enchantment input exact rarity", card is MagicIndex ? CardRarity.Uncommon : CardRarity.Common, card.Rarity, effect: false);
        ctx.AssertEqual("enchantment input exact type", card is MagicIndex ? CardType.Power : CardType.Attack, card.Type, effect: false);
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        ctx.AssertEqual("enchantment input full deck preview text", expected, Plain(outside.GetDescriptionForPile(PileType.Deck)), effect: false);
        ctx.AssertEqual("enchantment input full combat preview text", expected, Plain(card.GetDescriptionForPile(PileType.Hand)), effect: false);
    }

    internal static async Task Sword(CardEffectTestContext ctx, CardModel unenchanted, bool upgraded)
    {
        var deck = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(unenchanted.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(deck);
        Type expectedType = deck is MagicSword ? typeof(ChargeEnchantment) : deck is GaleSword ? typeof(Swift) : typeof(Vigorous);
        int amount = deck is ShiningSword ? 3 : 2;
        try
        {
            await CardPileCmd.AddGeneratedCardToCombat(unenchanted, PileType.Hand, ctx.Player);
            ctx.AssertTrue("combat generation is not pickup", unenchanted.Enchantment == null);
            await CardPileCmd.RemoveFromCombat(unenchanted, skipVisuals: true);
            await CardPileCmd.Add(deck, PileType.Deck, skipVisuals: true);
            ctx.AssertEqual("native pickup exact enchantment type", expectedType, deck.Enchantment?.GetType());
            ctx.AssertEqual("native pickup exact amount", amount, deck.Enchantment?.Amount ?? 0);
            EnchantmentModel original = deck.Enchantment!;
            await deck.AfterCardChangedPiles(deck, PileType.None, null);
            ctx.AssertTrue("repeated pickup notification preserves enchantment instance", ReferenceEquals(original, deck.Enchantment));
            ctx.AssertEqual("repeated pickup cannot add enchantment amount", amount, deck.Enchantment!.Amount);

            CardModel combat = ctx.Combat.CloneCard(deck);
            combat.DeckVersion = deck;
            await CardPileCmd.Add(combat, PileType.Hand, skipVisuals: true);
            ctx.AssertTrue("combat copy has separate enchantment instance", !ReferenceEquals(deck.Enchantment, combat.Enchantment));
            int ordinaryDamage = deck switch { MagicSword => upgraded ? 18 : 12, GaleSword => upgraded ? 14 : 11, _ => upgraded ? 12 : 8 };
            for (int play = 0; play < 2; play++)
            {
                await ctx.AddFillerCards(PileType.Draw, 2);
                int hand = PileType.Hand.GetPile(ctx.Player).Cards.Count - (combat.Pile?.Type == PileType.Hand ? 1 : 0);
                int energy = ctx.Player.PlayerCombatState!.Energy;
                int hp = ctx.PrimaryEnemy.CurrentHp;
                await ctx.Play(combat, ctx.PrimaryEnemy);
                ctx.AssertDamage("sword actual damage on play " + play, ctx.PrimaryEnemy, hp,
                    ordinaryDamage + (deck is ShiningSword && play == 0 ? 6 : 0));
                ctx.AssertEqual("charge only first play", energy + (deck is MagicSword && play == 0 ? 2 : 0), ctx.Player.PlayerCombatState.Energy);
                ctx.AssertEqual("swift only first play", hand + (deck is GaleSword && play == 0 ? 2 : 0), PileType.Hand.GetPile(ctx.Player).Cards.Count);
                ctx.AssertEqual("combat enchantment spent", EnchantmentStatus.Disabled, combat.Enchantment!.Status);
                ctx.AssertEqual("permanent enchantment not consumed", EnchantmentStatus.Normal, deck.Enchantment!.Status);
            }
            ctx.AssertEqual("permanent enchantment amount preserved", amount, deck.Enchantment!.Amount);
        }
        finally
        {
            if (!deck.HasBeenRemovedFromState && deck.Pile?.Type == PileType.Deck)
                await CardPileCmd.RemoveFromDeck(deck, showPreview: false);
        }
    }

    internal static async Task Index(CardEffectTestContext ctx, MagicIndex card, bool upgraded)
    {
        var choice = new BlockingPlayerChoiceContext();
        int Hand() => PileType.Hand.GetPile(ctx.Player).Cards.Count;
        MagicIndexPower power = null!;
        async Task Activate(int stacks = 1)
        {
            await ctx.Reset();
            for (int i = 0; i < stacks; i++) await ctx.Play(ctx.Create<MagicIndex>(upgraded));
            power = ctx.Self.GetPower<MagicIndexPower>()!;
            ctx.AssertPower("index actual stacking", ctx.Self, "MagicIndexPower", stacks);
            var smart = power.SmartDescription;
            smart.Add("Amount", power.Amount);
            ctx.AssertEqual("index exact smart description", $"每当你抽到1张附魔牌时，抽{stacks}张牌。", Plain(smart.GetFormattedText()), effect: false);
        }
        async Task<CardModel> Enchanted(PileType pile)
        {
            var result = ctx.Create<MaidenStrike>();
            CombatEnchantmentCmd.ApplyVanilla<Sharp>(result, 1);
            await CardPileCmd.Add(result, pile, CardPilePosition.Top, skipVisuals: true);
            return result;
        }
        foreach (int stacks in new[] { 1, 2 })
        {
            await Activate(stacks);
            await ctx.AddFillerCards(PileType.Draw, 5);
            await Enchanted(PileType.Draw);
            await Enchanted(PileType.Draw);
            await CardPileCmd.Draw(choice, 1, ctx.Player);
            ctx.AssertEqual("continuous enchanted draws preserve all nested triggers", stacks == 1 ? 3 : 5, Hand());
        }
        await Activate();
        await ctx.AddFillerCards(PileType.Draw, 3);
        await CardPileCmd.Draw(choice, 1, ctx.Player);
        ctx.AssertEqual("ordinary draw has no bonus", 1, Hand());

        await Activate();
        var generated = ctx.Create<MaidenStrike>();
        CombatEnchantmentCmd.ApplyVanilla<Sharp>(generated, 1);
        await ctx.AddFillerCards(PileType.Draw, 3);
        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, ctx.Player);
        ctx.AssertEqual("generated hand card is not drawn", 1, Hand());
        await CardPileCmd.Add(generated, PileType.Draw, CardPilePosition.Top, skipVisuals: true);
        for (int i = 0; i < 2; i++)
        {
            int before = Hand();
            await CardPileCmd.Draw(choice, 1, ctx.Player);
            ctx.AssertEqual("same instance drawn again gets a fresh trigger", before + 2, Hand());
            if (i == 0) await CardPileCmd.Add(generated, PileType.Draw, CardPilePosition.Top, skipVisuals: true);
        }

        await Activate();
        await ctx.AddFillerCards(PileType.Draw, 3);
        var layered = ctx.Create<LightWings>();
        CombatEnchantmentCmd.ApplyVanilla<Sharp>(layered, 1);
        CombatEnchantmentCmd.ApplyVanilla<Glam>(layered, 1);
        await CardPileCmd.Add(layered, PileType.Draw, CardPilePosition.Top, skipVisuals: true);
        await CardPileCmd.Draw(choice, 1, ctx.Player);
        ctx.AssertEqual("multiple enchantment layers still grant one draw", 2, Hand());

        foreach (int initial in new[] { 9, 10 })
        {
            await Activate();
            await ctx.AddFillerCards(PileType.Hand, initial);
            await Enchanted(PileType.Draw);
            await ctx.AddFillerCards(PileType.Draw, 2);
            await CardPileCmd.Draw(choice, 1, ctx.Player);
            ctx.AssertEqual("native full hand bound", 10, Hand());
            ctx.AssertEqual("blocked bonus stays in draw pile", initial == 9 ? 2 : 3, PileType.Draw.GetPile(ctx.Player).Cards.Count);
        }
        await Activate();
        await Enchanted(PileType.Draw);
        await ctx.AddFillerCards(PileType.Draw, 2);
        await ctx.ApplyPower<NoDrawPower>(ctx.Self, 1);
        await CardPileCmd.Draw(choice, 1, ctx.Player);
        ctx.AssertEqual("no draw blocks initial and bonus draws", 0, Hand());
        await CardPileCmd.Draw(choice, 1, ctx.Player, fromHandDraw: true);
        ctx.AssertEqual("turn start draw allowed but index bonus respects no draw", 1, Hand());

        await Activate();
        var shuffled = await Enchanted(PileType.Discard);
        await CardPileCmd.Draw(choice, 1, ctx.Player);
        ctx.AssertEqual("single enchanted discard reshuffles and stops at empty", 1, Hand());
        ctx.AssertEqual("reshuffle moves the original instance", PileType.Hand, shuffled.Pile?.Type);
        await PowerCmd.Remove(power);
        await ctx.AddFillerCards(PileType.Draw, 2);
        await power.AfterCardDrawnEarly(choice, shuffled, false);
        ctx.AssertEqual("removed power callback cannot draw", 1, Hand());
        await Activate();
        await CardPileCmd.Draw(choice, 1, ctx.Player);
        ctx.AssertEqual("empty piles cannot create cards", 0, Hand());
        await ctx.AddFillerCards(PileType.Draw, 2);
        var foreign = Player.CreateForNewRun<Ironclad>(ctx.Player.UnlockState, ctx.Player.NetId + 1000);
        foreign.RunState = ctx.Player.RunState;
        var foreignCard = ctx.Player.RunState.CreateCard<MaidenStrike>(foreign);
        CardCmd.Enchant<Sharp>(foreignCard, 1);
        await power.AfterCardDrawnEarly(choice, foreignCard, false);
        ctx.AssertEqual("another player draw cannot trigger owner's index", 0, Hand());
        var deckCard = ctx.Player.RunState.CreateCard<MaidenStrike>(ctx.Player);
        CardCmd.Enchant<Sharp>(deckCard, 1);
        await power.AfterCardDrawnEarly(choice, deckCard, false);
        ctx.AssertEqual("noncombat card cannot trigger combat index", 0, Hand());
    }
}
#endif
