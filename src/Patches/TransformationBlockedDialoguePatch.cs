using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch]
public static class TransformationBlockedDialoguePatch
{
    public static MethodBase TargetMethod() => AccessTools.Method(
        "MegaCrit.Sts2.Core.Entities.Cards.UnplayableReasonExtensions:GetPlayerDialogueLine")
        ?? throw new MissingMethodException("UnplayableReasonExtensions.GetPlayerDialogueLine");

    public static void Postfix(UnplayableReason __0, AbstractModel? __1, ref LocString? __result)
    {
        LocString? replacement = null;
        Safe.Run(() =>
        {
            if (__0.HasFlag(UnplayableReason.BlockedByHook)
                && __1 is CardModel card && card is Transform or LightPowerRelease
                && !card.ShouldPlay(card, AutoPlayType.None))
                replacement = new LocString("combat_messages", "MAIDENSUCCUBUS_ALREADY_TRANSFORMED");
        }, nameof(TransformationBlockedDialoguePatch));
        if (replacement != null) __result = replacement;
    }
}
