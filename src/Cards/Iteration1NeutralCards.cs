using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

internal static class IterationCardEffects
{
    public static Task Attack(
        CardModel card,
        PlayerChoiceContext context,
        CardPlay play,
        decimal damage,
        int hits = 1)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        return DamageCmd.Attack(damage)
            .WithHitCount(hits)
            .FromCard(card, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(context);
    }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class RepairAlyssa : MSNeutralCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(4, ValueProp.Move), new DynamicVar("Armor", 1)];

    public RepairAlyssa()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await TransformationCmd.GainArmor(
            context, Owner.Creature, DynamicVars["Armor"].IntValue, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

public abstract class HandDiscountCard : MSNeutralCard
{
    protected HandDiscountCard(CardType type, CardRarity rarity, TargetType target)
        : base(6, type, rarity, target) { }

    public override bool TryModifyEnergyCostInCombat(
        CardModel card, decimal original, out decimal modified)
    {
        modified = original;
        if (!ReferenceEquals(card, this))
        {
            return false;
        }

        int otherCardsInHand = PileType.Hand
            .GetPile(Owner)
            .Cards
            .Count(handCard => !ReferenceEquals(handCard, this));
        modified = Math.Max(0, original - otherCardsInHand);
        return true;
    }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class BurningBracelet : HandDiscountCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(14, ValueProp.Move)];

    public BurningBracelet()
        : base(CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        IterationCardEffects.Attack(this, context, play, DynamicVars.Damage.BaseValue);

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class FrozenBracelet : HandDiscountCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(12, ValueProp.Move)];

    public FrozenBracelet()
        : base(CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class MagiciansSecret : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Amplification", 1)];

    public MagiciansSecret()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<MagicAmplificationPower>(
            context,
            Owner.Creature,
            DynamicVars["Amplification"].BaseValue,
            Owner.Creature,
            this);
        if (await TransformationCmd.PayOverdraft(context, Owner.Creature, this))
        {
            await PowerCmd.Apply<MagicAmplificationPower>(
                context,
                Owner.Creature,
                DynamicVars["Amplification"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars["Amplification"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class CounterBarrier : MSNeutralCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<CounterBarrierII>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ThornsPower>(2), new PowerVar<PlatingPower>(2)];
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [SinkingKeyword.Value];

    public CounterBarrier()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override Task BeforeCombatStart() =>
        Pile?.Type == PileType.Draw
            ? CardPileCmd.Add(this, PileType.Discard)
            : Task.CompletedTask;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<ThornsPower>(context, Owner.Creature,
            DynamicVars["ThornsPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<PlatingPower>(context, Owner.Creature,
            DynamicVars["PlatingPower"].BaseValue, Owner.Creature, this);
        CardModel next = CombatState!.CreateCard(
            ModelDb.Card<CounterBarrierII>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(next, PileType.Discard, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public abstract class CounterBarrierToken<TNext> : MSGeneratedCard
    where TNext : CardModel
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<TNext>()];
    protected abstract int BaseThorns { get; }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ThornsPower>(BaseThorns), new PowerVar<PlatingPower>(BaseThorns)];

    protected CounterBarrierToken(int cost)
        : base(cost, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<ThornsPower>(context, Owner.Creature,
            DynamicVars["ThornsPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<PlatingPower>(context, Owner.Creature,
            DynamicVars["PlatingPower"].BaseValue, Owner.Creature, this);
        CardModel next = CombatState!.CreateCard(ModelDb.Card<TNext>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(next, PileType.Discard, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class CounterBarrierII : CounterBarrierToken<CounterBarrierIII>
{
    protected override int BaseThorns => 3;
    public CounterBarrierII() : base(1) { }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class CounterBarrierIII : CounterBarrierToken<CounterBarrierIV>
{
    protected override int BaseThorns => 5;
    public CounterBarrierIII() : base(1) { }
}

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class CounterBarrierIV : MSGeneratedCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ThornsPower>(30), new PowerVar<PlatingPower>(30)];

    public CounterBarrierIV()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<ThornsPower>(context, Owner.Creature,
            DynamicVars["ThornsPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<PlatingPower>(context, Owner.Creature,
            DynamicVars["PlatingPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class StudyPlan : MSNeutralCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(7, ValueProp.Move)];

    public StudyPlan()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<DrawCardsNextTurnPower>(
            context, Owner.Creature, 2, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class Procrastinate : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public Procrastinate()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel? selected = (await CardSelectCmd.FromHand(
            context,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            card => card != this,
            this)).FirstOrDefault();
        if (selected != null)
        {
            await CardPileCmd.Add(
                selected, PileType.Draw, CardPilePosition.Bottom);
        }
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class Bath : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(2), new EnergyVar("NextEnergy", 2)];

    public Bath()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        CardModel[] curses = Owner.Deck.Cards
            .OfType<MSInvasionCurseTemplate>()
            .ToArray();
        foreach (CardModel curse in curses)
        {
            await CardPileCmd.RemoveFromDeck(curse);
        }

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        if (await TransformationCmd.PayOverdraft(
            context, Owner.Creature, this))
        {
            await PowerCmd.Apply<EnergyNextTurnPower>(
                context,
                Owner.Creature,
                DynamicVars["NextEnergy"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars["NextEnergy"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class MagicResonance : MSNeutralCard
{
    // Compatibility identity only; do not offer this retired card in new content.
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<MagicResonancePower>(3)];

    public MagicResonance()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self,
            shouldShowInCardLibrary: false) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<MagicResonancePower>(
            context,
            Owner.Creature,
            DynamicVars["MagicResonancePower"].BaseValue,
            Owner.Creature,
            this);

    protected override void OnUpgrade() =>
        DynamicVars["MagicResonancePower"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class GoddessOfIce : MSNeutralCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<IceShard>(IsUpgraded)];

    public GoddessOfIce()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<GoddessOfIcePower>(
            context, Owner.Creature, IsUpgraded ? 2 : 1, Owner.Creature, this);

    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class BindingInsight : MSNeutralCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [PortableKeyword.Value];

    public BindingInsight()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<BindingInsightPower>(
            context, Owner.Creature, 1, Owner.Creature, this);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
