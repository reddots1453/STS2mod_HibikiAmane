using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace MaidenSuccubus.Pools;

// 堕落卡池：欲望/消耗/力量体系，偏向攻杀
[RegisterSharedCardPool]
public class MSCorruptCardPool : TypeListCardPoolModel
{
    public override string Title => "ms_corrupt";
    // 骨架阶段借用 Ironclad 能量图标
    public override string EnergyColorName => "ironclad";

    public override Color DeckEntryCardColor => new(0.72f, 0.28f, 0.58f);
    public override Color EnergyOutlineColor => new(0.72f, 0.28f, 0.58f);

    private static readonly Material? _poolFrameMaterial =
        MaterialUtils.CreateReplaceHueShaderMaterial(0.72f, 0.28f, 0.58f);

    public override Material? PoolFrameMaterial => _poolFrameMaterial;
    public override bool IsColorless => false;
}
