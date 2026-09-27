using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using MaidenSuccubus.Core.Routes;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace MaidenSuccubus.Pools;

// 中立卡池：所有路线通用的过渡数值牌
public class MSNeutralCardPool : TypeListCardPoolModel
{
    protected override IEnumerable<CardModel> FilterThroughEpochs(
        UnlockState unlockState, IEnumerable<CardModel> cards) =>
        RetiredCardCatalog.Obtainable(base.FilterThroughEpochs(unlockState, cards));

    public override string Title => "ms_neutral";
    // 骨架阶段借用 Ironclad 能量图标，避免引用不存在的资源导致 NullReferenceException
    public override string EnergyColorName => "ironclad";

    public override Color DeckEntryCardColor => new(0.85f, 0.85f, 0.88f);
    public override Color EnergyOutlineColor => new(0.85f, 0.85f, 0.88f);

    private static readonly Material? _poolFrameMaterial =
        MaterialUtils.CreateReplaceHueShaderMaterial(0.85f, 0.85f, 0.88f);

    public override Material? PoolFrameMaterial => _poolFrameMaterial;
    public override bool IsColorless => false;
}
