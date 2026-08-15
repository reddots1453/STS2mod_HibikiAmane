using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunData;
using MaidenSuccubus.Core.Corruption;

namespace MaidenSuccubus.Data;

// 堕落值静态 API
// 用法：Corruption.Get(runState) / Corruption.Modify(runState, +1) / Corruption.IsCorrupt(runState)
public static class Corruption
{
    public const int Min = -5;
    public const int Max = 5;
    public const int Neutral = 0;

    // +3 或更高：堕落路线（无法打出圣洁卡，圣洁卡进入封印区）
    public const int CorruptThreshold = 3;
    // -3 或更低：圣洁路线（无法打出堕落卡，堕落卡进入封印区）
    public const int HolyThreshold = -3;
    // 极端值触发额外效果
    public const int MaxCorrupt = 5;
    public const int MaxHoly = -5;

    // 数据槽位句柄，在 Entry.Init 中注册后赋值
    public static RunSavedData<CorruptionState> Handle = null!;

    public static int Get(RunState runState) => CorruptionQuery.Get(runState);

    public static void Modify(RunState runState, int delta) =>
        CorruptionCmd.Modify(runState, delta);

    public static void Set(RunState runState, int value) =>
        CorruptionCmd.Set(runState, value);

    // 路线判断
    public static bool IsCorrupt(RunState runState) => CorruptionQuery.IsCorrupt(runState);
    public static bool IsHoly(RunState runState) => CorruptionQuery.IsHoly(runState);
    public static bool IsNeutral(RunState runState) => CorruptionQuery.IsNeutral(runState);
    public static bool IsMaxCorrupt(RunState runState) => CorruptionQuery.IsMaxCorrupt(runState);
    public static bool IsMaxHoly(RunState runState) => CorruptionQuery.IsMaxHoly(runState);

    // 每局限一次的行为标记
    public static bool HasTriggeredOnce(RunState runState, string flag) =>
        CorruptionQuery.HasTriggeredOnce(runState, flag);

    // 如果未触发过则触发并返回 true；已触发过返回 false
    public static bool TryTriggerOnce(RunState runState, string flag)
    {
        return CorruptionCmd.TryTriggerOnce(runState, flag);
    }
}
