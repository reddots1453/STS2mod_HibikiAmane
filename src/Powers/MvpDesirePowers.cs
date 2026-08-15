using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MaidenSuccubus.Core.Desire;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class LegendaryMinerPower : ModPowerTemplate, ISecondaryResourceHookListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public Task AfterSecondaryResourceChanged(SecondaryResourceChangeContext context)
    {
        if (context.Definition.Id != DesireResource.Id
            || context.Player.Creature != Owner
            || context.Delta <= 0)
            return Task.CompletedTask;

        Flash();
        return CreatureCmd.GainBlock(Owner, context.Delta * Amount, MegaCrit.Sts2.Core.ValueProps.ValueProp.Move, null);
    }

    public Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context)
    {
        if (context.Definition.Id != DesireResource.Id || context.Player.Creature != Owner)
            return Task.CompletedTask;

        Flash();
        return CreatureCmd.GainBlock(Owner, context.Amount * Amount, MegaCrit.Sts2.Core.ValueProps.ValueProp.Move, null);
    }
}
