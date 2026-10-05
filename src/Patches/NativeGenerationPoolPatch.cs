using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Patches;

/// <summary>Expand only native generation/transform call sites, never the reward,
/// event, merchant or per-route pool APIs. Native downstream filters stay intact.</summary>
[HarmonyPatch]
internal static class NativeGenerationPoolPatch
{
    private static readonly MethodInfo GetUnlocked = AccessTools.Method(typeof(CardPoolModel),
        nameof(CardPoolModel.GetUnlockedCards));
    private static IEnumerable<MethodBase> TargetMethods()
    {
        HashSet<MethodBase> seen = [];
        foreach (Type type in typeof(CardModel).Assembly.GetTypes().Where(type =>
            IsGenerationOwner(type) || type == typeof(CardFactory)))
        foreach (MethodInfo method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            if (type == typeof(CardFactory) && method.Name != nameof(CardFactory.GetDefaultTransformationOptions)) continue;
            MethodInfo? body = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
                .GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ?? method;
            if (body.ContainsGenericParameters || !seen.Add(body)) continue;
            if (body.GetMethodBody() != null && PatchProcessor.GetOriginalInstructions(body)
                .Any(code => code.Calls(GetUnlocked))) yield return body;
        }
    }
    private static bool IsGenerationOwner(Type type)
    {
        // LINQ lambdas (e.g. Splash's SelectMany) live on nested compiler types.
        for (Type? owner = type; owner != null; owner = owner.DeclaringType)
            if (typeof(CardModel).IsAssignableFrom(owner) || typeof(PotionModel).IsAssignableFrom(owner)
                || typeof(RelicModel).IsAssignableFrom(owner) || typeof(PowerModel).IsAssignableFrom(owner)) return true;
        return false;
    }
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction code in instructions)
        {
            if (code.Calls(GetUnlocked))
            {
                // The native factory chooses the pool first, including Colorless fallback.
                // Expand only a selected Maiden route pool, never derivative or special pools.
                code.opcode = OpCodes.Call;
                code.operand = AccessTools.Method(typeof(NativeGenerationPoolPatch), nameof(GetGenerationCards));
            }
            yield return code;
        }
    }
    internal static IEnumerable<CardModel> GetGenerationCards(CardPoolModel pool, UnlockState unlocks,
        CardMultiplayerConstraint multiplayer)
    {
        if (pool is not (MSNeutralCardPool or MSCorruptCardPool or MSHolyCardPool))
            return pool.GetUnlockedCards(unlocks, multiplayer);
        return AllMaidenSuccubusCards.Pools.SelectMany(route => route.GetUnlockedCards(unlocks, multiplayer))
            .Where(card => !RetiredCardCatalog.IsRetired(card))
            .DistinctBy(card => card.Id).OrderBy(card => card.Id.Entry, StringComparer.Ordinal).ToArray();
    }
}
