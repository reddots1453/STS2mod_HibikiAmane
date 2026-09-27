using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class MiasmaFlame : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7, ValueProp.Move), new PowerVar<BurningPower>(3)];

    public MiasmaFlame() : base(0, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
        => this.SecondaryCosts().Set(DesireResource.Id, 2);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        Creature[] enemies = CombatState.HittableEnemies.ToArray();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .TargetingAllOpponents(CombatState).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        foreach (Creature enemy in enemies.Where(enemy => enemy.IsAlive))
            await PowerCmd.Apply<BurningPower>(context, enemy, DynamicVars["BurningPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class PlayingWithFire : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BurningPower>(1), new CardsVar(2)];
    public PlayingWithFire() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<BurningPower>(context, Owner.Creature, 1, Owner.Creature, this);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

public sealed class ScorchingMagic : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ScorchingMagicPower>(3)];
    public ScorchingMagic() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<ScorchingMagicPower>(context, Owner.Creature,
            DynamicVars["ScorchingMagicPower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["ScorchingMagicPower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class FlameBloom : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8, ValueProp.Move), new PowerVar<BurningPower>(1)];
    public FlameBloom() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        await PowerCmd.Apply<BurningPower>(
            context, play.Target, DynamicVars["BurningPower"].BaseValue, Owner.Creature, this);
        if (await Core.Transformation.TransformationCmd.PayOverdraft(
                context, Owner.Creature, this))
        {
            await PowerCmd.Apply<BurningPower>(
                context, play.Target, 1, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}
