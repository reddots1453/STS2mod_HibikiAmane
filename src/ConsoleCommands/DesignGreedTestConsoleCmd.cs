#if DEBUG
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Patches;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignGreedTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_greed";
    public override string Args => "confirm";
    public override string Description => "Destructive Greed shop tests; disposable run only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (_running || issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState.Players.Count != 1 || CombatManager.Instance.IsInProgress
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_greed confirm outside combat in a disposable single-player Maiden run.");
        return new CmdResult(Run(issuingPlayer), true, "Destructive tests started; see [DS27GreedTest].");
    }

    // Temporary test-only provider, not a production event implementation.
    public static bool ReserveEvent(ref RoomType __result)
    {
        RoomType result = __result;
        Safe.Run(() => result = RoomType.Event, "GreedTest.ReserveEvent");
        __result = result;
        return false;
    }

    private static async Task Run(Player player)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        var run = (RunState)player.RunState;
        int floorBefore = run.ActFloor;
        int roomCountBefore = run.CurrentRoomCount;
        var roll = AccessTools.Method(typeof(RunManager), "RollRoomTypeFor");
        var create = AccessTools.Method(typeof(RunManager), "CreateRoom");
        var price = AccessTools.Method(typeof(Hook), nameof(Hook.ModifyMerchantPrice));
        var reservation = new Harmony("MaidenSuccubus.Debug.GreedReservation");
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 greed: " + name);
            checks++;
        }
        RoomType Roll(MapPointType point) => (RoomType)roll.Invoke(RunManager.Instance, [point, Array.Empty<RoomType>()])!;
        MerchantRoom CreateShop(MapPointType point) => (MerchantRoom)create.Invoke(RunManager.Instance, [RoomType.Shop, point, null])!;
        async Task<GreedRouteRelic> Obtain(int stage)
        {
            foreach (var relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
            var result = (GreedRouteRelic)ModelDb.Relic<GreedRouteRelic>().ToMutable();
            result.Stage = stage;
            await RelicCmd.Obtain(result, player);
            return result;
        }
        try
        {
            TestMode.IsOn = true;
            Check(Harmony.GetPatchInfo(roll)?.Prefixes.Any(p => p.PatchMethod.DeclaringType == typeof(GreedUnknownRoomPatch)) == true,
                "real room-roll prefix installed");
            Check(Harmony.GetPatchInfo(create)?.Postfixes.Any(p => p.PatchMethod.DeclaringType == typeof(GreedCreatedRoomPatch)) == true,
                "real room-create postfix installed");
            Check(Harmony.GetPatchInfo(price)?.Postfixes.Any(p => p.PatchMethod.DeclaringType == typeof(GreedFinalPricePatch)) == true,
                "final native merchant-price postfix installed");
            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            {
                int gold = player.Gold;
                int curses = player.Deck.Cards.OfType<Greed>().Count();
                var relic = await Obtain(stage);
                bool awake = stage is 3 or 4;
                Check(player.Gold - gold == (stage is 1 or 2 ? 100 : 0), "stage pickup gold");
                Check(player.Deck.Cards.OfType<Greed>().Count() - curses == (awake ? 1 : 0), "stage pickup curse");
                Check(relic.FreeShopPending == awake && !relic.FreeShopActive, "pickup queues without activating ordinary shop");
                await relic.AfterObtained();
                Check(player.Gold - gold == (stage is 1 or 2 ? 100 : 0), "repeated pickup no duplicate gold");
                Check(player.Deck.Cards.OfType<Greed>().Count() - curses == (awake ? 1 : 0), "repeated pickup no duplicate curse");
                var saved = (GreedRouteRelic)ModelDb.Relic<GreedRouteRelic>().ToMutable();
                SavedProperties.From(relic)!.Fill(saved);
                saved.Owner = player;
                await saved.AfterObtained();
                Check(saved.Stage == stage && saved.FreeShopPending == awake && saved.PickupEffectGranted == (stage != 0),
                    "native saved pickup receipt and pending restored");
                Check(player.Gold - gold == (stage is 1 or 2 ? 100 : 0)
                    && player.Deck.Cards.OfType<Greed>().Count() - curses == (awake ? 1 : 0), "saved pickup does not replay rewards");
                if (!awake) continue;

                Check(Roll(MapPointType.Shop) == RoomType.Shop, "ordinary map shop remains native shop");
                MerchantRoom ordinary = CreateShop(MapPointType.Shop);
                run.PushRoom(ordinary);
                try
                {
                    await relic.AfterRoomEntered(ordinary);
                    var inventory = MerchantInventory.CreateForNormalMerchant(player);
                    Check(inventory.AllEntries.Any(entry => entry.Cost > 0), "ordinary inventory remains paid");
                    Check(relic.FreeShopPending && !relic.FreeShopActive, "ordinary inventory does not consume entitlement");
                }
                finally { run.PopCurrentRoom(); }

                reservation.Patch(roll, prefix: new HarmonyMethod(typeof(DesignGreedTestConsoleCmd), nameof(ReserveEvent))
                    { priority = Priority.VeryHigh });
                try
                {
                    Check(Roll(MapPointType.Unknown) == RoomType.Event, "higher priority reservation wins actual patched roll");
                    Check(relic.FreeShopPending && !relic.FreeShopActive, "reserved event leaves free shop pending");
                }
                finally { reservation.UnpatchAll(reservation.Id); }

                Check(Roll(MapPointType.Unknown) == RoomType.Shop, "next unknown actually resolves to shop");
                Check(relic.FreeShopPending && !relic.FreeShopActive, "roll alone does not consume entitlement");
                MerchantRoom freeRoom = CreateShop(MapPointType.Unknown);
                Check(!relic.FreeShopPending && relic.FreeShopActive, "successful creation consumes exactly once");
                run.PushRoom(freeRoom);
                try
                {
                    await relic.AfterRoomEntered(freeRoom);
                    for (int refresh = 0; refresh < 2; refresh++)
                    {
                        var inventory = MerchantInventory.CreateForNormalMerchant(player);
                        Check(inventory.CardEntries.Any() && inventory.RelicEntries.Any() && inventory.PotionEntries.Any()
                            && inventory.CardRemovalEntry != null, "native inventory covers all product types");
                        Check(inventory.AllEntries.All(entry => entry.Cost == 0), "all items and card removal free across inventory rebuilds");
                        Check(Hook.ModifyMerchantPrice(run, player, inventory.CardRemovalEntry!, 100m) == 0,
                            "fixed starting price reduced to zero by native final hook");
                    }
                    var restored = (GreedRouteRelic)ModelDb.Relic<GreedRouteRelic>().ToMutable();
                    SavedProperties.From(relic)!.Fill(restored);
                    restored.Owner = player;
                    Check(restored.FreeShopLocation == GreedShopService.Location(run) && restored.FreeShopActive && !restored.FreeShopPending,
                        "saved active location and consumption retained");
                    Check(!restored.IsFreeShopFor(player), "loaded model must bind actual room before pricing");
                    restored.BindFreeShop(GreedShopService.Location(run), freeRoom);
                    Check(restored.IsFreeShopFor(player), "saved active entitlement can rebind same room");
                    Player foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
                    foreign.RunState = run;
                    Check(!relic.IsFreeShopFor(foreign), "other player pays normally");
                    var impostor = (GreedRouteRelic)ModelDb.Relic<GreedRouteRelic>().ToMutable();
                    impostor.Stage = 4; impostor.Owner = foreign; impostor.FreeShopPending = true;
                    Check(!impostor.CanReplaceUnknown(GreedShopService.Location(run)), "non-Maiden cannot trigger replacement");
                    var nested = new MerchantRoom();
                    run.PushRoom(nested);
                    try { Check(!relic.IsFreeShopFor(player), "different shop instance at same floor is not free"); }
                    finally { run.PopCurrentRoom(); }
                    run.ActFloor++;
                    Check(!relic.IsFreeShopFor(player), "different floor cannot reuse active price");
                    run.ActFloor--;
                }
                finally { run.PopCurrentRoom(); }
                await relic.AfterRoomEntered(new MapRoom());
                Check(!relic.FreeShopActive && !relic.FreeShopPending, "leaving consumes free room permanently");
                Check(!GreedShopService.Select(run, MapPointType.Unknown, false), "later unknown cannot trigger second free shop");
            }
            MaidenSuccubusMod.Logger.Info($"[DS27GreedTest] PASS {checks} assertions; disposable run modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27GreedTest] FAIL " + ex);
            throw;
        }
        finally
        {
            reservation.UnpatchAll(reservation.Id);
            GreedShopService.Cancel(run);
            while (run.CurrentRoomCount > roomCountBefore) run.PopCurrentRoom();
            run.ActFloor = floorBefore;
            TestMode.IsOn = previousTestMode;
            _running = false;
        }
    }
}
#endif
