using System.Runtime.CompilerServices;
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
    private static readonly PileType[] SelectablePiles = [PileType.Draw, PileType.Discard, PileType.Exhaust];
    private static readonly ConditionalWeakTable<Player, PlayChain> ActiveChains = new();

    private sealed class PlayChain(ICombatState combat)
    {
        internal ICombatState Combat { get; } = combat;
        // Identity, not card ID: two copies may each play once in this chain.
        internal HashSet<CardModel> Reserved { get; } = new(ReferenceEqualityComparer.Instance);
    }

    internal static async Task Play(PlayerChoiceContext context, Player player, int count, Func<bool>? stillActive = null)
    {
        var combat = player.Creature.CombatState;
        bool InCombat() => combat != null && player.Creature.CombatState == combat
            && player.Creature.IsAlive && !CombatManager.Instance.IsOverOrEnding;
        bool Current() => InCombat() && (stillActive?.Invoke() ?? true);
        if (count <= 0 || !Current()) return;

        bool ownsChain = !ActiveChains.TryGetValue(player, out PlayChain? chain)
            || !ReferenceEquals(chain.Combat, combat);
        if (ownsChain)
        {
            ActiveChains.Remove(player);
            chain = new PlayChain(combat!);
            ActiveChains.Add(player, chain);
        }
        PlayChain activeChain = chain!;
        try
        {
            bool Eligible(CardModel card) => card.Owner == player && !activeChain.Reserved.Contains(card);
            if (!SelectablePiles.Any(pile => pile.GetPile(player).Cards.Any(Eligible))) return;
            LibraryPileChoice[] choices = SelectablePiles.Select(pile =>
            {
                LibraryPileChoice choice = combat!.CreateCard<LibraryPileChoice>(player);
                choice.Configure(pile, count);
                return choice;
            }).ToArray();
            if (await CardSelectCmd.FromChooseACardScreen(context, choices, player, canSkip: false)
                is not LibraryPileChoice selected) return;
            if (!Current() || !choices.Contains(selected)) return;

            CardPile sourcePile = selected.SelectedPile.GetPile(player);
            List<CardModel> candidates = sourcePile.Cards.Where(Eligible).ToList();
            candidates.StableShuffle(player.RunState.Rng.CombatCardGeneration);
            List<CardModel> cards = candidates.Take(count).ToList();
            // Claim the whole batch before any move hook or nested card can select it.
            foreach (CardModel card in cards) activeChain.Reserved.Add(card);
            List<CardModel> pending = new(cards);
            try
            {
                // Match Cascade's order: move every selected instance to Play first.
                foreach (CardModel card in cards)
                {
                    if (!Current()) break;
                    if (card.Owner == player && card.Pile == sourcePile)
                        await CardPileCmd.Add(card, PileType.Play);
                }
                foreach (CardModel card in cards)
                {
                    if (!Current()) break;
                    if (card.Owner != player || card.Pile != PileType.Play.GetPile(player)) continue;
                    pending.Remove(card);
                    await CardCmd.AutoPlay(context, card, target: null);
                }
            }
            finally
            {
                // If the source power expires, restore cards that never started playing.
                foreach (CardModel card in pending)
                {
                    if (!InCombat()) break;
                    if (card.Owner == player && card.Pile == PileType.Play.GetPile(player))
                        await CardPileCmd.Add(card, sourcePile);
                }
            }
        }
        finally
        {
            if (ownsChain && ActiveChains.TryGetValue(player, out PlayChain? current)
                && ReferenceEquals(current, activeChain))
                ActiveChains.Remove(player);
        }
    }
}
