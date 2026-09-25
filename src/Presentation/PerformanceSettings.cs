using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Context;
using MaidenSuccubus.Characters;

namespace MaidenSuccubus.Presentation;

public interface IMaidenPerformanceSettings
{
    bool AdultCgEnabled { get; }
    bool AdultAudioEnabled { get; }
    float NormalAudioVolume { get; }
    float AdultAudioVolume { get; }
}

/// <summary>
/// Integration seam for a future settings screen. Adult presentation is
/// deliberately disabled by the fallback until product defaults are approved.
/// </summary>
public static class PerformanceSettings
{
    public static IMaidenPerformanceSettings Current { get; set; } =
        SafePerformanceSettings.Instance;

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
