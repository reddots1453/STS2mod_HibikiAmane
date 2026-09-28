using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Relics;

// Preserve both model IDs for existing saves. Variation changes presentation
// and behavior, never relic ownership, pickup history, slot or acquisition floor.
public abstract class HeartNecklace : MSRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/circlet.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres");

    internal string ActiveEntry => "MAIDEN_SUCCUBUS_RELIC_" + (IsMurky
        ? "MURKY_HEART_NECKLACE" : "CLEAR_HEART_NECKLACE");

    private bool IsMurky => IsMutable && Owner is { Character: MaidenSuccubusCharacter, RunState: RunState run }
        ? HeartNecklaceRules.IsMurky(CorruptionQuery.Get(run))
        : this is MurkyHeartNecklace;

    public override LocString Title
    {
        get
        {
            var title = new LocString("relics", ActiveEntry + ".title");
            if (!IsWax) return title;
            LocString wax = ToyBox.WaxRelicPrefix;
            wax.Add("Title", title);
            return wax;
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (!ReferenceEquals(player, Owner) || Owner.Character is not MaidenSuccubusCharacter
            || HasBeenRemovedFromState || Owner.Creature.IsDead || Owner.RunState is not RunState run)
            return;

        int delta = HeartNecklaceRules.TurnDelta(CorruptionQuery.Get(run), Data.Desire.Get(Owner));
        if (delta == 0) return;
        Flash();
        // Use the normal resource pipeline (including gain prevention/listeners).
        await Data.Desire.Modify(Owner, delta);
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class ClearHeartNecklace : HeartNecklace
{
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class MurkyHeartNecklace : HeartNecklace
{
    // Registered for save reconstruction, not a second rollable relic.
    public override bool IsAllowed(IRunState runState) => false;
    public override bool IsAllowedInShops => false;
}
