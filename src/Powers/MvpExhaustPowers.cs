using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Core.Desire;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class DesireRecyclePower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal)
    {
        if (card.Owner.Creature != Owner
            || !card.TryGetSecondaryCosts(out var costs)
            || !costs.Get(DesireResource.Id).IsMaterial)
            return;
        await Data.Desire.Modify(card.Owner, (int)Amount);
        await CardPileCmd.Draw(context, (int)Amount, card.Owner);
    }
}
