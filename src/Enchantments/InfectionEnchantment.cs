using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Commands;

namespace MaidenSuccubus.Enchantments;

/// <summary>
/// Negative enchantment granted by an event. When its card is played, its
/// owner takes 2 damage and the cards that were immediately adjacent in hand
/// receive combat-only Infection, subject to the normal single-enchantment
/// slot rule.
/// </summary>
[RegisterEnchantment]
public sealed class InfectionEnchantment : ModEnchantmentTemplate
{
    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://MaidenSuccubus/images/ui/desire_resource.png");

    public override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay? cardPlay)
    {
        if (cardPlay?.Card != Card)
        {
            return;
        }

        await CreatureCmd.Damage(
            choiceContext,
            Card.Owner.Creature,
            2m,
            ValueProp.Unpowered | ValueProp.Move,
            Card.Owner.Creature,
            Card,
            cardPlay);

        foreach (CardModel adjacentCard in InfectionHandSnapshot.Consume(Card))
        {
            CombatEnchantmentCmd.TryApplyAudited<InfectionEnchantment>(
                adjacentCard,
                1m,
                out _);
        }
    }
}
