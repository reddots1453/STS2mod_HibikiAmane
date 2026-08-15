using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Random;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Seals;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(
    typeof(Player),
    nameof(Player.PopulateCombatState))]
public static class CombatSealPatch
{
    public static bool Prefix(
        Player __instance,
        Rng __0,
        CombatState __1)
    {
        bool runOriginal = true;
        Safe.Run(
            () =>
            {
                if (__instance.Character is not MaidenSuccubusCharacter)
                {
                    return;
                }

                var playerCombatState = __instance.PlayerCombatState
                    ?? throw new InvalidOperationException(
                        "Player combat state was not initialized before deck population.");
                int sealedCount = 0;
                foreach (var deckCard in __instance.Deck.Cards.ToList())
                {
                    if (__instance.RunState is MegaCrit.Sts2.Core.Runs.RunState runState
                        && CombatSealQuery.IsSealed(runState, deckCard))
                    {
                        sealedCount++;
                        continue;
                    }

                    var combatCard = __1.CloneCard(deckCard);
                    combatCard.DeckVersion = deckCard;
                    playerCombatState.DrawPile.AddInternal(combatCard);
                }

                playerCombatState.DrawPile.RandomizeOrderInternal(
                    __instance,
                    __0,
                    __1);
                runOriginal = false;
                MaidenSuccubusMod.Logger.Info(
                    $"Combat seal snapshot: excluded {sealedCount} deck card(s).");
            },
            nameof(CombatSealPatch));
        return runOriginal;
    }
}
