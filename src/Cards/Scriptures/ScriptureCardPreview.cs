using MegaCrit.Sts2.Core.HoverTips;

namespace MaidenSuccubus.Cards.Scriptures;

internal static class ScriptureCardPreview
{
    public static IEnumerable<IHoverTip> All(bool upgraded = false) =>
    [
        HoverTipFactory.FromCard<GuardianScripture>(upgraded),
        HoverTipFactory.FromCard<NimbleScripture>(upgraded),
        HoverTipFactory.FromCard<PunishmentScripture>(upgraded),
        HoverTipFactory.FromCard<WisdomScripture>(upgraded),
        HoverTipFactory.FromCard<VitalityScripture>(upgraded),
        HoverTipFactory.FromCard<BlissScripture>(upgraded),
    ];
}
