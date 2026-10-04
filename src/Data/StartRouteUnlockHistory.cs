using System.Text.Json;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib;
using STS2RitsuLib.Utils.Persistence;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Data;

public sealed class StartRouteVictoryRecord
{
    public long StartTime { get; set; }
    public ulong PlayerId { get; set; }
    public string CharacterCategory { get; set; } = "";
    public int FinalCorruption { get; set; }
    public bool IsVictory { get; set; }
}

public sealed class StartRouteUnlockHistoryState
{
    // Archive flags also preserve players whose old native histories lack corruption.
    public bool CorruptUnlocked { get; set; }
    public bool HolyUnlocked { get; set; }
    public List<StartRouteVictoryRecord> Victories { get; set; } = [];
}

/// <summary>Stable profile-scoped unlock archive and native-history reconciliation.</summary>
internal static class StartRouteUnlockHistory
{
    private const string Key = "route_start_unlock_history";
    private const string FileName = "route_start_unlock_history.json";
    private const string CharacterCategory = "MAIDEN_SUCCUBUS_CHARACTER";
    private static string? _scannedProfilePath;
    private static bool _recovering;
    private static readonly JsonSerializerOptions BackupJson = new() { PropertyNameCaseInsensitive = true };

    internal static void Register() => RitsuLibFramework.GetDataStore(MaidenSuccubusMod.ModId).Register(
        key: Key, fileName: FileName, scope: SaveScope.Profile,
        defaultFactory: () => new StartRouteUnlockHistoryState());

    internal static void RecordVictory(StartRouteVictoryRecord record)
    {
        if (!IsQualifying(record)) return;
        var store = RitsuLibFramework.GetDataStore(MaidenSuccubusMod.ModId);
        store.Modify<StartRouteUnlockHistoryState>(Key, history =>
        {
            bool corrupt = record.FinalCorruption >= 3;
            history.CorruptUnlocked |= corrupt;
            history.HolyUnlocked |= !corrupt;
            history.Victories ??= [];
            // One verified completion for each route suffices forever; keep
            // the first evidence rather than expanding this file each run.
            if (!history.Victories.Any(old => IsQualifying(old)
                    && (old.FinalCorruption >= 3) == corrupt))
                history.Victories.Add(record);
        });
        store.Save(Key);
        MaidenSuccubusMod.Logger.Info(
            $"[StartRoutes] Archived victory: start={record.StartTime}; finalCorruption={record.FinalCorruption}; profile={SaveManager.Instance.CurrentProfileId}.");
    }

    internal static void Recover() => Safe.Run(RecoverCore, "StartRoutes.RecoverHistory");

    private static void RecoverCore()
    {
        if (_recovering || !SaveManager.Instance.IsProfileInitialized) return;
        _recovering = true;
        try
        {
            var store = RitsuLibFramework.GetDataStore(MaidenSuccubusMod.ModId);
            var progress = store.Get<StartUnlockProgressState>(StartUnlockProgress.Key);
            var history = store.Get<StartRouteUnlockHistoryState>(Key);
            string profilePath = ProfileManager.GetBasePath(
                SaveScope.Profile, SaveManager.Instance.CurrentProfileId, MaidenSuccubusMod.ModId);
            bool corrupt = progress.CorruptUnlocked || history.CorruptUnlocked;
            bool holy = progress.HolyUnlocked || history.HolyUnlocked;
            if (_scannedProfilePath != profilePath)
            {
                // OR evidence from backups: an older true flag must never be
                // downgraded merely because a newer valid file says false.
                MergeBackup(profilePath + "/route_start_unlocks.json.backup", ref corrupt, ref holy);
                MergeBackup(profilePath + "/" + FileName + ".backup", ref corrupt, ref holy);
                history.Victories ??= [];
                var evidence = history.Victories.Where(IsQualifying).ToArray();
                var names = SaveManager.Instance.GetAllRunHistoryNames();
                int matched = 0;
                foreach (string file in names)
                {
                    if (!long.TryParse(System.IO.Path.GetFileNameWithoutExtension(file), out long start)) continue;
                    var records = evidence.Where(record => record.StartTime == start).ToArray();
                    if (records.Length == 0) continue;
                    // Use vanilla migration/cloud-aware loader, and match the
                    // local player's recorded identity in multiplayer history.
                    var loaded = SaveManager.Instance.LoadRunHistory(file);
                    if (!loaded.Success || loaded.SaveData is not { Win: true, WasAbandoned: false } run) continue;
                    foreach (var record in records)
                    {
                        if (!run.Players.Any(player => player.Id == record.PlayerId
                                && player.Character.Category == CharacterCategory)) continue;
                        corrupt |= record.FinalCorruption >= 3;
                        holy |= record.FinalCorruption <= -3;
                        matched++;
                    }
                }
                // Evidence was captured at the qualifying victory, so cloud
                // history eviction does not revoke a permanent unlock.
                corrupt |= evidence.Any(record => record.FinalCorruption >= 3);
                holy |= evidence.Any(record => record.FinalCorruption <= -3);
                _scannedProfilePath = profilePath;
                MaidenSuccubusMod.Logger.Info(
                    $"[StartRoutes] History recovery: profile={SaveManager.Instance.CurrentProfileId}; histories={names.Count}; matched={matched}; corrupt={corrupt}; holy={holy}. Legacy histories without final corruption are not inferred.");
            }
            if (history.CorruptUnlocked != corrupt || history.HolyUnlocked != holy)
            {
                store.Modify<StartRouteUnlockHistoryState>(Key, state =>
                {
                    state.CorruptUnlocked |= corrupt;
                    state.HolyUnlocked |= holy;
                });
                store.Save(Key);
            }
            if (progress.CorruptUnlocked != corrupt || progress.HolyUnlocked != holy)
            {
                store.Modify<StartUnlockProgressState>(StartUnlockProgress.Key, state =>
                {
                    state.CorruptUnlocked |= corrupt;
                    state.HolyUnlocked |= holy;
                });
                store.Save(StartUnlockProgress.Key);
            }
        }
        finally { _recovering = false; }
    }

    private static bool IsQualifying(StartRouteVictoryRecord? record) =>
        record is { IsVictory: true } && record.CharacterCategory == CharacterCategory
        && (record.FinalCorruption is >= 3 and <= 5 or >= -5 and <= -3);

    private static void MergeBackup(string path, ref bool corrupt, ref bool holy)
    {
        if (!Godot.FileAccess.FileExists(path)) return;
        try
        {
            var backup = JsonSerializer.Deserialize<StartUnlockProgressState>(
                Godot.FileAccess.GetFileAsString(path), BackupJson);
            if (backup == null) return;
            corrupt |= backup.CorruptUnlocked;
            holy |= backup.HolyUnlocked;
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Warn($"[StartRoutes] Ignoring unreadable unlock backup: {ex.Message}");
        }
    }
}
