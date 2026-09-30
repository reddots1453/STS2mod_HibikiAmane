using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Presentation;

namespace MaidenSuccubus.Core.Intents;

public static class EroticEffectCmd
{
    public static async Task Resolve(
        PlayerChoiceContext context,
        MonsterModel source,
        Creature target,
        string effect,
        bool applyDesireFromText = false)
    {
        if (string.IsNullOrWhiteSpace(effect)) return;

        if (applyDesireFromText && target.Player != null)
        {
            int desire = Number(effect, @"(?:并使)?欲望增加(\d+)");
            if (desire > 0)
            {
                await MaidenSuccubus.Data.Desire.Modify(target.Player, desire);
            }
        }
        if (effect.Contains("撕裂衣服", StringComparison.Ordinal))
        {
            await TransformationCmd.LoseArmor(context, target, 1, source);
            if (PerformanceAudience.IsLocalMaiden(target.Player))
            {
                PerformanceAudioService.PlayOneShot(PerformanceAudioCue.ClothesTear);
            }
        }
        await ApplyOtherEnemyResources(context, source, effect);
        await ApplySelfResources(context, source, effect);
        await ApplyTargetDebuffs(context, source, target, effect);
        await AddCombatCards(source, target, effect);

        int heal = Number(effect, @"恢复自身(\d+)点生命值");
        if (heal > 0) await CreatureCmd.Heal(source.Creature, heal);

        int stolen = Number(effect, @"偷取(\d+)金币");
        if (stolen > 0 && target.Player != null)
        {
            await PlayerCmd.LoseGold(stolen, target.Player, GoldLossType.Stolen);
        }
    }

    private static async Task ApplyOtherEnemyResources(
        PlayerChoiceContext context,
        MonsterModel source,
        string effect)
    {
        int block = Number(effect, @"(?:所有)?其他敌人获得(\d+)点格挡");
        int summonBlock = Number(effect, @"召唤物获得(\d+)点格挡");
        int strength = Number(effect, @"(?:所有)?其他敌人获得(\d+)点力量");
        int summonStrength = Number(effect, @"召唤物获得(\d+)点力量");
        foreach (Creature enemy in source.CombatState.Enemies.Where(enemy =>
            enemy.IsAlive && !ReferenceEquals(enemy, source.Creature)))
        {
            int targetBlock = block + (enemy.IsSecondaryEnemy ? summonBlock : 0);
            int targetStrength = strength
                + (enemy.IsSecondaryEnemy ? summonStrength : 0);
            if (targetBlock > 0)
            {
                await CreatureCmd.GainBlock(
                    enemy, targetBlock, ValueProp.Move, null);
            }
            if (targetStrength > 0)
            {
                await PowerCmd.Apply<StrengthPower>(
                    context, enemy, targetStrength,
                    source.Creature, null);
            }
        }
    }

    private static async Task ApplySelfResources(
        PlayerChoiceContext context,
        MonsterModel source,
        string effect)
    {
        string withoutOthers = Regex.Replace(
            effect,
            @"(?:所有)?其他敌人获得\d+点(?:格挡|力量)|召唤物获得\d+点(?:格挡|力量)",
            string.Empty);
        int block = Number(withoutOthers, @"获得(\d+)点格挡");
        int strength = Number(withoutOthers, @"获得(\d+)点力量");
        if (block > 0)
        {
            await CreatureCmd.GainBlock(
                source.Creature, block, ValueProp.Move, null);
        }
        if (strength > 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                context, source.Creature, strength,
                source.Creature, null);
        }
    }

    private static async Task ApplyTargetDebuffs(
        PlayerChoiceContext context,
        MonsterModel source,
        Creature target,
        string effect)
    {
        foreach (Match match in Regex.Matches(
            effect,
            @"(?:给予|并给予|和)(\d+)层(虚弱|易伤|脆弱|燃烧)"))
        {
            int amount = int.Parse(match.Groups[1].Value);
            switch (match.Groups[2].Value)
            {
                case "虚弱":
                    await PowerCmd.Apply<WeakPower>(context, target, amount, source.Creature, null);
                    break;
                case "易伤":
                    await PowerCmd.Apply<VulnerablePower>(context, target, amount, source.Creature, null);
                    break;
                case "脆弱":
                    await PowerCmd.Apply<FrailPower>(context, target, amount, source.Creature, null);
                    break;
                case "燃烧":
                    await PowerCmd.Apply<BurningPower>(context, target, amount, source.Creature, null);
                    break;
            }
        }
    }

    private static async Task AddCombatCards(
        MonsterModel source,
        Creature target,
        string effect)
    {
        if (target.Player == null) return;
        foreach (Match match in Regex.Matches(
            effect,
            @"将(\d+)张([^；。，]+?)(置入弃牌堆|洗入弃牌堆|洗入抽牌堆)"))
        {
            int count = int.Parse(match.Groups[1].Value);
            string card = match.Groups[2].Value.Trim('“', '”', ' ');
            PileType pile = match.Groups[3].Value.Contains("抽牌堆", StringComparison.Ordinal)
                ? PileType.Draw : PileType.Discard;
            if (card.Contains("发情", StringComparison.Ordinal))
            {
                await AddToCombat<ArousalStatus>(target.Player, pile, count, source);
            }
            else if (card.Contains("晕眩", StringComparison.Ordinal))
            {
                await AddToCombat<Dazed>(target.Player, pile, count, source);
            }
            else if (card.Contains("黏液", StringComparison.Ordinal)
                || card.Contains("粘液", StringComparison.Ordinal))
            {
                await AddToCombat<Slimed>(target.Player, pile, count, source);
            }
            else if (card.Contains("感染", StringComparison.Ordinal))
            {
                await AddToCombat<Infection>(target.Player, pile, count, source);
            }
            else if (card.Contains("倒刺钩", StringComparison.Ordinal))
            {
                await AddToCombat<BarbedHookStatus>(target.Player, pile, count, source);
            }
            else if (card.Contains("衣物燃烧", StringComparison.Ordinal))
            {
                await AddToCombat<ClothingBurnStatus>(target.Player, pile, count, source);
            }
            else if (card.Contains("咬衣纸片", StringComparison.Ordinal))
            {
                await AddToCombat<BitingPaperStatus>(target.Player, pile, count, source);
            }
            else if (card.Contains("溶解液", StringComparison.Ordinal))
            {
                await AddToCombat<DissolvingFluidStatus>(target.Player, pile, count, source);
            }
        }
    }

    public static void Validate(string effect, EroticIntentKind kind)
    {
        if (string.IsNullOrWhiteSpace(effect))
        {
            throw new InvalidDataException($"{kind} intent has empty effect text.");
        }

        string remaining = effect;
        string[] supportedPatterns =
        [
            @"造成\d+点伤害(?:\d+次)?",
            @"(?:并使)?欲望增加\d+",
            @"(?:给予|并给予|和)\d+层(?:虚弱|易伤|脆弱|燃烧)",
            @"(?:并使)?(?:所有)?其他敌人获得\d+点(?:格挡|力量)",
            @"召唤物获得\d+点(?:格挡|力量)",
            @"(?:并)?获得\d+点(?:格挡|力量)",
            @"将\d+张[^；。，]+?(?:置入弃牌堆|洗入弃牌堆|洗入抽牌堆)",
            @"恢复自身\d+点生命值",
            @"(?:并)?偷取\d+金币",
            @"需要\d+点格挡阻止",
            @"失败则拘束(?:攻击|技能|能力)牌",
            @"挣脱值\d+",
            @"将\d+张[“""][^”""]+[”""]加入牌组",
            @"撕裂衣服",
        ];
        foreach (string pattern in supportedPatterns)
        {
            remaining = Regex.Replace(remaining, pattern, string.Empty);
        }
        remaining = Regex.Replace(remaining, @"[\s，；。、。]", string.Empty);
        remaining = remaining.Replace("并", string.Empty, StringComparison.Ordinal)
            .Replace("和", string.Empty, StringComparison.Ordinal);
        if (remaining.Length > 0)
        {
            throw new InvalidDataException(
                $"Unsupported {kind} effect clause '{remaining}' in '{effect}'.");
        }
    }

    public static IReadOnlyList<AbstractIntent> BuildSupplementalIntents(
        string effect,
        EroticIntentKind kind)
    {
        var intents = new List<AbstractIntent>();
        string selfEffect = Regex.Replace(effect,
            @"(?:所有)?其他敌人获得\d+点(?:格挡|力量)|召唤物获得\d+点(?:格挡|力量)", string.Empty);
        foreach (var (amount, recipient) in new[]
        {
            (Number(selfEffect, @"获得(\d+)点格挡"), EroticBlockRecipient.Self),
            (Number(effect, @"(?:所有)?其他敌人获得(\d+)点格挡"), EroticBlockRecipient.OtherEnemies),
            (Number(effect, @"召唤物获得(\d+)点格挡"), EroticBlockRecipient.Summons),
        })
        {
            if (amount > 0) intents.Add(new EroticBlockIntent(amount, recipient));
        }
        if (Regex.IsMatch(effect, @"获得\d+点力量"))
        {
            intents.Add(new BuffIntent());
        }
        if (Regex.IsMatch(effect, @"(?:虚弱|易伤|脆弱|燃烧|偷取\d+金币)"))
        {
            intents.Add(new DebuffIntent());
        }
        foreach (Match match in Regex.Matches(
            effect,
            @"将(\d+)张([^；。，]+?)(置入弃牌堆|洗入弃牌堆|洗入抽牌堆)"))
        {
            string cardName = match.Groups[2].Value.Trim('“', '”', ' ');
            intents.Add(new ClothingHazardIntent(
                cardName, int.Parse(match.Groups[1].Value), match.Groups[3].Value));
        }
        if (effect.Contains("恢复自身", StringComparison.Ordinal))
        {
            intents.Add(new HealIntent());
        }
        if (kind == EroticIntentKind.Invasion)
        {
            int desire = Number(effect, @"(?:并使)?欲望增加(\d+)");
            if (desire > 0)
                intents.Add(new DesireGainIntent(desire, "侵犯"));
        }
        if (effect.Contains("撕裂衣服", StringComparison.Ordinal))
        {
            intents.Add(new TearClothingIntent());
        }
        return intents;
    }

    private static async Task AddToCombat<T>(
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        PileType pile,
        int count,
        MonsterModel source)
        where T : CardModel
    {
        ArgumentNullException.ThrowIfNull(player.Creature.CombatState);
        CardPilePosition position = pile == PileType.Draw
            ? CardPilePosition.Random
            : CardPilePosition.Bottom;
        for (int index = 0; index < count; index++)
        {
            CardModel card = player.Creature.CombatState.CreateCard<T>(player);
            await CardPileCmd.Add(card, pile, position, source);
        }
    }

    private static int Number(string text, string pattern)
    {
        Match match = Regex.Match(text, pattern);
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }
}
