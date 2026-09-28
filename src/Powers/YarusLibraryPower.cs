using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class YarusLibraryPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    private bool IsActive => IsMutable && Amount > 0 && Owner.IsAlive && Owner.Powers.Contains(this);
    public override bool ShouldDraw(Player player, bool fromHandDraw) => !IsActive || player.Creature != Owner;

    // Preserve the old serialized Power ID; the source card is now TemperanceCirclet.
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (!IsActive || player.Creature != Owner || Owner.CombatState == null
            || CombatManager.Instance.IsOverOrEnding)
            return Task.CompletedTask;
        return MaidenSuccubus.Commands.TemperancePileCmd.Play(context, player, 10, () => IsActive);
    }
}
