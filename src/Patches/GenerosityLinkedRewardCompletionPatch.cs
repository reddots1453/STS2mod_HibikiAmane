using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MaidenSuccubus.Rewards;

namespace MaidenSuccubus.Patches;

/// <summary>
/// The native linked-reward row binds a zero-argument callback to the child's
/// one-argument RewardClaimed signal. Repair only our generosity rows and send
/// the parent's typed signal once; the screen owns its normal removal/closure.
/// </summary>
[HarmonyPatch(typeof(NLinkedRewardSet), "Reload")]
internal static class GenerosityLinkedRewardCompletionPatch
{
    private static readonly MethodInfo NativeCallable = AccessTools.Method(
        typeof(Callable), nameof(Callable.From), [typeof(Action)]);
    private static readonly MethodInfo TypedCallable = AccessTools.Method(
        typeof(GenerosityLinkedRewardCompletionPatch), nameof(CreateClaimCallback));

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
        GenerosityPatchMethods.ReplaceOne(instructions, NativeCallable, TypedCallable,
            "Generosity.TypedRewardClaimed");

    internal static Callable CreateClaimCallback(Action nativeCallback)
    {
        if (nativeCallback.Target is not NLinkedRewardSet node
            || node.LinkedRewardSet is not GenerosityOfferingGroup group)
            return Callable.From(nativeCallback);

        return Callable.From<NRewardButton>(button =>
        {
            if (!group.Resolved || !GodotObject.IsInstanceValid(node)
                || !node.IsInsideTree() || node.IsQueuedForDeletion()) return;
            // Preserve the group skip hook (idempotent for the other choice).
            // Do not call native GetReward: it also calls RewardCollectedFrom
            // directly, then emits the same parent signal without its argument.
            group.OnSkipped();
            node.EmitSignal(NLinkedRewardSet.SignalName.RewardClaimed, node);
            // NRewardsScreen's typed listener removes the row and closes only
            // a completed non-terminal screen. Do not skip a popped rewards set.
            MaidenSuccubusMod.Logger.Info(
                $"[Generosity] Linked choice UI completed: {button.Reward?.GetType().Name ?? "Unknown"}.");
        });
    }
}
