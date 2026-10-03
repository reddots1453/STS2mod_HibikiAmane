using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Powers;
using MaidenSuccubus.UI;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.Core.Control;

/// <summary>
/// Instance-attached runtime state for a card temporarily presented as Escape.
/// The card model remains the original object; ControlQuery reconciles this
/// capability from the saved ControlPower instances.
/// </summary>
[RegisterModelCapability(StableEntryStem = "escape_projection")]
public sealed class EscapeProjectionCapability :
    CardPlayCapability,
    ICardPropertyContributor,
    ICardPlayResultContributor,
    ICardOverlayContributor,
    ICardHoverTipContributor
{
    private ControlPower? _control;
    private string _originalTitle = string.Empty;

    internal ControlPower? Control => _control;
    internal string OriginalTitle => _originalTitle;

    internal void Bind(ControlPower control, string originalTitle)
    {
        ArgumentNullException.ThrowIfNull(control);
        _control = control;
        if (string.IsNullOrEmpty(_originalTitle))
        {
            _originalTitle = originalTitle;
            MarkDirty();
        }
    }

    internal bool TryCreateProjection(out EscapeProjection projection)
    {
        projection = default;
        CardModel? card = Owner;
        if (card == null)
        {
            return false;
        }

        if (_control == null || !ControlQuery.IsValidBinding(card, _control))
        {
            _control = ControlQuery.FindMatchingControl(card.Owner, card.Type);
        }
        if (_control == null || !ControlQuery.IsValidBinding(card, _control))
        {
            return false;
        }

        projection = new EscapeProjection(_control, card, _originalTitle);
        return true;
    }

    TargetType? ICardPropertyContributor.GetTargetType(CardModel card) =>
        TryCreateProjection(out _) ? TargetType.None : null;

    PileType? ICardPlayResultContributor.GetResultPileTypeForCardPlay(
        CardModel card) =>
        TryCreateProjection(out _) ? PileType.Discard : null;

    IEnumerable<CardOverlayContribution>
        ICardOverlayContributor.GetCardOverlays(CardOverlayContext context) =>
        TryCreateProjection(out _)
            ? [CardOverlayContribution.FromFactory(
                "maiden_escape_chains",
                _ => EscapeCardVisuals.CreateOverlay())]
            : [];

    IEnumerable<IHoverTip> ICardHoverTipContributor.GetHoverTips(CardModel card)
    {
        if (!TryCreateProjection(out EscapeProjection projection))
        {
            return [];
        }

        CardModel originalPreview;
        using (ControlQuery.SuppressPresentation())
        {
            originalPreview = (CardModel)card.MutableClone();
        }
        originalPreview.RemoveCapability<EscapeProjectionCapability>();
        return
        [
            new CardHoverTip(originalPreview),
            projection.Control.DumbHoverTip,
        ];
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.IsFirstInSeries
            && ReferenceEquals(cardPlay.Card, Owner)
            && TryCreateProjection(out EscapeProjection projection))
        {
            EscapeProjectionTracker.Begin(cardPlay, projection.Control);
        }
        return Task.CompletedTask;
    }

    protected override Task<bool> BeforeOwnerCardOnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        Task.FromResult(TryCreateProjection(out _));

    protected override Task OnOwnerCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) => Task.CompletedTask;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (_control != null
            && EscapeProjectionTracker.TryTake(cardPlay, _control, out int amount))
        {
            ControlPower control = _control;
            string originalTitle = _originalTitle ?? "";
            control.FlashForEscape();
            await ControlCmd.Escape(choiceContext, control, amount);
            if (ControlQuery.IsControlled(cardPlay.Player))
                CombatTextFeedback.Notify("escape_incomplete", cardPlay.Player.Creature,
                    control.Applier, amount: amount,
                    newValue: (int)cardPlay.Player.Creature.Powers.OfType<ControlPower>().Sum(p => p.Amount),
                    controlType: control.ControlType.LocalizedName(), card: originalTitle,
                    bindingType: control.ControlType);
        }
    }

    protected override JsonNode? SaveAdditionalState() =>
        string.IsNullOrEmpty(_originalTitle)
            ? null
            : new JsonObject { ["originalTitle"] = _originalTitle };

    protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
    {
        _control = null;
        _originalTitle = state?["originalTitle"]?.GetValue<string>() ?? string.Empty;
    }
}
