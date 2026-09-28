using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Core.Cards;

internal static class HumilityRewritePresentation
{
    internal static readonly WeakInstanceValueScope<CardModel, Creature?> Targets = new();

    internal static LocString Description(CardModel card, HumilityRewriteCapability capability)
    {
        Targets.TryGet(card, out Creature? target);
        HumilityXValues x = HumilityNativeEffects.XForPreview(card);
        List<string> lines = [];
        foreach (HumilityEffect effect in capability.Program.Effects)
        {
            decimal amount = effect.Amount.Evaluate(x, name => HumilityNativeEffects.ResolveValue(card, name, target))
                * capability.Program.AmountMultiplier;
            DynamicVar variable = effect.Kind == HumilityEffectKind.Damage
                ? effect.Source == HumilityAttackSource.Osty
                    ? new OstyDamageVar(amount, ValueProp.Move) : new DamageVar(amount, ValueProp.Move)
                : new BlockVar(amount, ValueProp.Move);
            variable.SetOwner(card);
            variable.UpdateCardPreview(card, CardPreviewMode.Normal, target, runGlobalHooks: card.CombatState != null);
            decimal repeats = Math.Max(0, decimal.Truncate(effect.Repeats.Evaluate(x,
                name => HumilityNativeEffects.ResolveValue(card, name, target))));
            string key = effect.Kind == HumilityEffectKind.Damage ? "damage" : "block";
            var line = new LocString("cards", $"MAIDEN_HUMILITY_REWRITE.{key}");
            line.Add("Amount", variable.ToHighlightedString(inverse: false));
            var times = new LocString("cards", "MAIDEN_HUMILITY_REWRITE.times");
            times.Add("Times", repeats);
            line.Add("Repeats", repeats == 1 ? "" : times.GetFormattedText());
            HumilityTarget effectTarget = HumilityNativeEffects.ResolveTarget(card, effect.Target);
            string targetKey = effectTarget == HumilityTarget.Self && effect.Kind == HumilityEffectKind.Damage
                ? "DamageSelf" : effectTarget == HumilityTarget.Selected && effect.Kind == HumilityEffectKind.Block
                    ? "BlockSelected" : effectTarget.ToString();
            line.Add("Target", new LocString("cards", $"MAIDEN_HUMILITY_REWRITE.target_{targetKey}"));
            lines.Add(line.GetFormattedText());
        }
        var description = new LocString("cards", "MAIDEN_HUMILITY_REWRITE.description");
        description.Add("Effects", string.Join('\n', lines));
        return description;
    }
}
