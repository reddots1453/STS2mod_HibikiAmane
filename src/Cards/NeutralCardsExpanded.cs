using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Powers;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class BalanceShield : MSNeutralCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(14),
        new CalculationExtraVar(-3),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(
            static (card, _) => Math.Abs(
                CorruptionQuery.Get((RunState)card.Owner.RunState))),
    ];

    public BalanceShield()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        return CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.CalculatedBlock.Calculate(null),
            DynamicVars.CalculatedBlock.Props,
            cardPlay);
    }

    protected override void OnUpgrade() =>
        DynamicVars.CalculationBase.UpgradeValueBy(4);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class BorrowedForceStrike : MSNeutralCard
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9, ValueProp.Move), new EnergyVar(1)];

    public BorrowedForceStrike()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        if (cardPlay.Target.Monster?.NextMove.Intents
                .Any(intent => intent is AttackIntent) == true)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class SteadyGuard : MSNeutralCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(13),
        new CalculationExtraVar(4),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(
            static (card, _) => CombatManager.Instance.History.Entries
                .OfType<DamageReceivedEntry>()
                .Any(entry =>
                    entry.Receiver == card.Owner.Creature
                    && !entry.Result.WasFullyBlocked
                    && entry.HappenedLastPlayerTurn(card.Owner))
                    ? 0
                    : 1),
    ];

    public SteadyGuard()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.CalculatedBlock.Calculate(null),
            DynamicVars.CalculatedBlock.Props,
            cardPlay);
        if (!CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Any(entry =>
                entry.Receiver == Owner.Creature
                && !entry.Result.WasFullyBlocked
                && entry.HappenedLastPlayerTurn(Owner)))
        {
            await TransformationCmd.GainArmor(
                choiceContext, Owner.Creature, 1, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(4);
    }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class JudgmentBlade : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(7),
        new ExtraDamageVar(3),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            static (_, target) => target is null ? 0 : PowerLayerQuery.CountDebuffLayers(target)),
    ];

    public JudgmentBlade()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        return DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() =>
        DynamicVars.ExtraDamage.UpgradeValueBy(2);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class HealingArt : MSNeutralCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HealVar(4), new DynamicVar("PerBuff", 2)];

    public HealingArt()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        decimal healing = DynamicVars.Heal.BaseValue
            + PowerLayerQuery.CountBuffLayers(Owner.Creature)
            * DynamicVars["PerBuff"].BaseValue;
        return CreatureCmd.Heal(Owner.Creature, healing);
    }

    protected override void OnUpgrade() => DynamicVars.Heal.UpgradeValueBy(4);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class BasicTraining : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("BasicTrainingPower", 3)];

    public BasicTraining()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        PowerCmd.Apply<BasicTrainingPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["BasicTrainingPower"].BaseValue,
            Owner.Creature,
            this);

    protected override void OnUpgrade() =>
        DynamicVars["BasicTrainingPower"].UpgradeValueBy(2);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class Fusion : MSNeutralCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public Fusion()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        List<CardModel> choices = [];
        AddOneFromPool<MSCorruptCardPool>(choices);
        AddOneFromPool<MSHolyCardPool>(choices);
        if (IsUpgraded)
        {
            foreach (CardModel choice in choices.Where(card => card.IsUpgradable))
                CardCmd.Upgrade(choice);
        }

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices,
            Owner,
            canSkip: false);
        if (selected == null)
            return;

        GeneratedCardCostCmd.SetFreeThisTurn(selected);
        await CardPileCmd.AddGeneratedCardToCombat(
            selected,
            PileType.Hand,
            Owner);
    }

    protected override void OnUpgrade()
    {
    }

    private void AddOneFromPool<TPool>(ICollection<CardModel> choices)
        where TPool : CardPoolModel
    {
        IEnumerable<CardModel> candidates = ModelDb.CardPool<TPool>()
            .GetUnlockedCards(
                Owner.UnlockState,
                Owner.RunState.CardMultiplayerConstraint)
            .Where(card => card.GetType() != GetType());
        CardModel? generated = CardFactory.GetDistinctForCombat(
                Owner,
                candidates,
                1,
                Owner.RunState.Rng.CombatCardGeneration)
            .FirstOrDefault();
        if (generated != null)
        {
            choices.Add(generated);
        }
    }
}
