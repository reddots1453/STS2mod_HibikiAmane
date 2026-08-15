using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Data;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Relics;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class TwinSoulChalice : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HealVar(5), new MaxHpVar(5), new CardsVar(1)];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/neows_lament.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/neows_lament.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/neows_lament.tres");

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (Owner.Creature.IsDead) return;
        Flash();
        if (CorruptionQuery.Get((RunState)Owner.RunState) >= 4)
            await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.BaseValue);
        else
            await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner
            || room is not CombatRoom
            || CorruptionQuery.Get((RunState)player.RunState) > -4)
            return false;

        rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3, Owner));
        Flash();
        return true;
    }
}
