using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Commands;

namespace MaidenSuccubus.Enchantments;

/// <summary>
/// Negative enchantment. It hurts its owner while retained in hand and spreads
/// to the immediately adjacent hand cards just before the enchanted card
/// leaves the hand to be played.
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

        foreach (CardModel adjacentCard in InfectionHandSnapshot.Consume(Card))
        {
            CombatEnchantmentCmd.TryApplyAudited<InfectionEnchantment>(
                adjacentCard,
                1m,
                out _);
        }
    }

    public override Task BeforeFlush(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Card.Owner || Card.Pile?.Type != PileType.Hand)
        {
            return Task.CompletedTask;
        }
        return CreatureCmd.Damage(
            choiceContext,
            Card.Owner.Creature,
            3m,
            ValueProp.Unpowered | ValueProp.Move,
            Card.Owner.Creature,
            Card,
            null);
    }
}
