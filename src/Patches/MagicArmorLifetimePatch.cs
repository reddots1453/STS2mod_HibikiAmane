using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

// The vanilla counter contract removes zero automatically. Armour is the sole
// exception: zero is a real, visible state until the next loss attempt.
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.ShouldRemoveDueToAmount))]
public static class MagicArmorLifetimePatch
{
    public static void Postfix(PowerModel __instance, ref bool __result)
    {
        bool preserve = false;
        Safe.Run(() => preserve = __instance is MagicArmorPower { Amount: 0 }
            && TransformationCmd.IsTransformed(__instance.Owner),
            nameof(MagicArmorLifetimePatch));
        if (preserve) __result = false;
    }
}
