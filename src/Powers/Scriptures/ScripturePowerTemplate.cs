using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Scriptures;

namespace MaidenSuccubus.Powers.Scriptures;

public enum ScriptureTriggerTiming
{
    OnApply,
    TurnStart,
    TurnEnd,
}

/// <summary>
/// One independently timed Scripture effect. Amount is remaining duration.
/// Instances do not merge, so multiple transformed cards retain independent
/// expiration and each remaining turn counts as a buff layer.
/// </summary>
public abstract class ScripturePowerTemplate : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected abstract ScriptureTriggerTiming Timing { get; }

    protected abstract Task TriggerEffect(PlayerChoiceContext choiceContext);

    private bool IsActive => IsMutable && Amount > 0 && Owner.IsAlive
        && Owner.Powers.Contains(this);

    public override async Task AfterApplied(
        Creature? applier,
        MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        if (!IsActive || Timing != ScriptureTriggerTiming.OnApply)
        {
            return;
        }

        // Nimble grants Dexterity now, but its Scripture event is the turn-end
        // duration tick (DesignDoc SYS-SCR-001 / Holy Resonance).
        await TriggerEffect(new ThrowingPlayerChoiceContext());
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (IsActive && Timing == ScriptureTriggerTiming.TurnStart
            && player.Creature == Owner)
        {
            await TriggerPublishAndTick(choiceContext);
        }
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!IsActive || side != Owner.Side || !participants.Contains(Owner))
        {
            return;
        }

        if (Timing == ScriptureTriggerTiming.TurnEnd)
        {
            await TriggerPublishAndTick(choiceContext);
        }
        else if (Timing == ScriptureTriggerTiming.OnApply)
        {
            Flash();
            await ScriptureCmd.Publish(new ScriptureTriggered(this, Owner));
            await Tick(choiceContext);
        }
    }

    private async Task TriggerPublishAndTick(PlayerChoiceContext choiceContext)
    {
        await TriggerAndPublish(choiceContext);
        await Tick(choiceContext);
    }

    private async Task TriggerAndPublish(PlayerChoiceContext choiceContext)
    {
        Flash();
        await TriggerEffect(choiceContext);
        await ScriptureCmd.Publish(new ScriptureTriggered(this, Owner));
    }

    private async Task Tick(PlayerChoiceContext choiceContext)
    {
        if (IsActive)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                this,
                -1m,
                Owner,
                null);
        }
    }
}
