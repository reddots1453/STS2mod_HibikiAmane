using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class MultipleReproductionPower : MaidenSuccubusPowerTemplate
{
    public bool DelayOneTurn { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldTakeExtraTurn(Player player)
    {
        if (player.Creature != Owner)
            return false;
        if (DelayOneTurn)
        {
            DelayOneTurn = false;
            return false;
        }
        return true;
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player.Creature == Owner)
            return PowerCmd.Remove(this);
        return Task.CompletedTask;
    }
}
