using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Hooks;
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
    private sealed class PreviewTrace { internal string Text = "not captured"; }
    private static readonly ConditionalWeakTable<CardModel, PreviewTrace> LastTargetPreviews = new();
    internal static string LastTargetPreview(CardModel card) =>
        LastTargetPreviews.TryGetValue(card, out var trace) ? trace.Text : "not captured";

    internal static LocString Description(CardModel card, HumilityRewriteCapability capability)
    {
        Targets.TryGet(card, out Creature? target);
        HumilityXValues x = HumilityNativeEffects.XForPreview(card);
        List<string> lines = [];
        List<string> previewTrace = [];
        Dictionary<int, decimal> damageResults = [];
        Dictionary<int, decimal> firstDamageResults = [];
        for (int index = 0; index < capability.Program.Effects.Count; index++)
        {
            HumilityEffect effect = capability.Program.Effects[index];
            damageResults[index] = 0;
            firstDamageResults[index] = 0;
            if (effect.RequiredCardType != null && card.Type.ToString() != effect.RequiredCardType) continue;
            Creature? previewTarget = effect.Target == HumilityTarget.LowestHpEnemy
                ? HumilityNativeEffects.LowestHpEnemy(card) : target;
            decimal Resolve(string name) => name.StartsWith("$effectDamage:", StringComparison.Ordinal)
                ? damageResults[int.Parse(name[14..], System.Globalization.CultureInfo.InvariantCulture)]
                : name.StartsWith("$effectFirstDamage:", StringComparison.Ordinal)
                    ? firstDamageResults[int.Parse(name[19..], System.Globalization.CultureInfo.InvariantCulture)]
                : HumilityNativeEffects.ResolveValue(card, name, previewTarget);
            decimal amount = effect.Amount.Evaluate(x, Resolve)
                * capability.Program.AmountMultiplier;
            DynamicVar variable = effect.Kind == HumilityEffectKind.Damage
                ? effect.Source == HumilityAttackSource.Osty
                    ? new OstyDamageVar(amount, ValueProp.Move) : new DamageVar(amount,
                        HumilityNativeEffects.DamageProps(card, effect.Source))
                : new BlockVar(amount, ValueProp.Move);
            variable.SetOwner(card);
            if (effect.Kind == HumilityEffectKind.Damage && card.CombatState is { } combat)
            {
                // Use the same entry point as CreatureCmd.Damage/AttackCommand.
                // A synthetic DamageVar can receive extra preview-only patches
                // from other mods that the real attack never receives.
                Creature? dealer = effect.Source == HumilityAttackSource.Osty
                    ? card.Owner.Osty : card.Owner.Creature;
                variable.PreviewValue = dealer == null ? 0 : Hook.ModifyDamage(
                    card.Owner.RunState, combat, previewTarget, dealer, amount,
                    HumilityNativeEffects.DamageProps(card, effect.Source), card, null,
                    ModifyDamageHookType.All, CardPreviewMode.Normal, out _);
            }
            else variable.UpdateCardPreview(card, CardPreviewMode.Normal, previewTarget,
                runGlobalHooks: card.CombatState != null);
            previewTrace.Add($"effect={index},base={amount},preview={variable.PreviewValue},target={previewTarget?.Monster?.Id.Entry ?? "none"}");
            decimal repeats = Math.Max(0, decimal.Truncate(effect.Repeats.Evaluate(x, Resolve)));
            damageResults[index] = effect.Kind == HumilityEffectKind.Damage
                ? Math.Max(0, decimal.Floor(variable.PreviewValue)) * repeats : 0;
            firstDamageResults[index] = effect.Kind == HumilityEffectKind.Damage && repeats > 0
                ? Math.Max(0, decimal.Floor(variable.PreviewValue)) : 0;
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
        if (target != null)
            LastTargetPreviews.GetValue(card, _ => new PreviewTrace()).Text = string.Join(";", previewTrace);
        var description = new LocString("cards", "MAIDEN_HUMILITY_REWRITE.description");
        description.Add("Effects", string.Join('\n', lines));
        return description;
    }
}
