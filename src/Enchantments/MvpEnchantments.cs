using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Core.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MaidenSuccubus.Enchantments;

/// <summary>
/// Retired identity kept for old saves. No new acquisition or runtime effect.
/// </summary>
[RegisterEnchantment]
public sealed class EnergyOverloadEnchantment : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => EnchantmentIconAssets.For("energy_overload_enchantment_v1");
    public override bool HasExtraCardText => true;
    public override bool CanEnchant(CardModel card) => false;
}

[RegisterEnchantment]
public sealed class ChargeEnchantment : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => EnchantmentIconAssets.For("charge_enchantment_v1");
    public override bool HasExtraCardText => true;
    public override bool ShowAmount => true;
    public override bool CanEnchantCardType(CardType cardType) => true;

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
    public override EnchantmentAssetProfile AssetProfile => EnchantmentIconAssets.For("necromancy_enchantment_v1");
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
    public override EnchantmentAssetProfile AssetProfile => EnchantmentIconAssets.For("soul_link_enchantment_v1");
    public override bool HasExtraCardText => true;

    public override async Task OnPlay(PlayerChoiceContext context, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != Card) return;
        CardModel[] linked = PileType.Draw.GetPile(Card.Owner).Cards
            .Where(LayeredEnchantments.Has<SoulLinkEnchantment>)
            .ToArray();
        foreach (CardModel card in linked)
            await CardPileCmd.Add(card, PileType.Hand);
    }
}

[RegisterEnchantment]
public sealed class IronWallEnchantment : CombatOnlyEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => EnchantmentIconAssets.For("iron_wall_enchantment_v1");
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
public sealed class ProliferationEnchantment : CombatOnlyEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => EnchantmentIconAssets.For("proliferation_enchantment_v1");
    public override bool HasExtraCardText => true;

    public override async Task OnPlay(PlayerChoiceContext context, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != Card || Card.CombatState is null)
            return;
        CardModel copy = Card.CreateClone();
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Draw, Card.Owner, CardPilePosition.Random);
    }
}

[RegisterEnchantment]
public sealed class WrathEnchantment : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => EnchantmentIconAssets.For("wrath_enchantment_v1");
    private bool _used;
    [SavedProperty]
    public bool UsedThisCombat { get => _used; set { AssertMutable(); _used = value; } }
    public override bool HasExtraCardText => true;
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
        => WrathRules.FirstPlayBonus(_used, props.IsPoweredAttack());

    public override async Task OnPlay(PlayerChoiceContext context, CardPlay? cardPlay)
    {
        if (_used || cardPlay?.Card != Card || Card.CombatState == null) return;
        _used = true;
        CardModel copy = Card.CreateClone();
        ResetGeneratedCopy(copy);
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, Card.Owner);
    }

    // Only copies produced by Wrath get a fresh Wrath trigger. General clones,
    // save/load, and the spent states of unrelated enchantments remain intact.
    internal static void ResetGeneratedCopy(CardModel copy)
    {
        if (copy.Enchantment is WrathEnchantment single) single.UsedThisCombat = false;
        if (copy.Enchantment is LayeredEnchantment layers)
            foreach (var wrath in layers.Layers.OfType<WrathEnchantment>()) wrath.UsedThisCombat = false;
    }
}

[RegisterEnchantment]
public sealed class FamiliarEnchantment : CombatOnlyEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => EnchantmentIconAssets.For("familiar_enchantment_v1");
    public override bool HasExtraCardText => true;

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player
            && Card.Owner.Creature.Side == side
            && Card.Pile?.Type == PileType.Hand)
        {
            await CardCmd.AutoPlay(choiceContext, Card, null);
        }
    }
}
