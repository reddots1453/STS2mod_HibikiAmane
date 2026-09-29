namespace MaidenSuccubus.Data;

// 堕落值状态：单局全局共享，跨战斗持久化
// 范围 -5（圣洁）到 +5（堕落），0 为中立
public sealed class CorruptionState
{
    public int Value { get; set; } = 0;

    // 初始为处女状态；首次侵犯结算后永久失去。
    public bool VirginMark { get; set; } = true;

    // 每局限一次的行为标记（如"失去纯洁印记获得堕落值"等战斗行为）
    public HashSet<string> TriggeredOnceFlags { get; set; } = new();

    // Fixed second unknown-room appointment in Acts 2 and 3 (1-based act number).
    public int MassageAppointmentAct { get; set; }
    public int ActTwoUnknownRoomsVisited { get; set; }
    public int ActThreeUnknownRoomsVisited { get; set; }
}
