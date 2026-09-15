using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Commands;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class TakemikazuchiTrackerPower : MaidenSuccubusPowerTemplate,
    ICombatEnchantmentAppliedListener
{
    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    [SavedProperty]
    public int GeneratedEnchantedCards { get; set; }

    [SavedProperty]
    public int PlayedEnchantedCards { get; set; }

    public void AfterCombatEnchantmentApplied(CardModel card)
    {
        if (card.Owner.Creature == Owner)
        {
            GeneratedEnchantedCards++;
        }
    }

    public override Task AfterCardGeneratedForCombat(
        CardModel card,
        Player? creator)
    {
        if (card.Owner.Creature == Owner && card.Enchantment != null)
        {
            GeneratedEnchantedCards++;
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (cardPlay.IsLastInSeries
            && cardPlay.Card.Owner.Creature == Owner
            && cardPlay.Card.Enchantment != null)
        {
            PlayedEnchantedCards++;
        }
        return Task.CompletedTask;
    }
}

[RegisterPower]
public sealed class WindGodCloakPower : MaidenSuccubusPowerTemplate
{
    private CardModel? _activationCardToIgnore;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    [SavedProperty]
    public bool CopiedThisTurn { get; set; }

    public void IgnoreActivationCard(CardModel card) =>
        _activationCardToIgnore = card;

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<MegaCrit.Sts2.Core.Entities.Creatures.Creature> creatures,
        ICombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            CopiedThisTurn = false;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (ReferenceEquals(cardPlay.Card, _activationCardToIgnore))
        {
            _activationCardToIgnore = null;
            return;
        }

        if (CopiedThisTurn
            || !cardPlay.IsLastInSeries
            || cardPlay.Card.Owner.Creature != Owner
            || cardPlay.Resources.EnergyValue != 0
            || Owner.Player == null
            || Owner.CombatState == null)
        {
            return;
        }

        CopiedThisTurn = true;
        CardModel copy = Owner.CombatState.CloneCard(cardPlay.Card);
        copy.DeckVersion = null;
        await CardPileCmd.AddGeneratedCardToCombat(
            copy, PileType.Hand, Owner.Player);
    }
}
