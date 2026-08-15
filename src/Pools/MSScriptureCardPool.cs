using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Pools;

/// <summary>
/// Registration-only pool for generated Scripture cards. It is deliberately
/// not exposed by the character's reward-pool list.
/// </summary>
[RegisterSharedCardPool]
public sealed class MSScriptureCardPool : TypeListCardPoolModel
{
    public override string Title => "ms_scripture";
    public override string EnergyColorName => "ironclad";
    public override Color DeckEntryCardColor => new(1.0f, 0.92f, 0.65f);
    public override Color EnergyOutlineColor => new(1.0f, 0.92f, 0.65f);
    public override bool IsColorless => false;
}
