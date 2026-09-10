using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace MaidenSuccubus.UI;

public static class EscapeCardVisuals
{
    public const string OverlayName = "MaidenSuccubusEscapeOverlay";

    [ThreadStatic]
    private static bool _refreshing;

    public static void Refresh(Player player)
    {
        if (_refreshing)
        {
            return;
        }

        _refreshing = true;
        try
        {
            foreach (PileType pileType in Enum.GetValues<PileType>()
                .Where(type => type.IsCombatPile()))
            {
                foreach (var card in pileType.GetPile(player).Cards)
                {
                    NCard? node = NCard.FindOnTable(card);
                    node?.UpdateVisuals(pileType, CardPreviewMode.Normal);
                }
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    public static Control CreateOverlay()
    {
        Control root = new()
        {
            Name = OverlayName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        ColorRect shade = new()
        {
            Color = new Color(0.06f, 0.08f, 0.12f, 0.48f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(shade);

        Label chains = new()
        {
            Text = "⛓     ⛓\n\n⛓     ⛓",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        chains.AddThemeFontSizeOverride("font_size", 52);
        chains.AddThemeColorOverride("font_color", new Color(0.72f, 0.76f, 0.82f, 0.9f));
        chains.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(chains);
        return root;
    }
}
