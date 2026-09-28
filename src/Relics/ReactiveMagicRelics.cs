using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
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
using MaidenSuccubus.Util;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Relics;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class CounterCurseMirror : MSRelicTemplate
{
    // Shared by copies owned by the same creature, not by all players. The
    // entire awaited reaction chain is guarded, including effects of judgment.
    private static readonly WeakInstanceScope<Creature> Reactions = new();
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override RelicAssetProfile AssetProfile => MagicSupportRelicAssets.Placeholder;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<BurningPower>(), HoverTipFactory.FromPower<CondemnationPower>()];

    internal bool HolyVariation => IsMutable && Owner is { Character: MaidenSuccubusCharacter, RunState: RunState run }
        && ReactiveMagicRelicRules.MirrorVariation(CorruptionQuery.Get(run));

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (!IsMutable || Owner is not { Character: MaidenSuccubusCharacter }
            || HasBeenRemovedFromState || Owner.Creature.IsDead || Owner.Creature.CombatState == null)
            return;
        Creature self = Owner.Creature;
        Creature target = power.Owner;
        bool enemy = target.IsAlive && target.Side != self.Side
            && ReferenceEquals(target.CombatState, self.CombatState);
        if (!ReactiveMagicRelicRules.ShouldReflect(ReferenceEquals(applier, self), enemy,
                amount, power.GetTypeForAmount(amount) == PowerType.Debuff, Reactions.Contains(self)))
            return;
        using (Reactions.Enter(self))
        {
            Flash();
            if (HolyVariation)
                await PowerCmd.Apply<CondemnationPower>(context, target, 1, self, null);
            else
                await PowerCmd.Apply<BurningPower>(context, target, 1, self, null);
        }
    }
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class DivineStardust : MSRelicTemplate
{
    private sealed class PlayReceipt(bool eligible)
    {
        internal readonly bool Eligible = eligible;
        internal bool Counted;
    }

    private ConditionalWeakTable<CardPlay, PlayReceipt> _plays = new();
    private int _progress;
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override RelicAssetProfile AssetProfile => MagicSupportRelicAssets.Placeholder;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<MagicAmplificationPower>()];
    public override bool ShowCounter => true;
    public override int DisplayAmount => Progress;

    [SavedProperty]
    public int Progress
    {
        get => _progress;
        set
        {
            AssertMutable();
            _progress = Math.Clamp(value, 0, 6);
            Status = _progress == 6 ? RelicStatus.Active : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    private bool CanTrigger => IsMutable && Owner is { Character: MaidenSuccubusCharacter }
        && !HasBeenRemovedFromState && Owner.Creature.IsAlive && Owner.Creature.CombatState != null;

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _plays = new();
    }

    public override Task BeforeCombatStart()
    {
        _plays = new();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _plays = new();
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (!CanTrigger || !ReferenceEquals(creator, Owner) || card.Enchantment == null
            || !ReferenceEquals(card.CombatState, Owner.Creature.CombatState))
            return Task.CompletedTask;
        return CountOne(new BlockingPlayerChoiceContext());
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (CanTrigger)
            _plays.GetValue(cardPlay, play => new PlayReceipt(ReferenceEquals(play.Player, Owner)
                && ReferenceEquals(play.Card.CombatState, Owner.Creature.CombatState) && play.Card.Enchantment != null));
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!CanTrigger || !_plays.TryGetValue(cardPlay, out PlayReceipt? receipt)
            || !receipt.Eligible || receipt.Counted)
            return Task.CompletedTask;
        // Consume before awaiting: re-entry cannot count this play twice.
        // Each replay has its own CardPlay; an expiring enchantment is still counted.
        receipt.Counted = true;
        return CountOne(context);
    }

    private async Task CountOne(PlayerChoiceContext context)
    {
        Progress = ReactiveMagicRelicRules.NextStardust(Progress);
        if (Progress != 0) return;
        Flash();
        await PowerCmd.Apply<MagicAmplificationPower>(context, Owner.Creature, 2, Owner.Creature, null);
    }
}
