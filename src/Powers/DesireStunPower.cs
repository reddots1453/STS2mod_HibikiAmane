using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

/// <summary>
/// Maximum-desire punishment based on Ceremonial Beast's RingingPower.
/// Ringing allows one card; this version blocks every card in hand.
/// </summary>
[RegisterPower]
public sealed class DesireStunPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => CommonPowerAssets.Generic;

    public override bool ShouldPlay(CardModel card, AutoPlayType _)
    {
        if (card.Owner.Creature != Owner)
        {
            return true;
        }

        return card.Pile?.Type != PileType.Hand;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        Flash();
        await PowerCmd.Decrement(this);
    }
}
