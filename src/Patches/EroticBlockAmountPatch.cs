using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.addons.mega_text;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

// Vanilla writes value labels only for attack/status intents. Extend that one
// visual refresh path for our numeric defend component, including state changes.
[HarmonyPatch(typeof(NIntent), "UpdateVisuals")]
internal static class EroticBlockAmountPatch
{
    private static readonly FieldInfo Intent = AccessTools.Field(typeof(NIntent), "_intent");
    private static readonly FieldInfo Owner = AccessTools.Field(typeof(NIntent), "_owner");
    private static readonly FieldInfo Targets = AccessTools.Field(typeof(NIntent), "_targets");

    private static bool Prepare() => Intent?.FieldType == typeof(AbstractIntent)
        && Owner?.FieldType == typeof(Creature) && Targets != null;

    [HarmonyPostfix]
    private static void Postfix(NIntent __instance) => Safe.Run(() =>
    {
        if (Intent.GetValue(__instance) is not EroticBlockIntent intent
            || Owner.GetValue(__instance) is not Creature owner) return;
        var targets = Targets.GetValue(__instance) as IEnumerable<Creature> ?? [];
        __instance.GetNode<MegaRichTextLabel>("%Value").Text = intent.GetIntentLabel(targets, owner).GetFormattedText();
    }, nameof(EroticBlockAmountPatch));
}
