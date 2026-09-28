using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;

namespace MaidenSuccubus.Commands;

internal static class TemperancePileCmd
{
    internal static async Task Play(PlayerChoiceContext context, Player player, int count, Func<bool>? stillActive = null)
    {
        var combat = player.Creature.CombatState;
        bool Current() => combat != null && player.Creature.CombatState == combat
            && player.Creature.IsAlive && !CombatManager.Instance.IsOverOrEnding && (stillActive?.Invoke() ?? true);
        if (count <= 0 || !Current()) return;
        LibraryPileChoice[] choices = new[] { PileType.Draw, PileType.Discard, PileType.Exhaust }
            .Select(pile =>
            {
                LibraryPileChoice choice = combat!.CreateCard<LibraryPileChoice>(player);
                choice.Configure(pile, count);
                return choice;
            }).ToArray();
        if (await CardSelectCmd.FromChooseACardScreen(context, choices, player, canSkip: false)
            is not LibraryPileChoice selected) return;
        if (!Current() || !choices.Contains(selected)) return;
        // Snapshot once: cards returning to this pile are not sampled again.
        List<CardModel> cards = selected.SelectedPile.GetPile(player).Cards.ToList();
        cards.StableShuffle(player.RunState.Rng.CombatCardGeneration);
        foreach (CardModel card in cards.Take(count))
        {
            if (!Current()) break;
            if (card.Owner == player && card.Pile == selected.SelectedPile.GetPile(player))
                await CardCmd.AutoPlay(context, card, target: null);
        }
    }
}
