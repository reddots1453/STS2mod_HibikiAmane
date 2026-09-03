using MegaCrit.Sts2.Core.Entities.Creatures;

namespace MaidenSuccubus.Core.Transformation;

public static class TransformationEvents
{
    public static event Action<Creature>? Changed;

    internal static void Publish(Creature creature)
    {
        foreach (Action<Creature> handler in
            Changed?.GetInvocationList().Cast<Action<Creature>>() ?? [])
        {
            try
            {
                handler(creature);
            }
            catch (Exception ex)
            {
                MaidenSuccubusMod.Logger.Warn(
                    $"Transformation listener failed: {ex.Message}");
            }
        }
    }
}
