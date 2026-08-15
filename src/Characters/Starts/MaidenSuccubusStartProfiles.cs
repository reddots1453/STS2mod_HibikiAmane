using Godot;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.Characters.Starts;

public enum MaidenSuccubusStartProfileId
{
    Normal,
    HolyMaiden,
    Succubus,
}

public interface IMaidenSuccubusStartProfile
{
    MaidenSuccubusStartProfileId StartProfileId { get; }
    int InitialCorruption { get; }
    CharacterAssetProfile StartAssetProfile { get; }
}

public interface IStartUnlockPolicy
{
    bool IsUnlocked(MaidenSuccubusStartProfileId profile, UnlockState unlockState);
}

public sealed class DebugAllStartsUnlockedPolicy : IStartUnlockPolicy
{
    public bool IsUnlocked(
        MaidenSuccubusStartProfileId profile,
        UnlockState unlockState) => true;
}

public static class MaidenSuccubusStartRegistry
{
    public static IStartUnlockPolicy UnlockPolicy { get; set; } =
        new DebugAllStartsUnlockedPolicy();

    // 已达到可实施状态的初始遗物候选。START-002 的最终选择界面仍属 OPEN；
    // 在界面落地前，角色继续使用 TwinSoulChalice 作为默认遗物。
    public static IReadOnlyList<Type> StartingRelicOptions { get; } =
        [typeof(TwinSoulChalice), typeof(BalancedLens)];
}

[RegisterCharacter]
public sealed class HolyMaidenCharacter : MaidenSuccubusCharacter
{
    public override MaidenSuccubusStartProfileId StartProfileId =>
        MaidenSuccubusStartProfileId.HolyMaiden;
    public override int InitialCorruption => -3;
    public override Color NameColor => new(0.82f, 0.9f, 1f);
    public override Color EnergyLabelOutlineColor => NameColor;
    public override Color MapDrawingColor => NameColor;
}

[RegisterCharacter]
public sealed class SuccubusCharacter : MaidenSuccubusCharacter
{
    public override MaidenSuccubusStartProfileId StartProfileId =>
        MaidenSuccubusStartProfileId.Succubus;
    public override int InitialCorruption => 3;
    public override Color NameColor => new(0.95f, 0.35f, 0.65f);
    public override Color EnergyLabelOutlineColor => NameColor;
    public override Color MapDrawingColor => NameColor;
}
