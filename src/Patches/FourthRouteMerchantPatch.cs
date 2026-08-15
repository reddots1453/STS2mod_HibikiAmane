using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Data;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant))]
public static class FourthRouteMerchantPatch
{
    private static readonly FieldInfo RelicEntriesField = AccessTools.Field(typeof(MerchantInventory), "_relicEntries");
    [HarmonyPostfix]
    public static void Postfix(MerchantInventory __result)
    {
        Safe.Run(() =>
        {
            if (__result.Player.RunState is not RunState runState) return;
            __result.Player.Relics.OfType<GreedRouteRelic>().FirstOrDefault()?.OnMerchantCreated();
            M5ProgressState state = M5Progress.Handle.Get(runState);
            if (!state.FourthRouteFragmentPending || state.FourthRouteFragmentOffered
                || state.FourthRouteRelicStage != 1) return;
            var entries = (List<MerchantRelicEntry>)RelicEntriesField.GetValue(__result)!;
            MerchantRelicEntry fragment = new(ModelDb.Relic<FourthRouteFragmentRelic>().ToMutable(), __result.Player);
            EventInfo purchaseEvent = AccessTools.Event(typeof(MerchantEntry), nameof(MerchantEntry.PurchaseCompleted));
            MethodInfo updateEntries = AccessTools.Method(typeof(MerchantInventory), "UpdateEntries");
            Delegate handler = Delegate.CreateDelegate(purchaseEvent.EventHandlerType!, __result, updateEntries);
            purchaseEvent.AddEventHandler(fragment, handler);
            if (entries.Count > 0) entries[0] = fragment; else entries.Add(fragment);
            M5Progress.Handle.Modify(runState, data => data.FourthRouteFragmentOffered = true);
        }, "FourthRoute.MerchantFragment");
    }
}
