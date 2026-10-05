using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace MaidenSuccubus.UI;

/// <summary>Static merchant portrait; it does not have a Spine skeleton.</summary>
public sealed partial class MaidenSuccubusMerchantCharacter : NMerchantCharacter
{
    public override void _Ready() => SetProcess(false);
}
