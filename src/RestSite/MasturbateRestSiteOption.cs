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

    public override Task<bool> OnSelect() => PerformActionAsync(Owner, prayer: false);

    internal static async Task<bool> PerformActionAsync(Player owner, bool prayer)
    {
        if (Desire.Get(owner) < 5 || owner.RunState is not RunState runState
            || prayer != (CorruptionQuery.Get(runState) <= Corruption.HolyThreshold))
        {
            return false;
        }

        await Desire.Set(owner, Desire.ValueAfterOverflow);
        string onceFlag = prayer
            ? "SYS-CORRUPTION-FIRST-PRAYER"
            : "SYS-CORRUPTION-FIRST-MASTURBATION";
        if (CorruptionCmd.TryTriggerOnce(runState, onceFlag))
        {
            CorruptionCmd.Modify(runState, prayer ? -1 : 1, prayer
                ? CorruptionChangeSource.FirstPrayer
                : CorruptionChangeSource.FirstMasturbation);
        }
        await PerformanceDirector.PlayMasturbationAsync(owner);
        return true;
    }
}
