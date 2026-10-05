using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;
using STS2RitsuLib;

namespace MaidenSuccubus.Patches;

// Adapted from sts2_foresight's RelicHoverPatch and MapPointPredictionPatch.
// Only the next encounter is exposed; rewards, events and RNG predictions are not used.
[HarmonyPatch]
internal static class BlindfoldEncounterHoverPatch
{
    private const string RelicMeta = "maiden_blindfold_hover";
    private const string MapMeta = "maiden_blindfold_map_preview";
    private const string PanelName = "MaidenBlindfoldEncounterPreview";
    private const float PanelWidth = 460f;

    [HarmonyPatch(typeof(NRelicInventoryHolder), "OnFocus")]
    [HarmonyPostfix]
    private static void AfterRelicFocus(NRelicInventoryHolder __instance) => Safe.Run(() =>
    {
        if (__instance.HasMeta(RelicMeta) || __instance.Relic?.Model is not Blindfold relic) return;
        var tips = BlindfoldPresentation.PreviewTips(relic).OfType<HoverTip>().ToArray();
        if (tips.Length == 0) return;
        string text = string.Join("\n\n", tips.Select(tip =>
            $"[b]{tip.Title}[/b]\n{tip.Description}"));
        // Foresight adds its preview to the native hover set after OnFocus creates it.
        if (!HoverTipHelper.AddTipToOwner(__instance, relic.Title.GetFormattedText(), text))
        {
            NHoverTipSet.Remove(__instance);
            NHoverTipSet.CreateAndShow(__instance, relic.HoverTips.Concat(tips.Cast<IHoverTip>()))
                ?.SetAlignmentForRelic(__instance.Relic);
        }
        __instance.SetMeta(RelicMeta, true);
        WidenRelicPreview(__instance);
    }, nameof(BlindfoldEncounterHoverPatch));

    [HarmonyPatch(typeof(NRelicInventoryHolder), "OnUnfocus")]
    [HarmonyPostfix]
    private static void AfterRelicUnfocus(NRelicInventoryHolder __instance) => Safe.Run(() =>
    {
        if (__instance.HasMeta(RelicMeta)) __instance.RemoveMeta(RelicMeta);
    }, nameof(BlindfoldEncounterHoverPatch));

    private static void WidenRelicPreview(NRelicInventoryHolder owner)
    {
        var active = Traverse.Create(typeof(NHoverTipSet)).Field("_activeHoverTips")
            .GetValue<Dictionary<Control, NHoverTipSet>>();
        if (active == null || !active.TryGetValue(owner, out var set)) return;
        var container = Traverse.Create(set).Field("_textHoverTipContainer").GetValue<Control>();
        if (container == null) return;
        float width = Math.Min(520f, Math.Max(240f, owner.GetViewportRect().Size.X - 48f));
        container.CustomMinimumSize = new Vector2(width, 0f);
        container.Size = new Vector2(width, container.Size.Y);
        foreach (var child in container.GetChildren().OfType<Control>())
            child.CustomMinimumSize = new Vector2(width, 0f);
        // Preserve the native alignment for both left and right screen edges.
        set.SetAlignmentForRelic(owner.Relic);
    }

    [HarmonyPatch(typeof(NMapPoint), "OnFocus")]
    [HarmonyPostfix]
    private static void AfterMapFocus(NMapPoint __instance) => Safe.Run(() =>
    {
        if (__instance.HasMeta(MapMeta) || __instance.State == MapPointState.Traveled
            || __instance.Point == null) return;
        var run = RunManager.Instance?.DebugOnlyGetState();
        var player = run?.Players.FirstOrDefault(LocalContext.IsMe);
        if (run == null || !BlindfoldPresentation.HasEffect(player)) return;
        RoomType? type = __instance.Point.PointType switch
        {
            MapPointType.Monster => RoomType.Monster,
            MapPointType.Elite => RoomType.Elite,
            MapPointType.Boss => RoomType.Boss,
            _ => null,
        };
        if (type == null) return;
        var tip = BlindfoldPresentation.EncounterTip(run.Act, type.Value);
        var panel = CreatePanel($"[b][color=#E7C75F]{tip.Title}[/color][/b]\n\n{tip.Description}");
        __instance.AddChild(panel);
        panel.ZIndex = 500;
        __instance.SetMeta(MapMeta, true);
        PositionPanel(__instance, panel);
        Callable.From(() => Safe.Run(() =>
        {
            if (GodotObject.IsInstanceValid(__instance) && GodotObject.IsInstanceValid(panel)
                && !panel.IsQueuedForDeletion()) PositionPanel(__instance, panel);
        }, nameof(BlindfoldEncounterHoverPatch))).CallDeferred();
    }, nameof(BlindfoldEncounterHoverPatch));

    [HarmonyPatch(typeof(NMapPoint), "OnUnfocus")]
    [HarmonyPostfix]
    private static void AfterMapUnfocus(NMapPoint __instance) => Safe.Run(() =>
    {
        if (!__instance.HasMeta(MapMeta)) return;
        foreach (Node child in __instance.GetChildren())
            if (child.Name == PanelName)
            {
                __instance.RemoveChild(child);
                child.QueueFree();
            }
        __instance.RemoveMeta(MapMeta);
    }, nameof(BlindfoldEncounterHoverPatch));

    private static PanelContainer CreatePanel(string text)
    {
        var panel = new PanelContainer
        {
            Name = PanelName, MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(PanelWidth, 0f),
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("111b21f5"), BorderColor = new Color("57717fbd"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 18, ContentMarginRight = 18,
            ContentMarginTop = 14, ContentMarginBottom = 14,
        });
        var label = new RichTextLabel
        {
            BbcodeEnabled = true, Text = text, FitContent = true, ScrollActive = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(PanelWidth - 36f, 0f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.AddThemeFontSizeOverride("normal_font_size", 18);
        label.AddThemeConstantOverride("line_separation", 4);
        label.AddThemeColorOverride("default_color", new Color("e5e1d8"));
        panel.AddChild(label);
        return panel;
    }

    private static void PositionPanel(NMapPoint target, Control panel)
    {
        Rect2 rect = target.GetGlobalRect();
        Vector2 viewport = target.GetViewportRect().Size;
        float width = Math.Min(PanelWidth, Math.Max(200f, viewport.X - 24f));
        panel.CustomMinimumSize = new Vector2(width, 0f);
        float x = rect.Position.X - width - 20f;
        if (x < 12f) x = Math.Min(rect.End.X + 20f, viewport.X - width - 12f);
        float y = Math.Clamp(rect.Position.Y, 12f,
            Math.Max(12f, viewport.Y - Math.Max(panel.Size.Y, 240f) - 12f));
        panel.GlobalPosition = new Vector2(Math.Max(12f, x), y);
    }
}
