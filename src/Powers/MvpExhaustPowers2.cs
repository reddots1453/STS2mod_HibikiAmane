using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class IgnitePower : MaidenSuccubusPowerTemplate
{
    [SavedProperty] public string CardId { get; set; } = string.Empty;
    [SavedProperty] public bool WasUpgraded { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner) return;
        CardModel? card = PileType.Exhaust.GetPile(player).Cards.FirstOrDefault(
            candidate => candidate.Id.Entry == CardId && candidate.IsUpgraded == WasUpgraded);
        if (card != null) await CardCmd.AutoPlay(context, card, null);
        await PowerCmd.Remove(this);
    }
}

[RegisterPower]
public sealed class ChainDestructionPower : MaidenSuccubusPowerTemplate
{
    [SavedProperty] public int ExhaustProgress { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => 4 - ExhaustProgress % 4;

    public override async Task AfterCardExhausted(
        PlayerChoiceContext context,
        CardModel card,
        bool causedByEthereal)
    {
        if (card.Owner.Creature != Owner) return;
        ExhaustProgress++;
        if (ExhaustProgress >= 4)
        {
            int replays = ExhaustProgress / 4;
            ExhaustProgress %= 4;
            Flash();
            await PowerCmd.Apply<ChainDestructionReplayPower>(
                context,
                Owner,
                replays,
                Owner,
                null);
        }
        InvokeDisplayAmountChanged();
    }
}

[RegisterPower]
public sealed class ChainDestructionReplayPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        card.Owner.Creature == Owner ? playCount + 1 : playCount;

    public override async Task AfterModifyingCardPlayCount(CardModel card)
    {
        await PowerCmd.Decrement(this);
    }
}

[RegisterPower]
public sealed class CurseCorridorPower : MaidenSuccubusPowerTemplate
{
    private int _pendingDraw;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player.Creature != Owner) return count;
        _pendingDraw = Math.Max(0, (int)count + 1);
        return 0;
    }

    public override async Task AfterModifyingHandDraw()
    {
        if (_pendingDraw <= 0 || Owner.Player == null) return;
        CardModel[] cards = PileType.Exhaust.GetPile(Owner.Player).Cards.Take(_pendingDraw).ToArray();
        _pendingDraw = 0;
        foreach (CardModel card in cards)
            await CardPileCmd.Add(card, PileType.Hand);
    }
}
