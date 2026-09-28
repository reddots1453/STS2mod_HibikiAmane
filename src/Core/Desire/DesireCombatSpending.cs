using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Combat.SecondaryResources;

namespace MaidenSuccubus.Core.Desire;

internal static class DesireCombatSpending
{
    private static readonly CombatSpendLedger<ICombatState, Player> Ledger = new();

    internal static int Get(Player player) =>
        player.PlayerCombatState == null ? 0 : Ledger.Get(player.Creature.CombatState, player);

    internal static void Record(SecondaryResourceSpendContext context)
    {
        if (context.Definition.Id != DesireResource.Id || context.Amount <= 0
            || context.Player.PlayerCombatState == null
            || !ReferenceEquals(context.Player.Creature.CombatState, context.CombatState))
            return;
        Ledger.Record(context.CombatState, context.Player, context.Amount);
    }

    internal static void Close(ICombatState combat) => Ledger.Close(combat);

#if DEBUG
    internal static void ResetForTests(ICombatState combat) => Ledger.ResetForTests(combat);
#endif
}
