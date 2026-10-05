using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

// Enemy-only adaptation of local sts2_foresight's MapPointPredictionPatch.
// Only the next encounter is exposed; rewards, events and RNG predictions are not used.
[HarmonyPatch]
internal static class BlindfoldEncounterHoverPatch
{
    private const string MapMeta = "maiden_blindfold_map_preview";
    private const string PanelName = "MaidenBlindfoldEncounterPreview";
    private const float PanelWidth = 760f;

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
        string color = type.Value switch
        {
            RoomType.Elite => "D98BB8",
            RoomType.Boss => "E07A7A",
            _ => "D8C39A",
        };
        var panel = CreatePanel($"[font_size=18][font_size=21][color=#{color}]▌ {tip.Title}[/color][/font_size]\n"
            + $"[color=#40535D]────────────────────────────────[/color]\n{tip.Description}[/font_size]");
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
        foreach (var label in panel.GetChildren().OfType<RichTextLabel>())
            label.CustomMinimumSize = new Vector2(Math.Max(1f, width - 36f), 0f);
        panel.Size = new Vector2(width, panel.Size.Y);
        float x = rect.Position.X - width - 20f;
        if (x < 12f) x = Math.Min(rect.End.X + 20f, viewport.X - width - 12f);
        float y = Math.Clamp(rect.Position.Y, 12f,
            Math.Max(12f, viewport.Y - Math.Max(panel.Size.Y, 240f) - 12f));
        panel.GlobalPosition = new Vector2(Math.Max(12f, x), y);
    }
}
