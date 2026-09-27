using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Bootstrap;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>Extend the existing await, before native ancient dialogue/options; never replay SetupLayout.</summary>
[HarmonyPatch]
public static class FourthRouteOpeningPatch
{
    private static readonly FieldInfo? EventField = ModCompatibility.FindField(typeof(NEventRoom), "_event", typeof(EventModel));
    private static readonly FieldInfo? FinishedField = ModCompatibility.FindField(typeof(NEventRoom), "_isPreFinished", typeof(bool));

    public static MethodBase TargetMethod() => AccessTools.Method(typeof(NEventRoom), "SetupLayout")
        .GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new MissingMethodException("NEventRoom.SetupLayout.MoveNext");

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var original = instructions.ToList();
        List<CodeInstruction>? replacement = null;
        Safe.Run(() =>
        {
            var sites = original.Select((instruction, index) => (instruction, index)).Where(pair =>
                pair.instruction.operand is MethodInfo method && method.DeclaringType == typeof(Cmd)
                && method.Name == nameof(Cmd.Wait) && method.ReturnType == typeof(Task)).ToArray();
            if (sites.Length != 1) throw new InvalidOperationException("Expected exactly one native SetupLayout wait.");
            FieldInfo owner = __originalMethod.DeclaringType!.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(field => field.FieldType == typeof(NEventRoom));
            var result = original.Select(instruction => new CodeInstruction(instruction)).ToList();
            result.InsertRange(sites[0].index + 1,
            [
                new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldfld, owner),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FourthRouteOpeningPatch), nameof(WaitForOpening))),
            ]);
            replacement = result;
        }, "FourthRoute.OpeningAwaitInjection");
        return replacement ?? original;
    }

    internal static async Task WaitForOpening(Task originalWait, NEventRoom room)
    {
        await originalWait;
        if (TestMode.IsOn || EventField?.GetValue(room) is not Neow ancient || FinishedField?.GetValue(room) is not false
            || ancient.Owner is not { } player || !FourthRouteLifecycle.IsEligible(player) || !LocalContext.IsMe(player)
            || player.RunState is not RunState run || run.Players.Count != 1 || run.CurrentActIndex != 0
            || !FourthRouteOpeningService.NeedsOpening(run)) return;
        bool Current() => GodotObject.IsInstanceValid(room) && room.IsInsideTree()
            && ReferenceEquals(NEventRoom.Instance, room) && RunManager.Instance.DebugOnlyGetState() == run;
        while (Current() && NModalContainer.Instance?.OpenModal != null)
            await room.ToSignal(room.GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!Current()) throw new OperationCanceledException("Opening room exited.");
        if (!FourthRouteOpeningService.NeedsOpening(run)) return;
        bool completed;
        try { completed = await FourthRouteOpeningScreen.Show(player, run, Current); }
        catch (Exception ex) when (ex is not OperationCanceledException && Current())
        {
            // Keep the run usable if UI/resource setup fails; the map fallback remains available.
            MaidenSuccubusMod.Logger.Error("[FourthRouteOpening] UI failed; retaining saved choice and native event. " + ex);
            return;
        }
        if (!completed || !Current()) throw new OperationCanceledException("Opening interrupted before ancient rewards.");
    }
}
