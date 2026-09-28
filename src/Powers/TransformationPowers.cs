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
using MaidenSuccubus.Core.Temptation;
using MaidenSuccubus.Presentation;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class ImmaculateRobePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        TransformationEvents.Publish(Owner);
        TemptationEvents.Publish(Owner);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        TransformationEvents.Publish(oldOwner);
        TemptationEvents.Publish(oldOwner);
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (!ReferenceEquals(player.Creature, Owner) || Owner.HasPower<EternalRobePower>())
        {
            return;
        }
        await PowerCmd.Apply<MagicAmplificationPower>(
            choiceContext, Owner, 1, Owner, null);
    }
}

[RegisterPower]
public sealed class CorruptRobePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        TransformationEvents.Publish(Owner);
        TemptationEvents.Publish(Owner);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        TransformationEvents.Publish(oldOwner);
        TemptationEvents.Publish(oldOwner);
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player) =>
        ReferenceEquals(player.Creature, Owner)
            ? PowerCmd.Apply<MagicAmplificationPower>(
                choiceContext, Owner, 1, Owner, null)
            : Task.CompletedTask;
}

[RegisterPower]
public sealed class MagicArmorPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => CommonPowerAssets.Generic;
    [SavedProperty] public bool UsedThisTurn { get; set; }
    internal bool SuppressFormRemoval { get; set; }

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            return description;
        }
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        TransformationEvents.Publish(Owner);
        TemptationEvents.Publish(Owner);
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext context,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (ReferenceEquals(power, this))
        {
            TransformationEvents.Publish(Owner);
            TemptationEvents.Publish(Owner);
            if (amount < 0
                && Owner.HasPower<CorruptRobePower>()
                && Owner.Player is { } player)
            {
                await Data.Desire.Modify(player, 1);
            }
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

        // Preview hooks must be pure. Commit the use only in the actual
        // damage callback, never while the UI calculates a forecast.
        return amount * 0.67m;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        if (UsedThisTurn || Amount <= 0)
        {
            return;
        }
        UsedThisTurn = true;
        Flash();
        await TransformationCmd.LoseArmor(
            new BlockingPlayerChoiceContext(), Owner, 1, null);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext context, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // At zero the HP modifier is unchanged, so the engine does not call
        // AfterModifyingHpLostAfterOsty. Use the real damage result instead.
        if (target != Owner || UsedThisTurn || result.UnblockedDamage <= 0
            || dealer?.Side != CombatSide.Enemy || !props.HasFlag(ValueProp.Move)
            || props.HasFlag(ValueProp.Unpowered)) return;
        UsedThisTurn = true;
        await TransformationCmd.LoseArmor(context, Owner, 1, cardSource);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        TransformationEvents.Publish(oldOwner);
        TemptationEvents.Publish(oldOwner);
        if (SuppressFormRemoval)
        {
            return;
        }
        foreach (PowerModel form in oldOwner.Powers
            .Where(power => power is ImmaculateRobePower or CorruptRobePower or EternalRobePower)
            .ToArray())
        {
            await PowerCmd.Remove(form);
        }
    }
}

[RegisterPower]
public sealed class MagicAmplificationPower : MaidenSuccubusPowerTemplate
{
    private CardModel? _cardToAmplify;
    private int _reservedForOverdraft;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => CommonPowerAssets.Generic;

    public bool IsAmplifying(CardModel card) => ReferenceEquals(card, _cardToAmplify);

    // Forecast only. Never reserve a layer while descriptions are refreshed.
    internal bool CanPreviewOverdraft(CardModel card) => Amount > 0
        && card.Owner?.Creature == Owner
        && card.Type is CardType.Attack or CardType.Skill
        && !AmplificationConsumptionScope.IsExempt(card)
        && (_cardToAmplify == null
            || (ReferenceEquals(card, _cardToAmplify) && _reservedForOverdraft < Amount));

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        PlayGainFeedback();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext context,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (ReferenceEquals(power, this) && amount > 0)
        {
            PlayGainFeedback();
        }
        return Task.CompletedTask;
    }

    internal bool TryReserveForOverdraft(CardModel? card)
    {
        if (card == null || AmplificationConsumptionScope.IsExempt(card)) return false;
        // Vortex can gain its first amplification from a child after its own
        // BeforeCardPlayed already ran. Reserve that newly available resource
        // for the outer Vortex, never for its exempt auto-played children.
        if (_cardToAmplify == null && Amount > 0
            && card is MaidenSuccubus.Cards.BlackVortex
            && card.Pile?.Type == PileType.Play && card.Owner?.Creature == Owner)
        {
            _cardToAmplify = card;
            _reservedForOverdraft = 0;
        }
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
            && !AmplificationConsumptionScope.IsExempt(cardPlay.Card)
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
        ReferenceEquals(dealer, Owner) && ShouldAmplify(cardSource)
            ? AmplificationMultiplier(cardSource)
            : 1m;

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay) =>
        ReferenceEquals(target, Owner) && ShouldAmplify(cardSource)
            ? AmplificationMultiplier(cardSource)
            : 1m;

    private bool ShouldAmplify(CardModel? card)
    {
        if (Amount <= 0
            || card == null
            || card.Owner?.Creature != Owner
            || card.Type is not (CardType.Attack or CardType.Skill))
        {
            return false;
        }

        // Before a card enters the play pile there is no reserved card yet.
        // Treat every eligible card as the possible "next card" so the
        // standard DynamicVar preview can show its amplified damage/block,
        // as Lethality does. Once play starts, only the reserved card and all
        // of its repeated values remain amplified.
        return _cardToAmplify == null || ReferenceEquals(card, _cardToAmplify);
    }

    private decimal AmplificationMultiplier(CardModel? card) =>
        MagicAmplificationCardRules.HasIntrinsicDouble(card)
        || (card?.Enchantment != null && Owner.HasPower<TacticalCorePower>())
            ? 2m
            : 1.5m;

    private void PlayGainFeedback()
    {
        if (CombatManager.Instance.IsInProgress
            && !CombatManager.Instance.IsEnding
            && Owner.CombatState != null
            && PerformanceAudience.IsLocalMaiden(Owner.Player))
        {
            PerformanceAudioService.PlayOneShot(PerformanceAudioCue.MagicCast);
        }
    }

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
