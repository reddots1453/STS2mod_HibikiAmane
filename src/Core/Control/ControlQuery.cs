using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Core.Control;

public sealed record EscapeProjection(
    ControlPower Control,
    CardModel OriginalCard)
{
    public int EscapeRemaining => Control.Amount;
    public ControlType Type => Control.ControlType;
}

public static class ControlQuery
{
    // CardModel.Keywords builds its result from Enchantment/Affliction, while the
    // projection patches for those properties also need to ask whether a card is
    // Portable. Without a guard that forms Keywords -> Enchantment -> projection
    // -> Keywords and eventually overflows the native stack when combat cards are
    // materialized.
    [ThreadStatic]
    private static bool _resolvingProjection;

    public static IReadOnlyList<ControlPower> GetInstances(Player player) =>
        player.Creature.Powers
            .OfType<ControlPower>()
            .Where(power => power.Amount > 0)
            .OrderBy(power => power.Amount)
            .ThenBy(power => SourceBattlefieldIndex(player, power))
            .ToArray();

    private static int SourceBattlefieldIndex(
        Player player,
        ControlPower power)
    {
        IReadOnlyList<MegaCrit.Sts2.Core.Entities.Creatures.Creature>? enemies =
            player.Creature.CombatState?.Enemies;
        if (enemies == null || power.Applier == null)
        {
            return int.MaxValue;
        }
        for (int index = 0; index < enemies.Count; index++)
        {
            if (ReferenceEquals(enemies[index], power.Applier))
            {
                return index;
            }
        }
        return int.MaxValue;
    }

    public static bool IsControlled(Player player) =>
        GetInstances(player).Count > 0;

    public static EscapeProjection? GetProjection(CardModel card)
    {
        if (_resolvingProjection || !card.IsMutable)
        {
            return null;
        }

        _resolvingProjection = true;
        try
        {
            // Unowned generated cards and canonical database models are queried by
            // UI/reward code too. They cannot participate in combat control.
            if (card.Pile?.IsCombatPile != true)
            {
                return null;
            }

            Player owner = card.Owner;
            if (owner.Character is not MaidenSuccubusCharacter
                || PortableKeyword.IsPortable(card))
            {
                return null;
            }

            ControlPower? control = GetInstances(owner)
                .FirstOrDefault(power => power.ControlType.Matches(card.Type));
            return control == null ? null : new EscapeProjection(control, card);
        }
        finally
        {
            _resolvingProjection = false;
        }
    }

    internal static EscapeProjection? GetProjection(
        CardModel card,
        IReadOnlySet<CardKeyword> rawKeywords)
    {
        if (_resolvingProjection || !card.IsMutable)
        {
            return null;
        }

        _resolvingProjection = true;
        try
        {
            if (card.Pile?.IsCombatPile != true)
            {
                return null;
            }

            Player owner = card.Owner;
            if (owner.Character is not MaidenSuccubusCharacter
                || rawKeywords.Contains(PortableKeyword.Value))
            {
                return null;
            }

            ControlPower? control = GetInstances(owner)
                .FirstOrDefault(power => power.ControlType.Matches(card.Type));
            return control == null ? null : new EscapeProjection(control, card);
        }
        finally
        {
            _resolvingProjection = false;
        }
    }
}
