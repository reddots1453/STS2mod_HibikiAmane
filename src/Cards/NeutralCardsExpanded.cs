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
using MaidenSuccubus.Pools;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

public sealed class Impermanence : MSNeutralCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4, ValueProp.Move),
        new BlockVar(4, ValueProp.Move),
        new DynamicVar("Scaling", 3),
    ];

    public Impermanence()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int corruption = CorruptionQuery.Get((RunState)Owner.RunState);
        decimal damage = DynamicVars.Damage.BaseValue
            + Math.Max(0, corruption) * DynamicVars["Scaling"].BaseValue;
        decimal block = DynamicVars.Block.BaseValue
            + Math.Max(0, -corruption) * DynamicVars["Scaling"].BaseValue;
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await CreatureCmd.GainBlock(
            Owner.Creature,
            block,
            ValueProp.Move,
            cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1);
        DynamicVars.Block.UpgradeValueBy(1);
        DynamicVars["Scaling"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class BalanceShield : MSNeutralCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(14, ValueProp.Move), new DynamicVar("Penalty", 3)];

    public BalanceShield()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        decimal block = Math.Max(
            0,
            DynamicVars.Block.BaseValue
                - Math.Abs(CorruptionQuery.Get((RunState)Owner.RunState))
                * DynamicVars["Penalty"].BaseValue);
        return CreatureCmd.GainBlock(
            Owner.Creature,
            block,
            ValueProp.Move,
            cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4);
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
        [new BlockVar(13, ValueProp.Move), new DynamicVar("BonusBlock", 5)];

    public SteadyGuard()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        bool lostHpLastTurn = CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Any(entry =>
                entry.Receiver == Owner.Creature
                && !entry.Result.WasFullyBlocked
                && entry.HappenedLastPlayerTurn(Owner));
        decimal block = DynamicVars.Block.BaseValue
            + (lostHpLastTurn ? 0 : DynamicVars["BonusBlock"].BaseValue);
        await CreatureCmd.GainBlock(
            Owner.Creature,
            block,
            ValueProp.Move,
            cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4);
    }
}

public sealed class CloseQuartersBlade : MSNeutralCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [MaidenSuccubus.Keywords.PortableKeyword.Value];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(10, ValueProp.Move)];

    public CloseQuartersBlade()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4);
}

public sealed class LubricatingOil : MSNeutralCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [MaidenSuccubus.Keywords.PortableKeyword.Value, CardKeyword.Exhaust];

    public LubricatingOil()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await ControlCmd.Release(choiceContext, Owner.Creature);
        await Data.Desire.Modify(Owner, 1);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class JudgmentBlade : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7, ValueProp.Move), new DynamicVar("PerDebuff", 5)];

    public JudgmentBlade()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        decimal damage = DynamicVars.Damage.BaseValue
            + PowerLayerQuery.CountDebuffLayers(cardPlay.Target)
            * DynamicVars["PerDebuff"].BaseValue;
        return DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() =>
        DynamicVars["PerDebuff"].UpgradeValueBy(2);
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
        AddOneFromPool<MSNeutralCardPool>(choices);
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

        selected.EnergyCost.SetThisTurnOrUntilPlayed(0);
        selected.SecondaryCosts().Set(DesireResource.Id, 0);
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
