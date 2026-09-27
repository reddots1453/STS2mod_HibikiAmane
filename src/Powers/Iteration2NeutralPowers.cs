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
public sealed class TakemikazuchiTrackerPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    [SavedProperty]
    public int PlayedEnchantedCards { get; set; }

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
    private CardModel? _pendingCopyCard;
    private int _pendingCopyCount;
    private int _pendingSeriesDepth;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    [SavedProperty]
    public bool CopiedThisTurn { get; set; }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<MegaCrit.Sts2.Core.Entities.Creatures.Creature> creatures,
        ICombatState combatState)
    {
        if (side == CombatSide.Player && creatures.Contains(Owner))
        {
            CopiedThisTurn = false;
            ClearPendingCopy();
        }
        return Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (!cardPlay.IsFirstInSeries || cardPlay.Card.Owner.Creature != Owner
            || Owner.Player == null || Owner.CombatState == null)
            return Task.CompletedTask;

        // Track reentrant plays of this SAME instance separately from native
        // replay iterations. Their completion must not consume the outer copy.
        if (ReferenceEquals(cardPlay.Card, _pendingCopyCard))
        {
            _pendingSeriesDepth++;
            return Task.CompletedTask;
        }
        if (CopiedThisTurn || cardPlay.Resources.EnergySpent != 0 || Amount <= 0)
            return Task.CompletedTask;

        // Capture before OnPlay: nested auto-plays cannot steal "first", and a
        // new Cloak power created by OnPlay cannot retroactively copy itself.
        CopiedThisTurn = true;
        _pendingCopyCard = cardPlay.Card;
        _pendingCopyCount = Amount;
        _pendingSeriesDepth = 1;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (!cardPlay.IsLastInSeries || !ReferenceEquals(cardPlay.Card, _pendingCopyCard))
            return;
        if (--_pendingSeriesDepth > 0) return;

        int copies = _pendingCopyCount;
        ClearPendingCopy(); // Release before yielding or dispatching generation hooks.
        if (Owner.Player is not { } player || Owner.CombatState is not { } combat) return;
        for (int i = 0; i < copies; i++)
        {
            if (Owner.CombatState != combat || CombatManager.Instance.IsOverOrEnding) break;
            CardModel copy = combat.CloneCard(cardPlay.Card);
            copy.DeckVersion = null;
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, player);
        }
    }

    private void ClearPendingCopy()
    {
        _pendingCopyCard = null;
        _pendingCopyCount = 0;
        _pendingSeriesDepth = 0;
    }

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        ClearPendingCopy();
    }
}
