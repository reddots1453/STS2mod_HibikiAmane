using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class BlasphemousDesire : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(3),
            SecondaryResourceVars.ForLocal(
                "Desire", MaidenSuccubusMod.ModId, DesireResource.LocalId, 5)];

    public BlasphemousDesire()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        decimal loss = DynamicVars["StrengthPower"].BaseValue;
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            -loss,
            Owner.Creature,
            this);
        await PowerCmd.Apply<RestoreStrengthAtTurnEndPower>(
            choiceContext,
            Owner.Creature,
            loss,
            Owner.Creature,
            this);
        await Data.Desire.Modify(Owner, DynamicVars["Desire"].IntValue);
    }

    protected override void OnUpgrade() =>
        DynamicVars["Desire"].UpgradeValueBy(2);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class FleetingYears : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2), new EnergyVar(2)];

    public FleetingYears()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        this.SecondaryCosts().Set(DesireResource.Id, 2);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class BurningBladeRitual : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(10, ValueProp.Move)];

    public BurningBladeRitual()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        CardModel? selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            card => card != this,
            this)).FirstOrDefault();
        if (selected == null)
        {
            return;
        }
        await CardCmd.Exhaust(choiceContext, selected);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class SacrificialFrenzy : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12, ValueProp.Move),
        new CardsVar(3),
        new DynamicVar("BonusDamage", 6),
    ];

    public SacrificialFrenzy()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
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

        CardModel[] cards = PileType.Draw.GetPile(Owner).Cards
            .Take(DynamicVars.Cards.IntValue)
            .ToArray();
        foreach (CardModel card in cards)
        {
            await CardCmd.Exhaust(choiceContext, card);
            if (card.Type == CardType.Attack && !cardPlay.Target.IsDead)
            {
                await DamageCmd.Attack(DynamicVars["BonusDamage"].BaseValue)
                    .FromCard(this, cardPlay)
                    .Targeting(cardPlay.Target)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}

public sealed class CursedTomb : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Ethereal];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];

    public CursedTomb()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        CardModel[] selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            PileType.Draw.GetPile(Owner).Cards,
            Owner,
            new CardSelectorPrefs(
                SelectionScreenPrompt,
                DynamicVars.Cards.IntValue))).ToArray();
        foreach (CardModel card in selected)
        {
            card.AddKeyword(CardKeyword.Ethereal);
        }
        foreach (CardModel card in selected)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class PriceOfStrength : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(4),
        new PowerVar<ShatterPower>(4),
    ];

    public PriceOfStrength()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["StrengthPower"].BaseValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<ShatterPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["ShatterPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class Coronation : MSCorruptCard
{
    public Coronation()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        this.SecondaryCosts().Set(DesireResource.Id, 1);
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            CorruptionQuery.Get((RunState)Owner.RunState),
            Owner.Creature,
            this);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class BurningDesire : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(3, ValueProp.Move)];

    public BurningDesire()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        this.SecondaryCosts().Set(DesireResource.Id, 1);
    }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int hits = Math.Max(1, Data.Desire.Get(Owner) + 1);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class FearAura : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<FearAuraStrengthLossPower>(3)];

    public FearAura()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        decimal loss = DynamicVars["FearAuraStrengthLossPower"].BaseValue;
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<FearAuraStrengthLossPower>(
                choiceContext,
                enemy,
                loss,
                Owner.Creature,
                this);
        }
        if (await TransformationCmd.PayOverdraft(
                choiceContext, Owner.Creature, this))
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext, Owner.Creature, loss, Owner.Creature, this);
            await PowerCmd.Apply<RestoreStrengthAtTurnEndPower>(
                choiceContext, Owner.Creature, -loss, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars["FearAuraStrengthLossPower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class TentacleArmor : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PlatingPower>(4)];

    public TentacleArmor()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await PowerCmd.Apply<PlatingPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["PlatingPower"].BaseValue,
            Owner.Creature,
            this);
        CardModel copy = CombatState!.CloneCard(this);
        copy.DeckVersion = null;
        await CardPileCmd.Add(copy, PileType.Draw, CardPilePosition.Random);
    }

    protected override void OnUpgrade() =>
        DynamicVars["PlatingPower"].UpgradeValueBy(2);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class SmallFry : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VulnerablePower>(3), new PowerVar<StrengthPower>(1)];

    public SmallFry()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["VulnerablePower"].BaseValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["StrengthPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() =>
        DynamicVars["VulnerablePower"].UpgradeValueBy(1);
}
