using MaidenSuccubus.UI;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.Core.Replay;

/// <summary>
/// Marks only the temporary card instance created by Battle Technique Replay.
/// The capability follows that copied instance and disappears when it is
/// transformed back into BattleTechniqueReplay.
/// </summary>
[RegisterModelCapability(StableEntryStem = "battle_replay_origin")]
public sealed class BattleReplayOriginCapability :
    CardCapability,
    ICardOverlayContributor
{
    IEnumerable<CardOverlayContribution>
        ICardOverlayContributor.GetCardOverlays(CardOverlayContext context) =>
        [
            CardOverlayContribution.FromFactory(
                "maiden_battle_replay_shadow",
                _ => BattleReplayCardVisuals.CreateShadowOverlay(),
                order: 1,
                fullRect: false),
        ];
}
