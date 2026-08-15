using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Pools;

[RegisterSharedCardPool]
public sealed class MSInvasionCursePool : TypeListCardPoolModel
{
    public override string Title => "ms_invasion_curse";
    public override string EnergyColorName => "colorless";
    public override Color DeckEntryCardColor => new(0.45f, 0.15f, 0.48f);
    public override Color EnergyOutlineColor => new(0.85f, 0.45f, 0.9f);
    public override bool IsColorless => true;
}
