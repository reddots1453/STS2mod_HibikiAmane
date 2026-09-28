using Godot;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MaidenSuccubus.Rewards;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.UI;

public partial class GenerosityOfferingVisibility : Node
{
    public override void _Process(double delta) => Safe.Run(() =>
    {
        if (GetParent() is not NLinkedRewardSet node || node.LinkedRewardSet is not GenerosityOfferingGroup group) return;
        bool available = !group.Resolved && GenerosityOffering.CanOffer(group.Player);
        foreach (var button in node.GetNode<Control>("%RewardContainer").GetChildren().OfType<NRewardButton>())
            if (button.Reward is GenerosityOfferReward) button.Visible = available;
        node.GetNode<Control>("%ChainContainer").Visible = available;
    }, "Generosity.RefreshEligibility");
}
