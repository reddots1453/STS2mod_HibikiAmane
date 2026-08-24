using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Core.Transformation;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class ImmaculateRobePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (!ReferenceEquals(player.Creature, Owner))
        {
            return;
        }
        decimal amount = Owner.GetPower<EternalRobePower>()?.Amount ?? 1m;
        await PowerCmd.Apply<MagicAmplificationPower>(
            choiceContext, Owner, amount, Owner, null);
    }
}

[RegisterPower]
public sealed class MagicArmorPower : ModPowerTemplate
{
    private bool _pendingDecrement;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    [SavedProperty] public bool UsedThisTurn { get; set; }
    internal bool SuppressFormRemoval { get; set; }

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            description.Add("Chance", Amount switch
            {
                >= 3 => "10%",
                2 => "30%",
                1 => "50%",
                _ => "0%",
            });
            return description;
        }
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        // "每回合第一次" is one use from the start of one player turn until
        // the next player turn.  Resetting again when the enemy side starts
        // allowed a counterattack during the player turn and a normal enemy
        // attack to consume two armour layers in the same round.
        if (side == CombatSide.Player)
        {
            UsedThisTurn = false;
            _pendingDecrement = false;
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!ReferenceEquals(target, Owner)
            || Amount <= 0
            || UsedThisTurn
            || amount <= 0
            || dealer == null
            || dealer.Side != CombatSide.Enemy
            || !props.HasFlag(ValueProp.Move)
            || props.HasFlag(ValueProp.Unpowered))
        {
            return amount;
        }

        UsedThisTurn = true;
        _pendingDecrement = true;
        return amount * 0.5m;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        if (!_pendingDecrement)
        {
            return;
        }
        _pendingDecrement = false;
        Flash();
        await PowerCmd.Decrement(this);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (SuppressFormRemoval)
        {
            return;
        }
        ImmaculateRobePower? form = oldOwner.Powers
            .OfType<ImmaculateRobePower>()
            .FirstOrDefault();
        if (form != null)
        {
            await PowerCmd.Remove(form);
        }
    }
}

[RegisterPower]
public sealed class MagicAmplificationPower : ModPowerTemplate
{
    private CardModel? _cardToAmplify;
    private int _reservedForOverdraft;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public bool IsAmplifying(CardModel card) => ReferenceEquals(card, _cardToAmplify);

    internal bool TryReserveForOverdraft(CardModel? card)
    {
        if (card == null
            || !ReferenceEquals(card, _cardToAmplify)
            || _reservedForOverdraft >= Amount)
        {
            return false;
        }

        _reservedForOverdraft++;
        return true;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (_cardToAmplify == null
            && Amount > 0
            && cardPlay.IsFirstInSeries
            && cardPlay.Card.Owner?.Creature == Owner
            && cardPlay.Card.Type is CardType.Attack or CardType.Skill)
        {
            _cardToAmplify = cardPlay.Card;
            _reservedForOverdraft = 0;
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay) =>
        ReferenceEquals(dealer, Owner) && ReferenceEquals(cardSource, _cardToAmplify)
            ? AmplificationMultiplier(cardSource)
            : 1m;

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay) =>
        ReferenceEquals(target, Owner) && ReferenceEquals(cardSource, _cardToAmplify)
            ? AmplificationMultiplier(cardSource)
            : 1m;

    private decimal AmplificationMultiplier(CardModel? card) =>
        card is IDoubleMagicAmplification
        || (card?.Enchantment != null && Owner.HasPower<TacticalCorePower>())
            ? 2m
            : 1.5m;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (!ReferenceEquals(cardPlay.Card, _cardToAmplify)
            || !cardPlay.IsLastInSeries)
        {
            return;
        }

        int reservedForOverdraft = _reservedForOverdraft;
        _cardToAmplify = null;
        _reservedForOverdraft = 0;
        if (reservedForOverdraft > 0)
        {
            await PowerCmd.ModifyAmount(
                context,
                this,
                -reservedForOverdraft,
                null,
                null);
        }
        else
        {
            await PowerCmd.Decrement(this);
        }
    }
}
