using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class AbnormalAdaptationPower : MaidenSuccubusPowerTemplate
{
    [SavedProperty]
    public int RemainingTriggers { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this && amount > 0)
            RemainingTriggers += (int)amount * 2;
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == Owner.Side)
            RemainingTriggers = (int)Amount * 2;
        return Task.CompletedTask;
    }

    public override async Task AfterCardDrawnEarly(PlayerChoiceContext context, CardModel card, bool fromHandDraw)
    {
        if (RemainingTriggers <= 0 || card.Owner.Creature != Owner || card.Type is not (CardType.Curse or CardType.Status))
            return;
        RemainingTriggers--;
        await CardCmd.Discard(context, [card]);
        await CardPileCmd.Draw(context, 2, card.Owner);
    }
}

[RegisterPower]
public sealed class MasochisticGirlPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power.Owner != Owner || power.Type != PowerType.Debuff || amount <= 0)
            return Task.CompletedTask;
        return CreatureCmd.GainBlock(Owner, amount * Amount, ValueProp.Unpowered, null);
    }
}

[RegisterPower]
public sealed class SanctuaryPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay) => target == Owner ? 0.5m : 1m;
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == Owner.Side)
            await PowerCmd.Decrement(this);
    }
}

[RegisterPower]
public sealed class BlizzardEchoPower : MaidenSuccubusPowerTemplate
{
    [SavedProperty] public decimal Damage { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.Player is null)
            return;
        ArgumentNullException.ThrowIfNull(Owner.CombatState);
        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            Owner.CombatState.GetOpponentsOf(Owner).Where(c => c.IsAlive),
            Damage,
            ValueProp.Move,
            Owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            Owner.CombatState.CreateCard<IceMist>(Owner.Player), PileType.Hand, Owner.Player);
        await PowerCmd.Decrement(this);
    }
}
