using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Relics;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class Milker : MSRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;
    public override RelicAssetProfile AssetProfile => MagicSupportRelicAssets.Placeholder;
    [SavedProperty] public bool CombatPrepared { get; set; }

    public override async Task BeforeCombatStart()
    {
        if (CombatPrepared || !IsMutable || HasBeenRemovedFromState
            || Owner?.Character is not MaidenSuccubusCharacter
            || Owner.Creature.CombatState == null || !Owner.Creature.IsAlive) return;

        CombatPrepared = true;
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature,
            3, ValueProp.Unpowered, null, null);
        if (!Owner.Creature.IsAlive) return;
        for (int i = 0; i < 2; i++)
        {
            Milk milk = Owner.RunState.CreateCard<Milk>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(
                milk, PileType.Draw, Owner, CardPilePosition.Random);
        }
        Flash();
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        CombatPrepared = false;
        return Task.CompletedTask;
    }
}
