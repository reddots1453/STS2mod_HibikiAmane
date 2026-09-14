using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Data;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Core.Invasion;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Core.Temptation;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Cards.Curses;

public abstract class MSInvasionCurseTemplate :
    ModCardTemplate,
    IInvasionSourcedCurse
{
    [SavedProperty]
    public string SourceMonsterId { get; set; } = "";

    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedByModifiers => false;
    public override CardPoolModel Pool => ModelDb.CardPool<MSInvasionCursePool>();
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/packed/card_portraits/ironclad/bash.png");

    protected virtual IEnumerable<IHoverTip> CardSpecificHoverTips => [];
    protected sealed override IEnumerable<IHoverTip> AdditionalHoverTips =>
        CardHoverTipSupport.FromDynamicPowerVars(DynamicVars.Values)
            .Concat(CardHoverTipSupport.FromDescriptionReferences(this))
            .Concat(CardSpecificHoverTips);

    protected MSInvasionCurseTemplate(int cost = 1)
        : base(cost, CardType.Curse, CardRarity.Curse, TargetType.None, true) { }

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation cardLocation) =>
        ReferenceEquals(card, this)
            ? new CardLocation(
                cardLocation.player,
                PileType.None,
                CardPilePosition.Bottom)
            : cardLocation;

    protected sealed override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay play)
    {
        await ResolveEffect(context);

        CardModel? deckCard = DeckVersion;
        if (deckCard is
            {
                HasBeenRemovedFromState: false,
                Pile.Type: PileType.Deck,
            })
        {
            await CardPileCmd.RemoveFromDeck(deckCard, showPreview: false);
        }
    }

    protected virtual Task ResolveEffect(PlayerChoiceContext context) =>
        Task.CompletedTask;

    protected async Task GenerateIntoDraw<T>() where T : CardModel
    {
        T generated = CombatState!.CreateCard<T>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            generated,
            PileType.Draw,
            Owner,
            CardPilePosition.Random);
    }
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class SemenCurse : MSInvasionCurseTemplate;

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class FoulSlimeCurse : MSInvasionCurseTemplate
{
    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        PowerCmd.Apply<VulnerablePower>(
            context, Owner.Creature, 1, Owner.Creature, this);
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class AphrodisiacCurse : MSInvasionCurseTemplate
{
    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        Desire.Modify(Owner, 2);
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class SporeMucusCurse : MSInvasionCurseTemplate
{
    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        PowerCmd.Apply<WeakPower>(
            context, Owner.Creature, 1, Owner.Creature, this);
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class ParalyticSlimeCurse : MSInvasionCurseTemplate
{
    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        PowerCmd.Apply<FrailPower>(
            context, Owner.Creature, 1, Owner.Creature, this);
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class CorrosiveSlimeCurse : MSInvasionCurseTemplate
{
    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        TransformationCmd.LoseArmor(context, Owner.Creature, 1, this);
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class InsectEggCurse : MSInvasionCurseTemplate
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<ArousalStatus>()];

    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        GenerateIntoDraw<ArousalStatus>();
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class ParasiticEggCurse : MSInvasionCurseTemplate
{
    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        CreatureCmd.Damage(
            context,
            Owner.Creature,
            3,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            Owner.Creature,
            this,
            null);
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class InkFluidCurse : MSInvasionCurseTemplate
{
    protected override Task ResolveEffect(PlayerChoiceContext context)
    {
        IReadOnlyList<CardModel> hand = PileType.Hand.GetPile(Owner).Cards;
        if (hand.Count == 0)
        {
            return Task.CompletedTask;
        }
        CardModel selected = hand[
            Owner.RunState.Rng.CombatCardSelection.NextInt(hand.Count)];
        CardCmd.ApplyKeyword(selected, CardKeyword.Ethereal);
        return Task.CompletedTask;
    }
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class ScorchingFluidCurse : MSInvasionCurseTemplate
{
    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        PowerCmd.Apply<BurningPower>(
            context, Owner.Creature, 2, Owner.Creature, this);
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class EctoplasmResidueCurse : MSInvasionCurseTemplate
{
    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        PowerCmd.Apply<EctoplasmResidueEnergyLossPower>(
            context, Owner.Creature, 1, Owner.Creature, this);
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class MagicResidueCurse : MSInvasionCurseTemplate
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<Slimed>()];

    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        GenerateIntoDraw<Slimed>();
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class VineSeedCurse : MSInvasionCurseTemplate
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<SporeMind>()];

    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        GenerateIntoDraw<SporeMind>();
}
[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class SludgeSemenCurse : MSInvasionCurseTemplate
{
    public SludgeSemenCurse() : base(2) { }
}
[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class DeepSeaSlimeCurse : MSInvasionCurseTemplate
{
    protected override async Task ResolveEffect(PlayerChoiceContext context)
    {
        MagicArmorPower? armor = TransformationCmd.GetArmor(Owner.Creature);
        if (armor != null)
        {
            await PowerCmd.Decrement(armor);
        }
    }
}

[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class ExperimentalLiquidCurse : MSInvasionCurseTemplate
{
    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        Owner.RunState.Rng.CombatCardGeneration.NextInt(5) switch
        {
            0 => PowerCmd.Apply<WeakPower>(
                context, Owner.Creature, 1, Owner.Creature, this),
            1 => PowerCmd.Apply<FrailPower>(
                context, Owner.Creature, 1, Owner.Creature, this),
            2 => PowerCmd.Apply<VulnerablePower>(
                context, Owner.Creature, 1, Owner.Creature, this),
            3 => PowerCmd.Apply<StrengthPower>(
                context, Owner.Creature, 1, Owner.Creature, this),
            _ => PowerCmd.Apply<DexterityPower>(
                context, Owner.Creature, 1, Owner.Creature, this),
        };
}
[RegisterCard(typeof(MSInvasionCursePool))]
public sealed class RoyalEssenceCurse : MSInvasionCurseTemplate
{
    public RoyalEssenceCurse() : base(2) { }

    protected override Task ResolveEffect(PlayerChoiceContext context) =>
        CreatureCmd.Heal(Owner.Creature, 5);
}

public abstract class MSEventCurseTemplate : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedByModifiers => false;
    public override CardPoolModel Pool => ModelDb.CardPool<MSGeneratedCardPool>();
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/packed/card_portraits/ironclad/bash.png");
    protected virtual IEnumerable<IHoverTip> CardSpecificHoverTips => [];
    protected sealed override IEnumerable<IHoverTip> AdditionalHoverTips =>
        CardHoverTipSupport.FromDynamicPowerVars(DynamicVars.Values)
            .Concat(CardHoverTipSupport.FromDescriptionReferences(this))
            .Concat(CardSpecificHoverTips);
    protected MSEventCurseTemplate(int cost = -1)
        : base(cost, CardType.Curse, CardRarity.Curse, TargetType.None, true) { }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class LewdMarkMinorCurse : MSEventCurseTemplate
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<ArousalStatus>()];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    public override bool HasTurnEndInHandEffect => true;

    protected override Task OnTurnEndInHand(PlayerChoiceContext context) =>
        GenerateArousal(1);

    private async Task GenerateArousal(int count)
    {
        for (int i = 0; i < count; i++)
        {
            ArousalStatus generated = CombatState!.CreateCard<ArousalStatus>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(
                generated, PileType.Draw, Owner, CardPilePosition.Random);
        }
    }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class LewdMarkSpreadCurse : MSEventCurseTemplate
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<ArousalStatus>()];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext context)
    {
        for (int i = 0; i < 2; i++)
        {
            ArousalStatus generated = CombatState!.CreateCard<ArousalStatus>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(
                generated, PileType.Draw, Owner, CardPilePosition.Random);
        }
    }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class LewdMarkCompleteCurse : MSEventCurseTemplate
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<ArousalStatus>()];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Retain];
    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext context)
    {
        for (int i = 0; i < 2; i++)
        {
            ArousalStatus generated = CombatState!.CreateCard<ArousalStatus>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(
                generated, PileType.Draw, Owner, CardPilePosition.Random);
        }
    }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class TransparentOutfitCurse : MSEventCurseTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Unplayable, CardKeyword.Retain];

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? source)
    {
        if (ReferenceEquals(card, this)
            && Owner.Creature.CombatState != null
            && (Pile?.Type == PileType.Hand || oldPileType == PileType.Hand))
        {
            Temptation.NotifyHandChanged(Owner);
        }
        return Task.CompletedTask;
    }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class InfatuationCurse : MSEventCurseTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public InfatuationCurse() : base(2) { }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        card.Owner != Owner
        || Pile?.Type != PileType.Hand
        || ReferenceEquals(card, this)
        || card.Type != CardType.Attack;
}
[RegisterCard(typeof(MSGeneratedCardPool))] public sealed class HypnosisCurse : MSEventCurseTemplate;
[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class GagCurse : MSEventCurseTemplate
{
    public GagCurse() : base(2) { }

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner != Owner
            || Pile?.Type != PileType.Hand
            || card.Type != CardType.Skill)
        {
            return false;
        }
        modifiedCost = originalCost + 1;
        return true;
    }
}
[RegisterCard(typeof(MSGeneratedCardPool))] public sealed class ClimaxBanCurse : MSEventCurseTemplate;
