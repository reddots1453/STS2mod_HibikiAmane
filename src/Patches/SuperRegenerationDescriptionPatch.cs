using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

// DesignDoc places Exhaust before this card's release clause. Move the native
// keyword line; do not print a second keyword or change actual Keywords.
[HarmonyPatch]
internal static class SuperRegenerationDescriptionPatch
{
    private static MethodBase TargetMethod() => AccessTools.GetDeclaredMethods(typeof(CardModel))
        .Single(method => method.Name == nameof(CardModel.GetDescriptionForPile) && method.GetParameters().Length == 3);

    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not SuperRegeneration) return;
        string result = __result;
        Safe.Run(() =>
        {
            string[] ownLines = new LocString("cards", __instance.Id.Entry + ".description").GetFormattedText().Split('\n');
            if (ownLines.Length != 2) return;
            string keyword = "[gold]" + new LocString("card_keywords", "EXHAUST.title").GetFormattedText()
                + "[/gold]" + new LocString("card_keywords", "PERIOD").GetRawText();
            var lines = result.Split('\n').ToList();
            int release = lines.IndexOf(ownLines[1]);
            int exhaust = lines.LastIndexOf(keyword);
            if (release < 0 || exhaust <= release) return;
            lines.RemoveAt(exhaust);
            lines.Insert(release, keyword);
            result = string.Join('\n', lines);
        }, "SuperRegeneration.DescriptionOrder");
        __result = result;
    }
}
