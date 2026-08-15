using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

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
