using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Merchant;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize))]
public static class MerchantInvasionCursePatch
{
    private const string ButtonName = "MaidenSuccubusInvasionCurseRemoval";

    [HarmonyPostfix]
    public static void Postfix(NMerchantInventory __instance)
    {
        Safe.Run(
            () => AttachButton(__instance),
            "MerchantInvasionCurse.Initialize");
    }

    private static void AttachButton(NMerchantInventory inventory)
    {
        var player = inventory.Inventory?.Player;
        if (player?.Character is not MaidenSuccubusCharacter
            || InvasionCurseMerchantService.GetEligibleCount(player) == 0
            || inventory.GetNodeOrNull<Button>(ButtonName) != null)
        {
            return;
        }

        Control host = inventory.GetNodeOrNull<Control>("%MerchantCardRemoval")
            ?? inventory.GetNode<Control>("%SlotsContainer");
        var button = new Button
        {
            Name = ButtonName,
            Text = BuildText(player),
            CustomMinimumSize = new Vector2(300f, 64f),
            Position = new Vector2(-50f, 155f),
            FocusMode = Control.FocusModeEnum.All,
            TooltipText = "永久移除牌组中全部精液类诅咒；每张获得50金币；不消耗普通删牌次数。",
        };
        host.AddChild(button);
        button.Pressed += () =>
        {
            button.Disabled = true;
            TaskHelper.RunSafely(RemoveAndRefresh(button, player));
        };
    }

    private static async Task RemoveAndRefresh(Button button, Player player)
    {
        await InvasionCurseMerchantService.SelectAndRemove(player);
        if (!GodotObject.IsInstanceValid(button))
        {
            return;
        }
        if (InvasionCurseMerchantService.GetEligibleCount(player) == 0)
        {
            button.QueueFree();
            return;
        }
        button.Text = BuildText(player);
        button.Disabled = false;
    }

    private static string BuildText(Player player) =>
        $"清理全部精液类诅咒（获得"
        + $"{InvasionCurseMerchantConfig.RefundGoldPerCurse * InvasionCurseMerchantService.GetEligibleCount(player)}金币）";
}
