using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Replay;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Keywords;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class ChastityDefensePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public void NotifyInvasionBlocked() => Flash();

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext context,
        Player player)
    {
        if (player.Creature == Owner)
        {
            await PowerCmd.Decrement(this);
        }
    }
}

[RegisterPower]
public sealed class RegenerativeMagicFiberPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext context,
        Player player)
    {
        if (player.Creature == Owner)
        {
            await TransformationCmd.GainArmor(
                context, Owner, (int)Amount, null);
        }
    }
}

[RegisterPower]
public sealed class RestNextTurnPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext context,
        Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        await CardPileCmd.Draw(context, 2, player);
        await PlayerCmd.GainEnergy(2, player);
        await PowerCmd.Remove(this);
    }
}

[RegisterPower]
public sealed class EctoplasmResidueEnergyLossPower :
    MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }
        await PlayerCmd.LoseEnergy(Amount, player);
        await PowerCmd.Remove(this);
    }
}

[RegisterPower]
public sealed class EternalRobePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}

[RegisterPower]
public sealed class TacticalCorePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext context,
        Player player)
    {
        if (player.Creature == Owner)
        {
            await PowerCmd.Decrement(this);
        }
    }
}

[RegisterPower]
public sealed class BattleTechniqueReplayPower : MaidenSuccubusPowerTemplate
{
    private readonly Dictionary<CardModel, bool> _generatedOrigins = [];
    private readonly Dictionary<CardModel, bool> _pendingRestores = [];
    private bool _changing;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (_changing
            || !cardPlay.IsLastInSeries
            || cardPlay.Card.Owner.Creature != Owner
            || Owner.Player == null
            || Owner.CombatState == null)
        {
            return;
        }

        _changing = true;
        try
        {
            if (TryGetReplayOrigin(cardPlay.Card, out bool originUpgraded))
            {
                // AfterCardPlayed runs while the card is still in PileType.Play.
                // Restoring it here strands the replacement there because the
                // engine later moves the original (already removed) instance.
                _pendingRestores[cardPlay.Card] = originUpgraded;
                return;
            }

            CardModel[] replayCards = Owner.Player.PlayerCombatState!.AllCards
                .Where(card => card != cardPlay.Card
                    && (card is BattleTechniqueReplay
                        || card.DeckVersion is BattleTechniqueReplay
                        || _generatedOrigins.ContainsKey(card)))
                .ToArray();
            foreach (CardModel replay in replayCards)
            {
                if (replay.Pile == null || !replay.IsTransformable)
                {
                    continue;
                }
                bool upgraded = replay.DeckVersion is BattleTechniqueReplay deckReplay
                    ? deckReplay.IsUpgraded
                    : replay is BattleTechniqueReplay replayModel
                        ? replayModel.IsUpgraded
                        : _generatedOrigins.GetValueOrDefault(replay);
                CardModel copy = Owner.CombatState.CloneCard(cardPlay.Card);
                copy.DeckVersion = replay.DeckVersion;
                copy.AddKeyword(BattleReplayKeyword.Value);
                copy.AddCapability(
                    ModelCapabilityRegistry.Create<BattleReplayOriginCapability>(),
                    allowMerge: false);
                if (upgraded)
                {
                    copy.AddKeyword(CardKeyword.Retain);
                }
                var result = await CardCmd.Transform(
                    replay,
                    copy,
                    MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
                if (result?.cardAdded != null && copy.DeckVersion == null)
                {
                    _generatedOrigins[result.Value.cardAdded] = upgraded;
                }
            }
        }
        finally
        {
            _changing = false;
        }
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        if (_changing
            || oldPileType != PileType.Play
            || !_pendingRestores.Remove(card, out bool originUpgraded))
        {
            return;
        }

        _generatedOrigins.Remove(card);
        if (card.Pile == null
            || !card.IsTransformable
            || Owner.Player == null
            || Owner.CombatState == null)
        {
            return;
        }

        _changing = true;
        try
        {
            BattleTechniqueReplay restored =
                Owner.CombatState.CreateCard<BattleTechniqueReplay>(Owner.Player);
            if (originUpgraded)
            {
                CardCmd.Upgrade(restored);
            }
            restored.DeckVersion = card.DeckVersion;
            await CardCmd.Transform(
                card,
                restored,
                MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
        }
        finally
        {
            _changing = false;
        }
    }

    private bool TryGetReplayOrigin(CardModel card, out bool upgraded)
    {
        if (card is BattleTechniqueReplay replay)
        {
            upgraded = replay.IsUpgraded;
            return true;
        }
        if (card.DeckVersion is BattleTechniqueReplay deckReplay)
        {
            upgraded = deckReplay.IsUpgraded;
            return true;
        }
        return _generatedOrigins.TryGetValue(card, out upgraded);
    }
}
