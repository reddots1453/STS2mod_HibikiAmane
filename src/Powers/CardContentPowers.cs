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
public sealed class RestoreStrengthAtTurnEndPower : MaidenSuccubusPowerTemplate
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
public sealed class DelayedStrengthPricePower : MaidenSuccubusPowerTemplate
{
    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
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
