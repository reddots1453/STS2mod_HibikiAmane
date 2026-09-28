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
        Dictionary<int, decimal> damageResults = [];
        for (int index = 0; index < capability.Program.Effects.Count; index++)
        {
            HumilityEffect effect = capability.Program.Effects[index];
            Creature? previewTarget = effect.Target == HumilityTarget.LowestHpEnemy
                ? HumilityNativeEffects.LowestHpEnemy(card) : target;
            decimal Resolve(string name) => name.StartsWith("$effectDamage:", StringComparison.Ordinal)
                ? damageResults[int.Parse(name[14..], System.Globalization.CultureInfo.InvariantCulture)]
                : HumilityNativeEffects.ResolveValue(card, name, previewTarget);
            decimal amount = effect.Amount.Evaluate(x, Resolve)
                * capability.Program.AmountMultiplier;
            DynamicVar variable = effect.Kind == HumilityEffectKind.Damage
                ? effect.Source == HumilityAttackSource.Osty
                    ? new OstyDamageVar(amount, ValueProp.Move) : new DamageVar(amount, ValueProp.Move)
                : new BlockVar(amount, ValueProp.Move);
            variable.SetOwner(card);
            variable.UpdateCardPreview(card, CardPreviewMode.Normal, previewTarget, runGlobalHooks: card.CombatState != null);
            decimal repeats = Math.Max(0, decimal.Truncate(effect.Repeats.Evaluate(x, Resolve)));
            damageResults[index] = effect.Kind == HumilityEffectKind.Damage
                ? Math.Max(0, decimal.Floor(variable.PreviewValue)) * repeats : 0;
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
