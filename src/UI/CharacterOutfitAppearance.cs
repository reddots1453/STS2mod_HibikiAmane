using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Characters.Starts;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Data;
using STS2RitsuLib;

namespace MaidenSuccubus.UI;

/// <summary>Cosmetic state only: outfits never introduce a counted gameplay power.</summary>
internal static class CharacterOutfitAppearance
{
    private sealed class CombatOutfits
    {
        internal HashSet<Creature> TentacleWearers { get; } = [];
    }

    private static readonly ConditionalWeakTable<ICombatState, CombatOutfits> Outfits = new();
    private static readonly IDisposable EndSubscription =
        RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(evt =>
        {
            if (evt.CombatState == null || !Outfits.TryGetValue(evt.CombatState, out var outfits)) return;
            Outfits.Remove(evt.CombatState);
            foreach (Creature creature in outfits.TentacleWearers)
                TransformationEvents.Publish(creature);
        }, replayCurrentState: false);

    internal static void WearTentacle(Creature creature)
    {
        if (creature.Player?.Character is not MaidenSuccubusCharacter
            || creature.CombatState is not { } combat) return;
        Outfits.GetValue(combat, _ => new CombatOutfits()).TentacleWearers.Add(creature);
        TransformationEvents.Publish(creature);
    }

    internal static bool IsWearingTentacle(Creature creature) =>
        creature.CombatState is { } combat && Outfits.TryGetValue(combat, out var outfits)
        && outfits.TentacleWearers.Contains(creature);

    internal static string BaseTexture(Player? player)
    {
        if (player == null) return TextureFor(MaidenSuccubusStartProfileId.Normal);
        // Lobby selections are authoritative per player and persisted for the entire run.
        if (StarterRelicChoice.Handle != null)
        {
            var choice = StarterRelicChoice.Handle.Get(player);
            if (choice.RouteApplied) return TextureFor(choice.Route);
        }
        // Old runs saved the start identity before per-player route payloads existed.
        if (player.RunState is RunState run && M5Progress.Handle != null)
        {
            var progress = M5Progress.Handle.Get(run);
            if (progress.StartProfileApplied
                && Enum.TryParse<MaidenSuccubusStartProfileId>(progress.StartProfileId, out var route))
                return TextureFor(route);
        }
        return TextureFor((player.Character as IMaidenSuccubusStartProfile)?.StartProfileId
            ?? MaidenSuccubusStartProfileId.Normal);
    }

    private static string TextureFor(MaidenSuccubusStartProfileId route) => route switch
    {
        MaidenSuccubusStartProfileId.Succubus => "character_bunny.png",
        MaidenSuccubusStartProfileId.HolyMaiden => "character_plain.png",
        _ => "character_normal.png",
    };
}
