using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MaidenSuccubus.Data;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.Characters.Starts;

internal static class StarterRelicSelection
{
    internal static RelicModel Preview(StarterRelicKind kind) => StarterRelicChoice.Normalize(kind) == StarterRelicKind.Hero
        ? ModelDb.Relic<HeroOrb>() : ModelDb.Relic<TwinSoulChalice>();

    // Called only at NEW-run starter finalization, after RitsuLib imported the
    // authoritative per-player lobby payload. Never called for loaded saves.
    internal static void Apply(Player player)
    {
        if (player.Character is not MaidenSuccubusCharacter || player.RunState is not RunState) return;
        StarterRelicChoiceState state = StarterRelicChoice.Handle.Get(player);
        if (state.Applied) return;
        TwinSoulChalice? original = player.GetRelic<TwinSoulChalice>();
        if (StarterRelicChoice.Normalize(state.Kind) == StarterRelicKind.Hero && original != null)
        {
            // Compose the initial inventory, rather than Obtain/Replace: the
            // native finalizer will invoke AfterObtained exactly once afterward.
            var replacement = ModelDb.Relic<HeroOrb>().ToMutable();
            replacement.FloorAddedToDeck = original.FloorAddedToDeck;
            int index = player.Relics.ToList().IndexOf(original);
            player.RemoveRelicInternal(original, silent: true);
            player.AddRelicInternal(replacement, index, silent: true);
            SaveManager.Instance.MarkRelicAsSeen(replacement);
        }
        StarterRelicChoice.Handle.Modify(player, data => data.Applied = true);
    }
}
