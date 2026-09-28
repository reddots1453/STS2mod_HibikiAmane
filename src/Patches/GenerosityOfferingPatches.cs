using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Rewards;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.GenerateWithoutOffering))]
public static class GenerosityCombatRewardPatch
{
    [HarmonyPostfix]
    public static void Postfix(RewardsSet __instance, ref Task __result)
    {
        Task original = __result;
        Safe.Run(() => { original = After(original, __instance); }, "Generosity.WrapRewards");
        __result = original;
    }
    private static async Task After(Task original, RewardsSet set)
    {
        await original;
        Safe.Run(() => GenerosityOffering.WrapCombatRewards(set), "Generosity.WrapPopulatedRewards");
    }
}

internal static class GenerosityPatchMethods
{
    internal static MethodBase AsyncBody(Type type, string method) => AccessTools.Method(type, method)
        .GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType.GetMethod("MoveNext",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new MissingMethodException(type.FullName, method + ".MoveNext");

    internal static IEnumerable<CodeInstruction> ReplaceOne(IEnumerable<CodeInstruction> instructions,
        MethodInfo source, MethodInfo replacement, string operation)
    {
        var original = instructions.ToList();
        List<CodeInstruction>? result = null;
        Safe.Run(() =>
        {
            if (original.Count(code => code.Calls(source)) != 1)
                throw new InvalidOperationException("Expected exactly one native call: " + source);
            result = original.Select(code => new CodeInstruction(code)).ToList();
            var target = result.Single(code => code.Calls(source));
            target.opcode = OpCodes.Call;
            target.operand = replacement;
        }, operation);
        return result ?? original;
    }
}

// Leave vote/RPS/random allocation untouched; change only its final obtain call.
[HarmonyPatch]
public static class GenerosityTreasureObtainPatch
{
    public static MethodBase TargetMethod() => GenerosityPatchMethods.AsyncBody(typeof(NTreasureRoomRelicCollection), "AnimateRelicAwards");
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
        GenerosityPatchMethods.ReplaceOne(instructions,
            AccessTools.Method(typeof(RelicCmd), nameof(RelicCmd.Obtain), [typeof(RelicModel), typeof(Player), typeof(int)]),
            AccessTools.Method(typeof(GenerosityOffering), nameof(GenerosityOffering.ObtainAllocated)),
            "Generosity.TreasureObtainSite");
}

[HarmonyPatch]
public static class GenerosityPendingTreasureAnimationPatch
{
    public static IEnumerable<MethodBase> TargetMethods() => AccessTools.GetDeclaredMethods(typeof(NRelicInventory))
        .Where(method => method.Name == "AnimateRelic" && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(RelicModel));
    [HarmonyPrefix]
    public static bool Prefix(RelicModel __0)
    {
        bool show = true;
        Safe.Run(() => show = !GenerosityOffering.IsPendingTreasure(__0), "Generosity.DelayObtainAnimation");
        return show;
    }
}

// Keep native RewardSelectedMessage and its room/set/player validation. Only our
// child rewards use the reserved index range; all other rewards keep native indexes.
[HarmonyPatch]
public static class GenerosityLocalRewardIndexPatch
{
    public static MethodBase TargetMethod() => GenerosityPatchMethods.AsyncBody(typeof(RewardsSetSynchronizer), "SelectLocalReward");
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
        GenerosityPatchMethods.ReplaceOne(instructions,
            AccessTools.Method(typeof(List<Reward>), nameof(List<Reward>.IndexOf), [typeof(Reward)]),
            AccessTools.Method(typeof(GenerosityLocalRewardIndexPatch), nameof(IndexOf)), "Generosity.LocalRewardIndex");

    public static int IndexOf(List<Reward> rewards, Reward reward)
    {
        int result = rewards.IndexOf(reward);
        Safe.Run(() =>
        {
            if (reward.ParentRewardSet is not GenerosityOfferingGroup group) return;
            int parent = rewards.IndexOf(group);
            int child = Array.IndexOf(group.Choices, reward);
            if (parent >= 0 && child >= 0) result = GenerosityOfferingRules.Encode(parent, child);
        }, "Generosity.EncodeChildIndex");
        return result;
    }
}

[HarmonyPatch(typeof(RewardsSetSynchronizer), "SelectRewardForPlayer", [typeof(Player), typeof(int)])]
public static class GenerosityRemoteRewardIndexPatch
{
    [HarmonyPrefix]
    public static bool Prefix(RewardsSetSynchronizer __instance, Player player, int rewardIndex, ref Task __result)
    {
        if (rewardIndex < GenerosityOfferingRules.ChildIndexBase) return true;
        Task resolved = Task.CompletedTask;
        Safe.Run(() =>
        {
            object state = AccessTools.Method(typeof(RewardsSetSynchronizer), "GetRewardStateForPlayer").Invoke(__instance, [player])!;
            var stack = (IList)AccessTools.Field(state.GetType(), "rewardsStack").GetValue(state)!;
            if (stack.Count == 0) return;
            object current = stack[stack.Count - 1]!;
            var set = (RewardsSet)AccessTools.Field(current.GetType(), "set").GetValue(current)!;
            if (!GenerosityOfferingRules.TryDecode(rewardIndex, set.Rewards.Count, out int parent, out int child)
                || set.Player != player || set.Rewards[parent] is not GenerosityOfferingGroup group) return;
            // This native overload completes the parent's rewards set and preserves native hooks.
            resolved = (Task)AccessTools.Method(typeof(RewardsSetSynchronizer), "SelectRewardForPlayer",
                [current.GetType(), typeof(Reward)]).Invoke(__instance, [current, group.Choices[child]])!;
        }, "Generosity.ResolveRemoteChild");
        __result = resolved;
        return false;
    }
}

[HarmonyPatch(typeof(RewardsSetSynchronizer), nameof(RewardsSetSynchronizer.HandleRewardSelectedMessage))]
public static class GenerosityCompletedMessagePatch
{
    [HarmonyPrefix]
    public static bool Prefix(RewardsSetSynchronizer __instance, RewardSelectedMessage __0, ulong __1)
    {
        bool proceed = true;
        Safe.Run(() =>
        {
            if (__0.rewardIndex < GenerosityOfferingRules.ChildIndexBase) return;
            var collection = (IPlayerCollection)AccessTools.Field(typeof(RewardsSetSynchronizer), "_playerCollection").GetValue(__instance)!;
            var player = collection.GetPlayer(__1);
            proceed = player != null && !__instance.IsRewardsSetCompleted(player, __0.setId);
        }, "Generosity.RejectCompletedMessage");
        return proceed;
    }
}

[HarmonyPatch(typeof(NLinkedRewardSet), nameof(NLinkedRewardSet._Ready))]
public static class GenerosityOfferingVisibilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(NLinkedRewardSet __instance) => Safe.Run(() =>
    {
        if (__instance.LinkedRewardSet is GenerosityOfferingGroup)
            __instance.AddChild(new GenerosityOfferingVisibility());
    }, "Generosity.AttachVisibility");
}
