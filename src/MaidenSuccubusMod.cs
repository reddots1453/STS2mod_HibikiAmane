using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.RunData;
using MaidenSuccubus.Data;
using MaidenSuccubus.Bootstrap;
using MaidenSuccubus.Debugging;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Patches;
using MaidenSuccubus.Core.Features;

namespace MaidenSuccubus;

[ModInitializer(nameof(Init))]
public static class MaidenSuccubusMod
{
    public const string ModId = "MaidenSuccubus";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static void Init()
    {
        Logger.Info("Maiden & Succubus v0.1.0 initializing...");
        ModCompatibility.LogAndValidate();

        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        RegisterRunSavedData();
        DesireResource.Register();
        if (MvpFeatureFlags.EnemyIntentExtensions)
        {
            EroticAttackCatalog.Validate();
            Logger.Info(
                $"Erotic intent catalogue validated: "
                + $"{EroticAttackCatalog.All.Count} monsters.");
        }
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        FrameworkSelfTests.Run(Logger);

        var harmony = new Harmony("com.maidensuccubus.sts2");
        var patchCount = 0;
        foreach (var type in assembly.GetTypes())
        {
            bool hasAttr = type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0
                || type.GetMethods().Any(m => m.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);
            if (!hasAttr) continue;
            if (!MvpPatchPolicy.ShouldInstall(type))
            {
                Logger.Info($"MVP feature gate skipped deferred patch: {type.FullName}");
                continue;
            }
            try { harmony.CreateClassProcessor(type).Patch(); patchCount++; }
            catch (Exception ex) { Logger.Warn($"Harmony failed: {type.Name}: {ex.Message}"); }
        }
        if (MvpFeatureFlags.ControlAndInvasion)
        {
            try
            {
                int cardEffects = EscapeEffectPatcher.Install(harmony);
                Logger.Info($"Escape projection patched {cardEffects} concrete card effects.");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Escape projection patching failed: {ex}");
            }
        }
        Logger.Info($"Maiden & Succubus ready — {patchCount} patches applied");
    }

    private static void RegisterRunSavedData()
    {
        using (RitsuLibFramework.BeginModDataRegistration(ModId))
        {
            var store = RitsuLibFramework.GetRunSavedDataStore(ModId);

            Corruption.Handle = store.Register(
                key: "corruption",
                defaultFactory: () => new CorruptionState(),
                options: new RunSavedDataOptions
                {
                    WritePolicy = RunSavedDataWritePolicy.WhenNonDefault,
                    SyncLobbyOnChange = true,
                }
            );

            Desire.Handle = store.Register(
                key: "desire",
                defaultFactory: () => new DesireState(),
                options: new RunSavedDataOptions
                {
                    WritePolicy = RunSavedDataWritePolicy.WhenNonDefault,
                    SyncLobbyOnChange = true,
                }
            );

            M5Progress.Handle = store.Register(
                key: "m5_progress",
                defaultFactory: () => new M5ProgressState(),
                options: new RunSavedDataOptions
                {
                    WritePolicy = RunSavedDataWritePolicy.WhenNonDefault,
                    SyncLobbyOnChange = true,
                }
            );
        }
        Logger.Info("RunSavedData registered: corruption, desire, m5_progress");
    }
}
