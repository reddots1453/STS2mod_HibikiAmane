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
using MaidenSuccubus.UI;
using MaidenSuccubus.Localization;
using MaidenSuccubus.Presentation;
using MaidenSuccubus.Telemetry;
#if DEBUG
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Debugging.ControlIntents;
#endif

namespace MaidenSuccubus;

[ModInitializer(nameof(Init))]
public static class MaidenSuccubusMod
{
    public const string ModId = "MaidenSuccubus";
    public const string ModDisplayName = "HibikiAmane";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static void Init()
    {
        Logger.Info($"{ModDisplayName} v0.1.0 initializing...");
        ModCompatibility.LogAndValidate();

        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        PowerIconAssets.Register();
        MaidenEnergyIconAssets.Register();
        MaidenDesireIconAssets.Register();
        MaidenLocalizationFormatters.Register();
        RegisterRunSavedData();
        Util.Safe.Run(StartUnlockProgress.Register, "StartRoutes.RegisterProgress");
        Util.Safe.Run(Acts.GoddessTrialMode.Register, "GoddessTrial.RegisterSettings");
        Util.Safe.Run(PerformanceSettings.Register, "Performance.RegisterSettings");
        Util.Safe.Run(MaidenTelemetry.Register, "Telemetry.RegisterApplicant");
        RegisterKeywords();
        DesireResource.Register();
        DesirePersistenceCoordinator.Initialize();
        PerformanceAudioService.Initialize();
        CorruptionChangeFeedback.Initialize();
        Util.Safe.Run(CombatTextFeedback.Initialize, "CombatFeedback.Initialize");
        if (MvpFeatureFlags.EnemyIntentExtensions)
        {
            EroticAttackCatalog.Validate();
            Logger.Info(
                $"Erotic intent catalogue validated: "
                + $"{EroticAttackCatalog.All.Count} monsters.");
        }
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        // CharacterModel overrides are not vanilla hook listeners by default.
        // RunState already includes the combat listener stream when in combat;
        // subscribing only here avoids executing every combat hook twice.
        ModHelper.SubscribeForCombatStateHooks(ModId, combat =>
            combat.Players
                .Where(player => player.IsActiveForHooks)
                .Select(player => player.Character)
                .OfType<Characters.MaidenSuccubusCharacter>()
                .Distinct());
        Logger.Info("Character combat hooks subscribed through ModHelper.");
        // Run callbacks include combat callbacks where needed. This separate
        // per-player tracker survives starter replacement without double hooks.
        ModHelper.SubscribeForRunStateHooks(ModId + ".FourthRoute", Acts.FourthRouteLifecycle.Listeners);
        ModHelper.SubscribeForRunStateHooks(ModId + ".CorruptionAct", Acts.CorruptionActLifecycle.Listeners);
        RunFrameworkSelfTestsWithoutBlockingInitialization();
#if DEBUG
        CardEffectTestHotkey.Register();
        ControlIntentTestHotkey.Register();
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
            catch (Exception ex) { Logger.Warn($"Harmony failed: {type.Name}: {ex}"); }
        }
        if (MvpFeatureFlags.ControlAndInvasion)
            Logger.Info(
                "Escape projection uses a RitsuLib capability attached to each controlled card instance.");
        Logger.Info($"{ModDisplayName} ready — {patchCount} patches applied");
    }

    private static void RunFrameworkSelfTestsWithoutBlockingInitialization()
    {
        try
        {
            FrameworkSelfTests.Run(Logger);
        }
        catch (Exception ex)
        {
            // Debug builds are deployed for manual gameplay testing. A stale
            // assertion must remain visible, but must not leave the mod in a
            // half-initialized state with all later Harmony patches missing.
            Logger.Error(
                "Framework self-tests failed; continuing mod initialization so "
                + $"runtime diagnostics remain available. {ex}");
        }
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

            Desire.AmountHandle = store.RegisterPerPlayer(
                key: "desire_amount",
                defaultFactory: () => new DesireAmountState(),
                options: new RunSavedDataOptions
                {
                    WritePolicy = RunSavedDataWritePolicy.WhenNonDefault,
                    SyncLobbyOnChange = true,
                }
            );

            StarterRelicChoice.Handle = store.RegisterPerPlayer(
                key: "starter_relic_choice",
                defaultFactory: () => new StarterRelicChoiceState(),
                options: new RunSavedDataOptions
                {
                    WritePolicy = RunSavedDataWritePolicy.WhenNonDefault,
                    SyncLobbyOnChange = true,
                });

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
        Logger.Info(
            "RunSavedData registered: corruption, desire, desire_amount, "
            + "m5_progress, starter_relic_choice");
    }
}
