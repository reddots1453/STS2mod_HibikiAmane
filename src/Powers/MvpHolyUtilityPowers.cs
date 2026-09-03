using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards.Scriptures;
using MaidenSuccubus.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class ConsecrationPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner) return;
        CardModel? card = (await CardSelectCmd.FromHand(context, player,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1),
            candidate => candidate.IsTransformable, this)).FirstOrDefault();
        if (card != null) await ScriptureCmd.TransformToRandomScripture(card);
    }
}

[RegisterPower]
public sealed class SoulPurificationPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation location)
    {
        if (card.Owner.Creature != Owner || card is not ScriptureCardTemplate)
            return location;
        return new CardLocation(card.Owner, PileType.Exhaust, CardPilePosition.Bottom);
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner && cardPlay.Card is ScriptureCardTemplate)
            await CardPileCmd.Draw(context, (int)Amount, cardPlay.Card.Owner);
    }
}

[RegisterPower]
public sealed class MemoryImprintPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner) return;
        CardPile discard = PileType.Discard.GetPile(player);
        if (discard.Cards.Count == 0) return;
        CardModel? card = (await CardSelectCmd.FromCombatPile(context, discard, player,
            new CardSelectorPrefs(new MegaCrit.Sts2.Core.Localization.LocString(
                "card_selection", "MAIDEN_SUCCUBUS_TO_DRAW_TOP"), 1))).FirstOrDefault();
        if (card != null) await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
    }
}
