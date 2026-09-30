using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Relics;

// Keep the existing model ID for saves; its formal display name is 全能宝珠.
[RegisterTouchOfOrobasRefinement(typeof(SkyOrb))]
[RegisterRelic(typeof(MSRelicPool))]
public sealed class TwinSoulChalice : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(5), new MaxHpVar(5)];
    public override RelicAssetProfile AssetProfile => RelicIconAssets.For("starter_AllPurposeOrb");

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner.Character is not MaidenSuccubusCharacter || Owner.Creature.IsDead
            || Owner.RunState is not RunState run) return;
        Flash();
        if (OrbRules.GrantsMaxHp(CorruptionQuery.Get(run), sky: false))
            await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.BaseValue);
        else
            await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    }

    public override bool ShouldAllowSelectingMoreCardRewards(Player player, CardReward cardReward) =>
        ReferenceEquals(player, Owner) && ReferenceEquals(cardReward.Player, player)
        && player.Character is MaidenSuccubusCharacter && player.RunState is RunState run
        && OrbRules.AllowsMoreCards(CorruptionQuery.Get(run), sky: false, cardReward.Cards.Count());
}
