using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Keywords;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Models.Capabilities;

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
    private sealed class PresentationScope : IDisposable
    {
        public void Dispose() => _presentationSuppressionDepth--;
    }

    [ThreadStatic]
    private static int _presentationSuppressionDepth;

    internal static IDisposable SuppressPresentation()
    {
        _presentationSuppressionDepth++;
        return new PresentationScope();
    }

    public static IReadOnlyList<ControlPower> GetInstances(Player player)
    {
        List<ControlPower>? controls = null;
        foreach (PowerModel power in player.Creature.Powers)
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

    private static int SourceBattlefieldIndex(Player player, ControlPower power)
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
        foreach (PowerModel power in player.Creature.Powers)
        {
            if (power is ControlPower { Amount: > 0 })
            {
                return true;
            }
        }
        return false;
    }

    internal static ControlPower? FindMatchingControl(
        Player player,
        CardType cardType)
    {
        ControlPower? best = null;
        int bestBattlefieldIndex = int.MaxValue;
        foreach (PowerModel power in player.Creature.Powers)
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
        if (_presentationSuppressionDepth > 0
            || !card.IsMutable
            || !ModelCapabilities.TryGet(card, out ModelCapabilitySet? capabilities))
        {
            return null;
        }

        EscapeProjectionCapability? capability =
            capabilities.Get<EscapeProjectionCapability>();
        return capability != null
            && capability.TryCreateProjection(out EscapeProjection projection)
                ? projection
                : null;
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

        EscapeProjectionCapability? existing = null;
        ModelCapabilitySet? capabilities = null;
        if (ModelCapabilities.TryGet(card, out capabilities))
        {
            existing = capabilities.Get<EscapeProjectionCapability>();
        }

        ControlPower? control = null;
        if (card.Pile?.IsCombatPile == true
            && card.Owner.Character is MaidenSuccubusCharacter)
        {
            bool portable;
            using (SuppressPresentation())
            {
                portable = PortableKeyword.IsPortable(card);
            }
            if (!portable)
            {
                control = FindMatchingControl(card.Owner, card.Type);
            }
        }

        if (control == null)
        {
            if (existing != null)
            {
                capabilities!.Remove(existing);
            }
            return;
        }

        if (existing == null)
        {
            existing =
                ModelCapabilityRegistry.Create<EscapeProjectionCapability>();
            card.AddCapability(existing, allowMerge: false);
        }
        existing.Bind(control, GetUnprojectedTitle(card));
    }

    internal static bool IsValidBinding(CardModel card, ControlPower control)
    {
        if (card.Pile?.IsCombatPile != true
            || card.Owner.Character is not MaidenSuccubusCharacter
            || control.Amount <= 0
            || !control.ControlType.Matches(card.Type)
            || !card.Owner.Creature.Powers.Contains(control))
        {
            return false;
        }

        return ReferenceEquals(
            FindMatchingControl(card.Owner, card.Type),
            control);
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
