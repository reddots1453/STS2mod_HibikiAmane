using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Core.Transformation;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSGeneratedCardPool))]
public sealed class DrowsyStatus : MSGeneratedCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Retain];
    public DrowsyStatus() : base(0, CardType.Status, CardRarity.Common, TargetType.None) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class MentalUnity : MSNeutralCard
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6, ValueProp.Move),
            new BlockVar(2, ValueProp.Unpowered | ValueProp.Move)];
    public MentalUnity() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        if (play.Target.IsAlive)
        {
            decimal block = DynamicVars.Block.BaseValue;
            if (Enchantment is { } enchantment)
            {
                block += enchantment.EnchantBlockAdditive(block);
                block *= enchantment.EnchantBlockMultiplicative(block);
            }
            await PowerCmd.Apply<MentalUnityPower>(
                context, play.Target, block, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars.Block.UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class ObstructingShot : MSNeutralCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [StunIntent.GetStaticHoverTip()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3, ValueProp.Move)];
    public ObstructingShot() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        bool shouldStun = play.Target.Monster?.IntendsToAttack == false;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        if (shouldStun && play.Target.IsAlive) await IntentMoveFactory.Stun(play.Target);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class CounterDefense : MSNeutralCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(6, ValueProp.Move), new PowerVar<CounterDefensePower>(6)];
    public CounterDefense() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<CounterDefensePower>(context, Owner.Creature,
            DynamicVars["CounterDefensePower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
        DynamicVars["CounterDefensePower"].UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class MindsEye : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(3, ValueProp.Move), new PowerVar<WeakPower>(1), new PowerVar<VulnerablePower>(1)];
    public MindsEye() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        bool attacking = play.Target.Monster?.IntendsToAttack == true;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        if (!play.Target.IsAlive) return;
        if (attacking)
            await PowerCmd.Apply<WeakPower>(context, play.Target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
        else
            await PowerCmd.Apply<VulnerablePower>(context, play.Target, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars["WeakPower"].UpgradeValueBy(1);
        DynamicVars["VulnerablePower"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class ExplosiveImpact : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(3, ValueProp.Move), new RepeatVar(2), new PowerVar<ShatterPower>(2)];
    public ExplosiveImpact() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(2).FromCard(this, play)
            .TargetingRandomOpponents(CombatState).WithHitFx("vfx/vfx_attack_blunt").Execute(context);
        foreach (var enemy in attack.Results.SelectMany(group => group).Select(result => result.Receiver)
                     .Distinct().Where(enemy => enemy.IsAlive))
            await PowerCmd.Apply<ShatterPower>(context, enemy, 2, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class UltimateFlare : MSNeutralCard
{
    public override bool HasTurnEndInHandEffect => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(40, ValueProp.Move)];
    public UltimateFlare() : base(4, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(context);
    }

    protected override Task OnTurnEndInHand(PlayerChoiceContext context)
    {
        EnergyCost.AddThisCombat(-1);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(12);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class MagicIndex : MSNeutralCard
{
    public MagicIndex() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<MagicIndexPower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class ResonanceArmor : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ResonanceArmorPower>(3)];
    public ResonanceArmor() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<ResonanceArmorPower>(context, Owner.Creature,
            DynamicVars["ResonanceArmorPower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["ResonanceArmorPower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class Lullaby : MSNeutralCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.FromCard<DrowsyStatus>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BlockPerCard", 2)];
    public Lullaby() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<LullabyPower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class TenaciousResistance : MSNeutralCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2, ValueProp.Move)];
    public TenaciousResistance() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        decimal growth = Owner.Creature.GetPower<TenaciousResistancePower>()?.Amount ?? 0;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue + growth, DynamicVars.Block.Props, play);
        await PowerCmd.Apply<TenaciousResistancePower>(context, Owner.Creature, 2, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2);
}
