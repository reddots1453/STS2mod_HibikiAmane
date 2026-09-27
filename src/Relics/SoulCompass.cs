using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Relics;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class SoulCompass : MSRelicTemplate, IMSRouteRewardModifierRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool HasUponPickupEffect => true;
    [SavedProperty] public bool PickupRewardsGranted { get; set; }

    // Dedicated art is not available yet; do not depend on another mod's assets.
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/circlet.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres");

    public static RouteRewardProbabilityBonus BonusAt(int corruption) =>
        RouteRewardProbabilityBonus.ForSoulCompass(corruption);

    public RouteRewardProbabilityBonus GetRouteRewardProbabilityBonus(Player player) =>
        ReferenceEquals(player, Owner) && player.Character is MaidenSuccubusCharacter
            && player.RunState is RunState run
                ? BonusAt(CorruptionQuery.Get(run))
                : default;

    internal static List<Reward> CreatePickupRewards(Player player) =>
        Enumerable.Range(0, 3).Select(_ => (Reward)new CardReward(
            CardCreationOptions.ForRoom(player, RoomType.Monster), 3, player)).ToList();

    public override async Task AfterObtained()
    {
        if (PickupRewardsGranted || Owner.Character is not MaidenSuccubusCharacter) return;
        // The event only obtains the relic. This is the single reward owner;
        // duplicate notifications or a restored acquired relic cannot grant again.
        PickupRewardsGranted = true;
        await RewardsCmd.OfferCustom(Owner, CreatePickupRewards(Owner));
    }
}
