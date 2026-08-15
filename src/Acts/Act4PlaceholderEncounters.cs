using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Acts;

public abstract class Act4PlaceholderBoss : ModEncounterTemplate
{
    // Until route-specific boss art exists, reuse the Queen's real assets. The
    // scaffolding default derives paths from this placeholder's ID, which made
    // every combat preload six nonexistent run-history textures.
    public override string? CustomRunHistoryIconPath =>
        "res://images/ui/run_history/queen_boss.png";
    public override string? CustomRunHistoryIconOutlinePath =>
        "res://images/ui/run_history/queen_boss_outline.png";

    public override RoomType RoomType => RoomType.Boss;
    public override bool ShouldGiveRewards => false;
    protected override bool UseActCombatBackground => true;
    public override string BossNodePath =>
        ModelDb.Encounter<QueenBoss>().BossNodePath;
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [ModelDb.Monster<DevotedSculptor>()];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        [(ModelDb.Monster<DevotedSculptor>().ToMutable(), null)];
}

[RegisterActEncounter(typeof(MaidenSuccubusFourthAct))]
public sealed class HolyAct4PlaceholderBoss : Act4PlaceholderBoss;

[RegisterActEncounter(typeof(MaidenSuccubusFourthAct))]
public sealed class NeutralAct4PlaceholderBoss : Act4PlaceholderBoss;

[RegisterActEncounter(typeof(MaidenSuccubusFourthAct))]
public sealed class CorruptAct4PlaceholderBoss : Act4PlaceholderBoss;
