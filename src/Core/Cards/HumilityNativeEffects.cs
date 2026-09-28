using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Powers;
using MaidenSuccubus.Core.Intents;
using STS2RitsuLib.Combat.SecondaryResources;

namespace MaidenSuccubus.Core.Cards;

internal sealed class HumilityNativeEffects(PlayerChoiceContext context, CardPlay play) : IHumilityEffectSink
{
    private readonly ICombatState? _combat = play.Card.CombatState;
    private readonly Dictionary<int, decimal> _damageResults = [];
    private int _effectIndex;
    public void BeginEffect(int index)
    {
        _effectIndex = index;
        _damageResults[index] = 0; // Skipped/zero-hit attacks still have an indexed empty result.
    }
    internal decimal ResolveValue(string name) => name.StartsWith("$effectDamage:", StringComparison.Ordinal)
        ? _damageResults[int.Parse(name[14..], System.Globalization.CultureInfo.InvariantCulture)]
        : ResolveValue(play.Card, name, play.Target);
    public bool CanContinue => _combat != null && ReferenceEquals(play.Card.CombatState, _combat)
        && CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding
        && !play.Player.Creature.IsDead;

    internal static void Validate(HumilityEffectProgram program)
    {
        foreach (HumilityEffect effect in program.Effects)
        {
            bool valid = effect.Kind == HumilityEffectKind.Damage
                ? effect.Target is HumilityTarget.Selected or HumilityTarget.Self or HumilityTarget.AllEnemies or HumilityTarget.RandomEnemy or HumilityTarget.CurrentCardTarget or HumilityTarget.LowestHpEnemy
                : effect.Target is HumilityTarget.Selected or HumilityTarget.Self or HumilityTarget.AllAllies or HumilityTarget.AllPlayers;
            if (!valid) throw new ArgumentException("This humility target/operation needs a native adapter.", nameof(program));
        }
    }

    internal static HumilityTarget ResolveTarget(CardModel card, HumilityTarget target) =>
        target != HumilityTarget.CurrentCardTarget ? target : card.TargetType switch
        {
            TargetType.AnyEnemy => HumilityTarget.Selected,
            TargetType.AllEnemies => HumilityTarget.AllEnemies,
            _ => throw new InvalidOperationException($"Unsupported dynamic humility target for {card.Id}: {card.TargetType}."),
        };

    internal static HumilityXValues XForPlay(CardPlay play) => new(
        play.Card.EnergyCost.CostsX ? play.Card.ResolveEnergyXValue() : play.Resources.EnergyValue,
        play.Card.HasStarCostX ? play.Card.ResolveStarXValue() : play.Resources.StarValue,
        checked((int)play.SecondaryResources().Value(DesireResource.Id)));

    internal static HumilityXValues XForPreview(CardModel card)
    {
        if (card.CombatState is not { } combat) return default;
        return new(
            card.EnergyCost.CostsX ? Hook.ModifyXValue(combat, card, card.EnergyCost.GetAmountToSpend()) : 0,
            card.HasStarCostX ? Hook.ModifyXValue(combat, card, card.GetStarCostWithModifiers()) : 0,
            (int)Data.Desire.Get(card.Owner));
    }

    internal static Creature? LowestHpEnemy(CardModel card) => card.CombatState?.HittableEnemies
        .OrderBy(enemy => enemy.CurrentHp).FirstOrDefault();

    internal static decimal ResolveValue(CardModel card, string name, Creature? target)
    {
        if (name == "$upgraded") return card.IsUpgraded ? 1 : 0;
        if (name == "$ownerLostHpThisTurn") return card.CombatState != null
            && CombatManager.Instance.History.Entries.OfType<DamageReceivedEntry>()
                .Any(entry => entry.HappenedThisTurn(card.CombatState) && entry.Receiver == card.Owner.Creature
                    && entry.Result.UnblockedDamage > 0) ? 1 : 0;
        if (name == "$block") return card.Owner.Creature.Block;
        if (name == "$hp") return card.Owner.Creature.CurrentHp;
        if (name == "$enemies") return card.CombatState?.HittableEnemies.Count ?? 0;
        if (name == "$spentSecondary") return DesireCombatSpending.Get(card.Owner);
        if (name == "$buffLayers") return PowerLayerQuery.CountBuffLayers(card.Owner.Creature);
        if (name == "$targetBelowHalf") return target != null && target.CurrentHp * 2 < target.MaxHp ? 1 : 0;
        if (name == "$targetControlBlock") return target?.Monster?.NextMove.Intents
            .OfType<ControlIntent>().FirstOrDefault()?.BlockRequired ?? 0;
        if (name.StartsWith("$targetHasPower:", StringComparison.Ordinal))
        {
            string type = name[16..];
            return target?.Powers.Any(power => power.GetType().Name == type || power.GetType().FullName == type) == true ? 1 : 0;
        }
        if (name.StartsWith("$power:", StringComparison.Ordinal))
        {
            string powerType = name[7..];
            return card.Owner.Creature.Powers.FirstOrDefault(power => power.GetType().Name == powerType
                || power.GetType().FullName == powerType)?.Amount ?? 0;
        }
        if (name.StartsWith("$pile:", StringComparison.Ordinal)
            && Enum.TryParse(name[6..], out PileType pile))
        {
            var cards = pile.GetPile(card.Owner).Cards;
            return cards.Count - (pile == PileType.Hand && cards.Contains(card) ? 1 : 0);
        }
        if (!card.DynamicVars.TryGetValue(name, out DynamicVar? value))
            throw new InvalidOperationException($"Missing humility value {card.Id}:{name}.");
        return value is CalculatedVar calculated ? calculated.Calculate(target) : value.BaseValue;
    }

    public async Task Damage(decimal baseAmount, HumilityTarget target, int hits, HumilityAttackSource source)
    {
        if (!CanContinue) return;
        target = ResolveTarget(play.Card, target);
        AttackCommand attack = DamageCmd.Attack(baseAmount).WithHitCount(hits);
        if (source == HumilityAttackSource.Osty)
        {
            // Summoning was removed with the card's other effects. Never substitute
            // the player when the original attack's required source does not exist.
            if (play.Player.Osty is not { IsDead: false } osty) return;
            attack.FromOsty(osty, play.Card, play);
        }
        else attack.FromCard(play.Card, play);
        switch (target)
        {
            case HumilityTarget.Selected:
                if (play.Target?.IsHittable != true) return;
                attack.Targeting(play.Target);
                break;
            case HumilityTarget.Self: attack.Targeting(play.Player.Creature); break;
            case HumilityTarget.LowestHpEnemy:
                var lowest = LowestHpEnemy(play.Card);
                if (lowest == null) return;
                attack.Targeting(lowest);
                break;
            case HumilityTarget.AllEnemies: attack.TargetingAllOpponents(_combat!); break;
            case HumilityTarget.RandomEnemy: attack.TargetingRandomOpponents(_combat!); break;
            default: throw new InvalidOperationException("Unsupported humility damage target.");
        }
        await attack.WithHitFx("vfx/vfx_attack_slash").Execute(context);
        _damageResults[_effectIndex] = attack.Results.SelectMany(results => results)
            .Sum(result => result.TotalDamage + result.OverkillDamage);
    }

    public async Task Block(decimal baseAmount, HumilityTarget target, int repetitions)
    {
        for (int index = 0; index < repetitions && CanContinue; index++)
        {
            IEnumerable<Creature> targets = target switch
            {
                HumilityTarget.Self => [play.Player.Creature],
                HumilityTarget.Selected => play.Target != null ? [play.Target] : [],
                HumilityTarget.AllAllies => _combat!.Allies,
                HumilityTarget.AllPlayers => _combat!.GetTeammatesOf(play.Player.Creature)
                    .Where(creature => creature.IsAlive && creature.IsPlayer),
                _ => throw new InvalidOperationException("Unsupported humility block target."),
            };
            foreach (Creature creature in targets.ToArray())
            {
                if (!CanContinue) return;
                if (!creature.IsDead) await CreatureCmd.GainBlock(creature, baseAmount, ValueProp.Move, play);
            }
        }
    }
}
