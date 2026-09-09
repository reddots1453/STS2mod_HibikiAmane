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
using MaidenSuccubus.Keywords;
using STS2RitsuLib.Keywords;
#if DEBUG
using MaidenSuccubus.Debugging.CardEffects;
#endif

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
        RegisterKeywords();
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
#if DEBUG
        CardEffectTestHotkey.Register();
#endif

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
            Logger.Info("Escape projection uses RitsuLib's instance-scoped card OnPlay hook.");
        Logger.Info($"Maiden & Succubus ready — {patchCount} patches applied");
    }

    private static void RegisterKeywords()
    {
        // RitsuLib's attribute scanner skips static classes.  All keyword
        // holders in this mod are intentionally static, so register their
        // definitions explicitly before type discovery freezes the registry.
        ModKeywordRegistry registry = ModKeywordRegistry.For(ModId);
        registry.RegisterCardKeywordOwnedByLocNamespace(
            PortableKeyword.LocalStem,
            iconPath: null,
            ModKeywordCardDescriptionPlacement.AfterCardDescription,
            includeInCardHoverTip: true);
        registry.RegisterCardKeywordOwnedByLocNamespace(
            SinkingKeyword.LocalStem,
            iconPath: null,
            ModKeywordCardDescriptionPlacement.AfterCardDescription,
            includeInCardHoverTip: true);
        registry.RegisterCardKeywordOwnedByLocNamespace(
            BattleReplayKeyword.LocalStem,
            iconPath: null,
            ModKeywordCardDescriptionPlacement.None,
            includeInCardHoverTip: true);
        registry.RegisterCardKeywordOwnedByLocNamespace(
            CurseInfectionKeyword.LocalStem,
            iconPath: null,
            ModKeywordCardDescriptionPlacement.None,
            includeInCardHoverTip: true);
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
