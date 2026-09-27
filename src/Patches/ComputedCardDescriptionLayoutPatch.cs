using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

// Only reorder the native keyword line: keep Exhaust semantics and hover tips.
[HarmonyPatch]
internal static class ComputedCardDescriptionLayoutPatch
{
    private static MethodBase TargetMethod() => AccessTools.GetDeclaredMethods(typeof(CardModel))
        .Single(method => method.Name == nameof(CardModel.GetDescriptionForPile) && method.GetParameters().Length == 3);

    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not (HealingArt or JudgmentBlade)) return;
        string result = __result;
        Safe.Run(() =>
        {
            // Conditional totals can leave an empty line outside combat.
            var lines = result.Split('\n').Where(line => line.Length > 0).ToList();
            result = string.Join('\n', lines);
            if (__instance is not HealingArt) return;
            int preview = lines.FindIndex(line => line.StartsWith("（恢复", StringComparison.Ordinal)
                && line.EndsWith("点生命值）", StringComparison.Ordinal));
            // CardKeywordExtensions is internal in the installed game assembly.
            string keyword = "[gold]" + new LocString("card_keywords", "EXHAUST.title").GetFormattedText()
                + "[/gold]" + new LocString("card_keywords", "PERIOD").GetRawText();
            int exhaust = lines.LastIndexOf(keyword);
            if (preview < 0 || exhaust <= preview) return;
            lines.RemoveAt(exhaust);
            lines.Insert(preview, keyword);
            result = string.Join('\n', lines);
        }, "ComputedCard.DescriptionLayout");
        __result = result;
    }
}
