using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Util;
using MaidenSuccubus.UI;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using System.Runtime.CompilerServices;

namespace MaidenSuccubus.Patches;

/// <summary>
/// RitsuLib's character filter normally matches only CharacterModel.CardPool.
/// This character deliberately uses separate route pools, so the compendium
/// predicate must group those pools without merging reward generation pools.
/// </summary>
[HarmonyAfter("com.ritsukage.sts2-RitsuLib.framework-character-assets")]
[HarmonyPriority(Priority.Last)]
[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
public static class CardLibraryRoutePoolPatch
{
    public static void Postfix(
        NCardLibrary __instance,
        Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters,
        Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters)
    {
        Safe.Run(
            () =>
            {
                KeyValuePair<CharacterModel, NCardPoolFilter> entry =
                    ____cardPoolFilters.FirstOrDefault(pair =>
                        pair.Key is MaidenSuccubusCharacter);

                if (entry.Key is null || entry.Value is null)
                {
                    MaidenSuccubusMod.Logger.Warn(
                        "[CardLibraryRoutePoolPatch] Maiden character filter was not registered by RitsuLib.");
                    return;
                }

                // Keep the existing three-pool view even if a game update
                // changes the native filter scene and our extra row cannot mount.
                ____poolFilters[entry.Value] = IsMaidenSuccubusCompendiumCard;
                MaidenRouteFilterState? routeFilters = null;
                Safe.Run(() => routeFilters = MaidenRouteFilterState.Install(
                    __instance, entry.Value), "CardLibraryRouteFilter.Install");
                if (routeFilters != null)
                    ____poolFilters[entry.Value] = card =>
                        IsMaidenSuccubusCompendiumCard(card) && routeFilters.Allows(card);
                // RitsuLib creates this button from CustomIconTexturePath,
                // bypassing the CharacterModel.IconTexture getter override.
                // Set only our existing Image's texture, preserving its shader,
                // size, selection animation and the other character buttons.
                TextureRect? image = entry.Value.GetNodeOrNull<TextureRect>("Image");
                Texture2D? texture = RuntimeTextureAssets.Load(
                    "ui/core/hibiki_amane_character_icon_128.png");
                if (image != null && texture != null)
                {
                    image.Texture = texture;
                }
                else
                {
                    MaidenSuccubusMod.Logger.Warn(
                        "[CardLibraryRoutePoolPatch] Custom compendium icon unavailable; keeping existing icon.");
                }
                MaidenSuccubusMod.Logger.Info(
                    "[CardLibraryRoutePoolPatch] Installed three-route compendium filter.");
            },
            nameof(CardLibraryRoutePoolPatch));
    }

    private static bool IsMaidenSuccubusCompendiumCard(CardModel card) =>
        card is IMaidenSuccubusRouteCard;
}

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuOpened))]
internal static class CardLibraryRouteFilterOpenPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCardLibrary __instance) => Safe.Run(
        () => MaidenRouteFilterState.For(__instance)?.ResetAndRefresh(),
        nameof(CardLibraryRouteFilterOpenPatch));
}

[HarmonyPatch(typeof(NCardLibrary), "UpdateCardPoolFilter")]
internal static class CardLibraryRouteFilterSelectionPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCardLibrary __instance) => Safe.Run(
        () => MaidenRouteFilterState.For(__instance)?.RefreshVisibility(),
        nameof(CardLibraryRouteFilterSelectionPatch));
}

/// <summary>
/// A second, native-looking filter row under the library's rarity row. It is
/// owned by the screen, never by the card pools, so rewards are unaffected.
/// </summary>
internal sealed class MaidenRouteFilterState
{
    private static readonly ConditionalWeakTable<NCardLibrary, MaidenRouteFilterState> States = new();
    private readonly NCardLibrary _library;
    private readonly NCardPoolFilter _maidenPool;
    private readonly HBoxContainer _row;
    private readonly Dictionary<RouteCardKind, NCardRarityTickbox> _filters = [];

    private MaidenRouteFilterState(NCardLibrary library, NCardPoolFilter maidenPool,
        HBoxContainer row)
    {
        _library = library;
        _maidenPool = maidenPool;
        _row = row;
    }

    internal static MaidenRouteFilterState? For(NCardLibrary library) =>
        States.TryGetValue(library, out MaidenRouteFilterState? state) ? state : null;

    internal static MaidenRouteFilterState? Install(NCardLibrary library, NCardPoolFilter maidenPool)
    {
        if (For(library) is { } previous) return previous;
        NCardRarityTickbox? prototype = library.GetNodeOrNull<NCardRarityTickbox>("%CommonRarity");
        if (prototype == null) return null;

        // Insert beside the nearest native filter row in its VBox so layout,
        // scroll area and scale remain controlled by the library scene.
        Node anchor = prototype;
        while (anchor.GetParent() != null && anchor.GetParent() is not VBoxContainer)
            anchor = anchor.GetParent();
        if (anchor.GetParent() is not VBoxContainer host)
        {
            MaidenSuccubusMod.Logger.Warn("[CardLibraryRouteFilter] Native filter layout unavailable.");
            return null;
        }

        HBoxContainer row = new()
        {
            Name = "MaidenRouteFilters",
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        if (prototype.GetParent() is BoxContainer nativeRow)
            row.AddThemeConstantOverride("separation", nativeRow.GetThemeConstant("separation"));
        host.AddChild(row);
        host.MoveChild(row, anchor.GetIndex() + 1);
        MaidenRouteFilterState state = new(library, maidenPool, row);
        state.AddFilter(prototype, RouteCardKind.Corrupt, "CORRUPT");
        state.AddFilter(prototype, RouteCardKind.Holy, "HOLY");
        state.AddFilter(prototype, RouteCardKind.Neutral, "NEUTRAL");
        States.Add(library, state);
        state.RefreshVisibility();
        return state;
    }

    private void AddFilter(NCardRarityTickbox prototype, RouteCardKind route, string key)
    {
        var filter = (NCardRarityTickbox)prototype.Duplicate((int)Node.DuplicateFlags.Scripts);
        filter.Name = $"MaidenRoute{key}";
        IsolateMaterials(filter);
        filter.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        filter.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        filter.CustomMinimumSize = new Vector2(
            Math.Max(96f, prototype.Size.X), Math.Max(44f, prototype.Size.Y));
        _row.AddChild(filter);
        filter.Loc = new LocString("card_library", $"MAIDEN_ROUTE_{key}_TIP");
        filter.SetLabel(new LocString("card_library", $"MAIDEN_ROUTE_{key}").GetRawText());
        filter.IsTicked = false;
        filter.Connect(NTickbox.SignalName.Toggled,
            Callable.From<NTickbox>(_ => Safe.Run(RefreshCards, "CardLibraryRouteFilter.Toggle")));
        _filters.Add(route, filter);
    }

    private static void IsolateMaterials(Node node)
    {
        // Native checkbox hover animates shader parameters. A shallow resource
        // copy would also highlight the original rarity checkbox and siblings.
        if (node is CanvasItem canvas && canvas.Material != null)
            canvas.Material = (Material)canvas.Material.Duplicate(true);
        foreach (Node child in node.GetChildren()) IsolateMaterials(child);
    }

    internal bool Allows(CardModel card)
    {
        if (card is not IMaidenSuccubusRouteCard routeCard) return false;
        return MaidenRouteFilterRules.Allows(routeCard.RouteKind,
            _filters[RouteCardKind.Neutral].IsTicked,
            _filters[RouteCardKind.Corrupt].IsTicked,
            _filters[RouteCardKind.Holy].IsTicked);
    }

    internal void ResetAndRefresh()
    {
        bool hadSelection = _filters.Values.Any(filter => filter.IsTicked);
        foreach (NCardRarityTickbox filter in _filters.Values) filter.IsTicked = false;
        RefreshVisibility();
        if (hadSelection) RefreshCards();
    }

    internal void RefreshVisibility() =>
        _row.Visible = _maidenPool.Visible && _maidenPool.IsSelected;

    private void RefreshCards() =>
        AccessTools.Method(typeof(NCardLibrary), "UpdateFilter", [typeof(bool)])
            ?.Invoke(_library, [false]);
}
