#if DEBUG
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.addons.mega_text;
using MaidenSuccubus.Characters.Starts;
using MaidenSuccubus.Data;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using MaidenSuccubus.Characters;
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
        if (_started || !OS.GetCmdlineUserArgs().Any(arg => arg.Trim() is "--maiden-ui-probe" or "--maiden-relic-arrow-probe")) return;
        _started = true;
        MaidenSuccubusMod.Logger.Info("[NativeUiProbe] START");
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

    private static async Task CheckRelicArrowGeometry(NMainMenu menu, Control host)
    {
        using var scene = NCharacterSelectScreen.Create() ?? throw new InvalidOperationException("Character scene unavailable");
        var panel = (Control)scene.GetNode<Control>("InfoPanel").Duplicate((int)Node.DuplicateFlags.Scripts);
        NativeUiClone.RestoreOwners(panel);
        host.AddChild(panel);
        Control relic = panel.GetNode<Control>("VBoxContainer/Relic");
        Control icon = relic.GetNode<Control>("Icon");
        var ascension = scene.GetNode<NAscensionPanel>("AscensionPanel");
        var layout = new StarterRelicArrowLayout(relic, icon,
            ascension.GetNode<NButton>("HBoxContainer/LeftArrowContainer/LeftArrow"),
            ascension.GetNode<NButton>("HBoxContainer/RightArrowContainer/RightArrow"));
        layout.Previous.Show();
        layout.Next.Show();
        const float epsilon = .05f;
        async Task Frames(int count)
        {
            for (int i = 0; i < count; i++) await menu.ToSignal(menu.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        Vector2 Center(Control node) => node.GetGlobalRect().GetCenter();
        void Aligned(string phase)
        {
            Rect2 bounds = relic.GetGlobalRect();
            float gap = 8f * relic.GetGlobalTransform().Scale.X;
            if (Mathf.Abs(layout.Previous.GetGlobalRect().End.X - bounds.Position.X + gap) > epsilon ||
                Mathf.Abs(layout.Next.GetGlobalRect().Position.X - bounds.End.X - gap) > epsilon ||
                Mathf.Abs(Center(layout.Previous).Y - Center(icon).Y) > epsilon ||
                Mathf.Abs(Center(layout.Next).Y - Center(icon).Y) > epsilon)
                throw new InvalidOperationException($"Arrow alignment changed: {phase}; panel={bounds}; prev={layout.Previous.GetGlobalRect()}; next={layout.Next.GetGlobalRect()}; icon={icon.GetGlobalRect()}");
        }
        await Frames(5);
        var title = relic.GetNode<MegaRichTextLabel>("Name/RichTextLabel");
        var description = relic.GetNode<MegaRichTextLabel>("Description");
        var character = ModelDb.Character<MaidenSuccubusCharacter>();
        panel.GetNode<MegaLabel>("VBoxContainer/Name").SetTextAutoSize(new MegaCrit.Sts2.Core.Localization.LocString("characters", character.CharacterSelectTitle).GetFormattedText());
        panel.GetNode<MegaRichTextLabel>("VBoxContainer/DescriptionLabel").Text = new MegaCrit.Sts2.Core.Localization.LocString("characters", character.CharacterSelectDesc).GetFormattedText();
        await Frames(5);
        Vector2 baselinePrevious = Center(layout.Previous);
        Vector2 baselineNext = Center(layout.Next);
        for (int iteration = 0; iteration < 8; iteration++)
        {
            var preview = StarterRelicSelection.Preview(iteration % 2 == 0 ? StarterRelicKind.Omnipotent : StarterRelicKind.Hero);
            title.Text = preview.Title.GetFormattedText();
            description.Text = preview.DynamicDescription.GetFormattedText();
            relic.GetNode<TextureRect>("Icon").Texture = preview.Icon;
            relic.GetNode<TextureRect>("Icon/Outline").Texture = preview.IconOutline;
            await Frames(5);
            Aligned("toggle " + iteration);
            Check(Center(layout.Previous).DistanceTo(baselinePrevious) < epsilon && Center(layout.Next).DistanceTo(baselineNext) < epsilon,
                "relic toggle " + iteration + " keeps both button centers");
        }
        foreach (var button in new[] { layout.Previous, layout.Next })
        {
            AccessTools.Method(button.GetType(), "OnFocus").Invoke(button, null);
            AccessTools.Method(button.GetType(), "OnPress").Invoke(button, null);
            AccessTools.Method(button.GetType(), "OnRelease").Invoke(button, null);
            Aligned("native press/release");
            AccessTools.Method(button.GetType(), "OnUnfocus").Invoke(button, null);
        }
        for (int frame = 0; frame < 45; frame++)
        {
            panel.Position += new Vector2(3f, 1f);
            await Frames(1);
            Aligned("panel animation " + frame);
        }
        Check(true, "45 panel-animation frames and native button animations stay aligned");
        host.Scale = new Vector2(1.25f, 1.25f);
        relic.Size += new Vector2(90f, 0f);
        icon.Position += new Vector2(0f, 6f);
        await Frames(5);
        Aligned("resize/scale/icon move");
        Check(layout.Previous.GetParent() == relic && layout.Next.GetParent() == relic,
            "panel anchors survive resize, parent scale and icon position change");
        Check(!ReferenceEquals(layout.Previous.GetNode<TextureRect>("TextureRect").Material,
            ascension.GetNode<TextureRect>("HBoxContainer/LeftArrowContainer/LeftArrow/TextureRect").Material),
            "hover material isolated from ascension button");
    }

    private static void CheckEroticHoverTips(Control host)
    {
        var player = Player.CreateForNewRun<MaidenSuccubusCharacter>(UnlockState.all, 1);
        var run = RunState.CreateForNewRun([player], ActModel.GetDefaultList().Select(a => (ActModel)a.ToMutable()).ToArray(), [], GameMode.Standard, 0, "UI_INTENT_PROBE");
        var owner = player.Creature;
        var node = NIntent.Create(0f);
        host.AddChild(node);
        int moves = 0, tips = 0, blockComponents = 0;
        foreach (var spec in EroticAttackCatalog.All.Values)
        {
            foreach (var (method, value) in new (string, object?)[]
            {
                ("BuildDesireIntents", spec.Desire),
                ("BuildControlIntents", spec.Control),
                ("BuildInvasionIntents", spec.Invasion),
            })
            {
                if (value == null) continue;
                moves++;
                var intents = (AbstractIntent[])AccessTools.Method(typeof(IntentMoveFactory), method).Invoke(null, [value])!;
                foreach (var intent in intents)
                {
                    var tip = intent.GetHoverTip([], owner);
                    if ((tip.Title ?? string.Empty).Contains("MAIDENSUCCUBUS_", StringComparison.Ordinal)
                        || tip.Description.Contains("MAIDENSUCCUBUS_", StringComparison.Ordinal)
                        || System.Text.RegularExpressions.Regex.IsMatch(tip.Description, @"\{\w+\}"))
                        throw new InvalidOperationException($"Unformatted intent tip: {spec.MonsterId}: {tip.Title}: {tip.Description}");
                    if (intent is InvasionCurseIntent curse && !tip.Description.Contains(curse.CurseName, StringComparison.Ordinal))
                        throw new InvalidOperationException("Curse name missing: " + spec.MonsterId);
                    if (intent is EroticBlockIntent block)
                    {
                        blockComponents++;
                        node.UpdateIntent(block, [], owner);
                        string expected = block.GetIntentLabel([], owner).GetFormattedText();
                        if (node.GetNode<MegaRichTextLabel>("%Value").Text != expected
                            || !tip.Description.Contains(expected, StringComparison.Ordinal))
                            throw new InvalidOperationException("Block amount missing: " + spec.MonsterId);
                        node.UpdateIntent(new DefendIntent(), [], owner);
                        if (node.GetNode<MegaRichTextLabel>("%Value").Text.Length != 0)
                            throw new InvalidOperationException("Block number leaked into vanilla defend");
                        if (IntentAnimData.GetAnimationFrameCount(block.GetAnimation([], owner)) <= 0)
                            throw new InvalidOperationException("Invalid defend animation");
                    }
                    tips++;
                }
            }
        }
        foreach (var (text, amount, recipient) in new[]
        {
            ("欲望增加1；获得5点格挡。", 5, EroticBlockRecipient.Self),
            ("其他敌人获得6点格挡。", 6, EroticBlockRecipient.OtherEnemies),
            ("召唤物获得7点格挡。", 7, EroticBlockRecipient.Summons),
        })
        {
            var components = EroticEffectCmd.BuildSupplementalIntents(text, EroticIntentKind.Desire).OfType<EroticBlockIntent>().ToArray();
            if (components.Length != 1 || components[0].Amount != amount || components[0].Recipient != recipient)
                throw new InvalidOperationException("Block recipient mismatch: " + text);
        }
        var description = new MegaCrit.Sts2.Core.Localization.LocString("static_hover_tips", "MAIDENSUCCUBUS_SHOP_CURSE_REMOVAL.description");
        description.Add("Count", 2); description.Add("Refund", 100);
        var shopTip = new MegaCrit.Sts2.Core.HoverTips.HoverTip(new MegaCrit.Sts2.Core.Localization.LocString("static_hover_tips", "MAIDENSUCCUBUS_SHOP_CURSE_REMOVAL.title"), description);
        Check(shopTip.Title == "清理精液类诅咒" && shopTip.Description.Contains("2") && shopTip.Description.Contains("100"), "shop curse tip formats count and refund");
        Check(EroticAttackCatalog.All.Count == 102 && blockComponents > 0, $"102 monsters: {moves} moves, {tips} formatted tips, {blockComponents} numeric defend components; no stale reused labels");
    }

    private static async Task Run(NMainMenu menu)
    {
        var host = new Control { Name = "MaidenUiProbe", Visible = false };
        try
        {
            for (int i = 0; i < 5; i++) await menu.ToSignal(menu.GetTree(), SceneTree.SignalName.ProcessFrame);
            MaidenSuccubusMod.Logger.Info("[NativeUiProbe] UserData=" + OS.GetUserDataDir());
            (NGame.Instance ?? throw new InvalidOperationException("Game unavailable")).AddChild(host);
            if (OS.GetCmdlineUserArgs().Any(arg => arg.Trim() == "--maiden-relic-arrow-probe"))
            {
                await CheckRelicArrowGeometry(menu, host);
                CheckEroticHoverTips(host);
                host.QueueFree();
                for (int i = 0; i < 5; i++) await menu.ToSignal(menu.GetTree(), SceneTree.SignalName.ProcessFrame);
                MaidenSuccubusMod.Logger.Info("[NativeUiProbe] RELIC GEOMETRY COMPLETE PASS (no lobby/run/save changes)");
                menu.GetTree().Quit(0);
                return;
            }
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
            var rug = Descendants(merchant).OfType<NMerchantInventory>().First();
            var player = Player.CreateForNewRun<MaidenSuccubusCharacter>(UnlockState.all, 1);
            var run = RunState.CreateForNewRun([player], ActModel.GetDefaultList().Select(a => (ActModel)a.ToMutable()).ToArray(), [], GameMode.Standard, 0, "UI_PROBE");
            AccessTools.Property(typeof(NMerchantInventory), "Inventory").SetValue(rug, new MerchantInventory(player));
            var slot = (NMerchantCardRemoval)ordinary.Duplicate((int)Node.DuplicateFlags.Scripts);
            NativeUiClone.RestoreOwners(slot);
            host.AddChild(slot);
            slot.Initialize(rug);
            slot.FillSlot(new MerchantCardRemovalEntry(player));
            foreach (string key in new[] { "%Hitbox", "%Visual", "%CostLabel", "%Animation" })
                Check(slot.GetNodeOrNull<Node>(key) != null, "shop clone " + key + " resolves after _Ready");
            Check(slot.IsNodeReady(), "shop clone native _Ready completed");
            Check(slot.GetNode<Control>("Cost").Visible, "shop clone native cost badge initialized");
            int checkedMoves = 0;
            foreach (var spec in EroticAttackCatalog.All.Values)
            {
                foreach (var (method, value, effect) in new (string, object?, string)[]
                {
                    ("BuildDesireIntents", spec.Desire, spec.Desire?.EffectText ?? ""),
                    ("BuildControlIntents", spec.Control, spec.Control?.EffectText ?? ""),
                    ("BuildInvasionIntents", spec.Invasion, spec.Invasion?.EffectText ?? "")
                })
                {
                    if (value == null) continue;
                    var components = (AbstractIntent[])AccessTools.Method(typeof(IntentMoveFactory), method).Invoke(null, [value])!;
                    int expectedStatuses = System.Text.RegularExpressions.Regex.Matches(effect, @"将(\d+)张([^；。，]+?)(置入弃牌堆|洗入弃牌堆|洗入抽牌堆)").Count;
                    if (components.OfType<ClothingHazardIntent>().Count() != expectedStatuses)
                        throw new InvalidOperationException(spec.MonsterId + " status component mismatch");
                    foreach (var component in components.Where(c => c is ControlIntent or InvasionIntent or DesireGainIntent or TearClothingIntent or ClothingHazardIntent or InvasionCurseIntent))
                        if (IntentAnimData.GetAnimationFrameCount(component.GetAnimation([], player.Creature)) <= 0)
                            throw new InvalidOperationException(spec.MonsterId + " invalid animation key");
                    checkedMoves++;
                }
            }
            Check(EroticAttackCatalog.All.Count == 102 && checkedMoves > 200,
                $"102 monster catalog: {checkedMoves} move component/status/animation checks");
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
            for (int i = 0; i < 90; i++) await menu.ToSignal(menu.GetTree(), SceneTree.SignalName.ProcessFrame);
            host.QueueFree();
            for (int i = 0; i < 5; i++) await menu.ToSignal(menu.GetTree(), SceneTree.SignalName.ProcessFrame);
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
