using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

// Diagnostic only: leave the original Tasks, save contents and loader untouched.
// Background observers read cached strings/managed Tasks, never Godot objects.
internal static class ContinueRunDiagnostics
{
    internal sealed class Trace
    {
        internal readonly Stopwatch Clock = Stopwatch.StartNew();
        internal string Stage = "continue entered";
        internal int Done;
    }

    internal sealed class AssetProbe
    {
        internal string Snapshot = "loader has not processed this session";
        internal long SampleTicks;
    }

    private static Trace? _active;
    private static readonly FieldInfo? SaveField = AccessTools.Field(typeof(NMainMenu), "_readRunSaveResult");
    private static readonly ConditionalWeakTable<AssetLoadingSession, AssetProbe> Assets = new();
    internal static Trace? Active => Volatile.Read(ref _active);

    internal static void Begin(NMainMenu menu)
    {
        if (SaveField?.GetValue(menu) is not ReadSaveResult<SerializableRun> { SaveData: { } save }
            || !save.Players.Any(p => p.CharacterId?.Entry.StartsWith("MAIDEN_SUCCUBUS_", StringComparison.Ordinal) == true)) return;
        var trace = new Trace();
        Volatile.Write(ref _active, trace);
        Info("continue entered; character=MaidenSuccubus");
        _ = WatchContinue(trace);
    }

    internal static void Stage(string name)
    {
        if (Active is not { } trace || Volatile.Read(ref trace.Done) != 0) return;
        Volatile.Write(ref trace.Stage, name);
        Info($"stage={name}; elapsedMs={trace.Clock.ElapsedMilliseconds}");
    }

    internal static void Observe(Task task, string name, bool root = false)
    {
        if (Active is not { } trace || Volatile.Read(ref trace.Done) != 0) return;
        _ = ObserveAsync(task, trace, name, root);
    }

    private static async Task ObserveAsync(Task task, Trace trace, string name, bool root)
    {
        try
        {
            await task.ConfigureAwait(false);
            Info($"completed={name}; elapsedMs={trace.Clock.ElapsedMilliseconds}");
        }
        catch (Exception ex)
        {
            Warn($"failed={name}; elapsedMs={trace.Clock.ElapsedMilliseconds}; {ex}");
        }
        finally
        {
            if (root) Interlocked.Exchange(ref trace.Done, 1);
        }
    }

    private static async Task WatchContinue(Trace trace)
    {
        for (int report = 0; report < 20; report++)
        {
            await Task.Delay(30000).ConfigureAwait(false);
            if (Volatile.Read(ref trace.Done) != 0 || !ReferenceEquals(Active, trace)) return;
            Warn($"still waiting; stage={Volatile.Read(ref trace.Stage)}; elapsedMs={trace.Clock.ElapsedMilliseconds}");
        }
    }

    internal static void TrackSession(AssetLoadingSession session, string name)
    {
        if (name != "Common" && Active is not { Done: 0 }) return;
        var probe = Assets.GetValue(session, _ => new AssetProbe());
        Snapshot(session, probe);
        Info($"asset session entered: {name}; {probe.Snapshot}");
        _ = WatchAssets(session.Task, probe, name);
    }

    private static async Task WatchAssets(Task completion, AssetProbe probe, string name)
    {
        for (int report = 0; report < 20; report++)
        {
            await Task.Delay(30000).ConfigureAwait(false);
            if (completion.IsCompleted) return;
            long ticks = Volatile.Read(ref probe.SampleTicks);
            long age = (long)((Stopwatch.GetTimestamp() - ticks) * 1000d / Stopwatch.Frequency);
            Warn($"asset session waiting: {name}; lastProcessAgoMs={age}; {Volatile.Read(ref probe.Snapshot)}");
        }
    }

    internal static void Sample(AssetLoadingSession session)
    {
        if (!Assets.TryGetValue(session, out var probe)) return;
        if (Stopwatch.GetTimestamp() - Volatile.Read(ref probe.SampleTicks) < Stopwatch.Frequency) return;
        Snapshot(session, probe);
    }

    private static readonly string[] QueueNames = ["_toLoad", "_loading", "_finalizing", "_vfxScenes"];
    private static readonly FieldInfo?[] QueueFields = QueueNames.Select(name => AccessTools.Field(typeof(AssetLoadingSession), name)).ToArray();
    private static readonly FieldInfo? VfxField = AccessTools.Field(typeof(AssetLoadingSession), "_currentVfxPath");

    private static void Snapshot(AssetLoadingSession session, AssetProbe probe)
    {
        // Called only from the loader/main thread. Never enumerate its queues
        // from the watchdog; a blocked native Process leaves this last sample.
        var parts = QueueFields.Select((field, index) => field?.GetValue(session) is ICollection queue
            ? $"{QueueNames[index]}={queue.Count}[{string.Join(",", queue.Cast<object>().Take(3))}]"
            : $"{QueueNames[index]}=unavailable");
        Volatile.Write(ref probe.Snapshot, string.Join("; ", parts) + $"; currentVfx={VfxField?.GetValue(session)}");
        Volatile.Write(ref probe.SampleTicks, Stopwatch.GetTimestamp());
    }

    private static void Info(string text) => Safe.Run(() => MaidenSuccubusMod.Logger.Info("[ContinueDiag] " + text), "ContinueDiag.Log");
    private static void Warn(string text) => Safe.Run(() => MaidenSuccubusMod.Logger.Warn("[ContinueDiag] " + text), "ContinueDiag.Log");
}

[HarmonyPatch(typeof(NMainMenu), "OnContinueButtonPressedAsync")]
internal static class MaidenContinueRunDiagnosticsPatch
{
    [HarmonyPrefix]
    private static void Prefix(NMainMenu __instance) => Safe.Run(() => ContinueRunDiagnostics.Begin(__instance), "ContinueDiag.Begin");
    [HarmonyPostfix]
    private static void Postfix(Task __result) => Safe.Run(() => ContinueRunDiagnostics.Observe(__result, "continue including fade-in", root: true), "ContinueDiag.Observe");
}

[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
internal static class MaidenContinueDeserializeDiagnosticsPatch
{
    [HarmonyPrefix]
    private static void Prefix() => ContinueRunDiagnostics.Stage("RunState.FromSerializable entered");
    [HarmonyPostfix]
    private static void Postfix() => ContinueRunDiagnostics.Stage("RunState.FromSerializable completed");
}

[HarmonyPatch]
internal static class MaidenContinueAsyncStageDiagnosticsPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpSavedSingleplayer));
        yield return AccessTools.Method(typeof(SaveManager), nameof(SaveManager.IncrementNumReloads));
        yield return AccessTools.Method(typeof(NGame), nameof(NGame.LoadRun));
        yield return AccessTools.Method(typeof(PreloadManager), nameof(PreloadManager.LoadRunAssets));
        yield return AccessTools.Method(typeof(PreloadManager), nameof(PreloadManager.LoadActAssets));
        yield return AccessTools.Method(typeof(RunManager), nameof(RunManager.GenerateMap));
        yield return AccessTools.Method(typeof(RunManager), nameof(RunManager.LoadIntoLatestMapCoord));
    }
    [HarmonyPrefix]
    private static void Prefix(MethodBase __originalMethod) => ContinueRunDiagnostics.Stage(__originalMethod.Name + " entered");
    [HarmonyPostfix]
    private static void Postfix(MethodBase __originalMethod, Task __result) => Safe.Run(() => ContinueRunDiagnostics.Observe(__result, __originalMethod.Name), "ContinueDiag.Stage");
}

[HarmonyPatch(typeof(AssetLoadingSession), MethodType.Constructor, [typeof(string), typeof(IEnumerable<string>), typeof(System.Collections.Concurrent.ConcurrentDictionary<string, Godot.Resource>), typeof(AssetCache)])]
internal static class MaidenContinueAssetSessionDiagnosticsPatch
{
    [HarmonyPostfix]
    private static void Postfix(AssetLoadingSession __instance, string __0) => Safe.Run(() => ContinueRunDiagnostics.TrackSession(__instance, __0), "ContinueDiag.Assets");
}

[HarmonyPatch(typeof(AssetLoadingSession), nameof(AssetLoadingSession.Process))]
internal static class MaidenContinueAssetProgressDiagnosticsPatch
{
    [HarmonyPrefix]
    private static void Prefix(AssetLoadingSession __instance) => Safe.Run(() => ContinueRunDiagnostics.Sample(__instance), "ContinueDiag.Assets.Sample");
}
