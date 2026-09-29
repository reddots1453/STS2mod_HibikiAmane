#if DEBUG
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Data;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncShatterRandomContract
{
    internal static readonly Type[] Types = [typeof(CycloneRupture), typeof(LightningKick), typeof(ExplosiveImpact), typeof(BlasphemousTwilight)];

    internal static void Validate(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        if (!Types.Contains(card.GetType())) return;
        string expected = card switch
        {
            CycloneRupture => $"造成5点伤害。\n给予{(upgraded ? 2 : 1)}层破碎。",
            LightningKick => $"造成{(upgraded ? 12 : 10)}点伤害。\n给予{(upgraded ? 5 : 4)}层破碎。",
            ExplosiveImpact => $"随机造成{(upgraded ? 5 : 3)}点伤害2次。\n给予受到伤害的敌人2层破碎。",
            _ => $"随机对敌人造成{(upgraded ? 6 : 5)}点伤害{(upgraded ? 6 : 5)}次。"
        };
        ctx.AssertEqual("shatter/random attack type", CardType.Attack, card.Type, effect: false);
        ctx.AssertEqual("shatter/random cost", card is CycloneRupture ? 0 : card is LightningKick ? 2 : 1,
            card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        ctx.AssertEqual("shatter/random rarity", card is BlasphemousTwilight ? CardRarity.Rare : card is ExplosiveImpact ? CardRarity.Uncommon : CardRarity.Common,
            card.Rarity, effect: false);
        ctx.AssertEqual("shatter/random target", card is ExplosiveImpact or BlasphemousTwilight ? TargetType.RandomEnemy : TargetType.AnyEnemy,
            card.TargetType, effect: false);
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        foreach (var (instance, pile) in new[] { (outside, PileType.Deck), (card, PileType.Hand) })
            ctx.AssertEqual("shatter/random full text " + pile, expected,
                Regex.Replace(instance.GetDescriptionForPile(pile), @"\[[^\]]*\]", ""), effect: false);
    }

    internal static async Task Targeted(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        int damage = card is CycloneRupture ? 5 : upgraded ? 12 : 10;
        int layers = card is CycloneRupture ? upgraded ? 2 : 1 : upgraded ? 5 : 4;
        int hp = ctx.PrimaryEnemy.CurrentHp;
        await ctx.Play(card, ctx.PrimaryEnemy);
        ctx.AssertDamage("targeted shatter card damage", ctx.PrimaryEnemy, hp, damage);
        ctx.AssertPower<ShatterPower>("targeted shatter card layers", ctx.PrimaryEnemy, layers);
        await VerifyShatter(ctx);
    }

    private static async Task VerifyShatter(CardEffectTestContext ctx)
    {
        await ctx.Reset();
        var choice = new BlockingPlayerChoiceContext();
        foreach (var target in new[] { ctx.Self, ctx.PrimaryEnemy })
        {
            var opponent = target == ctx.Self ? ctx.PrimaryEnemy : ctx.Self;
            await ctx.ApplyPower<ShatterPower>(target, 2);
            var power = target.GetPower<ShatterPower>()!;
            foreach (var (props, expected) in new[]
            {
                (ValueProp.Unpowered, 3), (ValueProp.Unpowered | ValueProp.Move, 3),
                (ValueProp.Unblockable | ValueProp.Unpowered, 3),
                (ValueProp.Move, 5), (ValueProp.Move | ValueProp.Unblockable, 5)
            })
            {
                int hp = target.CurrentHp;
                await CreatureCmd.Damage(choice, target, 3, props, opponent);
                ctx.AssertDamage(target.Side + " shatter damage type " + props, target, hp, expected);
            }
            int before = target.CurrentHp;
            await CreatureCmd.Damage(choice, target, 3, ValueProp.Move, target);
            ctx.AssertDamage("same-side damage is not amplified", target, before, 3);
            before = target.CurrentHp;
            await CreatureCmd.Damage(choice, target, 3, ValueProp.Move, null, null, null);
            ctx.AssertDamage("damage with no dealer is not amplified", target, before, 3);
            before = target.CurrentHp;
            if (target == ctx.Self)
                await DamageCmd.Attack(4).FromMonster(opponent.Monster!).WithHitCount(2).Execute(choice);
            else
                await ctx.Play(ctx.Create<MaidenStrike>(), target);
            ctx.AssertDamage("real attack applies shatter per hit", target, before, target == ctx.Self ? 12 : 8);
            var otherSide = target.Side == CombatSide.Player ? CombatSide.Enemy : CombatSide.Player;
            await power.AfterSideTurnEnd(choice, otherSide, [opponent]);
            ctx.AssertPower<ShatterPower>("other side cannot tick shatter", target, 2);
            await power.AfterSideTurnEnd(choice, target.Side, [target]);
            ctx.AssertPower<ShatterPower>("owner side ticks once", target, 1);
            before = target.CurrentHp;
            await CreatureCmd.Damage(choice, target, 3, ValueProp.Move, opponent);
            ctx.AssertDamage("damage bonus follows remaining layers", target, before, 4);
            await power.AfterSideTurnEnd(choice, target.Side, [target]);
            ctx.AssertTrue("shatter is removed at zero", !target.HasPower<ShatterPower>());
        }
    }

    internal static async Task Random(CardEffectTestContext ctx, CardModel template, bool upgraded)
    {
        foreach (int strength in new[] { 0, 2 })
        {
            await ctx.Reset();
            var extra = await CreatureCmd.Add<Byrdonis>(ctx.Combat);
            try
            {
                foreach (var power in extra.Powers.ToArray()) await PowerCmd.Remove(power);
                await CreatureCmd.SetMaxAndCurrentHp(extra, 20_000);
                if (strength > 0) await ctx.ApplyPower<StrengthPower>(ctx.Self, strength);
                if (template is BlasphemousTwilight) await Desire.Set(ctx.Player, 4);
                var card = ctx.Create(template.GetType(), upgraded);
                int perHit = (card is ExplosiveImpact ? upgraded ? 5 : 3 : upgraded ? 6 : 5) + strength;
                int hits = card is ExplosiveImpact ? 2 : upgraded ? 6 : 5;
                var hp = ctx.Enemies.ToDictionary(enemy => enemy, enemy => enemy.CurrentHp);
                ctx.AssertTrue("random attack fixture really has multiple enemies", hp.Count >= 2);
                await ctx.Play(card);
                ctx.AssertEqual("random attack total exact damage across every enemy", perHit * hits,
                    hp.Sum(pair => pair.Value - pair.Key.CurrentHp));
                int observedHits = 0;
                foreach (var pair in hp)
                {
                    int lost = pair.Value - pair.Key.CurrentHp;
                    ctx.AssertTrue("per-enemy damage is an exact number of hits", lost >= 0 && lost % perHit == 0);
                    observedHits += lost / perHit;
                    ctx.AssertPower<ShatterPower>("only hit targets get shatter once, never per hit", pair.Key,
                        card is ExplosiveImpact && lost > 0 ? 2 : 0);
                }
                ctx.AssertEqual("random attack exact hit count", hits, observedHits);
            }
            finally { if (!extra.IsDead) await CreatureCmd.Escape(extra); }
        }
    }
}
#endif
