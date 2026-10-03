using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Patches;

// This native hook runs once after each damage settlement, including lethal hits.
// Hook.AfterDamageReceived skips dead targets; it is unsuitable for a damage log.
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageGiven))]
internal static class CombatDamageTextPatch
{
    private static void Prefix(Creature? __2, DamageResult __3)
    {
        if (__2?.Side == CombatSide.Enemy && __3.UnblockedDamage > 0)
            CombatTextFeedback.Notify("damage_received", __3.Receiver, __2,
                amount: __3.UnblockedDamage);
    }
}
