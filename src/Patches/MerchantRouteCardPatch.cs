using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(MerchantInventory), "PopulateCharacterCardEntries")]
internal static class MerchantRouteCardPatch
{
    // Native inventory layout, including its two Attack/Skill slots and Power slot.
    private static readonly CardType[] SlotTypes = [CardType.Attack, CardType.Attack, CardType.Skill, CardType.Skill, CardType.Power];

    [HarmonyPrefix]
    private static bool Prefix(MerchantInventory __instance)
    {
        bool populated = false;
        Safe.Run(() => populated = Populate(__instance), nameof(MerchantRouteCardPatch));
        return !populated;
    }

    internal static bool Populate(MerchantInventory inventory)
    {
        var player = inventory.Player;
        if (player.Character is not MaidenSuccubusCharacter || player.RunState is not RunState run
            || inventory.CharacterCardEntries is not List<MerchantCardEntry> entries || entries.Count != 0) return false;
        var pools = new Dictionary<RouteCardKind, CardPoolModel>
        {
            [RouteCardKind.Neutral] = ModelDb.CardPool<MSNeutralCardPool>(),
            [RouteCardKind.Corrupt] = ModelDb.CardPool<MSCorruptCardPool>(),
            [RouteCardKind.Holy] = ModelDb.CardPool<MSHolyCardPool>(),
        }.ToDictionary(pair => pair.Key, pair => pair.Value.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
            .OrderBy(card => card.Id.Entry, StringComparer.Ordinal).ToArray());
        foreach (var pool in pools.Values)
            foreach (var type in SlotTypes.Distinct())
                if (pool.Count(card => card.Type == type) < SlotTypes.Count(slot => slot == type)) return false;
        var bonus = RouteRewardProbabilityModifiers.GetTotal(player);
        var probabilities = RouteRewardProbabilities.Calculate(CorruptionQuery.Get(run), bonus.Holy, bonus.Corrupt);
        var rng = player.PlayerRng.Shops;
        RouteCardKind[] routes = MerchantRoutePlan.Create(probabilities, () => rng.NextFloat(), count => rng.NextInt(count));
        int onSale = rng.NextInt(SlotTypes.Length);
        bool complete = false;
        try
        {
            for (int i = 0; i < SlotTypes.Length; i++)
            {
                var entry = new MerchantCardEntry(player, inventory, pools[routes[i]], SlotTypes[i]);
                entry.Populate(); // Native rarity, price, upgrade, creation hooks and duplicate exclusion.
                if (i == onSale) entry.SetOnSale();
                entries.Add(entry);
            }
            complete = true;
            return true;
        }
        finally { if (!complete) entries.Clear(); } // Only our initially empty list; preserve vanilla fallback.
    }
}
