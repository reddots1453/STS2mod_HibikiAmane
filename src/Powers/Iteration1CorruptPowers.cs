using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Core.Invasion;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class LordOfBlazePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext context,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || Owner.CombatState == null)
        {
            return;
        }
        foreach (Creature creature in Owner.CombatState.Creatures
            .Where(creature => creature.IsAlive)
            .ToArray())
        {
            await PowerCmd.Apply<BurningPower>(
                context, creature, Amount, Owner, null);
        }
    }
}

[RegisterPower]
public sealed class DarkFlameBarrierPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay) =>
        target == Owner && dealer?.HasPower<BurningPower>() == true
            ? 0.5m
            : 1m;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext context,
        Player player)
    {
        if (player.Creature == Owner)
        {
            await PowerCmd.Decrement(this);
        }
    }
}

[RegisterPower]
public sealed class RecollectionRoomPower : MaidenSuccubusPowerTemplate
{
    private int _pendingDraw;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player.Creature != Owner)
        {
            return count;
        }
        _pendingDraw = Math.Max(0, (int)count + (int)Amount);
        return 0;
    }

    public override async Task AfterModifyingHandDraw()
    {
        if (_pendingDraw <= 0 || Owner.Player == null)
        {
            return;
        }
        CardModel[] cards = PileType.Exhaust.GetPile(Owner.Player).Cards
            .Take(_pendingDraw)
            .ToArray();
        _pendingDraw = 0;
        foreach (CardModel card in cards)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}

[RegisterPower]
public sealed class SemenAppetitePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldAddToDeck(CardModel card)
    {
        if (card is not IInvasionSourcedCurse)
        {
            return true;
        }
        Flash();
        TaskHelper.RunSafely(CreatureCmd.GainMaxHp(Owner, Amount));
        return false;
    }
}
