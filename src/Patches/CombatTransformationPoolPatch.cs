using HarmonyLib;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Patches;

/// <summary>Choose the source pool before native rarity/player-count/identity filtering.</summary>
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.GetDefaultTransformationOptions))]
internal static class CombatTransformationPoolPatch
{
    private static readonly Func<CardModel, IEnumerable<CardModel>, bool, CardModel[]> Filter =
        AccessTools.Method(typeof(CardFactory), "GetFilteredTransformationOptions")
            .CreateDelegate<Func<CardModel, IEnumerable<CardModel>, bool, CardModel[]>>();

    [HarmonyPriority(Priority.First)]
    private static bool Prefix(CardModel original, bool isInCombat, ref IEnumerable<CardModel> __result)
    {
        if (!isInCombat) return true;
        IEnumerable<CardModel> candidates;
        if (original.Pool is MSInvasionCursePool pool)
            candidates = pool.GetUnlockedCards(original.Owner.UnlockState, original.Owner.RunState.CardMultiplayerConstraint);
        else if (original.Pool is MSGeneratedCardPool or MSNeutralCardPool or MSCorruptCardPool or MSHolyCardPool)
            candidates = NativeGenerationPoolPatch.GetGenerationCards(ModelDb.CardPool<MSNeutralCardPool>(),
                original.Owner.UnlockState, original.Owner.RunState.CardMultiplayerConstraint);
        else return true;

        __result = Filter(original, candidates, isInCombat);
        return false;
    }
}

