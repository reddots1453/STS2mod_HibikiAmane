using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
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
        // Reuse the vanilla Queen/Bound card overlay. Its packed scene is
        // authored against NCard's overlay container, so it keeps the chains
        // aligned at every card scale and preview size. A text-label imitation
        // drifts when the fallback font or the hover-preview layout changes.
        Control overlay = ModelDb.Affliction<Bound>().CreateOverlay();
        overlay.Name = OverlayName;
        overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        return overlay;
    }
}
