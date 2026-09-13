using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class ForgeStrike : MSNeutralCard
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<Glam>();
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6, ValueProp.Move)];

    public ForgeStrike()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await IterationCardEffects.Attack(
            this, context, play, DynamicVars.Damage.BaseValue);
        CardModel? target = (await CardSelectCmd.FromHand(
            context,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1),
            card => card != this
                && card.Tags.Contains(CardTag.Strike)
                && card.Enchantment == null,
            this)).FirstOrDefault();
        if (target != null)
        {
            CombatEnchantmentCmd.ApplyVanilla<Glam>(target, 1);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class SummonThunder : MSNeutralCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Fatal)];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7, ValueProp.Move)];

    public SummonThunder()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        bool amplified = TransformationCmd.GetAmplification(Owner.Creature)
            ?.IsAmplifying(this) == true;
        bool initialFatalEligible = play.Target.Powers.All(
            power => power.ShouldOwnerDeathTriggerFatal());
        var initial = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(context);
        int pending = (initialFatalEligible
                ? initial.Results.SelectMany(group => group)
                    .Count(result => result.WasTargetKilled)
                : 0)
            + (amplified ? 1 : 0);
        while (pending-- > 0 && CombatState?.HittableEnemies.Count > 0)
        {
            var enemies = CombatState.HittableEnemies;
            var target = enemies[
                Owner.RunState.Rng.CombatTargets.NextInt(enemies.Count)];
            bool fatalEligible = target.Powers.All(
                power => power.ShouldOwnerDeathTriggerFatal());
            var followUp = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, play)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(context);
            if (fatalEligible)
            {
                pending += followUp.Results.SelectMany(group => group)
                    .Count(result => result.WasTargetKilled);
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class MagicStarBomb : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(20, ValueProp.Move)];

    public MagicStarBomb()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        UltimateFlarePower power =
            (UltimateFlarePower)ModelDb.Power<UltimateFlarePower>().ToMutable();
        decimal damage = DynamicVars.Damage.BaseValue;
        if (Enchantment is { } enchantment)
        {
            damage += enchantment.EnchantDamageAdditive(
                damage, DynamicVars.Damage.Props);
            damage *= enchantment.EnchantDamageMultiplicative(
                damage, DynamicVars.Damage.Props);
        }
        power.Damage = TransformationCmd.ApplyAmplificationToDelayedValue(
            Owner.Creature, this, damage);
        await PowerCmd.Apply(
            context, power, Owner.Creature, 1, Owner.Creature, this);
        if (await TransformationCmd.PayOverdraft(context, Owner.Creature, this))
        {
            await PlayerCmd.GainEnergy(1, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(8);
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class Takemikazuchi : MSNeutralCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6, ValueProp.Move), new ExtraDamageVar(3)];

    public Takemikazuchi()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        TakemikazuchiTrackerPower? tracker =
            Owner.Creature.GetPower<TakemikazuchiTrackerPower>();
        decimal damage = DynamicVars.Damage.BaseValue
            + (tracker?.GeneratedEnchantedCards ?? 0)
                * DynamicVars.ExtraDamage.BaseValue;
        int hits = 1 + (tracker?.PlayedEnchantedCards ?? 0);
        return IterationCardEffects.Attack(this, context, play, damage, hits);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars.ExtraDamage.UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSNeutralCardPool))]
public sealed class WindGodCloak : MSNeutralCard
{
    public WindGodCloak()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<WindGodCloakPower>(
            context, Owner.Creature, 1, Owner.Creature, this);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
