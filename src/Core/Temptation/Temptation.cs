using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Core.Temptation;

public readonly record struct TemptationChanged(Player Player, int NewValue);

public static class TemptationEvents
{
    public static event Action<TemptationChanged>? Changed;

    internal static void Publish(Creature creature)
    {
        if (creature.Player is { } player)
        {
            Publish(player);
        }
    }

    internal static void Publish(Player player)
    {
        foreach (Action<TemptationChanged> handler in
            Changed?.GetInvocationList().Cast<Action<TemptationChanged>>() ?? [])
        {
            try
            {
                handler(new TemptationChanged(player, Temptation.Get(player)));
            }
            catch (Exception ex)
            {
                MaidenSuccubusMod.Logger.Warn(
                    $"Temptation Changed listener failed: {ex.Message}");
            }
        }
    }
}

public static class Temptation
{
    public const int ImmaculateBase = 10;
    public const int CorruptBase = 20;
    public const int PerMissingArmor = 20;
    public const int TransparentOutfitAmount = 30;

    public static int Get(Player player)
    {
        Creature creature = player.Creature;
        int total = BaseValue(creature)
            + (creature.GetPower<TemptationRuntimePower>()?.Modifier ?? 0);
        if (creature.CombatState != null)
        {
            total += PileType.Hand.GetPile(player).Cards
                .Count(card => card is TransparentOutfitCurse)
                * TransparentOutfitAmount;
        }
        return total;
    }

    public static int BaseValue(Creature creature)
    {
        bool transformed = TransformationCmd.IsTransformed(creature);
        int baseValue = FormBaseValue(creature);
        if (!transformed)
        {
            return baseValue;
        }

        return baseValue + ArmorModifier(creature);
    }

    public static int FormBaseValue(Creature creature) =>
        TransformationCmd.IsTransformed(creature) && IsCorruptForm(creature)
            ? CorruptBase
            : ImmaculateBase;

    public static int ArmorModifier(Creature creature)
    {
        if (!TransformationCmd.IsTransformed(creature))
        {
            return 0;
        }
        int armor = (int)(TransformationCmd.GetArmor(creature)?.Amount ?? 0);
        return Math.Max(0, TransformationCmd.MaxArmor - Math.Min(
            TransformationCmd.MaxArmor,
            armor)) * PerMissingArmor;
    }

    public static async Task Initialize(
        PlayerChoiceContext context,
        Player player)
    {
        if (player.Creature.GetPower<TemptationRuntimePower>() == null)
        {
            await PowerCmd.Apply<TemptationRuntimePower>(
                context,
                player.Creature,
                1,
                player.Creature,
                null,
                silent: true);
        }
        TemptationEvents.Publish(player);
    }

    public static async Task Modify(
        PlayerChoiceContext context,
        Player player,
        int delta)
    {
        TemptationRuntimePower? carrier =
            player.Creature.GetPower<TemptationRuntimePower>();
        if (carrier == null)
        {
            await Initialize(context, player);
            carrier = player.Creature.GetPower<TemptationRuntimePower>();
        }
        if (carrier == null || delta == 0)
        {
            return;
        }

        carrier.Modifier += delta;
        TemptationEvents.Publish(player);
    }

    public static void NotifyHandChanged(Player player) =>
        TemptationEvents.Publish(player);

    private static bool IsCorruptForm(Creature creature) =>
        creature.HasPower<CorruptRobePower>();
}
