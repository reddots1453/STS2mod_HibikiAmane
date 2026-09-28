using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
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
    private bool IsActive => IsMutable && Amount > 0 && Owner.IsAlive && Owner.Powers.Contains(this);

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay) =>
        IsActive && target == Owner && props.IsPoweredAttack()
            && dealer != null && dealer.Side != Owner.Side && dealer.HasPower<BurningPower>()
            ? 0.5m
            : 1m;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext context,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        // Native Colossus lifetime boundary, not each individual player's turn.
        if (IsActive && side == CombatSide.Enemy)
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

    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player.Creature == Owner ? count + Amount : count;

    public override decimal ModifyHandDrawLate(Player player, decimal count)
    {
        if (player.Creature != Owner)
        {
            return count;
        }
        decimal total = Math.Max(0, count);
        _pendingDraw = Math.Min((int)total, PileType.Exhaust.GetPile(player).Cards.Count);
        // The early +Amount also registers this listener when one exhausted card
        // exactly cancels the bonus: Hook only registers changed draw counts.
        // CombatManager invokes AfterModifyingHandDraw before its normal Draw.
        // Recover exhausted cards first, then let that Draw fill only the remainder.
        return total - _pendingDraw;
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
        ICombatState? combat = Owner.CombatState;
        if (combat == null || CombatManager.Instance.IsOverOrEnding
            || !Hook.ShouldDraw(combat, Owner.Player, fromHandDraw: true, out _))
        {
            return;
        }
        BlockingPlayerChoiceContext context = new();
        foreach (CardModel card in cards)
        {
            if (CombatManager.Instance.IsOverOrEnding
                || PileType.Hand.GetPile(Owner.Player).Cards.Count >= 10)
            {
                break;
            }
            if (card.Pile?.Type != PileType.Exhaust)
            {
                continue;
            }
            await CardPileCmd.Add(card, PileType.Hand);
            // Match the draw lifecycle, not just a pile transfer: draw-triggered
            // effects and history must see recovered cards as start-of-turn draws.
            CombatManager.Instance.History.CardDrawn(combat, card, fromHandDraw: true);
            await Hook.AfterCardDrawn(combat, context, card, fromHandDraw: true);
            card.InvokeDrawn();
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
