using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Acts;
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
                    LocString effect = new("relics", routeRelic.Stage == 0
                        ? "MAIDEN_SUCCUBUS_RELIC_FOURTH_ROUTE_STAGE_0.description"
                        : routeRelic.Id.Entry + $".descriptionStage{Math.Min(routeRelic.Stage, 3)}");
                    result = effect;
                    // Ownerless reward previews intentionally reveal effects only,
                    // never the next trial/fragment/sacrifice hint.
                    if (routeRelic.IsMutable && routeRelic.Owner is { } owner)
                    {
                        result = new LocString("relics", routeRelic.Stage == 0
                            ? "MAIDEN_SUCCUBUS_RELIC_FOURTH_ROUTE_PROGRESS.descriptionDormant"
                            : "MAIDEN_SUCCUBUS_RELIC_FOURTH_ROUTE_PROGRESS.description");
                        result.Add("Effect", effect);
                        result.Add("Progress", FourthRouteProgressService.ProgressText(owner, routeRelic.Quest));
                    }
                    return;
                }

                if (__instance is not (TwinSoulChalice or HeroOrb) || !__instance.IsMutable
                    || __instance.Owner.RunState is not RunState runState)
                {
                    return;
                }

                int corruption = CorruptionQuery.Get(runState);
                string suffix = corruption >= 4
                    ? ".descriptionCorrupt"
                    : corruption <= -4
                        ? ".descriptionHoly"
                        : ".description";
                result = new LocString("relics", __instance.Id.Entry + suffix);
            },
            "TwinSoulChalice.DynamicDescription");
        __result = result;
    }
}
