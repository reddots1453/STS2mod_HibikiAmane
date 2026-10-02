using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Relics;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Relics;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class PrayerEarrings : MSRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Common;
    public override RelicAssetProfile AssetProfile => RelicIconAssets.For(HolyVariation
        ? "character_PrayerEarrings_Holy" : "character_PrayerEarrings");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<MagicAmplificationPower>()];

    internal bool HolyVariation => IsMutable && Owner is { Character: MaidenSuccubusCharacter, RunState: RunState run }
        && MagicSupportRelicRules.PrayerVariation(CorruptionQuery.Get(run));

    private bool CanTrigger => IsMutable && Owner is { Character: MaidenSuccubusCharacter }
        && !HasBeenRemovedFromState && Owner.Creature.IsAlive && Owner.Creature.CombatState != null;

    public override Task BeforeCombatStart() => CanTrigger
        ? Grant(new BlockingPlayerChoiceContext()) : Task.CompletedTask;

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (!CanTrigger || Owner.RunState is not RunState run) return Task.CompletedTask;
        bool ownForm = ReferenceEquals(power.Owner, Owner.Creature)
            && power is ImmaculateRobePower or CorruptRobePower or EternalRobePower;
        // Only actual 0 -> positive application, not subsequent form stacks,
        // armour changes, exit notifications or failed applications.
        return MagicSupportRelicRules.GrantsOnForm(CorruptionQuery.Get(run), ownForm, power.Amount, amount)
            ? Grant(context) : Task.CompletedTask;
    }

    private async Task Grant(PlayerChoiceContext context)
    {
        Flash();
        await PowerCmd.Apply<MagicAmplificationPower>(context, Owner.Creature, 1, Owner.Creature, null);
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class BarrierGenerator : MSRelicTemplate
{
    private int _turnsSeen;
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override RelicAssetProfile AssetProfile => RelicIconAssets.For("character_BarrierGenerator");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<SanctuaryPower>()];
    public override bool ShowCounter => true;
    public override int DisplayAmount => TurnsSeen;

    [SavedProperty]
    public int TurnsSeen
    {
        get => _turnsSeen;
        set
        {
            AssertMutable();
            _turnsSeen = Math.Clamp(value, 0, 4);
            Status = _turnsSeen == 4 ? RelicStatus.Active : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterSideTurnStartLate(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner.Character is not MaidenSuccubusCharacter || HasBeenRemovedFromState || Owner.Creature.IsDead
            || !ReferenceEquals(Owner.Creature.CombatState, combatState) || side != Owner.Creature.Side
            || !participants.Contains(Owner.Creature))
            return;
        TurnsSeen = MagicSupportRelicRules.NextBarrierTurn(TurnsSeen);
        if (TurnsSeen != 0) return;
        // All normal side-start duration ticks finish before this late phase.
        // Player-start can precede side-start in the native asynchronous setup.
        Flash();
        await PowerCmd.Apply<SanctuaryPower>(new BlockingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        Status = RelicStatus.Normal;
        // Like Happy Flower, partial progress carries into the next combat.
        return Task.CompletedTask;
    }
}
