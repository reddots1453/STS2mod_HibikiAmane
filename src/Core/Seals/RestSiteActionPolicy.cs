using MegaCrit.Sts2.Core.Entities.Players;

namespace MaidenSuccubus.Core.Seals;

public static class RestSiteActionPolicy
{
    private static readonly HashSet<Player> PreserveOptionsFor = [];

    public static void PreserveRemainingOptionsOnce(Player player)
    {
        lock (PreserveOptionsFor)
        {
            PreserveOptionsFor.Add(player);
        }
    }

    public static bool ConsumePreserveRemainingOptions(Player player)
    {
        lock (PreserveOptionsFor)
        {
            return PreserveOptionsFor.Remove(player);
        }
    }
}
