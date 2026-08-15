using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace MaidenSuccubus.Pools;

// 圣洁卡池：断罪/圣言/净化体系，偏向防杀
[RegisterSharedCardPool]
public class MSHolyCardPool : TypeListCardPoolModel
{
    public override string Title => "ms_holy";
    // 骨架阶段借用 Ironclad 能量图标
    public override string EnergyColorName => "ironclad";

    public override Color DeckEntryCardColor => new(1.0f, 0.92f, 0.65f);
    public override Color EnergyOutlineColor => new(1.0f, 0.92f, 0.65f);

    private static readonly Material? _poolFrameMaterial =
        MaterialUtils.CreateReplaceHueShaderMaterial(1.0f, 0.92f, 0.65f);

    public override Material? PoolFrameMaterial => _poolFrameMaterial;
    public override bool IsColorless => false;
}
