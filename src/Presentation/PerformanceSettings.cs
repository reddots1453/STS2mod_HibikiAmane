using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Context;
using MaidenSuccubus.Characters;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace MaidenSuccubus.Presentation;

public interface IMaidenPerformanceSettings
{
    bool AdultCgEnabled { get; }
    bool AdultAudioEnabled { get; }
    float NormalAudioVolume { get; }
    float AdultAudioVolume { get; }
}

public sealed class PerformanceSettingsData
{
    public bool AdultCgEnabled { get; set; }
    public bool AdultAudioEnabled { get; set; }
    public int NormalAudioVolumePercent { get; set; } = 75;
    public int AdultAudioVolumePercent { get; set; } = 50;
}

/// <summary>Independent, opt-in presentation settings persisted for the main menu.</summary>
public static class PerformanceSettings
{
    private const string SettingsKey = "performance";

    public static IMaidenPerformanceSettings Current { get; set; } =
        SafePerformanceSettings.Instance;

    public static void Register()
    {
        RitsuLibFramework.GetDataStore(MaidenSuccubusMod.ModId).Register(
            key: SettingsKey, fileName: "performance.json", scope: SaveScope.Global,
            defaultFactory: () => new PerformanceSettingsData());

        var cg = ModSettingsBindings.Global<PerformanceSettingsData, bool>(
            MaidenSuccubusMod.ModId, SettingsKey,
            data => data.AdultCgEnabled, (data, value) => data.AdultCgEnabled = value);
        var audio = ModSettingsBindings.Global<PerformanceSettingsData, bool>(
            MaidenSuccubusMod.ModId, SettingsKey,
            data => data.AdultAudioEnabled, (data, value) => data.AdultAudioEnabled = value);
        var normalVolume = ModSettingsBindings.Global<PerformanceSettingsData, int>(
            MaidenSuccubusMod.ModId, SettingsKey,
            data => data.NormalAudioVolumePercent,
            (data, value) => data.NormalAudioVolumePercent = value);
        var adultVolume = ModSettingsBindings.Global<PerformanceSettingsData, int>(
            MaidenSuccubusMod.ModId, SettingsKey,
            data => data.AdultAudioVolumePercent,
            (data, value) => data.AdultAudioVolumePercent = value);

        ModSettingsRegistry.Register(MaidenSuccubusMod.ModId, page => page
            .AsChildOf(MaidenSuccubusMod.ModId)
            .WithTitle(ModSettingsText.Literal("过场与音频"))
            .WithModDisplayName(ModSettingsText.Literal(MaidenSuccubusMod.ModDisplayName))
            .WithVisibleOnHostSurfaces(ModSettingsHostSurface.All)
            .AddSection("adult_presentation", section => section
                .WithTitle(ModSettingsText.Literal("瑟瑟演出"))
                .AddToggle("adult_cg", ModSettingsText.Literal("播放瑟瑟过场 CG"), cg,
                    ModSettingsText.Literal("关闭时不创建过场画面；战斗与火堆效果照常结算。"))
                .AddToggle("adult_audio", ModSettingsText.Literal("播放瑟瑟演出音频"), audio,
                    ModSettingsText.Literal("关闭时不加载瑟瑟演出音频；变身开始与完成音效不受影响。")))
            .AddSection("volumes", section => section
                .WithTitle(ModSettingsText.Literal("音量"))
                .AddIntSlider("normal_volume", ModSettingsText.Literal("普通演出音量"),
                    normalVolume, 0, 100, 5, value => $"{value}%")
                .AddIntSlider("adult_volume", ModSettingsText.Literal("瑟瑟演出音量"),
                    adultVolume, 0, 100, 5, value => $"{value}%")),
            pageId: SettingsKey);

        Current = PersistedPerformanceSettings.Instance;
    }

    private sealed class PersistedPerformanceSettings : IMaidenPerformanceSettings
    {
        public static readonly PersistedPerformanceSettings Instance = new();

        private static PerformanceSettingsData Data =>
            RitsuLibFramework.GetDataStore(MaidenSuccubusMod.ModId)
                .Get<PerformanceSettingsData>(SettingsKey);

        public bool AdultCgEnabled => Data.AdultCgEnabled;
        public bool AdultAudioEnabled => Data.AdultAudioEnabled;
        public float NormalAudioVolume =>
            Math.Clamp(Data.NormalAudioVolumePercent, 0, 100) / 100f;
        public float AdultAudioVolume =>
            Math.Clamp(Data.AdultAudioVolumePercent, 0, 100) / 100f;
    }

    private sealed class SafePerformanceSettings : IMaidenPerformanceSettings
    {
        public static readonly SafePerformanceSettings Instance = new();
        public bool AdultCgEnabled => false;
        public bool AdultAudioEnabled => false;
        public float NormalAudioVolume => 0.75f;
        public float AdultAudioVolume => 0.5f;
    }
}

internal static class PerformanceAudience
{
    public static bool IsLocalMaiden(Player? player)
    {
        if (player?.Character is not MaidenSuccubusCharacter)
        {
            return false;
        }

        Player? local = LocalContext.GetMe(player.RunState);
        return local != null && local.NetId == player.NetId;
    }
}
