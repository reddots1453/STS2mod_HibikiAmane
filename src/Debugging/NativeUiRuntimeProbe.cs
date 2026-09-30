#if DEBUG
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Patches;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Debugging;

/// <summary>Opt-in main-menu engine checks; never starts a run or edits a save.</summary>
[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
internal static class NativeUiRuntimeProbe
{
    private static bool _started;
    [HarmonyPostfix]
    private static void Postfix(NMainMenu __instance)
    {
        if (_started || !OS.GetCmdlineUserArgs().Contains("--maiden-ui-probe")) return;
        _started = true;
        _ = Run(__instance);
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node nested in Descendants(child)) yield return nested;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        MaidenSuccubusMod.Logger.Info("[NativeUiProbe] PASS " + message);
    }

    private static async Task Run(NMainMenu menu)
    {
        var host = new Control { Name = "MaidenUiProbe", Visible = false };
        try
        {
            for (int i = 0; i < 45; i++) await menu.ToSignal(menu.GetTree(), SceneTree.SignalName.ProcessFrame);
            (NGame.Instance ?? throw new InvalidOperationException("Game unavailable")).AddChild(host);
            NCardLibrary library = NCardLibrary.Create() ?? throw new InvalidOperationException("Library unavailable");
            host.AddChild(library);
            var row = Descendants(library).FirstOrDefault(n => n.Name == "MaidenRouteFilters");
            Check(row != null && row.GetChildCount() == 3, "three native route checkboxes mounted");
            foreach (var box in row!.GetChildren().OfType<NCardRarityTickbox>())
            {
                Check(box.GetNodeOrNull<Control>("%TickboxVisuals") != null, box.Name + " unique owner resolves");
                box.IsTicked = true;
                Check(box.GetNode<Control>("%TickboxVisuals/Ticked").Visible, box.Name + " native tick state works");
                box.IsTicked = false;
            }
            using Node merchant = PreloadManager.Cache.GetScene("res://scenes/rooms/merchant_room.tscn").Instantiate();
            var ordinary = Descendants(merchant).OfType<NMerchantCardRemoval>().First();
            var slot = (NMerchantCardRemoval)ordinary.Duplicate((int)Node.DuplicateFlags.Scripts);
            NativeUiClone.RestoreOwners(slot);
            host.AddChild(slot);
            foreach (string key in new[] { "%Hitbox", "%Visual", "%CostLabel", "%Animation" })
                Check(slot.GetNodeOrNull<Node>(key) != null, "shop clone " + key + " resolves after _Ready");
            Check(slot.IsNodeReady(), "shop clone native _Ready completed");
            var node = NIntent.Create(0f);
            host.AddChild(node);
            var custom = new DesireGainIntent(1, "probe");
            Check(IntentAnimData.GetAnimationFrameCount(custom.GetAnimation([], null!)) > 0, "custom intent has valid native animation key");
            node.UpdateIntent(custom, [], null!);
            Check(ReferenceEquals(node.GetNode<Sprite2D>("%Intent").Texture, MaidenIntentIconAssets.Get(custom)), "custom sprite installed");
            node._Process(.25);
            Check(ReferenceEquals(node.GetNode<Sprite2D>("%Intent").Texture, MaidenIntentIconAssets.Get(custom)), "frame loop preserves custom sprite");
            node.UpdateIntent(new DebuffIntent(), [], null!);
            node._Process(.25);
            Check(!ReferenceEquals(node.GetNode<Sprite2D>("%Intent").Texture, MaidenIntentIconAssets.Get(custom)), "reused node restores vanilla sprite");
            node.UpdateIntent(custom, [], null!);
            AccessTools.Method(typeof(NIntent), "UpdateVisuals").Invoke(node, null);
            node._Process(.25);
            Check(ReferenceEquals(node.GetNode<Sprite2D>("%Intent").Texture, MaidenIntentIconAssets.Get(custom)), "state refresh preserves custom sprite");
            Check(Godot.FileAccess.FileExists("res://MaidenSuccubus/audio/magic/transformation_start.ogg"), "installed audio available to Godot");
            Check(Godot.FileAccess.FileExists("res://MaidenSuccubus/images/cutscenes/control/restraint_suit.png"), "installed CG available to Godot");
            MaidenSuccubusMod.Logger.Info("[NativeUiProbe] COMPLETE PASS (no run/save changes)");
            menu.GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[NativeUiProbe] FAILED " + ex);
            menu.GetTree().Quit(2);
        }
        finally { if (GodotObject.IsInstanceValid(host)) host.QueueFree(); }
    }
}
#endif
