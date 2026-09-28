using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

/// <summary>
/// For N turns, attack damage received is increased by N. N is both the
/// current magnitude and remaining duration, and decreases once per owner turn.
/// </summary>
[RegisterPower]
public sealed class ShatterPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target != Owner || dealer is null || dealer.Side == Owner.Side
            || !props.IsPoweredAttack())
        {
            return 0;
        }
        return Amount;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.TickDownDuration(this);
        }
    }
}

/// <summary>
/// Burning damages a player once before an attack card resolves, but damages
/// an enemy before every hit of its current attack.  The attack-active flag is
/// deliberately scoped by BeforeAttack/AfterAttack so non-attack damage dealt
/// by the same creature cannot accidentally trigger Burning.
/// </summary>
[RegisterPower]
public sealed class BurningPower : MaidenSuccubusPowerTemplate
{
    private bool _enemyAttackActive;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker != Owner)
            return;

        if (Owner.Side == CombatSide.Enemy)
        {
            _enemyAttackActive = true;
            return;
        }

        if (command.ModelSource is CardModel { Type: CardType.Attack })
        {
            await ResolveBurningDamage(new BlockingPlayerChoiceContext());
        }
    }

    public override Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!_enemyAttackActive || dealer != Owner || target.Side == Owner.Side)
            return Task.CompletedTask;

        return ResolveBurningDamage(choiceContext);
    }

    private Task ResolveBurningDamage(PlayerChoiceContext choiceContext)
    {
        if (Owner.Player != null
            && Owner.HasPower<LordOfBlazePower>()
            && Owner.CombatState != null)
        {
            Creature[] enemies = Owner.CombatState.GetOpponentsOf(Owner)
                .Where(creature => creature.IsAlive)
                .ToArray();
            if (enemies.Length > 0)
            {
                Creature target = enemies[
                    Owner.Player.RunState.Rng.CombatTargets.NextInt(enemies.Length)];
                return CreatureCmd.Damage(
                    choiceContext,
                    target,
                    Amount,
                    ValueProp.Unpowered | ValueProp.Move,
                    Owner);
            }
        }

        return CreatureCmd.Damage(
            choiceContext,
            Owner,
            Amount,
            ValueProp.Unpowered | ValueProp.Move,
            Owner);
    }

    public override Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker == Owner)
            _enemyAttackActive = false;
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
            await PowerCmd.ModifyAmount(choiceContext, this, -1, Owner, null);
    }
}

[RegisterPower]
public sealed class FearAuraStrengthLossPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<FearAura>();
    protected override bool IsPositive => false;
}
