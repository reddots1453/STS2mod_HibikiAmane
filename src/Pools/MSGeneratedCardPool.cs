using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Pools;

[RegisterSharedCardPool]
public sealed class MSGeneratedCardPool : TypeListCardPoolModel
{
    public override string Title => "ms_generated";
    public override string EnergyColorName => "colorless";
    public override Color DeckEntryCardColor => new(0.65f, 0.55f, 0.72f);
    public override Color EnergyOutlineColor => new(0.85f, 0.55f, 0.9f);
    public override bool IsColorless => true;
}
