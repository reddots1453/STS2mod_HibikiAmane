using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// Immediately before vanilla divides the hand into retained/discarded cards,
/// grants its one-turn retain flag to the first Portable card still in hand.
/// The flag is cleared by vanilla end-of-turn cleanup and never reaches the
/// permanent deck card.
/// </summary>
[HarmonyPatch]
public static class PortableRetainPatch
{
    public static MethodBase TargetMethod() =>
        typeof(CombatManager)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(method =>
                method.Name == "EndPlayerTurnPhaseTwoInternal"
                && method.GetParameters() is [{ ParameterType.Name: "CombatTurnState" }])
        ?? throw new MissingMethodException(
            typeof(CombatManager).FullName,
            "EndPlayerTurnPhaseTwoInternal(CombatTurnState)");

    public static void Prefix(CombatManager __instance)
    {
        Safe.Run(
            () =>
            {
                CombatState? state = __instance.DebugOnlyGetState();
                if (state == null)
                {
                    return;
                }

                foreach (var player in state.Players)
                {
                    if (player.Character is not MaidenSuccubusCharacter)
                    {
                        continue;
                    }

                    CardModel? portable =
                        PortableHandTracker.GetFirstPortableInHand(player);
                    portable?.GiveSingleTurnRetain();
                }
            },
            nameof(PortableRetainPatch));
    }
}
