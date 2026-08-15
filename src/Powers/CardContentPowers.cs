using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class RestoreStrengthAtTurnEndPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player)
        {
            return;
        }
        decimal amount = Amount;
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner,
            amount,
            Owner,
            null);
    }
}

[RegisterPower]
public sealed class DelayedStrengthPricePower : ModPowerTemplate
{
    public int Damage { get; set; } = 30;
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
        {
            return;
        }
        if (Amount > 1)
        {
            await PowerCmd.Apply<DelayedStrengthPricePower>(
                choiceContext,
                Owner,
                -1,
                Owner,
                null);
            return;
        }
        await PowerCmd.Remove(this);
        await CreatureCmd.Damage(
            choiceContext,
            Owner,
            Damage,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            null,
            null);
    }
}

[RegisterPower]
public sealed class MemoryKindlingPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal)
    {
        if (card.Owner.Creature != Owner)
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner,
            Amount,
            Owner,
            null);
        await PowerCmd.Apply<RestoreStrengthAtTurnEndPower>(
            choiceContext,
            Owner,
            Amount,
            Owner,
            null);
        if (Owner.CombatState == null)
        {
            return;
        }
        var enemies = Owner.CombatState.HittableEnemies;
        if (enemies.Count > 0)
        {
            Creature enemy = enemies[
                Owner.CombatState.RunState.Rng.CombatTargets
                    .NextInt(enemies.Count)];
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext,
                enemy,
                1,
                Owner,
                null);
        }
    }
}

[RegisterPower]
public sealed class AbyssalEchoPower : ModPowerTemplate
{
    private bool _echoing;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (_echoing
            || power.Owner != Owner
            || power is not StrengthPower
            || amount <= 0)
        {
            return;
        }
        _echoing = true;
        try
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                Owner,
                amount * Amount,
                Owner,
                null);
        }
        finally
        {
            _echoing = false;
        }
    }
}
