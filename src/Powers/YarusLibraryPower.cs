using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class YarusLibraryPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldDraw(Player player, bool fromHandDraw) => player.Creature != Owner;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner || Owner.CombatState == null
            || CombatManager.Instance.IsOverOrEnding)
            return;

        LibraryPileChoice[] choices = new[] { PileType.Draw, PileType.Discard, PileType.Exhaust }
            .Select(pile =>
            {
                LibraryPileChoice choice = Owner.CombatState.CreateCard<LibraryPileChoice>(player);
                choice.Configure(pile);
                return choice;
            }).ToArray();
        if (await CardSelectCmd.FromChooseACardScreen(context, choices, player, canSkip: false)
            is not LibraryPileChoice selected)
            return;

        // Snapshot the selected pile: cards returned by their own effects cannot
        // be replayed repeatedly to turn a one-card discard pile into ten plays.
        List<CardModel> cards = selected.SelectedPile.GetPile(player).Cards.ToList();
        cards.StableShuffle(player.RunState.Rng.CombatCardGeneration);
        foreach (CardModel card in cards.Take(10))
        {
            if (CombatManager.Instance.IsOverOrEnding) break;
            if (card.Pile?.Type == selected.SelectedPile)
                await CardCmd.AutoPlay(context, card, target: null);
        }
    }
}
