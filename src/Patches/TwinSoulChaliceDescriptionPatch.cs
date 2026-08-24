using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

/// <summary>
/// RelicModel.Description is not virtual in the base game. Select the active
/// text at the getter boundary so every relic UI receives the same branch and
/// DynamicDescription can still inject dynamic variables normally.
/// </summary>
[HarmonyPatch(typeof(RelicModel), "get_Description")]
public static class TwinSoulChaliceDescriptionPatch
{
    [HarmonyPostfix]
    public static void Postfix(RelicModel __instance, ref LocString __result)
    {
        LocString result = __result;
        Safe.Run(
            () =>
            {
                if (__instance is FourthRouteRelic routeRelic)
                {
                    result = new LocString(
                        "relics",
                        routeRelic.Id.Entry + $".descriptionStage{routeRelic.Stage}");
                    return;
                }

                if (__instance is not TwinSoulChalice chalice || !chalice.IsMutable
                    || chalice.Owner.RunState is not RunState runState)
                {
                    return;
                }

                int corruption = CorruptionQuery.Get(runState);
                string suffix = corruption >= 4
                    ? ".descriptionCorrupt"
                    : corruption <= -4
                        ? ".descriptionHoly"
                        : ".description";
                result = new LocString("relics", chalice.Id.Entry + suffix);
            },
            "TwinSoulChalice.DynamicDescription");
        __result = result;
    }
}
