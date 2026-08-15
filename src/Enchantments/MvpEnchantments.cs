using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.Enchantments;

[RegisterEnchantment]
public sealed class ChargeEnchantment : ModEnchantmentTemplate
{
    public override bool HasExtraCardText => true;
    public override bool ShowAmount => true;
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Skill;

    public override async Task OnPlay(PlayerChoiceContext context, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != Card || Status != EnchantmentStatus.Normal)
            return;
        await PlayerCmd.GainEnergy(Amount, Card.Owner);
        Status = EnchantmentStatus.Disabled;
    }
}

[RegisterEnchantment]
public sealed class NecromancyEnchantment : ModEnchantmentTemplate
{
    public override bool HasExtraCardText => true;

    public override async Task AfterCardExhausted(
        PlayerChoiceContext context,
        CardModel card,
        bool causedByEthereal)
    {
        if (card == Card && card.Pile?.Type == PileType.Exhaust)
            await CardPileCmd.Add(card, PileType.Hand);
    }
}

[RegisterEnchantment]
public sealed class SoulLinkEnchantment : ModEnchantmentTemplate
{
    public override bool HasExtraCardText => true;

    public override async Task OnPlay(PlayerChoiceContext context, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != Card) return;
        CardModel[] linked = PileType.Draw.GetPile(Card.Owner).Cards
            .Where(card => card.Enchantment is SoulLinkEnchantment)
            .ToArray();
        foreach (CardModel card in linked)
            await CardPileCmd.Add(card, PileType.Hand);
    }
}

[RegisterEnchantment]
public sealed class CurseInfectionEnchantment : CombatOnlyEnchantmentTemplate
{
    public override bool HasExtraCardText => true;

    public override async Task AfterCardExhausted(
        PlayerChoiceContext context,
        CardModel card,
        bool causedByEthereal)
    {
        if (card != Card) return;
        await CardPileCmd.Draw(context, 2, card.Owner);
        CardModel? next = PileType.Hand.GetPile(card.Owner).Cards
            .Where(candidate => candidate.Enchantment == null)
            .ToList()
            .StableShuffle(card.Owner.RunState.Rng.CombatCardSelection)
            .FirstOrDefault();
        if (next != null)
            MaidenSuccubus.Commands.CombatEnchantmentCmd.Apply<CurseInfectionEnchantment>(next, 1);
    }
}

[RegisterEnchantment]
public sealed class IronWallEnchantment : CombatOnlyEnchantmentTemplate
{
    public override bool HasExtraCardText => true;
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

    public override async Task AfterDamageGiven(
        PlayerChoiceContext context,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (cardSource == Card && dealer == Card.Owner.Creature && result.UnblockedDamage > 0)
            await CreatureCmd.GainBlock(Card.Owner.Creature, result.UnblockedDamage, ValueProp.Move, null);
    }
}

[RegisterEnchantment]
public sealed class WrathEnchantment : ModEnchantmentTemplate
{
    private bool _used;
    public override bool HasExtraCardText => true;
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        if (!props.IsPoweredAttack()) return 0;
        WrathRouteRelic? relic = Card.Owner.Relics.OfType<WrathRouteRelic>().FirstOrDefault();
        return (_used ? 0 : 6) + (relic?.Stage >= 3 ? 6 : 0);
    }

    public override async Task OnPlay(PlayerChoiceContext context, CardPlay? cardPlay)
    {
        if (_used || cardPlay?.Card != Card) return;
        _used = true;
        CardModel copy = Card.Owner.RunState.CloneCard(Card);
        await CardPileCmd.Add(copy, PileType.Discard);
    }
}
