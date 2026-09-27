using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Core.Transformation;
using STS2RitsuLib.Interop.AutoRegistration;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Routes;
using MegaCrit.Sts2.Core.Runs;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class MaidenStrike : MSNeutralCard
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6, ValueProp.Move)];

    public MaidenStrike()
        : base(
            1,
            CardType.Attack,
            CardRarity.Basic,
            TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class MaidenDefend : MSNeutralCard
{
    public override bool GainsBlock => true;
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(5, ValueProp.Move)];

    public MaidenDefend()
        : base(
            1,
            CardType.Skill,
            CardRarity.Basic,
            TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class Transform : MSHolyCard
{
    public override RouteCardKind RouteKind =>
        IsMutable
        && Owner?.RunState is RunState runState
        && CorruptionQuery.Get(runState) >= 3
            ? RouteCardKind.Corrupt
            : RouteCardKind.Holy;

    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [CardHoverTipSupport.Static("MAIDENSUCCUBUS_VARIATION")];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public Transform() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        card != this || (RouteKind == RouteCardKind.Corrupt
            ? !Owner.Creature.HasPower<CorruptRobePower>()
            : !Owner.Creature.HasPower<ImmaculateRobePower>());

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add(
            "IsCorrupt",
            IsMutable
                && Owner?.RunState is RunState runState
                && CorruptionQuery.Get(runState) >= 3);

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Owner.RunState is RunState runState && CorruptionQuery.Get(runState) >= 3
            ? TransformationCmd.EnterCorruptRobe(choiceContext, Owner.Creature, this)
            : TransformationCmd.EnterImmaculateRobe(choiceContext, Owner.Creature, this);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}

[RegisterArchaicToothTranscendence(typeof(DarkOrigin))]
[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class DarkElement : DarkElementBase
{
    public DarkElement() : base(CardRarity.Basic, 4, 2) { }
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class DarkOrigin : DarkElementBase
{
    public DarkOrigin() : base(CardRarity.Ancient, 8, 4) { }
}

public abstract class DarkElementBase : MSCorruptCard
{
    private readonly int _baseAmount;
    private readonly int _upgradeAmount;
    private bool IsHolyVariation =>
        IsMutable
        && Owner?.RunState is RunState runState
        && CorruptionQuery.Get(runState) <= -3;

    public override RouteCardKind RouteKind =>
        IsHolyVariation
            ? RouteCardKind.Holy
            : RouteCardKind.Corrupt;

    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [CardHoverTipSupport.Static("MAIDENSUCCUBUS_VARIATION")];

    public override bool GainsBlock => IsHolyVariation;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(_baseAmount, ValueProp.Move), new BlockVar(_baseAmount, ValueProp.Move)];

    protected DarkElementBase(CardRarity rarity, int baseAmount, int upgradeAmount)
        : base(0, CardType.Attack, rarity, TargetType.AnyEnemy)
    {
        _baseAmount = baseAmount;
        _upgradeAmount = upgradeAmount;
    }

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add(
            "IsCorrupt",
            !IsHolyVariation);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        if (!await Core.Transformation.TransformationCmd.PayOverdraft(
                choiceContext, Owner.Creature, this))
        {
            return;
        }
        if (!IsHolyVariation)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(_upgradeAmount);
        DynamicVars.Block.UpgradeValueBy(_upgradeAmount);
    }
}
