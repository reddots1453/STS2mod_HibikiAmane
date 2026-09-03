using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Control;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class MagicResonancePower : MaidenSuccubusPowerTemplate,
    ICombatEnchantmentAppliedListener
{
    [SavedProperty] public int Progress { get; set; }
    [SavedProperty] public int PendingAmplification { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => CommonPowerAssets.Generic;

    public void AfterCombatEnchantmentApplied(CardModel card)
    {
        if (card.Owner.Creature == Owner)
        {
            CountOne();
        }
    }

    public override async Task AfterCardGeneratedForCombat(
        CardModel card,
        Player? creator)
    {
        if (card.Owner.Creature != Owner || card.Enchantment == null)
        {
            return;
        }
        CountOne();
        await ResolvePending(new BlockingPlayerChoiceContext());
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (cardPlay.IsLastInSeries
            && cardPlay.Card.Owner.Creature == Owner
            && cardPlay.Card.Enchantment != null)
        {
            CountOne();
        }

        await ResolvePending(context);
    }

    private void CountOne()
    {
        Progress++;
        int threshold = Math.Max(1, (int)Amount);
        while (Progress >= threshold)
        {
            Progress -= threshold;
            PendingAmplification++;
        }
        InvokeDisplayAmountChanged();
    }

    private async Task ResolvePending(PlayerChoiceContext context)
    {
        if (PendingAmplification <= 0)
        {
            return;
        }
        int pending = PendingAmplification;
        PendingAmplification = 0;
        await PowerCmd.Apply<MagicAmplificationPower>(
            context, Owner, pending, Owner, null);
    }
}

[RegisterPower]
public sealed class GoddessOfIcePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (!cardPlay.IsLastInSeries
            || cardPlay.Card.Owner.Creature != Owner
            || cardPlay.Card.Enchantment == null
            || Owner.Player == null)
        {
            return;
        }

        CardModel shard = Owner.CombatState!.CreateCard(
            ModelDb.Card<IceShard>(), Owner.Player);
        if (Amount >= 2)
        {
            CardCmd.Upgrade(shard);
        }
        await CardPileCmd.AddGeneratedCardToCombat(
            shard, PileType.Hand, Owner.Player);
    }
}

public interface IEscapeCard
{
}

[RegisterPower]
public sealed class BindingInsightPower : MaidenSuccubusPowerTemplate
{
    private CardModel? _pendingEscapeCard;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.IsFirstInSeries
            && cardPlay.Card.Owner.Creature == Owner
            && (cardPlay.Card is IEscapeCard
                || ControlQuery.GetProjection(cardPlay.Card) != null))
        {
            _pendingEscapeCard = cardPlay.Card;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (cardPlay.IsLastInSeries
            && ReferenceEquals(cardPlay.Card, _pendingEscapeCard)
            && Owner.Player != null)
        {
            _pendingEscapeCard = null;
            await PlayerCmd.GainEnergy((int)Amount, Owner.Player);
        }
    }
}
