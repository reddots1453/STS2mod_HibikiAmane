using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.addons.mega_text;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Merchant;
using MaidenSuccubus.Util;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Patches;

/// <summary>
/// A second native card-removal slot, visually paired with the ordinary one.
/// Its entry is separate from MerchantInventory.CardRemovalEntry, so using it
/// never consumes or reprices the ordinary card-removal service.
/// </summary>
[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize))]
public static class MerchantInvasionCursePatch
{
    private const string SlotName = "MaidenSuccubusInvasionCurseRemoval";
    private static readonly FieldInfo? RemovalEntryField =
        AccessTools.Field(typeof(NMerchantCardRemoval), "_removalEntry");
    private static readonly FieldInfo? UnavailableField =
        AccessTools.Field(typeof(NMerchantCardRemoval), "_isUnavailable");
    private static readonly MethodInfo? UpdateVisualMethod =
        AccessTools.Method(typeof(NMerchantCardRemoval), "UpdateVisual");
    private static readonly ConditionalWeakTable<NMerchantCardRemoval, SlotState>
        States = new();

    private sealed class SlotState(
        Player player,
        MerchantCardRemovalEntry entry,
        NMerchantCardRemoval ordinary)
    {
        public Player Player { get; } = player;
        public MerchantCardRemovalEntry Entry { get; } = entry;
        public NMerchantCardRemoval Ordinary { get; } = ordinary;
        public NodePath RightNeighbor { get; set; } = ordinary.FocusNeighborRight;
        public bool Busy { get; set; }
    }

    [HarmonyPostfix]
    public static void Postfix(NMerchantInventory __instance) =>
        Safe.Run(() => AttachSlot(__instance), "MerchantInvasionCurse.Initialize");

    private static void AttachSlot(NMerchantInventory inventory)
    {
        Player? player = inventory.Inventory?.Player;
        NMerchantCardRemoval? ordinary =
            inventory.GetNodeOrNull<NMerchantCardRemoval>("%MerchantCardRemoval");
        if (player?.Character is not MaidenSuccubusCharacter
            || InvasionCurseMerchantService.GetEligibleCount(player) == 0
            || ordinary == null
            || RemovalEntryField == null
            || UnavailableField == null
            || UpdateVisualMethod == null)
            return;

        Control? parent = ordinary.GetParent() as Control;
        if (parent == null || parent.GetNodeOrNull<Node>(SlotName) != null)
            return;

        // Duplicate the native slot, including its art, focus hitbox, hover
        // scale, pressed animation and cost badge, but not its signal links.
        if (ordinary.Duplicate((int)Node.DuplicateFlags.Scripts)
            is not NMerchantCardRemoval slot)
            return;
        slot.Name = SlotName;
        slot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        NativeUiClone.RestoreOwners(slot);
        IsolateMaterials(slot);
        var entry = new MerchantCardRemovalEntry(player);
        RemovalEntryField.SetValue(slot, entry);
        UnavailableField.SetValue(slot, false);
        parent.AddChild(slot);
        try
        {
            if (parent is Container)
                parent.MoveChild(slot, ordinary.GetIndex() + 1);
            else
                PlaceBeside(slot, ordinary);

            var state = new SlotState(player, entry, ordinary);
            States.Add(slot, state);
            slot.Initialize(inventory);
            slot.FillSlot(entry);
            UpdateVisualMethod.Invoke(slot, null);
            LinkFocus(ordinary, slot, state);
            // Final layout is not available during Initialize. Position again after containers settle.
            Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(slot) && GodotObject.IsInstanceValid(ordinary))
                    PlaceBeside(slot, ordinary);
            }).CallDeferred();
            MaidenSuccubusMod.Logger.Info("[MerchantInvasionCurse] Native cleanup slot mounted beside card removal.");
        }
        catch
        {
            slot.QueueFree();
            throw;
        }
    }

    private static void PlaceBeside(Control slot, Control ordinary)
    {
        Rect2 bounds = ordinary.GetGlobalRect();
        const float gap = 14f;
        float viewportWidth = ordinary.GetViewport().GetVisibleRect().Size.X;
        float nextX = bounds.End.X + gap;
        if (nextX + bounds.Size.X > viewportWidth - gap)
            nextX = bounds.Position.X - bounds.Size.X - gap;
        nextX = Mathf.Clamp(nextX, gap,
            Mathf.Max(gap, viewportWidth - bounds.Size.X - gap));
        slot.GlobalPosition += new Vector2(nextX - slot.GetGlobalRect().Position.X,
            bounds.Position.Y - slot.GetGlobalRect().Position.Y);
    }

    private static void IsolateMaterials(Node node)
    {
        if (node is CanvasItem canvas && canvas.Material != null)
            canvas.Material = (Material)canvas.Material.Duplicate(true);
        foreach (Node child in node.GetChildren()) IsolateMaterials(child);
    }

    private static void LinkFocus(NMerchantCardRemoval ordinary,
        NMerchantCardRemoval slot, SlotState state)
    {
        if (ordinary.FocusNeighborRight != slot.GetPath())
            state.RightNeighbor = ordinary.FocusNeighborRight;
        slot.FocusNeighborRight = state.RightNeighbor;
        slot.FocusNeighborLeft = ordinary.GetPath();
        ordinary.FocusNeighborRight = slot.GetPath();
        slot.FocusNeighborTop = ordinary.FocusNeighborTop;
        slot.FocusNeighborBottom = ordinary.FocusNeighborBottom;
    }

    private static bool TryGetState(NMerchantCardRemoval slot,
        out SlotState state) => States.TryGetValue(slot, out state!);

    private static async Task RemoveAndRefresh(NMerchantCardRemoval slot,
        SlotState state)
    {
        try
        {
            if (InvasionCurseMerchantService.GetEligibleCount(state.Player) == 0)
                return;
            await InvasionCurseMerchantService.SelectAndRemove(state.Player);
            if (!GodotObject.IsInstanceValid(slot)) return;
            if (InvasionCurseMerchantService.GetEligibleCount(state.Player) == 0)
            {
                slot.OnCardRemovalUsed();
                if (GodotObject.IsInstanceValid(state.Ordinary))
                    state.Ordinary.FocusNeighborRight = state.RightNeighbor;
                SfxCmd.Play("event:/sfx/npcs/merchant/merchant_thank_yous");
            }
        }
        finally
        {
            state.Busy = false;
            if (GodotObject.IsInstanceValid(slot) && !state.Entry.Used)
                UpdateVisualMethod?.Invoke(slot, null);
        }
    }

    [HarmonyPatch(typeof(NMerchantCardRemoval), "OnTryPurchase")]
    private static class PurchasePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(NMerchantCardRemoval __instance,
            ref Task __result)
        {
            if (__instance.Name != SlotName) return true;
            Task replacement = Task.CompletedTask;
            Safe.Run(() =>
            {
                if (!TryGetState(__instance, out SlotState state)
                    || state.Busy || state.Entry.Used) return;
                state.Busy = true;
                replacement = RemoveAndRefresh(__instance, state);
            }, "MerchantInvasionCurse.OnTryPurchase");
            __result = replacement;
            return false;
        }
    }

    [HarmonyPatch(typeof(NMerchantCardRemoval), "UpdateVisual")]
    private static class VisualPatch
    {
        [HarmonyPostfix]
        private static void Postfix(NMerchantCardRemoval __instance) =>
            Safe.Run(() =>
            {
                if (!TryGetState(__instance, out SlotState state)
                    || state.Entry.Used) return;
                int refund = InvasionCurseMerchantService.GetEligibleCount(state.Player)
                    * InvasionCurseMerchantConfig.RefundGoldPerCurse;
                MegaLabel label = __instance.GetNode<MegaLabel>("%CostLabel");
                label.SetTextAutoSize($"+{refund}");
                label.Modulate = new Color(0.65f, 0.95f, 0.55f);
                __instance.GetNode<Sprite2D>("%Visual").Modulate =
                    new Color(0.91f, 0.78f, 1f);
            }, "MerchantInvasionCurse.UpdateVisual");
    }

    [HarmonyPatch(typeof(NMerchantCardRemoval), "CreateHoverTip")]
    private static class HoverPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(NMerchantCardRemoval __instance)
        {
            if (__instance.Name != SlotName) return true;
            Safe.Run(() =>
            {
                if (!TryGetState(__instance, out SlotState state)) return;
                int count = InvasionCurseMerchantService.GetEligibleCount(state.Player);
                var description = new LocString("static_hover_tips",
                    "MAIDENSUCCUBUS_SHOP_CURSE_REMOVAL.description");
                description.Add("Count", count);
                description.Add("Refund", count
                    * InvasionCurseMerchantConfig.RefundGoldPerCurse);
                NHoverTipSet.CreateAndShow(__instance, new HoverTip(
                    new LocString("static_hover_tips",
                        "MAIDENSUCCUBUS_SHOP_CURSE_REMOVAL.title"),
                    description), HoverTip.GetHoverTipAlignment(__instance));
            }, "MerchantInvasionCurse.CreateHoverTip");
            return false;
        }
    }

    [HarmonyPatch(typeof(NMerchantInventory), "UpdateNavigation")]
    private static class NavigationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(NMerchantInventory __instance) => Safe.Run(() =>
        {
            NMerchantCardRemoval? ordinary =
                __instance.GetNodeOrNull<NMerchantCardRemoval>("%MerchantCardRemoval");
            NMerchantCardRemoval? slot = ordinary?.GetParent()?.GetNodeOrNull<
                NMerchantCardRemoval>(SlotName);
            if (ordinary != null && slot != null
                && TryGetState(slot, out SlotState state)
                && !state.Entry.Used)
                LinkFocus(ordinary, slot, state);
            // Final layout is not available during Initialize. Position again after containers settle.
            Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(slot) && GodotObject.IsInstanceValid(ordinary))
                    PlaceBeside(slot, ordinary);
            }).CallDeferred();
            MaidenSuccubusMod.Logger.Info("[MerchantInvasionCurse] Native cleanup slot mounted beside card removal.");
        }, "MerchantInvasionCurse.UpdateNavigation");
    }
}
