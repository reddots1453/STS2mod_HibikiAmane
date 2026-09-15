using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Powers;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class EcstasyDew : MSCorruptCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [SecondaryResourceVars.ForLocal(
            "Desire", MaidenSuccubusMod.ModId, DesireResource.LocalId, 2),
            new CardsVar(1)];
    public EcstasyDew() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Data.Desire.Modify(Owner, DynamicVars["Desire"].IntValue);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars["Desire"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class MiasmaAbsorption : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];
    public MiasmaAbsorption() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        => this.SecondaryCosts().Set(DesireResource.Id, 2);
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class LastStand : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(3),
        new ExtraDamageVar(3),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            static (card, _) => PowerLayerQuery.CountDebuffLayers(card.Owner.Creature)),
    ];
    public LastStand() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        return DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this, play).Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(context);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(1);
        DynamicVars.ExtraDamage.UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class LightningKick : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(10, ValueProp.Move), new PowerVar<ShatterPower>(4)];
    public LightningKick() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_blunt").Execute(context);
        await PowerCmd.Apply<ShatterPower>(context, play.Target,
            DynamicVars["ShatterPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["ShatterPower"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class CalmingMist : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1), new PowerVar<WeakPower>(2)];
    public CalmingMist() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
        await PowerCmd.Apply<WeakPower>(context, play.Target,
            DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class SoulImpact : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(12, ValueProp.Move), new EnergyVar(2)];
    public SoulImpact() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        bool debuffed = PowerLayerQuery.CountDebuffLayers(play.Target) > 0;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_blunt").Execute(context);
        if (debuffed) await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class MagicBurst : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(7),
        new ExtraDamageVar(2),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            static (card, _) =>
                TransformationCmd.GetAmplification(card.Owner.Creature)?.Amount > 0
                || TransformationCmd.GetArmor(card.Owner.Creature)?.Amount > 0
                    ? PowerLayerQuery.CountBuffLayers(card.Owner.Creature)
                    : 0),
    ];
    public MagicBurst() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        bool overdrafted = await TransformationCmd.PayOverdraft(
            context, Owner.Creature, this);
        decimal damage = DynamicVars.CalculationBase.BaseValue;
        if (overdrafted)
        {
            damage += PowerLayerQuery.CountBuffLayers(Owner.Creature)
                * DynamicVars.ExtraDamage.BaseValue;
        }
        await DamageCmd.Attack(damage).FromCard(this, play).Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class DreamMist : MSNeutralCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WeakPower>(2), new PowerVar<VulnerablePower>(2)];
    public DreamMist() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<WeakPower>(context, Owner.Creature,
            DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(context, Owner.Creature,
            DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this);
        if (CombatState != null)
        {
            foreach (var enemy in CombatState.HittableEnemies.ToArray())
                await PowerCmd.Apply<WeakPower>(context, enemy,
                    DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
            foreach (var enemy in CombatState.HittableEnemies.ToArray())
                await PowerCmd.Apply<VulnerablePower>(context, enemy,
                    DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade()
    {
        DynamicVars["WeakPower"].UpgradeValueBy(1);
        DynamicVars["VulnerablePower"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class CycloneRupture : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5, ValueProp.Move), new PowerVar<ShatterPower>(1)];
    public CycloneRupture() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        await PowerCmd.Apply<ShatterPower>(context, play.Target,
            DynamicVars["ShatterPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() =>
        DynamicVars["ShatterPower"].UpgradeValueBy(1);
}
