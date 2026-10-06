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
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext context,
        Player player)
    {
        // Native turn-start hand draw has already completed. Exhaust recovery
        // adds cards independently and never replaces or tops up normal draws.
        ICombatState? combat = Owner.CombatState;
        if (player.Creature != Owner || Amount <= 0 || combat == null
            || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }
        CardModel[] cards = PileType.Exhaust.GetPile(player).Cards
            .Take(Amount)
            .ToArray();
        if (cards.Length == 0)
        {
            return;
        }
        if (!Hook.ShouldDraw(combat, player, fromHandDraw: true, out AbstractModel? modifier))
        {
            if (modifier != null)
                await Hook.AfterPreventingDraw(combat, modifier);
            return;
        }
        foreach (CardModel card in cards)
        {
            if (CombatManager.Instance.IsOverOrEnding
                || PileType.Hand.GetPile(player).Cards.Count >= CardPile.MaxCardsInHand)
            {
                break;
            }
            if (card.Pile?.Type != PileType.Exhaust)
            {
                continue;
            }
            await CardPileCmd.Add(card, PileType.Hand);
            // Recovery is a draw, so generated statuses and draw-triggered
            // effects receive the same lifecycle as native turn-start draws.
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
