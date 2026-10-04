using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Combat.SecondaryResources;

namespace MaidenSuccubus.Core.Desire;

/// <summary>Actual gains, independently scoped to this combat and player.</summary>
internal static class DesireCombatGains
{
    private static readonly CombatSpendLedger<ICombatState, Player> Ledger = new();

    internal static int Get(Player player) =>
        player.PlayerCombatState == null ? 0 : Ledger.Get(player.Creature.CombatState, player);

    internal static void Record(SecondaryResourceChangeContext context)
    {
        // Set/Reset/restore notifications cannot create battle growth. Record
        // the committed gain before a maximum-desire penalty lowers the amount.
        if (context.Definition.Id != DesireResource.Id
            || context.Reason != SecondaryResourceChangeReason.Gain
            || context.NewAmount <= context.OldAmount
            || context.Player.PlayerCombatState == null
            || !ReferenceEquals(context.Player.Creature.CombatState, context.CombatState))
            return;
        Ledger.Record(context.CombatState, context.Player, context.NewAmount - context.OldAmount);
    }

    internal static void Close(ICombatState combat) => Ledger.Close(combat);
}
