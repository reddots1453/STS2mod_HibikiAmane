#if DEBUG
using System.Text.Json;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>No commands on the live run. Advance only detached act copies.</summary>
public sealed class DesignBlindfoldTestConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_test_blindfold";
    public override string Args => "";
    public override string Description => "Read-only Blindfold preview/locality and detached encounter queue tests";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.Length != 0 || player?.Character is not MaidenSuccubusCharacter
            || !LocalContext.IsMe(player) || player.RunState is not RunState run
            || player.Relics.OfType<Blindfold>().FirstOrDefault() is not { } relic)
            return new CmdResult(false, "Use ms_test_blindfold in a local Maiden run holding Blindfold; no live state will be changed.");
        int checks = 0;
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            checks++;
        }
        string SnapshotActs() => JsonSerializer.Serialize(run.Acts.Select(act => act.ToSave()).ToArray());
        string Rngs() => JsonSerializer.Serialize(run.Rng.ToSerializable()) + JsonSerializer.Serialize(player.PlayerRng.ToSerializable());
        string beforeActs = SnapshotActs(), beforeRngs = Rngs();
        try
        {
            Check(relic.DynamicDescription.GetFormattedText() == "无法看到敌人意图。\n可以看到下一次遭遇战的内容。", "exact formal description");
            Check(BlindfoldPresentation.HasEffect(player), "local owner benefits");
            Check(!BlindfoldPresentation.HasEffect(null), "missing local context cannot benefit");
            Check(!BlindfoldPresentation.HidesIntents(player.Creature), "player intent is unaffected");
            Check(!BlindfoldPresentation.PreviewTips(ModelDb.Relic<Blindfold>()).Any(), "canonical encyclopedia exposes no future");

            // These detached players never join the run or fire acquisition hooks.
            var remote = Player.CreateForNewRun<MaidenSuccubusCharacter>(player.UnlockState, player.NetId ^ 1UL);
            var remoteRelic = ModelDb.Relic<Blindfold>().ToMutable();
            remote.AddRelicInternal(remoteRelic, silent: true);
            Check(!BlindfoldPresentation.HasEffect(remote), "remote owner cannot hide local intents");
            Check(!BlindfoldPresentation.PreviewTips((Blindfold)remoteRelic).Any(), "remote relic preview is private");
            var otherCharacter = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId);
            otherCharacter.AddRelicInternal(ModelDb.Relic<Blindfold>().ToMutable(), silent: true);
            Check(!BlindfoldPresentation.HasEffect(otherCharacter), "other character does not opt in");
            var detached = Player.CreateForNewRun<MaidenSuccubusCharacter>(player.UnlockState, player.NetId);
            Check(!BlindfoldPresentation.HasEffect(detached), "no relic is not active");
            var removed = ModelDb.Relic<Blindfold>().ToMutable();
            detached.AddRelicInternal(removed, silent: true);
            Check(BlindfoldPresentation.HasEffect(detached), "detached local holder recognized");
            detached.RemoveRelicInternal(removed, silent: true);
            Check(!BlindfoldPresentation.HasEffect(detached), "removal ends effect");
            Check(!BlindfoldPresentation.PreviewTips((Blindfold)removed).Any(), "removed relic cannot reveal preview");

            var first = BlindfoldPresentation.PreviewTips(relic).OfType<HoverTip>().ToArray();
            Check(first.Length == 3, "three separately labeled encounter categories");
            Check(first.Select(tip => tip.Title).Distinct().Count() == 3, "category titles distinct");
            for (int repeat = 0; repeat < 10; repeat++)
                Check(first.SequenceEqual(BlindfoldPresentation.PreviewTips(relic).OfType<HoverTip>()), "repeated hover stable");

            foreach (ActModel liveAct in run.Acts)
            {
                Check(BlindfoldPresentation.Peek(liveAct.CanonicalInstance, RoomType.Monster) == null, "canonical act is not a generated queue");
                var initial = liveAct.ToSave();
                foreach (RoomType type in new[] { RoomType.Monster, RoomType.Elite, RoomType.Boss })
                {
                    ActModel copy = ActModel.FromSave(initial);
                    int count = type == RoomType.Monster ? initial.SerializableRooms.NormalEncounterIds.Count
                        : type == RoomType.Elite ? initial.SerializableRooms.EliteEncounterIds.Count : 2;
                    for (int step = 0; step < count + 2; step++)
                    {
                        string before = JsonSerializer.Serialize(copy.ToSave());
                        EncounterModel? preview = BlindfoldPresentation.Peek(copy, type);
                        Check(before == JsonSerializer.Serialize(copy.ToSave()), "peek leaves cloned counters unchanged");
                        if (preview != null)
                        {
                            Check(ReferenceEquals(preview, copy.PullNextEncounter(type)), "preview is actual native next encounter");
                            bool generated = preview.HaveMonstersBeenGenerated;
                            _ = preview.AllPossibleMonsters.ToArray();
                            Check(preview.HaveMonstersBeenGenerated == generated, "hypothetical list does not create combat entities");
                        }
                        ActModel loaded = ActModel.FromSave(copy.ToSave());
                        Check(BlindfoldPresentation.Peek(loaded, type)?.Id == preview?.Id, "save reconstruction preserves preview");
                        copy.MarkRoomVisited(type); // Detached copy only; tests queue wrap and second boss.
                    }
                }
                var emptySave = liveAct.ToSave();
                emptySave.SerializableRooms.NormalEncounterIds.Clear();
                emptySave.SerializableRooms.EliteEncounterIds.Clear();
                emptySave.SerializableRooms.BossId = null;
                emptySave.SerializableRooms.SecondBossId = null;
                ActModel empty = ActModel.FromSave(emptySave);
                foreach (RoomType type in new[] { RoomType.Monster, RoomType.Elite, RoomType.Boss, RoomType.Shop })
                    Check(BlindfoldPresentation.Peek(empty, type) == null, "empty/unsupported category is safe");
            }
            if (player.Creature.CombatState is { } combat)
            {
                foreach (var enemy in combat.Enemies)
                {
                    Check(BlindfoldPresentation.HidesIntents(enemy), "local holder hides enemy intent");
                    Check(enemy.HoverTips.SequenceEqual(BlindfoldPresentation.PowerTips(enemy)), "actual creature hover keeps only power tips");
                }
            }
            Check(beforeActs == SnapshotActs(), "all live encounter lists and counters untouched");
            Check(beforeRngs == Rngs(), "all live run/player random streams untouched");
            string result = $"[DS27BlindfoldTest] PASS {checks} assertions; live state unchanged. Visual pickup/removal and multiplayer UI still require manual acceptance.";
            MaidenSuccubusMod.Logger.Info(result);
            return new CmdResult(true, result);
        }
        catch (Exception ex)
        {
            string result = "[DS27BlindfoldTest] FAIL " + ex.Message;
            MaidenSuccubusMod.Logger.Error(result);
            return new CmdResult(false, result);
        }
    }
}
#endif
