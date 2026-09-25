using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Data;
using MaidenSuccubus.Core.Corruption;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Presentation;

namespace MaidenSuccubus.RestSite;

public sealed class MasturbateRestSiteOption : ModRestSiteOptionTemplate
{
    public override string OptionId =>
        "MAIDEN_SUCCUBUS_MASTURBATE";

    public override RestSiteOptionAssetProfile AssetProfile => new(
        "res://images/ui/rest_site/option_heal.png");

    public override LocString? CustomTitle => new(
        "rest_site_ui",
        "OPTION_MAIDEN_SUCCUBUS_MASTURBATE.name");

    public override LocString Description => new(
        "rest_site_ui",
        "OPTION_MAIDEN_SUCCUBUS_MASTURBATE.description");

    public MasturbateRestSiteOption(Player owner)
        : base(owner)
    {
    }

    public override async Task<bool> OnSelect()
    {
        if (Desire.Get(Owner) < 5)
        {
            return false;
        }

        await Desire.Set(Owner, Desire.ValueAfterOverflow);
        if (Owner.RunState is RunState runState
            && CorruptionCmd.TryTriggerOnce(runState, "SYS-CORRUPTION-FIRST-MASTURBATION"))
        {
            CorruptionCmd.Modify(runState, 1, CorruptionChangeSource.Unknown);
        }
        await PerformanceDirector.PlayMasturbationAsync(Owner);
        return true;
    }
}
