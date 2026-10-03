using MaidenSuccubus.Characters.Starts;
using STS2RitsuLib;
using STS2RitsuLib.Utils.Persistence;

namespace MaidenSuccubus.Data;

public sealed class StartUnlockProgressState
{
    public bool CorruptUnlocked { get; set; }
    public bool HolyUnlocked { get; set; }
}

/// <summary>Profile progression, independent of the current run and lobby.</summary>
internal static class StartUnlockProgress
{
    private const string Key = "route_start_unlocks";
    internal static void Register() => RitsuLibFramework.GetDataStore(MaidenSuccubusMod.ModId).Register(
        key: Key, fileName: "route_start_unlocks.json", scope: SaveScope.Profile,
        defaultFactory: () => new StartUnlockProgressState());

    internal static MaidenSuccubusStartProfileId Normalize(MaidenSuccubusStartProfileId route) => route switch
    {
        MaidenSuccubusStartProfileId.HolyMaiden or MaidenSuccubusStartProfileId.Succubus => route,
        _ => MaidenSuccubusStartProfileId.Normal,
    };

    internal static int InitialValue(MaidenSuccubusStartProfileId route) => Normalize(route) switch
    {
        MaidenSuccubusStartProfileId.HolyMaiden => -3,
        MaidenSuccubusStartProfileId.Succubus => 3,
        _ => 0,
    };

    internal static bool IsUnlocked(MaidenSuccubusStartProfileId route)
    {
        if (Normalize(route) == MaidenSuccubusStartProfileId.Normal) return true;
        var progress = RitsuLibFramework.GetDataStore(MaidenSuccubusMod.ModId).Get<StartUnlockProgressState>(Key);
        return route == MaidenSuccubusStartProfileId.Succubus ? progress.CorruptUnlocked : progress.HolyUnlocked;
    }

    internal static MaidenSuccubusStartProfileId Next(MaidenSuccubusStartProfileId route, int direction)
    {
        MaidenSuccubusStartProfileId[] order = [MaidenSuccubusStartProfileId.Normal,
            MaidenSuccubusStartProfileId.Succubus, MaidenSuccubusStartProfileId.HolyMaiden];
        int index = Array.IndexOf(order, Normalize(route));
        for (int i = 1; i <= order.Length; i++)
        {
            var next = order[(index + direction * i + order.Length * 2) % order.Length];
            if (IsUnlocked(next)) return next;
        }
        return MaidenSuccubusStartProfileId.Normal;
    }

    internal static void RecordVictory(int corruption)
    {
        var route = corruption >= 3 ? MaidenSuccubusStartProfileId.Succubus
            : corruption <= -3 ? MaidenSuccubusStartProfileId.HolyMaiden : MaidenSuccubusStartProfileId.Normal;
        if (route == MaidenSuccubusStartProfileId.Normal) return;
        var store = RitsuLibFramework.GetDataStore(MaidenSuccubusMod.ModId);
        bool newlyUnlocked = !IsUnlocked(route);
        if (newlyUnlocked)
        {
            store.Modify<StartUnlockProgressState>(Key, progress =>
            {
                if (route == MaidenSuccubusStartProfileId.Succubus) progress.CorruptUnlocked = true;
                else progress.HolyUnlocked = true;
            });
        }
        // Modify only updates memory. Persist before the player can quit, and
        // retry on later qualifying victories even if memory is already unlocked.
        store.Save(Key);
        MaidenSuccubusMod.Logger.Info(
            $"[StartRoutes] {(newlyUnlocked ? "Unlocked" : "Retained")} {route} after victory at corruption {corruption}; profile save requested.");
    }
}
