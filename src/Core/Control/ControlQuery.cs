using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Core.Control;

public readonly record struct EscapeProjection(
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

    public static IReadOnlyList<ControlPower> GetInstances(Player player)
    {
        List<ControlPower>? controls = null;
        foreach (var power in player.Creature.Powers)
        {
            if (power is not ControlPower { Amount: > 0 } control)
            {
                continue;
            }
            controls ??= [];
            controls.Add(control);
        }

        if (controls == null)
        {
            return Array.Empty<ControlPower>();
        }

        controls.Sort((left, right) =>
        {
            int amountComparison = left.Amount.CompareTo(right.Amount);
            return amountComparison != 0
                ? amountComparison
                : SourceBattlefieldIndex(player, left).CompareTo(
                    SourceBattlefieldIndex(player, right));
        });
        return controls;
    }

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

    public static bool IsControlled(Player player)
    {
        foreach (var power in player.Creature.Powers)
        {
            if (power is ControlPower { Amount: > 0 })
            {
                return true;
            }
        }
        return false;
    }

    private static ControlPower? FindMatchingControl(
        Player player,
        CardType cardType)
    {
        ControlPower? best = null;
        int bestBattlefieldIndex = int.MaxValue;
        foreach (var power in player.Creature.Powers)
        {
            if (power is not ControlPower { Amount: > 0 } control
                || !control.ControlType.Matches(cardType))
            {
                continue;
            }

            int battlefieldIndex = SourceBattlefieldIndex(player, control);
            if (best == null
                || control.Amount < best.Amount
                || (control.Amount == best.Amount
                    && battlefieldIndex < bestBattlefieldIndex))
            {
                best = control;
                bestBattlefieldIndex = battlefieldIndex;
            }
        }
        return best;
    }

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
            if (owner.Character is not MaidenSuccubusCharacter)
            {
                return null;
            }

            ControlPower? control = FindMatchingControl(owner, card.Type);
            // Most combat frames have no matching control at all. Checking
            // that cheap condition before asking for Keywords avoids entering
            // CardModel.Keywords (and the Harmony projection chain again) for
            // every title/description/target refresh of every visible card.
            if (control == null || PortableKeyword.IsPortable(card))
            {
                return null;
            }
            return new EscapeProjection(control, card);
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

            ControlPower? control = FindMatchingControl(owner, card.Type);
            return control == null ? null : new EscapeProjection(control, card);
        }
        finally
        {
            _resolvingProjection = false;
        }
    }
}
