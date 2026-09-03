using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Patches;
using MaidenSuccubus.Powers;
using System.Runtime.CompilerServices;

namespace MaidenSuccubus.Core.Control;

public readonly record struct EscapeProjection(
    ControlPower Control,
    CardModel OriginalCard,
    string OriginalTitle)
{
    public int EscapeRemaining => Control.Amount;
    public ControlType Type => Control.ControlType;
}

public static class ControlQuery
{
    private sealed class ProjectionHolder
    {
        public required EscapeProjection Projection { get; set; }
    }

    // Projection membership belongs to the concrete combat card instance.  The
    // original card is never replaced or mutated; after loading a combat the table
    // is deterministically rebuilt from the saved ControlPower instances.
    private static readonly ConditionalWeakTable<CardModel, ProjectionHolder>
        Projections = new();

    // CardModel.Keywords is itself projected.  Portable detection must be able to
    // inspect the unprojected keyword result once without recursively re-entering
    // projection resolution.
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

        if (Projections.TryGetValue(card, out ProjectionHolder? existing)
            && IsStillValid(card, existing.Projection, rawKeywords: null))
        {
            return existing.Projection;
        }

        _resolvingProjection = true;
        try
        {
            return RefreshCard(card, rawKeywords: null);
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
        if (!card.IsMutable)
        {
            return null;
        }

        if (Projections.TryGetValue(card, out ProjectionHolder? existing)
            && IsStillValid(card, existing.Projection, rawKeywords))
        {
            return existing.Projection;
        }

        if (_resolvingProjection)
        {
            return null;
        }

        _resolvingProjection = true;
        try
        {
            return RefreshCard(card, rawKeywords);
        }
        finally
        {
            _resolvingProjection = false;
        }
    }

    public static void Refresh(Player player)
    {
        foreach (CardPile pile in player.Piles.Where(pile => pile.IsCombatPile))
        {
            foreach (CardModel card in pile.Cards)
            {
                Refresh(card);
            }
        }
    }

    public static void Refresh(CardModel card)
    {
        if (!card.IsMutable)
        {
            return;
        }

        _resolvingProjection = true;
        try
        {
            RefreshCard(card, rawKeywords: null);
        }
        finally
        {
            _resolvingProjection = false;
        }
    }

    private static EscapeProjection? RefreshCard(
        CardModel card,
        IReadOnlySet<CardKeyword>? rawKeywords)
    {
        Projections.Remove(card);
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
        bool portable = rawKeywords?.Contains(PortableKeyword.Value)
            ?? PortableKeyword.IsPortable(card);
        if (control == null || portable)
        {
            return null;
        }

        EscapeProjection projection = new(
            control,
            card,
            GetUnprojectedTitle(card));
        Projections.Add(card, new ProjectionHolder { Projection = projection });
        EscapeEffectPatcher.EnsurePatched(card);
        return projection;
    }

    private static bool IsStillValid(
        CardModel card,
        EscapeProjection projection,
        IReadOnlySet<CardKeyword>? rawKeywords)
    {
        if (card.Pile?.IsCombatPile != true
            || card.Owner.Character is not MaidenSuccubusCharacter
            || projection.Control.Amount <= 0
            || !projection.Control.ControlType.Matches(card.Type)
            || !card.Owner.Creature.Powers.Contains(projection.Control))
        {
            Projections.Remove(card);
            return false;
        }

        bool portable = rawKeywords?.Contains(PortableKeyword.Value)
            ?? PortableKeyword.IsPortable(card);
        if (portable)
        {
            Projections.Remove(card);
            return false;
        }

        ControlPower? current = FindMatchingControl(card.Owner, card.Type);
        if (!ReferenceEquals(current, projection.Control))
        {
            Projections.Remove(card);
            return false;
        }
        return true;
    }

    private static string GetUnprojectedTitle(CardModel card)
    {
        string title = card.TitleLocString.GetFormattedText();
        if (!card.IsUpgraded)
        {
            return title;
        }
        return card.MaxUpgradeLevel > 1
            ? $"{title}+{card.CurrentUpgradeLevel}"
            : title + "+";
    }
}
