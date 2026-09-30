using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Relics;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class SkyOrb : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(5)];
    public override RelicAssetProfile AssetProfile => RelicIconAssets.For("starter_HeavenlyOrb");

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner.Character is not MaidenSuccubusCharacter || Owner.Creature.IsDead) return;
        Flash();
        await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.BaseValue);
    }

    public override bool ShouldAllowSelectingMoreCardRewards(Player player, CardReward cardReward) =>
        ReferenceEquals(player, Owner) && ReferenceEquals(cardReward.Player, player)
        && player.Character is MaidenSuccubusCharacter
        && OrbRules.AllowsMoreCards(0, sky: true, cardReward.Cards.Count());
}
